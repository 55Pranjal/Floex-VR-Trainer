using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Oculus.Interaction;

/// <summary>
/// Workaround for Meta XR Interaction SDK v74.0.0 behaviour where only one
/// world-space canvas at a time receives click events from a shared RayInteractor.
///
/// This script bypasses PointableCanvasModule entirely for any canvas it's attached to.
/// Every frame it samples the RayInteractor's ray, intersects it with this canvas's
/// plane, and if the hit point is inside the canvas rect AND the interactor's selector
/// fired, it dispatches a synthetic pointer click to the UI element under the hit point.
///
/// Setup:
///   1. Attach to each canvas that's failing (PumpHead_01_Canvas, _02_Canvas, _03_Canvas).
///   2. In Inspector, drag all RayInteractor components in the scene into the
///      'Ray Interactors' list (typically left & right hand interactors).
///   3. Leave a PointableCanvasModule in the scene — it's harmless and may still
///      service other canvases (like the main pole canvas).
///   4. If this canvas also uses Poke (direct touch), uncheck 'Disable Pointable Canvas'
///      so PointableCanvas stays active and routes Poke events normally. Ray still flows
///      through this script's bypass path; the two pipelines don't conflict because each
///      handles a different interactor source.
///
/// Notes:
///   - This dispatches "click" only for Ray. Hover/drag/scroll are not bypassed.
///   - The visible laser still comes from Meta's interactor; we just listen to its state.
///   - Works for any number of canvases; each does its own intersection independently.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CanvasClickBypass : MonoBehaviour
{
    [Header("Ray sources (drag all RayInteractors here)")]
    public List<RayInteractor> rayInteractors = new List<RayInteractor>();

    [Header("PointableCanvas handling")]
    [Tooltip("If true, disable this canvas's PointableCanvas in Awake (Day 19 fix). " +
             "Set false on canvases that also use Poke, which needs PointableCanvas active.")]
    public bool disablePointableCanvas = true;

    [Header("Debug")]
    public bool logClicks = false;

    readonly Dictionary<GameObject, float> lastClickTime = new Dictionary<GameObject, float>();
    const float ClickCooldownSeconds = 0.3f;

    Canvas canvas;
    GraphicRaycaster raycaster;
    EventSystem eventSystem;
    readonly Dictionary<RayInteractor, bool> wasSelecting = new Dictionary<RayInteractor, bool>();
    readonly Dictionary<RayInteractor, GameObject> currentHover = new Dictionary<RayInteractor, GameObject>();

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null) raycaster = gameObject.AddComponent<GraphicRaycaster>();

        // Disable Meta's PointableCanvas on this canvas so it doesn't compete with us.
        // We own all click dispatch for Ray on this canvas; PCM still serves other canvases.
        // Skipped on Poke-enabled canvases — Poke events flow through PointableCanvas and
        // need it active. Ray still uses the bypass path either way (Ray events never
        // reach PCM because this script runs in Update before PCM's frame).
        if (disablePointableCanvas)
        {
            var pc = GetComponent<Oculus.Interaction.PointableCanvas>();
            if (pc != null) pc.enabled = false;
        }
    }

    void Start()
    {
        eventSystem = EventSystem.current;
        if (eventSystem == null) eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError($"[CanvasClickBypass:{name}] No EventSystem found in scene.");
            enabled = false;
        }

        // Auto-populate ray interactors if none assigned in Inspector.
        if (rayInteractors.Count == 0)
        {
            rayInteractors.AddRange(FindObjectsOfType<RayInteractor>());
            if (rayInteractors.Count == 0)
            {
                Debug.LogWarning($"[CanvasClickBypass:{name}] No RayInteractors found in scene.");
            }
        }
    }

    void Update()
    {
        foreach (RayInteractor interactor in rayInteractors)
        {
            if (interactor == null) continue;
            ProcessInteractor(interactor);
        }
    }

    void ProcessInteractor(RayInteractor interactor)
    {
        // Edge-detect on the interactor's selection state (button press).
        bool isSelecting = interactor.State == InteractorState.Select;
        bool wasSel;
        wasSelecting.TryGetValue(interactor, out wasSel);
        wasSelecting[interactor] = isSelecting;
        bool clickEdge = isSelecting && !wasSel;

        Vector3 origin = interactor.Origin;
        Vector3 direction = interactor.Forward;

        Vector3 planeNormal = -transform.forward;
        Vector3 planePoint = transform.position;

        float denom = Vector3.Dot(direction, planeNormal);
        if (Mathf.Abs(denom) < 1e-6f) { ClearHover(interactor); return; }

        float t = Vector3.Dot(planePoint - origin, planeNormal) / denom;
        if (t < 0f) { ClearHover(interactor); return; }

        Vector3 worldHit = origin + direction * t;

        RectTransform rt = transform as RectTransform;
        Vector2 localHit = rt.InverseTransformPoint(worldHit);
        Rect rect = rt.rect;
        if (!rect.Contains(localHit)) { ClearHover(interactor); return; }

        if (canvas.worldCamera == null)
        {
            canvas.worldCamera = Camera.main;
            if (canvas.worldCamera == null) return;
        }

        Vector2 screenPos = canvas.worldCamera.WorldToScreenPoint(worldHit);

        PointerEventData ped = new PointerEventData(eventSystem)
        {
            position = screenPos,
            pressPosition = screenPos,
            button = PointerEventData.InputButton.Left,
        };

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(ped, results);

        GameObject hit = results.Count > 0 ? results[0].gameObject : null;

        // --- Hover dispatch ---
        GameObject prev;
        currentHover.TryGetValue(interactor, out prev);
        if (hit != prev)
        {
            if (prev != null)
                ExecuteEvents.ExecuteHierarchy(prev, ped, ExecuteEvents.pointerExitHandler);
            if (hit != null)
                ExecuteEvents.ExecuteHierarchy(hit, ped, ExecuteEvents.pointerEnterHandler);
            currentHover[interactor] = hit;
        }

        // --- Click dispatch (with per-target cooldown to suppress state flicker re-fires) ---
        if (clickEdge && hit != null)
        {
            float now = Time.unscaledTime;
            float lastTime;
            lastClickTime.TryGetValue(hit, out lastTime);
            if (now - lastTime >= ClickCooldownSeconds)
            {
                if (logClicks) Debug.Log($"[CanvasClickBypass:{name}] Click on {hit.name}");
                ExecuteEvents.ExecuteHierarchy(hit, ped, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.ExecuteHierarchy(hit, ped, ExecuteEvents.pointerClickHandler);
                ExecuteEvents.ExecuteHierarchy(hit, ped, ExecuteEvents.pointerUpHandler);
                lastClickTime[hit] = now;
            }
            else if (logClicks)
            {
                Debug.Log($"[CanvasClickBypass:{name}] Click on {hit.name} suppressed (cooldown)");
            }
        }
    }

    void ClearHover(RayInteractor interactor)
    {
        GameObject prev;
        if (currentHover.TryGetValue(interactor, out prev) && prev != null)
        {
            PointerEventData ped = new PointerEventData(eventSystem);
            ExecuteEvents.ExecuteHierarchy(prev, ped, ExecuteEvents.pointerExitHandler);
            currentHover[interactor] = null;
        }
    }
}
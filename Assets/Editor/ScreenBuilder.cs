// ScreenBuilder.cs
// Floex VR Trainer — generates a CDM-style console screen from a JSON spec, in one click.
//
// PRODUCT A SCOPE LOCK: this builder places STATIC display elements only.
// It never adds scripts, animations, listeners, or any behaviour. Do not extend it to.
//
// Place this file in:  Assets/Editor/ScreenBuilder.cs   (the "Editor" folder is required)
// Place spec files in:  Assets/ScreenSpecs/*.json
// Sprites are resolved by name from: Assets/Textures/UI/<spriteName>.png
//
// Usage:  Unity menu  ->  Floex  ->  Build Screen From JSON...
//   1. Pick a *.json spec.
//   2. It builds under the active Canvas (or a Canvas you select), as a child named after "screenName".
//   3. If a screen with that name already exists under the Canvas, you're asked before it's replaced.
//
// Coordinate convention (matches the hand-build):
//   TouchGFX (X, Y) top-left origin, Y down  ->  Unity anchoredPosition (X, -Y),
//   with anchor = top-left and pivot = top-left (0,1). Same W/H.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScreenBuilder
{
    // ---- JSON DTOs (Unity's JsonUtility maps these) ----
    [Serializable] public class Spec
    {
        public string screenName;
        public int canvasWidth = 1280;
        public int canvasHeight = 800;
        public Group[] groups;
    }
    [Serializable] public class Group
    {
        public string name;
        public Element[] elements;
    }
    [Serializable] public class Element
    {
        public string name;
        public string type;     // "text" | "image"
        public float x, y, w, h;
        public bool stretch;     // image only: full-screen stretch-fill
        public string color;     // hex "RRGGBB" (images, and text colour)
        public string sprite;    // image only: filename stem under Assets/Textures/UI/
        public string text;      // text only
        public float size = 21;  // text only
        public string align = "left"; // text only: left|center|right
        public bool bold;        // text only
    }

    private const string SpriteDir = "Assets/Textures/UI";

    [MenuItem("Floex/Build Screen From JSON...")]
    public static void BuildFromJson()
    {
        string path = EditorUtility.OpenFilePanel("Select screen spec JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        string json;
        try { json = File.ReadAllText(path); }
        catch (Exception e) { EditorUtility.DisplayDialog("Read error", e.Message, "OK"); return; }

        Spec spec;
        try { spec = JsonUtility.FromJson<Spec>(json); }
        catch (Exception e) { EditorUtility.DisplayDialog("JSON parse error", e.Message, "OK"); return; }

        if (spec == null || string.IsNullOrEmpty(spec.screenName) || spec.groups == null)
        {
            EditorUtility.DisplayDialog("Invalid spec", "Missing screenName or groups.", "OK");
            return;
        }

        Canvas canvas = ResolveCanvas();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("No Canvas",
                "Select a Canvas in the hierarchy (or have one in the scene) before building.", "OK");
            return;
        }

        // Replace existing screen of same name, if present (with confirmation).
        Transform existing = canvas.transform.Find(spec.screenName);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Screen exists",
                $"'{spec.screenName}' already exists under {canvas.name}. Replace it?\n\n" +
                "This deletes the existing GameObject and rebuilds it from the spec.",
                "Replace", "Cancel");
            if (!replace) return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        int warnings = 0;

        // Screen root (stretch-fill the canvas, so child top-left coords map cleanly).
        GameObject screen = NewUIObject(spec.screenName, canvas.transform);
        StretchFill(screen.GetComponent<RectTransform>());
        Undo.RegisterCreatedObjectUndo(screen, "Build Screen");

        foreach (var group in spec.groups)
        {
            if (group == null || string.IsNullOrEmpty(group.name)) continue;
            GameObject groupGo = NewUIObject(group.name, screen.transform);
            StretchFill(groupGo.GetComponent<RectTransform>()); // group is an organisational parent

            if (group.elements == null) continue;
            foreach (var el in group.elements)
            {
                if (el == null || string.IsNullOrEmpty(el.name)) continue;
                if (el.type == "text")      BuildText(el, groupGo.transform);
                else if (el.type == "image") BuildImage(el, groupGo.transform, ref warnings);
                else Debug.LogWarning($"[ScreenBuilder] Unknown type '{el.type}' on '{el.name}' — skipped.");
            }
        }

        Selection.activeGameObject = screen;
        Debug.Log($"[ScreenBuilder] Built '{spec.screenName}' under '{canvas.name}'. " +
                  (warnings == 0 ? "No warnings." : $"{warnings} sprite warning(s) — see above."));
    }

    // ---- builders ----
    private static void BuildText(Element el, Transform parent)
    {
        GameObject go = NewUIObject(el.name, parent);
        var rt = go.GetComponent<RectTransform>();
        TopLeft(rt, el.x, el.y, el.w, el.h);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = el.text ?? "";
        tmp.fontSize = el.size;
        tmp.color = Hex(string.IsNullOrEmpty(el.color) ? "FFFFFF" : el.color);
        tmp.fontStyle = el.bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.enableWordWrapping = false;
        switch ((el.align ?? "left").ToLowerInvariant())
        {
            case "center": tmp.alignment = TextAlignmentOptions.Center; break;
            case "right":  tmp.alignment = TextAlignmentOptions.Right;  break;
            default:        tmp.alignment = TextAlignmentOptions.Left;   break;
        }
        // vertical middle, matching the hand-build
        tmp.alignment = MidVertical(tmp.alignment);
    }

    private static void BuildImage(Element el, Transform parent, ref int warnings)
    {
        GameObject go = NewUIObject(el.name, parent);
        var rt = go.GetComponent<RectTransform>();
        var img = go.AddComponent<Image>();

        if (el.stretch)
        {
            StretchFill(rt);
        }
        else
        {
            TopLeft(rt, el.x, el.y, el.w, el.h);
        }

        if (!string.IsNullOrEmpty(el.sprite))
        {
            Sprite s = LoadSprite(el.sprite);
            if (s != null) { img.sprite = s; img.preserveAspect = true; }
            else { warnings++; Debug.LogWarning($"[ScreenBuilder] Sprite '{el.sprite}' not found in {SpriteDir}/ for '{el.name}'. Placed as coloured box."); img.color = Hex(string.IsNullOrEmpty(el.color) ? "FFFFFF" : el.color); }
        }
        else
        {
            img.color = Hex(string.IsNullOrEmpty(el.color) ? "FFFFFF" : el.color);
        }
    }

    // ---- helpers ----
    private static Canvas ResolveCanvas()
    {
        // Prefer a selected Canvas (or one in the selection's parents).
        if (Selection.activeGameObject != null)
        {
            var c = Selection.activeGameObject.GetComponentInParent<Canvas>();
            if (c != null) return c;
        }
        // Otherwise the first Canvas in the scene.
        return UnityEngine.Object.FindObjectOfType<Canvas>();
    }

    private static GameObject NewUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    // Top-left anchor + pivot; TouchGFX (x,y) -> anchoredPosition (x, -y).
    private static void TopLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, -y);
    }

    private static void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Sprite LoadSprite(string stem)
    {
        // Try common extensions under the UI textures folder.
        string[] exts = { ".png", ".PNG", ".jpg", ".jpeg" };
        foreach (var ext in exts)
        {
            string p = $"{SpriteDir}/{stem}{ext}";
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (s != null) return s;
        }
        return null;
    }

    private static Color Hex(string hex)
    {
        hex = hex.TrimStart('#');
        if (ColorUtility.TryParseHtmlString("#" + hex, out var c)) return c;
        return Color.magenta; // visible "something's wrong" colour
    }

    private static TextAlignmentOptions MidVertical(TextAlignmentOptions horiz)
    {
        switch (horiz)
        {
            case TextAlignmentOptions.Left:   return TextAlignmentOptions.Left;
            case TextAlignmentOptions.Center: return TextAlignmentOptions.Center;
            case TextAlignmentOptions.Right:  return TextAlignmentOptions.Right;
            default: return horiz;
        }
        // Note: TMP's Left/Center/Right are already vertically-middle variants.
    }
}

using UnityEngine;
using UnityEditor;

public class MeasureBounds
{
    [MenuItem("Floex/Measure Selected Bounds")]
    static void Measure()
    {
        var go = Selection.activeGameObject;
        if (go == null) { Debug.LogWarning("Select an object first."); return; }

        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) { Debug.LogWarning("No renderers found."); return; }

        Bounds b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);

        Debug.Log($"[Bounds] {go.name} world size (metres): " +
                  $"X(width)={b.size.x:F3}  Y(height)={b.size.y:F3}  Z(depth)={b.size.z:F3}");
    }
}
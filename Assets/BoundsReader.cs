using UnityEngine;

public class BoundsReader : MonoBehaviour
{
    void Start()
    {
        var r = GetComponent<Renderer>();
        if (r != null)
        {
            Debug.Log($"[BoundsReader] World size (m): {r.bounds.size}  |  World center: {r.bounds.center}");
        }
        else
        {
            Debug.Log("[BoundsReader] No Renderer on this object.");
        }
    }
}
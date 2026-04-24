using UnityEngine;

/// <summary>
/// Spawns a "you are here" marker at world origin (0, 0, 0).
/// The player's GPS position is always at origin; the map tiles move around it.
/// Auto-injects itself into the scene at startup.
/// </summary>
public class PlayerMarker : MonoBehaviour
{
    [SerializeField] private float markerSize = 30f;
    [SerializeField] private Color markerColor = new Color(1f, 0.15f, 0.15f); // red

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindFirstObjectByType<GameManager>() == null) return;
        if (FindFirstObjectByType<PlayerMarker>() != null) return;
        var go = new GameObject("PlayerMarker");
        go.AddComponent<PlayerMarker>();
    }

    private void Start()
    {
        transform.position = Vector3.zero;

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "PlayerSphere";
        sphere.transform.SetParent(transform);
        sphere.transform.localPosition = new Vector3(0f, 5f, 0f);
        sphere.transform.localScale = Vector3.one * markerSize;
        Destroy(sphere.GetComponent<SphereCollider>());

        var mr = sphere.GetComponent<MeshRenderer>();
        var mat = new Material(mr.sharedMaterial);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", markerColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", markerColor);
        mr.material = mat;
    }
}

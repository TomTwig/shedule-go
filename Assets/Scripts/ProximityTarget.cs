using UnityEngine;

/// <summary>
/// A test interactable placed at a fixed world-space offset from the player.
/// Because the player is always at world origin, the offset is location-independent —
/// the object appears at the same relative position regardless of GPS coordinates.
/// </summary>
public class ProximityTarget : MonoBehaviour
{
    [Header("Offset from Player (metres)")]
    [Tooltip("East(+)/West(-) offset in metres from the player.")]
    [SerializeField] private float offsetX = 40f;
    [Tooltip("North(+)/South(-) offset in metres from the player.")]
    [SerializeField] private float offsetZ = 30f;

    [Header("Visual")]
    [SerializeField] private float displayScale = 25f;
    [SerializeField] private Color markerColor  = new Color(0.1f, 0.85f, 0.1f);

    public float DistanceToPlayer { get; private set; }

    private void Start()
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "TargetSphere";
        sphere.transform.SetParent(transform);
        sphere.transform.localPosition = new Vector3(0f, 5f, 0f);
        sphere.transform.localScale    = Vector3.one * displayScale;
        Destroy(sphere.GetComponent<SphereCollider>());

        var mr  = sphere.GetComponent<MeshRenderer>();
        var mat = new Material(mr.sharedMaterial);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", markerColor);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     markerColor);
        mr.material = mat;

        DistanceToPlayer = Mathf.Sqrt(offsetX * offsetX + offsetZ * offsetZ);
    }

    private void Update()
    {
        transform.position = new Vector3(offsetX, 0f, offsetZ);
        DistanceToPlayer   = Mathf.Sqrt(offsetX * offsetX + offsetZ * offsetZ);
    }
}

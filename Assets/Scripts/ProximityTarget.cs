using UnityEngine;

/// <summary>
/// A GPS-positioned interactable object. Tracks its real-world distance to the
/// player every frame so PlayerRangeIndicator can check proximity without doing
/// its own GPS math.
/// </summary>
public class ProximityTarget : MonoBehaviour
{
    [Header("GPS Position")]
    [SerializeField] private double targetLatitude  = 54.3439;
    [SerializeField] private double targetLongitude = 10.1321;

    [Header("Visual")]
    [SerializeField] private float displayScale = 25f;
    [SerializeField] private Color markerColor  = new Color(0.1f, 0.85f, 0.1f);

    public float DistanceToPlayer { get; private set; }

    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;

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
    }

    private void Update()
    {
        if (gameManager == null || !gameManager.IsLocationReady)
        {
            // Not ready — park 80 m east of origin so it's visible during startup.
            transform.position = new Vector3(80f, 0f, 0f);
            DistanceToPlayer   = 80f;
            return;
        }

        Vector3 offset = GeoUtils.GpsToUnityOffset(
            gameManager.SmoothedLatitude,  gameManager.SmoothedLongitude,
            targetLatitude,                targetLongitude);

        transform.position = new Vector3(offset.x, 0f, offset.z);

        DistanceToPlayer = GeoUtils.DistanceMetres(
            gameManager.PlayerLatitude,  gameManager.PlayerLongitude,
            targetLatitude,              targetLongitude);
    }
}

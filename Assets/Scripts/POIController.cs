using UnityEngine;

/// <summary>
/// Represents a single Point of Interest.
/// Keeps its own GPS coordinate and updates its Unity world position
/// relative to the player every frame (player stays at origin).
/// </summary>
public class POIController : MonoBehaviour
{
    [Header("POI GPS Coordinate (hardcoded for MVP)")]
    [SerializeField] private double poiLatitude  = 37.422_131; // Example: Googleplex, CA
    [SerializeField] private double poiLongitude = -122.084_801;

    [Header("Visual")]
    [Tooltip("Y offset so the POI sits on top of the ground plane.")]
    [SerializeField] private float yOffset = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDistanceLog = true;

    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null)
            Debug.LogError("[POIController] GameManager.Instance is null. Add a GameManager to the scene.");
    }

    private void Update()
    {
        if (gameManager == null || !gameManager.IsLocationReady)
        {
            // Location not ready yet — keep POI at a visible test position.
            transform.position = new Vector3(5f, yOffset, 5f);
            return;
        }

        Vector3 offset = GeoUtils.GpsToUnityOffset(
            gameManager.PlayerLatitude,
            gameManager.PlayerLongitude,
            poiLatitude,
            poiLongitude);

        transform.position = new Vector3(offset.x, yOffset, offset.z);

        if (showDistanceLog)
        {
            float dist = GeoUtils.DistanceMetres(
                gameManager.PlayerLatitude, gameManager.PlayerLongitude,
                poiLatitude, poiLongitude);

            Debug.Log($"[POIController] Distance to POI: {dist:F1} m");
        }
    }

    /// <summary>Call this at runtime to change which POI this object represents.</summary>
    public void SetCoordinate(double lat, double lon)
    {
        poiLatitude  = lat;
        poiLongitude = lon;
    }
}

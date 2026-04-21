using UnityEngine;

/// <summary>
/// Represents a single Point of Interest.
/// Keeps its own GPS coordinate and updates its Unity world position
/// relative to the player every frame (player stays at origin).
/// </summary>
public class POIController : MonoBehaviour
{
    [Header("POI GPS Coordinate (hardcoded for MVP)")]
    [SerializeField] private double poiLatitude  = 54.345_000; // ~150 m north within Kiel tiles
    [SerializeField] private double poiLongitude = 10.132_141;

    [Header("Visual")]
    [Tooltip("Y offset so the POI sits on top of the ground plane.")]
    [SerializeField] private float yOffset = 30f;

    [Tooltip("Scale applied to this transform on Start so it's visible from the top-down camera.")]
    [SerializeField] private float displayScale = 50f;

    [Header("Debug")]
    [SerializeField] private bool showDistanceLog = true;

    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null)
            Debug.LogError("[POIController] GameManager.Instance is null. Add a GameManager to the scene.");

        transform.localScale = Vector3.one * displayScale;
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
            gameManager.SmoothedLatitude,
            gameManager.SmoothedLongitude,
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

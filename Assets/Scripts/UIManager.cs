using UnityEngine;
using TMPro;

/// <summary>
/// Displays player GPS coordinates and POI distance on screen.
/// Requires TextMeshPro (included in Unity 2021+ via the Package Manager).
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text coordinatesText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text poiDistanceText;

    [Header("Dependencies")]
    [SerializeField] private GameManager  gameManager;
    [SerializeField] private POIController poiController;

    // POI coordinates cached for distance display — keep in sync with POIController.
    [Header("POI Reference (must match POIController values)")]
    [SerializeField] private double poiLatitude  = 37.422_131;
    [SerializeField] private double poiLongitude = -122.084_801;

    private LocationManager locationManager;

    private void Start()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (gameManager != null)
            locationManager = gameManager.GetComponent<LocationManager>();

        if (poiController == null)
            poiController = FindFirstObjectByType<POIController>();
    }

    private void Update()
    {
        UpdateStatusText();
        UpdateCoordinatesText();
        UpdatePoiDistanceText();
    }

    private void UpdateStatusText()
    {
        if (statusText == null) return;

        if (locationManager == null)
        {
            statusText.text = "Status: No LocationManager";
            return;
        }

        statusText.text = locationManager.IsReady
            ? "GPS: Active"
            : "GPS: Initialising...";
    }

    private void UpdateCoordinatesText()
    {
        if (coordinatesText == null || gameManager == null) return;

        if (locationManager == null || !locationManager.IsReady)
        {
            coordinatesText.text = "Lat: --\nLon: --";
            return;
        }

        coordinatesText.text =
            $"Lat: {gameManager.PlayerLatitude:F6}\n" +
            $"Lon: {gameManager.PlayerLongitude:F6}";
    }

    private void UpdatePoiDistanceText()
    {
        if (poiDistanceText == null || gameManager == null) return;

        if (locationManager == null || !locationManager.IsReady)
        {
            poiDistanceText.text = "POI: -- m";
            return;
        }

        float dist = GeoUtils.DistanceMetres(
            gameManager.PlayerLatitude, gameManager.PlayerLongitude,
            poiLatitude, poiLongitude);

        poiDistanceText.text = dist < 1000f
            ? $"POI: {dist:F0} m"
            : $"POI: {dist / 1000f:F2} km";
    }
}

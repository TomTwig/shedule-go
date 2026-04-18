using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text coordinatesText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text poiDistanceText;

    [Header("Dependencies")]
    [SerializeField] private GameManager   gameManager;
    [SerializeField] private POIController poiController;

    [Header("POI Reference (must match POIController values)")]
    [SerializeField] private double poiLatitude  = 54.345_000;
    [SerializeField] private double poiLongitude = 10.132_141;

    private void Start()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;

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

        if (gameManager == null)         { statusText.text = "Status: No GameManager";    return; }
        if (!gameManager.IsLocationReady){ statusText.text = "GPS: Initialising...";      return; }

        statusText.text = "GPS: Active";
    }

    private void UpdateCoordinatesText()
    {
        if (coordinatesText == null || gameManager == null) return;

        if (!gameManager.IsLocationReady)
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

        if (!gameManager.IsLocationReady)
        {
            poiDistanceText.text = "POI: -- m";
            return;
        }

        float dist = GeoUtils.DistanceMetres(
            gameManager.PlayerLatitude,  gameManager.PlayerLongitude,
            poiLatitude,                 poiLongitude);

        poiDistanceText.text = dist < 1000f
            ? $"POI: {dist:F0} m"
            : $"POI: {dist / 1000f:F2} km";
    }
}

using UnityEngine;

/// <summary>
/// Central game controller.
/// Reads the player's GPS position from LocationManager and broadcasts it to
/// any IPlayerPositionListener in the scene (e.g. POIController, UIManager).
/// </summary>
[DefaultExecutionOrder(-10)] // Runs after LocationManager (-20), before POI/UI/Map (0)
public class GameManager : MonoBehaviour
{
    [SerializeField] private LocationManager locationManager;

    // Cached values — other scripts poll these instead of reading Input.location.
    public double PlayerLatitude { get; private set; }
    public double PlayerLongitude { get; private set; }

    // Smoothed values for visual rendering — interpolate toward actual GPS each frame.
    public double SmoothedLatitude { get; private set; }
    public double SmoothedLongitude { get; private set; }

    [SerializeField] private float smoothSpeed = 5f;

    public bool IsLocationReady => locationManager != null && locationManager.IsReady;

    private bool smoothedInitialized;

    // Singleton for convenient access; only one GameManager should exist.
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.orientation = ScreenOrientation.Portrait;


    }

    private void Start()
    {
        if (locationManager == null)
            locationManager = FindFirstObjectByType<LocationManager>();

        if (locationManager == null)
            Debug.LogError("[GameManager] LocationManager not found in scene.");
    }

    private void Update()
    {
        if (locationManager == null || !locationManager.IsReady)
            return;

        PlayerLatitude = locationManager.CurrentLatitude;
        PlayerLongitude = locationManager.CurrentLongitude;

        if (!smoothedInitialized)
        {
            SmoothedLatitude = PlayerLatitude;
            SmoothedLongitude = PlayerLongitude;
            smoothedInitialized = true;
        }
        else
        {
            float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            SmoothedLatitude += (PlayerLatitude - SmoothedLatitude) * t;
            SmoothedLongitude += (PlayerLongitude - SmoothedLongitude) * t;
        }
    }
}

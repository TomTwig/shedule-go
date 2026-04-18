using System.Collections;
using UnityEngine;

/// <summary>
/// Owns the Unity LocationService lifecycle.
/// Other systems read CurrentLatitude / CurrentLongitude; they never touch Input.location directly.
/// </summary>
public class LocationManager : MonoBehaviour
{
    [Tooltip("Desired accuracy in metres.")]
    [SerializeField] private float desiredAccuracyMetres = 10f;

    [Tooltip("Minimum distance (metres) the device must move before an update fires.")]
    [SerializeField] private float updateDistanceMetres = 1f;

    [Tooltip("Seconds to wait for LocationService to initialise before giving up.")]
    [SerializeField] private float initTimeoutSeconds = 20f;

    public double CurrentLatitude  { get; private set; }
    public double CurrentLongitude { get; private set; }
    public bool   IsReady          { get; private set; }

    private void Start()
    {
        StartCoroutine(InitialiseLocation());
    }

    private IEnumerator InitialiseLocation()
    {
        // On Android the LocationService Start() call itself triggers the runtime permission
        // dialog (handled by Unity). On iOS the NSLocationWhenInUseUsageDescription in
        // Info.plist drives the system prompt — no extra code required here.
#if UNITY_EDITOR
        // Editor: skip permission flow; use the last-known location set in Unity's Fake GPS.
        Debug.Log("[LocationManager] Running in Editor — skipping permission check.");
#elif UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                UnityEngine.Android.Permission.FineLocation))
        {
            UnityEngine.Android.Permission.RequestUserPermission(
                UnityEngine.Android.Permission.FineLocation);

            // Wait a frame for the dialog to appear, then poll until the user responds.
            yield return null;
            float waited = 0f;
            while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                       UnityEngine.Android.Permission.FineLocation) && waited < 30f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                    UnityEngine.Android.Permission.FineLocation))
            {
                Debug.LogWarning("[LocationManager] Location permission denied by user.");
                yield break;
            }
        }
#endif

        Input.location.Start(desiredAccuracyMetres, updateDistanceMetres);

        float elapsed = 0f;
        while (Input.location.status == LocationServiceStatus.Initializing && elapsed < initTimeoutSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            Debug.LogWarning($"[LocationManager] LocationService failed to start. Status: {Input.location.status}");
            yield break;
        }

        IsReady = true;
        Debug.Log("[LocationManager] LocationService running.");
    }

    private void Update()
    {
        if (!IsReady || Input.location.status != LocationServiceStatus.Running)
            return;

        CurrentLatitude  = Input.location.lastData.latitude;
        CurrentLongitude = Input.location.lastData.longitude;
    }

    private void OnDestroy()
    {
        if (Input.location.status == LocationServiceStatus.Running)
            Input.location.Stop();
    }
}

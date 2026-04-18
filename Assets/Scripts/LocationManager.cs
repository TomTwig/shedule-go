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

    [Header("Editor Testing (ignored on device)")]
    [SerializeField] private double editorLatitude  = 48.137_154; // Munich Marienplatz
    [SerializeField] private double editorLongitude = 11.576_124;

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
        // Input.location doesn't work in the Editor — use hardcoded test coordinates instead.
        CurrentLatitude  = editorLatitude;
        CurrentLongitude = editorLongitude;
        IsReady = true;
        Debug.Log($"[LocationManager] Editor mode — fake GPS: {editorLatitude}, {editorLongitude}");
        yield break;
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

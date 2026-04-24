using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Keeps the main camera centered on the player (world origin) and adjusts
/// orthographic size so the tile grid fills the screen in any orientation.
/// Supports pinch-to-zoom within configurable limits.
/// Auto-attaches to the Main Camera on startup.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [SerializeField] private int   zoomLevel    = 16;
    [SerializeField] private int   tilesVisible = 3;
    [SerializeField] private float referenceLat = 54.35f;
    [SerializeField] private float cameraHeight = 800f;

    [Header("Pinch Zoom")]
    [Tooltip("Closest the camera can zoom in (metres of half-height).")]
    [SerializeField] private float minOrthoSize = 100f;
    [Tooltip("Furthest the camera can zoom out (metres of half-height).")]
    [SerializeField] private float maxOrthoSize = 900f;

    private Camera cam;
    private int    lastWidth;
    private int    lastHeight;
    private float  currentOrthoSize = -1f;

    private float prevPinchDist;
    private bool  isPinching;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttach()
    {
        if (FindFirstObjectByType<GameManager>() == null) return;
        var mainCam = Camera.main;
        if (mainCam == null || mainCam.GetComponent<CameraController>() != null) return;
        mainCam.gameObject.AddComponent<CameraController>();
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void LateUpdate()
    {
        // Player is always at world origin — keep camera directly above it.
        var pos = transform.position;
        pos.x = 0f;
        pos.z = 0f;
        pos.y = cameraHeight;
        transform.position = pos;

        HandlePinchZoom();

        int w = Screen.width;
        int h = Screen.height;
        if (w != lastWidth || h != lastHeight)
        {
            lastWidth  = w;
            lastHeight = h;
            if (currentOrthoSize < 0f)
                currentOrthoSize = CalculateDefaultSize(w, h);
        }

        if (currentOrthoSize > 0f)
            cam.orthographicSize = currentOrthoSize;
    }

    private void HandlePinchZoom()
    {
        var touches = Touch.activeTouches;

        if (touches.Count != 2)
        {
            isPinching = false;
            return;
        }

        float dist = Vector2.Distance(
            touches[0].screenPosition,
            touches[1].screenPosition);

        if (!isPinching)
        {
            prevPinchDist = dist;
            isPinching    = true;
            return;
        }

        float delta = prevPinchDist - dist;
        prevPinchDist = dist;

        float sensitivity = currentOrthoSize / Screen.height;
        currentOrthoSize  = Mathf.Clamp(
            currentOrthoSize + delta * sensitivity * 2f,
            minOrthoSize,
            maxOrthoSize);
    }

    private float CalculateDefaultSize(int screenW, int screenH)
    {
        float tileW = TileUtils.TileWidthMetres(referenceLat, zoomLevel);
        float tileH = TileUtils.TileHeightMetres(
            TileUtils.LatLonToTile(referenceLat, 10.0, zoomLevel).y, zoomLevel);

        float aspect = (float)screenW / screenH;
        float size   = aspect < 1f
            ? (tileW * tilesVisible / 2f) / aspect
            : tileH * tilesVisible / 2f;

        return Mathf.Clamp(size, minOrthoSize, maxOrthoSize);
    }
}

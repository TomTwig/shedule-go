using UnityEngine;

/// <summary>
/// Keeps the main camera centered on the player (world origin) and adjusts
/// orthographic size so the tile grid fills the screen in any orientation.
/// Auto-attaches to the Main Camera on startup.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [SerializeField] private int   zoomLevel    = 16;
    [SerializeField] private int   tilesVisible = 5;
    [SerializeField] private float referenceLat = 54.35f;
    [SerializeField] private float cameraHeight = 800f;

    private Camera cam;
    private int    lastWidth;
    private int    lastHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttach()
    {
        var mainCam = Camera.main;
        if (mainCam == null || mainCam.GetComponent<CameraController>() != null) return;
        mainCam.gameObject.AddComponent<CameraController>();
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        // Player is always at world origin — keep camera directly above it.
        var pos = transform.position;
        pos.x = 0f;
        pos.z = 0f;
        pos.y = cameraHeight;
        transform.position = pos;

        // Resize to fit tiles on screen.
        int w = Screen.width;
        int h = Screen.height;
        if (w == lastWidth && h == lastHeight) return;
        lastWidth  = w;
        lastHeight = h;
        AdjustSize(w, h);
    }

    private void AdjustSize(int screenW, int screenH)
    {
        float tileW = TileUtils.TileWidthMetres(referenceLat, zoomLevel);
        float tileH = TileUtils.TileHeightMetres(
            TileUtils.LatLonToTile(referenceLat, 10.0, zoomLevel).y, zoomLevel);

        float aspect    = (float)screenW / screenH;
        float orthoSize = aspect < 1f
            ? (tileW * tilesVisible / 2f) / aspect   // portrait: fit width
            : tileH * tilesVisible / 2f;              // landscape: fit height

        cam.orthographicSize = Mathf.Clamp(orthoSize, 300f, 2000f);
    }
}

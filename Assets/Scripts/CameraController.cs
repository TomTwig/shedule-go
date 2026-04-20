using UnityEngine;

/// <summary>
/// Adjusts the main camera's orthographic size so the 3×3 tile grid fits
/// the screen correctly in both portrait and landscape orientations.
/// Auto-injects itself onto the main camera at startup.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [SerializeField] private int   zoomLevel    = 16;
    [SerializeField] private int   tilesVisible = 3;   // tiles along shorter axis
    [SerializeField] private float referenceLat = 54.35f;

    private Camera cam;
    private int    lastWidth;
    private int    lastHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttach()
    {
        var mainCam = Camera.main;
        if (mainCam == null) return;
        if (mainCam.GetComponent<CameraController>() != null) return;
        mainCam.gameObject.AddComponent<CameraController>();
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
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

        float aspect = (float)screenW / screenH;

        float orthoSize;
        if (aspect < 1f)
        {
            // Portrait — ensure all 3 tile columns fit in the width
            float halfWidthNeeded = tileW * tilesVisible / 2f;
            orthoSize = halfWidthNeeded / aspect;
        }
        else
        {
            // Landscape — ensure all 3 tile rows fit in the height
            orthoSize = tileH * tilesVisible / 2f;
        }

        cam.orthographicSize = Mathf.Clamp(orthoSize, 300f, 1200f);
    }
}

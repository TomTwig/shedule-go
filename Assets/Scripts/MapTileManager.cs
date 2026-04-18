using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders a GPS-aligned grid of map tiles loaded from local Resources.
///
/// Tile images must be placed at:
///   Assets/Resources/Tiles/{zoom}/{x}/{y}.png
///
/// Download tiles for your area with MOBAC (Mobile Atlas Creator):
///   1. mobac.sourceforge.io → download and run
///   2. Select atlas type "OSM Tile ZIP"
///   3. Draw your area, pick zoom levels (16 recommended)
///   4. Export → unzip into Assets/Resources/Tiles/
///
/// The player is always at world origin. Tiles reposition themselves every time
/// the player moves into a new tile cell, so the map always stays aligned.
/// </summary>
public class MapTileManager : MonoBehaviour
{
    [Header("Map Settings")]
    [Tooltip("OSM zoom level. 16 = street level (~600 m per tile).")]
    [SerializeField] private int zoomLevel = 16;

    [Tooltip("Tiles to load in each direction from the player. 1 = 3×3 grid, 2 = 5×5 grid.")]
    [SerializeField] [Range(1, 3)] private int gridRadius = 1;

    [Header("Dependencies")]
    [SerializeField] private GameManager gameManager;

    // Key = (tileX, tileY), value = the GameObject displaying that tile.
    private readonly Dictionary<(int, int), GameObject> activeTiles  = new();
    private readonly Queue<GameObject>                  tilePool      = new();
    private readonly List<(int, int)>                   keysToRemove  = new();

    private (int x, int y) lastCenterTile = (-1, -1);

    // Shared base material — each tile gets its own instance via new Material(base).
    private Material baseMaterial;
    private Texture2D fallbackTexture;

    private void Start()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError("[MapTileManager] GameManager not found.");
            return;
        }

        baseMaterial    = new Material(Shader.Find("Unlit/Texture"));
        fallbackTexture = CreateFallbackTexture();
    }

    private void Update()
    {
        if (gameManager == null || !gameManager.IsLocationReady) return;

        (int x, int y) centerTile = TileUtils.LatLonToTile(
            gameManager.PlayerLatitude,
            gameManager.PlayerLongitude,
            zoomLevel);

        if (centerTile == lastCenterTile) return;

        lastCenterTile = centerTile;
        RefreshGrid(centerTile);
    }

    // -------------------------------------------------------------------------
    // Grid management
    // -------------------------------------------------------------------------

    private void RefreshGrid((int cx, int cy) center)
    {
        // Build the set of tiles we want this frame.
        var needed = new HashSet<(int, int)>();
        for (int dx = -gridRadius; dx <= gridRadius; dx++)
        for (int dy = -gridRadius; dy <= gridRadius; dy++)
            needed.Add((center.cx + dx, center.cy + dy));

        // Return tiles that fell outside the grid back to the pool.
        keysToRemove.Clear();
        foreach (var key in activeTiles.Keys)
        {
            if (!needed.Contains(key))
            {
                ReturnToPool(activeTiles[key]);
                keysToRemove.Add(key);
            }
        }
        foreach (var key in keysToRemove) activeTiles.Remove(key);

        // Create or reuse a tile object for every needed position.
        foreach (var tileKey in needed)
        {
            if (activeTiles.TryGetValue(tileKey, out var existing))
            {
                // Already active — just keep its world position current.
                PositionTile(existing, tileKey.Item1, tileKey.Item2);
                continue;
            }

            var go = GetFromPool();
            activeTiles[tileKey] = go;
            PositionTile(go, tileKey.Item1, tileKey.Item2);
            StartCoroutine(LoadTileTexture(go, tileKey.Item1, tileKey.Item2));
        }
    }

    // -------------------------------------------------------------------------
    // Tile positioning
    // -------------------------------------------------------------------------

    private void PositionTile(GameObject go, int tileX, int tileY)
    {
        var (centerLat, centerLon) = TileUtils.TileCenterLatLon(tileX, tileY, zoomLevel);

        Vector3 offset = GeoUtils.GpsToUnityOffset(
            gameManager.PlayerLatitude, gameManager.PlayerLongitude,
            centerLat, centerLon);

        float width  = TileUtils.TileWidthMetres(centerLat, zoomLevel);
        float height = TileUtils.TileHeightMetres(tileY,    zoomLevel);

        // Y = -0.01 so tiles sit just below POIs (which are at Y ≥ 0).
        go.transform.position   = new Vector3(offset.x, -0.01f, offset.z);
        // Rotate the Quad (default XY plane) to lie flat on XZ.
        go.transform.rotation   = Quaternion.Euler(-90f, 0f, 0f);
        // Scale width (local X) and depth (local Y, which maps to world Z after rotation).
        go.transform.localScale = new Vector3(width, height, 1f);
        go.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // Async texture loading from Resources
    // -------------------------------------------------------------------------

    private IEnumerator LoadTileTexture(GameObject go, int tileX, int tileY)
    {
        string resourcePath = $"Tiles/{zoomLevel}/{tileX}/{tileY}";
        var    request      = Resources.LoadAsync<Texture2D>(resourcePath);

        yield return request;

        // The tile may have been recycled while we were loading — abort if so.
        if (go == null || !activeTiles.ContainsValue(go)) yield break;

        var renderer = go.GetComponent<MeshRenderer>();
        var mat      = new Material(baseMaterial);

        if (request.asset is Texture2D tex)
        {
            mat.mainTexture = tex;
            // Tile images store Y=0 at the top (north). Unity UVs store V=0 at the
            // bottom, so we flip the texture vertically to keep north pointing up.
            mat.mainTextureScale  = new Vector2( 1f, -1f);
            mat.mainTextureOffset = new Vector2( 0f,  1f);
        }
        else
        {
            Debug.LogWarning($"[MapTileManager] Missing tile: Resources/{resourcePath}.png — using fallback.");
            mat.mainTexture = fallbackTexture;
        }

        renderer.material = mat;
    }

    // -------------------------------------------------------------------------
    // Object pool
    // -------------------------------------------------------------------------

    private GameObject GetFromPool()
    {
        if (tilePool.Count > 0)
        {
            var pooled = tilePool.Dequeue();
            pooled.SetActive(true);
            return pooled;
        }
        return CreateTileObject();
    }

    private void ReturnToPool(GameObject go)
    {
        go.SetActive(false);
        tilePool.Enqueue(go);
    }

    private GameObject CreateTileObject()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "MapTile";
        go.transform.SetParent(transform);

        // Tiles are purely visual — no physics needed.
        Destroy(go.GetComponent<MeshCollider>());

        var renderer = go.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows    = false;

        var mat = new Material(baseMaterial) { mainTexture = fallbackTexture };
        renderer.material = mat;

        return go;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    // 2×2 light-grey texture shown while a real tile is loading or missing.
    private static Texture2D CreateFallbackTexture()
    {
        var tex   = new Texture2D(2, 2);
        var grey  = new Color(0.78f, 0.78f, 0.78f);
        tex.SetPixels(new[] { grey, grey, grey, grey });
        tex.Apply();
        return tex;
    }

    private void OnDestroy()
    {
        if (baseMaterial    != null) Destroy(baseMaterial);
        if (fallbackTexture != null) Destroy(fallbackTexture);
    }
}

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
    private readonly Dictionary<(int, int), GameObject> activeTiles   = new();
    private readonly Queue<GameObject>                  tilePool       = new();
    private readonly List<(int, int)>                   keysToRemove   = new();
    private readonly HashSet<(int, int)>                loggedMissing  = new(); // suppress repeat warnings

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
            Debug.LogError("[MapTileManager] GameManager not found — tiles will not load.");
            return;
        }

        // Clone the material from a temporary Quad — this guarantees the material
        // is correctly configured for whatever render pipeline is active in this project.
        var tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        baseMaterial = new Material(tempQuad.GetComponent<MeshRenderer>().sharedMaterial);
        baseMaterial.name = "MapTileMaterial";
        Destroy(tempQuad);

        // Ensure fully opaque rendering.
        if (baseMaterial.HasProperty("_Surface"))   baseMaterial.SetFloat("_Surface", 0f);
        if (baseMaterial.HasProperty("_BaseColor"))  baseMaterial.SetColor("_BaseColor", Color.white);
        if (baseMaterial.HasProperty("_Color"))      baseMaterial.SetColor("_Color",     Color.white);

        Debug.Log($"[MapTileManager] Base material shader: {baseMaterial.shader.name}");
        fallbackTexture = CreateFallbackTexture();

        // Spawn a white cube at world origin so you can verify the camera sees (0,0,0).
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "DEBUG_Origin";
        marker.transform.position   = Vector3.zero;
        marker.transform.localScale = Vector3.one * 50f;
        Debug.Log("[MapTileManager] Placed DEBUG_Origin cube at (0,0,0) — should be visible if camera is aimed correctly.");
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
        Debug.Log($"[MapTileManager] Refreshing grid — centre tile: {centerTile.x},{centerTile.y} | player: {gameManager.PlayerLatitude:F5},{gameManager.PlayerLongitude:F5}");
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
            ApplyTileTexture(go, tileKey.Item1, tileKey.Item2);
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

        var pos = new Vector3(offset.x, -0.01f, offset.z);
        go.transform.position   = pos;
        go.transform.rotation   = Quaternion.Euler(-90f, 0f, 0f);
        go.transform.localScale = new Vector3(width, height, 1f);
        go.SetActive(true);

        Debug.Log($"[MapTileManager] Tile {tileX}/{tileY} → world pos {pos} size {width:F0}×{height:F0} m");
    }

    // -------------------------------------------------------------------------
    // Async texture loading from Resources
    // -------------------------------------------------------------------------

    // Synchronous — no coroutine, no yield, no timing issues.
    private void ApplyTileTexture(GameObject go, int tileX, int tileY)
    {
        string resourcePath = $"Tiles/{zoomLevel}/{tileX}/{tileY}";
        var    tex          = Resources.Load<Texture2D>(resourcePath);

        var renderer = go.GetComponent<MeshRenderer>();
        var mat      = new Material(baseMaterial);

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     Color.white);

        if (tex != null)
        {
            SetTexture(mat, tex, flipV: true);
            Debug.Log($"[MapTileManager] Tile {tileX}/{tileY} textured ({tex.width}×{tex.height})");
        }
        else
        {
            SetTexture(mat, fallbackTexture, flipV: false);
            if (loggedMissing.Add((tileX, tileY)))
                Debug.LogWarning($"[MapTileManager] Missing: Resources/{resourcePath}.png");
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

        var mat = new Material(baseMaterial);
        SetTexture(mat, fallbackTexture, flipV: false);
        renderer.material = mat;

        return go;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    // Sets a texture on a material, choosing the correct property name for the
    // active render pipeline (URP uses _BaseMap; Built-in uses _MainTex).
    private static void SetTexture(Material mat, Texture2D tex, bool flipV)
    {
        Vector2 scale  = flipV ? new Vector2(1f, -1f) : Vector2.one;
        Vector2 offset = flipV ? new Vector2(0f,  1f) : Vector2.zero;

        if (mat.HasProperty("_BaseMap"))
        {
            // URP / Universal Render Pipeline
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale ("_BaseMap", scale);
            mat.SetTextureOffset("_BaseMap", offset);
        }
        else
        {
            // Built-in pipeline
            mat.mainTexture       = tex;
            mat.mainTextureScale  = scale;
            mat.mainTextureOffset = offset;
        }
    }

    // Returns the best available unlit texture shader (URP or Built-in).
    private static Shader FindUnlitShader()
    {
        // URP projects use a different shader path than the Built-in pipeline.
        string[] candidates =
        {
            "Universal Render Pipeline/Unlit",  // URP
            "Unlit/Texture",                    // Built-in
            "Sprites/Default",                  // fallback that works everywhere
        };

        foreach (var name in candidates)
        {
            var s = Shader.Find(name);
            if (s != null) return s;
        }

        Debug.LogError("[MapTileManager] No unlit shader found — tiles will be invisible.");
        return null;
    }

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

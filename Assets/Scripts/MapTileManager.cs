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

    [Tooltip("Tiles to load in each direction from the player. 1=3×3, 2=5×5, 5=11×11 (full set).")]
    [SerializeField] [Range(1, 5)] private int gridRadius = 5;

    [Header("Dependencies")]
    [SerializeField] private GameManager gameManager;

    private readonly Dictionary<(int, int), GameObject> activeTiles  = new();
    private readonly Queue<GameObject>                  tilePool      = new();
    private readonly List<(int, int)>                   keysToRemove  = new();
    private readonly HashSet<(int, int)>                loggedMissing = new();

    private (int x, int y) lastCenterTile = (-1, -1);

    // Shared flat mesh — all tile GameObjects use the same mesh geometry.
    // Normals point up (+Y) so the camera looking down always sees the front face.
    private static Mesh s_tileMesh;

    // Material prototype cloned from Unity's default primitive material.
    // Each tile gets its own instance (renderer.material) so textures don't bleed.
    private Material materialTemplate;
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

        fallbackTexture = CreateFallbackTexture();

        var shader = Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Standard");
        materialTemplate = new Material(shader);
    }

    private void Update()
    {
        if (gameManager == null || !gameManager.IsLocationReady) return;

        (int x, int y) centerTile = TileUtils.LatLonToTile(
            gameManager.PlayerLatitude,
            gameManager.PlayerLongitude,
            zoomLevel);

        if (centerTile != lastCenterTile)
        {
            lastCenterTile = centerTile;
            RefreshGrid(centerTile);
        }

        // Reposition every frame using smoothed coordinates for fluid movement.
        foreach (var kv in activeTiles)
            PositionTile(kv.Value, kv.Key.Item1, kv.Key.Item2);
    }

    // -------------------------------------------------------------------------
    // Grid management
    // -------------------------------------------------------------------------

    private void RefreshGrid((int cx, int cy) center)
    {
        var needed = new HashSet<(int, int)>();
        for (int dx = -gridRadius; dx <= gridRadius; dx++)
        for (int dy = -gridRadius; dy <= gridRadius; dy++)
            needed.Add((center.cx + dx, center.cy + dy));

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

        foreach (var tileKey in needed)
        {
            if (activeTiles.TryGetValue(tileKey, out var existing))
            {
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
            gameManager.SmoothedLatitude, gameManager.SmoothedLongitude,
            centerLat, centerLon);

        float width  = TileUtils.TileWidthMetres(centerLat, zoomLevel);
        float height = TileUtils.TileHeightMetres(tileY, zoomLevel);

        // Flat mesh sits in the XZ plane at Y=0.
        go.transform.position   = new Vector3(offset.x, 0f, offset.z);
        go.transform.rotation   = Quaternion.identity;
        go.transform.localScale = new Vector3(width, 1f, height);
        go.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // Texture loading from Resources
    // -------------------------------------------------------------------------

    private void ApplyTileTexture(GameObject go, int tileX, int tileY)
    {
        string resourcePath = $"Tiles/{zoomLevel}/{tileX}/{tileY}";
        var    tex          = Resources.Load<Texture2D>(resourcePath);

        // renderer.material auto-instances so each tile has its own material.
        var mat = go.GetComponent<MeshRenderer>().material;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     Color.white);

        if (tex != null)
        {
            // Unity imports PNGs with Y=0 at the bottom (OpenGL convention).
            // OSM image row 0 (top/North) lands at UV.v=1, not v=0.
            // Mesh UV has v=0 at North → must flip V so North samples v=1 in texture.
            SetTexture(mat, tex, flipV: true);
        }
        else
        {
            mat.color = Color.grey;
            if (loggedMissing.Add((tileX, tileY)))
                Debug.LogWarning($"[MapTileManager] Missing: Resources/{resourcePath}.png");
        }
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
        var go = new GameObject("MapTile");
        go.transform.SetParent(transform);

        go.AddComponent<MeshFilter>().sharedMesh = GetTileMesh();

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materialTemplate;

        return go;
    }

    // -------------------------------------------------------------------------
    // Flat tile mesh — explicit UV mapping, no Quad/Cube UV ambiguity
    // -------------------------------------------------------------------------

    // Flat mesh in the XZ plane. Scale on the GameObject sets real-world metres.
    //
    //   World +X = East   World +Z = North   (camera looks down from +Y)
    //
    //   NW(-0.5, 0, +0.5) ------ NE(+0.5, 0, +0.5)
    //        UV(0,0)                  UV(1,0)
    //            |                        |
    //   SW(-0.5, 0, -0.5) ------ SE(+0.5, 0, -0.5)
    //        UV(0,1)                  UV(1,1)
    //
    // OSM tile UV convention: (0,0)=NW corner, U increases East, V increases South.
    // This mesh matches that exactly — no flip or rotation needed in the shader.
    private static Mesh GetTileMesh()
    {
        if (s_tileMesh != null) return s_tileMesh;

        s_tileMesh = new Mesh { name = "FlatTile" };

        s_tileMesh.vertices = new Vector3[]
        {
            new(-0.5f, 0f,  0.5f),  // 0: NW
            new( 0.5f, 0f,  0.5f),  // 1: NE
            new(-0.5f, 0f, -0.5f),  // 2: SW
            new( 0.5f, 0f, -0.5f),  // 3: SE
        };

        s_tileMesh.uv = new Vector2[]
        {
            new(0f, 0f),  // NW → OSM top-left
            new(1f, 0f),  // NE → OSM top-right
            new(0f, 1f),  // SW → OSM bottom-left
            new(1f, 1f),  // SE → OSM bottom-right
        };

        // CCW winding when viewed from above (+Y) → normals point up.
        s_tileMesh.triangles = new int[] { 0, 1, 2, 1, 3, 2 };
        s_tileMesh.RecalculateNormals();
        s_tileMesh.RecalculateBounds();
        return s_tileMesh;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    // V-flip corrects Unity's OpenGL texture convention (Y=0 at bottom) vs
    // OSM PNG convention (row 0 = North = should map to UV.v=1, not v=0).
    private static void SetTexture(Material mat, Texture2D tex, bool flipV)
    {
        Vector2 scale  = flipV ? new Vector2(1f, -1f) : Vector2.one;
        Vector2 offset = flipV ? new Vector2(0f,  1f) : Vector2.zero;

        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale ("_BaseMap", scale);
            mat.SetTextureOffset("_BaseMap", offset);
        }
        else
        {
            mat.mainTexture       = tex;
            mat.mainTextureScale  = scale;
            mat.mainTextureOffset = offset;
        }
    }

    private static Texture2D CreateFallbackTexture()
    {
        var tex  = new Texture2D(2, 2);
        var grey = new Color(0.78f, 0.78f, 0.78f);
        tex.SetPixels(new[] { grey, grey, grey, grey });
        tex.Apply();
        return tex;
    }

    private void OnDestroy()
    {
        if (materialTemplate != null) Destroy(materialTemplate);
        if (fallbackTexture  != null) Destroy(fallbackTexture);
        if (s_tileMesh       != null) { Destroy(s_tileMesh); s_tileMesh = null; }
    }
}

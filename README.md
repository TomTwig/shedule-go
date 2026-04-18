# Shedule GO — Location-Based Game (Phase 1 MVP)

A minimal Unity mobile project demonstrating GPS-based gameplay similar to Pokémon GO.
No AR, no external SDKs — only Unity's built-in `LocationService` and **locally stored map tiles**.

---

## Architecture

```
LocationManager     ← owns Input.location lifecycle, exposes lat/lon + IsReady
        ↓
  GameManager       ← singleton hub, caches player position + IsLocationReady
      ↓    ↓    ↘
POIController  UIManager  MapTileManager
(world pos)  (HUD text)  (tile grid)
      ↑              ↑
   GeoUtils       TileUtils
(GPS ↔ Unity)  (tile math)
```

The **player is always at world origin (0,0,0)**. Everything else — tiles, POIs — repositions
itself in world space to reflect the GPS delta from the player.

---

## Map Tiles — Local / Offline (no API key needed)

Tiles follow the standard OSM **slippy-map XYZ** convention:

```
Assets/Resources/Tiles/{zoom}/{x}/{y}.png
                   ↑      ↑   ↑
               zoom 16   col  row
```

Each tile is a **256 × 256 PNG** that Unity loads at runtime with `Resources.LoadAsync`.
At zoom 16 each tile covers roughly **600 × 600 metres** — a good balance between detail
and file count.

### Downloading tiles with MOBAC (free, open-source)

1. Download **Mobile Atlas Creator** from [mobac.sourceforge.io](https://mobac.sourceforge.io)
2. Launch MOBAC.
3. In the **Map Source** drop-down, pick *OpenStreetMap MapQuest* (or any OSM source).
4. Draw a bounding box over the area you need.
5. Set zoom levels: tick **16** (and optionally 15 for a wider fallback view).
6. **Atlas → Add selection**.
7. In **Atlas Content** set:
   - Atlas format: `OSM Tile Zip`
   - Atlas name: `tiles`
8. Click **Create Atlas** → MOBAC downloads a ZIP.
9. Unzip the ZIP. You will get a folder structure like:
   ```
   16/
     34567/
       22345.png
       22346.png
   ```
10. Copy the `16/` folder into `Assets/Resources/Tiles/` in your Unity project:
    ```
    Assets/Resources/Tiles/16/34567/22345.png
                                    22346.png
                        ...
    ```

> **Tile count estimate:** A 2 km × 2 km area at zoom 16 ≈ 16 tiles ≈ ~800 KB.
> A city district (5 × 5 km) ≈ ~100 tiles ≈ ~5 MB.

---

## Unity Scene Setup

### 1. Create the Scene

1. Open Unity (2022 LTS or newer recommended).
2. **File → New Scene** → Basic (Built-in).
3. Save as `Assets/Scenes/Main.unity`.
4. **Delete the default Plane** — `MapTileManager` creates tile quads automatically.

### 2. Hierarchy

```
Main
├── Managers                          (empty GameObject)
│   ├── LocationManager   [LocationManager.cs]
│   ├── GameManager       [GameManager.cs]
│   ├── UIManager         [UIManager.cs]
│   └── MapTileManager    [MapTileManager.cs]
├── POI                               (3D Sphere or Cube)
│   └── [POIController.cs]
└── Canvas                            (Screen Space — Overlay)
    ├── StatusText    (TMP_Text)
    ├── CoordText     (TMP_Text)
    └── PoiDistText   (TMP_Text)
```

### 3. Component Wiring

| Script | Inspector field | Drag from Hierarchy |
|---|---|---|
| `GameManager` | `locationManager` | `Managers/LocationManager` |
| `MapTileManager` | `gameManager` | `Managers/GameManager` |
| `UIManager` | `gameManager` | `Managers/GameManager` |
| `UIManager` | `coordinatesText` | `Canvas/CoordText` |
| `UIManager` | `statusText` | `Canvas/StatusText` |
| `UIManager` | `poiDistanceText` | `Canvas/PoiDistText` |
| `UIManager` | `poiController` | `POI` |

> `POIController` auto-finds `GameManager.Instance` — no manual wiring needed.

### 4. MapTileManager Inspector Settings

| Field | Default | Notes |
|---|---|---|
| Zoom Level | `16` | Match the zoom you downloaded in MOBAC |
| Grid Radius | `1` | `1` = 3×3 tiles, `2` = 5×5 tiles |

### 5. Camera

```
Position:  (0, 300, 0)
Rotation:  (90, 0, 0)    ← straight down (top-down view)
Projection: Orthographic
Size:       600           ← shows ≈ 1 tile width
```

Or use a perspective angle:
```
Position:  (0, 500, -200)
Rotation:  (65, 0, 0)
Projection: Perspective
```

---

## Script Reference

| File | Purpose |
|---|---|
| `GeoUtils.cs` | GPS↔Unity math (flat-earth projection + Haversine distance) |
| `TileUtils.cs` | Slippy-map tile math (LatLon↔TileXY, tile size in metres) |
| `LocationManager.cs` | Starts/stops `Input.location`, handles permissions |
| `GameManager.cs` | Singleton hub — caches player GPS, exposes `IsLocationReady` |
| `MapTileManager.cs` | Pooled tile-quad grid, loads PNGs from `Resources/Tiles/` |
| `POIController.cs` | Positions a POI in world space relative to the player's GPS |
| `UIManager.cs` | HUD — shows lat/lon and distance to POI via TextMeshPro |
| `Editor/iOSPostBuild.cs` | Auto-injects NSLocation keys into Xcode Info.plist |

---

## Build Settings — Android

1. **File → Build Settings → Android** → Switch Platform.
2. **Player Settings → Other Settings:**
   - Minimum API Level: **26** (Android 8.0)
   - Target API Level: **34** (Android 14)
   - Internet Access: `Auto`
3. `Assets/Plugins/Android/AndroidManifest.xml` is merged automatically.

## Build Settings — iOS

1. **File → Build Settings → iOS** → Switch Platform.
2. **Player Settings → Other Settings:**
   - Bundle Identifier: `com.yourcompany.shedule`
   - Target minimum iOS Version: `15.0`
3. `Editor/iOSPostBuild.cs` injects `NSLocationWhenInUseUsageDescription` on every build.
4. Open Xcode → select a real device → **Product → Run**.

> GPS does not work in iOS Simulator — test on hardware.

---

## Testing in the Editor

GPS doesn't run in the Editor. Temporarily hardcode coordinates in `LocationManager.cs`:

```csharp
// In LocationManager.Update() — remove before shipping:
#if UNITY_EDITOR
CurrentLatitude  = 48.137_154;  // Munich Marienplatz
CurrentLongitude = 11.576_124;
IsReady = true;
return;
#endif
```

Make sure the coordinates are inside the tile area you downloaded — otherwise tiles
will not be found and you'll see the grey fallback grid.

---

## Scaling to Phase 2

- **Multiple POIs:** Replace the hardcoded coordinate with a JSON file or REST endpoint.
- **Compass:** Use `Input.compass` to rotate the camera / player arrow.
- **Zoom out:** Add zoom level 15 tiles in MOBAC and switch `MapTileManager.zoomLevel`
  based on player speed.
- **Streaming:** For larger areas, load tiles from a self-hosted tile server (nginx +
  MBTiles) instead of bundled Resources — swap `Resources.LoadAsync` for `UnityWebRequest`.
- **AR:** Add AR Foundation for a camera-overlay encounter view.

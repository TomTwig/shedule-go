# Shedule GO — Location-Based Game (Phase 1 MVP)

A minimal Unity mobile project demonstrating GPS-based gameplay similar to Pokémon GO.
No AR, no external SDKs — only Unity's built-in `LocationService`.

---

## Architecture

```
LocationManager   ← owns Input.location lifecycle, exposes lat/lon
      ↓
 GameManager      ← singleton hub, caches player position
      ↓        ↘
POIController    UIManager
(world pos)    (HUD text)
      ↑
   GeoUtils      ← pure static math (GPS ↔ Unity coords, Haversine distance)
```

The **player is always at world origin (0,0,0)**. The POI moves in world space to
reflect the GPS delta between the player and the fixed POI coordinate.

---

## Unity Scene Setup

### 1. Create the Scene

1. Open Unity (2022 LTS or newer recommended).
2. **File → New Scene** → Basic (Built-in).
3. Save as `Assets/Scenes/Main.unity`.

### 2. Hierarchy

```
Main
├── Managers          (empty GameObject)
│   ├── LocationManager   [LocationManager.cs]
│   ├── GameManager       [GameManager.cs]
│   └── UIManager         [UIManager.cs]
├── Environment
│   └── Ground        (3D Plane, scale 10,1,10, position 0,0,0)
├── POI               (3D Sphere or Cube) [POIController.cs]
└── Canvas            (Screen Space — Overlay)
    ├── StatusText    (TMP_Text, top-left)
    ├── CoordText     (TMP_Text, below status)
    └── PoiDistText   (TMP_Text, below coords)
```

### 3. Component Wiring

| Script | Inspector field | Drag from Hierarchy |
|---|---|---|
| `GameManager` | `locationManager` | `Managers/LocationManager` |
| `UIManager` | `gameManager` | `Managers/GameManager` |
| `UIManager` | `coordinatesText` | `Canvas/CoordText` |
| `UIManager` | `statusText` | `Canvas/StatusText` |
| `UIManager` | `poiDistanceText` | `Canvas/PoiDistText` |
| `UIManager` | `poiController` | `POI` |

> `POIController` auto-finds `GameManager.Instance`; no manual wiring needed.

### 4. Camera

Set the Main Camera to:
- **Position:** `(0, 10, -5)`
- **Rotation:** `(45, 0, 0)`
- **Projection:** Perspective

This gives a top-angled view of the ground plane with the POI visible.

---

## Script Reference

| File | Purpose |
|---|---|
| `GeoUtils.cs` | GPS↔Unity math (flat-earth approximation + Haversine) |
| `LocationManager.cs` | Starts/stops `Input.location`, exposes lat/lon |
| `GameManager.cs` | Singleton hub, caches player position each frame |
| `POIController.cs` | Moves POI object based on GPS delta to player |
| `UIManager.cs` | Renders coordinates + POI distance via TextMeshPro |
| `Editor/iOSPostBuild.cs` | Auto-injects NSLocation keys into Xcode Info.plist |

---

## Build Settings — Android

1. **File → Build Settings → Android**.
2. Switch Platform.
3. **Player Settings → Other Settings:**
   - Minimum API Level: **26** (Android 8.0)
   - Target API Level: **34** (Android 14)
   - Internet Access: `Auto`
4. The `Assets/Plugins/Android/AndroidManifest.xml` is merged automatically —
   no extra steps needed.

## Build Settings — iOS

1. **File → Build Settings → iOS**.
2. Switch Platform.
3. **Player Settings → Other Settings:**
   - Bundle Identifier: `com.yourcompany.shedule`
   - Target minimum iOS Version: `15.0`
4. The `Editor/iOSPostBuild.cs` script injects `NSLocationWhenInUseUsageDescription`
   into the generated Xcode project automatically on every build.
5. Open Xcode → select a real device → **Product → Run**.

> **Note:** GPS does not work in iOS Simulator. Test on physical hardware.

---

## Testing in the Editor

Unity's LocationService always returns the coordinates set in
**Edit → Project Settings → Input Manager** (or via a Fake GPS asset).

For editor testing you can temporarily hard-code a lat/lon in `LocationManager`:

```csharp
// Inside LocationManager.Update(), for editor testing only:
#if UNITY_EDITOR
CurrentLatitude  = 37.422_000; // near the POI
CurrentLongitude = -122.084_500;
return;
#endif
```

---

## Scaling to Phase 2

- Replace the hardcoded POI with a JSON/REST-loaded POI list.
- Add a `PlayerMarker` that shows compass heading using `Input.compass`.
- Introduce a mini-map using a secondary orthographic camera.
- Add AR Foundation for camera-overlay mode.

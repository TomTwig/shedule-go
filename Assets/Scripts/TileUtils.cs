using System;

/// <summary>
/// Pure static math for the OSM slippy-map tile system.
/// Tile coordinates follow the standard XYZ/ZXY web-mercator convention:
///   origin (0,0) = top-left (NW) of the world at every zoom level.
///   X increases eastward, Y increases southward.
/// Reference: https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames
/// </summary>
public static class TileUtils
{
    // Earth equatorial circumference in metres.
    private const double EarthCircumference = 40_075_017.0;

    /// <summary>
    /// Converts a GPS coordinate to the tile (x, y) that contains it at the given zoom level.
    /// </summary>
    public static (int x, int y) LatLonToTile(double lat, double lon, int zoom)
    {
        int x = (int)Math.Floor((lon + 180.0) / 360.0 * (1 << zoom));

        double latRad = lat * Math.PI / 180.0;
        int y = (int)Math.Floor(
            (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI)
            / 2.0 * (1 << zoom));

        return (x, y);
    }

    /// <summary>
    /// Returns the GPS coordinate (lat, lon) of the top-left (NW) corner of a tile.
    /// </summary>
    public static (double lat, double lon) TileTopLeftLatLon(int x, int y, int zoom)
    {
        double lon = x / (double)(1 << zoom) * 360.0 - 180.0;
        double n   = Math.PI - 2.0 * Math.PI * y / (1 << zoom);
        double lat = 180.0 / Math.PI * Math.Atan(Math.Sinh(n));
        return (lat, lon);
    }

    /// <summary>
    /// Returns the GPS coordinate of the centre of a tile.
    /// </summary>
    public static (double lat, double lon) TileCenterLatLon(int x, int y, int zoom)
    {
        var (topLat,    leftLon)  = TileTopLeftLatLon(x,     y,     zoom);
        var (bottomLat, rightLon) = TileTopLeftLatLon(x + 1, y + 1, zoom);
        return ((topLat + bottomLat) / 2.0, (leftLon + rightLon) / 2.0);
    }

    /// <summary>
    /// East-west width of a tile in metres at a given latitude and zoom level.
    /// </summary>
    public static float TileWidthMetres(double lat, int zoom)
    {
        double latRad = lat * Math.PI / 180.0;
        return (float)(EarthCircumference * Math.Cos(latRad) / (1 << zoom));
    }

    /// <summary>
    /// North-south height of a tile in metres (derived from lat difference of its edges).
    /// </summary>
    public static float TileHeightMetres(int tileY, int zoom)
    {
        var (topLat,    _) = TileTopLeftLatLon(0, tileY,     zoom);
        var (bottomLat, _) = TileTopLeftLatLon(0, tileY + 1, zoom);
        return (float)(Math.Abs(topLat - bottomLat) * 111_320.0);
    }
}

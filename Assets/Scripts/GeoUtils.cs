using UnityEngine;

/// <summary>
/// Static helper: GPS ↔ Unity world-space conversions and distance math.
/// Uses a flat-earth (equirectangular) approximation — accurate enough within ~10 km.
/// </summary>
public static class GeoUtils
{
    // Metres per degree of latitude (constant everywhere).
    private const double MetresPerLatDegree = 111_320.0;

    /// <summary>
    /// Converts a GPS delta (relativeLat, relativeLon) into Unity XZ metres
    /// with the player always at the world origin.
    /// </summary>
    /// <param name="playerLat">Player's current latitude in degrees.</param>
    /// <param name="playerLon">Player's current longitude in degrees.</param>
    /// <param name="targetLat">Target's latitude in degrees.</param>
    /// <param name="targetLon">Target's longitude in degrees.</param>
    /// <returns>
    /// A Vector3 where X = east/west offset and Z = north/south offset (Y = 0).
    /// </returns>
    public static Vector3 GpsToUnityOffset(
        double playerLat, double playerLon,
        double targetLat, double targetLon)
    {
        double deltaLat = targetLat - playerLat;
        double deltaLon = targetLon - playerLon;

        // Longitude degrees shrink toward the poles.
        double metresPerLonDegree = MetresPerLatDegree * System.Math.Cos(playerLat * System.Math.PI / 180.0);

        float x = (float)(deltaLon * metresPerLonDegree);
        float z = (float)(deltaLat * MetresPerLatDegree);

        return new Vector3(x, 0f, z);
    }

    /// <summary>
    /// Haversine distance in metres between two GPS coordinates.
    /// </summary>
    public static float DistanceMetres(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6_371_000.0; // Earth radius in metres

        double dLat = (lat2 - lat1) * System.Math.PI / 180.0;
        double dLon = (lon2 - lon1) * System.Math.PI / 180.0;

        double a = System.Math.Sin(dLat / 2) * System.Math.Sin(dLat / 2)
                 + System.Math.Cos(lat1 * System.Math.PI / 180.0)
                 * System.Math.Cos(lat2 * System.Math.PI / 180.0)
                 * System.Math.Sin(dLon / 2) * System.Math.Sin(dLon / 2);

        double c = 2.0 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1.0 - a));

        return (float)(R * c);
    }
}

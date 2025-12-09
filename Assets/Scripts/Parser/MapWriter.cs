using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class MapWriter : MonoBehaviour
{
    float centreX = 246006.1640f;
    float centreY = 2549078.799f;



    public Transform mapReader;
    public string fileName;

    public void WriteFromMapObject()
    {
        // Define CRS and resolution
        string crsName = "urn:ogc:def:crs:OGC:1.3:CRS84";
        float xyResolution = 1e-06f;

        if (mapReader != null)
        {
            List<string> features = new List<string>();

            Debug.Log("Indeed, map reader is not null");
            for (int buildId = 0; buildId < mapReader.childCount; buildId++)
            {
                Mesh mesh = mapReader.GetChild(buildId).GetChild(0).GetComponent<MeshFilter>().sharedMesh;
                float buildingHeight = mesh.vertices[0].y;

                for (int i = 0; i < mesh.vertices.Length; i++)
                {
                    Vector3 firstPoint = mesh.vertices[i];
                    Vector3 secondPoint = mesh.vertices[(i + 1) % mesh.vertices.Length];

                    (float lat_1, float lon_1) = (firstPoint.x + centreX, firstPoint.z + centreY);
                    (float lat_2, float lon_2) = (secondPoint.x + centreX, secondPoint.z + centreY);

                    // Write GeoJSON for this point
                    features.Add(CreateLineStringFeature(lat_1, lon_1, lat_2, lon_2, buildingHeight));
                }
            }

            string geoJson = ConstructGeoJson(crsName, xyResolution, features);

            // Optionally, write the output to a file
            string filePath = Path.Combine(Application.dataPath, "Resources", fileName + ".geojson");
            File.WriteAllText(filePath, geoJson);

            Debug.Log($"GeoJSON written to {filePath}");
        }
        else Debug.Log("Map reader is null bro");
    }
    // Example usage

    private string CreateLineStringFeature(float lat1, float lon1, float lat2, float lon2, float height)
    {
        // Create the coordinates array for the LineString
        string coordinates = $"[[{lon1}, {lat1}], [{lon2}, {lat2}]]";

        // Create the feature string
        string feature = $@"
        {{
            ""type"": ""Feature"",
            ""geometry"": {{
                ""type"": ""LineString"",
                ""coordinates"": {coordinates}
            }},
            ""properties"": {{
                ""name"": ""Wall"",
                ""height"": {height},
                ""color"": Red
            }}
        }}";

        return feature;
    }

    private string ConstructGeoJson(string crsName, float xyResolution, List<string> features)
    {
        // Join all features into a single array
        string featuresArray = string.Join(",", features);

        // Build the GeoJSON string
        string geoJson = $@"
        {{
            ""type"": ""FeatureCollection"",
            ""crs"": {{
                ""type"": ""name"",
                ""properties"": {{
                    ""name"": ""{crsName}""
                }}
            }},
            ""xy_coordinate_resolution"": {xyResolution},
            ""features"": [
                {featuresArray}
            ]
        }}";

        return geoJson;
    }
}
    ////Takes in Coordinates X and Y
    //public static void CoordinateConverter(double E, double N)
    //{
    //    // Constants for WGS84 and UTM Zone 43N
    //    double a = 6378137.0; // Semi-major axis
    //    double f = 1 / 298.257223563; // Flattening
    //    double e2 = 2 * f - f * f; // Eccentricity squared
    //    double k0 = 0.9996; // Scale factor
    //    double lambda0 = DegreesToRadians(75); // Central meridian (75° for zone 43N)

    //    // Example UTM coordinates (Easting and Northing)
    //    double E = 246006.164014719; // Easting in meters
    //    double N = 2549078.7998468; // Northing in meters

    //    // Step 1: Remove false easting and northing
    //    double x = E - 500000; // Remove false easting
    //    double y = N; // No false northing for the northern hemisphere

    //    // Step 2: Calculate the meridian arc
    //    double m = y / k0;
    //    double mu = m / (a * (1 - e2 / 4 - 3 * Math.Pow(e2, 2) / 64 - 5 * Math.Pow(e2, 3) / 256));

    //    // Step 3: Calculate the footprint latitude (phi_f)
    //    double e1 = (1 - Math.Sqrt(1 - e2)) / (1 + Math.Sqrt(1 - e2));
    //    double phiF = mu +
    //        (3 * e1 / 2 - 27 * Math.Pow(e1, 3) / 32) * Math.Sin(2 * mu) +
    //        (21 * Math.Pow(e1, 2) / 16 - 55 * Math.Pow(e1, 4) / 32) * Math.Sin(4 * mu) +
    //        (151 * Math.Pow(e1, 3) / 96) * Math.Sin(6 * mu) +
    //        (1097 * Math.Pow(e1, 4) / 512) * Math.Sin(8 * mu);

    //    // Step 4: Calculate Latitude and Longitude
    //    double n = a / Math.Sqrt(1 - e2 * Math.Pow(Math.Sin(phiF), 2));
    //    double t = Math.Pow(Math.Tan(phiF), 2);
    //    double c = e2 / (1 - e2) * Math.Pow(Math.Cos(phiF), 2);
    //    double r = a * (1 - e2) / Math.Pow(1 - e2 * Math.Pow(Math.Sin(phiF), 2), 1.5);
    //    double d = x / (n * k0);

    //    // Latitude
    //    double latitude = phiF - (n * Math.Tan(phiF) / r) *
    //                      (Math.Pow(d, 2) / 2 -
    //                       (5 + 3 * t + 10 * c - 4 * Math.Pow(c, 2) - 9 * e2) * Math.Pow(d, 4) / 24 +
    //                       (61 + 90 * t + 298 * c + 45 * Math.Pow(t, 2) - 252 * e2 - 3 * Math.Pow(c, 2)) * Math.Pow(d, 6) / 720);

    //    // Longitude
    //    double longitude = lambda0 + (d -
    //                     (1 + 2 * t + c) * Math.Pow(d, 3) / 6 +
    //                     (5 - 2 * c + 28 * t - 3 * Math.Pow(c, 2) + 8 * e2 + 24 * Math.Pow(t, 2)) * Math.Pow(d, 5) / 120) /
    //                     Math.Cos(phiF);

    //    // Convert latitude and longitude to degrees
    //    latitude = RadiansToDegrees(latitude);
    //    longitude = RadiansToDegrees(longitude);

    //    return (latitude, longitude);

    //    // Output results
    //    // debug.Log($"Latitude: {latitude}");
    //     //debug.Log($"Longitude: {longitude}");
    //}

    //static double DegreesToRadians(double degrees)
    //{
    //    return degrees * Math.PI / 180.0;
    //}

    //static double RadiansToDegrees(double radians)
    //{
    //    return radians * 180.0 / Math.PI;
    //}
    //}



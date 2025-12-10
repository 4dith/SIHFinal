using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class MapWriter : MonoBehaviour
{
    public static float centreX = 246006.1640f;
    public static float centreY = 2549078.799f;

    // Define CRS and resolution
    public static string crsName = "urn:ogc:def:crs:EPSG::32643";
    public static float xyResolution = 1e-06f;

    // GeoJSON root structure
    [System.Serializable]
    private class GeoJson
    {
        public string type = "FeatureCollection";
        public Crs crs;
        public float xy_coordinate_resolution;
        public List<Feature> features = new List<Feature>();
    }

    // CRS (Coordinate Reference System)
    [System.Serializable]
    private class Crs
    {
        public string type = "name";
        public CRSProperties properties;

        public Crs(string crsName)
        {
            properties = new CRSProperties(crsName);
        }
    }

    [System.Serializable]
    private class CRSProperties
    {
        public string name;

        public CRSProperties(string crsName)
        {
            name = crsName;
        }
    }

    // GeoJSON feature structure
    [System.Serializable]
    private class Feature
    {
        public string type = "Feature";
        public Geometry geometry;
        public FeatureProperties properties;

        public Feature(Geometry geometry, FeatureProperties properties)
        {
            this.geometry = geometry;
            this.properties = properties;
        }
    }

    // Geometry structure
    [System.Serializable]
    private class Geometry
    {
        public string type; // Can be "Point", "LineString", "Polygon", etc.

        public List<object> coordinates = new List<object>();

        public Geometry(string geometryType)
        {
            type = geometryType;
        }

        public void AddCoordinate(float lon, float lat)
        {
            coordinates.Add(new List<float> { lon, lat });
        }
    }

    // Feature properties structure
    [System.Serializable]
    private class FeatureProperties
    {
        public string name;
        public float height;


        public FeatureProperties(string name, float height)
        {
            this.name = name;
            this.height = height;
        }
    }

    public Transform mapReader;
    public string fileName;
    
    public void WriteFromMapObject()
    {
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

                    //(float lat_1, float lon_1) = ToLatLon(firstPoint.x + centreX, firstPoint.z + centreY);
                    //(float lat_2, float lon_2) = ToLatLon(secondPoint.x + centreX, secondPoint.z + centreY);

                    // Write GeoJSON for this point
                    features.Add(CreateLineStringFeature(firstPoint.x + centreX, firstPoint.z + centreY, secondPoint.x + centreX, secondPoint.z + centreY, buildingHeight, "hehe", 0.0f, 0.0f, 0.0f));
                }
            }

            string geoJson = ConstructGeoJson(features);
            
            // Optionally, write the output to a file
            string filePath = Path.Combine(Application.dataPath, "Resources", fileName + ".geojson");
            File.WriteAllText(filePath, geoJson);

            Debug.Log($"GeoJSON written to {filePath}");
        } else Debug.Log("Map reader is null bro");
    }
    // Example usage

    public static string CreateLineStringFeature(float lat1, float lon1, float lat2, float lon2, float height, string texture, float irradiance, float direct, float diffuse)
    {
        // Create the coordinates array for the LineString
        string coordinates = $"[[{lat1}, {lon1}], [{lat2}, {lon2}]]";

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
                ""texture"": ""{texture}"",
                ""irradiance"": ""{irradiance.ToString()}"",
                ""direct"": ""{direct.ToString()}"",
                ""diffuse"": ""{diffuse.ToString()}""
            }}
        }}";

        return feature;
    }

    public static string CreatePolygonFeature(Vector3[] topFaceVerts, Color color, float irradiance, float direct, float diffuse)
    {
        // Create the coordinates array for the Polygon
        float height = topFaceVerts[0].y;

        string coordinates = "[[";
        string coordinate;
        for (int i = 0; i < topFaceVerts.Length; i++)
        {

            Vector3 point = topFaceVerts[i];
            coordinate = $"[{point.x + centreX}, {point.z + centreY}], ";
            coordinates += coordinate;
        }
        
        coordinate = $"[{topFaceVerts[0].x + centreX}, {topFaceVerts[0].z + centreY}]";
        coordinates += coordinate;
        coordinates += "]]";

        // Create the feature string
        string feature = $@"
        {{
            ""type"": ""Feature"",
            ""geometry"": {{
                ""type"": ""Polygon"",
                ""coordinates"": {coordinates}
            }},
            ""properties"": {{
                ""name"": ""Roof"",
                ""height"": {height},
                ""color"": ""{ConvertRGBToHex(color)}"",
                ""irradiance"": ""{irradiance.ToString()}"",
                ""direct"": ""{direct.ToString()}"",
                ""diffuse"": ""{diffuse.ToString()}""
            }}
        }}";
        return feature;
    }

    static string ConvertRGBToHex(Color color)
    {
        string hexColor = "#" +
            ((int)(color.r * 255)).ToString("X2") +
            ((int)(color.g * 255)).ToString("X2") +
            ((int)(color.b * 255)).ToString("X2");
        return hexColor;
    }

    public static string ConstructGeoJson(List<string> features)
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

    public static (float Latitude, float Longitude) ToLatLon(float x, float y)
    {
        const float EarthRadius = 6378137.0f; // WGS84 major axis
        float lon = x / EarthRadius * (180 / (float)Math.PI);
        float lat = (2 * (float)Math.Atan(Math.Exp(y / EarthRadius)) - (float)Math.PI / 2) * (180 / (float)Math.PI);

        return (lat, lon);
    }

}



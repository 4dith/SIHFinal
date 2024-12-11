using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Esri;
using NetTopologySuite.IO.Esri.Shapefiles.Readers;
using NetTopologySuite.Triangulate.Polygon;
using System;
using NetTopologySuite.Triangulate;
using UnityEditor;

public class MapReader : MonoBehaviour
{
    //[Tooltip("Path to shapefile (.shp)")]
    public string shpPath;
    
    public Material topFaceMat;
    public Material vertFaceMat;

    double centreX = 246006.164014719;
    double centreY = 2549078.7998468;

    public void ReadShapefile()
    {
        if (transform.childCount != 0) return;
        int id = 0;
        
        foreach (Feature feature in Shapefile.ReadAllFeatures(shpPath))
        {
            float height = Convert.ToSingle(feature.Attributes["height"]);

            if (feature.Geometry is MultiPolygon multiPolygon)
            {
                // Iterate through each polygon in the multipolygon
                foreach (Polygon polygon in multiPolygon)
                {
                    // Debug.Log("Triangulating...");
                    try
                    {
                        CreateMeshesFromPolygon(new Polygon((LinearRing) polygon.ExteriorRing), height, id);
                    }
                    catch (Exception)
                    {
                        Debug.LogWarning("Could not triangulate: " + polygon.AsText());
                    }
                }
            }   
            
            id++;
        }
    }

    void CreateMeshesFromPolygon(Polygon polygon, float height, int id)
    {
        MeshFilter mf;
        MeshRenderer mr;
        List<Vector3> vertices = new List<Vector3>();

        for (int i = polygon.Coordinates.Length - 2; i >= 0; i--)
        {
            vertices.Add(new Vector3((float)(polygon.Coordinates[i].X - centreX), height, (float)(polygon.Coordinates[i].Y - centreY)));
        }

        int nTopVertices = vertices.Count;
        List<int> topFaceTris = EarClippingTriangulation.Triangulate(vertices);

        GameObject building = new GameObject(id.ToString());
        building.transform.parent = transform;

        // Top mesh
        Mesh topMesh = new Mesh();
        topMesh.vertices = vertices.ToArray();
        topMesh.triangles = topFaceTris.ToArray();
        
        GameObject topFace = new GameObject("0");
        topFace.transform.parent = building.transform;
        mf = topFace.AddComponent<MeshFilter>();
        mr = topFace.AddComponent<MeshRenderer>();
        mf.sharedMesh = topMesh;
        mr.sharedMaterial = new(topFaceMat);

        // Adding bottom vertices

        for (int i = 0; i < nTopVertices; i++)
        {
            Mesh vertFaceMesh = new Mesh();
            vertFaceMesh.vertices = new Vector3[]
            {
                vertices[i],
                vertices[(i + 1) % nTopVertices],
                vertices[i] + Vector3.down * vertices[i].y,
                vertices[(i + 1) % nTopVertices] + Vector3.down * vertices[(i + 1) % nTopVertices].y
            };

            vertFaceMesh.triangles = new int[] { 0, 1, 2, 1, 3, 2 };
            vertFaceMesh.uv = new Vector2[]
            {
                new(0, 0), new(0, 1), new(1, 0), new(1, 1)
            };

            GameObject vertFace = new GameObject((i + 1).ToString());
            vertFace.transform.parent = building.transform;
            mf = vertFace.AddComponent<MeshFilter>();
            mr = vertFace.AddComponent<MeshRenderer>();
            mf.sharedMesh = vertFaceMesh;
            mr.sharedMaterial = new(vertFaceMat);
        }
    }

    private static bool IsCounterClockwise(Vector3 a, Vector3 b, Vector3 c)
    {
        return (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x) > 0;
    }
}

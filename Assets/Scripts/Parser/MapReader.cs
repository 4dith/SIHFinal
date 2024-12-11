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
    public Material buildingMat;

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
                        CreateMeshFromPolygon(new Polygon((LinearRing) polygon.ExteriorRing), height, id);
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

    void CreateMeshFromPolygon(Polygon polygon, float height, int id)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();

        for (int i = polygon.Coordinates.Length - 2; i >= 0; i--)
        {
            vertices.Add(new Vector3((float)(polygon.Coordinates[i].X - centreX), height, (float)(polygon.Coordinates[i].Y - centreY)));
        }
        int nTopVertices = vertices.Count;
        mesh.subMeshCount = nTopVertices + 1;

        List<int> topFaceTris = EarClippingTriangulation.Triangulate(vertices);

        // Adding bottom vertices

        for (int i = 0; i < nTopVertices; i++)
        {
            Vector3 bottomVert = vertices[i] + Vector3.down * vertices[i].y;
            vertices.Add(bottomVert);
        }
        mesh.SetVertices(vertices);

        mesh.SetTriangles(topFaceTris, 0);
        for (int i = 0; i < nTopVertices; i++)
        {
            int[] verticalFaceTris = new int[6];
            verticalFaceTris[0] = i;
            verticalFaceTris[1] = (i + 1) % nTopVertices;
            verticalFaceTris[2] = nTopVertices + i;

            verticalFaceTris[3] = (i + 1) % nTopVertices;
            verticalFaceTris[4] = nTopVertices + (i + 1) % nTopVertices;
            verticalFaceTris[5] = nTopVertices + i;
            mesh.SetTriangles(verticalFaceTris, i + 1);
        }

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject building = new GameObject(id.ToString());
        building.transform.parent = transform;
        MeshFilter mf = building.AddComponent<MeshFilter>();
        MeshRenderer mr = building.AddComponent<MeshRenderer>();

        Material[] materials = new Material[mesh.subMeshCount];
        for (int i = 0;i < mesh.subMeshCount; i++)
        {
            materials[i] = new Material(buildingMat);
        }

        mr.sharedMaterials = materials;
        mf.sharedMesh = mesh;
    }

    private static bool IsCounterClockwise(Vector3 a, Vector3 b, Vector3 c)
    {
        return (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x) > 0;
    }
}

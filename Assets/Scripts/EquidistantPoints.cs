using NetTopologySuite.Algorithm;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquidistantPoints : MonoBehaviour
{
    //public MeshFilter meshFilter;

    //[Tooltip("Points per square area")]
    //[Range(1, 3)]
    //public int resolutionFactor;

    public Vector2 end1;
    public Vector2 end2;
    public float height;

    public static List<Vector3> SamplePointsBarycentric(Vector3[] verts, int[] tris, out float faceArea)
    {
        List<Vector3> points = new List<Vector3>();
        faceArea = 0;

        for (int k = 0; k < tris.Length; k += 3) { 
            Vector3 vert1 = verts[tris[k]];
            Vector3 vert2 = verts[tris[k + 1]];
            Vector3 vert3 = verts[tris[k + 2]];
            float area = CalculateTriangleArea(vert1, vert2, vert3);
            faceArea += area;
            int numPoints = Mathf.CeilToInt(Mathf.Sqrt(area));

            float epsilon = 1 - 1.0f / numPoints;

            for (int i = 0; i <= numPoints; i++)
            {
                for (int j = 0; j <= numPoints - i; j++)
                {
                    
                    float u = ((float)i / numPoints) * epsilon + (1 - epsilon) / 3;
                    float v = ((float)j / numPoints) * epsilon + (1 - epsilon) / 3;
                    float w = 1 - u - v;

                    points.Add(u * vert1 + v * vert2 + w * vert3);
                    
                }
            }
        }

        return points;
    }

    public void GenerateRectangle()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(end1.x, height, end1.y),
            new Vector3(end2.x, height, end2.y),
            new Vector3(end1.x, 0, end1.y),
            new Vector3(end2.x, 0, end2.y)
        };
        mesh.triangles = new int[] { 0, 1, 2, 1, 3, 2 };
        mesh.uv = new Vector2[]
        {
            new(0, 0), new(0, 1), new(1, 0), new(1, 1)
        };

        MeshFilter filter = GetComponent<MeshFilter>();
        filter.sharedMesh = mesh;
    }

    public static List<Vector3> SampleRectanglePoints(Vector3[] verts, out float faceArea, out int pointsWidth, out Vector3 outwardNormal)
    {
        List<Vector3> points = new List<Vector3>();
        
        Vector3 firstVert = verts[0];
        Vector3 horDir = verts[1] - firstVert;
        Vector3 verDir = verts[2] - firstVert;

        outwardNormal = Vector3.Cross(horDir, verDir).normalized;

        faceArea = horDir.magnitude * verDir.magnitude;

        int width = Mathf.CeilToInt(horDir.magnitude);
        int height = Mathf.CeilToInt(verDir.magnitude);

        firstVert += (horDir - (width - 1) * horDir.normalized + verDir - (height - 1) * verDir.normalized) / 2.0f;
        horDir = horDir.normalized;
        verDir = verDir.normalized;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                points.Add(firstVert + horDir * i + verDir * j);
            }
        }

        pointsWidth = width;
        return points;
    }

    public void CreateRefinedMesh(MeshFilter meshFilter, int resolutionFactor)
    {
        Mesh original = meshFilter.sharedMesh;
        GetComponent<MeshFilter>().sharedMesh = RefineMeshUsingBarycentric(original, resolutionFactor);
    }

    Mesh RefineMeshUsingBarycentric(Mesh mesh, int resolution)
    {
        List<Vector3> newVertices = new List<Vector3>();
        List<Color> newColors = new List<Color>();
        List<int> newTriangles = new List<int>();

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Color[] colors = mesh.colors.Length > 0 ? mesh.colors : null;

        int vertexIndex = 0;

        // Loop through each triangle
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            Color c0 = colors != null ? colors[triangles[i]] : Color.white;
            Color c1 = colors != null ? colors[triangles[i + 1]] : Color.white;
            Color c2 = colors != null ? colors[triangles[i + 2]] : Color.white;

            // Subdivide the triangle using barycentric coordinates
            for (int uStep = 0; uStep <= resolution; uStep++)
            {
                for (int vStep = 0; vStep <= resolution - uStep; vStep++)
                {
                    float u = (float)uStep / resolution;
                    float v = (float)vStep / resolution;
                    float w = 1 - u - v;

                    // Compute interpolated vertex position and color
                    Vector3 point = u * v0 + v * v1 + w * v2;
                    newVertices.Add(point);

                    if (colors != null)
                    {
                        Color interpolatedColor = u * c0 + v * c1 + w * c2;
                        newColors.Add(interpolatedColor);
                    }

                    // Define triangles
                    if (uStep < resolution && vStep < resolution - uStep)
                    {
                        int current = vertexIndex;
                        int right = vertexIndex + 1;
                        int below = vertexIndex + (resolution - uStep + 1);

                        newTriangles.Add(current);
                        newTriangles.Add(below);
                        newTriangles.Add(right);

                        if (vStep < resolution - uStep - 1)
                        {
                            newTriangles.Add(right);
                            newTriangles.Add(below);
                            newTriangles.Add(below + 1);
                        }
                    }
                    vertexIndex++;
                }
            }
        }

        // Create a new mesh
        Mesh newMesh = new Mesh();
        newMesh.vertices = newVertices.ToArray();
        newMesh.triangles = newTriangles.ToArray();
        if (colors != null) newMesh.colors = newColors.ToArray();

        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();

        return newMesh;
    }

    public void GenerateTriangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, int resolutionFactor)
    {
        float area = CalculateTriangleArea(vertex1, vertex2, vertex3);
        int numPoints = Mathf.CeilToInt(Mathf.Sqrt(area) * resolutionFactor);

        // Lists to store vertices, colors, and triangles
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();

        // Generate vertices and colors using barycentric coordinates
        for (int i = 0; i <= numPoints; i++)
        {
            for (int j = 0; j <= numPoints - i; j++)
            {
                float u = (float)i / numPoints;
                float v = (float)j / numPoints;
                float w = 1 - u - v;

                vertices.Add(u * vertex1 + v * vertex2 + w * vertex3);
                colors.Add(ColorFunction(u, v, w));
            }
        }

        // Generate triangle indices
        int index = 0;
        for (int i = 0; i < numPoints; i++)
        {
            for (int j = 0; j < numPoints - i; j++)
            {
                int current = index;
                int next = index + 1;
                int below = index + (numPoints - i + 1);

                // First triangle
                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(below);

                // Second triangle (only if not on the edge)
                if (j < numPoints - i - 1)
                {
                    triangles.Add(next);
                    triangles.Add(below + 1);
                    triangles.Add(below);
                }

                index++;
            }
            index++;
        }

        // Assign to mesh
        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);

        GetComponent<MeshFilter>().mesh = mesh;
    }

    Color ColorFunction(float u, float v, float w)
    {
        float noise = Mathf.PerlinNoise(u * 10000, v * 10000);
        return new Color(noise, noise, noise); // Simple gradient for demonstration
    }

    static float CalculateTriangleArea(Vector3 a, Vector3 b, Vector3 c)
    {
        return Vector3.Cross(b - a, c - a).magnitude * 0.5f;
    }
}

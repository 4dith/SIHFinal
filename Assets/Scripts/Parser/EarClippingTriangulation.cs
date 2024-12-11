using System.Collections.Generic;
using UnityEngine;

// This code was copied from ChatGPT and slightly modified. A more robust implementation is needed which takes in clockwise vertices
// The algorithm does not support holes or common edges. These cases cause undesired behavior and/or errors.

public class EarClippingTriangulation
{
    // Main triangulation function, accepting an isClockwise parameter
    public static List<int> Triangulate(List<Vector3> points)
    {
        List<int> triangles = new List<int>();
        List<int> vertexIndices = new List<int>();

        for (int i = 0; i < points.Count; i++)
        {
            vertexIndices.Add(i);
        }


        while (vertexIndices.Count > 3)
        {
            bool earFound = false;

            for (int i = 0; i < vertexIndices.Count; i++)
            {
                int prev = vertexIndices[(i - 1 + vertexIndices.Count) % vertexIndices.Count];
                int curr = vertexIndices[i];
                int next = vertexIndices[(i + 1) % vertexIndices.Count];

                if (IsEar(points, prev, curr, next, vertexIndices))
                {
                    // Add the ear triangle
                    triangles.Add(prev);
                    triangles.Add(next);
                    triangles.Add(curr);

                    // Remove the ear vertex
                    vertexIndices.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }

            // If no ear is found, break (polygon may be invalid)
            if (!earFound)
            {
                Debug.Log("Failed to find an ear. The polygon may be invalid.");
                return triangles;
            }
        }

        // Add the last remaining triangle
        triangles.Add(vertexIndices[0]);
        triangles.Add(vertexIndices[2]);
        triangles.Add(vertexIndices[1]);

        return triangles;
    }

    private static bool IsEar(List<Vector3> vertices, int prev, int curr, int next, List<int> vertexIndices)
    {
        Vector3 a = vertices[prev];
        Vector3 b = vertices[curr];
        Vector3 c = vertices[next];

        // Check if the triangle is counterclockwise
        if (!IsCounterClockwise(a, b, c))
            return false;

        // Check if no other points are inside the triangle (prev, curr, next)
        for (int i = 0; i < vertexIndices.Count; i++)
        {
            int vi = vertexIndices[i];
            if (vi == prev || vi == curr || vi == next) continue;

            if (PointInTriangle(vertices[vi], a, b, c))
                return false;
        }

        return true;
    }

    private static bool IsCounterClockwise(Vector3 a, Vector3 b, Vector3 c)
    {
        return (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x) > 0;
    }

    private static bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool hasNegative = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPositive = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNegative && hasPositive);
    }

    private static float Sign(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return (p1.x - p3.x) * (p2.z - p3.z) - (p2.x - p3.x) * (p1.z - p3.z);
    }
}
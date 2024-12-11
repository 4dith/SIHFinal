using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BVHTriangle
{
    public Vector3[] vertices;

    public int vAIndex;
    public int vBIndex;
    public int vCIndex;

    public Vector3 VertA => vertices[vAIndex];
    public Vector3 VertB => vertices[vBIndex];
    public Vector3 VertC => vertices[vCIndex];
    public Vector3 Centre => (vertices[vAIndex] + vertices[vBIndex] + vertices[vCIndex]) / 3f;

    public bool CalculateRayIntersection(Vector3 rayOrigin, Vector3 rayDir, float near, float far, out float t)
    {
        t = 0;

        // Edge vectors
        Vector3 edge1 = VertB - VertA;
        Vector3 edge2 = VertC - VertA;

        // Calculate determinant
        Vector3 h = Vector3.Cross(rayDir, edge2);
        float a = Vector3.Dot(edge1, h);

        // If the determinant is near zero, the ray is parallel to the triangle
        if (Mathf.Abs(a) < 1e-6f)
            return false;

        float f = 1.0f / a;
        Vector3 s = rayOrigin - VertA;
        float u = f * Vector3.Dot(s, h);

        // Check if intersection is outside the triangle
        if (u < 0.0f || u > 1.0f)
            return false;

        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(rayDir, q);

        // Check if intersection is outside the triangle
        if (v < 0.0f || u + v > 1.0f)
            return false;

        // Calculate t to find intersection point
        t = f * Vector3.Dot(edge2, q);

        if (t >= near && t <= far) // Intersection between limits
            return true;

        return false; // No valid intersection
    }
}

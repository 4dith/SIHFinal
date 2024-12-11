using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BVHBox
{
    public Vector3 Min = Vector3.positiveInfinity;
    public Vector3 Max = Vector3.negativeInfinity;

    public Vector3 Centre => (Min + Max) * 0.5f;
    public Vector3 Size => Max - Min;

    public void FitVertex(Vector3 vert)
    {
        Min = Vector3.Min(Min, vert);
        Max = Vector3.Max(Max, vert);
    }

    public void FitTriangle(BVHTriangle tri)
    {
        FitVertex(tri.VertA);
        FitVertex(tri.VertB);
        FitVertex(tri.VertC);
    }

    public bool CalculateRayIntersection(Vector3 rayOrigin, Vector3 rayDir, float near, float far, out float t)
    {
        t = 0;
        float tMin = (Min.x - rayOrigin.x) / rayDir.x;
        float tMax = (Max.x - rayOrigin.x) / rayDir.x;

        if (tMin > tMax) Swap(ref tMin, ref tMax);

        float tyMin = (Min.y - rayOrigin.y) / rayDir.y;
        float tyMax = (Max.y - rayOrigin.y) / rayDir.y;

        if (tyMin > tyMax) Swap(ref tyMin, ref tyMax);

        if ((tMin > tyMax) || (tyMin > tMax))
            return false;

        if (tyMin > tMin)
            tMin = tyMin;
        if (tyMax < tMax)
            tMax = tyMax;

        float tzMin = (Min.z - rayOrigin.z) / rayDir.z;
        float tzMax = (Max.z - rayOrigin.z) / rayDir.z;

        if (tzMin > tzMax) Swap(ref tzMin, ref tzMax);

        if ((tMin > tzMax) || (tzMin > tMax))
            return false;

        if (tzMin > tMin)
            tMin = tzMin;
        if (tzMax < tMax)
            tMax = tzMax;

        if (tMax < 0)
            return false;

        t = tMin > 0 ? tMin : tMax;
        return t >= near && t <= far;
    }

    static void Swap(ref float a, ref float b)
    {
        float temp = a;
        a = b;
        b = temp;
    }
}

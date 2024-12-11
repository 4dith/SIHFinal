using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BVHNode
{
    public BVH bvh;
    public BVHBox box = new();
    
    public int index;
    public int ChildA => 2 * index;
    public int ChildB => 2 * index + 1;
    
    public int startTriangle;
    public int triCount;

    public void SetAsRoot(BVH parentBvh)
    {
        foreach (Vector3 vert in parentBvh.vertices)
        {
            box.FitVertex(vert);
        }

        bvh = parentBvh;
        index = 1;
        startTriangle = 0;
        triCount = bvh.triangles.Length;
    }

    public void MidPointSplit(int height)
    {
        if (height <= 1) return;

        BVHNode childA = new()
        {
            index = ChildA,
            bvh = bvh,
        };
        bvh.nodes[ChildA] = childA;

        BVHNode childB = new()
        {
            index = ChildB,
            bvh = bvh,
        };
        bvh.nodes[ChildB] = childB;

        int splitAxis = GetSplitAxis(), partition = startTriangle - 1;

        for (int triI = startTriangle; triI < startTriangle + triCount; triI++)
        {
            BVHTriangle triangle = bvh.triangles[triI];
            bool isSideA = triangle.Centre[splitAxis] <= box.Centre[splitAxis];
            BVHNode child = isSideA ? childA : childB;
            child.box.FitTriangle(triangle);

            if (isSideA)
            {
                partition++;
                bvh.triangles[triI] = bvh.triangles[partition];
                bvh.triangles[partition] = triangle;
            }
        }

        childA.startTriangle = startTriangle;
        childA.triCount = partition + 1 - startTriangle;
        childB.startTriangle = startTriangle + childA.triCount;
        childB.triCount = triCount - childA.triCount;

        childA.MidPointSplit(height - 1);
        childB.MidPointSplit(height - 1);
    }

    int GetSplitAxis()
    {
        Vector3 size = box.Size;
        return size.x > Math.Max(size.y, size.z) ? 0 : size.y > size.z ? 1 : 2;
    }
}

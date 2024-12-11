using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public struct Triangle
{
    public uint v0;
    public uint v1;
    public uint v2;
    public uint colorIndex;
}

public struct BoundingBox
{
    public Vector3 Min;
    public Vector3 Max;
    public uint startIndex;
    public uint triCount;
}

public class BVH
{
    public Vector3[] vertices;
    public BVHNode[] nodes;
    public BVHTriangle[] triangles;
    public int depth;

    public void Initialize(Vector3[] verts, int[] tris, int _depth) {
        vertices = verts;
        triangles = new BVHTriangle[tris.Length / 3];
        nodes = new BVHNode[(int) Mathf.Pow(2, _depth)];
        depth = _depth;
        
        for (int i = 0; i < tris.Length; i += 3)
        {
            BVHTriangle triangle = new()
            {
                vertices = verts,
                vAIndex = tris[i],
                vBIndex = tris[i + 1],
                vCIndex = tris[i + 2]
            };

            triangles[i / 3] = triangle;
        }

        nodes[1] = new BVHNode();
        nodes[1].SetAsRoot(this);
        nodes[1].MidPointSplit(_depth);
    }

    public BVHTriangle TraverseBVH(Vector3 rayOrigin, Vector3 rayDir, float near, float far, out float tHit)
    {
        BVHTriangle closestHit = null;
        tHit = float.PositiveInfinity;

        int[] stack = new int[depth];
        stack[0] = 1;
        int top = 0;
        float t;

        while (top != -1)
        {
            BVHNode node = nodes[stack[top--]];

            if (!node.box.CalculateRayIntersection(rayOrigin, rayDir, near, far, out t))
                continue;

            if (node.index >= nodes.Length / 2) // Leaf node
            {
                for (int triIndex = node.startTriangle; triIndex < node.startTriangle + node.triCount; triIndex++)
                {
                    if (triangles[triIndex].CalculateRayIntersection(rayOrigin, rayDir, near, far, out t))
                    {
                        if (t < tHit)
                        {
                            tHit = t;
                            closestHit = triangles[triIndex];
                        }
                    }
                }
            } else
            {
                stack[++top] = node.ChildA;
                stack[++top] = node.ChildB;
            }
        }

        return closestHit;
    }

    public static BVH CreateBVH(Transform meshObj, int depth)
    {
        List<Vector3> vertList = new();
        List<int> triList = new();
        int vertIndex = 0;

        for (int i = 0; i < meshObj.childCount; i++)
        {
            Transform child = meshObj.GetChild(i);

            MeshFilter meshFilter = child.GetComponent<MeshFilter>();
            int[] tris = meshFilter.sharedMesh.triangles;
            Vector3[] verts = meshFilter.sharedMesh.vertices;

            for (int j = 0; j < verts.Length; j++)
            {
                vertList.Add(child.TransformPoint(verts[j]));
            }

            for (int j = 0; j < tris.Length; j++)
            {
                triList.Add(vertIndex + tris[j]);
            }

            vertIndex += verts.Length;
        }

        BVH bvh = new BVH();
        bvh.Initialize(vertList.ToArray(), triList.ToArray(), depth);
        return bvh;
    }

    public void DrawBVH(Color boxColor)
    {
        Gizmos.color = boxColor;

        for (int i = 1; i < nodes.Length; i++)
        {
            Gizmos.DrawWireCube(nodes[i].box.Centre, nodes[i].box.Size);
        }
    }

    public void DebugView(Color boxColor, Color triColor, Color rayColor, Transform ray, float maxRayLength)
    {
        DrawBVH(boxColor);

        Gizmos.color = triColor;

        float tHit;
        BVHTriangle triangle = TraverseBVH(ray.position, ray.forward, 0.0f, maxRayLength, out tHit);
        if (triangle != null)
        {
            Mesh mesh = new Mesh()
            {
                vertices = new Vector3[] { triangle.VertA, triangle.VertB, triangle.VertC },
                triangles = new int[] { 0, 1, 2 }
            };
            mesh.RecalculateNormals();

            Gizmos.DrawMesh(mesh);
        }

        Gizmos.color = rayColor;
        Gizmos.DrawRay(ray.position, ray.forward * Mathf.Min(tHit, maxRayLength));
    }
}

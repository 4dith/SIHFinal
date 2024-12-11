using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracer : MonoBehaviour
{
    RenderTexture renderTexture;
    public BVH bvh;

    Camera _camera;
    Triangle[] triangles;
    BoundingBox[] boundingBoxes;
    ComputeBuffer triangleBuffer, vertexBuffer, boundsBuffer, colorBuffer;

    [Header("Graphics")]
    public Color buildingColor;

    [Header("Render Settings")]
    public ComputeShader computeShader;
    public int screenWidth;
    public int screenHeight;
    public int nSamples;
    public int maxDepth;

    [Header("BVH Settings")]
    [Range(1, 16)]
    public int depth = 10;
    public Transform meshObj;

    [Header("Debug Settings")]
    public Color boxColor = Color.red;
    public Color rayColor = Color.green;
    public Color triColor = Color.blue;

    public Transform ray;
    public float maxRayLength;

    private void Start()
    {
        _camera = GetComponent<Camera>();
        bvh = BVH.CreateBVH(meshObj, depth);
        
        triangles = new Triangle[bvh.triangles.Length];
        for (int i = 0; i < bvh.triangles.Length; i++)
        {
            triangles[i] = new Triangle()
            {
                v0 = (uint)bvh.triangles[i].vAIndex,
                v1 = (uint)bvh.triangles[i].vBIndex,
                v2 = (uint)bvh.triangles[i].vCIndex,
                colorIndex = 0
            };
        }

        boundingBoxes = new BoundingBox[bvh.nodes.Length];
        for (int i = 1; i < bvh.nodes.Length; i++)
        {
            BVHNode bVHNode = bvh.nodes[i];
            boundingBoxes[i] = new BoundingBox()
            {
                Min = bVHNode.box.Min,
                Max = bVHNode.box.Max,
                startIndex = (uint)bVHNode.startTriangle,
                triCount = (uint)bVHNode.triCount
            };
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(screenWidth, screenHeight, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();

            computeShader.SetTexture(0, "Result", renderTexture);
            computeShader.SetMatrix("CameraInverseProj", _camera.projectionMatrix.inverse);
            computeShader.SetInt("NSamples", nSamples);
            computeShader.SetInt("MaxDepth", maxDepth);

            triangleBuffer = new ComputeBuffer(triangles.Length, sizeof(uint) * 4);
            triangleBuffer.SetData(triangles);
            computeShader.SetBuffer(0, "Triangles", triangleBuffer);

            vertexBuffer = new ComputeBuffer(bvh.vertices.Length, sizeof(float) * 3);
            vertexBuffer.SetData(bvh.vertices);
            computeShader.SetBuffer(0, "Vertices", vertexBuffer);

            boundsBuffer = new ComputeBuffer(boundingBoxes.Length, sizeof(float) * 6 + sizeof(uint) * 2);
            boundsBuffer.SetData(boundingBoxes);
            computeShader.SetBuffer(0, "Bounds", boundsBuffer);

            Vector3[] colors = new Vector3[1];
            colors[0] = new()
            {
                x = buildingColor.r,
                y = buildingColor.g,
                z = buildingColor.b
            };
            colorBuffer = new ComputeBuffer(colors.Length, sizeof(float) * 3);
            colorBuffer.SetData(colors);
            computeShader.SetBuffer(0, "Colors", colorBuffer);
        }

        computeShader.SetMatrix("CameraToWorld", _camera.cameraToWorldMatrix);
        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);
        Graphics.Blit(renderTexture, destination);
    }

    void OnDestroy()
    {
        // Release the buffer when done (important to avoid memory leaks)
        if (vertexBuffer != null)
        {
            vertexBuffer.Release();
        }

        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
        }

        if (boundsBuffer != null)
        {
            boundsBuffer.Release();
        }

        if (colorBuffer != null)
        {
            colorBuffer.Release();
        }
    }

    private void OnDrawGizmos()
    {
        if (bvh != null) bvh.DebugView(boxColor, triColor, rayColor, ray, maxRayLength);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ColorlessTri
{
    public uint v0;
    public uint v1;
    public uint v2;
}

struct TestPointMeta
{
    public int buildingPointLimit;
    public List<int> facePointCounts;
    public List<float> faceAreas;
    public List<bool> isFaceVerticalList;
    public List<int> vertFacePointWidths;
    public List<Vector3> vertFaceNormalList;
}

public class BIPVEstimator : MonoBehaviour
{
    public Transform meshObj;

    [Header("Light and Energy")]
    [Range(0, 8759)]
    public int hourOfYear;
    public Light directionalLight;
    public string epwFilePath;
    public EPWData[] ePWData;

    float lat = 23.0225f * Mathf.Deg2Rad, lon = 72.5714f * Mathf.Deg2Rad;

    [Header("Compute Shader")]
    public BVH bvh;
    [Range(1, 16)]
    public int depth = 10;
    public ComputeShader computeShader;

    List<Vector3> testPoints = new();
    List<TestPointMeta> testPointData = new();
    uint[] testResults;

    private ComputeBuffer triangleBuffer, vertexBuffer, boundsBuffer, inputBuffer, outputBuffer;

    [Header("Debug Settings")]
    public Color boxColor = Color.green;
    public Color blockColor = Color.red;
    public Color freeColor = Color.blue;

    public Transform ray;
    public float maxRayLength;

    public float minBIPVThreshold = 0f;
    public float maxBIPVThreshold = 1000f;

    public void ComputeObstructions()
    {
        testPoints.Clear();
        testPointData.Clear();
        int currentPointIndex = 0;

        int nBuildings = meshObj.childCount;
        for (int i = 0; i < nBuildings; i++)
        {
            Transform building = meshObj.GetChild(i);

            List<int> facePointCounts = new();
            List<bool> isFaceVerticalList = new();
            List<float> faceAreaList = new();
            List<int> vertFacePointWidths = new();
            List<Vector3> vertFaceNormalList = new();


            for (int j = 0; j < building.childCount; j++)
            {
                Transform buildingFace = building.GetChild(j);
                Mesh faceMesh = buildingFace.GetComponent<MeshFilter>().sharedMesh;
                List<Vector3> samplePoints;
                float faceArea;
                int pointsWidth;

                if (j == 0)
                    samplePoints = EquidistantPoints.SamplePointsBarycentric(faceMesh.vertices, faceMesh.triangles, out faceArea);
                
                else
                {
                    samplePoints = EquidistantPoints.SampleRectanglePoints(faceMesh.vertices, out faceArea, out pointsWidth, out Vector3 outwardNormal);
                    vertFacePointWidths.Add(pointsWidth);
                    vertFaceNormalList.Add(outwardNormal);
                } 
                facePointCounts.Add(samplePoints.Count);
                isFaceVerticalList.Add(j != 0);
                faceAreaList.Add(faceArea);
                currentPointIndex += samplePoints.Count;
                testPoints.AddRange(samplePoints);
            }

            TestPointMeta buildingPointData = new()
            {
                buildingPointLimit = currentPointIndex,
                facePointCounts = facePointCounts,
                isFaceVerticalList = isFaceVerticalList,
                faceAreas = faceAreaList,
                vertFacePointWidths = vertFacePointWidths,
                vertFaceNormalList = vertFaceNormalList,
            };
            testPointData.Add(buildingPointData);
        }

        bvh = BVH.CreateBVH(meshObj, depth);

        ColorlessTri[] triangles = new ColorlessTri[bvh.triangles.Length];

        for (int i = 0; i < bvh.triangles.Length; i++)
        {
            triangles[i] = new ColorlessTri()
            {
                v0 = (uint)bvh.triangles[i].vAIndex,
                v1 = (uint)bvh.triangles[i].vBIndex,
                v2 = (uint)bvh.triangles[i].vCIndex,
            };
        }

        BoundingBox[] boundingBoxes = new BoundingBox[bvh.nodes.Length];
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

        computeShader.SetInt("_NumPoints", testPoints.Count);
        computeShader.SetInt("_NumBounds", bvh.nodes.Length);
        computeShader.SetVector("SunUnitDirection", new Vector4(directionalLight.transform.forward.x, directionalLight.transform.forward.y, directionalLight.transform.forward.z));

        triangleBuffer = new ComputeBuffer(triangles.Length, sizeof(uint) * 3);
        triangleBuffer.SetData(triangles);
        computeShader.SetBuffer(0, "Triangles", triangleBuffer);

        vertexBuffer = new ComputeBuffer(bvh.vertices.Length, sizeof(float) * 3);
        vertexBuffer.SetData(bvh.vertices);
        computeShader.SetBuffer(0, "Vertices", vertexBuffer);

        boundsBuffer = new ComputeBuffer(boundingBoxes.Length, sizeof(float) * 6 + sizeof(uint) * 2);
        boundsBuffer.SetData(boundingBoxes);
        computeShader.SetBuffer(0, "Bounds", boundsBuffer);

        inputBuffer = new ComputeBuffer(testPoints.Count, sizeof(float) * 3);
        inputBuffer.SetData(testPoints);
        computeShader.SetBuffer(0, "InputPoints", inputBuffer);

        testResults = new uint[testPoints.Count];
        outputBuffer = new ComputeBuffer(testResults.Length, sizeof(uint) * 1);
        outputBuffer.SetData(testResults);
        computeShader.SetBuffer(0, "IsBlocked", outputBuffer);

        computeShader.Dispatch(0, Mathf.CeilToInt(testPoints.Count / 256.0f), 1, 1);
        outputBuffer.GetData(testResults);

        triangleBuffer.Release();
        vertexBuffer.Release();
        boundsBuffer.Release();
        inputBuffer.Release();
        outputBuffer.Release();

        Debug.Log("Calculated obstructions");
    }

    public void CalculatePerFaceBIPV()
    {
        if (testResults == null || testPointData.Count == 0 || testPoints.Count == 0 || ePWData == null)
        {
            Debug.Log("Data not found");
            return;
        }

        for (int buildingId = 0; buildingId < testPointData.Count; buildingId++)
        {
            TestPointMeta buildingPointData = testPointData[buildingId];
            int startIndex = (buildingId == 0) ? 0 : testPointData[buildingId - 1].buildingPointLimit;
            int endIndex = buildingPointData.buildingPointLimit;
            List<int> facePointCounts = buildingPointData.facePointCounts;
            List<float> faceAreas = buildingPointData.faceAreas;
            List<bool> isFaceVerticalList = buildingPointData.isFaceVerticalList;
            List<int> vertFacePointWidths = buildingPointData.vertFacePointWidths;
            List<Vector3> vertFaceNormalList = buildingPointData.vertFaceNormalList;

            Transform buildingChild = meshObj.GetChild(buildingId);

            int currentStartIndex = startIndex;

            for (int faceIndex = 0; faceIndex < facePointCounts.Count; faceIndex++)
            {
                int numPointsInFace = facePointCounts[faceIndex];
                float faceArea = faceAreas[faceIndex];
                uint numObstructions = 0;
                for (int pointIndex = currentStartIndex; pointIndex < currentStartIndex + numPointsInFace; pointIndex++)
                {
                    numObstructions += testResults[pointIndex];
                }

                float shadowFraction = (float) numObstructions / numPointsInFace;

                EPWData hourlyData = ePWData[hourOfYear];
                int tiltFactor = isFaceVerticalList[faceIndex] ? 0 : 1;

                Vector3 outwardNormal = (faceIndex == 0) ? Vector3.up : vertFaceNormalList[faceIndex - 1];
                float cosTheta = Mathf.Max(0f, Vector3.Dot(outwardNormal, -directionalLight.transform.forward));

                float eDirect = hourlyData.dirNI * (1.0f - shadowFraction) * cosTheta;
                float eDiffuse = hourlyData.difHI * (1 + tiltFactor) / 2.0f;
                float eReflected = hourlyData.globHI * 0.2f * (1 - tiltFactor) / 2.0f;
                float faceIrradiance = eDirect + eDiffuse + eReflected;

                if (faceIndex == 0)
                {
                    Color faceColor = Color.Lerp(blockColor, freeColor, Mathf.Max((faceIrradiance - minBIPVThreshold), 0f) / (maxBIPVThreshold - minBIPVThreshold));
                    buildingChild.GetChild(faceIndex).GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Color", faceColor);
                }
                else
                {
                    int width = vertFacePointWidths[faceIndex - 1];
                    if (width == 0) break;
                    
                    int height = numPointsInFace / width;
                    
                    Texture2D faceTexture = new Texture2D(width, height);

                    int pIndex = currentStartIndex;
                    for (int i = 0; i < width; i++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            float pointIrradiance = (1 - testResults[pIndex]) * hourlyData.dirNI * cosTheta + eDiffuse + eReflected;
                            Color pixelColor = Color.Lerp(blockColor, freeColor, Mathf.Max((pointIrradiance - minBIPVThreshold), 0f) / (maxBIPVThreshold - minBIPVThreshold));
                            faceTexture.SetPixel(i, j, pixelColor);
                            pIndex++;
                        }
                    }
                    faceTexture.Apply();

                    buildingChild.GetChild(faceIndex).GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_MainTex", faceTexture);
                }

                currentStartIndex += numPointsInFace;
            }
        }

        Debug.Log("Calculated BIPV potentials");
    }

    private void OnDrawGizmos()
    {
        if (bvh == null) return;
        // Debug.Log("Hello");
        bvh.DrawBVH(boxColor);

        //for (int i = 0; i < testPoints.Count; i++)
        //{
        //    Gizmos.color = testResults[i] == 1 ? blockColor : freeColor;
        //    Gizmos.DrawRay(testPoints[i], -directionalLight.transform.forward * maxRayLength);
        //}

        //bvh.DebugView(boxColor, Color.yellow, Color.white, ray, maxRayLength);
    }

    public void SetSunPosition()
    {
        float dec = -23.45f * Mathf.Cos(360.0f / 365.0f * (hourOfYear / 24 + 11) * Mathf.Deg2Rad) * Mathf.Deg2Rad;
        float hAngle = 15f * (hourOfYear % 24 - 12) * Mathf.Deg2Rad;

        float el = Mathf.Asin(Mathf.Sin(dec) * Mathf.Sin(lat) - Mathf.Cos(dec) * Mathf.Cos(lat) * Mathf.Cos(hAngle));
        // float azi = Mathf.Acos((Mathf.Sin(dec) * Mathf.Cos(lat) - Mathf.Cos(dec) * Mathf.Sin(lat) * Mathf.Cos(hAngle)) / Mathf.Cos(el));
        float azi = Mathf.Atan2(
                                -Mathf.Sin(hAngle),
                                (Mathf.Tan(dec) * Mathf.Cos(lat) - Mathf.Sin(lat) * Mathf.Cos(hAngle))
                            );

        // Convert to degrees
        el = el * Mathf.Rad2Deg;
        azi = azi * Mathf.Rad2Deg;

        // Step 3: Set the Unity Directional Light rotation
        Vector3 sunDirection = new Vector3(
            -el,   // Tilt downward for altitude
            -azi,         // Rotate around Y-axis for azimuth
            0f
        );

        directionalLight.transform.rotation = Quaternion.Euler(sunDirection);
    }
}

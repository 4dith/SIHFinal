using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RayTracer))]
public class RayTracerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        RayTracer tracer = (RayTracer)target;

        if (GUILayout.Button("Make BVH"))
        {
            tracer.bvh = BVH.CreateBVH(tracer.meshObj, tracer.depth);
        }
    }
}

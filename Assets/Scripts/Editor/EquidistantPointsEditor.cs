using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EquidistantPoints))]
public class EquidistantPointsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EquidistantPoints generator = (EquidistantPoints)target;
        //generator.SamplePoints();

        //if (GUILayout.Button("Create Refined Mesh"))
        //{
            
        //}
    }
}

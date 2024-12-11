using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapReader))]
public class MapReaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        MapReader reader = (MapReader)target;

        if (GUILayout.Button("Read Shapefile"))
        {
            reader.ReadShapefile();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(MapWriter))]
public class MapWriterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        MapWriter writer = (MapWriter)target;

        if (GUILayout.Button("Write GeoJSON"))
        {
            writer.WriteFromMapObject();
        }
    }
}

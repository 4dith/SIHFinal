using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BIPVEstimator))]
public class BIPVEstimatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        BIPVEstimator estimator = (BIPVEstimator)target;

        if (GUILayout.Button("Load Weather Data"))
        {
            estimator.ePWData = EPWReader.ReadEPW(estimator.epwFilePath);
        }

        if (GUILayout.Button("Compute Obstructions"))
        {
            estimator.ComputeObstructions();
        }

        if (GUILayout.Button("Calculate BIPV Potentials"))
        {
            estimator.CalculatePerFaceBIPV();
        }

        estimator.SetSunPosition();
    }
}

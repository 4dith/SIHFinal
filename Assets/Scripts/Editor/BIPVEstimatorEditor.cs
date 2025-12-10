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

        estimator.SetSunPosition(estimator.dayOfYear * 24 + 12);

        if (GUILayout.Button("Compute Obstructions and Potentials"))
        {
            estimator.ePWData = EPWReader.ReadEPW(estimator.epwFilePath);
            estimator.ComputeObstructions();
            estimator.CalculatePerFaceBIPV();
        }
    }
}

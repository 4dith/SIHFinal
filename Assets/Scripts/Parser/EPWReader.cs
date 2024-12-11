using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public struct EPWData
{
    public float globHI;
    public float dirNI;
    public float difHI;
}

public static class EPWReader
{
    public static EPWData[] ReadEPW(string filePath)
    {
        List<EPWData> data = new();
        string[] lines = File.ReadAllLines(filePath);

        for (int i = 8; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] values = line.Split(',');

            float globHI = float.Parse(values[13]), dirHI = float.Parse(values[14]), difNI = float.Parse(values[15]);

            //Debug.Log("Hour " + (i - 7) + " GHI: " + globHI + " DNI: " + dirHI + " DHI: " + difNI);

            data.Add(new EPWData()
            {
                globHI = globHI,
                dirNI = dirHI,
                difHI = difNI
            });
        }

        Debug.Log("Weather data loaded");
        return data.ToArray();
    }
}

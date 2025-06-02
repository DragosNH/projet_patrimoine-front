using System;
using UnityEngine;

[Serializable]
public class ModelInfo
{
    public int id;
    public string name;
    public string file;
    [HideInInspector] public double latitude;
    [HideInInspector] public double longitude;
    [HideInInspector] public double altitude;
}

[Serializable]
public class ModelInfoList
{
    public ModelInfo[] results;
}


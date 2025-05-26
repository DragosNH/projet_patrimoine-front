using System;

[Serializable]
public class ModelInfo
{
    public int id;
    public string name;
    public string file;
    public double latitude;
    public double longitude;
    public double altitude;
}

[Serializable]
public class ModelInfoList
{
    public ModelInfo[] results;
}


using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class ModelInfo
{
    public int id;
    public string name;
    public string file;
    public float latitude;
    public float longitude;
    public float altitude;
}

[Serializable]
public class ModelInfoList
{
    public List<ModelInfo> results;
}

public class ModelLoader : MonoBehaviour
{
    string apiUrl = NetworkConfig.ServerIP + "/api/models/";

    public float originLat = 48.5734f;
    public float originLon = 7.7521f;

    void Start()
    {
        StartCoroutine(FetchAndLoad());
    }

    IEnumerator FetchAndLoad()
    {
        using var req = UnityWebRequest.Get(apiUrl);
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) 
        {
            Debug.LogError("Faild to fetch models: " + req.error);
            yield break;
        }

        string wrapper = "{\"results\":" + req.downloadHandler.text + "}";
        var list = JsonUtility.FromJson<ModelInfoList>(wrapper);

        foreach (var info in list.results) 
        {
            StartCoroutine(DownloadAndInstantiate(info));
        }
    }

    Vector3 GeoToWorld(float lat, float lon, float alt)
    {
        float metersPerDegLat = 111132f;
        float metersPerDegLon = 111320f * Mathf.Cos(originLat * Mathf.Deg2Rad);
        float x = (lon - originLon) * metersPerDegLon;
        float z = (lat - originLat) * metersPerDegLat;
        return new Vector3(x, alt, z);
    }

    async IEnumerator DownloadAndInstantiate(ModelInfo info)
    {
        var loader = new GLTFast.GltfImport();
        bool success = await loader.Load(info.file);
        if (!success)
        {
            Debug.LogError($"Failed to load {info.name}");
            yield break;
        }

        var go = loader.InstantiateMainScene();
        go.transform.position = GeoToWorld(
            info.latitude, info.longitude, info.altitude
        );
        go.name = info.name;
    }

}
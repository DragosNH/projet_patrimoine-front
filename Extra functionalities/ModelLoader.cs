using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;


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
    public ModelInfo[] results;
}

public class ModelLoader : MonoBehaviour
{
    [Header("API Settings")]
    [Tooltip("Point this at your /api/models/ JSON endpoint")]
    public string apiUrl = NetworkConfig.ServerIP + "/api/models/";


    [Header("Origin GPS (for Geo→World)")]
    public float originLat = 48.5734f;
    public float originLon = 7.7521f;

    public TMP_Text debugTxt;

    void Start()
    {
        StartCoroutine(FetchAndLoad());
    }

    IEnumerator FetchAndLoad()
    {
        var req = UnityWebRequest.Get(apiUrl);
        req.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("access_token"));
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to fetch model list: {req.error}");
            yield break;
        }

        string json = "{\"results\":" + req.downloadHandler.text + "}";
        var list = JsonUtility.FromJson<ModelInfoList>(json);

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

    IEnumerator DownloadAndInstantiate(ModelInfo info)
    {
        Debug.Log($"[ModelLoader] ▶ Starting DownloadAndInstantiate for {info.name} (URL: {info.file})"); // ← Debug Text
        debugTxt.text = $"[ModelLoader] ▶ Starting DownloadAndInstantiate for {info.name} (URL: {info.file})"; // ← Debug Text
    

        UnityWebRequest bundleReq = UnityWebRequestAssetBundle.GetAssetBundle(info.file);
        Debug.Log($"[ModelLoader] → Sending request..."); // ← Debug Text
        debugTxt.text = $"[ModelLoader] → Sending request..."; // ← Debug Text
        yield return bundleReq.SendWebRequest();

        if (bundleReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to download bundle for {info.name}: {bundleReq.error}");
            yield break;
        }
        Debug.Log($"[ModelLoader] ✔ Download succeeded for {info.name}, bytes received: {bundleReq.downloadedBytes}"); // ← Debug Text
        debugTxt.text = $"[ModelLoader] ✔ Download succeeded for {info.name}, bytes received: {bundleReq.downloadedBytes}"; // ← Debug Text

        var bundle = DownloadHandlerAssetBundle.GetContent(bundleReq);
        string[] assetNames = bundle.GetAllAssetNames();

        var prefab = bundle.LoadAsset<GameObject>(assetNames[0]);
        var go = Instantiate(prefab,
                             GeoToWorld(info.latitude, info.longitude, info.altitude),
                             Quaternion.identity);
        go.name = info.name;

        bundle.Unload(false);
    }

}
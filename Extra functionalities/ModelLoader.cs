using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using TMPro;
using Newtonsoft.Json;


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

public class ModelLoader : MonoBehaviour
{
    [Header("API Settings")]
    [Tooltip("Point this at your /api/models/ JSON endpoint")]
    public string apiUrl = NetworkConfig.ServerIP + "/api/models/";

    [Header("Calibration Points")]
    [Tooltip("Enter one or more trusted GPS readings here to improve accuracy")]
    public List<LatLon> calibrationPoints = new List<LatLon>();

    private double _originLat;
    private double _originLon;

    [Serializable]
    public struct LatLon
    {
        [Tooltip("Calibration latitude (decimal degrees)")]
        public double latitude;
        [Tooltip("Calibration longitude (decimal degrees)")]
        public double longitude;
    }

    public TMP_Text debugTxt;

    void Start()
    {

        Input.location.Start();

        if (calibrationPoints != null && calibrationPoints.Count > 0)
        {
            _originLat = calibrationPoints.Average(p => p.latitude);
            _originLon = calibrationPoints.Average(p => p.longitude);
            debugTxt.text = $"Using {calibrationPoints.Count} manual calibrations";
        }
        else
        {
            _originLat = (float)Input.location.lastData.latitude;
            _originLon = (float)Input.location.lastData.longitude;
            debugTxt.text = $"Using device GPS origin";
        }


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

    Vector3 GeoToWorld(double lat, double lon, double alt)
    {
        double metersPerDegLat = 111132.0;
        double metersPerDegLon = 111320.0 * Math.Cos(_originLat * Math.PI / 180.0);

        double x = (lon - _originLon) * metersPerDegLon;
        double z = (lat - _originLat) * metersPerDegLat;
        float y = (float)alt;

        return new Vector3((float)x, y, (float)z);
    }

    IEnumerator DownloadAndInstantiate(ModelInfo info)
    {
        debugTxt.text = $"Downloading {info.name} from {info.file}";

        var bundleReq = UnityWebRequestAssetBundle.GetAssetBundle(info.file);
        yield return bundleReq.SendWebRequest();

        if (bundleReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Download failed: {bundleReq.error}");
            yield break;
        }

        var bundle = DownloadHandlerAssetBundle.GetContent(bundleReq);
        var assetNames = bundle.GetAllAssetNames();
        var prefab = bundle.LoadAsset<GameObject>(assetNames[0]);
        var worldPos = GeoToWorld(info.latitude, info.longitude, info.altitude);
        var instance = Instantiate(prefab, worldPos, Quaternion.identity);
        instance.name = info.name;

        bundle.Unload(false);
        debugTxt.text = $"{info.name} instantiated";
    }

}
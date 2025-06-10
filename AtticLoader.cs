using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

[System.Serializable]
public class AtticData
{
    public int id;
    public string name;
    public string file;
}

[System.Serializable]
public class AtticListWrapper
{
    public AtticData[] atticSkeletons;
}

public class AtticLoader : MonoBehaviour
{
    [SerializeField] ARSessionOrigin arOrigin; 

    string apiUrl = NetworkConfig.ServerIP + "/api/attic-skeleton/";

    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        StartCoroutine(FetchAndLoadAttic());
    }

    IEnumerator FetchAndLoadAttic()
    {
        var req = UnityWebRequest.Get(apiUrl);
        req.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("access_token"));
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch attic model: " + req.error);
            yield break;
        }

        string wrapped = "{\"atticSkeletons\":" + req.downloadHandler.text + "}";
        var data = JsonConvert.DeserializeObject<AtticListWrapper>(wrapped);

        if (data.atticSkeletons.Length == 0)
        {
            Debug.LogWarning("No attic model found in API response.");
            yield break;
        }

        string bundleUrl = data.atticSkeletons[0].file;

        var bundleReq = UnityWebRequestAssetBundle.GetAssetBundle(bundleUrl);
        yield return bundleReq.SendWebRequest();

        if (bundleReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download AssetBundle: " + bundleReq.error);
            yield break;
        }

        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(bundleReq);
        if (bundle == null)
        {
            Debug.LogError("AssetBundle is null.");
            yield break;
        }

        string assetName = bundle.GetAllAssetNames()[0];
        GameObject prefab = bundle.LoadAsset<GameObject>(assetName);
        bundle.Unload(false);

        if (prefab == null)
        {
            Debug.LogError("Could not load prefab from AssetBundle.");
            yield break;
        }

        // Instantiate under ARSessionOrigin so it moves with your AR camera
        GameObject attic = Instantiate(
            prefab,
            Vector3.zero,
            Quaternion.identity,
            arOrigin.trackablesParent);

        // Position it 1.5m in front of the camera, 0.5m up
        Transform cam = arOrigin.camera.transform;
        Vector3 forward = cam.forward;
        forward.y = 0f;
        attic.transform.localPosition = forward * 1.5f + Vector3.up * 0.5f;
        attic.transform.localRotation = Quaternion.LookRotation(-forward);
    }

    public void Return()
    {
        SceneManager.LoadScene("MainPage");
    }
}

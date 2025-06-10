using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;

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
    string apiUrl = NetworkConfig.ServerIP + "/api/attic-skeleton/";

    void Start()
    {
        StartCoroutine(FetchAndLoadAttic());
    }

    IEnumerator FetchAndLoadAttic()
    {
        UnityWebRequest req = UnityWebRequest.Get(apiUrl);
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

        UnityWebRequest bundleRequest = UnityWebRequestAssetBundle.GetAssetBundle(bundleUrl);
        yield return bundleRequest.SendWebRequest();

        if (bundleRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download AssetBundle: " + bundleRequest.error);
            yield break;
        }

        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(bundleRequest);
        if (bundle == null)
        {
            Debug.LogError("AssetBundle is null.");
            yield break;
        }

        string assetName = bundle.GetAllAssetNames()[0];
        GameObject prefab = bundle.LoadAsset<GameObject>(assetName);

        if (prefab == null)
        {
            Debug.LogError("Could not load prefab from AssetBundle.");
            yield break;
        }

        GameObject attic = Instantiate(prefab);
        attic.transform.position = new Vector3(0f, 0f, 2f);

        bundle.Unload(false);
    }
}

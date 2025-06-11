using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.Networking;
using UnityEngine.UI;
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
    XROrigin xrOrigin;
    [Header("UI Controls")]
    public Slider heightSlider;

    private GameObject atticInstance;
    private float manualYOffset = 0f;

    string apiUrl = NetworkConfig.ServerIP + "/api/attic-skeleton/";

    void Awake()
    {
        xrOrigin = Object.FindFirstObjectByType<XROrigin>();
        if (xrOrigin == null)
            Debug.LogError("No XROrigin found in scene!");
    }

    void Start()
    {
        // Keep the screen on all the time as long as if this scene is active
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        ARSession.stateChanged += OnARStateChanged;
        ARSession.CheckAvailability();

        StartCoroutine(FetchAndLoadAttic());
    }

    void OnEnable()
    {
        ARSession.stateChanged += OnARStateChanged;
    }

    void OnDisable()
    {
        ARSession.stateChanged -= OnARStateChanged;
    }

    void OnARStateChanged(ARSessionStateChangedEventArgs evt)
    {
        if (evt.state == ARSessionState.SessionTracking)
            DisableSkybox();
    }

    void DisableSkybox()
    {
        // 1) Remove any skybox material
        RenderSettings.skybox = null;

        // 2) Make the AR camera only clear depth
        if (Camera.main != null)
            Camera.main.clearFlags = CameraClearFlags.Depth;

        Debug.Log("Skybox disabled, clearFlags set to Depth");
    }

    // --------------------- Fetch the model and load it in AR ---------------------
    IEnumerator FetchAndLoadAttic()
    {
        // Download model metadata
        var req = UnityWebRequest.Get(apiUrl);
        req.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("access_token"));
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch attic model: " + req.error);
            yield break;
        }

        // Parse JSON
        string wrapped = "{\"atticSkeletons\":" + req.downloadHandler.text + "}";
        var data = JsonConvert.DeserializeObject<AtticListWrapper>(wrapped);
        if (data.atticSkeletons.Length == 0)
        {
            Debug.LogWarning("No attic model found in API response.");
            yield break;
        }
        // Download AssetBundle
        var bundleReq = UnityWebRequestAssetBundle.GetAssetBundle(data.atticSkeletons[0].file);
        yield return bundleReq.SendWebRequest();
        if (bundleReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download AssetBundle: " + bundleReq.error);
            yield break;
        }

        var bundle = DownloadHandlerAssetBundle.GetContent(bundleReq);
        if (bundle == null)
        {
            Debug.LogError("AssetBundle is null.");
            yield break;
        }

        // Load prefab
        var assetName = bundle.GetAllAssetNames()[0];
        var prefab = bundle.LoadAsset<GameObject>(assetName);
        bundle.Unload(false);

        if (prefab == null)
        {
            Debug.LogError("Could not load prefab from AssetBundle.");
            yield break;
        }

        // Instantiate and keep reference
        atticInstance = Instantiate(prefab,
            Vector3.zero,
            Quaternion.identity,
            xrOrigin.transform);

        // Initial position
        manualYOffset = (heightSlider != null) ? heightSlider.value : 0f;
        UpdateAtticPosition();

        // Hook slider AFTER instance exists
        if (heightSlider != null)
        {
            heightSlider.onValueChanged.RemoveAllListeners();
            heightSlider.onValueChanged.AddListener(OnManualYOffsetChanged);
        }

        Debug.Log($"Instantiated attic at localPos {atticInstance.transform.localPosition}");
    }

    // --------------------- Attick Skeleton manual position ---------------------
    // --------- Manual Y offset change ---------
    public void OnManualYOffsetChanged(float newOffset)
    {
        manualYOffset = newOffset;
        Debug.Log($"---**--- Slider Fired! manualYOffset = {manualYOffset}");
        if(atticInstance != null)
        {
            UpdateAtticPosition();
            Debug.Log($"------ Moved attic to localPos {atticInstance.transform.localPosition}");
        }
    }
    // --------- Update position ---------
    private void UpdateAtticPosition()
    {
        var cam = xrOrigin.Camera.transform;
        var forward = cam.forward;
        forward.y = 0f;
        atticInstance.transform.localPosition = forward * 1.5f + Vector3.up * (0.5f + manualYOffset);
        atticInstance.transform.localRotation = Quaternion.LookRotation(-forward);
    }

    // --------------------- Return to main page ---------------------
    public void Return()
    {
        SceneManager.LoadScene("MainPage");
    }

    
}

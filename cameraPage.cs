﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using Newtonsoft.Json;

public class CameraPage : MonoBehaviour
{
    [Header("UI Controls")]
    public Slider heightSlider;

    // --------- ↓ Backend link for the 3d models ↓ ---------
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
    // --------- ↑ Backend link for the 3d models ↑ ---------

    [SerializeField] ARSession m_Session;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] ARAnchorManager anchorManager;
    [SerializeField] ARPlaneManager planeManager;
    [Tooltip("Adjust this if your model pivot isn't at the base.")]
    public float verticalOffset = 0f;

    [Header("Manual Y-Offset (meters)")]
    [Tooltip("0 = ground. Slide up/down if it floats.")]
    public float manualYOffset = 0f;

    [System.Serializable]
    public class GPSPoint
    {
        [Header("Location of the object")]
        public ModelInfo info;

        [HideInInspector] public double latitude;
        [HideInInspector] public double longitude;

        [HideInInspector] public GameObject downloadedPrefab;
        [HideInInspector] public bool placed;
        [HideInInspector] public GameObject instance;
        [HideInInspector] public float baseY;

        [HideInInspector] public bool isDownloading;
    }

    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private bool isSwiping = false;
    public TMP_Text debugTxt;
    public TMP_Text calibrateTxt;

    public bool gps_ok = false;

    GPSLoc startLoc = new GPSLoc();
    GPSLoc currLoc = new GPSLoc();

    private double referenceLat;
    private double referenceLon;
    private double referenceAltitude;

    [Tooltip("One entry per house: Gps coordonates + model")]
    public List<GPSPoint> points = new List<GPSPoint>();

    // ----------------------------- Start function -----------------------------
    IEnumerator Start()
    {
        // Keep the screen on as long as this scene is running
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // --- AR availability / install checks ---
        if ((ARSession.state == ARSessionState.None) ||
            (ARSession.state == ARSessionState.CheckingAvailability))
        {
            yield return ARSession.CheckAvailability();
        }

        if (ARSession.state == ARSessionState.Unsupported)
        {
            debugTxt.text = "Votre appareil n'est pas compatible avec la réalité augmentée.";
            yield break;
        }
        else
        {
            m_Session.enabled = true;
        }

        if (ARSession.state == ARSessionState.NeedsInstall)
        {
            debugTxt.text = "Vous devez installer le module de réalité augmentée depuis le Play Store.";
            yield break;
        }

        // --- Location permission & start ---
        if (!Input.location.isEnabledByUser)
        {
            debugTxt.text = "Votre appareil ne permet pas l'accès à votre localisation.";
            yield break;
        }
        Input.location.Start();

        // Wait for location service to initialize
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }
        if (maxWait < 1)
        {
            debugTxt.text += "\nLe temps d'attente est passé.";
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            debugTxt.text += "\nImpossible de trouver la localisation de l'appareil.";
            yield break;
        }
        else
        {
            gps_ok = true;
            referenceLat = Input.location.lastData.latitude;
            referenceLon = Input.location.lastData.longitude;
            referenceAltitude = Input.location.lastData.altitude;

            debugTxt.text =
                $"Localisation OK:\nLat: {referenceLat:F6}\n" +
                $"Lon: {referenceLon:F6}\n" +
                $"Alt: {referenceAltitude:F1} m";
        }

        // --- Manual Y-offset slider listener ---
        if (heightSlider != null)
            heightSlider.onValueChanged.AddListener(OnManualYOffsetChanged);

        // --- Compute geo‐origin for relative positioning ---
        if (calibrationPoints != null && calibrationPoints.Count > 0)
        {
            _originLat = calibrationPoints.Average(p => p.latitude);
            _originLon = calibrationPoints.Average(p => p.longitude);
            debugTxt.text += $"\nUsing {calibrationPoints.Count} manual calibrations";
        }
        else
        {
            _originLat = referenceLat;
            _originLon = referenceLon;
            debugTxt.text += "\nUsing device GPS origin";
        }

        // --- Kick off your FetchAndCache pipeline ---
        StartCoroutine(FetchAndCacheModels());
    }

    // ----------------------------- Fetch & populate only -----------------------------
    IEnumerator FetchAndCacheModels()
    {
        double userLat = referenceLat;
        double userLon = referenceLon;

        // -- Fetch the JSON list --
        var req = UnityWebRequest.Get(apiUrl);
        req.SetRequestHeader("Authorization",
            "Bearer " + PlayerPrefs.GetString("access_token"));
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to fetch model list: {req.error}");
            yield break;
        }

        // -- Parse into ModelInfoList --
        string wrapped = "{\"results\":" + req.downloadHandler.text + "}";
        var list = JsonUtility.FromJson<ModelInfoList>(wrapped);

        // -- Sort by distance (km) from the user’s starting GPS location --
        var sortedResults = list.results
            .OrderBy(info => Distance(
                                 userLat,
                                 userLon,
                                 info.latitude,
                                 info.longitude,
                                 'K'))
            .ToList();

        points.Clear();

        // -- Populate points WITHOUT starting any downloads --
        foreach (var info in sortedResults)
        {
            var pt = new GPSPoint
            {
                info = info,
                latitude = info.latitude,
                longitude = info.longitude,
                placed = false,
                downloadedPrefab = null,
                instance = null,
                baseY = 0f
            };
            points.Add(pt);
        }
    }

    // ----------------------------- Lazy‐download & place -----------------------------
    IEnumerator DownloadAndPlacePrefab(GPSPoint pt)
    {
        debugTxt.text = $"Downloading {pt.info.name}…";

        // -- Download the AssetBundle asynchronously --
        var bundleReq = UnityWebRequestAssetBundle.GetAssetBundle(pt.info.file);
        yield return bundleReq.SendWebRequest();
        if (bundleReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Download failed: {bundleReq.error}");
            pt.isDownloading = false;
            yield break;
        }

        // -- Get the AssetBundle reference --
        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(bundleReq);
        if (bundle == null)
        {
            Debug.LogError($"Failed to load AssetBundle from {pt.info.file}");
            pt.isDownloading = false;
            yield break;
        }

        // -- Kick off an async load of the prefab inside the bundle --
        string assetName = bundle.GetAllAssetNames()[0];
        var loadRequest = bundle.LoadAssetAsync<GameObject>(assetName);

        // -- Wait until LoadAssetAsync is done; this yields a few frames so the camera won't freeze --
        yield return loadRequest;

        // -- Extract the loaded prefab --
        GameObject prefab = loadRequest.asset as GameObject;
        if (prefab == null)
        {
            Debug.LogError($"Failed to load prefab '{assetName}' from bundle");
            bundle.Unload(false);
            pt.isDownloading = false;
            yield break;
        }

        // -- Cache the prefab on the GPSPoint so we don’t re-download if we re-calibrate --
        pt.downloadedPrefab = prefab;
        debugTxt.text = $"{pt.info.name} cached";

        // -- Compute camera‐relative “groundY” and rotate north/east offset --
        Vector3 cameraWorldPos = Camera.main.transform.position;
        float cameraYawDeg = Camera.main.transform.eulerAngles.y;

        float northMeters = (float)((pt.latitude - _originLat) * 110540f);
        float eastMeters = (float)((pt.longitude - _originLon) * 111320f *
                                    Mathf.Cos((float)(_originLat * Mathf.Deg2Rad)));
        Vector3 geoOffset = new Vector3(eastMeters, 0f, northMeters);
        Vector3 rotatedOffset = Quaternion.Euler(0f, cameraYawDeg, 0f) * geoOffset;

        // -- Determine “groundY” once (camera Y + verticalOffset), then add manualYOffset --
        float groundY = cameraWorldPos.y + verticalOffset;
        Vector3 spawnPos = new Vector3(
            cameraWorldPos.x + rotatedOffset.x,
            groundY + manualYOffset,
            cameraWorldPos.z + rotatedOffset.z
        );

        // -- Instantiate the prefab at spawnPos, then record baseY --
        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
        pt.instance = go;
        pt.baseY = groundY;
        pt.placed = true;

        debugTxt.text = $"Placed {pt.info.name} at Y={spawnPos.y:F2}";

        // -- Unload the raw AssetBundle memory but keep the asset loaded --
        bundle.Unload(false);

        pt.isDownloading = false;
    }


    // ----------------------------- return to main page -----------------------------
    public void Return()
    {
        SceneManager.LoadScene("MainPage");
    }

    // ----------------------------- Update function (lazy loader) -----------------------------
    void Update()
    {
        DetectSwipe();
        UpdateDebugDisplay();

        if (!gps_ok)
            return;

        // --- Current GPS reading ---
        var curr = Input.location.lastData;
        double currLat = curr.latitude;
        double currLon = curr.longitude;

        // --- Rebase origin if user walked >500 m from previous (_originLat,_originLon) ---
        double distFromOriginKm = Distance(_originLat, _originLon, currLat, currLon, 'K');
        if (distFromOriginKm * 1000.0 > 500.0f)
        {
            _originLat = currLat;
            _originLon = currLon;
            Debug.Log($"Rebased origin: {_originLat:F6}, {_originLon:F6}");
        }

        // --- Get AR Camera's world position & yaw ---
        Vector3 cameraWorldPos = Camera.main.transform.position;
        float cameraYawDeg = Camera.main.transform.eulerAngles.y;

        foreach (var pt in points)
        {
            // -- Skip any already placed --
            if (pt.placed || pt.isDownloading)
                continue;

            // -- Check distance (meters) from current location to this point’s GPS --
            double distKm = Distance(currLat, currLon, pt.latitude, pt.longitude, 'K');
            float distM = (float)(distKm * 1000.0);
            if (distM > 50f)
                continue; 

            // -- If we get here, user is within 50 m AND pt.placed == false --
            if (pt.downloadedPrefab == null)
            {
                pt.isDownloading = true;
                StartCoroutine(DownloadAndPlacePrefab(pt));
            }
            break;
        }
    }

    // ----------------------------- Offset change function -----------------------------
    public void OnManualYOffsetChanged(float newOffset)
    {
        Debug.Log($"------ Slider fired! manualYOffset = {manualYOffset}");
        manualYOffset = newOffset;

        foreach (var pt in points)
        {
            if (!pt.placed || pt.instance == null)
                continue;

            Vector3 p = pt.instance.transform.position;
            p.y = pt.baseY + manualYOffset;
            pt.instance.transform.position = p;
            Debug.Log($"------ Moved instance to Y = {p.y:F2}");
        }
    }

    // ----------------------------- CalibrateGround function -----------------------------
    public void CalibrateGround()
    {
        referenceAltitude = Input.location.lastData.altitude;
        calibrateTxt.text = $"Ground calibrated at {referenceAltitude:F1} m";

        foreach(var pt in points)
        {
            if(pt.instance != null)
            {
                Destroy(pt.instance);
                pt.instance = null;
            }

            pt.downloadedPrefab = null;
            pt.placed = false;
            pt.isDownloading = false;
            //pt.baseY = 0f;
        }
    }

    // ----------------------------- UpdateDebugDisplay function -----------------------------
    private void UpdateDebugDisplay()
    {
        if (!gps_ok || points.Count == 0)
            return;

        double currLat = Input.location.lastData.latitude;
        double currLon = Input.location.lastData.longitude;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("GPS: Working");
        foreach (var pt in points)
        {
            double d = Distance(currLat, currLon, pt.latitude, pt.longitude, 'K') * 1000.0;
            sb.AppendFormat(
                "{0:F6},{1:F6}: {2:F1} m{3}\n",
                pt.latitude,
                pt.longitude,
                d,
                pt.placed ? " (placed)" : ""
            );
        }

        debugTxt.text = sb.ToString();
    }

    // ----------------------------- Detect the swipe of the user on the camera scene ---------------
    void DetectSwipe()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                startTouchPosition = touch.position;
                isSwiping = true;
            }
            else if (touch.phase == TouchPhase.Moved && isSwiping)
            {
                endTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended && isSwiping)
            {
                endTouchPosition = touch.position;
                CheckSwipeDirection();
                isSwiping = false;
            }
        }
    }

    void CheckSwipeDirection()
    {
        float swipeDistanceX = endTouchPosition.x - startTouchPosition.x;
        Debug.Log("Swipe Distance X: " + swipeDistanceX);

        if (Mathf.Abs(swipeDistanceX) > 150f)
        {
            if (swipeDistanceX > 0)
            {
                SceneManager.LoadScene("MainPage");
            }
            else
            {
                Debug.Log("Swiped RIGHT — no action");
            }
        }
    }

    // ---------------- GPS Localisation helper ----------------
    private Vector3 GPSLocationToUnityPosition(double targetLat, double targetLon, double targetAlt)
    {
        double latRad = referenceLat * Mathf.Deg2Rad;
        float x = (float)((targetLon - referenceLon) * 111320 * Math.Cos(latRad));
        float z = (float)((targetLat - referenceLat) * 110540);
        return new Vector3(x, 0, z);
    }

    public void StopGps()
    {
        Input.location.Stop();
    }

    public void StoreCurrentGPS()
    {
        startLoc = new GPSLoc(currLoc.lon, currLoc.lat);
    }

    private double Distance(double lat1, double lon1, double lat2, double lon2, char unit)
    {
        if ((lat1 == lat2) && (lon1 == lon2))
        {
            return 0;
        }
        else
        {
            double theta = lon1 - lon2;
            double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) +
                          Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) *
                          Math.Cos(deg2rad(theta));
            dist = Math.Acos(dist);
            dist = rad2deg(dist);
            dist = dist * 60 * 1.1515;
            if (unit == 'K')
            {
                dist = dist * 1.609344;
            }
            else if (unit == 'N')
            {
                dist = dist * 0.8684;
            }
            return dist;
        }
    }

    // Convert decimal degrees to radians
    private double deg2rad(double deg)
    {
        return (deg * Math.PI / 180.0);
    }

    private double rad2deg(double rad)
    {
        return (rad / Math.PI * 180.0);
    }
}

public class GPSLoc
{
    public float lon;
    public float lat;

    public GPSLoc()
    {
        lon = 0;
        lat = 0;
    }

    public GPSLoc(float lon, float lat)
    {
        this.lon = lon;
        this.lat = lat;
    }

    public string getLocData()
    {
        return "Lat: " + lat + " \nLon: " + lon;
    }
}

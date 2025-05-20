using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Linq;


public class CameraPage : MonoBehaviour
{
    [Header("UI Controls")]
    public Slider heightSlider;

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
        public double latitude;
        public double longitude;

        [Header("Selected house")]
        public GameObject housePrefab;

        [HideInInspector] public bool placed = false;

        [HideInInspector] public GameObject instance;
        [HideInInspector] public float baseY;
    }

    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private bool isSwiping = false;
    public TMP_Text debugTxt;
    public TMP_Text calibrateTxt;

    // ------ Object position variables ------
    private GameObject spawnedObject;

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
        // Keep the screen on as long as the camera scene is running
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // AR warnings
        if ((ARSession.state == ARSessionState.None) ||
            (ARSession.state == ARSessionState.CheckingAvailability))
        {
            yield return ARSession.CheckAvailability();
        }

        if (ARSession.state == ARSessionState.Unsupported)
        {
            Debug.LogWarning("Your device is not supported for the AR!!!!!");
            debugTxt.text = "Votre appareil n'est pas compatible avec la réalité augmentée.";
        }
        else
        {
            // Start the AR session
            m_Session.enabled = true;
        }

        if (ARSession.state == ARSessionState.NeedsInstall)
        {
            Debug.Log("You need to install the AR in order for it to work");
            debugTxt.text = "\nVous devez installer le module de réalité augmentée depuis le Play Store.";
        }

        // ---------------- Location messages ----------------
        // Check if the user has the location on
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("Location not enabled on device or app does not have permission to access location");
            debugTxt.text = "Votre appareil ne permet pas l'accès à votre localisation.";
        }

        // Start location service
        Input.location.Start();

        // Waits until the location service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // If the service didn't initialize in 20 seconds this cancels location service use.
        if (maxWait < 1)
        {
            Debug.Log("Time out");
            debugTxt.text += "\nLe temps d'attente est passé.";

            yield break;
        }

        // If connection faield this will cancel the service use
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Unable to determine device location");
            debugTxt.text += "\nImpossible de trouver la localisation de l'appareil.";

            yield break;
        }
        else
        {
            // If the connection succeeded, it will retrive the current location
            Debug.Log("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);

            debugTxt.text
               = "\nLocalisation: \nLat: " + Input.location.lastData.latitude
                + " \nLon: " + Input.location.lastData.longitude
                + " \nAlt: " + Input.location.lastData.altitude
                + " \nH_Acc: " + Input.location.lastData.horizontalAccuracy
                + " \nTemps: " + Input.location.lastData.timestamp;

            gps_ok = true;

            referenceLat = Input.location.lastData.latitude;
            referenceLon = Input.location.lastData.longitude;
            referenceAltitude = Input.location.lastData.altitude;
        }

        if (heightSlider != null)
            heightSlider.onValueChanged.AddListener(OnManualYOffsetChanged);



    }
    // ----------------------------- end of Start function -----------------------------


    // ----------------------------- return to main page -----------------------------
    public void Return()
    {
        SceneManager.LoadScene("MainPage");
    }
    // ----------------------------- end of return to main page -----------------------------


    // ----------------------------- Update function -----------------------------
    void Update()
    {
        DetectSwipe();
        UpdateDebugDisplay();

        if (!gps_ok) return;

        if (heightSlider != null)
        {
            Debug.Log($"[DEBUG SLIDER] slider.value={heightSlider.value:F2}");
        }

        foreach (var pt in points)
        {
            if (pt.placed) continue;

            // 1) Distance check
            double currLat = Input.location.lastData.latitude;
            double currLon = Input.location.lastData.longitude;
            double dLat = pt.latitude - currLat;
            double dLon = pt.longitude - currLon;
            float north = (float)(dLat * 110540f);
            float east = (float)(dLon * 111320f * Mathf.Cos((float)(currLat * Mathf.Deg2Rad)));
            if (Mathf.Sqrt(north * north + east * east) > 50f) continue;

            // 2) Raycast for the plane
            var hits = new List<ARRaycastHit>();
            var center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            if (!raycastManager.Raycast(center, hits, TrackableType.PlaneWithinPolygon))
                continue;
            Pose hitPose = hits[0].pose;

            // 3) GPS → world offset
            Vector3 geoOffset = new Vector3(east, 0, north);
            float heading = Camera.main.transform.eulerAngles.y;
            Vector3 worldOffset = Quaternion.Euler(0, heading, 0) * geoOffset;

            // 4) Determine ground Y via the plane + your pivot tweak
            float groundY = hitPose.position.y + verticalOffset;

            // 5) Compute spawnPos = (X,Z) + groundY + current manualYOffset
            Vector3 spawnPos = new Vector3(
                hitPose.position.x + worldOffset.x,
                groundY + manualYOffset,
                hitPose.position.z + worldOffset.z
            );

            // 6) Spawn the prefab **without parenting** (so you can move it later)
            GameObject go = Instantiate(pt.housePrefab, spawnPos, Quaternion.identity);

            // 7) Record for sliding
            pt.instance = go;
            pt.baseY = groundY;
            pt.placed = true;

            debugTxt.text = $"Placed at Y={groundY:F2}";

            // 8) Stop after one per frame
            break;
        }
    }


    // ----------------------------- end of Update function -----------------------------

    // ----------------------------- Offset change function -----------------------------
    public void OnManualYOffsetChanged(float newOffset)
    {
        manualYOffset = newOffset;
        Debug.Log($"Slider fired! manualYOffset = {manualYOffset}");

        foreach (var pt in points)
        {
            if (!pt.placed || pt.instance == null) continue;
            Vector3 p = pt.instance.transform.position;
            p.y = pt.baseY + manualYOffset;
            pt.instance.transform.position = p;
            Debug.Log($"Moved instance to Y = {p.y:F2}");
        }
    }
    // ----------------------------- end of Offset change function -----------------------------


    // ----------------------------- CalibrateGround function -----------------------------
    public void CalibrateGround()
    {
        referenceAltitude = Input.location.lastData.altitude;
        calibrateTxt.text = $"Ground calibrated at {referenceAltitude:F1} m";

    }

    // ----------------------------- UpdateDebugDisplay function -----------------------------
    private void UpdateDebugDisplay()
    {
        if (!gps_ok || points.Count == 0) return;

        double currLat = Input.location.lastData.latitude;
        double currLon = Input.location.lastData.longitude;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("GPS: Working");
        foreach(var pt in points)
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


    // ---------------- Detect the swipe of the user on the camera scene ---------------
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
    // ---------------- End of swipe dection ----------------



    // ---------------- GPS Localisation ----------------
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
        if((lat1 == lat2) && (lon1 == lon2))
        {
            return 0;
        }
        else
        {
            double theta = lon1 - lon2;
            double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) + Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * Math.Cos(deg2rad(theta));
            dist = Math.Acos(dist);
            dist = rad2deg(dist);
            dist = dist * 60 * 1.1515;
            if(unit == 'K')
            {
                dist = dist * 1.609344;
            }
            else if(unit == 'N')
            {
                dist = dist * 0.8684;
            }
            return (dist);
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
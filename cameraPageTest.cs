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


public class CameraPageTest : MonoBehaviour
{
    [SerializeField] ARSession m_Session;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] ARAnchorManager anchorManager;
    [SerializeField] ARPlaneManager planeManager;

    [System.Serializable]
    public class GPSPoint
    {
        [Header("Location of the object")]
        public double latitude;
        public double longitude;

        [Header("Selected house")]
        public GameObject housePrefab;

        [HideInInspector] public bool placed = false;
    }

    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private bool isSwiping = false;
    public TMP_Text debugTxt;
    public TMP_Text calibrateTxt;
    //private bool objectPlaced = false; -- remove latter

    // ------ Object position variables ------
    //public GameObject objectPrefab; -- Remove latter
    private GameObject spawnedObject;

    public bool gps_ok = false;

    GPSLoc startLoc = new GPSLoc();
    GPSLoc currLoc = new GPSLoc();

    private double referenceLat;
    private double referenceLon;
    private double referenceAltitude;

    // 47.732033156335284, 7.2862330808561495 -- Must change it inside unity as well !!!
    //public double parkingLatitude = 47.732033156335284; -- remove latter
    //public double parkingLongitude = 7.2862330808561495; -- remove latter

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

        if (planeManager.trackables.count == 0)
            return;

        if (!gps_ok)
            return;

        foreach (var pt in points)
        {
            if (pt.placed)
                continue;

            double currLat = Input.location.lastData.latitude;
            double currLon = Input.location.lastData.longitude;
            double dLat = pt.latitude - currLat;
            double dLon = pt.longitude - currLon;
            double latRad = currLat * Mathf.Deg2Rad;
            float north = (float)(dLat * 110540f);
            float east = (float)(dLon * 111320f * Math.Cos(latRad));
            float dist = Mathf.Sqrt(north * north + east * east);
            if (dist > 50f)
                continue;

            // Raycast to detect plane
            var hits = new List<ARRaycastHit>();
            var center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            if (!raycastManager.Raycast(center, hits, TrackableType.PlaneWithinPolygon))
                continue;

            Pose hitPose = hits[0].pose;

            // Offset by GPS
            Vector3 geoOffset = new Vector3(east, 0f, north);
            Quaternion yaw = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
            Vector3 worldOffset = yaw * geoOffset;

            Vector3 finalPosition = hitPose.position + worldOffset;
            finalPosition.y = hitPose.position.y;  // Lock to plane height

            // Attach to plane
            ARPlane plane = planeManager.GetPlane(hits[0].trackableId);
            ARAnchor anchor = anchorManager.AttachAnchor(plane, new Pose(finalPosition, Quaternion.identity));

            if (anchor == null)
            {
                Debug.LogWarning("Anchor attach failed, fallback instantiate");
                Instantiate(pt.housePrefab, finalPosition, Quaternion.identity);
            }
            else
            {
                Instantiate(pt.housePrefab, anchor.transform);
            }

            pt.placed = true;
            debugTxt.text = $"Placed: {pt.latitude:F6}, {pt.longitude:F6}";

            if (points.All(x => x.placed))
            {
                raycastManager.enabled = false;
                planeManager.enabled = false;
                enabled = false;
            }

            break;
        }
    }


    // ----------------------------- end of Update function -----------------------------

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
        if ((lat1 == lat2) && (lon1 == lon2))
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
            if (unit == 'K')
            {
                dist = dist * 1.609344;
            }
            else if (unit == 'N')
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

public class GPSLocTest
{
    public float lon;
    public float lat;

    public GPSLocTest()
    {
        lon = 0;
        lat = 0;
    }

    public GPSLocTest(float lon, float lat)
    {
        this.lon = lon;
        this.lat = lat;
    }

    public string getLocData()
    {
        return "Lat: " + lat + " \nLon: " + lon;
    }
}

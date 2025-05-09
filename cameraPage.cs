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



public class CameraPage : MonoBehaviour
{
    [SerializeField] ARSession m_Session;
    [SerializeField] private ARRaycastManager raycastManager;


    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private bool isSwiping = false;
    public TMP_Text debugTxt;
    public bool planeTouched;
    private bool objectPlaced = false;

    // ------ Object position variables ------
    public GameObject objectPrefab;
    private GameObject spawnedObject;

    public bool gps_ok = false;

    GPSLoc startLoc = new GPSLoc();
    GPSLoc currLoc = new GPSLoc();

    private double referenceLat;
    private double referenceLon;
    private double referenceAltitude;


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
        if(Input.location.status == LocationServiceStatus.Failed)
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


            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

            StartCoroutine(TryPlaceObjectWhenPlaneFound());

        }

    }

    // Check the XR Status
    public void CheckXRStatus()
    {
        if(UnityEngine.XR.XRSettings.enabled)
        {
            Debug.Log("------ XR is active. ------");
        }
        else
        {
            Debug.Log("------ XR is not available. ------");
        }
    }

    // return to main page
    public void Return()
    {
        SceneManager.LoadScene("MainPage");
    }

    void Update()
    {
        DetectSwipe();

        string display = "";

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                display += $"Position: {touch.position}\n";
            }
        }

        if (gps_ok)
        {
            display += "GPS: Working\n";

            currLoc.lat = Input.location.lastData.latitude;
            currLoc.lon = Input.location.lastData.longitude;

            // ▼▼ Coordonates that need to change ▼▼
            // 47.73210898241744, 7.286019618830404
            double distanceBetween = Distance(currLoc.lat, currLoc.lon, 47.73210898241744, 7.286019618830404, 'K');

            display += $"Distance: {distanceBetween}\n";

            if (distanceBetween <= 0.01)
            {
                display += "L'objet est dans le coin";
            }
        }

        debugTxt.text = display;
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

        if (Mathf.Abs(swipeDistanceX) > 100f)
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
        float scale = 111320f;

        float x = (float)((targetLon - referenceLon) * scale);
        float y = (float)(targetAlt - referenceAltitude);
        float z = (float)((targetLat - referenceLat) * scale);

        return new Vector3(x, y, z);
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

    private IEnumerator TryPlaceObjectWhenPlaneFound()
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        while (!objectPlaced)
        {

            debugTxt.text = "Recherche d’un plan AR pour placer l’objet...";

            if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
            {
                currLoc.lat = Input.location.lastData.latitude;
                currLoc.lon = Input.location.lastData.longitude;

                // 47.73210898241744, 7.286019618830404
                double distanceToTarget = Distance(currLoc.lat, currLoc.lon, 47.73210898241744, 7.286019618830404, 'K');

                if(distanceToTarget <= 0.02)
                {
                    Pose hitPose = hits[0].pose;
                    spawnedObject = Instantiate(objectPrefab, hitPose.position, hitPose.rotation);
                    objectPlaced = true;
                    debugTxt.text = "Bâtiment chargé à l’emplacement du parking.";
                    Debug.Log("Model placed at parking lot.");
                }
                else
                {
                    debugTxt.text = "Déplacez-vous vers le parking pour voir le bâtiment.";
                }
            }

            yield return new WaitForSeconds(0.5f);

        }

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
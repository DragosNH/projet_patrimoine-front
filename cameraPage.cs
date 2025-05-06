using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using System.Runtime.CompilerServices;


public class cameraPage : MonoBehaviour
{
    [SerializeField] ARSession m_Session;

    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;
    private bool isSwiping = false;
    public TMP_Text debugTxt;
    public bool planeTouched;

    // ------ Object position variables ------
    public GameObject gameObject;
    bool createdGameObject = false;

    private char unit = 'K';
    public bool gps_ok = false;
    float PI = Mathf.PI;

    GPSLoc startLoc = new GPSLoc();
    GPSLoc currLoc = new GPSLoc();

    bool measureDistance = false;

    private double referenceLat;
    private double referenceLon;

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
        }
        else
        {
            // Start the AR session
            m_Session.enabled = true;
        }

        if (ARSession.state == ARSessionState.NeedsInstall)
        {
            Debug.Log("You need to install the AR in order for it to work");
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

            // ▼ Acttual coordonates are not accurate ▼
            Vector3 position = GPSLocationToUnityPosition(47.73200138526153, 7.286370112966); // <-- GPS coordonates
            gameObject = Instantiate(gameObject, position, Quaternion.identity);
            createdGameObject = true;
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
        // Detect swipe in order to return to the privious screen
        DetectSwipe();

        if (Input.touchCount > 0)
        {
            for(int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                debugTxt.text = $"Position: {touch.position}";
            }
        }

        // Get licalisation and display AR Object to the specific location
        if (gps_ok)
        {
            debugTxt.text = "GPS: Working";
        }

        currLoc.lat = Input.location.lastData.latitude;
        currLoc.lon = Input.location.lastData.longitude;

        // 47.73200138526153, 7.286370112966
        double distanceBetween = Distance((double)currLoc.lat, (double)47.73200138526153, (double)startLoc.lat, (double)7.286370112966, 'K'); // <-- For the moment the coordonates are not accurate

        debugTxt.text += "\nDisatance: " + distanceBetween;

        if(distanceBetween <= 0.01)
        {
            debugTxt.text += "l'Objet est dans le coin";
        }

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

    private Vector3 GPSLocationToUnityPosition(double targetLat, double targetLon)
    {
        float scale = 111320f;

        float x = (float)((targetLon - referenceLon) * scale);
        float z = (float)((targetLat - referenceLat) * scale);

        return new Vector3(x, 0f, z);
    }

    public void StopGps()
    {
        Input.location.Stop();
    }

    public void StoreCurrentGPS()
    {
        startLoc = new GPSLoc(currLoc.lon, currLoc.lat);
        measureDistance = true;
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
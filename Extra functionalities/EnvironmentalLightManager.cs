using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(Light))]
public class EnvironmentalLightManager : MonoBehaviour
{
    [SerializeField]

    Light sceneLight;
    ARCameraManager cameraManager;

    void Awake()
    {
        sceneLight = GetComponent<Light>();
    }

    void OnEnable()
    {
        if (cameraManager != null) 
        {
            cameraManager.frameReceived += FrameChanged;
        }
    }

    void OnDisable()
    {
        if (cameraManager != null)
            cameraManager.frameReceived -= FrameChanged;
    }

    private void FrameChanged(ARCameraFrameEventArgs args)
    {
        if (args.lightEstimation.averageBrightness.HasValue)
        {
            sceneLight.intensity = args.lightEstimation.averageBrightness.Value;
        }
    }
}
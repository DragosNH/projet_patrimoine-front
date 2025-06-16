using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(AROcclusionManager))]
public class DepthOcclusionManager : MonoBehaviour
{
    AROcclusionManager occlusionManager;

    // WARNING! this code is inactive inside unity because the depth is not working so well on big objects like houses, maybe because the code is not strong enough or the Unity is not there yet, but this code is a good fondation for the future!


    void Awake()
    {
        Debug.Log("[DepthOcclusionManager] Awake");
        occlusionManager = GetComponent<AROcclusionManager>();
    }

    void Start()
    {
        Debug.Log("[DepthOcclusionManager] Start");
    }

    void OnEnable()
    {
        Debug.Log("[DepthOcclusionManager] OnEnable");
        // --- Check for environment-depth support ---
        if (occlusionManager.descriptor.environmentDepthImageSupported
                == Supported.Supported)
        {
            occlusionManager.requestedEnvironmentDepthMode
                = EnvironmentDepthMode.Best;
            Debug.Log("[Occlusion] Environment depth enabled (Best).");
        }
        else
        {
            Debug.LogWarning(
              "[Occlusion] Environment depth NOT supported on this device.");
        }

        // --- Check for temporal-smoothing support, then request it ---
        if (occlusionManager.descriptor.environmentDepthTemporalSmoothingSupported
                == Supported.Supported)
        {
            occlusionManager.environmentDepthTemporalSmoothingRequested = true;
            Debug.Log("[Occlusion] Temporal smoothing enabled.");
        }
        else
        {
            Debug.Log("[Occlusion] Temporal smoothing NOT supported.");
        }
    }
}

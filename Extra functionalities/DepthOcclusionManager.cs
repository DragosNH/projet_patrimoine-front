using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(AROcclusionManager))]
public class DepthOcclusionManager : MonoBehaviour
{
    AROcclusionManager occlusionManager;

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

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(AROcclusionManager))]
public class DepthOcclusionManager : MonoBehaviour
{
    AROcclusionManager occlusionManager;

    void Awake()
    {
        occlusionManager = GetComponent<AROcclusionManager>();
    }

    void OnEnable()
    {
        // 1) Check for environment-depth support
        if (occlusionManager.descriptor.environmentDepthImageSupported
                == Supported.Supported)
        {
            // quality/speed trade-off: Fastest, Medium or Best
            occlusionManager.requestedEnvironmentDepthMode
                = EnvironmentDepthMode.Best;
            Debug.Log("[Occlusion] Environment depth enabled (Best).");
        }
        else
        {
            Debug.LogWarning(
              "[Occlusion] Environment depth NOT supported on this device.");
        }

        // 2) Check for temporal-smoothing support, then request it
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

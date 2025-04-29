using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ForceARCore : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(CheckARSupport());
    }

    System.Collections.IEnumerator CheckARSupport()
    {
        yield return ARSession.CheckAvailability();

        if (ARSession.state == ARSessionState.Unsupported)
        {
            Debug.LogWarning("ARCore not officially supported. Trying to force start anyway...");
        }
        else if (ARSession.state == ARSessionState.NeedsInstall)
        {
            yield return ARSession.Install();
        }

        if (ARSession.state == ARSessionState.Ready)
        {
            Debug.Log("Starting AR Session manually.");
            var arSession = Object.FindFirstObjectByType<ARSession>();
            if (arSession != null)
            {
                arSession.enabled = true;
            }
        }
    }
}

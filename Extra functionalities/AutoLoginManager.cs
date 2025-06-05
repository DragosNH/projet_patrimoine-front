using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

public class AutoLoginManager : MonoBehaviour
{
    private string refreshUrl = NetworkConfig.ServerIP + "/api/token/refresh/";

    void Start()
    {

    }

    IEnumerator CheckLoginStatus()
    {
        string refreshToken = PlayerPrefs.GetString("refresh_token", "");

        // If there is not a refresh token redirect the user to the loginpage 
        if (string.IsNullOrEmpty(refreshToken))
        {
            SceneManager.LoadScene("LoginPage");
            yield break;
        }

    }
}
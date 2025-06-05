using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

public class AutoLoginManager : MonoBehaviour
{
    private string refreshUrl = NetworkConfig.ServerIP + "/api/token/refresh/";

    void Start()
    {
        StartCoroutine(CheckLoginStatus());
        Debug.Log("-----------RunningAutoLoginManager script is running.");
    }

    IEnumerator CheckLoginStatus()
    {
        string refreshToken = PlayerPrefs.GetString("refresh_token", "");
        Debug.Log("AutoLoginManager found refresh_token: " + refreshToken);

        // No refresh token saved → redirect to login
        if (string.IsNullOrEmpty(refreshToken))
        {
            SceneManager.LoadScene("Login");
            yield break;
        }

        // Prepare request to refresh access token
        RefreshData data = new RefreshData { refresh = refreshToken };
        string jsonData = JsonUtility.ToJson(data);

        UnityWebRequest request = new UnityWebRequest(refreshUrl, "POST");
        byte[] body = new System.Text.UTF8Encoding().GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            TokenResponse newTokens = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
            PlayerPrefs.SetString("access_token", newTokens.access);
            PlayerPrefs.Save();

            Debug.Log("Access token refreshed. Redirecting to MainPage...");
            SceneManager.LoadScene("MainPage");
        }
        else
        {
            Debug.LogWarning("Token refresh failed. Redirecting to LoginPage...");
            SceneManager.LoadScene("Login");
        }
    }

    [System.Serializable]
    public class RefreshData
    {
        public string refresh;
    }

    [System.Serializable]
    public class TokenResponse
    {
        public string access;
        public string refresh;
    }
}

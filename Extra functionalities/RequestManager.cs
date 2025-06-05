using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class RequestManager : MonoBehaviour
{

    private static string refreshUrl = NetworkConfig.ServerIP + "/api/token/refresh/";

    public static IEnumerator SendAuthorizedRequest(UnityWebRequest request, Action<UnityWebRequest> callback)
    {
        string accessToken = PlayerPrefs.GetString("access_token", "");

        request.SetRequestHeader("Authorization", "Bearer " + accessToken);
        yield return request.SendWebRequest();

        // If the token expired, refresh
        if (request.responseCode == 401)
        {
            Debug.LogWarning("Access token expired. Trying refresh...");

            bool refreshSuccess = false;
            yield return RefreshAccessToken(success => refreshSuccess = success);

            if (refreshSuccess)
            {
                string newAccess = PlayerPrefs.GetString("access_token", "");
                request.SetRequestHeader("Authorization", "Bearer " + newAccess);

                yield return request.SendWebRequest();
            }
            else
            {
                Debug.LogError("Token refresh failed. You should redirect to login.");
            }
        }

        callback?.Invoke(request);
    }

    private static IEnumerator RefreshAccessToken(Action<bool> callback)
    {
        string refreshToken = PlayerPrefs.GetString("refresh_token", "");
        if (string.IsNullOrEmpty(refreshToken))
        {
            callback(false);
            yield break;
        }

        RefreshData data = new RefreshData { refresh = refreshToken };
        string json = JsonUtility.ToJson(data);

        UnityWebRequest refreshRequest = new UnityWebRequest(refreshUrl, "POST");
        byte[] body = new System.Text.UTF8Encoding().GetBytes(json);
        refreshRequest.uploadHandler = new UploadHandlerRaw(body);
        refreshRequest.downloadHandler = new DownloadHandlerBuffer();
        refreshRequest.SetRequestHeader("Content-Type", "application/json");

        yield return refreshRequest.SendWebRequest();

        if (refreshRequest.result == UnityWebRequest.Result.Success)
        {
            TokenResponse tokens = JsonUtility.FromJson<TokenResponse>(refreshRequest.downloadHandler.text);
            PlayerPrefs.SetString("access_token", tokens.access);
            PlayerPrefs.Save();
            callback(true);
        }
        else
        {
            Debug.LogError("Refresh token failed: " + refreshRequest.downloadHandler.text);
            callback(false);
        }
    }

    [Serializable]
    private class RefreshData
    {
        public string refresh;
    }

    [Serializable]
    private class TokenResponse
    {
        public string access;
        public string refresh;
    }
}

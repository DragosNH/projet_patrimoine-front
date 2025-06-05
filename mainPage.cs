using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;


public class MainPage: MonoBehaviour
{

    public GameObject popupMenu;
    public Toggle themeToggle;
    public PopupFader popupFader;

    // -- Background Image --
    // - Background -
    public Image backgroundImage;
    public Sprite lightBackground;
    public Sprite darkBackground;
    // - Popup background -
    public Image menuPopup;
    public Sprite lightPopup;
    public Sprite darkPopup;

    // -- Buttons --
    public Button[] buttons;
    // - Logo Button -
    public Button logoButton;
    public Sprite lightLogoButton;
    public Sprite darkLogoButton;
    // - Close popup logo button -
    public Button closeLogoButton;
    public Sprite lightCloseLogoButton;
    public Sprite darkCloseLogoButton;
    // - Profile Button -
    public Button myProfileBtn;
    public Sprite lightMyProfileBtn;
    public Sprite darkMyProfileBtn;
    // - Logout Button -
    public Button logoutBtn;
    public Sprite lightLogoutBtn;
    public Sprite darkLogoutBtn;
    // - Map Button -
    public Button mapButton;
    public Sprite lightMapBtn;
    public Sprite darkMapBtn;
    // - Camera Button -
    public Button cameraButton;
    public Sprite lightCameraBtn;
    public Sprite darkCameraBtn;

    // -- Text Color --
    public TextMeshProUGUI[] texts;

    string apiUrl = NetworkConfig.ServerIP + "/api/logout/";


    void Start()
    {
        /// Load saved theme
        ThemeManager.Instance.LoadTheme();
        themeToggle.isOn = ThemeManager.Instance.isDarkMode;
        if (ThemeManager.Instance == null)
            Debug.LogError("ThemeManager is NULL!");

        if (themeToggle == null)
            Debug.LogError("Theme toggle is not asigned!");
        Debug.Log("ThemeManager.Instance is: " + ThemeManager.Instance);

        themeToggle.onValueChanged.AddListener(delegate { ToggleTheme(); });

        UpdateTheme();
    }

    void ToggleTheme()
    {
        ThemeManager.Instance.ToggleTheme();
        UpdateTheme();
    }

    void UpdateTheme()
    {
        bool isDarkMode = ThemeManager.Instance.isDarkMode;

        // - Background -
        backgroundImage.sprite = isDarkMode ? darkBackground : lightBackground;

        // --- Buttons ---
        // - Logo button as burger menu -
        logoButton.GetComponent<Image>().sprite = isDarkMode ? darkLogoButton : lightLogoButton;
        closeLogoButton.GetComponent<Image>().sprite = isDarkMode ? darkCloseLogoButton : lightCloseLogoButton;
        // -- Buttons inside the Popup --
        // - My Profile Button -
        myProfileBtn.GetComponent<Image>().sprite= isDarkMode ? darkMyProfileBtn : lightMyProfileBtn;
        // - Logout Button -
        logoutBtn.GetComponent<Image>().sprite = isDarkMode ? darkLogoutBtn : lightLogoutBtn;
        // -- Page Buttons --
        // - Map Button -
        mapButton.GetComponent<Image>().sprite = isDarkMode ? darkMapBtn : lightMapBtn;
        // - Camera Button -
        cameraButton.GetComponent<Image>().sprite = isDarkMode? darkCameraBtn : lightCameraBtn;


        // - Menu Popup -
        menuPopup.GetComponent<Image>().sprite = isDarkMode ? darkPopup : lightPopup;


        // -- Texts --
        foreach (TextMeshProUGUI txt in texts)
        {
            txt.color = isDarkMode ? Color.white : Color.black;
        }
    }


    // ---- Popup menu: hide and display -----
    public void ShowPopUpMenu()
    {
        popupMenu.SetActive(true);
        popupFader.FadeIn();

    }

    public void HidePopupMenu()
    {
        popupFader.FadeOut();
    }


    // ---- Go to profile page ----
    public void ProfilePage()
    {
        SceneManager.LoadScene("ProfilePage");
    }


    // ---- Go to camera ----
    public void CameraScene()
    {
        SceneManager.LoadScene("ARCameraScene");
    }

    // ---- Go to the map ----
    public void MapScene()
    {
        SceneManager.LoadScene("MapScene");
    }

    // ---- logout button ----
    public void Logout()
    {
        StartCoroutine(LogoutRequest());
    }

    IEnumerator LogoutRequest()
    {
        //string refresh = PlayerPrefs.GetString("refresh_token");

        //UnityWebRequest request = new UnityWebRequest("http://127.0.0.1:8000/api/logout/", "POST");
        //byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{\"refresh\":\"" + refresh + "\"}");
        //request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        //request.downloadHandler = new DownloadHandlerBuffer();
        //request.SetRequestHeader("Content-Type", "application/json");

        //yield return request.SendWebRequest();

        //if (request.result != UnityWebRequest.Result.Success)
        //{
        //    Debug.LogWarning("Logout failed: " + request.error);
        //}
        //else
        //{
        //    Debug.Log("Logged out successfully: " + request.downloadHandler.text);
        //}

        //// Clear tokens
        //PlayerPrefs.DeleteKey("access_token");
        //PlayerPrefs.DeleteKey("refresh_token");
        //PlayerPrefs.Save();

        //// Redirect to login scene
        //SceneManager.LoadScene("Login");

        string refresh = PlayerPrefs.GetString("refresh_token");

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{\"refresh\":\"" + refresh + "\"}");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return RequestManager.SendAuthorizedRequest(request, (response) =>
        {
            if (response.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Logout failed: " + response.error);
            }
            else
            {
                Debug.Log("Logged out successfully: " + response.downloadHandler.text);
            }

            PlayerPrefs.DeleteKey("access_token");
            PlayerPrefs.DeleteKey("refresh_token");
            PlayerPrefs.Save();

            SceneManager.LoadScene("Login");
        });
    }
}


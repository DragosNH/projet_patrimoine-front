using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using System.Text;
using Unity.VisualScripting;
using UnityEngine.UI;


public class ProfileScecne: MonoBehaviour
{

    public GameObject deletePopupPanel;
    public TMP_InputField popupPasswordInput;
    public TMP_Text deleteErrorText;
    public TMP_Text firstNameText;
    public TMP_Text lastNameText;
    public TMP_Text usernameText;
    public TMP_Text emailText;
    public GameObject popupMenu;
    public Toggle themeToggle;
    public PopupFader popupFader;

    // -- Background Images -- 
    // - Background -
    public Image backgroundImage;
    public Sprite lightBackground;
    public Sprite darkBackground;
    // - Popup Background -
    public Image menuPopup;
    public Sprite lightPopup;
    public Sprite darkPopup;
    // - Confirmation Scene -
    public Image confirmationScene;
    public Sprite lightConfirmationScene;
    public Sprite darkConfirmationScene;

    // -- Buttons --
    public Button[] buttons;
    // - logo open button -
    public Button logoButton;
    public Sprite lightLogoButton;
    public Sprite darkLogoButton;
    // - Close popup menu logo button -
    public Button closeLogoButton;
    public Sprite lightCloseLogoButton;
    public Sprite darkCloseLogoButton;
    // - Logout Button -
    public Button logoutButton;
    public Sprite lightLogoutButton;
    public Sprite darkLogoutButton;
    // - Return Button -
    public Button returnButton;
    public Sprite lightReturnButton;
    public Sprite darkReturnButton;
    // - Delete button -
    public Button deleteButton;
    public Sprite lightDeleteButton;
    public Sprite darkDeleteButton;
    // - Confirm Delete button -
    public Button confirmDelete;
    public Sprite lightConfirmDelete;
    public Sprite darkConfirmDelete;
    // - Cancel delete button -
    public Button cancelDelete;
    public Sprite lightCancelDelete;
    public Sprite darkCancelDelete;

    // -- text colors --
    public TextMeshProUGUI[] texts;

    // -- Url --
    string apiUrl = NetworkConfig.ServerIP;
    //string apiUrl = NetworkConfig.ServerIP + "/api/profile/";

    void Start()
    {
        StartCoroutine(GetUserProfile());
        // Load saved theme
        ThemeManager.Instance.LoadTheme();
        themeToggle.isOn = ThemeManager.Instance.isDarkMode;
        if (ThemeManager.Instance == null)
            Debug.Log("ThemeManager is NULL!");

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
        confirmationScene.sprite = isDarkMode ? darkConfirmationScene : lightConfirmationScene;

        // -- Buttons --
        // - Logo button as the burger menu -
        logoButton.GetComponent<Image>().sprite = isDarkMode ? darkLogoButton : lightLogoButton;
        closeLogoButton.GetComponent<Image>().sprite = isDarkMode ? darkCloseLogoButton : lightCloseLogoButton;
        // - Return and delete buttons -
        returnButton.GetComponent<Image>().sprite = isDarkMode ? darkReturnButton : lightReturnButton;
        deleteButton.GetComponent<Image>().sprite = isDarkMode ? darkDeleteButton : lightDeleteButton;
        // - Delete & cancel confirmation scene -
        deleteButton.GetComponent<Image>().sprite = isDarkMode ? darkConfirmDelete : lightConfirmDelete;
        cancelDelete.GetComponent<Image>().sprite = isDarkMode ? darkCancelDelete : lightCancelDelete;
        logoutButton.GetComponent<Image>().sprite = isDarkMode ? darkLogoutButton : lightLogoutButton;
        confirmDelete.GetComponent<Image>().sprite = isDarkMode ? darkConfirmDelete : lightConfirmDelete;

        // - Menu popup -
        menuPopup.GetComponent<Image>().sprite = isDarkMode ? darkPopup : lightPopup;

        // - Texts -
        foreach (TextMeshProUGUI txt in texts)
        {
            txt.color = isDarkMode ? Color.white : Color.black;
        }
    }

    // ---- Popup menu ----

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

    // ---- Recover personnal informations ----

    IEnumerator GetUserProfile()
    {
        string token = PlayerPrefs.GetString("access_token");

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No token found.");
            yield break;
        }

        UnityWebRequest request = UnityWebRequest.Get(apiUrl + "/api/profile/");
        request.SetRequestHeader("Authorization", "Bearer " + token);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch profile: " + request.downloadHandler.text);
        }
        else
        {
            UserProfile user = JsonUtility.FromJson<UserProfile>(request.downloadHandler.text);

            firstNameText.text = user.first_name;
            lastNameText.text = user.last_name;
            usernameText.text = user.username;
            emailText.text = user.email;
            Debug.Log("User data: " + request.downloadHandler.text);
        }
    }


    // ---- Return button ----
    public void Return()
    {
        SceneManager.LoadScene("MainPage");
    }


    // ---- logout button ----
    public void Logout()
    {
        StartCoroutine(LogoutRequest());
    }

    IEnumerator LogoutRequest()
    {
        string refresh = PlayerPrefs.GetString("refresh_token");

        UnityWebRequest request = new UnityWebRequest(apiUrl + "/api/logout/", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{\"refresh\":\"" + refresh + "\"}");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("Logout failed: " + request.error);
        }
        else
        {
            Debug.Log("Logged out successfully: " + request.downloadHandler.text);
        }

        // Clear tokens
        PlayerPrefs.DeleteKey("access_token");
        PlayerPrefs.DeleteKey("refresh_token");
        PlayerPrefs.Save();

        // Redirect to login scene
        SceneManager.LoadScene("Login");
    }

    //---- Delete account ----

    //-- Display Popup --
    public void ShowDeletePopup()
    {
        deletePopupPanel.SetActive(true);
        deleteErrorText.text = "Si vous voulez supprimmer votre compte, merci d'inserrer votre mot de passe et appuyer sur \"confirmer\""; // clear any old error
        popupPasswordInput.text = ""; // reset password field
    }

    //-- Hide Popup --
    public void HideDeletePopup()
    {
        deletePopupPanel.SetActive(false);
    }

    public void ConfirmDeleteFromPopup()
    {
        string password = popupPasswordInput.text;
        if (string.IsNullOrEmpty(password))
        {
            deleteErrorText.text = "Le mot de passe est requis.";
            return;
        }

        StartCoroutine(DeleteAccountRequest(password));
    }

    IEnumerator DeleteAccountRequest(string password)
    {
        string token = PlayerPrefs.GetString("access_token");

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Access token is missing or empty!");
            yield break;
        }

        string jsonBody = "{\"password\":\"" + password + "\"}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(apiUrl + "/api/delete-account/", "DELETE");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + token); 
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Delete account failed: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
            deleteErrorText.text = "Mot de passe incorrect ou erreur de connexion.";
        }
        else
        {
            Debug.Log("Account deleted: " + request.downloadHandler.text);

            PlayerPrefs.DeleteKey("access_token");
            PlayerPrefs.DeleteKey("refresh_token");
            PlayerPrefs.Save();

            SceneManager.LoadScene("Login");
        }
    }

}

[System.Serializable]
public class UserProfile
{
    public string username;
    public string first_name;
    public string last_name;
    public string email;
}
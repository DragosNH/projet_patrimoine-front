using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class LoginPage : MonoBehaviour
{
    // -- Declared variables --
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text loginErrorText;
    public GameObject popupMenu;
    public Toggle themeToggle;
    public PopupFader popupFader;

    // -- Background images --
    // - Background-
    public Image backgroundImage;
    public Sprite lightBackground;
    public Sprite darkBackground;
    // - Popup background -
    public Image menuPopup;
    public Sprite lightPopup;
    public Sprite darkPopup;

    // -- Buttons --
    public Button[] buttons;
    // - logo open button -
    public Button logoButton;
    public Sprite lightLogoBtn;
    public Sprite darkLogoBtn;
    // - Close popup menu logo button -
    public Button closeLogoBtn;
    public Sprite lightCloseLogoBtn;
    public Sprite darkCloseLogoBtn;
    // - Login Button -
    public Button loginBtn;
    public Sprite lightLoginBtn;
    public Sprite darkLoginBtn;
    // - Sign up button -
    public Button signUpBtn;
    public Sprite lightSignUpBtn;
    public Sprite darkSignUpBtn;

    // -- text colors --
    public TextMeshProUGUI[] texts;


    private string loginURL = "http://127.0.0.1:8000/api/token/";

    void Start()
    {
        // Load saved theme
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

        // -- Buttons --
        // -- Login and Suign up buttons -
        loginBtn.GetComponent<Image>().sprite = isDarkMode ? darkLoginBtn : lightLoginBtn;
        signUpBtn.GetComponent<Image>().sprite = isDarkMode ? darkSignUpBtn : lightSignUpBtn;

        // - Logo button as the burger menu -
        logoButton.GetComponent<Image>().sprite = isDarkMode ? darkLogoBtn : lightLogoBtn;
        closeLogoBtn.GetComponent<Image>().sprite = isDarkMode ? darkCloseLogoBtn : lightCloseLogoBtn;

        // - Menu popup -
        menuPopup.GetComponent<Image>().sprite = isDarkMode ? darkPopup : lightPopup;

        // - Texts -
        foreach ( TextMeshProUGUI txt in texts)
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

    // ---- User Login ----
    public void OnLoginClick()
    {
        StartCoroutine(LoginCoroutine());
    }

    IEnumerator LoginCoroutine()
    {
        LoginData loginData = new LoginData
        {
            username = usernameInput.text,
            password = passwordInput.text,
        };

        string jsonData = JsonUtility.ToJson(loginData);

        UnityWebRequest request = new UnityWebRequest(loginURL, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Login success: " + request.downloadHandler.text);

            // Parse the token response
            TokenResponse tokenResponse = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);

            // Save tokens locally
            PlayerPrefs.SetString("access_token", tokenResponse.access);
            PlayerPrefs.SetString("refresh_token", tokenResponse.refresh);
            PlayerPrefs.Save();

            // Load main page
            SceneManager.LoadScene("MainPage");

            //Clear text
            loginErrorText.text = "";
            loginErrorText.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Login failed: " + request.downloadHandler.text);
            loginErrorText.text = "Nom d'utilisateur ou mot de passe incorrect.";
            loginErrorText.gameObject.SetActive(true);
        }
    }

    [System.Serializable]
    public class LoginData
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    public class TokenResponse
    {
        public string access;
        public string refresh;
    }

    public void SignUp()
    {
        SceneManager.LoadScene("SignUp");
    }

    public void ForgotPassword()
    {
        SceneManager.LoadScene("ForgotPassword");
    }
}

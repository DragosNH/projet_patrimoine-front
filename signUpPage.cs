using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;


public class SignUp : MonoBehaviour
{

    public TMP_InputField firstNameInput;
    public TMP_InputField lastNameInput;
    public TMP_InputField usernameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public GameObject loadingIndicator;
    public TextMeshProUGUI loadingText;
    public GameObject popupMenu;
    public Toggle themeToggle;
    public PopupFader popupFader;

    // -- Background Images --
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
    // - Close popup menu logo button -
    public Button closeLogoButton;
    public Sprite lightCloseLogoButton;
    public Sprite darkCloseLogoButton;
    // - Sign up button - 
    public Button signUpButton;
    public Sprite lightSignUpButton;
    public Sprite darkSignUpButton;

    // -- Text color --
    public TextMeshProUGUI[] texts;

    // --- Sign up Url ---
    private string signupURL = "http://192.168.1.123/api/signup/";

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

        // -- Add darkmode and light mode for the items --

        // - Background -
        backgroundImage.sprite = isDarkMode ? darkBackground : lightBackground;

        // -- Buttons --
        // - Sign up button -
        signUpButton.GetComponent<Image>().sprite = isDarkMode ? darkSignUpButton : lightSignUpButton;

        // - Logo button as burger menu -
        logoButton.GetComponent<Image>().sprite = isDarkMode ? darkLogoButton : lightLogoButton;
        closeLogoButton.GetComponent<Image>().sprite = isDarkMode ? darkCloseLogoButton : lightCloseLogoButton;

        // - Menu Popup -
        menuPopup.GetComponent<Image>().sprite = isDarkMode ? darkPopup : lightPopup;

        // -- Texts --
        foreach(TextMeshProUGUI txt in texts)
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

    public void OnSignUpClick()
    {
        loadingIndicator.SetActive(true);
        loadingText.gameObject.SetActive(true);
        loadingText.text = "En cours de création...";
        StartCoroutine(SignUpCoroutine());
    }

    IEnumerator SignUpCoroutine()
    {
        string jsonData = JsonUtility.ToJson(new SignupData
        {
            first_name = firstNameInput.text, 
            last_name = lastNameInput.text,
            username = usernameInput.text,
            email = emailInput.text,
            password = passwordInput.text,
            confirm_password = confirmPasswordInput.text
        });

        UnityWebRequest request = new UnityWebRequest(signupURL, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        loadingIndicator.SetActive(false);

        if (request.result != UnityWebRequest.Result.Success)
        {
            long code = request.responseCode;
            string body = request.downloadHandler?.text;
            Debug.LogError($"Signup failed → code: {code}, networkError: {request.error}, body: {body}");
            SceneManager.LoadScene("Login");
        }
        else
        {
            Debug.Log($"Signup succeeded: {request.downloadHandler.text}");
        }

    }

    [System.Serializable]
    public class SignupData
    {
        public string first_name;
        public string last_name;
        public string username;
        public string email;
        public string password;
        public string confirm_password;
    }

    public void Login()
    {
        SceneManager.LoadScene("Login");
    }

    public void ForgotPassword()
    {
        SceneManager.LoadScene("ForgotPassword");
    }
}

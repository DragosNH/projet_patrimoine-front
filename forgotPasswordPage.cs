using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.XR.ARSubsystems;


public class ForgotPassword: MonoBehaviour
{
    // --- Declared Variables ---
    public TMP_InputField emailInput;
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
    // - Send button -
    public Button sendButton;
    public Sprite lightSendButton;
    public Sprite darkSendButton;
    // - Return button -
    public Button returnButton;
    public Sprite lightReturnButton;
    public Sprite darkReturnButton;

    // -- text colors --
    public TextMeshProUGUI[] texts;

    // -- Url --
    string apiUrl = NetworkConfig.ServerIP + "/api/password-reset/";

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
        // - Logo button as the burger menu -
        logoButton.GetComponent<Image>().sprite = isDarkMode ? darkLogoButton : lightLogoButton;
        closeLogoButton.GetComponent<Image>().sprite = isDarkMode ? darkCloseLogoButton : lightCloseLogoButton;
        // - Send and return buttons -
        sendButton.GetComponent<Image>().sprite = isDarkMode ? darkSendButton : lightSendButton;
        returnButton.GetComponent<Image>().sprite = isDarkMode ? darkReturnButton : lightReturnButton;

        // - Menu popup -
        menuPopup.GetComponent<Image>().sprite = isDarkMode ? darkPopup : lightPopup;

        // - Texts -
        foreach (TextMeshProUGUI txt in texts)
        {
            txt.color = isDarkMode ? Color.white : Color.black;
        }
    }


    // ---- Popup menu: Display and hide ----
    public void ShowPopUpMenu()
    {
        popupMenu.SetActive(true);
        popupFader.FadeIn();

    }

    public void HidePopupMenu()
    {
        popupFader.FadeOut();
    }



    public void SendBtn()
    {
        StartCoroutine(SendResetRequest());
    }

    private IEnumerator SendResetRequest()
    {
        string email = emailInput.text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            Debug.LogWarning("Email field is empty!");
            yield break;
        }

        if (emailInput == null)
        {
            Debug.LogError("emailInput is not assigned!");
            yield break;
        }

        string jsonData = JsonUtility.ToJson(new EmailForm { email = email });

        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Accept", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Password reset email sent successfully!");

                yield return new WaitForSeconds(1f);

                UnityEngine.SceneManagement.SceneManager.LoadScene("SuccessResetScene");
            }
            else
            {
                Debug.LogError($"Password reset failed: {www.error}");
                Debug.LogError(www.downloadHandler.text);
            }
        }
    }

    [System.Serializable]
    public class EmailForm
    {
        public string email;
    }


    public void Rerurn()
    {
        SceneManager.LoadScene("Login");
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;


public class ForgotPassword: MonoBehaviour
{
    // --- Declared Variables ---
    //public TMP_InputField emailInput; -- I will need this one latter
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
        // Send information
        Debug.Log("Button clicked!");
    }

    public void Rerurn()
    {
        SceneManager.LoadScene("Login");
    }
}
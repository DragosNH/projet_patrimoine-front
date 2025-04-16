using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainLoginPage : MonoBehaviour
{
    public GameObject popupMenu;
    public PopupFader popupFader;

    // -- Theme toggle --
    public Toggle themeToggle;

    // -- Background images --
    public Image backgroundImage;
    public Image gradientImage;
    public Image menuPopup;
    public Sprite lightBackground;
    public Sprite darkBackground;
    public Sprite lightGradient;
    public Sprite darkGradient;
    public Sprite lightMenu;
    public Sprite darkMenu;

    // -- Button visuals --
    public Button[] buttons;
    public Button signUpButton;
    public Button logoButton;
    public Button logoCloseButton;
    public Sprite lightButtonSprite;
    public Sprite darkButtonSprite;
    public Sprite lightSignUpBtnSprite;
    public Sprite darkSignUpBtnSprite;
    public Sprite lightLogoSprite;
    public Sprite darkLogoSprite;
    public Sprite lightLogoCloseBtn;
    public Sprite darkLogoCloseBtn;

    // -- Text colors --
    public TextMeshProUGUI[] texts;

    void Start()
    {
        // Load saved theme preference from ThemeManager
        ThemeManager.Instance.LoadTheme();
        themeToggle.isOn = ThemeManager.Instance.isDarkMode;
        if (ThemeManager.Instance == null)
            Debug.LogError("ThemeManager is NULL!");

        if (themeToggle == null)
            Debug.LogError("Theme toggle is not assigned!");
        Debug.Log("ThemeManager.Instance is: " + ThemeManager.Instance);

        // Hook up the toggle change
        themeToggle.onValueChanged.AddListener(delegate { ToggleTheme(); });

        // Apply current theme
        UpdateTheme();
    }

    // Toggle dark/light mode
    void ToggleTheme()
    {
        ThemeManager.Instance.ToggleTheme();
        UpdateTheme();
    }

    // Apply theme visuals
    void UpdateTheme()
    {
        bool isDarkMode = ThemeManager.Instance.isDarkMode;

        // Background
        backgroundImage.sprite = isDarkMode ? darkBackground : lightBackground;

        // General buttons
        foreach (var btn in buttons)
        {
            btn.GetComponent<Image>().sprite = isDarkMode ? darkButtonSprite : lightButtonSprite;
        }

        // Sign up button
        signUpButton.GetComponent<Image>().sprite = isDarkMode ? darkSignUpBtnSprite : lightSignUpBtnSprite;

        // Logo button
        logoButton.GetComponent<Image>().sprite = isDarkMode ? darkLogoSprite : lightLogoSprite;
        logoCloseButton.GetComponent<Image>().sprite = !isDarkMode ? darkLogoCloseBtn : lightLogoCloseBtn;

        // Menu Popup
        menuPopup.GetComponent<Image>().sprite = isDarkMode ? darkMenu : lightMenu;

        // Texts
        foreach (TextMeshProUGUI txt in texts)
        {
            txt.color = isDarkMode ? Color.white : Color.black;
        }

        // Gradient
        gradientImage.GetComponent<Image>().sprite = isDarkMode ? darkGradient : lightGradient;
    }

    // -- Popup menu --
    public void ShowPopUpMenu()
    {
        popupMenu.SetActive(true);
        popupFader.FadeIn();

    }

    public void HidePopupMenu()
    {
        popupFader.FadeOut();
    }

    // -- Navigation buttons --
    public void Login()
    {
        SceneManager.LoadScene("Login");
    }

    public void SignUp()
    {
        SceneManager.LoadScene("SignUp");
    }
}

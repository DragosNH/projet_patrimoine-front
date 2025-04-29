using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.XR.ARSubsystems;


public class SuccessResetPage : MonoBehaviour
{

    // - Background -
    public Image backgroundImage;
    public Sprite lightBackground;
    public Sprite darkBackground;

    // - Return button -
    public Button returnButton;
    public Sprite lightReturnButton;
    public Sprite darkReturnButton;

    // -- text colors --
    public TextMeshProUGUI[] texts;

    public void ReturnToMainPage()
    {
        SceneManager.LoadScene("MainLogin");
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
        
        // - Return Button - 
        returnButton.GetComponent<Image>().sprite = isDarkMode ? darkReturnButton : lightReturnButton;

        // - Texts -
        foreach (TextMeshProUGUI txt in texts)
        {
            txt.color = isDarkMode ? Color.white : Color.black;
        }
    }

}
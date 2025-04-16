using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance { get; private set; }

    public bool isDarkMode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ToggleTheme()
    {
        isDarkMode = !isDarkMode;
        PlayerPrefs.SetInt("dark_mode", isDarkMode ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void LoadTheme()
    {
        isDarkMode = PlayerPrefs.GetInt("dark_mode", 0) == 1;
    }
}
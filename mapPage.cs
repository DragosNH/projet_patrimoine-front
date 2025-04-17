    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections;
    using UnityEngine.Networking;
    using UnityEngine.SceneManagement;
    using TMPro;
    using Unity.VisualScripting;

    public class OSMTileLoader : MonoBehaviour
    {
        public GameObject popupMenu;
        public Toggle themeToggle;
        public PopupFader popupFader;
        // Reference to the drag handler
        public MapDragHandler mapDragHandler;

        public float horisontalNudge = -100f;

        // -- Map Variables --
        public RawImage mapImage;
        public int zoom = 15;
        // Remove the dragStartPos and isDragging since input comes via MapDragHandler now
        public int tileX = 17184;
        public int tileY = 11646;

        // -- Tiles --
        public GameObject tilePrefab;
        public Transform tileContainer;

        // -- Background Image --
        public Image backgroundImage;
        public Sprite lightBackground;
        public Sprite darkBackground;
        public Image menuPopup;
        public Sprite lightPopup;
        public Sprite darkPopup;

        // -- Buttons --
        public Button[] buttons;
        public Button logoButton;
        public Sprite lightLogoButton;
        public Sprite darkLogoButton;
        public Button closeLogoButton;
        public Sprite lightCloseLogoButton;
        public Sprite darkCloseLogoButton;
        public Button logoutBtn;
        public Sprite lightLogoutBtn;
        public Sprite darkLogoutBtn;
        public Button returnButton;
        public Sprite lightReturnButton;
        public Sprite darkReturnButton;
        public Button zoomInButton;
        public Button zoomOutButton;

        // -- Text Color --
        public TextMeshProUGUI[] texts;

        // User location mark
        public GameObject userMarkerPrefab;
        public GameObject UserMarkerInstance;

        void Start()
        {
            // Subscribe to drag finished event from MapDragHandler
            if (mapDragHandler != null)
            {
                mapDragHandler.OnDragFinished += HandleDragFinished;
            }
            else
            {
                Debug.LogWarning("MapDragHandler not set! Please attach it to your dedicated drag panel.");
            }

            // Initial tile coordinates & zoom
            tileX = 4296;
            tileY = 2911;
            zoom = 13;

            ChangeZoom(0);
            LoadMapGrid(tileX, tileY, zoom);

            ThemeManager.Instance.LoadTheme();
            themeToggle.isOn = ThemeManager.Instance.isDarkMode;

            if (ThemeManager.Instance == null)
                Debug.LogError("ThemeManager is NULL!");
            if (themeToggle == null)
                Debug.LogError("Theme toggle is not assigned!");

            themeToggle.onValueChanged.AddListener(delegate { ToggleTheme(); });
            UpdateTheme();

            zoomInButton.onClick.AddListener(() => ChangeZoom(1));
            zoomOutButton.onClick.AddListener(() => ChangeZoom(-1));

        StartCoroutine(StartLocationService());
    }

    // Called when MapDragHandler finishes dragging.
    void HandleDragFinished(Vector2 finalOffset)
        {
            // Calculate tile coordinate shifts based on snapped offset.
            int dx = Mathf.RoundToInt(-finalOffset.x / 256f);
            int dy = Mathf.RoundToInt(finalOffset.y / 256f);

            tileX += dx;
            tileY += dy;

            // Reload the grid using new tile coordinates.
            LoadMapGrid(tileX, tileY, zoom);
        }

        // Zoom changes recalculate center tile and reload grid.
        void ChangeZoom(int delta)
        {
            zoom += delta;
            zoom = Mathf.Clamp(zoom, 1, 19);

            float mulhouseLat = 47.7508f;
            float mulhouseLon = 7.3359f;

            tileX = (int)((mulhouseLon + 180.0f) / 360.0f * Mathf.Pow(2, zoom));
            tileY = (int)((1.0f - Mathf.Log(Mathf.Tan(mulhouseLat * Mathf.Deg2Rad) + 1.0f / Mathf.Cos(mulhouseLat * Mathf.Deg2Rad)) / Mathf.PI) / 2.0f * Mathf.Pow(2, zoom));

            tileX = Mathf.Clamp(tileX, 0, (int)Mathf.Pow(2, zoom) - 1);
            tileY = Mathf.Clamp(tileY, 0, (int)Mathf.Pow(2, zoom) - 1);

            // Clear existing tiles.
            foreach (Transform child in tileContainer)
            {
                Destroy(child.gameObject);
            }

            LoadMapGrid(tileX, tileY, zoom);

        }

        // Create a dynamic grid that fills the tile container.
        void LoadMapGrid(int centerX, int centerY, int z)
        {
            // Clear existing tiles.
            foreach (Transform child in tileContainer)
            {
                Destroy(child.gameObject);
            }

            // Get container dimensions.
            RectTransform containerRect = tileContainer.GetComponent<RectTransform>();
            float containerWidth = containerRect.rect.width;
            float containerHeight = containerRect.rect.height;
            float tileSize = 256f;

            // Determine how many columns and rows are needed to cover the container.
            int columns = Mathf.CeilToInt(containerWidth / tileSize) +5;
            int rows = Mathf.CeilToInt(containerHeight / tileSize) +5;

            // Calculate base offset to center the grid.
            float horizontalOffset = (columns * tileSize - containerWidth) / 2f;
            float verticalOffset = (rows * tileSize - containerHeight) / 2f;
            float adjustedHorizontalOffset = horizontalOffset + horisontalNudge;
            Debug.Log($"HorizontalOffset: {horizontalOffset}, horisontalNudge: {horisontalNudge}, adjustedHorizontalOffset: {adjustedHorizontalOffset}");



        // Debug log container info
        Debug.Log($"Container width: {containerWidth}, Height: {containerHeight}, Pivot: {containerRect.pivot}");
        Debug.Log($"Calculated horizontal offset: {horizontalOffset}, vertical offset: {verticalOffset}");


            // Instantiate tiles.
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    // Compute tile's anchored position.
                    float posX = col * tileSize - adjustedHorizontalOffset;
                    float posY = row * tileSize - verticalOffset;

                    // Compute tile grid coordinates.
                    int tileGridX = centerX + col - columns / 2;
                    int tileGridY = centerY - (row - rows / 2);

                    // Instantiate tile.
                    GameObject tileObj = Instantiate(tilePrefab, tileContainer);
                    tileObj.transform.localScale = Vector3.one;

                    // Force native scale.
                    tileObj.transform.localScale = Vector3.one;

                    // Set size and position.
                    RectTransform rt = tileObj.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(tileSize, tileSize);
                    rt.anchoredPosition = new Vector2(posX, posY);

                    // Load tile texture.
                    RawImage rawImage = tileObj.GetComponent<RawImage>();
                    StartCoroutine(LoadTile(tileGridX, tileGridY, z, rawImage));

                    // Optional debug color.
                    rawImage.color = new Color(0.95f, 1f, 0.95f);
                }
            }
        Debug.Log($"Container width: {containerWidth}, Height: {containerHeight}, Pivot: {containerRect.pivot}");
        Debug.Log($"Calculated horizontal offset: {horizontalOffset}, vertical offset: {verticalOffset}");

    }


    // Load a tile texture asynchronously from OpenStreetMap.
        IEnumerator LoadTile(int x, int y, int z, RawImage targetImage)
        {
            string url = $"https://tile.openstreetmap.org/{z}/{x}/{y}.png";
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (targetImage == null)
                yield break;

    #if UNITY_2020_1_OR_NEWER
            if (request.result == UnityWebRequest.Result.Success)
    #else
            if (!request.isNetworkError && !request.isHttpError)
    #endif
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(request);
                if (tex != null)
                {
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    targetImage.texture = tex;
                    targetImage.rectTransform.sizeDelta = new Vector2(tex.width, tex.height);
                }
                else
                {
                    Debug.LogError($"Texture was null for tile {x}, {y}");
                }
            }
            else
            {
                Debug.LogError($"Failed to load tile {x}, {y}: {request.error}");
            }
        }

        // Find User's location
        IEnumerator StartLocationService()
        {
            if (!Input.location.isEnabledByUser)
            {
                Debug.Log("Access not permited by the user");
                yield break;
            }

            Input.location.Start();

            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            if (maxWait <= 0)
            {
                Debug.Log("time out");
                yield break;
            }

            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.Log("Unable to determine the location");
                yield break;
            }
            else
            {
                float latitude = Input.location.lastData.latitude;
                float longitude = Input.location.lastData.longitude;
                Debug.Log($"User Location:\nlatitude: {latitude}\nlongitude: {longitude}");

            // Convert GPS to tile position
            int userTileX = (int)(((longitude + 180.0f) / 360.0f) * Mathf.Pow(2, zoom));
            int userTileY = (int)(((1.0f - Mathf.Log(Mathf.Tan(latitude * Mathf.Deg2Rad) + 1.0f / Mathf.Cos(latitude * Mathf.Deg2Rad)) / Mathf.PI) / 2.0f) * Mathf.Pow(2, zoom));

            // Calculate offset from center tile
            int dx = userTileX - tileX;
                int dy = tileY - userTileY;

                float tileSize = 256f;
                float markerX = tileContainer.GetComponent<RectTransform>().rect.width / 2 + dx * tileSize;
                float markerY = tileContainer.GetComponent<RectTransform>().rect.height / 2 + dy * tileSize;

                if (UserMarkerInstance != null)
                {
                    Destroy(UserMarkerInstance);
                }

                UserMarkerInstance = Instantiate(userMarkerPrefab, tileContainer);
                UserMarkerInstance.GetComponent<RectTransform>().anchoredPosition = new Vector2(markerX, markerY);
        }

            Input.location.Stop();

        }


        // ---- Theme and UI Navigation Methods ----

        void ToggleTheme()
        {
            ThemeManager.Instance.ToggleTheme();
            UpdateTheme();
        }

        void UpdateTheme()
        {
            bool isDarkMode = ThemeManager.Instance.isDarkMode;
            backgroundImage.sprite = isDarkMode ? darkBackground : lightBackground;
            logoButton.GetComponent<Image>().sprite = isDarkMode ? darkLogoButton : lightLogoButton;
            closeLogoButton.GetComponent<Image>().sprite = isDarkMode ? darkCloseLogoButton : lightCloseLogoButton;
            logoutBtn.GetComponent<Image>().sprite = isDarkMode ? darkLogoutBtn : lightLogoutBtn;
            returnButton.GetComponent<Image>().sprite = isDarkMode ? darkReturnButton : lightReturnButton;
            menuPopup.GetComponent<Image>().sprite = isDarkMode ? darkPopup : lightPopup;
            foreach (TextMeshProUGUI txt in texts)
            {
                txt.color = isDarkMode ? Color.white : Color.black;
            }
        }

        public void ShowPopUpMenu()
        {
            popupMenu.SetActive(true);
            popupFader.FadeIn();
        }

        public void HidePopupMenu()
        {
            popupFader.FadeOut();
        }

        public void ProfilePage()
        {
            SceneManager.LoadScene("ProfilePage");
        }

        public void Return()
        {
            SceneManager.LoadScene("MainPage");
        }

        public void Logout()
        {
            StartCoroutine(LogoutRequest());
        }

        IEnumerator LogoutRequest()
        {
            string refresh = PlayerPrefs.GetString("refresh_token");
            UnityWebRequest request = new UnityWebRequest("http://127.0.0.1:8000/api/logout/", "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{\"refresh\":\"" + refresh + "\"}");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("Logout failed: " + request.error);
            else
                Debug.Log("Logged out successfully: " + request.downloadHandler.text);
            PlayerPrefs.DeleteKey("access_token");
            PlayerPrefs.DeleteKey("refresh_token");
            PlayerPrefs.Save();
            SceneManager.LoadScene("Login");
        }
    }

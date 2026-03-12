using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    private const string DisplayModeKey = "opt_display_mode";
    private const string ResolutionWidthKey = "opt_resolution_width";
    private const string ResolutionHeightKey = "opt_resolution_height";
    private const string QualityLevelKey = "opt_quality_level";
    private const string CameraFovKey = "opt_camera_fov";
    private const string CameraSensitivityPercentKey = "opt_camera_sensitivity_pct";
    private const string MasterVolumeKey = "opt_master_volume";
    private const string GameSfxVolumeKey = "opt_game_sfx_volume";
    private const string MenuSfxVolumeKey = "opt_menu_sfx_volume";
    private const string GameMusicVolumeKey = "opt_game_music_volume";
    private const string MenuMusicVolumeKey = "opt_menu_music_volume";
    private const string CrosshairSizeKey = "opt_crosshair_size";
    private const string CrosshairColorKey = "opt_crosshair_color";
    private const string ShowFpsKey = "opt_show_fps";
    private const string ShowPingKey = "opt_show_ping";
    private const string ShowSystemClockKey = "opt_show_system_clock";
    private const int BorderlessModeOption = 1;
    private const int BestResolutionIndex = 0;
    private const int DefaultHudWidgetVisible = 1;
    private const float MinCrosshairSliderValue = 1f;
    private const float DefaultCrosshairSliderValue = 1f;
    private const float MaxCrosshairSliderValue = 10f;
    private const float MinCrosshairPixelSize = 5f;
    private const float MaxCrosshairPixelSize = 50f;

    private const float MinFov = 60f;
    private const float MaxFov = 100f;
    private const float DefaultFov = 80f;
    private const float MinSensitivity = 0.01f;
    private const float MinSensitivityPercent = 1f;
    private const float DefaultSensitivityPercent = 50f;
    private const float DefaultVolumePercent = 50f;
    private const string VolumeDefaultsInitializedKey = "opt_volume_defaults_initialized_v1";

    [Header("Video")]
    [SerializeField] private TMP_Dropdown displayModeDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown graphicsQualityDropdown;
    [SerializeField] private Slider cameraFovSlider;
    [SerializeField] private Slider cameraSensitivitySlider;

    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider gameSfxVolumeSlider;
    [SerializeField] private Slider menuSfxVolumeSlider;
    [SerializeField] private Slider gameMusicVolumeSlider;
    [SerializeField] private Slider menuMusicVolumeSlider;
    [SerializeField] private AudioMixer audioMixer;
    private const string masterVolumeParam = "MasterVolume";
    private const string gameSfxVolumeParam = "GameSFXVolume";
    private const string menuSfxVolumeParam = "MenuSFXVolume";
    private const string gameMusicVolumeParam = "GameMusicVolume";
    private const string menuMusicVolumeParam = "MenuMusicVolume";

    [Header("HUD")]
    [SerializeField] private Slider crosshairSizeSlider;
    [SerializeField] private TMP_Dropdown crosshairColorDropdown;
    [SerializeField] private AnimatedToggle showFpsToggle;
    [SerializeField] private AnimatedToggle showPingToggle;
    [SerializeField] private AnimatedToggle showSystemClockToggle;
    [SerializeField] private RectTransform crosshairRect;
    [SerializeField] private Graphic crosshairGraphic;
    [SerializeField] private GameObject fpsWidget;
    [SerializeField] private GameObject pingWidget;
    [SerializeField] private GameObject systemClockWidget;

    [Serializable]
    public class Tab
    {
        public Button button;
        public GameObject panel;
        public Graphic labelGraphic;
    }

    [Header("Tabs")]
    [SerializeField] private Tab[] tabs = Array.Empty<Tab>();
    [SerializeField] private int defaultTabIndex;
    [SerializeField] private Color activeTextColor = new Color32(51, 51, 51, 255);
    [SerializeField] private Color inactiveTextColor = new Color32(200, 150, 50, 255);

    private UnityAction[] tabClickHandlers;
    private bool tabsRegistered;
    private InputActions input;

    private readonly List<Vector2Int> resList = new List<Vector2Int>();
    private int currentDisplayModeOption = BorderlessModeOption;
    private int lastManualResolutionIndex;

    private static readonly Color[] CrossCols =
    {
        Color.white,
        Color.gray,
        Color.black,
        Color.red,
        Color.green,
        Color.blue,
        new Color32(255, 255, 0, 255),
        Color.cyan,
        Color.magenta,
        new Color32(255, 128, 0, 255)
    };

    void Awake()
    {
        EnsureVolumeDefaults();

        PopulateResolutionDropdown();
        PopulateQualityDropdown();
        EnableDropdownHelpers();
        LoadFromPreferencesAndApply();
        RegisterListeners();
    }

    void OnEnable()
    {
        if (input == null)
        {
            input = new InputActions();
        }

        input.System.Pause.performed += OnPauseInput;
        input.System.Enable();
        RegisterTabs();
        ShowTab(defaultTabIndex);
    }

    void OnDisable()
    {
        if (input != null)
        {
            input.System.Pause.performed -= OnPauseInput;
            input.System.Disable();
        }

        UnregisterTabs();
    }

    void OnDestroy()
    {
        UnregisterListeners();
    }

    public static float SensMap(float percent)
    {
        float clampedPercent = Mathf.Clamp(percent, MinSensitivityPercent, 100f);
        float steppedPercent = Mathf.Round(clampedPercent);
        return Mathf.Max(MinSensitivity, steppedPercent / 100f);
    }

    public static float SavedFov(float fallback)
    {
        float defaultValue = Mathf.Clamp(fallback, MinFov, MaxFov);
        return Mathf.Clamp(PlayerPrefs.GetFloat(CameraFovKey, defaultValue), MinFov, MaxFov);
    }

    public static float SavedSensPct(float fallbackPercent)
    {
        float defaultValue = Mathf.Clamp(fallbackPercent, MinSensitivityPercent, 100f);
        return Mathf.Clamp(PlayerPrefs.GetFloat(CameraSensitivityPercentKey, defaultValue), MinSensitivityPercent, 100f);
    }

    void RegisterListeners()
    {
        if (displayModeDropdown != null)
        {
            displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        if (graphicsQualityDropdown != null)
        {
            graphicsQualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        }

        if (cameraFovSlider != null)
        {
            cameraFovSlider.onValueChanged.AddListener(OnFovChanged);
        }

        if (cameraSensitivitySlider != null)
        {
            cameraSensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (gameSfxVolumeSlider != null)
        {
            gameSfxVolumeSlider.onValueChanged.AddListener(OnGameSfxVolumeChanged);
        }

        if (menuSfxVolumeSlider != null)
        {
            menuSfxVolumeSlider.onValueChanged.AddListener(OnMenuSfxVolumeChanged);
        }

        if (gameMusicVolumeSlider != null)
        {
            gameMusicVolumeSlider.onValueChanged.AddListener(OnGameMusicVolumeChanged);
        }

        if (menuMusicVolumeSlider != null)
        {
            menuMusicVolumeSlider.onValueChanged.AddListener(OnMenuMusicVolumeChanged);
        }

        if (crosshairSizeSlider != null)
        {
            crosshairSizeSlider.onValueChanged.AddListener(OnCrosshairSizeChanged);
        }

        if (crosshairColorDropdown != null)
        {
            crosshairColorDropdown.onValueChanged.AddListener(OnCrosshairColorChanged);
        }

        if (showFpsToggle != null)
        {
            showFpsToggle.onValueChanged.AddListener(OnShowFpsChanged);
        }

        if (showPingToggle != null)
        {
            showPingToggle.onValueChanged.AddListener(OnShowPingChanged);
        }

        if (showSystemClockToggle != null)
        {
            showSystemClockToggle.onValueChanged.AddListener(OnShowSystemClockChanged);
        }
    }

    void UnregisterListeners()
    {
        if (displayModeDropdown != null)
        {
            displayModeDropdown.onValueChanged.RemoveListener(OnDisplayModeChanged);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        }

        if (graphicsQualityDropdown != null)
        {
            graphicsQualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
        }

        if (cameraFovSlider != null)
        {
            cameraFovSlider.onValueChanged.RemoveListener(OnFovChanged);
        }

        if (cameraSensitivitySlider != null)
        {
            cameraSensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        }

        if (gameSfxVolumeSlider != null)
        {
            gameSfxVolumeSlider.onValueChanged.RemoveListener(OnGameSfxVolumeChanged);
        }

        if (menuSfxVolumeSlider != null)
        {
            menuSfxVolumeSlider.onValueChanged.RemoveListener(OnMenuSfxVolumeChanged);
        }

        if (gameMusicVolumeSlider != null)
        {
            gameMusicVolumeSlider.onValueChanged.RemoveListener(OnGameMusicVolumeChanged);
        }

        if (menuMusicVolumeSlider != null)
        {
            menuMusicVolumeSlider.onValueChanged.RemoveListener(OnMenuMusicVolumeChanged);
        }

        if (crosshairSizeSlider != null)
        {
            crosshairSizeSlider.onValueChanged.RemoveListener(OnCrosshairSizeChanged);
        }

        if (crosshairColorDropdown != null)
        {
            crosshairColorDropdown.onValueChanged.RemoveListener(OnCrosshairColorChanged);
        }

        if (showFpsToggle != null)
        {
            showFpsToggle.onValueChanged.RemoveListener(OnShowFpsChanged);
        }

        if (showPingToggle != null)
        {
            showPingToggle.onValueChanged.RemoveListener(OnShowPingChanged);
        }

        if (showSystemClockToggle != null)
        {
            showSystemClockToggle.onValueChanged.RemoveListener(OnShowSystemClockChanged);
        }
    }

    void PopulateResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            return;
        }

        resList.Clear();

        HashSet<long> seen = new HashSet<long>();
        Resolution[] resolutions = Screen.resolutions;

        for (int i = 0; i < resolutions.Length; i++)
        {
            int width = resolutions[i].width;
            int height = resolutions[i].height;
            long key = ((long)width << 32) | (uint)height;

            if (!seen.Add(key))
            {
                continue;
            }

            resList.Add(new Vector2Int(width, height));
        }

        if (resList.Count == 0)
        {
            resList.Add(new Vector2Int(Screen.width, Screen.height));
        }

        resList.Sort((a, b) =>
        {
            long pixelsA = (long)a.x * a.y;
            long pixelsB = (long)b.x * b.y;
            int pixelCompare = pixelsB.CompareTo(pixelsA);
            if (pixelCompare != 0)
            {
                return pixelCompare;
            }

            int widthCompare = b.x.CompareTo(a.x);
            if (widthCompare != 0)
            {
                return widthCompare;
            }

            return b.y.CompareTo(a.y);
        });

        List<string> options = new List<string>(resList.Count);
        for (int i = 0; i < resList.Count; i++)
        {
            Vector2Int r = resList[i];
            options.Add($"{r.x} x {r.y}");
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    void PopulateQualityDropdown()
    {
        if (graphicsQualityDropdown == null)
        {
            return;
        }

        graphicsQualityDropdown.ClearOptions();
        graphicsQualityDropdown.AddOptions(new List<string>(QualitySettings.names));
    }

    void EnableDropdownHelpers()
    {
        AddDropdownHelper(displayModeDropdown);
        AddDropdownHelper(resolutionDropdown);
        AddDropdownHelper(graphicsQualityDropdown);
        AddDropdownHelper(crosshairColorDropdown);
    }

    void AddDropdownHelper(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
        {
            return;
        }

        if (dropdown.GetComponent<DropdownHelper>() == null)
        {
            dropdown.gameObject.AddComponent<DropdownHelper>();
        }
    }

    public void ResetTab()
    {
        ShowTab(defaultTabIndex);
    }

    public void ShowTab(int index)
    {
        if (tabs == null || tabs.Length == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(index, 0, tabs.Length - 1);

        for (int i = 0; i < tabs.Length; i++)
        {
            bool isActive = i == clampedIndex;
            Tab tab = tabs[i];

            if (tab == null)
            {
                continue;
            }

            if (tab.panel != null)
            {
                tab.panel.SetActive(isActive);
            }

            if (tab.button != null)
            {
                tab.button.interactable = !isActive;
            }

            if (tab.labelGraphic != null)
            {
                tab.labelGraphic.color = isActive ? activeTextColor : inactiveTextColor;
            }
        }
    }

    void RegisterTabs()
    {
        if (tabsRegistered)
        {
            return;
        }

        tabClickHandlers = new UnityAction[tabs.Length];

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null || tabs[i].button == null)
            {
                continue;
            }

            int index = i;
            tabClickHandlers[i] = () => ShowTab(index);
            tabs[i].button.onClick.AddListener(tabClickHandlers[i]);
        }

        tabsRegistered = true;
    }

    void UnregisterTabs()
    {
        if (!tabsRegistered || tabs == null || tabClickHandlers == null)
        {
            return;
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null || tabs[i].button == null || tabClickHandlers[i] == null)
            {
                continue;
            }

            tabs[i].button.onClick.RemoveListener(tabClickHandlers[i]);
        }

        tabsRegistered = false;
    }

    void OnPauseInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        PauseMenu pauseMenu = UnityEngine.Object.FindFirstObjectByType<PauseMenu>();

        if (pauseMenu != null && PauseMenu.isOpen)
        {
            return;
        }

        MainMenu mainMenu = UnityEngine.Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);

        if (mainMenu != null)
        {
            mainMenu.CloseOptions();
        }
    }

    void LoadFromPreferencesAndApply()
    {
        int displayModeOption = PlayerPrefs.HasKey(DisplayModeKey)
            ? PlayerPrefs.GetInt(DisplayModeKey)
            : 1;
        displayModeOption = Mathf.Clamp(displayModeOption, 0, 2);
        currentDisplayModeOption = displayModeOption;

        if (displayModeDropdown != null)
        {
            displayModeDropdown.SetValueWithoutNotify(displayModeOption);
            displayModeDropdown.RefreshShownValue();
        }

        ApplyDisplayMode(displayModeOption, false);

        int resolutionIndex = GetSavedResolutionIndex();
        lastManualResolutionIndex = resolutionIndex;
        if (resolutionDropdown != null && resList.Count > 0)
        {
            resolutionDropdown.SetValueWithoutNotify(resolutionIndex);
            resolutionDropdown.RefreshShownValue();
        }

        ApplyResolutionForMode(currentDisplayModeOption, wasBorderless: false);

        int savedQualityIndex = PlayerPrefs.GetInt(QualityLevelKey, QualitySettings.GetQualityLevel());
        savedQualityIndex = Mathf.Clamp(savedQualityIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        int qualityDropdownIndex = GetDropdownIndexForQuality(savedQualityIndex);

        if (graphicsQualityDropdown != null)
        {
            qualityDropdownIndex = Mathf.Clamp(qualityDropdownIndex, 0, Mathf.Max(0, graphicsQualityDropdown.options.Count - 1));
            graphicsQualityDropdown.SetValueWithoutNotify(qualityDropdownIndex);
            graphicsQualityDropdown.RefreshShownValue();
        }

        ApplyQualityByDropdownIndex(qualityDropdownIndex, false);

        float fov = SavedFov(DefaultFov);
        if (cameraFovSlider != null)
        {
            cameraFovSlider.minValue = MinFov;
            cameraFovSlider.maxValue = MaxFov;
            cameraFovSlider.SetValueWithoutNotify(fov);
        }
        ApplyCameraFov(fov, false);

        float sensitivityPercent = SavedSensPct(DefaultSensitivityPercent);
        if (cameraSensitivitySlider != null)
        {
            cameraSensitivitySlider.minValue = MinSensitivityPercent;
            cameraSensitivitySlider.maxValue = 100f;
            cameraSensitivitySlider.wholeNumbers = true;
            cameraSensitivitySlider.SetValueWithoutNotify(sensitivityPercent);
        }
        ApplyCameraSensitivity(sensitivityPercent, false);

        float masterVolume = Mathf.Clamp(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolumePercent), 0f, 100f);
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 100f;
            masterVolumeSlider.SetValueWithoutNotify(masterVolume);
        }
        ApplyVolume(masterVolume, masterVolumeParam, MasterVolumeKey, false, true);

        float gameSfxVolume = Mathf.Clamp(PlayerPrefs.GetFloat(GameSfxVolumeKey, DefaultVolumePercent), 0f, 100f);
        if (gameSfxVolumeSlider != null)
        {
            gameSfxVolumeSlider.minValue = 0f;
            gameSfxVolumeSlider.maxValue = 100f;
            gameSfxVolumeSlider.SetValueWithoutNotify(gameSfxVolume);
        }
        ApplyVolume(gameSfxVolume, gameSfxVolumeParam, GameSfxVolumeKey, false, false);

        float menuSfxVolume = Mathf.Clamp(PlayerPrefs.GetFloat(MenuSfxVolumeKey, DefaultVolumePercent), 0f, 100f);
        if (menuSfxVolumeSlider != null)
        {
            menuSfxVolumeSlider.minValue = 0f;
            menuSfxVolumeSlider.maxValue = 100f;
            menuSfxVolumeSlider.SetValueWithoutNotify(menuSfxVolume);
        }
        ApplyVolume(menuSfxVolume, menuSfxVolumeParam, MenuSfxVolumeKey, false, false);

        float gameMusicVolume = Mathf.Clamp(PlayerPrefs.GetFloat(GameMusicVolumeKey, DefaultVolumePercent), 0f, 100f);
        if (gameMusicVolumeSlider != null)
        {
            gameMusicVolumeSlider.minValue = 0f;
            gameMusicVolumeSlider.maxValue = 100f;
            gameMusicVolumeSlider.SetValueWithoutNotify(gameMusicVolume);
        }
        ApplyVolume(gameMusicVolume, gameMusicVolumeParam, GameMusicVolumeKey, false, false);

        float menuMusicVolume = Mathf.Clamp(PlayerPrefs.GetFloat(MenuMusicVolumeKey, DefaultVolumePercent), 0f, 100f);
        if (menuMusicVolumeSlider != null)
        {
            menuMusicVolumeSlider.minValue = 0f;
            menuMusicVolumeSlider.maxValue = 100f;
            menuMusicVolumeSlider.SetValueWithoutNotify(menuMusicVolume);
        }
        ApplyVolume(menuMusicVolume, menuMusicVolumeParam, MenuMusicVolumeKey, false, false);

        float crosshairSize = Mathf.Clamp(PlayerPrefs.GetFloat(CrosshairSizeKey, DefaultCrosshairSliderValue), MinCrosshairSliderValue, MaxCrosshairSliderValue);
        if (crosshairSizeSlider != null)
        {
            crosshairSizeSlider.minValue = MinCrosshairSliderValue;
            crosshairSizeSlider.maxValue = MaxCrosshairSliderValue;
            crosshairSizeSlider.SetValueWithoutNotify(crosshairSize);
        }
        ApplyCrosshairSize(crosshairSize, false);

        int crosshairColorIndex = Mathf.Clamp(PlayerPrefs.GetInt(CrosshairColorKey, 0), 0, CrossCols.Length - 1);
        if (crosshairColorDropdown != null)
        {
            crosshairColorDropdown.SetValueWithoutNotify(crosshairColorIndex);
            crosshairColorDropdown.RefreshShownValue();
        }
        ApplyCrosshairColor(crosshairColorIndex, false);

        bool showFps = PlayerPrefs.GetInt(ShowFpsKey, DefaultHudWidgetVisible) == 1;
        if (showFpsToggle != null)
        {
            showFpsToggle.SetValue(showFps, false);
        }
        ApplyHudWidget(fpsWidget, showFps, ShowFpsKey, false);

        bool showPing = PlayerPrefs.GetInt(ShowPingKey, DefaultHudWidgetVisible) == 1;
        if (showPingToggle != null)
        {
            showPingToggle.SetValue(showPing, false);
        }
        ApplyHudWidget(pingWidget, showPing, ShowPingKey, false);

        bool showSystemClock = PlayerPrefs.GetInt(ShowSystemClockKey, DefaultHudWidgetVisible) == 1;
        if (showSystemClockToggle != null)
        {
            showSystemClockToggle.SetValue(showSystemClock, false);
        }
        ApplyHudWidget(systemClockWidget, showSystemClock, ShowSystemClockKey, false);
    }

    int GetSavedResolutionIndex()
    {
        if (resList.Count == 0)
        {
            return 0;
        }

        if (!PlayerPrefs.HasKey(ResolutionWidthKey) || !PlayerPrefs.HasKey(ResolutionHeightKey))
        {
            return 0;
        }

        int defaultWidth = Screen.width;
        int defaultHeight = Screen.height;

        int savedWidth = PlayerPrefs.GetInt(ResolutionWidthKey, defaultWidth);
        int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, defaultHeight);

        for (int i = 0; i < resList.Count; i++)
        {
            if (resList[i].x == savedWidth && resList[i].y == savedHeight)
            {
                return i;
            }
        }

        int closestIndex = 0;
        long closestDistance = long.MaxValue;

        for (int i = 0; i < resList.Count; i++)
        {
            long dx = resList[i].x - defaultWidth;
            long dy = resList[i].y - defaultHeight;
            long distance = dx * dx + dy * dy;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    FullScreenMode GetFullScreenModeFromOption(int option)
    {
        switch (option)
        {
            case 1:
                return FullScreenMode.FullScreenWindow;
            case 2:
#if UNITY_STANDALONE_WIN
                return FullScreenMode.ExclusiveFullScreen;
#else
                return FullScreenMode.FullScreenWindow;
#endif
            default:
                return FullScreenMode.Windowed;
        }
    }

    int GetModeOption()
    {
        if (displayModeDropdown != null)
        {
            return Mathf.Clamp(displayModeDropdown.value, 0, 2);
        }

        return Mathf.Clamp(PlayerPrefs.GetInt(DisplayModeKey, 1), 0, 2);
    }

    FullScreenMode GetMode()
    {
        return GetFullScreenModeFromOption(GetModeOption());
    }

    bool IsBorderlessOption(int option)
    {
        return option == BorderlessModeOption;
    }

    int GetCurrentResolutionIndex()
    {
        if (resolutionDropdown != null)
        {
            return Mathf.Clamp(resolutionDropdown.value, 0, Mathf.Max(0, resList.Count - 1));
        }

        return GetSavedResolutionIndex();
    }

    void SetResolutionInteractable(bool isInteractable)
    {
        if (resolutionDropdown != null)
        {
            resolutionDropdown.interactable = isInteractable;
        }
    }

    void SetResolutionValue(int index)
    {
        if (resolutionDropdown == null || resList.Count == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(index, 0, resList.Count - 1);
        resolutionDropdown.SetValueWithoutNotify(clampedIndex);
        resolutionDropdown.RefreshShownValue();
    }

    void ApplyResolutionForMode(int modeOption, bool wasBorderless)
    {
        if (resList.Count == 0)
        {
            SetResolutionInteractable(!IsBorderlessOption(modeOption));
            return;
        }

        if (IsBorderlessOption(modeOption))
        {
            SetResolutionInteractable(false);
            SetResolutionValue(BestResolutionIndex);
            ApplyResolution(BestResolutionIndex, false);
            return;
        }

        SetResolutionInteractable(true);
        int targetIndex = wasBorderless
            ? Mathf.Clamp(lastManualResolutionIndex, 0, resList.Count - 1)
            : GetCurrentResolutionIndex();

        SetResolutionValue(targetIndex);
        ApplyResolution(targetIndex, false);
    }

    int GetDropdownIndexForQuality(int qualityIndex)
    {
        if (graphicsQualityDropdown == null || graphicsQualityDropdown.options.Count == 0)
        {
            return qualityIndex;
        }

        return Mathf.Clamp(qualityIndex, 0, graphicsQualityDropdown.options.Count - 1);
    }

    int GetQualityIndexFromDropdownOption(int optionIndex)
    {
        if (graphicsQualityDropdown == null || graphicsQualityDropdown.options.Count == 0)
        {
            return Mathf.Clamp(optionIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        }

        return Mathf.Clamp(optionIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
    }

    void OnDisplayModeChanged(int optionIndex)
    {
        int clampedOption = Mathf.Clamp(optionIndex, 0, 2);
        bool wasBorderless = IsBorderlessOption(currentDisplayModeOption);
        bool isBorderless = IsBorderlessOption(clampedOption);

        if (!wasBorderless && isBorderless)
        {
            lastManualResolutionIndex = GetCurrentResolutionIndex();
        }

        ApplyDisplayMode(clampedOption, true);
        ApplyResolutionForMode(clampedOption, wasBorderless);
        currentDisplayModeOption = clampedOption;
    }

    void OnResolutionChanged(int optionIndex)
    {
        if (IsBorderlessOption(currentDisplayModeOption))
        {
            return;
        }

        if (resList.Count == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(optionIndex, 0, resList.Count - 1);
        lastManualResolutionIndex = clampedIndex;
        ApplyResolution(clampedIndex, true);
    }

    void OnQualityChanged(int optionIndex)
    {
        ApplyQualityByDropdownIndex(optionIndex, true);
    }

    void OnFovChanged(float value)
    {
        ApplyCameraFov(value, true);
    }

    void OnSensitivityChanged(float value)
    {
        ApplyCameraSensitivity(value, true);
    }

    void OnMasterVolumeChanged(float value)
    {
        ApplyVolume(value, masterVolumeParam, MasterVolumeKey, true, true);
    }

    void OnGameSfxVolumeChanged(float value)
    {
        ApplyVolume(value, gameSfxVolumeParam, GameSfxVolumeKey, true, false);
    }

    void OnMenuSfxVolumeChanged(float value)
    {
        ApplyVolume(value, menuSfxVolumeParam, MenuSfxVolumeKey, true, false);
    }

    void OnGameMusicVolumeChanged(float value)
    {
        ApplyVolume(value, gameMusicVolumeParam, GameMusicVolumeKey, true, false);
    }

    void OnMenuMusicVolumeChanged(float value)
    {
        ApplyVolume(value, menuMusicVolumeParam, MenuMusicVolumeKey, true, false);
    }

    void OnCrosshairSizeChanged(float value)
    {
        ApplyCrosshairSize(value, true);
    }

    void OnCrosshairColorChanged(int index)
    {
        ApplyCrosshairColor(index, true);
    }

    void OnShowFpsChanged(bool isOn)
    {
        ApplyHudWidget(fpsWidget, isOn, ShowFpsKey, true);
    }

    void OnShowPingChanged(bool isOn)
    {
        ApplyHudWidget(pingWidget, isOn, ShowPingKey, true);
    }

    void OnShowSystemClockChanged(bool isOn)
    {
        ApplyHudWidget(systemClockWidget, isOn, ShowSystemClockKey, true);
    }

    void ApplyDisplayMode(int optionIndex, bool save)
    {
        int clampedOption = Mathf.Clamp(optionIndex, 0, 2);
        FullScreenMode mode = GetFullScreenModeFromOption(clampedOption);
        Screen.fullScreenMode = mode;

        if (save)
        {
            PlayerPrefs.SetInt(DisplayModeKey, clampedOption);
        }
    }

    void ApplyResolution(int optionIndex, bool save)
    {
        if (resList.Count == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(optionIndex, 0, resList.Count - 1);
        Vector2Int resolution = resList[clampedIndex];
        FullScreenMode mode = GetMode();
        Screen.SetResolution(resolution.x, resolution.y, mode);
        Screen.fullScreenMode = mode;

        if (save)
        {
            PlayerPrefs.SetInt(ResolutionWidthKey, resolution.x);
            PlayerPrefs.SetInt(ResolutionHeightKey, resolution.y);
        }
    }

    void ApplyQualityByDropdownIndex(int optionIndex, bool save)
    {
        int qualityIndex = GetQualityIndexFromDropdownOption(Mathf.Clamp(optionIndex, 0, Mathf.Max(0, graphicsQualityDropdown != null ? graphicsQualityDropdown.options.Count - 1 : QualitySettings.names.Length - 1)));
        qualityIndex = Mathf.Clamp(qualityIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        QualitySettings.SetQualityLevel(qualityIndex, true);

        if (save)
        {
            PlayerPrefs.SetInt(QualityLevelKey, qualityIndex);
        }
    }

    void ApplyCameraFov(float fov, bool save)
    {
        float clamped = Mathf.Clamp(fov, MinFov, MaxFov);
        PlayerController[] players = UnityEngine.Object.FindObjectsByType<PlayerController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null || !players[i].IsOwner || players[i].playerCamera == null)
            {
                continue;
            }

            players[i].playerCamera.fieldOfView = clamped;
        }

        if (save)
        {
            PlayerPrefs.SetFloat(CameraFovKey, clamped);
        }
    }

    void ApplyCameraSensitivity(float sensitivityPercent, bool save)
    {
        float clampedPercent = Mathf.Clamp(sensitivityPercent, MinSensitivityPercent, 100f);
        float steppedPercent = Mathf.Round(clampedPercent);
        float mappedSensitivity = SensMap(steppedPercent);

        PlayerController[] players = UnityEngine.Object.FindObjectsByType<PlayerController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null || !players[i].IsOwner)
            {
                continue;
            }

            players[i].mouseSensitivity = mappedSensitivity;
        }

        if (save)
        {
            PlayerPrefs.SetFloat(CameraSensitivityPercentKey, steppedPercent);
        }
    }

    void ApplyVolume(float percentValue, string exposedParamName, string prefKey, bool save, bool fallbackToListener)
    {
        float clamped = Mathf.Clamp(percentValue, 0f, 100f);
        float normalized = clamped / 100f;
        bool appliedMixer = false;

        if (audioMixer != null)
        {
            appliedMixer = TrySetMixerVolume(exposedParamName, normalized);

            if (!appliedMixer)
            {
                string alternateParamName = GetAlternateVolumeParamName(exposedParamName);
                appliedMixer = TrySetMixerVolume(alternateParamName, normalized);
            }
        }

        if (fallbackToListener && !appliedMixer)
        {
            AudioListener.volume = normalized;
        }

        if (save)
        {
            PlayerPrefs.SetFloat(prefKey, clamped);
        }
    }

    bool TrySetMixerVolume(string paramName, float normalized)
    {
        if (audioMixer == null || string.IsNullOrWhiteSpace(paramName))
        {
            return false;
        }

        return audioMixer.SetFloat(paramName.Trim(), LinearToDecibel(normalized));
    }

    string GetAlternateVolumeParamName(string paramName)
    {
        if (string.IsNullOrWhiteSpace(paramName))
        {
            return string.Empty;
        }

        string trimmed = paramName.Trim();
        if (trimmed.EndsWith("Volume", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed.Substring(0, trimmed.Length - "Volume".Length);
        }

        return trimmed + "Volume";
    }

    float LinearToDecibel(float linear)
    {
        if (linear <= 0.0001f)
        {
            return -80f;
        }

        return Mathf.Log10(linear) * 20f;
    }

    void ApplyCrosshairSize(float sliderValue, bool save)
    {
        float clamped = Mathf.Clamp(sliderValue, MinCrosshairSliderValue, MaxCrosshairSliderValue);
        float t = Mathf.InverseLerp(MinCrosshairSliderValue, MaxCrosshairSliderValue, clamped);
        float pixelSize = Mathf.Lerp(MinCrosshairPixelSize, MaxCrosshairPixelSize, t);

        if (crosshairRect != null)
        {
            Vector2 size = crosshairRect.sizeDelta;
            size.x = pixelSize;
            size.y = pixelSize;
            crosshairRect.sizeDelta = size;
        }

        if (save)
        {
            PlayerPrefs.SetFloat(CrosshairSizeKey, clamped);
        }
    }

    void ApplyCrosshairColor(int colorIndex, bool save)
    {
        int clamped = Mathf.Clamp(colorIndex, 0, CrossCols.Length - 1);

        if (crosshairGraphic != null)
        {
            crosshairGraphic.color = CrossCols[clamped];
        }

        if (save)
        {
            PlayerPrefs.SetInt(CrosshairColorKey, clamped);
        }
    }

    void ApplyHudWidget(GameObject widget, bool isVisible, string prefKey, bool save)
    {
        if (widget != null)
        {
            widget.SetActive(isVisible);
        }

        if (save)
        {
            PlayerPrefs.SetInt(prefKey, isVisible ? 1 : 0);
        }
    }

    void EnsureVolumeDefaults()
    {
        if (PlayerPrefs.GetInt(VolumeDefaultsInitializedKey, 0) == 1)
        {
            return;
        }

        bool hasAnyVolumeKey =
            PlayerPrefs.HasKey(MasterVolumeKey) ||
            PlayerPrefs.HasKey(GameSfxVolumeKey) ||
            PlayerPrefs.HasKey(MenuSfxVolumeKey) ||
            PlayerPrefs.HasKey(GameMusicVolumeKey) ||
            PlayerPrefs.HasKey(MenuMusicVolumeKey);

        bool allVolumesZero =
            Mathf.Approximately(PlayerPrefs.GetFloat(MasterVolumeKey, 0f), 0f) &&
            Mathf.Approximately(PlayerPrefs.GetFloat(GameSfxVolumeKey, 0f), 0f) &&
            Mathf.Approximately(PlayerPrefs.GetFloat(MenuSfxVolumeKey, 0f), 0f) &&
            Mathf.Approximately(PlayerPrefs.GetFloat(GameMusicVolumeKey, 0f), 0f) &&
            Mathf.Approximately(PlayerPrefs.GetFloat(MenuMusicVolumeKey, 0f), 0f);

        if (!hasAnyVolumeKey || allVolumesZero)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, DefaultVolumePercent);
            PlayerPrefs.SetFloat(GameSfxVolumeKey, DefaultVolumePercent);
            PlayerPrefs.SetFloat(MenuSfxVolumeKey, DefaultVolumePercent);
            PlayerPrefs.SetFloat(GameMusicVolumeKey, DefaultVolumePercent);
            PlayerPrefs.SetFloat(MenuMusicVolumeKey, DefaultVolumePercent);
        }

        PlayerPrefs.SetInt(VolumeDefaultsInitializedKey, 1);
    }
}

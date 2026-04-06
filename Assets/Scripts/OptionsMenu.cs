using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Manages graphics, audio, HUD, and control settings.
public class OptionsMenu : MonoBehaviour
{
    private enum PrefKey
    {
        DisplayMode,
        ResolutionWidth,
        ResolutionHeight,
        QualityLevel,
        CameraFov,
        SensPct,
        MasterVolume,
        GameSfx,
        MenuSfx,
        GameMusic,
        MenuMusic,
        CrosshairSize,
        CrosshairColor,
        ShowFps,
        ShowPing,
        ShowClock
    }

    private enum ModeOption
    {
        Windowed = 0,
        Borderless = 1,
        Fullscreen = 2
    }

    private enum MixerParam
    {
        Master,
        GameSfx,
        MenuSfx,
        GameMusic,
        MenuMusic
    }

    private static readonly Dictionary<PrefKey, string> PrefNames = new Dictionary<PrefKey, string>
    {
        { PrefKey.DisplayMode, "opt_display_mode" },
        { PrefKey.ResolutionWidth, "opt_resolution_width" },
        { PrefKey.ResolutionHeight, "opt_resolution_height" },
        { PrefKey.QualityLevel, "opt_quality_level" },
        { PrefKey.CameraFov, "opt_camera_fov" },
        { PrefKey.SensPct, "opt_camera_sensitivity_pct" },
        { PrefKey.MasterVolume, "opt_master_volume" },
        { PrefKey.GameSfx, "opt_game_sfx_volume" },
        { PrefKey.MenuSfx, "opt_menu_sfx_volume" },
        { PrefKey.GameMusic, "opt_game_music_volume" },
        { PrefKey.MenuMusic, "opt_menu_music_volume" },
        { PrefKey.CrosshairSize, "opt_crosshair_size" },
        { PrefKey.CrosshairColor, "opt_crosshair_color" },
        { PrefKey.ShowFps, "opt_show_fps" },
        { PrefKey.ShowPing, "opt_show_ping" },
        { PrefKey.ShowClock, "opt_show_system_clock" }
    };

    // Returns the PlayerPrefs key name for an options entry.
    private static string Pref(PrefKey key)
    {
        return PrefNames[key];
    }

    // Reads an integer option from PlayerPrefs.
    private static int PrefGetInt(PrefKey key, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(Pref(key), defaultValue);
    }

    // Writes an integer option to PlayerPrefs.
    private static void PrefSetInt(PrefKey key, int value)
    {
        PlayerPrefs.SetInt(Pref(key), value);
    }

    // Reads a float option from PlayerPrefs.
    private static float PrefGetFloat(PrefKey key, float defaultValue = 0f)
    {
        return PlayerPrefs.GetFloat(Pref(key), defaultValue);
    }

    // Writes a float option to PlayerPrefs.
    private static void PrefSetFloat(PrefKey key, float value)
    {
        PlayerPrefs.SetFloat(Pref(key), value);
    }

    // Returns whether a PlayerPrefs option key already exists.
    private static bool PrefHasKey(PrefKey key)
    {
        return PlayerPrefs.HasKey(Pref(key));
    }

    private const int BestResolutionIndex = 0;
    private const int DefaultHudOn = 1;
    private const int DefaultTabIndex = 0;
    private const float MinCrosshairSize = 1f;
    private const float DefaultCrosshairVal = 1f;
    private const float MaxCrosshairSize = 10f;
    private const float MinCrosshairPx = 5f;
    private const float MaxCrosshairPx = 50f;
    private const float MinFov = 60f;
    private const float MaxFov = 100f;
    private const float DefaultFov = 80f;
    private const float MinSensitivity = 0.01f;
    private const float MinSensitivityPercent = 1f;
    private const float DefaultSensitivityPercent = 50f;
    private const float DefaultVolumePercent = 100f;
    private const float FullscreenAspectTolerance = 0.01f;
    private const float MinLinearVolume = 0.0001f;
    private const float MuteDb = -80f;
    [SerializeField] private TMP_Dropdown displayModeDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown graphicsQualityDropdown;
    [SerializeField] private Slider cameraFovSlider;
    [SerializeField] private Slider cameraSensitivitySlider;
    [SerializeField] private Slider masterVolSlider;
    [SerializeField] private Slider gameSfxSlider;
    [SerializeField] private Slider menuSfxSlider;
    [SerializeField] private Slider gameMusicSlider;
    [SerializeField] private Slider menuMusicSlider;
    [SerializeField] private AudioMixer audioMixer;
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
    [SerializeField] private PauseMenu pauseMenuRef;
    [SerializeField] private MainMenu mainMenuRef;
    [Serializable]

    // Describes one options tab with its button and panel.
    public class Tab
    {
        // References the button that activates this tab.
        public Button button;

        // References the panel shown when this tab is active.
        public GameObject panel;

        // References the label graphic tinted by active state.
        public Graphic labelGraphic;
    }

    [SerializeField] private Tab[] tabs = Array.Empty<Tab>();
    private Color activeTextColor = new Color32(51, 51, 51, 255);
    private Color inactiveTextColor = new Color32(200, 150, 50, 255);
    private UnityAction[] tabClickHandlers;
    private bool tabsRegistered;
    private InputActions input;
    private readonly List<Vector2Int> allResList = new List<Vector2Int>();
    private readonly List<Vector2Int> resList = new List<Vector2Int>();
    private ModeOption displayModeOption = ModeOption.Borderless;
    private int manualResIndex;
    private PlayerMovement ownerPlayer;
    private bool applyingLoadedPrefs;
    
    private static readonly Color[] CrossCols=
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

    // Initializes references and loads saved settings at runtime.
    void Awake()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        EnsureSetup();
        input = new InputActions();
        CacheRefs();
        PopulateResolutionDropdown();
        PopulateQualityDropdown();
        EnableDropdownHelpers();
        LoadPrefs();
    }

    // Enables pause input and tab button handlers.
    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        input.System.Pause.performed += OnPauseInput;
        input.System.Enable();
        RegisterTabs();
        ShowTab(DefaultTabIndex);
    }

    // Disables pause input and unregisters tab button handlers.
    void OnDisable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (input != null)
        {
            input.System.Pause.performed -= OnPauseInput;
            input.System.Disable();
        }

        PlayerPrefs.Save();
        UnregisterTabs();
    }

    // Disposes runtime input resources on destroy.
    void OnDestroy()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (input != null)
        {
            input.Dispose();
        }
    }

    // Converts sensitivity percent to runtime mouse sensitivity.
    public static float SensMap(float percent)
    {
        float clampedPercent = Mathf.Clamp(percent, MinSensitivityPercent, 100f);
        float steppedPercent = Mathf.Round(clampedPercent);
        return Mathf.Max(MinSensitivity, steppedPercent / 100f);
    }

    // Reads the saved field-of-view value.
    public static float SavedFov(float fallback)
    {
        float defaultValue = Mathf.Clamp(fallback, MinFov, MaxFov);
        return Mathf.Clamp(PrefGetFloat(PrefKey.CameraFov, defaultValue), MinFov, MaxFov);
    }

    // Reads the saved sensitivity percentage.
    public static float SavedSensPct(float fallbackPercent)
    {
        float defaultValue = Mathf.Clamp(fallbackPercent, MinSensitivityPercent, 100f);
        return Mathf.Clamp(PrefGetFloat(PrefKey.SensPct, defaultValue), MinSensitivityPercent, 100f);
    }

    // Stops game music by muting the game music mixer channel.
    public static void StopGameMusic()
    {
        OptionsMenu[] menus = UnityEngine.Object.FindObjectsByType<OptionsMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i] == null || menus[i].audioMixer == null)
            {
                continue;
            }

            menus[i].audioMixer.SetFloat(MixerName(MixerParam.GameMusic), MuteDb);
            return;
        }

        throw new InvalidOperationException("OptionsMenu.StopGameMusic failed: no OptionsMenu with an assigned audioMixer was found.");
    }

    // Applies a quality option selected from the UI.
    public void OnQualityChanged(int value) => ApplyQualityOption(value, true);

    // Applies a crosshair color selected from the UI.
    public void OnCrosshairColorChanged(int value) => ApplyCrosshairColor(value, true);

    // Applies a field-of-view slider change.
    public void OnFovChanged(float value) => ApplyCameraFov(value, true);

    // Applies a sensitivity slider change.
    public void OnSensChanged(float value) => ApplyCameraSensitivity(value, true);

    // Applies a master volume slider change.
    public void OnMasterVolChanged(float value) => ApplyVolume(value, MixerName(MixerParam.Master), PrefKey.MasterVolume, true, true);

    // Applies a game SFX volume slider change.
    public void OnGameSfxChanged(float value) => ApplyVolume(value, MixerName(MixerParam.GameSfx), PrefKey.GameSfx, true, false);

    // Applies a menu SFX volume slider change.
    public void OnMenuSfxChanged(float value) => ApplyVolume(value, MixerName(MixerParam.MenuSfx), PrefKey.MenuSfx, true, false);

    // Applies a game music volume slider change.
    public void OnGameMusicChanged(float value) => ApplyVolume(value, MixerName(MixerParam.GameMusic), PrefKey.GameMusic, true, false);

    // Applies a menu music volume slider change.
    public void OnMenuMusicChanged(float value) => ApplyVolume(value, MixerName(MixerParam.MenuMusic), PrefKey.MenuMusic, true, false);

    // Applies a crosshair size slider change.
    public void OnCrosshairSizeChanged(float value) => ApplyCrosshairSize(value, true);

    // Applies the FPS widget visibility toggle.
    public void OnShowFpsChanged(bool value) => ApplyHudWidget(fpsWidget, value, PrefKey.ShowFps, true);

    // Applies the ping widget visibility toggle.
    public void OnShowPingChanged(bool value) => ApplyHudWidget(pingWidget, value, PrefKey.ShowPing, true);

    // Applies the system clock widget visibility toggle.
    public void OnShowClockChanged(bool value) => ApplyHudWidget(systemClockWidget, value, PrefKey.ShowClock, true);

    // Collects supported resolutions and builds the active list.
    void PopulateResolutionDropdown()
    {
        allResList.Clear();
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

            allResList.Add(new Vector2Int(width, height));
        }

        if (allResList.Count == 0)
        {
            allResList.Add(new Vector2Int(Screen.width, Screen.height));
        }

        allResList.Sort((a, b) =>
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
        
        RebuildResolutionList(GetModeOption());
    }

    // Rebuilds the selectable resolution list for the selected display mode.
    void RebuildResolutionList(ModeOption displayModeOption)
    {
        resList.Clear();

        if (IsFullscreenOption(displayModeOption))
        {
            Vector2Int screenRes = CurrentScreenResolution();
            float screenAspect = Aspect(screenRes);

            for (int i = 0; i < allResList.Count; i++)
            {
                Vector2Int current = allResList[i];

                if (Mathf.Abs(Aspect(current) - screenAspect) <= FullscreenAspectTolerance)
                {
                    resList.Add(current);
                }
            }
        }
        else
        {
            resList.AddRange(allResList);
        }

        if (resList.Count == 0)
        {
            resList.AddRange(allResList);
        }

        RefreshResOptions();
    }

    // Refreshes the resolution dropdown option labels.
    void RefreshResOptions()
    {
        List<string> options = new List<string>(resList.Count);

        for (int i = 0; i < resList.Count; i++)
        {
            Vector2Int r = resList[i];
            options.Add($"{r.x} x {r.y}");
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    // Returns the current screen resolution fallback.
    Vector2Int CurrentScreenResolution()
    {
        Resolution currentResolution = Screen.currentResolution;

        if (currentResolution.width > 0 && currentResolution.height > 0)
        {
            return new Vector2Int(currentResolution.width, currentResolution.height);
        }

        return new Vector2Int(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
    }

    // Returns the aspect ratio for a resolution value.
    float Aspect(Vector2Int resolution)
    {
        return resolution.y <= 0 ? 1f : (float)resolution.x / resolution.y;
    }

    // Populates quality dropdown options from QualitySettings.
    void PopulateQualityDropdown()
    {
        graphicsQualityDropdown.ClearOptions();
        graphicsQualityDropdown.AddOptions(new List<string>(QualitySettings.names));
    }

    // Ensures dropdown helper components exist on all dropdown controls.
    void EnableDropdownHelpers()
    {
        AddDropdownHelper(displayModeDropdown);
        AddDropdownHelper(resolutionDropdown);
        AddDropdownHelper(graphicsQualityDropdown);
        AddDropdownHelper(crosshairColorDropdown);
    }

    // Adds a DropdownHelper component when missing.
    void AddDropdownHelper(TMP_Dropdown dropdown)
    {
        if (dropdown.GetComponent<DropdownHelper>() == null)
        {
            dropdown.gameObject.AddComponent<DropdownHelper>();
        }
    }

    // Switches back to the default tab.
    public void ResetTab()
    {
        ShowTab(DefaultTabIndex);
    }

    // Shows a tab by index.
    public void ShowTab(int index)
    {
        int clampedIndex = Mathf.Clamp(index, 0, tabs.Length - 1);

        for (int i = 0; i < tabs.Length; i++)
        {
            bool isActive = i == clampedIndex;
            Tab tab = tabs[i];
            tab.panel.SetActive(isActive);
            tab.button.interactable=!isActive;
            tab.labelGraphic.color = isActive ? activeTextColor : inactiveTextColor;
        }
    }

    // Registers click listeners for tab buttons once.
    void RegisterTabs()
    {
        if (tabsRegistered)
        {
            return;
        }

        tabClickHandlers = new UnityAction[tabs.Length];

        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            tabClickHandlers[i] = () => ShowTab(index);
            tabs[i].button.onClick.AddListener(tabClickHandlers[i]);
        }

        tabsRegistered = true;
    }

    // Unregisters previously bound tab button listeners.
    void UnregisterTabs()
    {
        if (!tabsRegistered)
        {
            return;
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabClickHandlers[i] == null)
            {
                continue;
            }

            tabs[i].button.onClick.RemoveListener(tabClickHandlers[i]);
        }

        tabsRegistered = false;
    }

    // Closes options from pause input when appropriate.
    void OnPauseInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        if (pauseMenuRef != null && PauseMenu.isOpen)
        {
            return;
        }

        if (mainMenuRef != null)
        {
            mainMenuRef.CloseOptions();
        }
    }

    // Caches optional menu references from the active scene.
    void CacheRefs()
    {
        if (pauseMenuRef == null)
        {
            pauseMenuRef = UnityEngine.Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
        }

        if (mainMenuRef == null)
        {
            mainMenuRef = UnityEngine.Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        }
    }

    // Loads all persisted options and applies them to runtime state.
    void LoadPrefs()
    {
        applyingLoadedPrefs = true;

        try
        {
            // Apply display mode before resolution so the filtered list is valid.
            int savedModeOption = PrefHasKey(PrefKey.DisplayMode) ? PrefGetInt(PrefKey.DisplayMode) : (int)ModeOption.Borderless;
            displayModeOption = ClampModeOption(savedModeOption);
            displayModeDropdown.SetValueWithoutNotify((int)displayModeOption);
            displayModeDropdown.RefreshShownValue();
            ApplyDisplayMode(displayModeOption, false);
            RebuildResolutionList(displayModeOption);

            // Restore resolution index against the mode-filtered resolution list.
            int resolutionIndex = GetSavedResIndex();
            manualResIndex = resolutionIndex;
            resolutionDropdown.SetValueWithoutNotify(resolutionIndex);
            resolutionDropdown.RefreshShownValue();
            ApplyResMode(displayModeOption, wasBorderless: false);

            // Restore quality using dropdown-safe and quality-safe clamping.
            int savedQualityIndex = PrefGetInt(PrefKey.QualityLevel, QualitySettings.GetQualityLevel());
            savedQualityIndex = Mathf.Clamp(savedQualityIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            int qualityDropdownIndex = GetQualityDropdownIndex(savedQualityIndex);
            qualityDropdownIndex = Mathf.Clamp(qualityDropdownIndex, 0, Mathf.Max(0, graphicsQualityDropdown.options.Count - 1));
            graphicsQualityDropdown.SetValueWithoutNotify(qualityDropdownIndex);
            graphicsQualityDropdown.RefreshShownValue();
            ApplyQualityOption(qualityDropdownIndex, false);

            // Restore owner-camera controls with explicit runtime slider ranges.
            float fov = SavedFov(DefaultFov);
            cameraFovSlider.minValue = MinFov;
            cameraFovSlider.maxValue = MaxFov;
            cameraFovSlider.SetValueWithoutNotify(fov);
            ApplyCameraFov(fov, false);
            float sensitivityPercent = SavedSensPct(DefaultSensitivityPercent);
            cameraSensitivitySlider.minValue = MinSensitivityPercent;
            cameraSensitivitySlider.maxValue = 100f;
            cameraSensitivitySlider.wholeNumbers = true;
            cameraSensitivitySlider.SetValueWithoutNotify(sensitivityPercent);
            ApplyCameraSensitivity(sensitivityPercent, false);

            // Restore all mixer-controlled volume channels as percentages.
            float masterVolume = Mathf.Clamp(PrefGetFloat(PrefKey.MasterVolume, DefaultVolumePercent), 0f, 100f);
            masterVolSlider.minValue = 0f;
            masterVolSlider.maxValue = 100f;
            masterVolSlider.SetValueWithoutNotify(masterVolume);
            ApplyVolume(masterVolume, MixerName(MixerParam.Master), PrefKey.MasterVolume, false, true);
            float gameSfxVolume = Mathf.Clamp(PrefGetFloat(PrefKey.GameSfx, DefaultVolumePercent), 0f, 100f);
            gameSfxSlider.minValue = 0f;
            gameSfxSlider.maxValue = 100f;
            gameSfxSlider.SetValueWithoutNotify(gameSfxVolume);
            ApplyVolume(gameSfxVolume, MixerName(MixerParam.GameSfx), PrefKey.GameSfx, false, false);
            float menuSfxVolume = Mathf.Clamp(PrefGetFloat(PrefKey.MenuSfx, DefaultVolumePercent), 0f, 100f);
            menuSfxSlider.minValue = 0f;
            menuSfxSlider.maxValue = 100f;
            menuSfxSlider.SetValueWithoutNotify(menuSfxVolume);
            ApplyVolume(menuSfxVolume, MixerName(MixerParam.MenuSfx), PrefKey.MenuSfx, false, false);
            float gameMusicVolume = Mathf.Clamp(PrefGetFloat(PrefKey.GameMusic, DefaultVolumePercent), 0f, 100f);
            gameMusicSlider.minValue = 0f;
            gameMusicSlider.maxValue = 100f;
            gameMusicSlider.SetValueWithoutNotify(gameMusicVolume);
            ApplyVolume(gameMusicVolume, MixerName(MixerParam.GameMusic), PrefKey.GameMusic, false, false);
            float menuMusicVolume = Mathf.Clamp(PrefGetFloat(PrefKey.MenuMusic, DefaultVolumePercent), 0f, 100f);
            menuMusicSlider.minValue = 0f;
            menuMusicSlider.maxValue = 100f;
            menuMusicSlider.SetValueWithoutNotify(menuMusicVolume);
            ApplyVolume(menuMusicVolume, MixerName(MixerParam.MenuMusic), PrefKey.MenuMusic, false, false);

            // Restore crosshair shape and color settings.
            float crosshairSize = Mathf.Clamp(PrefGetFloat(PrefKey.CrosshairSize, DefaultCrosshairVal), MinCrosshairSize, MaxCrosshairSize);
            crosshairSizeSlider.minValue = MinCrosshairSize;
            crosshairSizeSlider.maxValue = MaxCrosshairSize;
            crosshairSizeSlider.SetValueWithoutNotify(crosshairSize);
            ApplyCrosshairSize(crosshairSize, false);
            int crosshairColorIndex = Mathf.Clamp(PrefGetInt(PrefKey.CrosshairColor, 0), 0, CrossCols.Length - 1);
            crosshairColorDropdown.SetValueWithoutNotify(crosshairColorIndex);
            crosshairColorDropdown.RefreshShownValue();
            ApplyCrosshairColor(crosshairColorIndex, false);

            // Restore HUD visibility toggles as persisted integer flags.
            bool showFps = PrefGetInt(PrefKey.ShowFps, DefaultHudOn) == 1;
            showFpsToggle.SetValue(showFps, false);
            ApplyHudWidget(fpsWidget, showFps, PrefKey.ShowFps, false);
            bool showPing = PrefGetInt(PrefKey.ShowPing, DefaultHudOn) == 1;
            showPingToggle.SetValue(showPing, false);
            ApplyHudWidget(pingWidget, showPing, PrefKey.ShowPing, false);
            bool showSystemClock = PrefGetInt(PrefKey.ShowClock, DefaultHudOn) == 1;
            showSystemClockToggle.SetValue(showSystemClock, false);
            ApplyHudWidget(systemClockWidget, showSystemClock, PrefKey.ShowClock, false);
        }
        finally
        {
            // Re-enable save-on-change callbacks after load/apply completes.
            applyingLoadedPrefs = false;
        }
    }

    // Returns the saved resolution index or the closest fallback.
    int GetSavedResIndex()
    {
        if (resList.Count == 0)
        {
            return 0;
        }

        if (!PrefHasKey(PrefKey.ResolutionWidth) || !PrefHasKey(PrefKey.ResolutionHeight))
        {
            return 0;
        }

        int defaultWidth = Screen.width;
        int defaultHeight = Screen.height;
        int savedWidth = PrefGetInt(PrefKey.ResolutionWidth, defaultWidth);
        int savedHeight = PrefGetInt(PrefKey.ResolutionHeight, defaultHeight);

        for (int i = 0; i < resList.Count; i++)
        {
            if (resList[i].x == savedWidth && resList[i].y == savedHeight)
            {
                return i;
            }
        }

        int closestIndex = 0;
        long closestDistance = long.MaxValue;

        // Choose nearest available resolution when exact saved size is missing.
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

    // Clamps an integer to a valid display mode option.
    ModeOption ClampModeOption(int option)
    {
        return (ModeOption)Mathf.Clamp(option, (int)ModeOption.Windowed, (int)ModeOption.Fullscreen);
    }

    // Maps a mode option enum to Unity fullscreen mode.
    FullScreenMode GetDisplayModeFromOption(ModeOption option)
    {
        switch (option)
        {
        case ModeOption.Borderless: return FullScreenMode.FullScreenWindow;
        case ModeOption.Fullscreen:
#if UNITY_STANDALONE_WIN
            return FullScreenMode.ExclusiveFullScreen;
#else
            return FullScreenMode.FullScreenWindow;
#endif
        default: return FullScreenMode.Windowed;
        }
    }

    // Returns the currently selected display mode option.
    ModeOption GetModeOption()
    {
        return ClampModeOption(displayModeDropdown.value);
    }

    // Returns the Unity fullscreen mode for the selected option.
    FullScreenMode GetMode()
    {
        return GetDisplayModeFromOption(GetModeOption());
    }

    // Returns whether the selected option is borderless mode.
    bool IsBorderlessOption(ModeOption option)
    {
        return option == ModeOption.Borderless;
    }

    // Returns whether the selected option is fullscreen mode.
    bool IsFullscreenOption(ModeOption option)
    {
        return option == ModeOption.Fullscreen;
    }

    // Returns the clamped current resolution dropdown index.
    int GetCurrentResIndex()
    {
        return Mathf.Clamp(resolutionDropdown.value, 0, Mathf.Max(0, resList.Count - 1));
    }

    // Sets whether the resolution dropdown is interactable.
    void SetResolutionInteractable(bool isInteractable)
    {
        resolutionDropdown.interactable = isInteractable;
    }

    // Sets the resolution dropdown value safely.
    void SetResolutionValue(int index)
    {
        if (resList.Count == 0)
        {
            throw new InvalidOperationException("OptionsMenu state failed: resolution list is empty.");
        }

        int clampedIndex = Mathf.Clamp(index, 0, resList.Count - 1);
        resolutionDropdown.SetValueWithoutNotify(clampedIndex);
        resolutionDropdown.RefreshShownValue();
    }

    // Applies resolution behavior for the selected display mode.
    void ApplyResMode(ModeOption displayModeOption, bool wasBorderless)
    {
        if (resList.Count == 0)
        {
            SetResolutionInteractable(!IsBorderlessOption(displayModeOption));
            return;
        }

        if (IsBorderlessOption(displayModeOption))
        {
            SetResolutionInteractable(false);
            SetResolutionValue(BestResolutionIndex);
            ApplyResolution(BestResolutionIndex, false);
            return;
        }

        // Leaving borderless returns to the last manual selection.
        SetResolutionInteractable(true);
        int targetIndex = wasBorderless ? Mathf.Clamp(manualResIndex, 0, resList.Count - 1) : GetCurrentResIndex();
        SetResolutionValue(targetIndex);
        ApplyResolution(targetIndex, false);
    }

    // Returns a valid quality dropdown index.
    int GetQualityDropdownIndex(int qualityIndex)
    {
        if (graphicsQualityDropdown.options.Count == 0)
        {
            throw new InvalidOperationException("OptionsMenu setup failed: graphicsQualityDropdown options are unavailable.");
        }

        return Mathf.Clamp(qualityIndex, 0, graphicsQualityDropdown.options.Count - 1);
    }

    // Maps dropdown option index to quality level index.
    int GetQualityFromOption(int optionIndex)
    {
        return Mathf.Clamp(optionIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
    }

    // Applies a display mode change from the dropdown.
    public void OnDisplayModeChanged(int optionIndex)
    {
        ModeOption clampedOption = ClampModeOption(optionIndex);
        bool wasBorderless = IsBorderlessOption(displayModeOption);
        bool isBorderless = IsBorderlessOption(clampedOption);

        if (!wasBorderless && isBorderless)
        {
            manualResIndex = GetCurrentResIndex();
        }

        ApplyDisplayMode(clampedOption, true);
        RebuildResolutionList(clampedOption);
        ApplyResMode(clampedOption, wasBorderless);
        displayModeOption = clampedOption;
    }

    // Applies a resolution change from the dropdown.
    public void OnResolutionChanged(int optionIndex)
    {
        if (IsBorderlessOption(displayModeOption))
        {
            return;
        }

        if (resList.Count == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(optionIndex, 0, resList.Count - 1);
        manualResIndex = clampedIndex;
        ApplyResolution(clampedIndex, true);
    }

    // Applies the selected display mode and optionally saves it.
    void ApplyDisplayMode(ModeOption option, bool save)
    {
        FullScreenMode mode = GetDisplayModeFromOption(option);
        Screen.fullScreenMode = mode;

        if (save)
        {
            PrefSetInt(PrefKey.DisplayMode, (int)option);
        }
    }

    // Applies the selected resolution and optionally saves it.
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
            PrefSetInt(PrefKey.ResolutionWidth, resolution.x);
            PrefSetInt(PrefKey.ResolutionHeight, resolution.y);
        }
    }

    // Applies quality level and optionally saves it.
    void ApplyQualityOption(int optionIndex, bool save)
    {
        int qualityIndex = GetQualityFromOption(Mathf.Clamp(optionIndex, 0, Mathf.Max(0, graphicsQualityDropdown.options.Count - 1)));
        qualityIndex = Mathf.Clamp(qualityIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        QualitySettings.SetQualityLevel(qualityIndex, true);

        if (save)
        {
            PrefSetInt(PrefKey.QualityLevel, qualityIndex);
        }
    }

    // Applies camera FOV to owner camera and optionally saves it.
    void ApplyCameraFov(float fov, bool save)
    {
        float clamped = Mathf.Clamp(fov, MinFov, MaxFov);
        PlayerMovement owner = GetOwnerPlayer();

        if (owner != null && owner.PlayerCamera != null)
        {
            owner.PlayerCamera.fieldOfView = clamped;
        }

        if (save && !applyingLoadedPrefs)
        {
            PrefSetFloat(PrefKey.CameraFov, clamped);
        }
    }

    // Applies camera sensitivity to owner movement and optionally saves it.
    void ApplyCameraSensitivity(float sensitivityPercent, bool save)
    {
        float clampedPercent = Mathf.Clamp(sensitivityPercent, MinSensitivityPercent, 100f);
        float steppedPercent = Mathf.Round(clampedPercent);
        float mappedSensitivity = SensMap(steppedPercent);
        PlayerMovement owner = GetOwnerPlayer();

        if (owner != null)
        {
            owner.mouseSensitivity = mappedSensitivity;
        }

        if (save && !applyingLoadedPrefs)
        {
            PrefSetFloat(PrefKey.SensPct, steppedPercent);
        }
    }

    // Applies a volume percentage to mixer and optionally saves it.
    void ApplyVolume(float percentValue, string exposedParamName, PrefKey prefKey, bool save, bool fallbackToListener)
    {
        float clamped = Mathf.Clamp(percentValue, 0f, 100f);
        float normalized = clamped / 100f;
        bool appliedMixer = SetMixerVol(exposedParamName, normalized);

        if (fallbackToListener && !appliedMixer)
        {
            AudioListener.volume = normalized;
        }

        if (save)
        {
            PrefSetFloat(prefKey, clamped);
        }
    }

    // Sets a mixer parameter from normalized linear volume.
    bool SetMixerVol(string paramName, float normalized)
    {
        if (string.IsNullOrWhiteSpace(paramName))
        {
            throw new ArgumentException("OptionsMenu.SetMixerVol failed: mixer parameter name is empty.", nameof(paramName));
        }

        return audioMixer.SetFloat(paramName.Trim(), LinearToDecibel(normalized));
    }

    // Returns mixer exposed parameter name by enum value.
    static string MixerName(MixerParam param)
    {
        switch (param)
        {
        case MixerParam.Master: return "Master";
        case MixerParam.GameSfx: return "GameSFX";
        case MixerParam.MenuSfx: return "MenuSFX";
        case MixerParam.GameMusic: return "GameMusic";
        case MixerParam.MenuMusic: return "MenuMusic";
        default: throw new ArgumentOutOfRangeException(nameof(param), param, null);
        }
    }

    // Finds and caches the owner PlayerMovement component.
    PlayerMovement GetOwnerPlayer()
    {
        if (ownerPlayer != null && ownerPlayer.IsOwner)
        {
            return ownerPlayer;
        }

        PlayerMovement[] players = UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].IsOwner)
            {
                ownerPlayer = players[i];
                return ownerPlayer;
            }
        }

        ownerPlayer = null;
        return null;
    }

    // Converts linear volume to decibel value for the mixer.
    float LinearToDecibel(float linear)
    {
        if (linear <= MinLinearVolume)
        {
            return MuteDb;
        }

        return Mathf.Log10(linear) * 20f;
    }

    // Applies crosshair size and optionally saves it.
    void ApplyCrosshairSize(float sliderValue, bool save)
    {
        float clamped = Mathf.Clamp(sliderValue, MinCrosshairSize, MaxCrosshairSize);
        float t = Mathf.InverseLerp(MinCrosshairSize, MaxCrosshairSize, clamped);
        float pixelSize = Mathf.Lerp(MinCrosshairPx, MaxCrosshairPx, t);

        if (crosshairRect != null)
        {
            Vector2 size = crosshairRect.sizeDelta;
            size.x = pixelSize;
            size.y = pixelSize;
            crosshairRect.sizeDelta = size;
        }

        if (save && !applyingLoadedPrefs)
        {
            PrefSetFloat(PrefKey.CrosshairSize, clamped);
        }
    }

    // Applies crosshair color and optionally saves it.
    void ApplyCrosshairColor(int colorIndex, bool save)
    {
        int clamped = Mathf.Clamp(colorIndex, 0, CrossCols.Length - 1);

        if (crosshairGraphic != null)
        {
            crosshairGraphic.color = CrossCols[clamped];
        }

        if (save)
        {
            PrefSetInt(PrefKey.CrosshairColor, clamped);
        }
    }

    // Applies HUD widget visibility and optionally saves it.
    void ApplyHudWidget(GameObject widget, bool isVisible, PrefKey prefKey, bool save)
    {
        if (widget != null)
        {
            widget.SetActive(isVisible);
        }

        if (save)
        {
            PrefSetInt(prefKey, isVisible ? 1 : 0);
        }
    }

    // Validates required options menu references.
    void EnsureSetup()
    {
        if (displayModeDropdown == null)
        {
            throw new InvalidOperationException("OptionsMenu setup failed: displayModeDropdown reference is missing.");
        }

        if (resolutionDropdown == null)
        {
            throw new InvalidOperationException("OptionsMenu setup failed: resolutionDropdown reference is missing.");
        }

        if (graphicsQualityDropdown == null)
        {
            throw new InvalidOperationException("OptionsMenu setup failed: graphicsQualityDropdown reference is missing.");
        }

        if (cameraFovSlider == null)
        {
            throw new InvalidOperationException("OptionsMenu setup failed: cameraFovSlider reference is missing.");
        }

        if (cameraSensitivitySlider == null)
        {
            throw new InvalidOperationException("OptionsMenu setup failed: cameraSensitivitySlider reference is missing.");
        }

        if (masterVolSlider == null || gameSfxSlider == null || menuSfxSlider == null || gameMusicSlider == null || menuMusicSlider == null)
        {
            throw new InvalidOperationException("OptionsMenu setup failed: one or more audio slider references are missing.");
        }

        if (audioMixer == null)
        {
            throw new InvalidOperationException("OptionsMenu setup failed: audioMixer reference is missing.");
        }

        if (crosshairSizeSlider == null || crosshairColorDropdown == null)
        {
            throw new InvalidOperationException("OptionsMenu setup failed: crosshair controls are missing.");
        }

        if (showFpsToggle == null || showPingToggle == null || showSystemClockToggle == null)
        {
            throw new InvalidOperationException("OptionsMenu setup failed: HUD toggle references are missing.");
        }

        if (tabs == null || tabs.Length == 0)
        {
            throw new InvalidOperationException("OptionsMenu setup failed: tabs are not configured.");
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null)
            {
                throw new InvalidOperationException($"OptionsMenu setup failed: tabs[{i}] is null.");
            }

            if (tabs[i].button == null)
            {
                throw new InvalidOperationException($"OptionsMenu setup failed: tabs[{i}].button is missing.");
            }

            if (tabs[i].panel == null)
            {
                throw new InvalidOperationException($"OptionsMenu setup failed: tabs[{i}].panel is missing.");
            }

            if (tabs[i].labelGraphic == null)
            {
                throw new InvalidOperationException($"OptionsMenu setup failed: tabs[{i}].labelGraphic is missing.");
            }
        }
    }
}

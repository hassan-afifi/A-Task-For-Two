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
        ShowClock,
        VolumeInit
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
        { PrefKey.ShowClock, "opt_show_system_clock" },
        { PrefKey.VolumeInit, "opt_volume_defaults_initialized_v1" }
    };

    private static string Pref(PrefKey key)
    {
        return PrefNames[key];
    }

    private static int PrefGetInt(PrefKey key, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(Pref(key), defaultValue);
    }

    private static void PrefSetInt(PrefKey key, int value)
    {
        PlayerPrefs.SetInt(Pref(key), value);
    }

    private static float PrefGetFloat(PrefKey key, float defaultValue = 0f)
    {
        return PlayerPrefs.GetFloat(Pref(key), defaultValue);
    }

    private static void PrefSetFloat(PrefKey key, float value)
    {
        PlayerPrefs.SetFloat(Pref(key), value);
    }

    private static bool PrefHasKey(PrefKey key)
    {
        return PlayerPrefs.HasKey(Pref(key));
    }

    private const int BestResolutionIndex = 0;
    private const int DefaultHudOn = 1;
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
    private const float DefaultVolumePercent = 50f;
    private const float FullscreenAspectTolerance = 0.01f;
    private const float MinLinearVolume = 0.0001f;
    private const float MuteDb = -80f;
    private const string VolumeSuffix = "Volume";

    [Header("Video")]
    [SerializeField] private TMP_Dropdown displayModeDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown graphicsQualityDropdown;
    [SerializeField] private Slider cameraFovSlider;
    [SerializeField] private Slider cameraSensitivitySlider;

    [Header("Audio")]
    [SerializeField] private Slider masterVolSlider;
    [SerializeField] private Slider gameSfxSlider;
    [SerializeField] private Slider menuSfxSlider;
    [SerializeField] private Slider gameMusicSlider;
    [SerializeField] private Slider menuMusicSlider;
    [SerializeField] private AudioMixer audioMixer;

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

    [Header("External")]
    [SerializeField] private PauseMenu pauseMenuRef;
    [SerializeField] private MainMenu mainMenuRef;

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
        if (!Application.isPlaying)
        {
            return;
        }

        EnsureVolumeDefaults();
        CacheRefs();
        PopulateResolutionDropdown();
        PopulateQualityDropdown();
        EnableDropdownHelpers();
        LoadPrefs();
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

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
        if (!Application.isPlaying)
        {
            return;
        }

        if (input != null)
        {
            input.System.Pause.performed -= OnPauseInput;
            input.System.Disable();
        }

        UnregisterTabs();
    }

    void OnDestroy()
    {
        if (!Application.isPlaying)
        {
            return;
        }

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
        return Mathf.Clamp(PrefGetFloat(PrefKey.CameraFov, defaultValue), MinFov, MaxFov);
    }

    public static float SavedSensPct(float fallbackPercent)
    {
        float defaultValue = Mathf.Clamp(fallbackPercent, MinSensitivityPercent, 100f);
        return Mathf.Clamp(PrefGetFloat(PrefKey.SensPct, defaultValue), MinSensitivityPercent, 100f);
    }

    public void OnQualityChanged(int value) => ApplyQualityOption(value, true);
    public void OnCrosshairColorChanged(int value) => ApplyCrosshairColor(value, true);
    public void OnFovChanged(float value) => ApplyCameraFov(value, true);
    public void OnSensChanged(float value) => ApplyCameraSensitivity(value, true);
    public void OnMasterVolChanged(float value) => ApplyVolume(value, MixerName(MixerParam.Master), PrefKey.MasterVolume, true, true);
    public void OnGameSfxChanged(float value) => ApplyVolume(value, MixerName(MixerParam.GameSfx), PrefKey.GameSfx, true, false);
    public void OnMenuSfxChanged(float value) => ApplyVolume(value, MixerName(MixerParam.MenuSfx), PrefKey.MenuSfx, true, false);
    public void OnGameMusicChanged(float value) => ApplyVolume(value, MixerName(MixerParam.GameMusic), PrefKey.GameMusic, true, false);
    public void OnMenuMusicChanged(float value) => ApplyVolume(value, MixerName(MixerParam.MenuMusic), PrefKey.MenuMusic, true, false);
    public void OnCrosshairSizeChanged(float value) => ApplyCrosshairSize(value, true);
    public void OnShowFpsChanged(bool value) => ApplyHudWidget(fpsWidget, value, PrefKey.ShowFps, true);
    public void OnShowPingChanged(bool value) => ApplyHudWidget(pingWidget, value, PrefKey.ShowPing, true);
    public void OnShowClockChanged(bool value) => ApplyHudWidget(systemClockWidget, value, PrefKey.ShowClock, true);

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

    void RefreshResOptions()
    {
        if (resolutionDropdown == null)
        {
            return;
        }

        List<string> options = new List<string>(resList.Count);

        for (int i = 0; i < resList.Count; i++)
        {
            Vector2Int r = resList[i];
            options.Add($"{r.x} x {r.y}");
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    Vector2Int CurrentScreenResolution()
    {
        Resolution currentResolution = Screen.currentResolution;

        if (currentResolution.width > 0 && currentResolution.height > 0)
        {
            return new Vector2Int(currentResolution.width, currentResolution.height);
        }

        return new Vector2Int(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
    }

    float Aspect(Vector2Int resolution)
    {
        return resolution.y <= 0 ? 1f : (float)resolution.x / resolution.y;
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

        if (pauseMenuRef != null && PauseMenu.isOpen)
        {
            return;
        }

        if (mainMenuRef != null)
        {
            mainMenuRef.CloseOptions();
        }
    }

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

    void LoadPrefs()
    {
        int savedModeOption = PrefHasKey(PrefKey.DisplayMode)
            ? PrefGetInt(PrefKey.DisplayMode)
            : (int)ModeOption.Borderless;
        displayModeOption = ClampModeOption(savedModeOption);

        if (displayModeDropdown != null)
        {
            displayModeDropdown.SetValueWithoutNotify((int)displayModeOption);
            displayModeDropdown.RefreshShownValue();
        }

        ApplyDisplayMode(displayModeOption, false);
        RebuildResolutionList(displayModeOption);

        int resolutionIndex = GetSavedResIndex();
        manualResIndex = resolutionIndex;
        if (resolutionDropdown != null && resList.Count > 0)
        {
            resolutionDropdown.SetValueWithoutNotify(resolutionIndex);
            resolutionDropdown.RefreshShownValue();
        }

        ApplyResMode(displayModeOption, wasBorderless: false);

        int savedQualityIndex = PrefGetInt(PrefKey.QualityLevel, QualitySettings.GetQualityLevel());
        savedQualityIndex = Mathf.Clamp(savedQualityIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        int qualityDropdownIndex = GetQualityDropdownIndex(savedQualityIndex);

        if (graphicsQualityDropdown != null)
        {
            qualityDropdownIndex = Mathf.Clamp(qualityDropdownIndex, 0, Mathf.Max(0, graphicsQualityDropdown.options.Count - 1));
            graphicsQualityDropdown.SetValueWithoutNotify(qualityDropdownIndex);
            graphicsQualityDropdown.RefreshShownValue();
        }

        ApplyQualityOption(qualityDropdownIndex, false);

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

        float masterVolume = Mathf.Clamp(PrefGetFloat(PrefKey.MasterVolume, DefaultVolumePercent), 0f, 100f);
        if (masterVolSlider != null)
        {
            masterVolSlider.minValue = 0f;
            masterVolSlider.maxValue = 100f;
            masterVolSlider.SetValueWithoutNotify(masterVolume);
        }
        ApplyVolume(masterVolume, MixerName(MixerParam.Master), PrefKey.MasterVolume, false, true);

        float gameSfxVolume = Mathf.Clamp(PrefGetFloat(PrefKey.GameSfx, DefaultVolumePercent), 0f, 100f);
        if (gameSfxSlider != null)
        {
            gameSfxSlider.minValue = 0f;
            gameSfxSlider.maxValue = 100f;
            gameSfxSlider.SetValueWithoutNotify(gameSfxVolume);
        }
        ApplyVolume(gameSfxVolume, MixerName(MixerParam.GameSfx), PrefKey.GameSfx, false, false);

        float menuSfxVolume = Mathf.Clamp(PrefGetFloat(PrefKey.MenuSfx, DefaultVolumePercent), 0f, 100f);
        if (menuSfxSlider != null)
        {
            menuSfxSlider.minValue = 0f;
            menuSfxSlider.maxValue = 100f;
            menuSfxSlider.SetValueWithoutNotify(menuSfxVolume);
        }
        ApplyVolume(menuSfxVolume, MixerName(MixerParam.MenuSfx), PrefKey.MenuSfx, false, false);

        float gameMusicVolume = Mathf.Clamp(PrefGetFloat(PrefKey.GameMusic, DefaultVolumePercent), 0f, 100f);
        if (gameMusicSlider != null)
        {
            gameMusicSlider.minValue = 0f;
            gameMusicSlider.maxValue = 100f;
            gameMusicSlider.SetValueWithoutNotify(gameMusicVolume);
        }
        ApplyVolume(gameMusicVolume, MixerName(MixerParam.GameMusic), PrefKey.GameMusic, false, false);

        float menuMusicVolume = Mathf.Clamp(PrefGetFloat(PrefKey.MenuMusic, DefaultVolumePercent), 0f, 100f);
        if (menuMusicSlider != null)
        {
            menuMusicSlider.minValue = 0f;
            menuMusicSlider.maxValue = 100f;
            menuMusicSlider.SetValueWithoutNotify(menuMusicVolume);
        }
        ApplyVolume(menuMusicVolume, MixerName(MixerParam.MenuMusic), PrefKey.MenuMusic, false, false);

        float crosshairSize = Mathf.Clamp(PrefGetFloat(PrefKey.CrosshairSize, DefaultCrosshairVal), MinCrosshairSize, MaxCrosshairSize);
        if (crosshairSizeSlider != null)
        {
            crosshairSizeSlider.minValue = MinCrosshairSize;
            crosshairSizeSlider.maxValue = MaxCrosshairSize;
            crosshairSizeSlider.SetValueWithoutNotify(crosshairSize);
        }
        ApplyCrosshairSize(crosshairSize, false);

        int crosshairColorIndex = Mathf.Clamp(PrefGetInt(PrefKey.CrosshairColor, 0), 0, CrossCols.Length - 1);
        if (crosshairColorDropdown != null)
        {
            crosshairColorDropdown.SetValueWithoutNotify(crosshairColorIndex);
            crosshairColorDropdown.RefreshShownValue();
        }
        ApplyCrosshairColor(crosshairColorIndex, false);

        bool showFps = PrefGetInt(PrefKey.ShowFps, DefaultHudOn) == 1;
        if (showFpsToggle != null)
        {
            showFpsToggle.SetValue(showFps, false);
        }
        ApplyHudWidget(fpsWidget, showFps, PrefKey.ShowFps, false);

        bool showPing = PrefGetInt(PrefKey.ShowPing, DefaultHudOn) == 1;
        if (showPingToggle != null)
        {
            showPingToggle.SetValue(showPing, false);
        }
        ApplyHudWidget(pingWidget, showPing, PrefKey.ShowPing, false);

        bool showSystemClock = PrefGetInt(PrefKey.ShowClock, DefaultHudOn) == 1;
        if (showSystemClockToggle != null)
        {
            showSystemClockToggle.SetValue(showSystemClock, false);
        }
        ApplyHudWidget(systemClockWidget, showSystemClock, PrefKey.ShowClock, false);
    }

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

    ModeOption ClampModeOption(int option)
    {
        return (ModeOption)Mathf.Clamp(option, (int)ModeOption.Windowed, (int)ModeOption.Fullscreen);
    }

    FullScreenMode GetDisplayModeFromOption(ModeOption option)
    {
        switch (option)
        {
            case ModeOption.Borderless:
                return FullScreenMode.FullScreenWindow;
            case ModeOption.Fullscreen:
#if UNITY_STANDALONE_WIN
                return FullScreenMode.ExclusiveFullScreen;
#else
                return FullScreenMode.FullScreenWindow;
#endif
            default:
                return FullScreenMode.Windowed;
        }
    }

    ModeOption GetModeOption()
    {
        if (displayModeDropdown != null)
        {
            return ClampModeOption(displayModeDropdown.value);
        }

        return ClampModeOption(PrefGetInt(PrefKey.DisplayMode, (int)ModeOption.Borderless));
    }

    FullScreenMode GetMode()
    {
        return GetDisplayModeFromOption(GetModeOption());
    }

    bool IsBorderlessOption(ModeOption option)
    {
        return option == ModeOption.Borderless;
    }

    bool IsFullscreenOption(ModeOption option)
    {
        return option == ModeOption.Fullscreen;
    }

    int GetCurrentResIndex()
    {
        if (resolutionDropdown != null)
        {
            return Mathf.Clamp(resolutionDropdown.value, 0, Mathf.Max(0, resList.Count - 1));
        }

        return GetSavedResIndex();
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

        SetResolutionInteractable(true);
        int targetIndex = wasBorderless
            ? Mathf.Clamp(manualResIndex, 0, resList.Count - 1)
            : GetCurrentResIndex();

        SetResolutionValue(targetIndex);
        ApplyResolution(targetIndex, false);
    }

    int GetQualityDropdownIndex(int qualityIndex)
    {
        if (graphicsQualityDropdown == null || graphicsQualityDropdown.options.Count == 0)
        {
            return qualityIndex;
        }

        return Mathf.Clamp(qualityIndex, 0, graphicsQualityDropdown.options.Count - 1);
    }

    int GetQualityFromOption(int optionIndex)
    {
        return Mathf.Clamp(optionIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
    }

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

    void ApplyDisplayMode(ModeOption option, bool save)
    {
        FullScreenMode mode = GetDisplayModeFromOption(option);
        Screen.fullScreenMode = mode;

        if (save)
        {
            PrefSetInt(PrefKey.DisplayMode, (int)option);
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
            PrefSetInt(PrefKey.ResolutionWidth, resolution.x);
            PrefSetInt(PrefKey.ResolutionHeight, resolution.y);
        }
    }

    void ApplyQualityOption(int optionIndex, bool save)
    {
        int qualityIndex = GetQualityFromOption(Mathf.Clamp(optionIndex, 0, Mathf.Max(0, graphicsQualityDropdown != null ? graphicsQualityDropdown.options.Count - 1 : QualitySettings.names.Length - 1)));
        qualityIndex = Mathf.Clamp(qualityIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        QualitySettings.SetQualityLevel(qualityIndex, true);

        if (save)
        {
            PrefSetInt(PrefKey.QualityLevel, qualityIndex);
        }
    }

    void ApplyCameraFov(float fov, bool save)
    {
        float clamped = Mathf.Clamp(fov, MinFov, MaxFov);
        PlayerMovement owner = GetOwnerPlayer();
        if (owner != null && owner.PlayerCamera != null)
        {
            owner.PlayerCamera.fieldOfView = clamped;
        }

        if (save)
        {
            PrefSetFloat(PrefKey.CameraFov, clamped);
        }
    }

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

        if (save)
        {
            PrefSetFloat(PrefKey.SensPct, steppedPercent);
        }
    }

    void ApplyVolume(float percentValue, string exposedParamName, PrefKey prefKey, bool save, bool fallbackToListener)
    {
        float clamped = Mathf.Clamp(percentValue, 0f, 100f);
        float normalized = clamped / 100f;
        bool appliedMixer = false;

        if (audioMixer != null)
        {
            appliedMixer = SetMixerVol(exposedParamName, normalized);

            if (!appliedMixer)
            {
                string alternateParamName = GetAltVolParamName(exposedParamName);
                appliedMixer = SetMixerVol(alternateParamName, normalized);
            }
        }

        if (fallbackToListener && !appliedMixer)
        {
            AudioListener.volume = normalized;
        }

        if (save)
        {
            PrefSetFloat(prefKey, clamped);
        }
    }

    bool SetMixerVol(string paramName, float normalized)
    {
        if (audioMixer == null || string.IsNullOrWhiteSpace(paramName))
        {
            return false;
        }

        return audioMixer.SetFloat(paramName.Trim(), LinearToDecibel(normalized));
    }

    string GetAltVolParamName(string paramName)
    {
        if (string.IsNullOrWhiteSpace(paramName))
        {
            return string.Empty;
        }

        string trimmed = paramName.Trim();
        if (trimmed.EndsWith(VolumeSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return trimmed.Substring(0, trimmed.Length - VolumeSuffix.Length);
        }

        return trimmed + VolumeSuffix;
    }

    static string MixerName(MixerParam param)
    {
        switch (param)
        {
            case MixerParam.Master:
                return "Master";
            case MixerParam.GameSfx:
                return "GameSFX";
            case MixerParam.MenuSfx:
                return "MenuSFX";
            case MixerParam.GameMusic:
                return "GameMusic";
            case MixerParam.MenuMusic:
                return "MenuMusic";
            default:
                throw new ArgumentOutOfRangeException(nameof(param), param, null);
        }
    }

    PlayerMovement GetOwnerPlayer()
    {
        if (ownerPlayer != null && ownerPlayer.IsOwner)
        {
            return ownerPlayer;
        }

        PlayerMovement[] players = UnityEngine.Object.FindObjectsByType<PlayerMovement>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].IsOwner)
            {
                ownerPlayer = players[i];
                return ownerPlayer;
            }
        }

        ownerPlayer = null;
        return null;
    }

    float LinearToDecibel(float linear)
    {
        if (linear <= MinLinearVolume)
        {
            return MuteDb;
        }

        return Mathf.Log10(linear) * 20f;
    }

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

        if (save)
        {
            PrefSetFloat(PrefKey.CrosshairSize, clamped);
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
            PrefSetInt(PrefKey.CrosshairColor, clamped);
        }
    }

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

    void EnsureVolumeDefaults()
    {
        if (PrefGetInt(PrefKey.VolumeInit, 0) == 1)
        {
            return;
        }

        bool hasVolumeKey =
            PrefHasKey(PrefKey.MasterVolume) ||
            PrefHasKey(PrefKey.GameSfx) ||
            PrefHasKey(PrefKey.MenuSfx) ||
            PrefHasKey(PrefKey.GameMusic) ||
            PrefHasKey(PrefKey.MenuMusic);

        bool allVolumesZero =
            Mathf.Approximately(PrefGetFloat(PrefKey.MasterVolume, 0f), 0f) &&
            Mathf.Approximately(PrefGetFloat(PrefKey.GameSfx, 0f), 0f) &&
            Mathf.Approximately(PrefGetFloat(PrefKey.MenuSfx, 0f), 0f) &&
            Mathf.Approximately(PrefGetFloat(PrefKey.GameMusic, 0f), 0f) &&
            Mathf.Approximately(PrefGetFloat(PrefKey.MenuMusic, 0f), 0f);

        if (!hasVolumeKey || allVolumesZero)
        {
            PrefSetFloat(PrefKey.MasterVolume, DefaultVolumePercent);
            PrefSetFloat(PrefKey.GameSfx, DefaultVolumePercent);
            PrefSetFloat(PrefKey.MenuSfx, DefaultVolumePercent);
            PrefSetFloat(PrefKey.GameMusic, DefaultVolumePercent);
            PrefSetFloat(PrefKey.MenuMusic, DefaultVolumePercent);
        }

        PrefSetInt(PrefKey.VolumeInit, 1);
    }
}

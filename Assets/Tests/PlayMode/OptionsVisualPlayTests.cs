using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsVisualPlayTests
{
    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteAll();
        DestroyAll<OptionsMenu>();
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteAll();
        DestroyAll<OptionsMenu>();
    }

    [Test]
    public void OnDisplayModeChangedTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        menu.OnDisplayModeChanged(1);
        Assert.That(PlayerPrefs.GetInt("opt_display_mode", -1), Is.EqualTo(1));
        menu.OnDisplayModeChanged(2);
        Assert.That(PlayerPrefs.GetInt("opt_display_mode", -1), Is.EqualTo(2));
    }

    [Test]
    public void OnResolutionChangedTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        AddResolution(menu, 800, 600);
        SetPrivateEnum(menu, "displayModeOption", 0);
        menu.OnResolutionChanged(0);
        Assert.That(PlayerPrefs.GetInt("opt_resolution_width", 0), Is.EqualTo(800));
        Assert.That(PlayerPrefs.GetInt("opt_resolution_height", 0), Is.EqualTo(600));
        PlayerPrefs.DeleteKey("opt_resolution_width");
        PlayerPrefs.DeleteKey("opt_resolution_height");
        SetPrivateEnum(menu, "displayModeOption", 1);
        menu.OnResolutionChanged(0);
        Assert.That(PlayerPrefs.HasKey("opt_resolution_width"), Is.False);
        Assert.That(PlayerPrefs.HasKey("opt_resolution_height"), Is.False);
    }

    [Test]
    public void ResetTabTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        ConfigureTabs(menu, out GameObject panel0, out GameObject panel1);
        menu.ShowTab(1);
        menu.ResetTab();
        Assert.That(panel0.activeSelf, Is.True);
        Assert.That(panel1.activeSelf, Is.False);
    }

    [Test]
    public void ShowTabTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        ConfigureTabs(menu, out GameObject panel0, out GameObject panel1);
        menu.ShowTab(-5);
        Assert.That(panel0.activeSelf, Is.True);
        Assert.That(panel1.activeSelf, Is.False);
        menu.ShowTab(1);
        Assert.That(panel0.activeSelf, Is.False);
        Assert.That(panel1.activeSelf, Is.True);
        menu.ShowTab(8);
        Assert.That(panel0.activeSelf, Is.False);
        Assert.That(panel1.activeSelf, Is.True);
    }

    [Test]
    public void OnCrosshairColorChangedTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        Graphic crosshair = GetPrivate<Graphic>(menu, "crosshairGraphic");
        menu.OnCrosshairColorChanged(3);
        Assert.That(PlayerPrefs.GetInt("opt_crosshair_color", -1), Is.EqualTo(3));
        Assert.That(crosshair.color, Is.EqualTo(Color.red));
        menu.OnCrosshairColorChanged(999);
        Assert.That(PlayerPrefs.GetInt("opt_crosshair_color", -1), Is.EqualTo(9));
    }

    [Test]
    public void OnFovChangedTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        menu.OnFovChanged(91f);
        Assert.That(PlayerPrefs.GetFloat("opt_camera_fov", 0f), Is.EqualTo(91f).Within(0.0001f));
        menu.OnFovChanged(-100f);
        Assert.That(PlayerPrefs.GetFloat("opt_camera_fov", 0f), Is.EqualTo(60f).Within(0.0001f));
        menu.OnFovChanged(500f);
        Assert.That(PlayerPrefs.GetFloat("opt_camera_fov", 0f), Is.EqualTo(100f).Within(0.0001f));
    }

    [Test]
    public void OnSensChangedTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        menu.OnSensChanged(77f);
        Assert.That(PlayerPrefs.GetFloat("opt_camera_sensitivity_pct", 0f), Is.EqualTo(77f).Within(0.0001f));
        menu.OnSensChanged(0f);
        Assert.That(PlayerPrefs.GetFloat("opt_camera_sensitivity_pct", 0f), Is.EqualTo(1f).Within(0.0001f));
        menu.OnSensChanged(77.8f);
        Assert.That(PlayerPrefs.GetFloat("opt_camera_sensitivity_pct", 0f), Is.EqualTo(78f).Within(0.0001f));
        menu.OnSensChanged(500f);
        Assert.That(PlayerPrefs.GetFloat("opt_camera_sensitivity_pct", 0f), Is.EqualTo(100f).Within(0.0001f));
    }

    [Test]
    public void OnCrosshairSizeChangedTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        RectTransform crosshairRect = GetPrivate<RectTransform>(menu, "crosshairRect");
        menu.OnCrosshairSizeChanged(5f);
        Assert.That(PlayerPrefs.GetFloat("opt_crosshair_size", 0f), Is.EqualTo(5f).Within(0.0001f));
        Assert.That(crosshairRect.sizeDelta.x, Is.GreaterThan(5f));
        Assert.That(crosshairRect.sizeDelta.y, Is.EqualTo(crosshairRect.sizeDelta.x).Within(0.0001f));
        menu.OnCrosshairSizeChanged(-3f);
        Assert.That(PlayerPrefs.GetFloat("opt_crosshair_size", 0f), Is.EqualTo(1f).Within(0.0001f));
        menu.OnCrosshairSizeChanged(999f);
        Assert.That(PlayerPrefs.GetFloat("opt_crosshair_size", 0f), Is.EqualTo(10f).Within(0.0001f));
    }

    [Test]
    public void OnShowFpsChangedTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        GameObject widget = GetPrivate<GameObject>(menu, "fpsWidget");
        menu.OnShowFpsChanged(true);
        Assert.That(widget.activeSelf, Is.True);
        Assert.That(PlayerPrefs.GetInt("opt_show_fps", 0), Is.EqualTo(1));
        menu.OnShowFpsChanged(false);
        Assert.That(widget.activeSelf, Is.False);
        Assert.That(PlayerPrefs.GetInt("opt_show_fps", 1), Is.EqualTo(0));
    }

    [Test]
    public void OnShowPingChangedTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        GameObject widget = GetPrivate<GameObject>(menu, "pingWidget");
        menu.OnShowPingChanged(false);
        Assert.That(widget.activeSelf, Is.False);
        Assert.That(PlayerPrefs.GetInt("opt_show_ping", 1), Is.EqualTo(0));
        menu.OnShowPingChanged(true);
        Assert.That(widget.activeSelf, Is.True);
        Assert.That(PlayerPrefs.GetInt("opt_show_ping", 0), Is.EqualTo(1));
    }

    [Test]
    public void OnShowClockChangedTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        GameObject widget = GetPrivate<GameObject>(menu, "systemClockWidget");
        menu.OnShowClockChanged(true);
        Assert.That(widget.activeSelf, Is.True);
        Assert.That(PlayerPrefs.GetInt("opt_show_system_clock", 0), Is.EqualTo(1));
        menu.OnShowClockChanged(false);
        Assert.That(widget.activeSelf, Is.False);
        Assert.That(PlayerPrefs.GetInt("opt_show_system_clock", 1), Is.EqualTo(0));
    }

    private static OptionsMenu BuildOptionsMenu()
    {
        GameObject root = new GameObject("OptionsMenuTestRoot");
        root.SetActive(false);
        OptionsMenu menu = root.AddComponent<OptionsMenu>();
        SetPrivate(menu, "displayModeDropdown", new GameObject("Display", typeof(RectTransform), typeof(TMP_Dropdown)).GetComponent<TMP_Dropdown>());
        SetPrivate(menu, "resolutionDropdown", new GameObject("Resolution", typeof(RectTransform), typeof(TMP_Dropdown)).GetComponent<TMP_Dropdown>());
        SetPrivate(menu, "graphicsQualityDropdown", new GameObject("Quality", typeof(RectTransform), typeof(TMP_Dropdown)).GetComponent<TMP_Dropdown>());
        SetPrivate(menu, "cameraFovSlider", new GameObject("Fov", typeof(RectTransform), typeof(Slider)).GetComponent<Slider>());
        SetPrivate(menu, "cameraSensitivitySlider", new GameObject("Sens", typeof(RectTransform), typeof(Slider)).GetComponent<Slider>());
        SetPrivate(menu, "masterVolSlider", new GameObject("Master", typeof(RectTransform), typeof(Slider)).GetComponent<Slider>());
        SetPrivate(menu, "gameSfxSlider", new GameObject("GameSfx", typeof(RectTransform), typeof(Slider)).GetComponent<Slider>());
        SetPrivate(menu, "menuSfxSlider", new GameObject("MenuSfx", typeof(RectTransform), typeof(Slider)).GetComponent<Slider>());
        SetPrivate(menu, "gameMusicSlider", new GameObject("GameMusic", typeof(RectTransform), typeof(Slider)).GetComponent<Slider>());
        SetPrivate(menu, "menuMusicSlider", new GameObject("MenuMusic", typeof(RectTransform), typeof(Slider)).GetComponent<Slider>());
        SetPrivate(menu, "crosshairSizeSlider", new GameObject("CrosshairSize", typeof(RectTransform), typeof(Slider)).GetComponent<Slider>());
        SetPrivate(menu, "crosshairColorDropdown", new GameObject("CrosshairColor", typeof(RectTransform), typeof(TMP_Dropdown)).GetComponent<TMP_Dropdown>());
        SetPrivate(menu, "crosshairRect", new GameObject("CrosshairRect", typeof(RectTransform)).GetComponent<RectTransform>());
        SetPrivate(menu, "crosshairGraphic", new GameObject("CrosshairGraphic", typeof(RectTransform), typeof(Image)).GetComponent<Graphic>());
        SetPrivate(menu, "fpsWidget", new GameObject("FpsWidget"));
        SetPrivate(menu, "pingWidget", new GameObject("PingWidget"));
        SetPrivate(menu, "systemClockWidget", new GameObject("ClockWidget"));
        return menu;
    }

    private static void ConfigureTabs(OptionsMenu menu, out GameObject panel0, out GameObject panel1)
    {
        panel0 = new GameObject("TabPanel0");
        panel1 = new GameObject("TabPanel1");

        OptionsMenu.Tab tab0 = new OptionsMenu.Tab
        {
            button = new GameObject("TabBtn0", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>(),
            panel = panel0,
            labelGraphic = new GameObject("TabLbl0", typeof(RectTransform), typeof(Image)).GetComponent<Graphic>()
        };

        OptionsMenu.Tab tab1 = new OptionsMenu.Tab
        {
            button = new GameObject("TabBtn1", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>(),
            panel = panel1,
            labelGraphic = new GameObject("TabLbl1", typeof(RectTransform), typeof(Image)).GetComponent<Graphic>()
        };

        SetPrivate(menu, "tabs", new[] { tab0, tab1 });
    }

    private static void AddResolution(OptionsMenu menu, int width, int height)
    {
        List<Vector2Int> list = GetPrivate<List<Vector2Int>>(menu, "resList");
        list.Clear();
        list.Add(new Vector2Int(width, height));
    }

    private static T GetPrivate<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (T)field.GetValue(target);
    }

    private static void SetPrivate(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static void SetPrivateEnum(object target, string fieldName, int value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        object enumValue = Enum.ToObject(field.FieldType, value);
        field.SetValue(target, enumValue);
    }

    private static void DestroyAll<T>() where T : UnityEngine.Object
    {
        T[] objects = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null)
            {
                continue;
            }

            if (objects[i] is Component component)
            {
                UnityEngine.Object.DestroyImmediate(component.gameObject);
                continue;
            }

            UnityEngine.Object.DestroyImmediate(objects[i]);
        }
    }
}

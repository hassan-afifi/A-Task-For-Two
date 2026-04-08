using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsSettingsPlayTests
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
    public void SensMapTest()
    {
        Assert.That(OptionsMenu.SensMap(-1f), Is.EqualTo(0.01f).Within(0.0001f));
        Assert.That(OptionsMenu.SensMap(0f), Is.EqualTo(0.01f).Within(0.0001f));
        Assert.That(OptionsMenu.SensMap(75f), Is.EqualTo(0.75f).Within(0.0001f));
        Assert.That(OptionsMenu.SensMap(150f), Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    public void SavedFovTest()
    {
        Assert.That(OptionsMenu.SavedFov(80f), Is.EqualTo(80f).Within(0.0001f));
        PlayerPrefs.SetFloat("opt_camera_fov", 95f);
        Assert.That(OptionsMenu.SavedFov(80f), Is.EqualTo(95f).Within(0.0001f));
        PlayerPrefs.SetFloat("opt_camera_fov", 10f);
        Assert.That(OptionsMenu.SavedFov(80f), Is.EqualTo(60f).Within(0.0001f));
        PlayerPrefs.SetFloat("opt_camera_fov", 120f);
        Assert.That(OptionsMenu.SavedFov(80f), Is.EqualTo(100f).Within(0.0001f));
    }

    [Test]
    public void SavedSensPctTest()
    {
        Assert.That(OptionsMenu.SavedSensPct(50f), Is.EqualTo(50f).Within(0.0001f));
        PlayerPrefs.SetFloat("opt_camera_sensitivity_pct", 65f);
        Assert.That(OptionsMenu.SavedSensPct(50f), Is.EqualTo(65f).Within(0.0001f));
        PlayerPrefs.SetFloat("opt_camera_sensitivity_pct", 0f);
        Assert.That(OptionsMenu.SavedSensPct(50f), Is.EqualTo(1f).Within(0.0001f));
        PlayerPrefs.SetFloat("opt_camera_sensitivity_pct", 800f);
        Assert.That(OptionsMenu.SavedSensPct(50f), Is.EqualTo(100f).Within(0.0001f));
    }

    [Test]
    public void StopGameMusicTest()
    {
        Assert.Throws<InvalidOperationException>(() => OptionsMenu.StopGameMusic());
        OptionsMenu nullOnlyMenu = BuildOptionsMenu();
        Assert.Throws<InvalidOperationException>(() => OptionsMenu.StopGameMusic());
        UnityEngine.Object.DestroyImmediate(nullOnlyMenu.gameObject);
    }

    [Test]
    public void OnQualityChangedTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        TMP_Dropdown dropdown = GetPrivate<TMP_Dropdown>(menu, "graphicsQualityDropdown");
        dropdown.options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("Q0"), new TMP_Dropdown.OptionData("Q1") };
        menu.OnQualityChanged(0);
        Assert.That(PlayerPrefs.GetInt("opt_quality_level", -1), Is.EqualTo(0));
        menu.OnQualityChanged(999);
        Assert.That(PlayerPrefs.GetInt("opt_quality_level", -1), Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void OnMasterVolChangedTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        Assert.Throws<NullReferenceException>(() => menu.OnMasterVolChanged(30f));
        Assert.That(PlayerPrefs.HasKey("opt_master_volume"), Is.False);
    }

    [Test]
    public void OnGameSfxChangedTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        Assert.Throws<NullReferenceException>(() => menu.OnGameSfxChanged(30f));
        Assert.That(PlayerPrefs.HasKey("opt_game_sfx_volume"), Is.False);
    }

    [Test]
    public void OnMenuSfxChangedTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        Assert.Throws<NullReferenceException>(() => menu.OnMenuSfxChanged(30f));
        Assert.That(PlayerPrefs.HasKey("opt_menu_sfx_volume"), Is.False);
    }

    [Test]
    public void OnGameMusicChangedTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        Assert.Throws<NullReferenceException>(() => menu.OnGameMusicChanged(30f));
        Assert.That(PlayerPrefs.HasKey("opt_game_music_volume"), Is.False);
    }

    [Test]
    public void OnMenuMusicChangedTest()
    {
        OptionsMenu menu = BuildOptionsMenu();
        Assert.Throws<NullReferenceException>(() => menu.OnMenuMusicChanged(30f));
        Assert.That(PlayerPrefs.HasKey("opt_menu_music_volume"), Is.False);
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

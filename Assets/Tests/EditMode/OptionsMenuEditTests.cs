using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuEditTests
{
    private const string CameraFovKey = "opt_camera_fov";
    private const string SensPctKey = "opt_camera_sensitivity_pct";
    private const string VolumeInitKey = "opt_volume_defaults_initialized_v1";
    private const string MasterVolumeKey = "opt_master_volume";
    private const string GameSfxKey = "opt_game_sfx_volume";
    private const string MenuSfxKey = "opt_menu_sfx_volume";
    private const string GameMusicKey = "opt_game_music_volume";
    private const string MenuMusicKey = "opt_menu_music_volume";

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey(CameraFovKey);
        PlayerPrefs.DeleteKey(SensPctKey);
        PlayerPrefs.DeleteKey(VolumeInitKey);
        PlayerPrefs.DeleteKey(MasterVolumeKey);
        PlayerPrefs.DeleteKey(GameSfxKey);
        PlayerPrefs.DeleteKey(MenuSfxKey);
        PlayerPrefs.DeleteKey(GameMusicKey);
        PlayerPrefs.DeleteKey(MenuMusicKey);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(CameraFovKey);
        PlayerPrefs.DeleteKey(SensPctKey);
        PlayerPrefs.DeleteKey(VolumeInitKey);
        PlayerPrefs.DeleteKey(MasterVolumeKey);
        PlayerPrefs.DeleteKey(GameSfxKey);
        PlayerPrefs.DeleteKey(MenuSfxKey);
        PlayerPrefs.DeleteKey(GameMusicKey);
        PlayerPrefs.DeleteKey(MenuMusicKey);
    }

    [Test]
    public void SensMap_ClampsAndRounds()
    {
        Assert.That(OptionsMenu.SensMap(0f), Is.EqualTo(0.01f).Within(0.0001f));
        Assert.That(OptionsMenu.SensMap(50f), Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(OptionsMenu.SensMap(100f), Is.EqualTo(1f).Within(0.0001f));
        Assert.That(OptionsMenu.SensMap(33.6f), Is.EqualTo(0.34f).Within(0.0001f));
    }

    [Test]
    public void SavedValues_ClampCorrectly()
    {
        PlayerPrefs.SetFloat(CameraFovKey, 150f);
        PlayerPrefs.SetFloat(SensPctKey, 0f);

        Assert.That(OptionsMenu.SavedFov(80f), Is.EqualTo(100f).Within(0.0001f));
        Assert.That(OptionsMenu.SavedSensPct(50f), Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    public void ApplyCrosshairSize_ResizesRect()
    {
        using var scope = new ComponentScope<OptionsMenu>();
        var crosshair = new GameObject("Crosshair");
        var rect = crosshair.AddComponent<RectTransform>();
        SetField(scope.Component, "crosshairRect", rect);

        Invoke<object>(scope.Component, "ApplyCrosshairSize", 1f, false);
        Assert.That(rect.sizeDelta.x, Is.EqualTo(5f).Within(0.01f));

        Invoke<object>(scope.Component, "ApplyCrosshairSize", 10f, false);
        Assert.That(rect.sizeDelta.x, Is.EqualTo(50f).Within(0.01f));

        Object.DestroyImmediate(crosshair);
    }

    [Test]
    public void EnsureVolumeDefaults_InitializesOnce()
    {
        using var scope = new ComponentScope<OptionsMenu>();

        Invoke<object>(scope.Component, "EnsureVolumeDefaults");
        Assert.That(PlayerPrefs.GetInt(VolumeInitKey, 0), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetFloat(MasterVolumeKey, -1f), Is.EqualTo(50f).Within(0.0001f));

        PlayerPrefs.SetFloat(MasterVolumeKey, 77f);
        Invoke<object>(scope.Component, "EnsureVolumeDefaults");
        Assert.That(PlayerPrefs.GetFloat(MasterVolumeKey, -1f), Is.EqualTo(77f).Within(0.0001f));
    }

    [Test]
    public void LinearToDecibel_HandlesMuteAndUnity()
    {
        using var scope = new ComponentScope<OptionsMenu>();
        float muted = Invoke<float>(scope.Component, "LinearToDecibel", 0f);
        float unity = Invoke<float>(scope.Component, "LinearToDecibel", 1f);

        Assert.That(muted, Is.EqualTo(-80f).Within(0.0001f));
        Assert.That(unity, Is.EqualTo(0f).Within(0.0001f));
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
        field.SetValue(target, value);
    }

    private static T Invoke<T>(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Missing method {methodName}");
        object result = method.Invoke(target, args);

        if (typeof(T) == typeof(object) || result == null)
        {
            return default;
        }

        return (T)result;
    }

    private sealed class ComponentScope<T> : System.IDisposable where T : Component
    {
        public T Component { get; }
        private readonly GameObject gameObject;

        public ComponentScope()
        {
            gameObject = new GameObject(typeof(T).Name + "_Test");
            Component = gameObject.AddComponent<T>();
        }

        public void Dispose()
        {
            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}

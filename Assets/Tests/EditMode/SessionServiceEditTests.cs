using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

public class SessionServiceEditTests
{
    private const string CameraFovKey = "opt_camera_fov";
    private const string SensitivityKey = "opt_camera_sensitivity_pct";

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteAll();
        ResetAudioManagerInstance();
        DestroyAll<GameSession>();
        DestroyAll<AudioManager>();
        DestroyAll<OptionsMenu>();
        DestroyAll<RelayManager>();
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteAll();
        ResetAudioManagerInstance();
        DestroyAll<GameSession>();
        DestroyAll<AudioManager>();
        DestroyAll<OptionsMenu>();
        DestroyAll<RelayManager>();
    }

    [Test]
    public void SensMapTest()
    {
        Assert.That(OptionsMenu.SensMap(-5f), Is.EqualTo(0.01f).Within(0.0001f));
        Assert.That(OptionsMenu.SensMap(0f), Is.EqualTo(0.01f).Within(0.0001f));
        Assert.That(OptionsMenu.SensMap(1f), Is.EqualTo(0.01f).Within(0.0001f));
        Assert.That(OptionsMenu.SensMap(50f), Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(OptionsMenu.SensMap(99.4f), Is.EqualTo(0.99f).Within(0.0001f));
        Assert.That(OptionsMenu.SensMap(100f), Is.EqualTo(1f).Within(0.0001f));
        Assert.That(OptionsMenu.SensMap(200f), Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    public void SavedFovTest()
    {
        Assert.That(OptionsMenu.SavedFov(120f), Is.EqualTo(100f).Within(0.0001f));
        PlayerPrefs.SetFloat(CameraFovKey, 77f);
        Assert.That(OptionsMenu.SavedFov(80f), Is.EqualTo(77f).Within(0.0001f));
        PlayerPrefs.SetFloat(CameraFovKey, 1f);
        Assert.That(OptionsMenu.SavedFov(80f), Is.EqualTo(60f).Within(0.0001f));
        PlayerPrefs.SetFloat(CameraFovKey, 500f);
        Assert.That(OptionsMenu.SavedFov(80f), Is.EqualTo(100f).Within(0.0001f));
    }

    [Test]
    public void SavedSensPctTest()
    {
        Assert.That(OptionsMenu.SavedSensPct(0f), Is.EqualTo(1f).Within(0.0001f));
        PlayerPrefs.SetFloat(SensitivityKey, 42f);
        Assert.That(OptionsMenu.SavedSensPct(50f), Is.EqualTo(42f).Within(0.0001f));
        PlayerPrefs.SetFloat(SensitivityKey, 0f);
        Assert.That(OptionsMenu.SavedSensPct(50f), Is.EqualTo(1f).Within(0.0001f));
        PlayerPrefs.SetFloat(SensitivityKey, 900f);
        Assert.That(OptionsMenu.SavedSensPct(50f), Is.EqualTo(100f).Within(0.0001f));
    }

    [Test]
    public void StopGameMusicTest()
    {
        Assert.Throws<InvalidOperationException>(() => OptionsMenu.StopGameMusic());
        GameObject nullOnlyGo = new GameObject("OptionsMenuStopMusicNullOnlyTest");
        nullOnlyGo.AddComponent<OptionsMenu>();
        Assert.Throws<InvalidOperationException>(() => OptionsMenu.StopGameMusic());
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/AudioMixer.mixer");
        Assert.That(mixer, Is.Not.Null);
        GameObject validGo = new GameObject("OptionsMenuStopMusicValidTest");
        OptionsMenu validMenu = validGo.AddComponent<OptionsMenu>();
        SetPrivate(validMenu, "audioMixer", mixer);
        Assert.DoesNotThrow(OptionsMenu.StopGameMusic);
        UnityEngine.Object.DestroyImmediate(validGo);
        UnityEngine.Object.DestroyImmediate(nullOnlyGo);
    }

    [Test]
    public void PlayHoverTest()
    {
        Assert.Throws<InvalidOperationException>(() => AudioManager.PlayHover());
        AudioManager manager = BuildAudioManager("AudioManagerHoverTest");
        Assert.DoesNotThrow(AudioManager.PlayHover);
        UnityEngine.Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void PlayClickTest()
    {
        Assert.Throws<InvalidOperationException>(() => AudioManager.PlayClick());
        AudioManager manager = BuildAudioManager("AudioManagerClickTest");
        Assert.DoesNotThrow(AudioManager.PlayClick);
        UnityEngine.Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void SetNameTest()
    {
        GameObject go = new GameObject("GameSessionSetNameTest");
        GameSession session = go.AddComponent<GameSession>();
        session.SetName("Hassan");
        Assert.That(session.PlayerName, Is.EqualTo("Hassan"));
        session.SetName(null);
        Assert.That(session.PlayerName, Is.EqualTo(string.Empty));
    }

    [Test]
    public void SetCharTest()
    {
        GameObject go = new GameObject("GameSessionSetCharTest");
        GameSession session = go.AddComponent<GameSession>();
        session.SetChar(-5);
        Assert.That(session.CharIndex, Is.EqualTo(0));
        session.SetChar(3);
        Assert.That(session.CharIndex, Is.EqualTo(3));
    }

    [Test]
    public void CreateGameTest()
    {
        GameObject go = new GameObject("RelayManagerCreateTest");
        RelayManager relay = go.AddComponent<RelayManager>();
        Assert.ThrowsAsync<InvalidOperationException>(async () => await relay.CreateGame());
        SetPrivate(relay, "isBusy", true);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await relay.CreateGame());
    }
    [Test]
    public void JoinGameTest()
    {
        GameObject go = new GameObject("RelayManagerJoinTest");
        RelayManager relay = go.AddComponent<RelayManager>();
        Assert.ThrowsAsync<ArgumentException>(async () => await relay.JoinGame("   "));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await relay.JoinGame("ABCDEF"));
        SetPrivate(relay, "isBusy", true);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await relay.JoinGame("ABCDEF"));
    }

    private static AudioManager BuildAudioManager(string name)
    {
        GameObject go = new GameObject(name, typeof(AudioSource), typeof(AudioManager));
        AudioManager manager = go.GetComponent<AudioManager>();
        AudioSource source = go.GetComponent<AudioSource>();
        AudioClip hover = AudioClip.Create("Hover", 256, 1, 44100, false);
        AudioClip click = AudioClip.Create("Click", 256, 1, 44100, false);
        SetPrivate(manager, "uiAudioSource", source);
        SetPrivate(manager, "buttonHoverClip", hover);
        SetPrivate(manager, "buttonClickClip", click);
        FieldInfo instanceField = typeof(AudioManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(instanceField, Is.Not.Null);
        instanceField.SetValue(null, manager);
        return manager;
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

    private static void ResetAudioManagerInstance()
    {
        FieldInfo instanceField = typeof(AudioManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);

        if (instanceField != null)
        {
            instanceField.SetValue(null, null);
        }
    }
}

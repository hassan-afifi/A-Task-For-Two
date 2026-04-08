using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class NetworkUiPlayTests
{
    [SetUp]
    public void SetUp()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        DestroyAll<RelayManager>();
        DestroyAll<MenuSfx>();
        DestroyAll<AudioManager>();
        DestroyAll<GameSession>();
        CleanupNetworkManagers();
        EnsureEventSystem();
    }

    [TearDown]
    public void TearDown()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        DestroyAll<RelayManager>();
        DestroyAll<MenuSfx>();
        DestroyAll<AudioManager>();
        DestroyAll<GameSession>();
        CleanupNetworkManagers();
        DestroyAll<EventSystem>();
    }

    [Test]
    public void CreateGameTest()
    {
        GameObject go = new GameObject("RelayCreateTest");
        RelayManager relay = go.AddComponent<RelayManager>();
        Assert.ThrowsAsync<InvalidOperationException>(async () => await relay.CreateGame());
        SetField(relay, "isBusy", true);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await relay.CreateGame());
    }

    [Test]
    public void JoinGameTest()
    {
        GameObject go = new GameObject("RelayJoinTest");
        RelayManager relay = go.AddComponent<RelayManager>();
        Assert.ThrowsAsync<ArgumentException>(async () => await relay.JoinGame("   "));
        SetField(relay, "isBusy", true);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await relay.JoinGame("ABCDEF"));
    }

    [Test]
    public void MenuSfxOnPointerEnterTest()
    {
        MenuSfx sfx = BuildMenuSfxOnButton();
        Assert.Throws<InvalidOperationException>(() => sfx.OnPointerEnter(null));
        AudioManager manager = BuildAudioManager("AudioManagerMenuHoverTest");
        Assert.DoesNotThrow(() => sfx.OnPointerEnter(null));
        UnityEngine.Object.DestroyImmediate(manager.gameObject);
        GameObject sliderGo = new GameObject("MenuSfxPointerSliderTest", typeof(RectTransform), typeof(Slider), typeof(MenuSfx));
        MenuSfx sliderSfx = sliderGo.GetComponent<MenuSfx>();
        Assert.DoesNotThrow(() => sliderSfx.OnPointerEnter(null));
    }

    [Test]
    public void OnPointerDownTest()
    {
        GameObject sliderGo = new GameObject("MenuSfxPointerDownSliderTest", typeof(RectTransform), typeof(Slider), typeof(MenuSfx));
        MenuSfx sliderSfx = sliderGo.GetComponent<MenuSfx>();
        PointerEventData leftClick = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        Assert.DoesNotThrow(() => sliderSfx.OnPointerDown(leftClick));
        MenuSfx sfx = BuildMenuSfxOnButton();
        PointerEventData rightClick = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Right };
        sfx.GetComponent<Button>().interactable = false;
        Assert.DoesNotThrow(() => sfx.OnPointerDown(leftClick));
        sfx.GetComponent<Button>().interactable = true;
        Assert.DoesNotThrow(() => sfx.OnPointerDown(rightClick));
        Assert.Throws<InvalidOperationException>(() => sfx.OnPointerDown(leftClick));
        AudioManager manager = BuildAudioManager("AudioManagerPointerDownTest");
        Assert.DoesNotThrow(() => sfx.OnPointerDown(leftClick));
        UnityEngine.Object.DestroyImmediate(manager.gameObject);
    }
    [Test]
    public void OnSelectTest()
    {
        MenuSfx sfx = BuildMenuSfxOnButton();
        Assert.Throws<InvalidOperationException>(() => sfx.OnSelect(null));
        AudioManager manager = BuildAudioManager("AudioManagerMenuSelectTest");
        Assert.DoesNotThrow(() => sfx.OnSelect(null));
        UnityEngine.Object.DestroyImmediate(manager.gameObject);
        GameObject sliderGo = new GameObject("MenuSfxSelectSliderTest", typeof(RectTransform), typeof(Slider), typeof(MenuSfx));
        MenuSfx sliderSfx = sliderGo.GetComponent<MenuSfx>();
        Assert.DoesNotThrow(() => sliderSfx.OnSelect(null));
    }

    [Test]
    public void OnBeginDragTest()
    {
        GameObject go = new GameObject("MenuSfxSliderTest", typeof(RectTransform), typeof(Slider), typeof(MenuSfx));
        MenuSfx sfx = go.GetComponent<MenuSfx>();
        Assert.DoesNotThrow(() => sfx.OnBeginDrag(null));
        MenuSfx buttonSfx = BuildMenuSfxOnButton();
        Assert.DoesNotThrow(() => buttonSfx.OnBeginDrag(null));
    }

    [Test]
    public void OnEndDragTest()
    {
        GameObject go = new GameObject("MenuSfxSliderTest", typeof(RectTransform), typeof(Slider), typeof(MenuSfx));
        MenuSfx sfx = go.GetComponent<MenuSfx>();
        Assert.DoesNotThrow(() => sfx.OnEndDrag(null));
        MenuSfx buttonSfx = BuildMenuSfxOnButton();
        Assert.DoesNotThrow(() => buttonSfx.OnEndDrag(null));
    }

    [Test]
    public void DropHoverSfxOnPointerEnterTest()
    {
        GameObject go = new GameObject("DropHoverSfxTest", typeof(DropHoverSfx));
        DropHoverSfx sfx = go.GetComponent<DropHoverSfx>();
        Assert.Throws<InvalidOperationException>(() => sfx.OnPointerEnter(null));
        AudioManager manager = BuildAudioManager("AudioManagerDropHoverTest");
        Assert.DoesNotThrow(() => sfx.OnPointerEnter(null));
        UnityEngine.Object.DestroyImmediate(manager.gameObject);
    }

    [UnityTest]
    public IEnumerator MenuActionsReturnToMainMenuTest()
    {
        EnsureGameSession();
        MenuActions.ReturnToMainMenu();
        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
    }

    [UnityTest]
    public IEnumerator ConnectionLostReturnToMainMenuTest()
    {
        EnsureGameSession();
        ConnectionLost connectionLost = BuildConnectionLost();
        connectionLost.ReturnToMainMenu();
        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
    }
    private static MenuSfx BuildMenuSfxOnButton()
    {
        GameObject go = new GameObject("MenuSfxButtonTest", typeof(RectTransform), typeof(Image), typeof(Button));
        return go.AddComponent<MenuSfx>();
    }

    private static ConnectionLost BuildConnectionLost()
    {
        GameObject go = new GameObject("ConnectionLostReturnTest");
        go.SetActive(false);
        ConnectionLost connectionLost = go.AddComponent<ConnectionLost>();
        SetField(connectionLost, "panel", new GameObject("Panel"));
        SetField(connectionLost, "hudCanvas", new GameObject("HudCanvas", typeof(Canvas)).GetComponent<Canvas>());
        return connectionLost;
    }

    private static AudioManager BuildAudioManager(string name)
    {
        GameObject go = new GameObject(name, typeof(AudioSource), typeof(AudioManager));
        AudioManager manager = go.GetComponent<AudioManager>();
        SetField(manager, "uiAudioSource", go.GetComponent<AudioSource>());
        SetField(manager, "buttonHoverClip", AudioClip.Create($"{name}_Hover", 256, 1, 44100, false));
        SetField(manager, "buttonClickClip", AudioClip.Create($"{name}_Click", 256, 1, 44100, false));
        InvokeNonPublic(manager, "Awake");
        return manager;
    }

    private static void EnsureEventSystem()
    {
        Type inputSystemModuleType = FindType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
        EventSystem[] systems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (systems.Length == 0)
        {
            GameObject go = new GameObject("EventSystem", typeof(EventSystem));

            if (inputSystemModuleType != null)
            {
                go.AddComponent(inputSystemModuleType);
            }

            systems = new[] { go.GetComponent<EventSystem>() };
        }

        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null)
            {
                continue;
            }

            StandaloneInputModule legacy = systems[i].GetComponent<StandaloneInputModule>();

            if (legacy != null)
            {
                legacy.enabled = false;
            }

            if (inputSystemModuleType != null && systems[i].GetComponent(inputSystemModuleType) == null)
            {
                systems[i].gameObject.AddComponent(inputSystemModuleType);
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureEventSystem();
    }

    private static Type FindType(string fullName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        for (int i = 0; i < assemblies.Length; i++)
        {
            Type found = assemblies[i].GetType(fullName, false);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void CleanupNetworkManagers()
    {
        NetworkManager[] managers = UnityEngine.Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] == null)
            {
                continue;
            }

            managers[i].Shutdown();
            UnityEngine.Object.DestroyImmediate(managers[i].gameObject);
        }
    }

    private static void EnsureGameSession()
    {
        if (GameSession.Instance != null)
        {
            return;
        }

        new GameObject("GameSessionTest", typeof(GameSession));
    }

    private static void SetField(object target, string fieldName, object value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static void InvokeNonPublic(object target, string methodName)
    {
        System.Reflection.MethodInfo method = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(target, null);
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

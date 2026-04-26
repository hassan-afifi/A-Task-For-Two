using System.Reflection;
using NUnit.Framework;
using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerPlayTests
{
    [TearDown]
    public void TearDown()
    {
        DestroyAll<PlayerMovement>();
        DestroyAll<PlayerVisuals>();
        DestroyAll<PlayerAudio>();
        DestroyAll<PlayerInputHandler>();
        DestroyAll<OptionsMenu>();
    }

    [Test]
    public void PlayerMovementOnNetworkSpawnTest()
    {
        PlayerMovement movement = BuildPlayerMovement();
        Assert.DoesNotThrow(() => movement.OnNetworkSpawn());
        Assert.That(movement.PlayerCamera.enabled, Is.False);
        Assert.That(movement.IsGrounded, Is.False);

        GameObject missingCameraGo = new GameObject("PlayerMovementMissingCameraTest");
        missingCameraGo.SetActive(false);
        missingCameraGo.AddComponent<NetworkObject>();
        missingCameraGo.AddComponent<CharacterController>();
        missingCameraGo.AddComponent<PlayerInputHandler>();
        PlayerMovement missingCameraMovement = missingCameraGo.AddComponent<PlayerMovement>();
        InvalidOperationException missingCameraError = ExpectInner<InvalidOperationException>(() => InvokeNonPublic(missingCameraMovement, "Awake"));
        Assert.That(missingCameraError.Message, Does.Contain("player camera is missing"));
    }

    [Test]
    public void PlayerMovementOnNetworkDespawnTest()
    {
        PlayerMovement movement = BuildPlayerMovement();
        Assert.DoesNotThrow(() => movement.OnNetworkDespawn());
        Assert.That(PlayerMovement.LocalCamera, Is.Null);
    }

    [Test]
    public void PlayerVisualsOnNetworkSpawnTest()
    {
        PlayerVisuals visuals = BuildPlayerVisuals();
        Assert.DoesNotThrow(() => visuals.OnNetworkSpawn());
        Transform nameTagRoot = GetPrivate<Transform>(visuals, "nameTagRoot");
        Assert.That(nameTagRoot.gameObject.activeSelf, Is.True);
        Assert.That(visuals.ActiveAnimator, Is.Not.Null);

        GameObject noCharsGo = new GameObject("PlayerVisualsNoCharsTest");
        noCharsGo.SetActive(false);
        noCharsGo.AddComponent<NetworkObject>();
        noCharsGo.AddComponent<CharacterController>();
        noCharsGo.AddComponent<PlayerInputHandler>();
        new GameObject("Camera", typeof(Camera)).transform.SetParent(noCharsGo.transform, false);
        noCharsGo.AddComponent<PlayerMovement>();
        noCharsGo.AddComponent<NetworkAnimator>();
        PlayerVisuals noCharsVisuals = noCharsGo.AddComponent<PlayerVisuals>();
        InvalidOperationException noCharsError = ExpectInner<InvalidOperationException>(() => InvokeNonPublic(noCharsVisuals, "Awake"));
        Assert.That(noCharsError.Message, Does.Contain("no character visual roots"));

        GameObject missingNameTagRootGo = new GameObject("PlayerVisualsMissingNameTagRootTest");
        missingNameTagRootGo.SetActive(false);
        missingNameTagRootGo.AddComponent<NetworkObject>();
        missingNameTagRootGo.AddComponent<CharacterController>();
        missingNameTagRootGo.AddComponent<PlayerInputHandler>();
        new GameObject("Camera", typeof(Camera)).transform.SetParent(missingNameTagRootGo.transform, false);
        missingNameTagRootGo.AddComponent<PlayerMovement>();
        missingNameTagRootGo.AddComponent<NetworkAnimator>();
        GameObject charRoot = new GameObject("CharacterRoot");
        charRoot.transform.SetParent(missingNameTagRootGo.transform, false);
        charRoot.AddComponent<Animator>();
        new GameObject("NameTagAnchor").transform.SetParent(charRoot.transform, false);
        PlayerVisuals missingNameTagRootVisuals = missingNameTagRootGo.AddComponent<PlayerVisuals>();
        InvokeNonPublic(missingNameTagRootVisuals, "Awake");
        InvalidOperationException missingNameTagRootError = Assert.Throws<InvalidOperationException>(() => missingNameTagRootVisuals.OnNetworkSpawn());
        Assert.That(missingNameTagRootError.Message, Does.Contain("nameTagRoot"));
    }

    [Test]
    public void PlayerVisualsOnNetworkDespawnTest()
    {
        PlayerVisuals visuals = BuildPlayerVisuals();
        Assert.DoesNotThrow(() => visuals.OnNetworkDespawn());
        Assert.That(visuals.ActiveAnimator, Is.Not.Null);
    }

    [Test]
    public void PlayerAudioOnNetworkSpawnTest()
    {
        PlayerAudio audio = BuildPlayerAudio();
        Assert.DoesNotThrow(() => audio.OnNetworkSpawn());
        Assert.That(GetPrivate<bool>(audio, "loopOn"), Is.False);
        Assert.That(GetPrivate<bool>(audio, "jumpStarted"), Is.False);
        Assert.That(GetPrivate<bool>(audio, "jumpAirborne"), Is.False);

        PlayerMovement missingLoopClipMovement = BuildPlayerMovement();
        PlayerAudio missingLoopClipAudio = missingLoopClipMovement.gameObject.AddComponent<PlayerAudio>();
        AudioClip land = AudioClip.Create("Land", 256, 1, 44100, false);
        SetField(missingLoopClipAudio, "landClip", land);
        InvalidOperationException missingLoopClipError = ExpectInner<InvalidOperationException>(() => InvokeNonPublic(missingLoopClipAudio, "Awake"));
        Assert.That(missingLoopClipError.Message, Does.Contain("movementLoopClip"));

        PlayerMovement missingLandClipMovement = BuildPlayerMovement();
        PlayerAudio missingLandClipAudio = missingLandClipMovement.gameObject.AddComponent<PlayerAudio>();
        AudioClip loop = AudioClip.Create("Loop", 256, 1, 44100, false);
        SetField(missingLandClipAudio, "movementLoopClip", loop);
        InvalidOperationException missingLandClipError = ExpectInner<InvalidOperationException>(() => InvokeNonPublic(missingLandClipAudio, "Awake"));
        Assert.That(missingLandClipError.Message, Does.Contain("landClip"));

        InvalidOperationException targetIdsError = ExpectInner<InvalidOperationException>(() => InvokeNonPublic(audio, "TargetIds"));
        Assert.That(targetIdsError.Message, Does.Contain("NetworkManager reference is missing"));
    }

    [Test]
    public void PlayerAudioOnNetworkDespawnTest()
    {
        PlayerAudio audio = BuildPlayerAudio();
        SetField(audio, "loopOn", true);
        SetField(audio, "jumpStarted", true);
        SetField(audio, "jumpAirborne", true);
        Assert.DoesNotThrow(() => audio.OnNetworkDespawn());
        Assert.That(GetPrivate<bool>(audio, "loopOn"), Is.False);
        Assert.That(GetPrivate<bool>(audio, "jumpStarted"), Is.False);
        Assert.That(GetPrivate<bool>(audio, "jumpAirborne"), Is.False);
    }

    [Test]
    public void OnDestroyTest()
    {
        GameObject go = new GameObject("PlayerInputTest", typeof(NetworkObject));
        PlayerInputHandler input = go.AddComponent<PlayerInputHandler>();
        Assert.DoesNotThrow(() => input.OnDestroy());
    }

    [Test]
    public void PlayerInputHandlerOnNetworkSpawnTest()
    {
        GameObject go = new GameObject("PlayerInputSpawnTest", typeof(NetworkObject));
        PlayerInputHandler input = go.AddComponent<PlayerInputHandler>();
        Assert.DoesNotThrow(() => input.OnNetworkSpawn());
        bool inputOn = GetPrivate<bool>(input, "inputOn");
        Assert.That(inputOn, Is.False);
    }

    [Test]
    public void PlayerInputHandlerOnNetworkDespawnTest()
    {
        GameObject go = new GameObject("PlayerInputDespawnTest", typeof(NetworkObject));
        PlayerInputHandler input = go.AddComponent<PlayerInputHandler>();
        SetField(input, "inputOn", true);
        Assert.DoesNotThrow(() => input.OnNetworkDespawn());
        bool inputOn = GetPrivate<bool>(input, "inputOn");
        Assert.That(inputOn, Is.False);
    }

    [Test]
    public void OnFovChangedTest()
    {
        PlayerPrefs.DeleteKey("opt_camera_fov");
        GameObject go = new GameObject("OptionsMenuFovTest");
        go.SetActive(false);
        OptionsMenu menu = go.AddComponent<OptionsMenu>();
        menu.OnFovChanged(91f);
        Assert.That(PlayerPrefs.GetFloat("opt_camera_fov", 0f), Is.EqualTo(91f).Within(0.0001f));
        menu.OnFovChanged(-100f);
        Assert.That(PlayerPrefs.GetFloat("opt_camera_fov", 0f), Is.EqualTo(60f).Within(0.0001f));
        menu.OnFovChanged(500f);
        Assert.That(PlayerPrefs.GetFloat("opt_camera_fov", 0f), Is.EqualTo(100f).Within(0.0001f));
    }

    private static PlayerMovement BuildPlayerMovement()
    {
        GameObject go = new GameObject("PlayerMovementTest");
        go.SetActive(false);
        go.AddComponent<NetworkObject>();
        go.AddComponent<CharacterController>();
        go.AddComponent<PlayerInputHandler>();
        new GameObject("Camera", typeof(Camera), typeof(AudioListener)).transform.SetParent(go.transform, false);
        PlayerMovement movement = go.AddComponent<PlayerMovement>();
        InvokeNonPublic(movement, "Awake");
        return movement;
    }

    private static PlayerVisuals BuildPlayerVisuals()
    {
        GameObject go = new GameObject("PlayerVisualsTest");
        go.SetActive(false);
        go.AddComponent<NetworkObject>();
        go.AddComponent<CharacterController>();
        go.AddComponent<PlayerInputHandler>();
        new GameObject("Camera", typeof(Camera)).transform.SetParent(go.transform, false);
        go.AddComponent<PlayerMovement>();
        go.AddComponent<NetworkAnimator>();
        GameObject nameRoot = new GameObject("NameTagRoot");
        nameRoot.transform.SetParent(go.transform, false);
        TMP_Text nameText = new GameObject("NameText", typeof(TextMeshPro)).GetComponent<TMP_Text>();
        nameText.transform.SetParent(nameRoot.transform, false);
        GameObject charRoot = new GameObject("CharacterRoot");
        charRoot.transform.SetParent(go.transform, false);
        charRoot.AddComponent<Animator>();
        new GameObject("NameTagAnchor").transform.SetParent(charRoot.transform, false);
        PlayerVisuals visuals = go.AddComponent<PlayerVisuals>();
        SetField(visuals, "nameTagRoot", nameRoot.transform);
        SetField(visuals, "nameTagText", nameText);
        InvokeNonPublic(visuals, "Awake");
        return visuals;
    }

    private static PlayerAudio BuildPlayerAudio()
    {
        PlayerMovement movement = BuildPlayerMovement();
        GameObject go = movement.gameObject;
        AudioSource source = go.AddComponent<AudioSource>();
        PlayerAudio audio = go.AddComponent<PlayerAudio>();
        AudioClip loop = AudioClip.Create("Loop", 256, 1, 44100, false);
        AudioClip land = AudioClip.Create("Land", 256, 1, 44100, false);
        SetField(audio, "loopAudioSource", source);
        SetField(audio, "movementLoopClip", loop);
        SetField(audio, "landClip", land);
        InvokeNonPublic(audio, "Awake");
        return audio;
    }

    private static void InvokeNonPublic(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(target, null);
    }

    private static TException ExpectInner<TException>(TestDelegate action) where TException : Exception
    {
        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(action);
        Assert.That(invocation.InnerException, Is.TypeOf<TException>());
        return (TException)invocation.InnerException;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static T GetPrivate<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (T)field.GetValue(target);
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

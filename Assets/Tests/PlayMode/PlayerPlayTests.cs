using System.Reflection;
using NUnit.Framework;
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
    }

    [Test]
    public void PlayerMovementOnNetworkSpawnTest()
    {
        PlayerMovement movement = BuildPlayerMovement();
        Assert.DoesNotThrow(() => movement.OnNetworkSpawn());
        Assert.That(movement.PlayerCamera.enabled, Is.False);
        Assert.That(movement.IsGrounded, Is.False);
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

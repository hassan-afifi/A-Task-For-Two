using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public class GameSessionEditTests
{
    private const string PlayerNameKey = "session_player_name";
    private const string CharIndexKey = "session_char_index";

    [SetUp]
    public void SetUp()
    {
        CleanupSessions();
        PlayerPrefs.DeleteKey(PlayerNameKey);
        PlayerPrefs.DeleteKey(CharIndexKey);
    }

    [TearDown]
    public void TearDown()
    {
        CleanupSessions();
        PlayerPrefs.DeleteKey(PlayerNameKey);
        PlayerPrefs.DeleteKey(CharIndexKey);
    }

    [Test]
    public void SetName_AndSetChar_SaveToPlayerPrefs()
    {
        var root = new GameObject("GameSession_Test");
        var session = root.AddComponent<GameSession>();

        session.SetName("Hassan");
        session.SetChar(2);

        Assert.That(session.PlayerName, Is.EqualTo("Hassan"));
        Assert.That(PlayerPrefs.GetString(PlayerNameKey, ""), Is.EqualTo("Hassan"));
        Assert.That(session.CharIndex, Is.EqualTo(2));
        Assert.That(PlayerPrefs.GetInt(CharIndexKey, -1), Is.EqualTo(2));
    }

    [Test]
    public void SetChar_Negative_ClampsToZero()
    {
        var root = new GameObject("GameSession_Test");
        var session = root.AddComponent<GameSession>();

        session.SetChar(-5);
        Assert.That(session.CharIndex, Is.EqualTo(0));
        Assert.That(PlayerPrefs.GetInt(CharIndexKey, -1), Is.EqualTo(0));
    }

    [Test]
    public void Awake_LoadsSavedValues()
    {
        PlayerPrefs.SetString(PlayerNameKey, "SavedName");
        PlayerPrefs.SetInt(CharIndexKey, 3);

        var root = new GameObject("GameSession_Test");
        var session = root.AddComponent<GameSession>();
        Invoke(session, "Awake");

        Assert.That(session.PlayerName, Is.EqualTo("SavedName"));
        Assert.That(session.CharIndex, Is.EqualTo(3));
    }

    private static void Invoke(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Missing method {methodName}");
        method.Invoke(target, null);
    }

    private static void CleanupSessions()
    {
        GameSession[] sessions = Object.FindObjectsByType<GameSession>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sessions.Length; i++)
        {
            if (sessions[i] != null)
            {
                Object.DestroyImmediate(sessions[i].gameObject);
            }
        }

        SetGameSessionInstance(null);
    }

    private static void SetGameSessionInstance(GameSession session)
    {
        PropertyInfo instanceProperty = typeof(GameSession).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
        Assert.That(instanceProperty, Is.Not.Null, "Missing GameSession.Instance property");
        MethodInfo setMethod = instanceProperty.GetSetMethod(true);
        Assert.That(setMethod, Is.Not.Null, "Missing non-public setter for GameSession.Instance");
        setMethod.Invoke(null, new object[] { session });
    }
}

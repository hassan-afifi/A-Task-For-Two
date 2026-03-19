using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Type = System.Type;
using Object = UnityEngine.Object;

public class CharacterSelectionPlayTests
{
    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        CleanupRuntimeObjects();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        CleanupRuntimeObjects();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Navigation_ChangesCharacterName()
    {
        var root = new GameObject("CharacterSelection_Test");
        var parent = new GameObject("CharactersRoot").transform;
        parent.SetParent(root.transform, false);

        CreateCharacter(parent, "Adam");
        CreateCharacter(parent, "Brian");
        CreateCharacter(parent, "Joe");
        CreateCharacter(parent, "Sophie");
        CreateCharacter(parent, "Megan");
        CreateCharacter(parent, "Louise");

        Component label = CreateTmp(root.transform, "CharName");
        Assert.That(label, Is.Not.Null, "Could not create TMPro.TextMeshPro component.");

        var selection = root.AddComponent<CharacterSelection>();
        SetField(selection, "charactersParent", parent);
        SetField(selection, "characterNameText", label);
        SetField(selection, "rotationDuration", 0.01f);

        yield return null;

        Assert.That(GetText(label), Is.EqualTo("Adam"));

        selection.NextChar();
        yield return WaitForName(label, "Brian");
        Assert.That(GetText(label), Is.EqualTo("Brian"));

        selection.NextChar();
        yield return WaitForName(label, "Joe");
        Assert.That(GetText(label), Is.EqualTo("Joe"));

        selection.PrevChar();
        yield return WaitForName(label, "Brian");
        Assert.That(GetText(label), Is.EqualTo("Brian"));
    }

    [UnityTest]
    public IEnumerator EmptyCharacterList_DoesNotBreak()
    {
        var root = new GameObject("CharacterSelection_Empty");
        var parent = new GameObject("CharactersRoot").transform;
        parent.SetParent(root.transform, false);

        Component label = CreateTmp(root.transform, "CharName");
        Assert.That(label, Is.Not.Null, "Could not create TMPro.TextMeshPro component.");

        var selection = root.AddComponent<CharacterSelection>();
        SetField(selection, "charactersParent", parent);
        SetField(selection, "characterNameText", label);

        yield return null;

        Assert.That(GetText(label), Is.EqualTo(string.Empty));
        Assert.DoesNotThrow(() => selection.NextChar());
        Assert.DoesNotThrow(() => selection.PrevChar());
    }

    private static void CreateCharacter(Transform parent, string characterName)
    {
        var character = new GameObject(characterName);
        character.transform.SetParent(parent, false);
        character.AddComponent<Animator>();
    }

    private static IEnumerator WaitFrames(int frameCount)
    {
        for (int i = 0; i < frameCount; i++)
        {
            yield return null;
        }
    }

    private static IEnumerator WaitForName(Component label, string expectedName, int maxFrames = 120)
    {
        for (int i = 0; i < maxFrames; i++)
        {
            if (GetText(label) == expectedName)
            {
                yield break;
            }

            yield return null;
        }
    }

    private static void CleanupRuntimeObjects()
    {
        var selections = Object.FindObjectsByType<CharacterSelection>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < selections.Length; i++)
        {
            if (selections[i] != null)
            {
                Object.Destroy(selections[i].gameObject);
            }
        }

        var sessions = Object.FindObjectsByType<GameSession>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sessions.Length; i++)
        {
            if (sessions[i] != null)
            {
                Object.Destroy(sessions[i].gameObject);
            }
        }

        var netType = Type.GetType("Unity.Netcode.NetworkManager, Unity.Netcode.Runtime");
        if (netType != null)
        {
            Object[] networkManagers = Object.FindObjectsByType(netType, FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < networkManagers.Length; i++)
            {
                Component networkManager = networkManagers[i] as Component;
                if (networkManager != null)
                {
                    Object.Destroy(networkManager.gameObject);
                }
            }
        }

        SetGameSessionInstance(null);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
        field.SetValue(target, value);
    }

    private static Component CreateTmp(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var tmpType = Type.GetType("TMPro.TextMeshPro, Unity.TextMeshPro");
        if (tmpType == null)
        {
            Object.Destroy(go);
            return null;
        }

        return go.AddComponent(tmpType);
    }

    private static string GetText(Component component)
    {
        if (component == null)
        {
            return string.Empty;
        }

        PropertyInfo textProperty = component.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
        if (textProperty == null)
        {
            return string.Empty;
        }

        object value = textProperty.GetValue(component);
        return value as string ?? string.Empty;
    }

    private static void SetGameSessionInstance(GameSession session)
    {
        PropertyInfo instanceProperty = typeof(GameSession).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
        if (instanceProperty == null)
        {
            return;
        }

        MethodInfo setMethod = instanceProperty.GetSetMethod(true);
        if (setMethod == null)
        {
            return;
        }

        setMethod.Invoke(null, new object[] { session });
    }
}

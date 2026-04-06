using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

public class PuzzlePlayTests
{
    [TearDown]
    public void TearDown()
    {
        DestroyAll<PuzzleController>();
        DestroyAll<DieClickAnimate>();
    }

    [UnityTest]
    public IEnumerator RollDieTest()
    {
        DieClickAnimate die = BuildDie("DieRollTest");
        die.RollDie();
        Assert.That(die.IsRolling, Is.True);
        yield return new WaitForSeconds(1.0f);
        Assert.That(die.CurrentFace, Is.EqualTo(1));
        Assert.That(die.IsRolling, Is.False);
        die.RollDie();
        yield return new WaitForSeconds(1.0f);
        Assert.That(die.CurrentFace, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator SetLockedTest()
    {
        DieClickAnimate die = BuildDie("DieLockedTest");
        die.SetLocked(true);
        die.RollDie();
        yield return null;
        Assert.That(die.CurrentFace, Is.EqualTo(0));
        Assert.That(die.IsRolling, Is.False);
        die.SetLocked(false);
        die.RollDie();
        yield return new WaitForSeconds(1.0f);
        Assert.That(die.CurrentFace, Is.EqualTo(1));
    }

    [Test]
    public void DieClickAnimateOnNetworkSpawnTest()
    {
        DieClickAnimate die = BuildDie("DieSpawnTest");
        Assert.DoesNotThrow(() => die.OnNetworkSpawn());
        Assert.That(die.CurrentFace, Is.EqualTo(0));
        Assert.That(die.Locked, Is.False);
    }

    [Test]
    public void DieClickAnimateOnNetworkDespawnTest()
    {
        DieClickAnimate die = BuildDie("DieDespawnTest");
        die.RollDie();
        Assert.DoesNotThrow(() => die.OnNetworkDespawn());
        Assert.That(die.Locked, Is.False);
    }

    [Test]
    public void PuzzleControllerOnNetworkSpawnTest()
    {
        PuzzleController puzzle = BuildPuzzle("PuzzleSpawnTest");
        Assert.DoesNotThrow(() => puzzle.OnNetworkSpawn());
        TMP_Text sum1 = GetDisplaySumText(puzzle, "display1");
        TMP_Text sum2 = GetDisplaySumText(puzzle, "display2");
        Assert.That(sum1.text, Is.Not.Empty);
        Assert.That(sum2.text, Is.Not.Empty);
    }

    [Test]
    public void PuzzleControllerOnNetworkDespawnTest()
    {
        PuzzleController puzzle = BuildPuzzle("PuzzleDespawnTest");
        puzzle.Regenerate();
        Assert.DoesNotThrow(() => puzzle.OnNetworkDespawn());
        TMP_Text sum1 = GetDisplaySumText(puzzle, "display1");
        Assert.That(sum1.text, Is.Not.Empty);
    }

    [Test]
    public void RegenerateTest()
    {
        PuzzleController puzzle = BuildPuzzle("PuzzleRegenerateTest");
        DieClickAnimate die1 = GetPrivate<DieClickAnimate>(puzzle, "die1");
        DieClickAnimate die2 = GetPrivate<DieClickAnimate>(puzzle, "die2");
        die1.SetLocked(true);
        die2.SetLocked(true);
        puzzle.Regenerate();
        TMP_Text sum1 = GetDisplaySumText(puzzle, "display1");
        TMP_Text sum2 = GetDisplaySumText(puzzle, "display2");
        Assert.That(sum1.text, Is.Not.Empty);
        Assert.That(sum2.text, Is.Not.Empty);
        Assert.That(die1.Locked, Is.False);
        Assert.That(die2.Locked, Is.False);
        Transform bars = GetPrivate<Transform>(puzzle, "bars");
        Vector3 barsClosed = GetPrivate<Vector3>(puzzle, "barsClosedPos");
        Assert.That(bars.localPosition, Is.EqualTo(barsClosed));
    }

    [Test]
    public void EqualsTest()
    {
        Type eqType = typeof(PuzzleController).GetNestedType("PuzzleEquation", BindingFlags.NonPublic);
        Assert.That(eqType, Is.Not.Null);
        object eqA = Activator.CreateInstance(eqType);
        object eqB = Activator.CreateInstance(eqType);
        SetNestedField(eqType, eqA, "missing", 1);
        SetNestedField(eqType, eqA, "firstKnown", 2);
        SetNestedField(eqType, eqA, "secondKnown", 3);
        SetNestedField(eqType, eqA, "sum", 6);
        SetNestedField(eqType, eqB, "missing", 1);
        SetNestedField(eqType, eqB, "firstKnown", 2);
        SetNestedField(eqType, eqB, "secondKnown", 3);
        SetNestedField(eqType, eqB, "sum", 6);
        MethodInfo eqMethod = eqType.GetMethod("Equals", new[] { eqType });
        Assert.That(eqMethod, Is.Not.Null);
        bool sameEq = (bool)eqMethod.Invoke(eqA, new[] { eqB });
        Assert.That(sameEq, Is.True);
        SetNestedField(eqType, eqB, "sum", 7);
        bool differentEq = (bool)eqMethod.Invoke(eqA, new[] { eqB });
        Assert.That(differentEq, Is.False);

        Type stateType = typeof(PuzzleController).GetNestedType("NetworkPuzzleState", BindingFlags.NonPublic);
        Assert.That(stateType, Is.Not.Null);
        object stateA = Activator.CreateInstance(stateType);
        object stateB = Activator.CreateInstance(stateType);
        SetNestedField(stateType, stateA, "equation1", eqA);
        SetNestedField(stateType, stateA, "equation2", eqA);
        SetNestedField(stateType, stateA, "target1", 4);
        SetNestedField(stateType, stateA, "target2", 5);
        SetNestedField(stateType, stateA, "solved", false);
        SetNestedField(stateType, stateA, "version", 1);
        SetNestedField(stateType, stateB, "equation1", eqA);
        SetNestedField(stateType, stateB, "equation2", eqA);
        SetNestedField(stateType, stateB, "target1", 4);
        SetNestedField(stateType, stateB, "target2", 5);
        SetNestedField(stateType, stateB, "solved", false);
        SetNestedField(stateType, stateB, "version", 1);
        MethodInfo stateEqMethod = stateType.GetMethod("Equals", new[] { stateType });
        Assert.That(stateEqMethod, Is.Not.Null);
        bool sameState = (bool)stateEqMethod.Invoke(stateA, new[] { stateB });
        Assert.That(sameState, Is.True);
        SetNestedField(stateType, stateB, "solved", true);
        bool differentState = (bool)stateEqMethod.Invoke(stateA, new[] { stateB });
        Assert.That(differentState, Is.False);
    }

    private static DieClickAnimate BuildDie(string name)
    {
        GameObject go = new GameObject(name, typeof(NetworkObject), typeof(AudioSource));
        go.SetActive(false);
        Transform visual = new GameObject("DieVisual").transform;
        visual.SetParent(go.transform, false);
        AudioSource source = go.GetComponent<AudioSource>();
        AudioClip clip = AudioClip.Create($"{name}_Clip", 256, 1, 44100, false);
        DieClickAnimate die = go.AddComponent<DieClickAnimate>();
        SetField(die, "dieVisual", visual);
        SetField(die, "rollAudioSource", source);
        SetField(die, "rollClip", clip);
        InvokeNonPublic(die, "Awake");
        go.SetActive(true);
        return die;
    }

    private static PuzzleController BuildPuzzle(string name)
    {
        GameObject go = new GameObject(name, typeof(NetworkObject));
        go.SetActive(false);
        PuzzleController puzzle = go.AddComponent<PuzzleController>();
        TMP_Text sum1 = new GameObject("Sum1", typeof(TextMeshPro)).GetComponent<TMP_Text>();
        TMP_Text sum2 = new GameObject("Sum2", typeof(TextMeshPro)).GetComponent<TMP_Text>();
        Transform first1 = BuildSymbols("First1");
        Transform second1 = BuildSymbols("Second1");
        Transform first2 = BuildSymbols("First2");
        Transform second2 = BuildSymbols("Second2");
        DieClickAnimate die1 = BuildDie($"{name}Die1");
        DieClickAnimate die2 = BuildDie($"{name}Die2");
        Transform bars = new GameObject("Bars").transform;
        AudioSource barsSource = new GameObject("BarsAudio", typeof(AudioSource)).GetComponent<AudioSource>();
        AudioClip barsClip = AudioClip.Create($"{name}_BarsClip", 256, 1, 44100, false);
        SetDisplay(puzzle, "display1", sum1, first1, second1);
        SetDisplay(puzzle, "display2", sum2, first2, second2);
        SetField(puzzle, "die1", die1);
        SetField(puzzle, "die2", die2);
        SetField(puzzle, "bars", bars);
        SetField(puzzle, "barsAudioSource", barsSource);
        SetField(puzzle, "barsRaiseClip", barsClip);
        InvokeNonPublic(puzzle, "Awake");
        go.SetActive(true);
        return puzzle;
    }

    private static Transform BuildSymbols(string name)
    {
        GameObject root = new GameObject(name);

        for (int i = 0; i < 10; i++)
        {
            new GameObject($"Sym{i}").transform.SetParent(root.transform, false);
        }

        return root.transform;
    }

    private static TMP_Text GetDisplaySumText(PuzzleController puzzle, string fieldName)
    {
        FieldInfo field = puzzle.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        object display = field.GetValue(puzzle);
        FieldInfo sumField = display.GetType().GetField("sumText", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(sumField, Is.Not.Null);
        return (TMP_Text)sumField.GetValue(display);
    }

    private static void SetDisplay(PuzzleController puzzle, string fieldName, TMP_Text sumText, Transform firstKnown, Transform secondKnown)
    {
        FieldInfo field = puzzle.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        object display = field.GetValue(puzzle);
        FieldInfo sumField = display.GetType().GetField("sumText", BindingFlags.Instance | BindingFlags.Public);
        FieldInfo firstField = display.GetType().GetField("firstKnownSymbols", BindingFlags.Instance | BindingFlags.Public);
        FieldInfo secondField = display.GetType().GetField("secondKnownSymbols", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(sumField, Is.Not.Null);
        Assert.That(firstField, Is.Not.Null);
        Assert.That(secondField, Is.Not.Null);
        sumField.SetValue(display, sumText);
        firstField.SetValue(display, firstKnown);
        secondField.SetValue(display, secondKnown);
        field.SetValue(puzzle, display);
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

    private static void SetNestedField(Type type, object target, string fieldName, object value)
    {
        FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
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

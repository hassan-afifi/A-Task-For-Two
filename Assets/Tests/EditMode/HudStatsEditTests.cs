using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public class HudStatsEditTests
{
    [Test]
    public void UpdateClock_UsesInvariantAmPmFormat()
    {
        var root = new GameObject("HudStats_Test");
        var hud = root.AddComponent<HudStats>();
        var clock = new GameObject("ClockText").AddComponent<TextMeshProUGUI>();
        clock.transform.SetParent(root.transform, false);

        SetField(hud, "clockText", clock);
        SetField(hud, "clockTimer", 1f);

        Invoke(hud, "UpdateClock");

        Assert.That(Regex.IsMatch(clock.text, @"^\d{1,2}:\d{2} (AM|PM)$"), Is.True);

        Object.DestroyImmediate(root);
    }

    static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
        field.SetValue(target, value);
    }

    static void Invoke(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Missing method {methodName}");
        method.Invoke(target, args);
    }
}

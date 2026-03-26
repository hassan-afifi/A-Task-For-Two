using System.Collections.Generic;
using UnityEngine;

// Stores session data that persists between scenes.
public class GameSession : MonoBehaviour
{
    private enum PrefKey
    {
        PlayerName,
        CharIndex
    }

    private static readonly Dictionary<PrefKey, string> PrefNames = new Dictionary<PrefKey, string>
    {
        { PrefKey.PlayerName, "session_player_name" },
        { PrefKey.CharIndex, "session_char_index" }
    };

    private static string Pref(PrefKey key)
    {
        return PrefNames[key];
    }

    private static void PrefSetString(PrefKey key, string value)
    {
        PlayerPrefs.SetString(Pref(key), value);
    }

    private static string PrefGetString(PrefKey key, string defaultValue)
    {
        return PlayerPrefs.GetString(Pref(key), defaultValue);
    }

    private static void PrefSetInt(PrefKey key, int value)
    {
        PlayerPrefs.SetInt(Pref(key), value);
    }

    private static int PrefGetInt(PrefKey key, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(Pref(key), defaultValue);
    }

    // Stores the active game session singleton.
    public static GameSession Instance { get; private set; }

    // Stores the current player name.
    public string PlayerName;

    // Stores the current relay join code.
    public string JoinCode;

    // Stores the selected character index.
    public int CharIndex = 0;

    // Updates and persists the player name.
    public void SetName(string value)
    {
        PlayerName = value ?? string.Empty;
        PrefSetString(PrefKey.PlayerName, PlayerName);
    }

    // Updates and persists the selected character index.
    public void SetChar(int index)
    {
        CharIndex = Mathf.Max(0, index);
        PrefSetInt(PrefKey.CharIndex, CharIndex);
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            LoadSaved();
        }
        else
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }

    void LoadSaved()
    {
        PlayerName = PrefGetString(PrefKey.PlayerName, string.Empty);
        CharIndex = Mathf.Max(0, PrefGetInt(PrefKey.CharIndex, 0));
    }
}

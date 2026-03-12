using UnityEngine;

public class GameSession : MonoBehaviour
{
    private const string PlayerNameKey = "session_player_name";
    private const string CharIndexKey = "session_char_index";

    public static GameSession Instance;

    public string PlayerName;
    public string JoinCode;
    public int CharIndex = 0;

    public void SetName(string value)
    {
        PlayerName = value ?? string.Empty;
        PlayerPrefs.SetString(PlayerNameKey, PlayerName);
    }

    public void SetChar(int index)
    {
        CharIndex = Mathf.Max(0, index);
        PlayerPrefs.SetInt(CharIndexKey, CharIndex);
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSaved();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadSaved()
    {
        PlayerName = PlayerPrefs.GetString(PlayerNameKey, string.Empty);
        CharIndex = Mathf.Max(0, PlayerPrefs.GetInt(CharIndexKey, 0));
    }
}

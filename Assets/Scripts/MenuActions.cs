using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public static class MenuActions
{
    private const string MainMenuSceneName = "MainMenu";

    public static void ReturnToMainMenu()
    {
        StopNet();
        SceneManager.LoadScene(MainMenuSceneName, LoadSceneMode.Single);
    }

    public static void ExitGame()
    {
        StopNet();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static void StopNet()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}

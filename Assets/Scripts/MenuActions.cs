using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

// Provides shared menu actions used by UI buttons.
public static class MenuActions
{
    private enum SceneRef
    {
        MainMenu
    }

    // Shuts down networking and loads the main menu scene.
    public static void ReturnToMainMenu()
    {
        StopNet();
        SceneManager.LoadScene(SceneName(SceneRef.MainMenu), LoadSceneMode.Single);
    }

    // Shuts down networking and exits play mode or application.
    public static void ExitGame()
    {
        StopNet();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Shuts down Netcode when a session is currently active.
    static void StopNet()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    // Maps scene keys to concrete scene names.
    static string SceneName(SceneRef scene)
    {
        switch (scene)
        {
        case SceneRef.MainMenu: return "MainMenu";
        default: throw new ArgumentOutOfRangeException(nameof(scene), scene, null);
        }
    }
}

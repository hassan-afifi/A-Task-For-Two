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

    // Shuts down Netcode and removes the current NetworkManager instance.
    static void StopNet()
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (manager != null)
        {
            if (manager.IsListening)
            {
                manager.Shutdown();
            }

            UnityEngine.Object.Destroy(manager.gameObject);
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

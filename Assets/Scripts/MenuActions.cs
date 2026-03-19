using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public static class MenuActions
{
    private enum SceneRef
    {
        MainMenu
    }

    public static void ReturnToMainMenu()
    {
        StopNet();
        SceneManager.LoadScene(SceneName(SceneRef.MainMenu), LoadSceneMode.Single);
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

    static string SceneName(SceneRef scene)
    {
        switch (scene)
        {
            case SceneRef.MainMenu:
                return "MainMenu";
            default:
                throw new System.ArgumentOutOfRangeException(nameof(scene), scene, null);
        }
    }
}

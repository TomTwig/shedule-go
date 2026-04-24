using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneLoader : MonoBehaviour
{
    // Lädt eine Szene per Namen
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("LoadScene: sceneName ist leer.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    // Lädt eine Szene per Build Index
    public void LoadSceneByIndex(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"LoadSceneByIndex: Ungültiger Index {sceneIndex}");
            return;
        }

        SceneManager.LoadScene(sceneIndex);
    }

    // Lädt die nächste Szene in den Build Settings
    public void LoadNextScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("LoadNextScene: Keine nächste Szene vorhanden.");
            return;
        }

        SceneManager.LoadScene(nextIndex);
    }

    // Lädt die vorherige Szene
    public void LoadPreviousScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int previousIndex = currentIndex - 1;

        if (previousIndex < 0)
        {
            Debug.LogWarning("LoadPreviousScene: Keine vorherige Szene vorhanden.");
            return;
        }

        SceneManager.LoadScene(previousIndex);
    }

    // Spiel beenden
    public void QuitGame()
    {
        Debug.Log("Spiel wird beendet.");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
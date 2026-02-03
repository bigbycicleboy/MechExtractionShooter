using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public void QuitGame()
    {
        Debug.Log("Quitting game");
        Application.Quit();
    }

    public void LoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
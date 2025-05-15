using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Scripts
{
    public class PauseMenuController : MonoBehaviour
    {
        public GameObject pauseMenuUI;
        public bool isPaused = false;
        
        public void ResumeGame()
        {
            isPaused = false;
            pauseMenuUI.SetActive(false);
            Time.timeScale = 1f;
        }

        public void PauseGame()
        {
            isPaused = true;
            pauseMenuUI.SetActive(true);
            Time.timeScale = 0f;
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}

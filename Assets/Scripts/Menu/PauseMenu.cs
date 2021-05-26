using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool Paused;

    [SerializeField] private GameObject pauseMenuUI;

    private void Start()
    {
        if (Paused != pauseMenuUI.activeSelf)
        {
            if (Paused)
            {
                Pause();
            } else
            {
                Resume();
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Paused)
            {
                Resume();
            } else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        Paused = false;

        Animator animator = pauseMenuUI.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play("Entry", -1);
        }
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        Paused = true;
    }

    public void QuitGame()
    {
        SceneManager.LoadScene("UI Test Scene");
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    //Create a singleton instance so that we can reference the pausemenu from anywhere
    public static PauseMenu Instance;

    [SerializeField] private GameObject menu;
    [SerializeField] private GameObject bars;
    [SerializeField] private GameObject names;
    public static bool GamePaused = false;

    void Awake() {
        //Initialize the singleton of the pause menu
        if(Instance == null)
        {
            Instance = this;
        }
    }    

    //Handle pause input (Escape / Start)
    //This function is called by PlayerController Pause()
    public void ToggleMenu()
    {
        if (!GamePaused)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    //Pause Game
    private void PauseGame()
    {
        GamePaused = true;
        Time.timeScale = 0f;
        bars.SetActive(false);
        names.SetActive(false);
        menu.SetActive(true);
    }

    //Resume Game
    public void ResumeGame()
    {
        GamePaused = false;
        Time.timeScale = 1f;
        menu.SetActive(false);
        bars.SetActive(true);
        names.SetActive(true);
    }

    //Return to main menu
    public void MainMenu()
    {
        GamePaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        menu.SetActive(false);
    }

}

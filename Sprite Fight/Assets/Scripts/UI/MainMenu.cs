using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject characterMenu;
    [SerializeField] private GameObject optionsMenu;

    void Start() {
        mainMenu.SetActive(true);
    }
    
    public void PlayButton()
    {
        mainMenu.SetActive(false);
        characterMenu.SetActive(true);
    }

    public void OptionsButton()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }

    public void QuitButton()
    {
        Debug.Log("Pressed Quit");
        Application.Quit();
    }

}

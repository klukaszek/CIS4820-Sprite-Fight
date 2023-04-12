using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    private GameObject characterMenu;
    public static Fighter.Character p1CharacterSelection;
    public static Fighter.Character p2CharacterSelection;

    void Start() {
        characterMenu = transform.gameObject;
    }

    //Reset current scene context and then go to the next scene
    private void StartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        characterMenu.SetActive(false);
        PauseMenu.GamePaused = false;
    }

    //Start game as Samurai
    public void SelectSamurai()
    {
        Random.InitState(Time.frameCount);

        //Set player to samurai, and NPC to random character
        p1CharacterSelection = Fighter.Character.Samurai;
        p2CharacterSelection = (Fighter.Character) Random.Range(0, 2);
        //Load next scene in build index
        StartGame();
    }

    //Start game as Huntress
    public void SelectHuntress()
    {
        //Set player to samurai, and NPC to random character
        p1CharacterSelection = Fighter.Character.Huntress;
        p2CharacterSelection = (Fighter.Character)Random.Range(0, 2);
        //Load next scene in build index
        StartGame();
    }

    //Go back to main menu
    public void BackButton()
    {
        characterMenu.SetActive(false);
        mainMenu.SetActive(true);
    }
}

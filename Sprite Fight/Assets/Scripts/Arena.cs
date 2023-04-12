using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[RequireComponent(typeof(LineRenderer))]
public class Arena : MonoBehaviour
{
    //Arena singleton
    public static Arena Instance;

    #region Variables
    [SerializeField] public float radius;
    [SerializeField] private float updateDelay = 0.4f;
    [SerializeField] private LayerMask targetMask;
    public bool inArena;

    [Header("Outline Settings")]
    [Range(0.0f, 1.0f)] [SerializeField] private float opacity = 0.6f;
    [Range(0.0f, 0.1f)] [SerializeField] private float width = 0.01f;

    [Header("Playable Fighter Choices")]
    [SerializeField] private List<GameObject> playableFighters;

    [Header("NPC Fighter Choices")]
    [SerializeField] private List<GameObject> npcFighters;

    [Header("Left Hand UI Bars")]
    [SerializeField] private Bar leftHealth;
    [SerializeField] private Bar leftStamina;

    [Header("Right Hand UI Bars")]
    [SerializeField] private Bar rightHealth;
    [SerializeField] private Bar rightStamina;

    [Header("Player Names")]
    [SerializeField] private GameObject p1Name;
    [SerializeField] private GameObject p2Name;

    [Header("Winner UI Text")]
    [SerializeField] private GameObject winnerText;

    //Private variables
    private Fighter[] fightersInArena;
    private Fighter p1Fighter, p2Fighter;
    #endregion

    #region Initialization

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 0f;
        LineRenderer circleRenderer = GetComponent<LineRenderer>();

        //Draw radius of arena
        DrawArena(circleRenderer, radius, 200);

        //Load fighters into the scene from prefabs
        LoadFighters();

        //Set player character names
        SetNames();

        //Once fighters are loaded into the scene, we begin checking to see if they are out of bounds
        StartCoroutine(ArenaUpdate());
        Debug.Log("Started arena");

        Time.timeScale = 1f;
    }

    //Set names under health and stamina bars based on which fighters were selected
    private void SetNames()
    {
        TextMeshProUGUI p1Text = p1Name.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI p2Text = p2Name.GetComponent<TextMeshProUGUI>();

        //Set names under health bar and stamina bar
        p1Text.SetText(p1Fighter.GetCharacter().ToString().ToUpper());
        p2Text.SetText(p2Fighter.GetCharacter().ToString().ToUpper());
    }

    //Draw a circle to indicate the arena size
    private void DrawArena(LineRenderer circleRenderer, float radius, int steps)
    {
        circleRenderer.positionCount = steps;
        circleRenderer.loop = true;
        circleRenderer.startWidth = width;
        circleRenderer.endWidth = width;
        circleRenderer.startColor = new Color(255f, 0, 0, opacity);
        circleRenderer.endColor = new Color(255f, 0, 0, opacity);

        //iterate through each point along the curve of a circle and set the rendering position for each point
        for(int curStep = 0; curStep < steps; curStep++)
        {
            float circProgress = (float)curStep/steps;
            float curRadian = circProgress * 2 * Mathf.PI;

            float xScaled = Mathf.Cos(curRadian);
            float yScaled = Mathf.Sin(curRadian);

            float x = xScaled * radius;
            float y = yScaled * radius;

            Vector3 position = new Vector3(x, 0, y+transform.position.z);

            circleRenderer.SetPosition(curStep, position);
        }
    }

    //This function only really exists because I was planning on implementing PvP but decided not to due to time constraints
    private void LoadFighters()
    {
        loadSinglePlayer();
    }

    //Load 1 player with NPC opponent
    private void loadSinglePlayer()
    {
        GameObject p1, p2;

        //Instantiate player 1
        int i = (int) CharacterMenu.p1CharacterSelection;
        if(i < playableFighters.Count) p1 = Instantiate(playableFighters[i]);
        else return;

        //Disable game object so that we can reenable it once we set the necessary bars for our fighter
        p1.SetActive(false);
        p1.name = "Player " + CharacterMenu.p1CharacterSelection;

        p1Fighter = p1.GetComponentInChildren<Fighter>();
        p1Fighter.healthBar = leftHealth;
        p1Fighter.staminaBar = leftStamina;

        //Instantiate player 2
        i = (int) CharacterMenu.p2CharacterSelection;
        if(i < playableFighters.Count) p2 = Instantiate(npcFighters[i]);
        else return;

        //Disable game object so that we can reenable it once we set the necessary bars for our fighter
        p2.SetActive(false);
        p2.name = "NPC " + CharacterMenu.p2CharacterSelection;

        p2Fighter = p2.GetComponentInChildren<Fighter>();
        p2Fighter.healthBar = rightHealth;
        p2Fighter.staminaBar = rightStamina;
        
        //Spawn player 1
        p1.SetActive(true);
        Debug.Log("Player " + CharacterMenu.p1CharacterSelection + " Spawned");

        //Spawn player 2
        p2.SetActive(true);
        Debug.Log("NPC " + CharacterMenu.p1CharacterSelection + " Spawned");
    }

    //Check if a fighter has left the arena
    private IEnumerator ArenaUpdate()
    {
        //Look for fighter colliders within the radius
        Collider[] collisions = Physics.OverlapSphere(transform.position, radius-0.1f, targetMask);

        //initialize fighters array with length of collisions
        fightersInArena = new Fighter[collisions.Length];

        //Get all fighters that are within the radius
        for(int i = 0; i < collisions.Length; i++)
        {
            fightersInArena[i] = collisions[i].gameObject.GetComponent<Fighter>();
        }

        //Check if a fighter has left the radius
        while (true)
        {
            yield return new WaitForSeconds(updateDelay);
            ArenaCheck();
        }
    }
    #endregion

    //Check if both fighters are still within the arena radius
    //If one of them is outside of the arena or loses, the winner is displayed and the game returns to the main menu
    private void ArenaCheck()
    {
        //Look for fighters within the radius
        Collider[] collisions = Physics.OverlapSphere(transform.position, radius-0.05f, targetMask);

        if(collisions.Length >= 2) inArena = true;
        else inArena = false;

        //Find fighter that is not within the radius and make them die
        if(collisions.Length == 1 && !inArena)
        {
            Fighter fighter = collisions[0].gameObject.GetComponent<Fighter>();
            for(int i = 0; i < fightersInArena.Length; i++)
            {
                //If the fighter is not in the arena, then it should die
                if(!fightersInArena[i].Equals(fighter)) fightersInArena[i].Die();
            }

            //No longer update the arena
            StopAllCoroutines();

            //Determine winner and return to menu
            StartCoroutine(EndBattle());
        }
    }

    //Determine and display winner and then return to the main menu
    private IEnumerator EndBattle()
    {
        TextMeshProUGUI text = winnerText.GetComponent<TextMeshProUGUI>();

        //Set text depending on who won the game
        if(p1Fighter.IsDead()) text.SetText(p2Fighter.GetCharacter().ToString().ToUpper() + " WINS!");
        else if(p2Fighter.IsDead()) text.SetText(p1Fighter.GetCharacter().ToString().ToUpper() + " WINS!");

        //Wait 1 second before displaying the winner
        yield return new WaitForSeconds(1f);

        //Play victory sound
        AudioManager.Instance.PlaySound("Victory Cry");
        winnerText.SetActive(true);

        //Wait 4 seconds before returning to menu
        yield return new WaitForSeconds(4f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
}

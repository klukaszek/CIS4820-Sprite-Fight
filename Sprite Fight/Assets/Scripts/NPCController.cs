using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class NPCController : MonoBehaviour
{

    #region GameObjects and Components
    private Rigidbody body;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    //public Arena arena;
    private GameObject sprite;
    public GameObject player;
    private Fighter playerFighter, npcFighter;
    public NavMeshAgent navMeshAgent;
    #endregion

    #region Miscellaneous Values
    private Vector3 playerPosition;
    private Vector3 currentPosition;
    #endregion

    #region Movement & Rotation Variables
    private Vector3 npcDestination;
    public float movementTimer;
    public float movementDelay = 1f;

    private float rotationSpeed = 5.0F;
    #endregion

    #region Combat Variables
    private float attackRange;
    #endregion

    #region State Management Variables
    [SerializeField] private int rng_seed = 0;
    public State currentState;
    public StateType currentStateType;
    public bool recoveringFromTired = false;

    public enum StateType { neutral, combat, defensive, desperate, bad_spot, combat_defensive, defensive_combat }    
    enum EventType { outside_range, tired_recovery, in_range_and_healthy, in_range_and_half_hp, in_range_and_low_hp, close_to_ring, parried, opponent_tired }

    //In order of EventType
    StateType[,] state_matrix = new StateType[,]
    {
       { StateType.neutral,          StateType.neutral,          StateType.defensive,         StateType.neutral,     StateType.neutral   },  //outside range
       { StateType.bad_spot,         StateType.bad_spot,         StateType.bad_spot,          StateType.bad_spot,    StateType.bad_spot   },  //Recovering from tired
       { StateType.combat_defensive, StateType.combat_defensive, StateType.combat_defensive,  StateType.desperate,   StateType.bad_spot  },  //In range and healthy
       { StateType.defensive_combat, StateType.defensive_combat, StateType.defensive_combat,  StateType.desperate,   StateType.bad_spot  },  //In range and below half hp but greater than 20% HP
       { StateType.desperate,        StateType.desperate,        StateType.desperate,         StateType.desperate,   StateType.bad_spot  },  //Less than 20% HP
       { StateType.bad_spot,         StateType.bad_spot,         StateType.bad_spot,          StateType.bad_spot,    StateType.bad_spot  },  //Within 1 unit of ring out
       { StateType.combat,           StateType.combat,           StateType.combat,            StateType.combat,      StateType.combat    },  //Successfully parried the player attack
       { StateType.combat,           StateType.combat,           StateType.combat,            StateType.combat,      StateType.combat    }   //Player is tired
    };
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        //initialize random with a set seed
        Random.InitState(rng_seed);
 
        //get arena information
        //arena = GameObject.FindGameObjectWithTag("Arena").GetComponent<Arena>();

        //get components attached to npc
        body = GetComponent<Rigidbody>();
        npcFighter = GetComponent<Fighter>();
        sprite = npcFighter.GetSprite();
        spriteRenderer = sprite.GetComponent<SpriteRenderer>();
        anim = sprite.GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        //Setup player tracking
        GameObject[] players = GameObject.FindGameObjectsWithTag("Fighter");
        if(players.Length > 0)
        {
            int i = 0;
            while(players[i] == transform.gameObject)
            {
                i++;
            }
            player = players[i];
            playerFighter = player.GetComponent<Fighter>();
        }   

        currentStateType = StateType.neutral;
        //set default state
        currentState = new StateNeutral();

        //set movement timer
        movementTimer = movementDelay;

        attackRange = npcFighter.GetMaxAttackRange();
        navMeshAgent.speed = npcFighter.currentMoveSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        //Increment timer to determine if a new movement should be done
        movementTimer += Time.deltaTime;

        //Get positions for certain checks and calculations
        currentPosition = transform.position;
        playerPosition = player.transform.position;

        //Allow ring out when the npc is hit
        if(npcFighter.isStunned && CloseToRing()) StartCoroutine(AllowRingOut());

        //For some odd reason npcFighter.isTired does not work here even though it works everywhere else in the code base ???
        if(npcFighter.staminaBarDepleted()) recoveringFromTired = true;

        //Don't update if NPC can't be controlled
        if(!npcFighter.IsControllable()) 
        {
            //In the event that the NPC is not dead, we still want to update rotation and movement
            if(!npcFighter.IsDead())
            {
                //Make sure sprite doesn't become tired on some strange angle which causes the sprite to not appear
                NpcRotate();
                NpcMove();
            }
            else
            {
                if(navMeshAgent.enabled) navMeshAgent.destination = currentPosition;
            }

            //Make sure sprite follows collider in case npc gets attacked while tired
            sprite.transform.position = currentPosition;

            return;
        }
    
        //Begin performing actions again once the NPC is above 50% stamina
        if(npcFighter.GetStamina() > npcFighter.GetMaxStamina() / 3) recoveringFromTired = false;

        //if npc is currently attacking, don't update state
        if(!npcFighter.isAttacking && !playerFighter.IsDead())
        {
            UpdateState(currentStateType);
            currentState.Execute(this);
        }

        //Make sure sprite follows collider
        sprite.transform.position = currentPosition;

        //rotate npc
        NpcRotate();

        //move npc if the NavMeshAgent is currently enabled
        if(navMeshAgent.enabled) NpcMove();
    }

    #region Movement

    //Move player if movement direction is not (0,0,0)
    private void NpcMove()
    {
        float distanceFromPlayer = (currentPosition - playerPosition).magnitude;
        float distanceFromDestination = (currentPosition - npcDestination).magnitude;

        //Set navMeshAgent speed to current move speed of NPC
        navMeshAgent.speed = npcFighter.currentMoveSpeed;

        if(!navMeshAgent.enabled) return; 

        //Stop NPC movement if the player died or if NPC is tired
        if(playerFighter.IsDead() || npcFighter.isTired)
        {
            npcFighter.isMoving = false;
            navMeshAgent.destination = currentPosition;
        }
        else if(npcFighter.isRolling)
        {
            //body.position = navMeshAgent.nextPosition;
            navMeshAgent.destination = npcDestination;

            //set is moving to false to not play move animation
            npcFighter.isMoving = false;
        }
        //General NPC movement logic
        //Move NPC if they are further than 0.6 units from player and they are not idling at their target destination
        else if(distanceFromDestination > 0.25f && distanceFromPlayer > attackRange && !npcFighter.isStunned && !npcFighter.IsDead())
        {
            //body.position = navMeshAgent.nextPosition;
            navMeshAgent.destination = npcDestination;
            npcFighter.isMoving = true;
        }
        //NPC is somehow idling (undesired behaviour), so we reset the movement timer to force a movement action
        else 
        {
            //body.position = currentPosition;
            navMeshAgent.destination = currentPosition;
            npcFighter.isMoving = false;
            movementTimer = movementDelay;
        }

        //Set animation float parameter for movement animation
        anim.SetFloat("Moving", npcFighter.isMoving ? 1f: 0f);
    }

    //update NPC destination on navmesh
    public void SetDestination(Vector3 destination) => npcDestination = destination;

    //Set npc destination to somewhere on the navmesh within the specified distance
	public void RandomMove(float moveDistance)
	{
		Vector3 direction = Random.insideUnitSphere * moveDistance;

		direction += transform.position;

		NavMeshHit pointHit;

		//Check if the random position exists on the navmesh and store it pointHit
		//Use -1 layer mask to indicate all layers
		if(NavMesh.SamplePosition(direction, out pointHit, moveDistance, -1))
		{
			SetDestination(pointHit.position);
		}		
	}
    #endregion

    #region Rotation 
    private void NpcRotate()
    {   
        //keep original rotation speed as local variable
        float originalRotationSpeed = rotationSpeed;

        //if player is rolling, rotate faster for better game feel
        if (playerFighter.isRolling) 
        {
            rotationSpeed = 1.5F;
        }

        //Direction vector of player to enemy
        Vector3 direction = playerPosition - currentPosition;

        //Current rotation angle of player in euler angles
        float rotY = transform.eulerAngles.y;

        /*
        Atan2 can determine the angle from origin if a set of coordinates is given
        Normally one would use Atan2(direction.y, direction.x) on a cartesian plane where 0 degrees is to the east
        However, Unity has 0 degrees to the north, so we must flip direction.y and direction.x
        direction.y is then replaced with direction.z since we are working in R3 and not R2
        */
        float rotationAngle = (Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
        
        /*
        To rotate the sprite in a way that it faces the camera properly,
        we need to subtract 90 degrees from its Y rotation
        */
        float spriteAngle = rotationAngle - 90F;
        float curSpriteAngle = sprite.transform.eulerAngles.y;

        //convert angle to be between [0, 359] degrees
        rotationAngle = (rotationAngle + 360) % 360;

        /*
        Check if the sprite needs a predetermined angle
        Otherwise this returns the spriteAngle that was passed as a parameter
        */
        spriteAngle = SpriteRotate(rotY, spriteAngle);

        /*
        Apply angle using Mathf.LerpAngle() so that the rotation movement is less uniform as it interpolates over time 
        and to make sure the goalie doesnt spin from 359->0 instead of just regularly rotating to 0 from 359
        */
        rotationAngle = Mathf.LerpAngle(rotY, rotationAngle, rotationSpeed * Time.deltaTime);
        spriteAngle = Mathf.LerpAngle(curSpriteAngle, spriteAngle, rotationSpeed * Time.deltaTime);

        //Get rotation quaternions for controller and sprite
        Quaternion rotation = Quaternion.Euler(0F, rotationAngle, 0F);
        Quaternion spriteRotation = Quaternion.Euler(0F, spriteAngle, 0F); 
        
        //Set rotation along Y-Axis
        transform.rotation = rotation;
        sprite.transform.rotation = spriteRotation;

        //reset rotation speed if it was changed
        rotationSpeed = originalRotationSpeed;

        //Draw a ray in the direction the player is facing
        Debug.DrawRay(transform.position, rotation * Vector3.forward, Color.red);
        Debug.DrawRay(transform.position, spriteRotation * Vector3.forward, Color.yellow);
    }

    /*
    Determines if the sprite needs to oriented in a way such that 
    the player can still see the sprite even at extreme angles.
    Return new spriteAngle if the sprite needs to be reoriented.
    */
    float SpriteRotate(float rotY, float spriteAngle)
    {   
        //Looking up from the left side of Y-Axis
        if(rotY >= 0 && rotY < 40)
        {
            spriteAngle = 310F;
        }
        //Looking down from the left side of Y-Axis
        else if(rotY >= 140 && rotY < 180)
        {
            spriteAngle = 40F;
        }
        //Looking down from the right side of Y-Axis
        else if(rotY >= 180 && rotY < 220)
        {
            spriteAngle = 130F;
        }
        //Looking up from the right side of Y-Axis
        else if(rotY >= 320 && rotY < 360)
        {
            spriteAngle = 230F;
        }

        return spriteAngle;
    }
    #endregion

    #region State Management Functions
    
	//Performs all event checks in the correct order to determine the next state of the AI
	private EventType GetCurrentEvent()
    {
        if (recoveringFromTired) return EventType.tired_recovery;
        if (CloseToRing()) return EventType.close_to_ring;
        if (npcFighter.successfulParry) return EventType.parried;
        if (playerFighter.isTired) return EventType.opponent_tired;

        //Combat checks
        if (TargetInRange())
        {
            float hp = npcFighter.GetHealth();
            if(hp <= npcFighter.GetMaxHealth() / 5) return EventType.in_range_and_low_hp;
            if(hp <= npcFighter.GetMaxHealth() / 2 && hp > npcFighter.GetMaxHealth() / 5) return EventType.in_range_and_half_hp;
            if(npcFighter.IsHealthy()) return EventType.in_range_and_healthy;
        }

        //Neutral state
        return EventType.outside_range;
    }

    //Changes the AI's current state (should be called by UpdateState())
	public void ChangeState(State new_state)
	{
		currentState = new_state;
	}

	//References the transition table and calls ChangeState() if any changes need to be made to the AI's current staate
	private void UpdateState(StateType current_state)
	{
		EventType current_event = GetCurrentEvent();
		current_state = state_matrix[(int)current_event, (int)current_state];

        if(current_state == currentStateType) return;
        
        int random;

        //set change current state to new state object
		switch (current_state){

            case StateType.neutral:
                ChangeState(new StateNeutral());
                break;

            case StateType.combat:
                ChangeState(new StateCombat());
                break;

            case StateType.defensive:
                ChangeState(new StateDefensive());
                break;

            case StateType.combat_defensive:
                random = Random.Range(0, 5);

                //Lower chance for defensive, higher chance for combat
                if(random < 3) ChangeState(new StateCombat());
                else ChangeState(new StateDefensive()); 
                break;

            case StateType.defensive_combat:
                random = Random.Range(0, 2);

                //50 / 50
                if(random == 0) ChangeState(new StateDefensive());
                else ChangeState(new StateCombat());
                break;

            case StateType.desperate:
                ChangeState(new StateDesperate());
                break;

            case StateType.bad_spot:
                ChangeState(new StateBadSpot());
                break;
        }
	}
    #endregion

    #region Event Checking Functions

    //Disable NavMeshAgent when the NPC is hit so that it can be knocked out of the arena
    private IEnumerator AllowRingOut()
    {
        IDictionary<string, float> animations = npcFighter.GetAnimationDurations();
        navMeshAgent.enabled = false;
        yield return new WaitForSeconds(animations["Take Hit"] + 0.3f);
        if(!npcFighter.IsDead()) navMeshAgent.enabled = true;
    }

	//Returns distance to target from AI current position
	private float DistanceToTarget() => (playerFighter.transform.position - transform.position).magnitude;
    public bool TargetInRange() => DistanceToTarget() < 0.7f;
    private float DistanceFromRingOrigin() => (Arena.Instance.transform.position - transform.position).magnitude;
    private bool CloseToRing() => DistanceFromRingOrigin() > Arena.Instance.radius - 0.6f;

    #endregion

    #region Getters
    public Fighter GetNpcFighter() => npcFighter;
    public Fighter GetPlayerFighter() => playerFighter;

    #endregion
}
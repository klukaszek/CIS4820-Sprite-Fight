using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Fighter))]
[RequireComponent(typeof(ActionManager))]
public class PlayerController : MonoBehaviour
{

#region Variable Declarations
    
    #region GameObjects and Components
    private Fighter fighter;
    private Rigidbody body;
    private ActionManager actionManager;
    private Animator anim;
    private GameObject sprite;
    private GameObject enemy;
    #endregion

    #region Movement Variables
    private Vector3 inputDirection;
    private Vector3 movementDirection;
    #endregion

    #region Rotation Variables
    private float rotationSpeed = 5.0F;
    private float turnSmoothTime = 0.1F;
    private float turnSmoothVelocity;
    #endregion

    #region Blocking Variables
    private bool blockDelay = false;
    #endregion
    
    #region Position and Direction Variables
    private Vector3 playerPosition;
    private Vector3 enemyPosition;
    private Vector3 direction;
    #endregion

#endregion

    //Get components before first frame
    private void Start()
    {
        fighter = GetComponent<Fighter>();
        body = fighter.GetRigidbody();
        actionManager = fighter.GetActionManager();
        sprite = fighter.GetSprite();
        anim = fighter.GetAnimator();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Fighter");
        if(enemies.Length > 0)
        {
            int i = 0;
            while(enemies[i] == transform.gameObject)
            {
                i++;
            }
            enemy = enemies[i];
        }
    }

    //Update GameObject every frame
    private void Update()
    {
        if(PauseMenu.GamePaused) return;

        playerPosition = transform.position;
        enemyPosition = enemy.transform.position;

        //Update player facing angle and rotate sprite accordingly
        PlayerRotate();
    }

    //Used for RigidBody since Physics calls are not done every frame but instead at a fixed rate in Unity
    private void FixedUpdate() 
    {
        //Check if player can be moved, otherwise make sure movement is not updated
        if (!fighter.IsControllable()) 
        {
            fighter.isMoving = false;

            //Make sure sprite follows controller in the event that the player is hit while tired
            sprite.transform.position = transform.position;
            //Set animation float parameter for movement animation
            anim.SetFloat("Moving", fighter.isMoving ? 1f: 0f);
            return;
        }

        //Update player position and animate movement
        PlayerMove();
    }

    //Apply movement displacement to rigidbody, called by FixedUpdate()
    private void PlayerMove()
    {
        //Create a new movement vector without the Y component and multiply it by move speed
        Vector3 movement = new Vector3(inputDirection.x, 0F, inputDirection.z) * fighter.currentMoveSpeed;

        //Set isMoving state when moving 
        if (movement.magnitude > 0) fighter.isMoving = true;
        else fighter.isMoving = false;

        //Apply displacement to rigidbody
        body.MovePosition(body.position + movement * Time.deltaTime);
        
        //Set animation float parameter for movement animation
        anim.SetFloat("Moving", fighter.isMoving ? 1f: 0f);
        
        //Draw a ray in the direction the player is moving
        Debug.DrawRay(transform.position, movement, Color.green);

        //Make sure sprite follows controller
        sprite.transform.position = transform.position;
    }

#region Rotation Handling
    //Apply player rotation for movement as well as sprite, called by Update()
    private void PlayerRotate()
    {
        //Direction vector of player to enemy
        direction = enemyPosition - playerPosition;

        //Calculate the distance between the player and enemy
        float enemyDistance = Vector3.Distance(playerPosition, enemyPosition);

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

        //Determine the movement direction using the same logic as rotationAngle
        float movementAngle = (Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg);
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

        //Smooth between rotation from Player angle and current angle
        float moveSmoothAngle = Mathf.SmoothDampAngle(rotY, movementAngle, ref turnSmoothVelocity, turnSmoothTime);
        Quaternion moveRotation = Quaternion.Euler(0F, moveSmoothAngle, 0F);

        //Determine the direction that the player will move in
        movementDirection = moveRotation * Vector3.forward;

        //Draw a ray in the direction the player is facing
        Debug.DrawRay(transform.position, rotation * Vector3.forward, Color.red);
        Debug.DrawRay(transform.position, spriteRotation * Vector3.forward, Color.yellow);
    }

    /*
    Determines if the sprite needs to oriented in a way such that 
    the player can still see the sprite even at extreme angles.
    Return new spriteAngle if the sprite needs to be reoriented.
    */
    private float SpriteRotate(float rotY, float spriteAngle)
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

#region Player Input Functionality
    #region Movement Control Functionality
    //PlayerInput Move function, read input from context and then create a direction vector using the input values
    public void Move(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();

        //If player is hitstunned or blocking, restrict movement
        if(fighter.isStunned) input = Vector2.zero;
        
        inputDirection = new Vector3(input.x, 0.0F, input.y);
    }    

    #endregion

    #region Rolling Functionality

    //PlayerInput Roll function, return if the roll input has not just begun, and return if the Player is performing an action.
    public void Roll(InputAction.CallbackContext context)
    {
        if (!fighter.IsControllable()) return;
        if (fighter.isStunned) return;
        if (fighter.performingAction) return;
        if (!fighter.isMoving) return;
        if (!context.started) return;

        actionManager.Roll();
    }
    
    #endregion

    #region Attacking Functionality

    //Input Attack function, return if the click input has not just begun, and return if the player is currently attacking
    public void LightAttack(InputAction.CallbackContext context)
    {
        if (!fighter.IsControllable()) return;
        if (fighter.isStunned) return;
        if (fighter.performingAction) return;
        if (!context.performed) return;

        fighter.performingAction = true;

        actionManager.LightAttack();
    }

    //Input Heavy Attack function, return if the click input has not just begun, and return if the player is currently attacking
    public void HeavyAttack(InputAction.CallbackContext context)
    {
        if (!fighter.IsControllable()) return;
        if (fighter.isStunned) return;
        if (fighter.performingAction) return;
        if (!context.performed) return;

        fighter.performingAction = true;

        actionManager.HeavyAttack();
    }
    #endregion

    #region Blocking Functionality
    //PlayerInput Block function
    public void Block(InputAction.CallbackContext context)
    {
        if (fighter.isStunned) return;
        if (fighter.isRolling) return;
        if (fighter.isAttacking) return;
        
        //Block button held
        if (context.performed && fighter.IsControllable() && !blockDelay)
        {
            blockDelay = true;
            actionManager.BeginBlock();
            StartCoroutine(WaitForBlock());
        }
        //Block button released
        else if (context.canceled && blockDelay)
        {
            actionManager.EndBlock();
        }
    }

    //This solves a Unity Animator Trigger bug where a trigger can get stuck on enabled
    IEnumerator WaitForBlock()
    {
        yield return new WaitUntil(() => !fighter.isBlocking);
        anim.SetTrigger("End Block");
        yield return new WaitForSeconds(actionManager.GetParryTime());
        anim.ResetTrigger("End Block");
        blockDelay = false;
    }
    #endregion

    #region Pause Functionality
    public void Pause(InputAction.CallbackContext context)
    {
        if(context.performed)
        {
            Debug.Log("Pressed Pause");
            PauseMenu.Instance.ToggleMenu();
        }
    }
    #endregion
#endregion

#region Helper Functions
#endregion

}
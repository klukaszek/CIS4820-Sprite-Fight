using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Fighter))]
public class ActionManager : MonoBehaviour
{

    #region Game Objects and Components
    private Fighter fighter;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    #endregion

    #region Hitbox Settings
    //Full sizes of attack hitboxes, used by OverlapBox as halfExtents when divided in half
    private Vector3 hitboxCenter;    
    private Vector3 lightAttackHitbox;
    private Vector3 heavyAttackHitbox; 
    private Quaternion hitboxOrientation;
    private float attackCloseness = 2f;
    private LayerMask entityMask;

    #endregion

    #region Attacking variables
    private float lightAttackDamage;
    private float heavyAttackDamage;
    private float lightAttackKnockback;
    private float heavyAttackKnockback;
    public enum AttackType {Light, Heavy};
    #endregion

    #region Blocking variables
    [Header("Blocking Variables")]
    [SerializeField] private float parryTime;
    [SerializeField] private float blockStaminaDecayRate = 5f;
    #endregion

    #region Rolling variables

    #endregion

    #region Animation information
    private IDictionary<string, float> animationTimings = new Dictionary<string, float>();
    #endregion

    //Initialize action manager before first frame
    private void Start() 
    {
        fighter = GetComponent<Fighter>();
        anim = fighter.GetAnimator();
        spriteRenderer = fighter.GetSpriteRenderer();
        entityMask = LayerMask.GetMask("Fighters");
        
        //Update hitbox center and orientation before checking
        if(fighter.GetCharacter() == Fighter.Character.Huntress) attackCloseness = 3f;

        //Get animation timings from animator attached to entity
        animationTimings = fighter.GetAnimationDurations();

        //Begin coroutine that updates fighter when blocking
        StartCoroutine(BlockChecker());
    }

    #region Attacking functions
    //Perform a light attack with the correct parameters based on the current class
    //and check if the attack connects with an entity
    public void LightAttack()
    {
        //We pass our Attack function with Light param as a C# action so that it can be passed to the coroutine
        StartCoroutine(WaitForAttack(animationTimings["Light Attack"], () => Attack(AttackType.Light)));

        anim.SetTrigger("Light Attack");
    }

    //Perform a heavy attack with the correct parameters based on the current class
    //and check if the attack connects with an entity
    public void HeavyAttack()
    {
        //We pass our Attack function with the Heavy param as a C# action so that it can be passed to the coroutine
        StartCoroutine(WaitForAttack(animationTimings["Heavy Attack"], () => Attack(AttackType.Heavy)));

        anim.SetTrigger("Heavy Attack");
    }

    //Perform an attack with the correct setting based on the enum value passed to the function
    private void Attack(AttackType attack)
    {
        Vector3 hitbox;
        float attackDamage;
        float staminaDamage;
        float knockback;

        //Set attack stats based on the attack being performed
        switch (attack)
        {
            case AttackType.Light:
                hitbox = fighter.GetLightAttackHitbox();
                attackDamage = fighter.GetLightAttackDamage();
                staminaDamage = fighter.GetMaxStamina()/8f;
                knockback = fighter.GetLightAttackKnockback();
                break;

            case AttackType.Heavy:
                hitbox = fighter.GetHeavyAttackHitbox();
                attackDamage = fighter.GetHeavyAttackDamage();
                staminaDamage = fighter.GetMaxStamina()/4f;
                knockback = fighter.GetHeavyAttackKnockback();
                break;

            //Something went wrong
            default:
                return;
        }

        //Play attack sound
        AudioManager.Instance.PlaySound("Attack");

        //Consume stamina for performing the action
        fighter.takeStaminaDamage(staminaDamage);

        //Check if the attack connects
        GameObject target = CheckHit(hitbox);

        //if not, return
        if(target == null) return;

        //Get target hurtbox and fighter
        Fighter targetFighter = target.GetComponent<Fighter>();

        //Do not apply damage to a rolling fighter or a parrying fighter
        if(targetFighter.isRolling) return;

        //If the target that is hit is parrying, apply hitstun to the attacker
        if(targetFighter.isParrying) 
        {
            Parry(targetFighter);
            return;
        }

        //Reduce damage and knockback to a blocking fighter
        if(targetFighter.isBlocking)
        {
            attackDamage /= 2;
            knockback /= 2;
        }

        //Apply attack knockback to target in the direction that entity is facing
        targetFighter.ApplyKnockback(transform.forward/2f, knockback);
        
        //Apply damage to target
        targetFighter.takeHealthDamage(attackDamage);
    }

    //Coroutine that waits for attack to finish depending on waitTime and resets any changes to the player once the attack is complete
    IEnumerator WaitForAttack(float waitTime, Action attack)
    {
        //Set attacking state
        fighter.isAttacking = true;
        //Debug.Log("Attack Started");
        //Wait for half of the attack animation to finish
        yield return new WaitForSeconds(waitTime/2);
        attack();
        //Wait for the remaining half of the animation to finish
        yield return new WaitForSeconds(waitTime/2);
        //Debug.Log("Attack Finished after " + waitTime + " seconds");

        //Reset relevant states and fighter.moveSpeed of player
        fighter.performingAction = false;
        fighter.isAttacking = false;
    }
    
    //Cast a box in front of the parent gameobject to see if the attack connects
    private GameObject CheckHit(Vector3 hitbox)
    {   
        //Set half extent for hitbox by dividing hitbox in half
        Vector3 halfExtent = hitbox/2f;
        
        hitboxCenter = transform.position + (transform.forward/attackCloseness);

        hitboxOrientation = transform.rotation;
        
        //debugHitbox(hitbox);
        
        //Get all collisions from overlapped box
        Collider[] collisions = Physics.OverlapBox(hitboxCenter, halfExtent, hitboxOrientation, entityMask, QueryTriggerInteraction.UseGlobal);

        //If there is a collision in the array, return the first one
        if(collisions.Length > 0)
        { 
            int c = 0;
            //Get collider of current entity
            Collider thisCollider = GetComponent<BoxCollider>();

            //Get first collision from array
            Collider collision = collisions[c];
            c++;

            //Iterate through collisions until collision is not equal to the entity's collider
            //This needs to exist if an attack hitbox overlaps with the entity's collider
            while(collision.Equals(thisCollider) && c < collisions.Length)
            {
                collision = collisions[c];
                c++;
            }

            //Return null if no collision is found (that is not current entity)
            if(collision.Equals(thisCollider) && c == collisions.Length) return null;

            Debug.Log(fighter.name + " Hit " + collision.gameObject.name);
            return collision.gameObject;
        }
        
        //otherwise return null
        return null;
    }

    #endregion

    #region Blocking functions
    //Perform a block and check if the target is parrying
    public void BeginBlock()
    {
        anim.ResetTrigger("End Block");
        anim.SetTrigger("Start Block");

        fighter.performingAction = true;
        fighter.isBlocking = true;

        //Start parry
        StartCoroutine(ParryWindow());
    }

    
    //End block action
    public void EndBlock()
    {
        //Cancel parry
        StopCoroutine(ParryWindow());

        anim.ResetTrigger("Start Block");
        anim.SetTrigger("End Block");

        fighter.performingAction = false;
        fighter.isBlocking = false;
    }

    //This function handles blocking for NPCs
    //It is done this way since coroutines cannot be used in the state classes
    //and coroutines are needed to time the start of the block and the end of the block
    public void NpcBlock(float blockTime)
    {
        StartCoroutine(NpcPerformBlock(blockTime));
    }

    private IEnumerator NpcPerformBlock(float blockTime)
    {
        BeginBlock();
        yield return new WaitForSeconds(blockTime);
        EndBlock();
    }

    //Coroutine that drains stamina while blocking for the parent object
    private IEnumerator BlockChecker()
    {
        //Always run
        while(true)
        {   
            //Only execute if parent is blocking
            yield return new WaitUntil(() => fighter.isBlocking);
            //After parry window has completed, take stamina damage per update
            if(!fighter.isParrying) fighter.takeStaminaDamage(blockStaminaDecayRate * Time.fixedDeltaTime);
        }
    }

    //perform parry
    private void Parry(Fighter target)
    {
        ActionManager actionManager = target.GetActionManager();

        //Play parry sound
        AudioManager.Instance.PlaySound("Parry");
        
        Debug.Log(target.name + " Successfully Parried");

        //Apply a longer than usual hitstun to the attacker who got parried
        fighter.StartCoroutine("HitStun", 0.75);
            
        //refund 1/6 of the fighter's stamina when successfully parrying
        target.setStamina(target.GetStamina() + target.GetMaxStamina() / 6);

        //end block
        actionManager.EndBlock();   
    }

    //Sets the parrying state flag to true for 1s, and then set it back to false
    private IEnumerator ParryWindow()
    {
        fighter.isParrying = true;
        Debug.Log("Parry start");
        yield return new WaitForSeconds(parryTime);
        Debug.Log("Parry end");
        fighter.isParrying = false;
    }

    //Get parry time window
    public float GetParryTime() => parryTime;
    #endregion

    #region Rolling functions
    //Perform a roll
    public void Roll()
    {
        StartCoroutine(WaitForRoll());

        fighter.performingAction = true;
        fighter.isRolling = true;

        anim.SetTrigger("Rolling");
    }

    //Coroutine to execute a roll waiting for the animation duration
    private IEnumerator WaitForRoll()
    {
        //wait until player is rolling
        yield return new WaitUntil(() => fighter.isRolling);
        //Debug.Log("Roll Started");

        //Reduce stamina when performing a roll
        float maxStamina = fighter.GetMaxStamina();
        fighter.takeStaminaDamage(maxStamina / 5);

        yield return new WaitForSeconds(animationTimings["Roll"]);

        //Debug.Log("Roll Finished after " + animationTimings["Roll"] + " seconds");

        fighter.performingAction = false;
        fighter.isRolling = false;
    }
    #endregion

    #region Debugging
    //Function used for visualizing hitboxes for different characters and attacks
    private void debugHitbox(Vector3 hitbox)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.transform.position = hitboxCenter;
        obj.transform.localScale = hitbox;
        obj.transform.rotation = hitboxOrientation;

        obj.GetComponent<Renderer>().material.color = new Color(0, 255, 0, 128);

        Destroy(obj, 0.5f);
    }
    #endregion
}

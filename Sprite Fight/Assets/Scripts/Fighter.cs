using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(ActionManager))]
public class Fighter : MonoBehaviour
{
    #region Enums
    public enum Character { Samurai, Huntress };
    #endregion

    #region Game Objects and Components
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Rigidbody body;
    private ActionManager actionManager;
    [Header("Important Settings")]
    [SerializeField] private Character character;
    [SerializeField] private GameObject sprite;
    [SerializeField] public Bar healthBar;
    [SerializeField] public Bar staminaBar;
    #endregion

    #region Health, Stamina
    [Header("Health Settings")]
    [SerializeField] private float maxHealth;
    [SerializeField, Range(0.0f, 100f)] private float currentHealth;

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina;
    [SerializeField, Range(0.0f, 100f)] private float currentStamina;

    [SerializeField, Range(0.0f, 10f)] private float staminaRegenerationRate = 5f;
    private bool refillingStamina = false;
    [SerializeField] private float staminaRegenerationTime = 0.75f;
    #endregion

    #region Movement Variables
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed;
    public float currentMoveSpeed;
    private float rollSpeed = 2F;
    #endregion

    #region Combat Variables
    [Header("Attack Damages")]
    [SerializeField] private float lightAttackDamage;
    [SerializeField] private float heavyAttackDamage;

    [Header("Knockback Strengths")]
    [SerializeField] private float lightAttackKnockback;
    [SerializeField] private float heavyAttackKnockback;

    [Header("Attack Hitboxes")]
    private Vector3 lightAttackHitbox;
    private Vector3 heavyAttackHitbox;
    #endregion

    #region Action States
    [Header("Action States")]
    public bool performingAction = false;
    public bool isStopped = false;
    public bool isMoving = false;
    public bool isRolling = false;
    public bool isAttacking = false;
    public bool isBlocking = false;
    public bool isParrying = false;
    public bool isTired = false;
    public bool isStunned = false;    
    public bool successfulParry = false;
    #endregion

    #region Animation Information
    IDictionary<string, float> animationTimings = new Dictionary<string, float>();
    #endregion

    private bool initalized = false;

    void Start() 
    {
        loadCharacter(character);

        setMaxHealth(maxHealth);
        setMaxStamina(maxStamina);

        anim = sprite.GetComponent<Animator>();
        spriteRenderer = sprite.GetComponent<SpriteRenderer>();
        actionManager = GetComponent<ActionManager>();
        body = GetComponent<Rigidbody>();

        LoadAnimationDurations();

        StartCoroutine(CheckStamina());

        initalized = true;
    }

    //Update fighter every frame
    private void Update() 
    {
        if(!initalized) return;
        if(refillingStamina) RefillStamina();

        //Update bars
        healthBar.SetSlider(currentHealth);
        staminaBar.SetSlider(currentStamina);

        //Adjust speed based on if the player is blocking, rolling, or moving
        if(isBlocking) currentMoveSpeed = moveSpeed/8;
        else if(isAttacking) currentMoveSpeed = moveSpeed/4;
        else if(isRolling) currentMoveSpeed = rollSpeed;
        else currentMoveSpeed = moveSpeed;
    }

    #region Health Related Functions
    public void setMaxHealth(float health)
    {
        maxHealth = health;
        currentHealth = maxHealth;
        healthBar.SetMaxSliderValue(maxHealth);
    }

    //Handles taking damage
    public void takeHealthDamage(float damage)
    {
        //Take damage
        currentHealth -= damage;

        //Enable hit stun when taking damage so that no actions can be performed
        if(!isParrying && !isBlocking &&!isTired) StartCoroutine(HitStun(animationTimings["Take Hit"]));

        //Check if the entity has no health
        if(healthBarDepleted())
        {
            Die();
        }
    }

    //Handles hit stun when taking damage
    private IEnumerator HitStun(float stunTime)
    {
        isStunned = true;
        anim.SetTrigger("Hit");
        yield return new WaitForSeconds(stunTime);
        isStunned = false;
    }

    //Handles what happens when an entity dies
    public void Die()
    {
        anim.enabled = false;
        anim.enabled = true;
        anim.SetTrigger("Dead");
        
        //If the entity dies due to ring out, set health to zero
        currentHealth = 0;
        //Disable rigidbody on death
        body.detectCollisions = false;
        body.isKinematic = true;
    }
    #endregion

    #region Stamina Related Functions 
    //Set current stamina to max stamina and set stamina bar accordingly
    public void setMaxStamina(float stamina)
    {
        maxStamina = stamina;
        currentStamina = maxStamina;
        staminaBar.SetMaxSliderValue(maxStamina);
    }

    //Set stamina of fighter, only used by parry for stamina refund
    public void setStamina(float stamina)
    {
        currentStamina = stamina;

        if(currentStamina > maxStamina) currentStamina = maxStamina;

        staminaBar.SetSlider(currentStamina);
    }

    //Reduce stamina of the current entity and do required checks
    public void takeStaminaDamage(float damage)
    {
        //Ignore stamina damage if stamina is already depleted
        if(staminaBarDepleted()) return;

        //Disable stamina refilling process
        refillingStamina = false;

        //Apply damage to stamina
        currentStamina -= damage;

        //Stop coroutine does not work but stop all coroutines works
        //This resets the CheckStamina coroutine
        StopAllCoroutines();

        if(isStunned) isStunned = false;

        //Checks if entity has stamina remaining
        int staminaCheck = staminaBarDepleted() ? 0:1;

        //Determine which coroutine to run after losing stamina
        switch (staminaCheck)
        {
            //Entity is tired
            case 0:
                currentStamina = 0;
                if(isRolling) StartCoroutine(WaitForRoll());
                else if(isAttacking) StartCoroutine(WaitForAttackFinish());
                else StartCoroutine(StartTired());
                break;
            //Entity has stamina left
            case 1:
                //Cancel stamina refresh when you lose stamina
                StartCoroutine(CheckStamina());
                break;
        }
    }

    //Begins refilling stamina when coroutine determines it is correct to do so
    private void RefillStamina()
    {
        //Once we are close enough to refilling our stamina, we need to restart the coroutine and stop the stamina bar animation
        if(currentStamina > maxStamina - 1) 
        {
            currentStamina = maxStamina;
            refillingStamina = false;
            StartCoroutine(CheckStamina());
            return;
        }

        //replenish stamina
        currentStamina += (staminaRegenerationRate * Time.fixedDeltaTime);
    }

    //Checks if the stamina bar needs to be refilled after waiting when stamina is less than max stamina
    private IEnumerator CheckStamina()
    {
        refillingStamina = false;

        //Wait until stamina is less than max
        yield return new WaitUntil(staminaBarDepleting);
        if(isTired) yield return new WaitForSeconds(animationTimings["Tired"]);
        if(isRolling) yield return new WaitForSeconds(animationTimings["Roll"] + staminaRegenerationTime);
        else yield return new WaitForSeconds(staminaRegenerationTime);
        refillingStamina = true;
    }

    //If the entity runs out of stamina, they become tired for 1.5 seconds
    //This results in the entity being unable to move
    private IEnumerator StartTired()
    {
        isTired = true;
        Debug.Log(this.name + " is Tired...");
        //Begin stamina refill once recovered from tired
        StartCoroutine(CheckStamina());
        anim.ResetTrigger("End Tired");
        anim.SetTrigger("Start Tired");
        yield return new WaitForSeconds(animationTimings["Tired"]);
        anim.SetTrigger("End Tired");
        anim.ResetTrigger("Start Tired");
        //Insert function to flash sprite to indicate being tired
        isTired = false;
    }
    
    //If the entity becomes tired from a roll, wait for the roll to complete before being tired
    private IEnumerator WaitForRoll()
    {
        yield return new WaitUntil(() => !isRolling);
        StartCoroutine(StartTired());
    }

    //If the entity becomes tired from an attack, wait for the attack to complete before being tired
    private IEnumerator WaitForAttackFinish()
    {
        yield return new WaitUntil(() => !isAttacking);
        StartCoroutine(StartTired());
    }
    #endregion

    #region Getters
    public float GetMaxHealth() => maxHealth;
    public float GetMaxStamina() => maxStamina;
    public float GetHealth() => currentHealth;
    public float GetStamina() => currentStamina;
    public float GetLightAttackDamage() => lightAttackDamage;
    public float GetHeavyAttackDamage() => heavyAttackDamage;
    public float GetLightAttackKnockback() => lightAttackKnockback;
    public float GetHeavyAttackKnockback() => heavyAttackKnockback;
    public Vector3 GetLightAttackHitbox() => lightAttackHitbox;
    public Vector3 GetHeavyAttackHitbox() => heavyAttackHitbox;

    //returns the longest attack distance
    public float GetMaxAttackRange()
    {
        if(lightAttackHitbox.z > heavyAttackHitbox.z) return lightAttackHitbox.z;
        else return heavyAttackHitbox.z;
    }

    public Character GetCharacter() => character;

    //Return Animator for attached sprite
    public Animator GetAnimator() => sprite.GetComponent<Animator>();

    //Return hashmap of animation durations
    public IDictionary<string, float> GetAnimationDurations() => animationTimings;
    
    //Return sprite gameobject that belongs to the parent gameobject
    public GameObject GetSprite() => sprite;
    //Return sprite renderer for attached sprite
    public SpriteRenderer GetSpriteRenderer() => sprite.GetComponent<SpriteRenderer>();

    //Return action manager of the fighter
    public ActionManager GetActionManager() => GetComponent<ActionManager>();

    //Return the rigid body of the fighter    
    public Rigidbody GetRigidbody() => GetComponent<Rigidbody>();

    #endregion

    #region Boolean Check Functions
    public bool IsHealthy() => currentHealth > maxHealth/6;
    public bool IsDead() => currentHealth <= 0;
    public bool IsControllable() => (!PauseMenu.GamePaused && !isStopped && !isTired && !IsDead());
    private bool healthBarDepleted() => currentHealth <= 0;
    private bool staminaBarDepleting() => currentStamina < maxStamina;
    public bool staminaBarDepleted() => currentStamina <= 0;
    #endregion

    #region Knockback Functions
    //Apply knockback to the enemy in the direction of parent transfrom.forward
    public void ApplyKnockback(Vector3 direction, float knockback)
    {
        //check if the target is parrying, if so don't apply knockback.
        //else if the target is blocking, apply half the knockback.
        if(isParrying) return;
        if(isBlocking) knockback *= 0.5f;

        //Apply force as impulse in the direction that the attacker is facing, on the target Rigidbody
        body.AddForce(direction * knockback, ForceMode.Impulse);
    }
    #endregion

    #region Helper Functions
    //This function is called once when the script is loaded
    //Sets character stats based on serialized enum selection in fighter object
    private void loadCharacter(Character character)
    {
        switch (character)
        {
            case Character.Samurai:
                maxHealth = Samurai.maxHealth;
                maxStamina = Samurai.maxStamina;
                moveSpeed = Samurai.moveSpeed;

                lightAttackDamage = Samurai.lightAttackDamage;
                heavyAttackDamage = Samurai.heavyAttackDamage;

                lightAttackKnockback = Samurai.lightAttackKnockback;
                heavyAttackKnockback = Samurai.heavyAttackKnockback;

                lightAttackHitbox = Samurai.lightAttackHitbox;
                heavyAttackHitbox = Samurai.heavyAttackHitbox;
                break;
            
            case Character.Huntress:
                maxHealth = Huntress.maxHealth;
                maxStamina = Huntress.maxStamina;
                moveSpeed = Huntress.moveSpeed;

                lightAttackDamage = Huntress.lightAttackDamage;
                heavyAttackDamage = Huntress.heavyAttackDamage;

                lightAttackKnockback = Huntress.lightAttackKnockback;
                heavyAttackKnockback = Huntress.heavyAttackKnockback;

                lightAttackHitbox = Huntress.lightAttackHitbox;
                heavyAttackHitbox = Huntress.heavyAttackHitbox;
                break;
        }
    }

    //This function is called once when the script is loaded since the sprite animations will not be changing
    //Gets the duration of needed animations when performing actions. This is useful for determining when the player is attacking or maybe if they need I-frames
    private void LoadAnimationDurations()
    {
        AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            animationTimings.Add(clip.name, clip.length);
        }
    }
    #endregion
}

#region Fighter Character Classes
class Samurai
{
    #region Health & Stamina
    [Header("Health Settings")]
    public static float maxHealth = 100;

    [Header("Stamina Settings")]
    public static float maxStamina = 100;
    #endregion

    #region Movement Variables
    [Header("Movement Settings")]
    public static float moveSpeed = 1.2f;
    #endregion

    #region Combat Variables
    [Header("Attack Damages")]
    public static float lightAttackDamage = 9;
    public static float heavyAttackDamage = 14;

    [Header("Knockback Strengths")]
    public static float lightAttackKnockback = 10;
    public static float heavyAttackKnockback = 12;
    public static Vector3 lightAttackHitbox = new Vector3(0.2f, 0.525f, 0.55f);
    public static Vector3 heavyAttackHitbox = new Vector3(0.2f, 0.525f, 0.5f);
    #endregion
}

class Huntress
{
    #region Health & Stamina
    [Header("Health Settings")]
    public static float maxHealth = 80;

    [Header("Stamina Settings")]
    public static float maxStamina = 80;
    #endregion

    #region Movement Variables
    [Header("Movement Settings")]
    public static float moveSpeed = 1.3f;
    #endregion

    #region Combat Variables
    [Header("Attack Damages")]
    public static float lightAttackDamage = 8;
    public static float heavyAttackDamage = 13;

    [Header("Knockback Strengths")]
    public static float lightAttackKnockback = 9;
    public static float heavyAttackKnockback = 11;
    public static Vector3 lightAttackHitbox = new Vector3(0.2f, 0.525f, 0.55f);
    public static Vector3 heavyAttackHitbox = new Vector3(0.2f, 0.525f, 0.5f);
    #endregion
}
#endregion
using System;
using UnityEngine;
using UnityEngine.Events;

// This class is adapted and based off of Brackeys' CharacterController: https://github.com/Brackeys/2D-Character-Controller/blob/master/CharacterController2D.cs
public class PlayerController : MonoBehaviour
{
    // Speed variables
    [SerializeField] private float m_JumpSpeed = 15f;
    [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = 0.5f;
    [Range(0, 0.3f)] [SerializeField] private float m_MovementSmoothing = 0.05f;
    // Air control and jumps
    [SerializeField] private bool m_AirControl = false;
    [SerializeField] int m_MaxJumps = 1;
    // Layers to collide with
    [SerializeField] private LayerMask m_WhatIsGround;
    [SerializeField] public LayerMask m_WhatIsPunched;
    // Component/Object references
    [SerializeField] private Transform m_GroundCheck;
    [SerializeField] private Transform m_CeilingCheck;
    [SerializeField] private Transform m_RightWallCheck;
    [SerializeField] private Transform m_LeftWallCheck;
    [SerializeField] private ParticleSystem m_WalkingDust;
    [SerializeField] private Animator m_Animator;
    // Hit Box object references for attacks
    [SerializeField] private GameObject m_Fists;


    const float k_GroundedRadius = 0.4f; // Grounded detection radius
    [HideInInspector] public bool m_Grounded; // Is grounded

    private int m_RemainingJumps = 1; // Jumps remaining
    private bool m_HoldingJumpInput = false; // If player is holding jump input
    private float m_JumpTime = 0f; // time the player jumped
    [SerializeField] private float k_JumpStopDelay = 0.1f; // time the player can stop inputting jump after jumping
    private float m_GroundedTime = 0f;
    [SerializeField] private float k_GroundedDelay = 0.05f; // 

    const float k_CeilingRadius = 0.3f; // Ceiling detection radius

    private Rigidbody2D m_RigidBody2D; // The RigibBody2D to use for velocity

    public enum Weapon
    {
        Fist,
        Scythe,
        Sword,
        Greatsword
    };
    private Weapon m_EquippedWeapon;

    private bool m_FacingRight = true; // Player is facing right
    private Vector3 m_Velocity = Vector3.zero; // The movement velocity

    // List of events
    [Header("Events")]
    [Space]
    public UnityEvent onLandEvent; // landing events
    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }
    public BoolEvent OnCrouchEvent; // Bool event for crouch
    private bool m_WasCrouching = false; // was crouching
    // Smaller events/delegates
    public Action m_OnAttackIdle; // end of attack event

    private void Start()
    {
        if (m_Animator == null)
        {
            m_Animator = GetComponent<Animator>();
        }

        m_EquippedWeapon = Weapon.Fist;
    }

    // When this object awakes
    private void Awake()
    {
        // find the RigidBody2D component
        m_RigidBody2D = GetComponent<Rigidbody2D>();

        // set null events to non-null
        if (onLandEvent == null)
        {
            onLandEvent = new UnityEvent();
        }
        if (OnCrouchEvent == null)
        {
            OnCrouchEvent = new BoolEvent();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        CheckGrounded();
        CheckAttackState();
    }

    private void CheckGrounded()
    {
        // Establish if player was grounded
        bool wasGrounded = m_Grounded;
        m_Grounded = false;

        // find all colliders overlapping with the groundcheck object
        Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
        // iterate over and check if one of them is not this gameObject
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                // set grounded and invoke the land event
                m_Grounded = true;
                if (!wasGrounded)
                {
                    onLandEvent.Invoke();
                }
            }
        }

        if (wasGrounded)
        {
            m_GroundedTime = Time.time + k_GroundedDelay;
        }

        if (m_WalkingDust != null)
        {
            if (m_Grounded && !m_WalkingDust.isPlaying)
            {
                m_WalkingDust.Play();
                Debug.Log("Playing");
            } else if (!m_Grounded && m_WalkingDust.isPlaying)
            {
                m_WalkingDust.Stop();
                Debug.Log("Stopping");
            }
        }
    }

    // This method acts as movement input for the player
    public void Move(float move, bool jump, bool crouch)
    {
        // if the player does not want to crouch
        if (!crouch)
        {
            // if an object is colliding with the ceiling check and player is not in air, start crouching
            if (m_Grounded && Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
            {
                crouch = true;
            }
        }

        // if player is grounded, reset their remaining jumps
        if (m_Grounded)
        {
            m_RemainingJumps = m_MaxJumps;
        }

        // allow horizontal movement if grounded or player has air control
        if (m_Grounded || m_AirControl)
        {

            // if player is crouching
            if (crouch)
            {
                // if the player was not just crouching
                if (!m_WasCrouching)
                {
                    // invoke the crouch event
                    m_WasCrouching = true;
                    OnCrouchEvent.Invoke(true);
                }

                // multiply speed by crouch speed modifier
                move *= m_CrouchSpeed;

            } 
            else
            {
                // if the player was crouching
                if (m_WasCrouching)
                {
                    // set and invoke the uncrouch event
                    m_WasCrouching = false;
                    OnCrouchEvent.Invoke(false);
                }
            }

            // set the target horizontal velocity
            Vector3 targetVelocity = new Vector2(move * 10f, m_RigidBody2D.velocity.y);
            // smooth towards it
            m_RigidBody2D.velocity = Vector3.SmoothDamp(m_RigidBody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

            // if the player is moving right and not facing right, flip
            if (move > 0 && !m_FacingRight)
            {
                Flip();
            } 
            // if the player is moving left and facing right, flip
            else if (move < 0 && m_FacingRight)
            {
                Flip();
            }

        }

        // grab the velocity vector
        Vector3 velocity = m_RigidBody2D.velocity;

        // if the player is grounded, wants to jump, has remaining jump, and is not holding jump input from previous jump
        if (Time.time < m_GroundedTime && jump && m_RemainingJumps > 0 && !m_HoldingJumpInput)
        {
            // set grounded to false, as player is jumping
            m_Grounded = false;

            // set upwards velocity
            velocity.y = m_JumpSpeed;
            m_RigidBody2D.velocity = velocity;

            // reduce remaining jumps and set jumping true, player starts holding a new jump input
            m_RemainingJumps--;
            m_HoldingJumpInput = true;
            m_JumpTime = Time.time + k_JumpStopDelay;
        }
        // if the player is going up, is not grounded, is not putting jump input, but was holding jump previously
        else if (velocity.y > 0 && Time.time > m_GroundedTime && !jump && m_HoldingJumpInput)
        {
            // set upwards velocity to 0
            velocity.y = 0f;
            m_RigidBody2D.velocity = velocity;
        }

        // if player no longer wants to jump
        if (!jump && Time.time > m_JumpTime)
        {
            // they stop holding the jump input
            m_HoldingJumpInput = false;
        }

    }

    // Flip the player
    private void Flip()
    {
        // reverse value
        m_FacingRight = !m_FacingRight;

        // reverse the scale
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;

        // reverse scale of other things
        Vector3 particleScale = m_WalkingDust.transform.localScale;
        particleScale.x *= -1;
        m_WalkingDust.transform.localScale = particleScale;
    }

    // Command the player to attack
    public void Attack(int dir)
    {
        if (m_Animator.GetCurrentAnimatorStateInfo(1).IsName("Idle"))
        {
            switch (m_EquippedWeapon)
            {
                default:
                case Weapon.Fist:
                    if (m_Fists != null)
                    {
                        Action disableFist = () => m_Fists.SetActive(false);
                        m_OnAttackIdle += disableFist;
                    }
                    m_Fists.SetActive(true);
                    m_Animator.SetInteger("AttackType", 0);
                    break;
            }
            m_Animator.SetTrigger("NormalAttack");
        }
    }

    public void CheckAttackState()
    {
        if (m_Animator.GetCurrentAnimatorStateInfo(1).IsName("Idle") && !m_Animator.GetBool("NormalAttack"))
        {
            if (m_OnAttackIdle != null)
            {
                m_OnAttackIdle();
                m_OnAttackIdle = null;
            }
        }
    }

    // Command the player to equip a weapon
    public void EquipWeapon(Weapon weap)
    {
        m_EquippedWeapon = weap;
    }
    

}

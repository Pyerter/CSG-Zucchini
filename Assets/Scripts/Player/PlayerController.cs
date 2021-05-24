using System;
using UnityEngine;
using UnityEngine.Events;

// This class is adapted and based off of Brackeys' CharacterController: https://github.com/Brackeys/2D-Character-Controller/blob/master/CharacterController2D.cs
public class PlayerController : MonoBehaviour
{
    // Speed variables
    [SerializeField] private float m_RunSpeed = 10f;
    [SerializeField] private float m_JumpSpeed = 15f;
    [SerializeField] private float m_DashSpeed = 40f;
    [SerializeField] private float m_GrappleMaxSpeed = 60f;
    [SerializeField] private float m_GrappleSpeedAccelerate = 0.2f;
    [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = 0.5f;
    [Range(0, 0.3f)] [SerializeField] private float m_MovementSmoothing = 0.05f;
    private float m_GravMod; // value stores the grav mod for the rigidbody
    private Vector3 m_Velocity = Vector3.zero; // The movement velocity

    // Air control and jumps
    [SerializeField] private bool m_AirControl = false;
    [SerializeField] int m_MaxJumps = 1;
    [SerializeField] int m_MaxDashes = 1;
    private int m_RemainingJumps = 1; // Jumps remaining
    private int m_RemainingDashes = 1; // Dashes remaining

    // Layers to collide with
    [SerializeField] private LayerMask m_WhatIsGround;
    [SerializeField] public LayerMask m_WhatIsPunched;
    [SerializeField] private LayerMask m_WhatIsGrappled;

    // Component/Object references
    [SerializeField] private Transform m_GroundCheck;
    [SerializeField] private Transform m_CeilingCheck;
    [SerializeField] private Transform m_RightWallCheck;
    [SerializeField] private Transform m_LeftWallCheck;
    [SerializeField] public Transform m_GrappleGun;

    [SerializeField] private ParticleSystem m_WalkingDust;
    [SerializeField] private Animator m_Animator;
    private Rigidbody2D m_RigidBody2D; // The RigibBody2D to use for velocity

    [SerializeField] private GameObject m_Fists;

    // Dynamic references
    private GrappleHinge m_GrappleHinge;

    // Size variables
    const float k_GroundedRadius = 0.4f; // Grounded detection radius
    const float k_CeilingRadius = 0.3f; // Ceiling detection radius
    const float k_GrappleRadius = 8f; // radius where the player can grapple within

    // State variables
    [HideInInspector] public bool m_Grounded; // Is grounded
    private bool m_HoldingJumpInput = false; // If player is holding jump input
    private bool m_FacingRight = true; // Player is facing right
    private bool m_WasCrouching = false; // was crouching
    private bool m_WasDashing = false; // if the player was dashing in the previous frame
    private bool m_WasGrappling = false; // if the player was grappling

    // Timer variables
    private float m_JumpTime = 0f; // time the player jumped
    [SerializeField] private float k_JumpStopDelay = 0.1f; // time the player can stop inputting jump after jumping
    private float m_GroundedTime = 0f; // time player is grounded until
    [SerializeField] private float k_GroundedDelay = 0.05f; // delay to remain grounded after collider doesn't touch ground
    private float m_DashRefreshedTime = 0f; // Time the player is able to dash after cooldown
    [SerializeField] private float m_DashCooldown = 0.25f; // Time player must wait to dash again

    // Variables conerning attack states
    public enum Weapon
    {
        Fist,
        Scythe,
        Sword,
        Greatsword
    };
    private Weapon m_EquippedWeapon;


    // List of events
    [Header("Events")]
    [Space]
    public UnityEvent onLandEvent; // landing events
    [System.Serializable] public class BoolEvent : UnityEvent<bool> { } // class to store bool event
    public BoolEvent OnCrouchEvent; // Bool event for crouch

    // Smaller events/delegates
    public Action m_OnAttackIdle; // end of attack event

    // Variables pertaining to the ability to do stuff
    private bool u_CanDash = true;
    private bool u_CanWallLatch = false;
    private bool u_CanGrapple = true;
    // - - -

    // When this object awakes
    private void Awake()
    {
        // find the RigidBody2D component
        m_RigidBody2D = GetComponent<Rigidbody2D>();
        m_GravMod = m_RigidBody2D.gravityScale;
        // make sure the animator is present
        if (m_Animator == null)
        {
            m_Animator = GetComponent<Animator>();
        }

        // set null events to non-null
        if (onLandEvent == null)
        {
            onLandEvent = new UnityEvent();
        }
        if (OnCrouchEvent == null)
        {
            OnCrouchEvent = new BoolEvent();
        }

        // set default values
        m_EquippedWeapon = Weapon.Fist;
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
            } else if (!m_Grounded && m_WalkingDust.isPlaying)
            {
                m_WalkingDust.Stop();
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
            RefreshMovement();
        }


        HorizontalMovement(move, crouch);

        VerticalMovement(jump);

        CheckGrappleEnd();

    }

    private void HorizontalMovement(float move, bool crouch)
    {
        bool dashing = CheckDashing();

        bool canMoveSides = !dashing && (m_Grounded || m_AirControl);

        if (canMoveSides)
        {
            CheckRun(move, crouch);
        }

        // Check flips
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

    // returns if the player is currently dashing
    private bool CheckDashing()
    {
        bool dashing = m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Dash");

        if (dashing && !m_WasDashing)
        {
            float dirMod = 1;
            if (!m_FacingRight)
            {
                dirMod = -1;
            }
            m_RigidBody2D.velocity = new Vector2(m_DashSpeed * dirMod, 0f);
            m_RigidBody2D.gravityScale = 0f;

        }
        else if (!dashing && m_WasDashing)
        {
            m_RigidBody2D.gravityScale = m_GravMod;
            m_DashRefreshedTime = Time.time + m_DashCooldown;
        }

        m_WasDashing = dashing;

        return dashing;
    }

    private void CheckRun(float move, bool crouch)
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

        // Modify the x velocity based on grapple or not
        float xVelocity = move;
        if (!m_WasGrappling)
        {
            xVelocity = xVelocity * m_RunSpeed;
        } else
        {
            xVelocity = xVelocity * m_GrappleSpeedAccelerate + m_RigidBody2D.velocity.x;
            if (Math.Abs(xVelocity) > m_GrappleMaxSpeed)
            {
                if (xVelocity < 0)
                    xVelocity = -m_GrappleMaxSpeed;
                else
                    xVelocity = m_GrappleMaxSpeed;
            }
        }
        // set the target horizontal velocity
        Vector3 targetVelocity = new Vector2(xVelocity, m_RigidBody2D.velocity.y);
        // smooth towards it
        m_RigidBody2D.velocity = Vector3.SmoothDamp(m_RigidBody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);
    }

    private void VerticalMovement(bool jump)
    {

        // grab the velocity vector
        Vector3 velocity = m_RigidBody2D.velocity;

        // Check if the player can input jumps or fast falls
        bool jumpLocked = m_WasDashing || m_WasGrappling;
        if (!jumpLocked)
        {
            // Check for valid jumps or fast falls
            bool canJump = Time.time < m_GroundedTime && jump && m_RemainingJumps > 0 && !m_HoldingJumpInput;
            bool canFastFall = velocity.y > 0 && Time.time > m_GroundedTime && !jump && m_HoldingJumpInput;

            // if the player is grounded, wants to jump, has remaining jump, and is not holding jump input from previous jump
            if (canJump)
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
            else if (canFastFall)
            {
                // set upwards velocity to 0
                velocity.y = 0f;
                m_RigidBody2D.velocity = velocity;
            }
        }

        // check inputs, if player no longer wants to jump
        if (!jump && Time.time > m_JumpTime)
        {
            // they stop holding the jump input
            m_HoldingJumpInput = false;
        }

    }

    private void RefreshMovement()
    {
        m_RemainingJumps = m_MaxJumps;
        m_RemainingDashes = m_MaxDashes;
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

    public void DoDash()
    {
        if (u_CanDash && m_RemainingDashes > 0 && Time.time > m_DashRefreshedTime && !m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Dash"))
        {
            m_Animator.SetTrigger("Dash");
            m_RemainingDashes--;
        }
    }

    public void DoGrapple(bool grappling)
    {
        bool grappleLocekd = m_WasDashing || !m_Animator.GetCurrentAnimatorStateInfo(1).IsName("Idle");
        if (u_CanGrapple && !grappleLocekd)
        {
            if (!m_WasGrappling && grappling)
            {
                Debug.Log("Trying grapple");
                Collider2D grappled = Physics2D.OverlapCircle(m_GrappleGun.transform.position, k_GrappleRadius, m_WhatIsGrappled.value);
                if (grappled != null && grappled.TryGetComponent<GrappleHinge>(out GrappleHinge hinge))
                {
                    hinge.StartGrapple(this);
                    m_WasGrappling = true;
                    m_GrappleHinge = hinge;
                } else if (grappled == null)
                {
                    Debug.Log("No Collider");
                }
            } else if (m_WasGrappling && !grappling && m_GrappleHinge)
            {
                EndGrapple();
            }
        }
    }

    public void CheckGrappleEnd()
    {
        if (m_GrappleHinge)
        {
            if (!m_WasGrappling || Vector2.Distance(m_GrappleHinge.transform.position, gameObject.transform.position) > k_GrappleRadius)
            {
                EndGrapple();
            } else
            {
                float grappleWeight = Math.Abs(2 * m_RigidBody2D.velocity.magnitude / m_GrappleMaxSpeed);
                if (grappleWeight > 1)
                {
                    grappleWeight = 1f;
                }
                float zValue = (Utility.AngleInDeg(m_GrappleHinge.gameObject.transform.position, m_GrappleGun.gameObject.transform.position) + 90) * grappleWeight;
                Quaternion target = Quaternion.Euler(0, 0, zValue);
                gameObject.transform.rotation = target;
            }
        }
    }

    private void EndGrapple()
    {
        m_GrappleHinge.EndGrapple();
        m_WasGrappling = false;
        m_GrappleHinge = null;
        gameObject.transform.rotation = Quaternion.Euler(0, 0, 0);
    }
    
}

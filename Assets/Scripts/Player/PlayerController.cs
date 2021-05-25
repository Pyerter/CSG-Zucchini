using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// This class is adapted and based off of Brackeys' CharacterController: https://github.com/Brackeys/2D-Character-Controller/blob/master/CharacterController2D.cs
public class PlayerController : MonoBehaviour
{
    // Speed variables
    [SerializeField] private float m_RunSpeed = 10f;
    [SerializeField] private float m_JumpSpeed = 18f;
    [SerializeField] private float m_BounceSpeed = 14f;
    [SerializeField] private float m_DashSpeed = 40f;
    [SerializeField] private float m_WallSlideSpeed = 5f;
    [SerializeField] private float m_GrappleMaxSpeed = 60f;
    [SerializeField] private float m_GrappleSpeedAccelerate = 0.2f;
    [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = 0.5f;
    [Range(0, 0.3f)] [SerializeField] private float m_MovementSmoothing = 0.05f;
    [Range(0.1f, 1f)] [SerializeField] private float m_LossMovementSmoothing = 0.7f;
    private float m_InitialMovementSmooth;
    [Range(0, 0.5f)] [SerializeField] private float m_RotationLerp = 0.1f;
    private float m_GravMod; // value stores the grav mod for the rigidbody
    private Vector3 m_Velocity = Vector3.zero; // The movement velocity
    private Quaternion m_TargetRotation;

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
    private Enemy m_SlashTarget;
    private Vector3 m_SlashDirection;

    // Size variables
    const float k_GroundedRadius = 0.4f; // Grounded detection radius
    const float k_WalledRadius = 0.1f; // Radius to detect wall latching
    const float k_CeilingRadius = 0.3f; // Ceiling detection radius
    const float k_GrappleRadius = 8f; // radius where the player can grapple within
    const float k_SlashRadius = 5f; // radius where the player can dashslash
    const float k_SlashSpacingDistance = 1f; // distance the player is opposite of slash origin, away from the target

    // State variables
    [HideInInspector] public bool m_Grounded; // Is grounded
    private bool m_HoldingJumpInput = false; // If player is holding jump input
    private bool m_FacingRight = true; // Player is facing right
    private bool m_WasCrouching = false; // was crouching
    private bool m_WasDashing = false; // if the player was dashing in the previous frame
    private bool m_WasGrappling = false; // if the player was grappling
    private bool m_WasWalled = false; // if the player was wall latching
    private bool m_ShouldBounce = false; // if this is true, the player's y velocity will be bounced up
    public bool m_HasBounced = false; // if the controller already bounced and should not
    private bool m_WasSlashing = false; // if the player is slashing through an enemy
    private bool m_EndingSlash = false; // if the player is ending the slash

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
    private bool u_CanWallLatch = true;
    private bool u_CanGrapple = true;
    private bool u_CanSlash = true;
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
        m_TargetRotation = transform.rotation;
        m_InitialMovementSmooth = m_MovementSmoothing;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        CheckGrounded();
        CheckAttackState();
        CheckRotation();
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

        m_Animator.SetBool("Grounded", m_Grounded);
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
    public void Move(float move, float verticalMove, bool jump, bool crouch)
    {
        if (verticalMove > 0)
            verticalMove = 1;
        else if (verticalMove < 0)
            verticalMove = -1;
        int vertMove = (int)verticalMove;

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


        HorizontalMovement(move, vertMove, crouch);

        VerticalMovement(jump);

        CheckGrappleEnd();

        CheckSlash();

    }

    private void HorizontalMovement(float move, int verticalMovement, bool crouch)
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

        CheckWalled(verticalMovement);

        m_Animator.SetFloat("Speed", Math.Abs(m_RigidBody2D.velocity.x));
    }

    // returns if the player is currently dashing
    private bool CheckDashing()
    {
        bool dashing = m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Dash");

        if (dashing && !m_WasDashing)
        {
            if (m_WasWalled)
            {
                Flip();
            }
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
            EndDashing();
        }

        m_WasDashing = dashing;

        return dashing;
    }

    private void EndDashing()
    {
        m_RigidBody2D.gravityScale = m_GravMod;
        m_DashRefreshedTime = Time.time + m_DashCooldown;
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
        if (m_WasSlashing)
        {
            xVelocity = 0;
        } else if (!m_WasGrappling)
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
        bool jumpLocked = m_WasDashing || m_WasGrappling || m_ShouldBounce;
        if (!jumpLocked)
        {
            // Check for valid jumps or fast falls
            bool canJump = (m_WasWalled || Time.time < m_GroundedTime || m_RemainingJumps > 0) && jump && !m_HoldingJumpInput;
            bool willExtraJump = m_RemainingJumps > 0 && Time.time > m_GroundedTime && !m_WasWalled;
            bool canFastFall = velocity.y > 0 && Time.time > m_GroundedTime && !jump && m_HoldingJumpInput;

            // if the player is grounded, wants to jump, has remaining jump, and is not holding jump input from previous jump
            if (canJump)
            {
                // set grounded to false, as player is jumping
                m_Grounded = false;

                // set upwards velocity
                velocity.y = m_JumpSpeed;
                if (m_WasWalled)
                {
                    if (m_FacingRight)
                        velocity.x = -m_DashSpeed;
                    else
                        velocity.x = m_DashSpeed;
                }
                m_RigidBody2D.velocity = velocity;

                // reduce remaining jumps and set jumping true, player starts holding a new jump input
                if (willExtraJump)
                    m_RemainingJumps--;
                m_HoldingJumpInput = true;
                m_JumpTime = Time.time + k_JumpStopDelay;
                m_Animator.SetBool("Jumping", true);
            }
            // if the player is going up, is not grounded, is not putting jump input, but was holding jump previously
            else if (canFastFall)
            {
                // set upwards velocity to 0
                velocity.y = 0f;
                m_RigidBody2D.velocity = velocity;
            }
        }

        if (m_ShouldBounce && !m_HasBounced)
        {
            m_RigidBody2D.velocity = new Vector2(m_RigidBody2D.velocity.x, m_BounceSpeed);
            m_ShouldBounce = false;
            RefreshMovement();
            m_HasBounced = true;
        }

        // check inputs, if player no longer wants to jump
        if (!jump && Time.time > m_JumpTime)
        {
            // they stop holding the jump input
            m_HoldingJumpInput = false;
        }

        if (m_RigidBody2D.velocity.y < 0)
        {
            m_Animator.SetBool("Jumping", false);
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
        bool attackLocked = m_WasWalled || m_WasSlashing || m_WasGrappling || m_WasDashing || !m_Animator.GetCurrentAnimatorStateInfo(1).IsName("Idle");
        if (!attackLocked)
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
                    if (m_Grounded && dir == -1)
                        dir = 0;
                    m_Animator.SetInteger("AttackDirection", dir);
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
        bool dashLocked = !u_CanDash || m_WasSlashing || m_WasGrappling || !m_Animator.GetCurrentAnimatorStateInfo(1).IsName("Idle");
        bool dashAvailable = m_RemainingDashes > 0 && Time.time > m_DashRefreshedTime && !m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Dash");
        if (!dashLocked && dashAvailable)
        {
            m_Animator.SetTrigger("Dash");
            m_RemainingDashes--;
        }
    }

    public void DoGrapple(bool grappling)
    {
        bool grappleLocekd = !u_CanGrapple || m_WasSlashing || m_WasDashing || !m_Animator.GetCurrentAnimatorStateInfo(1).IsName("Idle");
        if (!grappleLocekd)
        {
            if (!m_WasGrappling && grappling)
            {
                Debug.Log("Trying grapple");
                Collider2D[] grappleds = Physics2D.OverlapCircleAll(m_GrappleGun.transform.position, k_GrappleRadius, m_WhatIsGrappled.value);
                if (grappleds.Length > 0)
                {
                    List<Collider2D> allGrapples = new List<Collider2D>(grappleds);
                    allGrapples.Sort((a, b) => { return (int)(Vector2.Distance(a.transform.position, gameObject.transform.position) - Vector2.Distance(b.transform.position, gameObject.transform.position)); });
                    foreach (Collider2D grappled in allGrapples) {
                        if (grappled != null && grappled.TryGetComponent<GrappleHinge>(out GrappleHinge hinge))
                        {
                            Unwall();
                            hinge.StartGrapple(this);
                            m_WasGrappling = true;
                            m_GrappleHinge = hinge;
                            break;
                        }
                    }
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
                m_TargetRotation = target;
                gameObject.transform.rotation = target;
            }
        }
    }

    private void EndGrapple()
    {
        m_GrappleHinge.EndGrapple();
        m_WasGrappling = false;
        m_GrappleHinge = null;
        m_TargetRotation = Quaternion.Euler(0, 0, 0);
        RefreshMovement();
    }

    void CheckRotation()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, m_TargetRotation, m_RotationLerp);
    }

    public void Bounce()
    {
        m_ShouldBounce = true;
    }

    private void CheckWalled(int vertical)
    {
        bool wallLocked = !u_CanWallLatch || m_Grounded || !m_Animator.GetCurrentAnimatorStateInfo(1).IsName("Idle");
        if (!wallLocked)
        {
            bool nowWalled = false;
            if (Physics2D.OverlapCircle(m_RightWallCheck.position, k_WalledRadius, m_WhatIsGround))
                nowWalled = true;

            if (!nowWalled)
            {
                Unwall();
                return;
            }

            if (!m_WasWalled)
            {
                if (m_WasDashing)
                {
                    EndDashing();
                    m_WasDashing = false;
                    m_Animator.SetTrigger("WalledDash");
                }
                if (m_WasGrappling)
                {
                    EndGrapple();
                }

                m_RigidBody2D.gravityScale = 0;
                m_RigidBody2D.velocity = new Vector2(0, 0);
                m_WasWalled = true;
                m_Animator.SetBool("Walled", true);
                RefreshMovement();
            }
            if (vertical < 0)
            {
                m_RigidBody2D.velocity = new Vector2(m_RigidBody2D.velocity.x, -m_WallSlideSpeed);
            } else if (Time.time > m_JumpTime)
            {
                m_RigidBody2D.velocity = new Vector2(m_RigidBody2D.velocity.x, 0);
            }
        }
    }

    private void Unwall()
    {
        if (m_WasWalled)
        {
            m_WasWalled = false;
            m_RigidBody2D.gravityScale = m_GravMod;
            m_Animator.SetBool("Walled", false);
        }
    }

    public void DoSlash()
    {
        Debug.Log("Trying slash...");
        bool slashLocked = !u_CanSlash || m_WasDashing || !m_Animator.GetCurrentAnimatorStateInfo(1).IsName("Idle");

        if (m_WasSlashing || m_EndingSlash || slashLocked)
            return;

        Collider2D[] collisions = Physics2D.OverlapCircleAll(gameObject.transform.position, k_SlashRadius, m_WhatIsPunched);
        if (collisions.Length <= 0)
            return;

        List<Collider2D> collisionsList = new List<Collider2D>(collisions);
        collisionsList.Sort((a, b) => { return (int)(Vector2.Distance(a.transform.position, gameObject.transform.position) - Vector2.Distance(b.transform.position, gameObject.transform.position)); });
        foreach (Collider2D slashes in collisionsList)
        {
            if (slashes != null && slashes.TryGetComponent<Enemy>(out Enemy enemy))
            {
                Vector2 distanceTo = slashes.gameObject.transform.position - gameObject.transform.position;
                distanceTo.Normalize();
                if (distanceTo.y < 0 && m_Grounded)
                    distanceTo.y = 0;
                Vector2 bounds = enemy.GetComponent<Collider2D>().bounds.extents / 2;
                float hitBoxSize = bounds.magnitude;
                bounds = GetComponent<Collider2D>().bounds.extents / 2;
                float hurtBoxSize = bounds.magnitude;
                Vector2 raycastSlashDirection = distanceTo * (hitBoxSize * 2 + hurtBoxSize + k_SlashSpacingDistance);
                Vector2 slashDirection = distanceTo * (hitBoxSize + hurtBoxSize + k_SlashSpacingDistance);
                m_SlashDirection = new Vector3(slashDirection.x, slashDirection.y, 0);
                m_SlashTarget = enemy;

                bool pathObstructed = false;
                RaycastHit2D[] rayHits = Physics2D.RaycastAll(gameObject.transform.position, m_SlashDirection, Vector2.Distance(gameObject.transform.position, enemy.transform.position));
                foreach (RaycastHit2D ray in rayHits)
                {
                    bool obstructed = ray.collider != null && m_WhatIsGround.value == (m_WhatIsGround.value | (1 << ray.collider.gameObject.layer));
                    if (obstructed)
                    {
                        pathObstructed = true;
                        break;
                    }
                }
                if (pathObstructed)
                {
                    m_SlashDirection = Vector3.zero;
                    m_SlashTarget = null;
                    continue;
                }

                bool spotObstructed = Physics2D.OverlapCircle(enemy.transform.position + m_SlashDirection, Math.Max(bounds.x, bounds.y), m_WhatIsGround);
                if (spotObstructed)
                {
                    m_SlashDirection = Vector3.zero;
                    m_SlashTarget = null;
                    continue;
                }

                RaycastHit2D[] obstructions = new RaycastHit2D[5];
                slashes.Raycast(raycastSlashDirection, obstructions, raycastSlashDirection.magnitude);

                bool hits = false;
                foreach (RaycastHit2D ray in obstructions)
                {
                    bool isObstruction = ray.collider != null && m_WhatIsGround.value == (m_WhatIsGround.value | (1 << ray.collider.gameObject.layer));
                    if (isObstruction)
                    {
                        hits = true;
                        //break;
                    }
                }

                if (!hits)
                {
                    TryStartSlash();
                    break;
                } else
                {
                    m_SlashDirection = Vector3.zero;
                    m_SlashTarget = null;
                }

            }
        }
    }

    private void CheckSlash()
    {
        bool starting = m_Animator.GetCurrentAnimatorStateInfo(0).IsName("SlashStartUp");
        bool ending = m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Slashing");
        bool slashing = starting || ending;
        if (ending && !m_EndingSlash)
        {
            EndSlash();
        } else if (!slashing && m_EndingSlash)
        {
            FinishSlash();
        }
    }

    private void TryStartSlash()
    {
        if (m_WasWalled)
            Unwall();

        m_Animator.SetTrigger("Slash");
        m_WasSlashing = true;
        m_RigidBody2D.gravityScale = 0;
        m_RigidBody2D.velocity = new Vector2(0, 0);
    }

    private void EndSlash()
    {
        m_WasSlashing = false;
        m_RigidBody2D.gravityScale = m_GravMod;
        m_RigidBody2D.position = m_SlashDirection + m_SlashTarget.transform.position;
        m_RigidBody2D.velocity = m_SlashDirection.normalized * m_DashSpeed;
        if (m_RigidBody2D.velocity.x > 0 && !m_FacingRight)
        {
            Flip();
        } else if (m_RigidBody2D.velocity.x < 0 && m_FacingRight)
        {
            Flip();
        }
        m_MovementSmoothing = m_LossMovementSmoothing;
        m_EndingSlash = true;
    }

    private void FinishSlash()
    {
        m_MovementSmoothing = m_InitialMovementSmooth;
        m_EndingSlash = false;
    }
    
}

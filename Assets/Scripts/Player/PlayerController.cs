using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// This class is adapted and based off of Brackeys' CharacterController: https://github.com/Brackeys/2D-Character-Controller/blob/master/CharacterController2D.cs
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float m_JumpSpeed = 15f;
    [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = 0.36f;
    [Range(0, 0.3f)] [SerializeField] private float m_MovementSmoothing = 0.05f;
    [SerializeField] private bool m_AirControl = false;
    [SerializeField] int m_MaxJumps = 1;
    [SerializeField] private LayerMask m_WhatIsGround;
    [SerializeField] private Transform m_GroundCheck;
    [SerializeField] private Transform m_CeilingCheck;
    [SerializeField] private Transform m_RightWallCheck;
    [SerializeField] private Transform m_LeftWallCheck;

    const float k_GroundedRadius = 0.2f;
    [HideInInspector] public bool m_Grounded;
    private int m_remainingJumps = 1;
    private bool jumping = false;
    private bool holdingJumpInput = false;
    const float k_CeilingRadius = 0.2f;
    private Rigidbody2D m_RigidBody2D;
    private bool m_FacingRight = true;
    private Vector3 m_Velocity = Vector3.zero;

    [Header("Events")]
    [Space]

    public UnityEvent onLandEvent;

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    public BoolEvent OnCrouchEvent;
    private bool m_WasCrouching = false;

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

    }

    // This method acts as movement input for the player
    public void Move(float move, bool jump, bool crouch)
    {
        // if the player does not want to crouch
        if (!crouch)
        {
            // if an object is colliding with the ceiling check and player is not in air, start crouching
            if (!m_Grounded && Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
            {
                crouch = true;
            }
        }

        // if player is grounded, reset their remaining jumps
        if (m_Grounded)
        {
            m_remainingJumps = m_MaxJumps;
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
        if (m_Grounded && jump && m_remainingJumps > 0 && !holdingJumpInput)
        {
            // set grounded to false, as player is jumping
            m_Grounded = false;

            // set upwards velocity
            velocity.y = m_JumpSpeed;
            m_RigidBody2D.velocity = velocity;

            // reduce remaining jumps and set jumping true, player starts holding a new jump input
            m_remainingJumps--;
            holdingJumpInput = true;
        }
        // if the player is going up, is not grounded, is not putting jump input, but was holding jump previously
        else if (velocity.y > 0 && !m_Grounded && !jump && holdingJumpInput)
        {
            // set upwards velocity to 0
            velocity.y = 0f;
            m_RigidBody2D.velocity = velocity;
        }

        // if player no longer wants to jump
        if (!jump)
        {
            // they stop holding the jump input
            holdingJumpInput = false;
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
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] public InputMaster controls;

    [SerializeField] private PlayerController controller;

    [SerializeField] private float runSpeed = 2f;

    private float horizontalMove = 0f;
    private float verticalMove = 0f;

    private bool requestJump = false;

    private bool requestingNormalAttack = false;

    private bool requestDash = false;

    private bool requestingGrapple = false;

    private void Awake()
    {
        controls = new InputMaster();

        controls.Player.Jump.started += _ => { Debug.Log("JUMP"); requestJump = true; };
        controls.Player.Jump.canceled += _ => { Debug.Log("NO JUMP"); requestJump = false; };

        controls.Player.Motion.performed += ctxt => { Vector2 vec = ctxt.ReadValue<Vector2>(); horizontalMove = vec.x * runSpeed; verticalMove = vec.y; };
        controls.Player.Motion.canceled += ctxt => { horizontalMove = 0; verticalMove = 0; };

        controls.Player.Light.started += _ => { requestingNormalAttack = true; };

        controls.Player.Dash.started += _ => { requestDash = true; };

        controls.Player.Grapple.started += _ => { requestingGrapple = true; };
        controls.Player.Grapple.canceled += _ => { requestingGrapple = false; };
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Do some start stuff

    }

    // Update is called once per frame
    void Update()
    {
        // grab the horizontal move value
        //horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;

        // grab the value if the player is requesting jump
        /*if (Input.GetButtonDown("Jump"))
        {
            requestJump = true;
        }
        else if (Input.GetButtonUp("Jump"))
        {
            requestJump = false;
        }*/

        // grab the value if player is doing normal attack
        /*if (Input.GetButtonDown("Fire1"))
        {
            requestingNormalAttack = true;
        }*/
    }

    // FixedUpdate is called a fixed amount of times per second: ?
    void FixedUpdate()
    {
        // pass in movement values (horizontalMove modified by fixedDeltaTime to properly calculate moveSpeed as moveSpeed per second)
        controller.Move(horizontalMove * Time.fixedDeltaTime, verticalMove, requestJump, false);

        if (requestingNormalAttack)
        {
            int dir = 0;
            if (verticalMove > 0)
            {
                dir = 1;
            } else if (verticalMove < 0)
            {
                dir = -1;
            }
            controller.Attack(dir);
            requestingNormalAttack = false;
        }

        if (requestDash)
        {
            controller.DoDash();
            requestDash = false;
        }

        controller.DoGrapple(requestingGrapple);
    }
}

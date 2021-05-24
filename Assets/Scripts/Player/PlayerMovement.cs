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

    private bool requestJump = false;

    private bool requestingNormalAttack = false;

    private bool requestDash = false;

    private void Awake()
    {
        controls = new InputMaster();

        controls.Player.Jump.started += _ => { Debug.Log("JUMP"); requestJump = true; };
        controls.Player.Jump.canceled += _ => { Debug.Log("NO JUMP"); requestJump = false; };

        controls.Player.Motion.performed += ctxt => { horizontalMove = ctxt.ReadValue<Vector2>().x * runSpeed; };
        controls.Player.Motion.canceled += ctxt => { horizontalMove = 0; };

        controls.Player.Light.started += _ => { requestingNormalAttack = true; };

        controls.Player.Dash.started += _ => { requestDash = true; };
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
        controller.Move(horizontalMove * Time.fixedDeltaTime, requestJump, false);

        if (requestingNormalAttack)
        {
            controller.Attack(0);
            requestingNormalAttack = false;
        }

        if (requestDash)
        {
            controller.DoDash();
            requestDash = false;
        }
    }
}

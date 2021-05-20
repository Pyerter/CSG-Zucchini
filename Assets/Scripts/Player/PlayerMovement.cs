using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerController controller;

    [SerializeField] private float runSpeed = 2f;

    private float horizontalMove = 0f;

    private bool requestJump = false;

    // Start is called before the first frame update
    void Start()
    {
        // Do some start stuff
    }

    // Update is called once per frame
    void Update()
    {
        // grab the horizontal move value
        horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;

        // grab the value if the player is requesting jump
        if (Input.GetButtonDown("Jump"))
        {
            requestJump = true;
        }
        else if (Input.GetButtonUp("Jump"))
        {
            requestJump = false;
        }
    }

    // FixedUpdate is called a fixed amount of times per second: ?
    void FixedUpdate()
    {
        // pass in movement values (horizontalMove modified by fixedDeltaTime to properly calculate moveSpeed as moveSpeed per second)
        controller.Move(horizontalMove * Time.fixedDeltaTime, requestJump, false);
    }
}
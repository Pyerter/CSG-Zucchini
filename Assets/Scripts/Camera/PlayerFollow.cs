using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerFollow : MonoBehaviour
{
    [Header("Misc. Data")]
    [SerializeField]
    private float ppu = 32;
    public float PPU { get { return ppu; } }

    [Header("Object Follow")]
    [SerializeField]
    private bool followObject;
    [SerializeField]
    private float offsetX = 0.0f;
    [SerializeField]
    private float offsetY = 1f;
    [SerializeField]
    private float lerpXValue = 0.015f;
    [SerializeField]
    private float lerpYValue = 0.1f;
    [SerializeField]
    private float minXDistance = 2f;
    [SerializeField]
    private float minYDistance = 1f;
    [SerializeField]
    private Transform follow;
    [SerializeField]
    private bool tryPredictFollow;
    [SerializeField]
    private float updatesAheadX = 2f;
    [SerializeField]
    private float updatesAheadY = 0.3f;
    private Rigidbody2D rb2dFollow;

    [Header("Mouse Follow")]
    [SerializeField]
    private bool mouseFollow;
    [SerializeField]
    private float pullStrength = 0.1f;
    [SerializeField]
    private float horizontalMultiplier = 0.02f;
    [SerializeField]
    private float verticalMultiplier = 0.1f;

    private float camX;
    private float camY;
    private float toX;
    private float toY;
    private float mouseX;
    private float mouseY;

    private bool restricting = false;

    void Start()
    {
        if (rb2dFollow == null)
        {
            rb2dFollow = follow.GetComponentInChildren<Rigidbody2D>();
        }
        if (tryPredictFollow && rb2dFollow == null)
        {
            tryPredictFollow = false;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        SetValues();

        RestrictDist();

        if (mouseFollow)
        {
            FollowMouse();
        }

        Follow();
    }

    private void SetValues()
    {
        camX = transform.position.x;
        camY = transform.position.y;

        if (followObject)
        {
            toX = follow.position.x + offsetX;
            toY = follow.position.y + offsetY;
        }
        else
        {
            toX = camX;
            toY = camY;
        }

        if (mouseFollow)
        {
            Vector3 pull = new Vector3(Input.mousePosition.x, Input.mousePosition.y);
            pull = Camera.main.ScreenToWorldPoint(pull);
            mouseX = pull.x;
            mouseY = pull.y;
        }
    }

    private void RestrictDist()
    {
        restricting = false;
        if (Math.Abs(toX - camX) >= minXDistance)
        {
            float dist = toX - camX;
            float sign = 1;
            if (dist != 0)
            {
                sign = dist / Math.Abs(dist);
            }
            camX = toX - (sign * minXDistance);
            restricting = true;
        }

        if (Math.Abs(toY - camY) >= minYDistance)
        {
            float dist = toY - camY;
            float sign = 1;
            if (dist != 0)
            {
                sign = dist / Math.Abs(dist);
            }
            camY = toY - (sign * minYDistance);
            restricting = true;
        }
    }

    private void FollowMouse()
    {
        float xDist = mouseX - camX;
        mouseX = camX + (xDist * horizontalMultiplier);

        float yDist = mouseY - camY;
        mouseY = camY + (yDist * verticalMultiplier);

        camX = Utility.Lerp(camX, mouseX, pullStrength);
        camY = Utility.Lerp(camY, mouseY, pullStrength);
    }

    private void Follow()
    {
        if (tryPredictFollow && rb2dFollow != null)
        {
            toX += (rb2dFollow.velocity.x * updatesAheadX);
            toY += (rb2dFollow.velocity.y * updatesAheadY);
        }
        float newX = Utility.Lerp(camX, toX, lerpXValue);
        float newY = Utility.Lerp(camY, toY, lerpYValue);
        if (!restricting)
        {
            newX = Mathf.RoundToInt(newX * ppu) / ppu;
            newY = Mathf.RoundToInt(newY * ppu) / ppu;
        }
        transform.position = new Vector3(newX, newY, transform.position.z);
    }
}


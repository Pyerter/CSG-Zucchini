using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FistAttackTrigger : MonoBehaviour
{
    [SerializeField] public PlayerController playerController;
    [SerializeField] public float m_Damage = 5f;
    [SerializeField] public bool m_IsHeavyAttack = false;
    [SerializeField] public float m_HeavyMultiplier = 2f;

    private void Start()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Object entered trigger");
        if (playerController.m_WhatIsPunched.value == (playerController.m_WhatIsPunched.value | (1 << collision.gameObject.layer)))
        {
            Debug.Log("Object is punchable");
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(m_Damage);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

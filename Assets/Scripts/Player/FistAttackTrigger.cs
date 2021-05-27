using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FistAttackTrigger : MonoBehaviour
{
    [SerializeField] public PlayerController playerController;
    [SerializeField] public float m_Damage = 5f;
    [SerializeField] public bool m_IsHeavyAttack = false;
    [SerializeField] public float m_HeavyMultiplier = 2f;
    [SerializeField] public float m_MaxFreezeTime = 0.5f;
    private float m_ReleaseFreeze = 0f;
    private float m_UpdateTime = 0f;
    [SerializeField] public float m_FreezeTimeScale = 0.0f;

    private Dictionary<GameObject, float> immuneTime;
    [SerializeField] public float m_ImmuneTimeOnHit = 0.5f;

    private void Awake()
    {
        immuneTime = new Dictionary<GameObject, float>();
    }

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
            if (immuneTime.ContainsKey(collision.gameObject) && Time.time > immuneTime[collision.gameObject])
            {
                immuneTime.Remove(gameObject);
            }
            if (!immuneTime.ContainsKey(collision.gameObject))
            {
                OnHit(collision);
                if (playerController.GetComponent<Animator>().GetInteger("AttackDirection") == -1)
                    playerController.Bounce();

            }
        }
    }
    private void OnHit(Collider2D collision)
    {
        Time.timeScale = m_FreezeTimeScale;
        m_ReleaseFreeze = Time.time + m_MaxFreezeTime;
        m_UpdateTime = Time.time;

        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(m_Damage);
            Debug.Log("Freeze Time");
        }
        immuneTime.Add(collision.gameObject, m_ImmuneTimeOnHit);
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale == m_FreezeTimeScale)
        {
            m_UpdateTime += Time.unscaledDeltaTime;
            if (m_UpdateTime > m_ReleaseFreeze)
            {
                Time.timeScale = 1f;
                m_ReleaseFreeze = 0f;
            }
        }
    }

    private void OnEnable()
    {
        immuneTime.Clear();
    }

    private void OnDisable()
    {
        if (Time.timeScale == m_FreezeTimeScale)
        {
            Time.timeScale = 1f;
        }

        immuneTime.Clear();
        playerController.m_HasBounced = false;
    }
}

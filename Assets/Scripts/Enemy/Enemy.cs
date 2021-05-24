using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float m_MaxHealth = 10;
    float m_CurrentHealth;

    // Start is called before the first frame update
    void Start()
    {
        m_CurrentHealth = m_MaxHealth;
    }

    public void TakeDamage(float damage)
    {
        m_CurrentHealth -= damage;

        if (m_CurrentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Enemy Died");

        this.gameObject.SetActive(false);
    }
}

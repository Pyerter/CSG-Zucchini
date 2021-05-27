using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHurtBox : MonoBehaviour
{
    public PlayerController m_Controller;

    public void TriggerDamage(float damage, Transform attacker)
    {
        if (m_Controller)
            m_Controller.TakeDamage(damage, attacker);
    }
}

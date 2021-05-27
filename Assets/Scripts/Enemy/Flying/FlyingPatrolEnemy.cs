using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingPatrolEnemy : Enemy
{

    [SerializeField] public List<Transform> m_FlyTargets;
    private int m_CurrentTarget = 0;
    private bool m_Reversing = false;
    private float m_MoveSmoothing = 0.05f;
    [SerializeField] private float m_FlySpeed = 50f;
    public Rigidbody2D m_Rigidbody2D;
    public float m_TargetRadius = 0.5f;
    public LayerMask m_WhatIsTarget;

    // Start is called before the first frame update
    void Start()
    {
        if (m_FlyTargets == null)
            m_FlyTargets = new List<Transform>();

        if (m_Rigidbody2D == null)
            m_Rigidbody2D = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        if (m_FlyTargets.Count == 0)
            return;



        if (m_CurrentTarget >= m_FlyTargets.Count)
        {
            m_CurrentTarget = m_FlyTargets.Count - 1;
            m_Reversing = true;
        } else if (m_CurrentTarget < 0)
        {
            m_CurrentTarget = 0;
            m_Reversing = false;
        }

        Vector3 currentVelocity;
        currentVelocity = m_FlyTargets[m_CurrentTarget].transform.position - transform.position;
        currentVelocity.Normalize();
        currentVelocity *= (m_FlySpeed);
        Vector3 velocity = m_Rigidbody2D.velocity;
        m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, currentVelocity, ref velocity, m_MoveSmoothing);

        Collider2D overlapTarget = Physics2D.OverlapCircle(transform.position, m_TargetRadius, m_WhatIsTarget);
        if (overlapTarget != null && overlapTarget.gameObject.transform == m_FlyTargets[m_CurrentTarget].transform)
        {
            if (m_Reversing)
                m_CurrentTarget--;
            else
                m_CurrentTarget++;
        }
    }
}

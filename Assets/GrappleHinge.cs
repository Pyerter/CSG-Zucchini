using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleHinge : MonoBehaviour
{
    private LineRenderer lr;
    public bool m_Movable = false;
    public PlayerController m_CurrentController;
    private SpringJoint2D m_CurrentJoint;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        m_CurrentController = null;
    }

    public void StartGrapple(PlayerController controller)
    {
        if (!m_CurrentJoint)
        {
            // Create a new spring joint component attached to this gameObject
            m_CurrentJoint = controller.gameObject.AddComponent<SpringJoint2D>();
            m_CurrentJoint.autoConfigureConnectedAnchor = false;
            m_CurrentJoint.connectedAnchor = gameObject.transform.position;

            // set the distance for the grapple
            m_CurrentJoint.autoConfigureDistance = true;

            // change joint key values
            m_CurrentJoint.dampingRatio = 7f;

            m_CurrentController = controller;

            lr.positionCount = 2;
        }
    }

    public void DrawGrapple()
    {
        if (m_CurrentJoint && m_CurrentController != null) {
            lr.SetPosition(0, m_CurrentController.m_GrappleGun.position);
            lr.SetPosition(1, gameObject.transform.position);
        }
    }

    public void EndGrapple()
    {
        if (m_CurrentJoint)
        {
            Destroy(m_CurrentJoint);
            m_CurrentJoint = null;
            m_CurrentController = null;
            lr.positionCount = 0;
        }
    }

    private void LateUpdate()
    {
        DrawGrapple();
    }
}

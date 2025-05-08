using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    public Transform visionOrigin;
    public Transform player;
    public float viewDistance = 10f;
    public float viewAngle = 60f;
    public LayerMask obstacleMask;

    public enum AwarenessState { Unaware, Suspicious, Alerted, Engaged }
    public AwarenessState currentState = AwarenessState.Unaware;

    void Update()
    {
        if (CanSeePlayer())
        {
            if (currentState != AwarenessState.Engaged)
            {
                currentState = AwarenessState.Engaged;
                Debug.Log("Enemy State: Engaged");
                GetComponent<Renderer>().material.color = Color.red;
            }
        }
        else
        {
            if (currentState != AwarenessState.Unaware)
            {
                currentState = AwarenessState.Unaware;
                Debug.Log("Enemy State: Unaware");
                GetComponent<Renderer>().material.color = Color.white;
            }
        }
    }

    bool CanSeePlayer()
    {
        Vector3 dirToPlayer = (player.position - visionOrigin.position).normalized;
        float angleToPlayer = Vector3.Angle(visionOrigin.forward, dirToPlayer);

        if (angleToPlayer < viewAngle / 2)
        {
            float distToPlayer = Vector3.Distance(visionOrigin.position, player.position);

            if (!Physics.Raycast(visionOrigin.position, dirToPlayer, distToPlayer, obstacleMask))
            {
                return distToPlayer <= viewDistance;
            }
        }

        return false;
    }
}

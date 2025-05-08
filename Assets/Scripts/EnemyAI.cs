using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Unaware, Suspicious, Alerted, Engaged, Searching, Retreating }
    public EnemyState currentState = EnemyState.Unaware;
    private EnemyState lastState; // Track previous state for voice lines

    private NavMeshAgent agent;
    private AudioSource voiceAudio;

    [Header("References")]
    public Transform player;
    public TextMeshProUGUI stateText;
    public Transform retreatPoint;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    private int patrolIndex = -1;
    public float patrolSpeed = 2f;

    [Header("Vision Settings")]
    public float viewRadius = 12f;
    [Range(0, 360)] public float viewAngle = 120f;
    public LayerMask playerMask;
    public LayerMask obstructionMask;

    [Header("Hearing Settings")]
    public float hearingRadius = 6f;

    [Header("Combat")]
    public float attackRange = 1.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;
    public Transform attackPoint;
    public LayerMask playerLayer;

    private float lastAttackTime;

    [Header("Search")]
    private Vector3 lastSeenPosition;
    private float searchDuration = 5f;
    private float searchTimer;
    private bool playerWasSeen = false;

    [Header("Retreat & Heal")]
    public float retreatSpeed = 3f;
    public float healRate = 10f;
    private bool isRetreating = false;

    [Header("Voice Clips")]
    public AudioClip spottedClip;
    public AudioClip searchingClip;
    public AudioClip patrollingClip;
    public AudioClip lostPlayerClip;

    void Start()
    {
        currentState = EnemyState.Unaware;
        lastState = currentState; // Initialize lastState
        agent = GetComponent<NavMeshAgent>();
        voiceAudio = GetComponent<AudioSource>();
        GoToNextPatrolPoint();
        InvokeRepeating("CheckPerception", 0f, 0.2f);
    }

    void Update()
    {
        UpdateStateDisplay();

        // Play voice ONLY when state changes
        if (currentState != lastState)
        {
            PlayStateVoice();
            lastState = currentState;
        }

        switch (currentState)
        {
            case EnemyState.Unaware:
            case EnemyState.Suspicious:
                Patrol();
                break;

            case EnemyState.Alerted:
                Chase();
                break;

            case EnemyState.Engaged:
                Engage();
                break;

            case EnemyState.Searching:
                Search();
                break;

            case EnemyState.Retreating:
                Retreat();
                break;
        }
    }
    void Patrol()
    {
        agent.speed = patrolSpeed;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
        {
            GoToNextPatrolPoint();
        }
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    void Chase()
    {
        agent.speed = patrolSpeed * 1.5f;
        agent.SetDestination(player.position);
    }

    void Engage()
    {
        agent.SetDestination(player.position);
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange)
        {
            TryAttackPlayer();
        }
    }

    void Search()
    {
        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            Vector3 randomOffset = Random.insideUnitSphere * 3f;
            randomOffset.y = 0;
            Vector3 searchTarget = lastSeenPosition + randomOffset;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(searchTarget, out hit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }

        searchTimer += Time.deltaTime;
        if (searchTimer > searchDuration)
        {
            playerWasSeen = false;
            searchTimer = 0f;
            currentState = EnemyState.Unaware;
            GoToNextPatrolPoint();
        }
    }

    void TryAttackPlayer()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        Collider[] hitPlayers = Physics.OverlapSphere(attackPoint.position, attackRange, playerLayer);
        foreach (Collider player in hitPlayers)
        {
            if (player.CompareTag("Player"))
            {
                HealthSystem hs = player.GetComponent<HealthSystem>();
                if (hs != null)
                {
                    hs.TakeDamage(attackDamage, hs.isBlocking);
                    lastAttackTime = Time.time;
                }
            }
        }
    }

    void Retreat()
    {
        if (!isRetreating)
        {
            isRetreating = true;
            agent.speed = retreatSpeed;
            agent.SetDestination(retreatPoint.position);
        }

        if (Vector3.Distance(transform.position, retreatPoint.position) < 0.5f)
        {
            HealthSystem health = GetComponent<HealthSystem>();
            if (health != null)
            {
                health.Heal(healRate * Time.deltaTime);
                if (health.GetCurrentHealth() >= health.maxHealth)
                {
                    isRetreating = false;
                    currentState = EnemyState.Unaware;
                    GoToNextPatrolPoint();
                }
            }
        }
    }

    public void RetreatAndHeal()
    {
        currentState = EnemyState.Retreating;
    }

    void CheckPerception()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        bool canSeePlayer = false;

        // Vision
        Collider[] hits = Physics.OverlapSphere(transform.position, viewRadius, playerMask);
        if (hits.Length > 0)
        {
            Transform target = hits[0].transform;
            Vector3 dirToPlayer = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2 &&
                !Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distToPlayer, obstructionMask))
            {
                canSeePlayer = true;
                lastSeenPosition = target.position;
                playerWasSeen = true;
            }
        }

        // Hearing
        bool heardPlayer = false;
        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        if (pm && pm.isSprinting && distToPlayer < hearingRadius)
        {
            heardPlayer = true;
        }

        // State Transitions
        if (canSeePlayer)
        {
            currentState = (distToPlayer <= attackRange) ? EnemyState.Engaged : EnemyState.Alerted;
            PlayVoice(spottedClip);
        }
        else if (heardPlayer && currentState == EnemyState.Unaware)
        {
            currentState = EnemyState.Suspicious;
        }
        else if (playerWasSeen && currentState != EnemyState.Searching && !canSeePlayer)
        {
            currentState = EnemyState.Searching;
        }
    }

    // Simplified PlayVoice (no repeat checks)
    void PlayVoice(AudioClip clip)
    {
        if (voiceAudio != null && clip != null)  
        {
            voiceAudio.Stop();
            voiceAudio.clip = clip;
            voiceAudio.Play();
            //lastVoiceTime = Time.time; // Record when the voice was last played
        }
    }
    void UpdateStateDisplay()
    {
        if (stateText != null)
            stateText.text = $"State: {currentState}";
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
        Gizmos.color = Color.red;
        if (attackPoint != null)
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    // New method to handle voice lines per state
    void PlayStateVoice()
    {
        AudioClip clipToPlay = null;

        switch (currentState)
        {
            case EnemyState.Alerted:
            case EnemyState.Engaged:
                clipToPlay = spottedClip;
                break;

            case EnemyState.Searching:
                clipToPlay = lostPlayerClip;
                break;

            case EnemyState.Unaware:
            case EnemyState.Suspicious:
                clipToPlay = patrollingClip;
                break;

            // Add retreating voice clip if needed
            case EnemyState.Retreating:
                break;
        }

        if (clipToPlay != null && voiceAudio != null)
        {
            voiceAudio.Stop();
            voiceAudio.clip = clipToPlay;
            voiceAudio.Play();
        }
    }
}

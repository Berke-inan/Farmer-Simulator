using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider))]
public class ChickenAI : NetworkBehaviour
{
    [Header("Kümes ve Gezinme")]
    public Transform coopTransform;
    public float wanderRadius = 8f;

    [Header("Hýz Ayarlarý")]
    public float walkSpeed = 1.2f;
    public float runSpeed = 3.5f;

    [Header("Korku Ayarlarý")]
    public float fleeDistance = 1.5f;
    public float discomfortDistance = 4f;
    public float discomfortTimeLimit = 3f;

    [Tooltip("Panikleyip kaçtýðýnda bizden kaç metre uzaða koþsun?")]
    public float runAwayDistance = 7f;

    public NetworkVariable<float> netSpeed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NavMeshAgent agent;
    private Animator animator;
    private float stateTimer;
    private float discomfortTimer;

    private Vector3 lastPosition;
    private float stuckTimer;

    private enum ChickenState { Idle, Walking, Pecking, Scared }
    private ChickenState currentState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            agent.enabled = false;
        }
        else
        {
            lastPosition = transform.position;
            ChooseNextState();
        }
    }

    private void Update()
    {
        if (animator != null) animator.SetFloat("Speed", netSpeed.Value);

        if (!IsServer || !agent.isOnNavMesh) return;

        HandlePlayerProximity();

        stateTimer -= Time.deltaTime;

        if (currentState == ChickenState.Walking || currentState == ChickenState.Scared)
        {
            netSpeed.Value = agent.velocity.magnitude;

            float distMovement = Vector3.Distance(transform.position, lastPosition);
            lastPosition = transform.position;

            if (distMovement < 0.01f && agent.remainingDistance > agent.stoppingDistance)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > 1.5f)
                {
                    stuckTimer = 0f;
                    agent.ResetPath();
                    ChooseNextState();
                    return;
                }
            }
            else
            {
                stuckTimer = 0f;
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (currentState == ChickenState.Scared)
                {
                    currentState = ChickenState.Idle;
                    stateTimer = Random.Range(2f, 4f);
                    netSpeed.Value = 0f;
                    agent.ResetPath();
                }
                else
                {
                    ChooseNextState();
                }
            }
        }
        else
        {
            netSpeed.Value = 0f;
            if (stateTimer <= 0) ChooseNextState();
        }
    }

    private void HandlePlayerProximity()
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, discomfortDistance);
        bool playerNearby = false;

        foreach (Collider col in nearbyObjects)
        {
            if (col.TryGetComponent(out PlayerMovement player))
            {
                playerNearby = true;
                float distance = Vector3.Distance(transform.position, player.transform.position);

                if (distance < fleeDistance)
                {
                    PanicAndRun(player.transform.position);
                    return;
                }

                // GÜNCELLEME: Tavuk hem beklerken hem de gagalarken rahatsýz olabilir
                if (currentState == ChickenState.Idle || currentState == ChickenState.Pecking)
                {
                    discomfortTimer += Time.deltaTime;
                    if (discomfortTimer >= discomfortTimeLimit)
                    {
                        discomfortTimer = 0;
                        ForceWalkAway(); // Rastgele seçme, kesinlikle yürüyerek uzaklaþ!
                    }
                }
                return;
            }
        }

        if (!playerNearby)
        {
            discomfortTimer = 0;
        }
    }

    // YENÝ FONKSÝYON: Rahatsýz olunca zorla yürüme
    private void ForceWalkAway()
    {
        currentState = ChickenState.Walking;
        agent.speed = walkSpeed;
        agent.acceleration = 3f; // Yürüme için yumuþak kalkýþ (Start Walk animasyonuna uyum saðlar)

        agent.SetDestination(GetRandomPointInCoop());
        stateTimer = 10f;
    }

    private void PanicAndRun(Vector3 threatPosition)
    {
        if (currentState == ChickenState.Scared) return;

        currentState = ChickenState.Scared;
        agent.speed = runSpeed;
        agent.acceleration = 12f;

        // Tehlikeden tam ters yöne kaçýþ noktasý hesapla
        Vector3 fleeDirection = (transform.position - threatPosition).normalized;

        // 5f yerine artýk Inspector'dan belirlediðimiz mesafeyi kullanýyoruz!
        Vector3 targetPoint = transform.position + fleeDirection * runAwayDistance;

        NavMeshHit hit;
        // NavMesh de bu yeni mesafeye göre güvenli alan taramasý yapacak
        if (NavMesh.SamplePosition(targetPoint, out hit, runAwayDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        stateTimer = 4f;
    }

    private void ChooseNextState()
    {
        agent.speed = walkSpeed;
        agent.acceleration = 3f; // Normal gezintide yumuþak ivme

        int randomAction = Random.Range(0, 3);

        if (randomAction == 0)
        {
            currentState = ChickenState.Idle;
            stateTimer = Random.Range(2f, 5f);
        }
        else if (randomAction == 1)
        {
            currentState = ChickenState.Pecking;
            stateTimer = Random.Range(3f, 6f);
            TriggerPeckRpc();
        }
        else
        {
            currentState = ChickenState.Walking;
            agent.SetDestination(GetRandomPointInCoop());
            stateTimer = 10f;
        }
    }

    private Vector3 GetRandomPointInCoop()
    {
        Vector3 centerPoint = coopTransform != null ? coopTransform.position : transform.position;
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += centerPoint;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randomDirection, out navHit, wanderRadius, NavMesh.AllAreas);
        return navHit.position;
    }

    [Rpc(SendTo.Everyone)]
    private void TriggerPeckRpc() => animator?.SetTrigger("Peck");
}
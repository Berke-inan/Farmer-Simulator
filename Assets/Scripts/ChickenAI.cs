using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ChickenAI : NetworkBehaviour
{
    [Header("Kümes ve Gezinme")]
    public Transform coopTransform;
    public float wanderRadius = 8f;

    [Header("Hýz Ayarlarý")]
    public float walkSpeed = 1.2f;
    public float runSpeed = 3.5f;

    [Header("Korku Ayarlarý")]
    public float fleeDistance = 1.5f; // Çarpma/Çok yakýn mesafe (Koþarak kaçar)
    public float discomfortDistance = 4f; // Rahatsýz olma mesafesi (Yürüyerek uzaklaþýr)
    public float discomfortTimeLimit = 3f; // Yanýnda ne kadar süre beklenirse uzaklaþýr?

    public NetworkVariable<float> netSpeed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NavMeshAgent agent;
    private Animator animator;
    private float stateTimer;
    private float discomfortTimer;

    private enum ChickenState { Idle, Walking, Pecking, Scared }
    private ChickenState currentState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.acceleration = 12f; // Daha hýzlý tepki vermesi için ivmeyi artýrdýk
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) agent.enabled = false;
        else ChooseNextState();
    }

    private void Update()
    {
        // Herkeste animasyonu güncelle (Hýz 1.5+ ise Koþma oynar)
        if (animator != null) animator.SetFloat("Speed", netSpeed.Value);

        if (!IsServer || !agent.isOnNavMesh) return;

        HandlePlayerProximity(); // Oyuncu yakýnlýk kontrolü

        stateTimer -= Time.deltaTime;

        if (currentState == ChickenState.Walking || currentState == ChickenState.Scared)
        {
            netSpeed.Value = agent.velocity.magnitude;

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                ChooseNextState();
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
        // Tavuðun etrafýna discomfortDistance (örn: 4 metre) çapýnda görünmez bir küre çizer ve içindekileri bulur
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, discomfortDistance);

        bool playerNearby = false;

        foreach (Collider col in nearbyObjects)
        {
            // Eðer yakýndaki objede PlayerMovement scripti varsa, bu kesinlikle bir oyuncudur! (Tag'e gerek yok)
            if (col.TryGetComponent(out PlayerMovement player))
            {
                playerNearby = true;
                float distance = Vector3.Distance(transform.position, player.transform.position);

                // 1. DURUM: ÇARPMA / ÇOK YAKIN (KAÇMA)
                if (distance < fleeDistance)
                {
                    PanicAndRun(player.transform.position);
                    return; // Bir oyuncudan kaçýyorsa diðerlerine bakmasýna gerek yok, döngüden çýk.
                }

                // 2. DURUM: YANINDA BEKLEME (RAHATSIZ OLMA)
                if (currentState == ChickenState.Idle)
                {
                    discomfortTimer += Time.deltaTime;
                    if (discomfortTimer >= discomfortTimeLimit)
                    {
                        discomfortTimer = 0;
                        ChooseNextState(); // Rahatsýz oldu, baþka yere yürüyecek
                    }
                }

                return; // Ýþlem tamam, döngüyü bitir.
            }
        }

        // Eðer döngü bittiðinde etrafta hiç oyuncu yoksa, rahatsýzlýk sayacýný sýfýrla
        if (!playerNearby)
        {
            discomfortTimer = 0;
        }
    }

    private void PanicAndRun(Vector3 threatPosition)
    {
        if (currentState == ChickenState.Scared) return;

        currentState = ChickenState.Scared;
        agent.speed = runSpeed;

        // Tehlikeden tam ters yöne kaçýþ noktasý hesapla
        Vector3 fleeDirection = (transform.position - threatPosition).normalized;
        Vector3 targetPoint = transform.position + fleeDirection * 5f;

        // Kaçtýðý noktanýn kümesten çok uzaklaþmamasýný saðla
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPoint, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        stateTimer = 4f; // En az 4 saniye panik modunda kal
        Debug.Log("<color=red>Tavuk:</color> KAÇIYORUM!");
    }

    private void ChooseNextState()
    {
        agent.speed = walkSpeed; // Hýzý normale çek
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
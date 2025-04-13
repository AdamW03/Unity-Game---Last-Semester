using UnityEngine;
using UnityEngine.AI; 


[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Referencje")]
    public Transform player; 

    [Header("Ustawienia Widzenia")]
    public float sightRange = 15f; // Jak daleko widzi AI
    public float fieldOfViewAngle = 90f; // Jak szeroki jest k¹t widzenia AI (w stopniach)
    public LayerMask obstacleMask; // Warstwa dla œcian, mebli itp. (przeszkody wzroku)
    public LayerMask playerMask;   // Warstwa TYLKO dla gracza

    [Header("Ustawienia Patrolowania")]
    public float patrolSpeed = 3f;
    public float walkPointRange = 10f; // Jak daleko od siebie AI szuka kolejnego punktu patrolu

    [Header("Ustawienia Gonienia")]
    public float chaseSpeed = 6f;
    public float timeToLosePlayer = 5f; // Ile sekund AI musi nie widzieæ gracza, by przestaæ goniæ

    private NavMeshAgent agent;
    private AIState currentState;
    private Vector3 walkPoint;
    private bool walkPointSet;
    private float losePlayerTimer;
    private Vector3 lastKnownPlayerPosition;

    private enum AIState
    {
        Patrolling, // Chodzenie po okolicy
        Chasing,    // Gonienie gracza
        Searching   // Szukanie gracza po utracie z oczu
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError("Nie znaleziono obiektu gracza z tagiem 'Player'! AI nie bêdzie dzia³aæ poprawnie.");
            enabled = false;
            return;
        }

        currentState = AIState.Patrolling;
        agent.speed = patrolSpeed; 
    }

    void Update()
    {
        bool canSeePlayer = CheckLineOfSight();

        switch (currentState)
        {
            case AIState.Patrolling:
                HandlePatrolling();
                if (canSeePlayer) TransitionToState(AIState.Chasing);
                break;

            case AIState.Chasing:
                HandleChasing(canSeePlayer);
                break;

            case AIState.Searching:
                HandleSearching(canSeePlayer);
                break;
        }
    }

    void HandlePatrolling()
    {    
        if (agent.speed != patrolSpeed) agent.speed = patrolSpeed;

        if (!walkPointSet) SearchWalkPoint();

        if (walkPointSet) agent.SetDestination(walkPoint);

        // SprawdŸ, czy dotar³ blisko celu LUB jeœli utkn¹³ (pathPending = false oznacza, ¿e ma œcie¿kê lub dotar³)
        if (walkPointSet && !agent.pathPending && agent.remainingDistance < agent.stoppingDistance + 0.5f)
        {
            walkPointSet = false;
        }
    }

    void HandleChasing(bool canSeePlayer)
    {
        if (agent.speed != chaseSpeed) agent.speed = chaseSpeed;

        if (canSeePlayer)
        {
            agent.SetDestination(player.position);
            lastKnownPlayerPosition = player.position;
            losePlayerTimer = 0f;
        }
        else
        {
            // IdŸ do ostatniej znanej pozycji (agent ju¿ tam idzie, bo to by³ ostatni cel)
            // agent.SetDestination(lastKnownPlayerPosition); // Nie trzeba tego powtarzaæ co klatkê

            losePlayerTimer += Time.deltaTime;

            // Jeœli min¹³ czas LUB dotarliœmy do ostatniej znanej pozycji i czekamy
            if (losePlayerTimer >= timeToLosePlayer)
            {
                // SprawdŸ dodatkowo, czy agent nie jest w trakcie obliczania nowej œcie¿ki
                if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance + 0.5f)
                {
                    TransitionToState(AIState.Searching);
                }
                // Jeœli czas min¹³, ale agent wci¹¿ idzie do celu, pozwól mu tam dojœæ
                // i dopiero wtedy przejdzie do Searching (lub jeœli zobaczy gracza)
            }
            // Jeœli dotar³ do celu ZANIM min¹³ czas, poczeka tam a¿ czas minie
            // lub zobaczy gracza, lub timer przekroczy limit
            if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance + 0.5f && losePlayerTimer >= timeToLosePlayer)
            {
                TransitionToState(AIState.Searching);
            }
        }
    }

    void HandleSearching(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            TransitionToState(AIState.Chasing);
            return;
        }

        if (agent.speed != patrolSpeed) agent.speed = patrolSpeed;

        agent.SetDestination(lastKnownPlayerPosition);

        if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance + 0.5f)
        {
            Debug.Log("AI: Dotar³em do ostatniej znanej pozycji, wracam do patrolowania.");
            TransitionToState(AIState.Patrolling);
        }
    }



    void TransitionToState(AIState newState)
    {
        if (currentState == newState) return; 

        Debug.Log($"AI zmienia stan z {currentState} na {newState}");
        currentState = newState;

        switch (newState)
        {
            case AIState.Patrolling:
                agent.speed = patrolSpeed;
                walkPointSet = false;
                break;
            case AIState.Chasing:
                agent.speed = chaseSpeed;
                losePlayerTimer = 0f;
                Debug.Log("AI: Widzê gracza! Zaczynam poœcig!");
                break;
            case AIState.Searching:
                agent.speed = patrolSpeed;
                Debug.Log("AI: Straci³em gracza z oczu, idê sprawdziæ ostatni¹ pozycjê.");
                break;
        }
    }

    void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);
        Vector3 randomDirection = new Vector3(randomX, 0, randomZ).normalized; // Kierunek losowy
        Vector3 potentialPoint = transform.position + randomDirection * Random.Range(walkPointRange * 0.5f, walkPointRange); // Punkt w pewnej odleg³oœci

        NavMeshHit hit;
        // Szukaj najbli¿szego punktu na NavMesh w promieniu np. 2.0f od potencjalnego punktu
        if (NavMesh.SamplePosition(potentialPoint, out hit, 2.0f, NavMesh.AllAreas))
        {
            walkPoint = hit.position;
            walkPointSet = true;
            // Debug.Log($"AI: Nowy punkt patrolu: {walkPoint}");
        }
        else
        {
            // Debug.Log("AI: Nie uda³o siê znaleŸæ punktu patrolu na NavMesh.");
            walkPointSet = false;
            // Spróbuj znaleŸæ punkt bli¿ej w nastêpnej klatce
        }
    }

    bool CheckLineOfSight()
    {
        if (player == null) return false; 

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > sightRange) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle > fieldOfViewAngle / 2) return false;

        // U¿yj wysokoœci agenta do okreœlenia pozycji oczu
        Vector3 rayOrigin = transform.position + Vector3.up * (agent.height * 0.8f); // Oko³o 80% wysokoœci agenta
        Vector3 playerTargetPos = player.position + Vector3.up * 1.0f; // Celuj w œrodek gracza
        Vector3 directionForRay = (playerTargetPos - rayOrigin).normalized;

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, directionForRay, out hit, sightRange, obstacleMask | playerMask))
        {
            if (((1 << hit.collider.gameObject.layer) & playerMask) != 0)
            {
                if (hit.transform == player)
                {
                    return true;
                }
            }
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        // Zasiêg widzenia
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // K¹t widzenia
        Gizmos.color = Color.blue;
        Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfViewAngle / 2, transform.up) * transform.forward * sightRange;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfViewAngle / 2, transform.up) * transform.forward * sightRange;
        Gizmos.DrawLine(transform.position, transform.position + fovLine1);
        Gizmos.DrawLine(transform.position, transform.position + fovLine2);

        // Linia wzroku (jeœli gracz istnieje)
        if (player != null)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * (agent != null ? agent.height * 0.8f : 1.5f); // U¿yj wysokoœci agenta jeœli dostêpna
            Vector3 playerTargetPos = player.position + Vector3.up * 1.0f;
            Vector3 directionForRay = (playerTargetPos - rayOrigin).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= sightRange && Vector3.Angle(transform.forward, (player.position - transform.position).normalized) <= fieldOfViewAngle / 2)
            {
                RaycastHit hit;
                if (Physics.Raycast(rayOrigin, directionForRay, out hit, sightRange, obstacleMask | playerMask))
                {
                    if (((1 << hit.collider.gameObject.layer) & playerMask) != 0 && hit.transform == player)
                        Gizmos.color = Color.green;
                    else
                        Gizmos.color = Color.red;
                    Gizmos.DrawLine(rayOrigin, hit.point);
                }
                else
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(rayOrigin, rayOrigin + directionForRay * sightRange);
                }
            }
        }

        // Punkt patrolu
        if (walkPointSet)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(walkPoint, 0.5f);
            Gizmos.DrawLine(transform.position, walkPoint);
        }
    }
}
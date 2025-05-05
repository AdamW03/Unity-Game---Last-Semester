using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic; // Potrzebne dla List<>
using System.Linq; // Potrzebne dla ToList()

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    #region Zmienne Konfiguracyjne (Inspektor)

    [Header("Referencje")]
    [Tooltip("Obiekt gracza, którego AI ma œcigaæ. Powinien mieæ tag 'Player'.")]
    public Transform player;
    [Tooltip("Lista punktów (Transform), które AI odwiedza podczas patrolowania. Jeœli pusta, AI u¿ywa losowych punktów.")]
    public List<Transform> patrolWaypoints = new List<Transform>();

    [Header("Ustawienia Widzenia")]
    [Tooltip("Maksymalna odleg³oœæ, z jakiej AI mo¿e zobaczyæ gracza.")]
    public float sightRange = 15f;
    [Tooltip("Szerokoœæ k¹ta widzenia AI (w stopniach).")]
    public float fieldOfViewAngle = 90f;
    [Tooltip("Warstwa (Layer) zawieraj¹ca przeszkody blokuj¹ce wzrok (œciany, meble itp.).")]
    public LayerMask obstacleMask;
    [Tooltip("Warstwa (Layer) przypisana TYLKO do obiektu gracza.")]
    public LayerMask playerMask;

    [Header("Ustawienia Patrolowania")]
    [Tooltip("Prêdkoœæ poruszania siê AI podczas patrolowania.")]
    public float patrolSpeed = 3f;
    [Tooltip("Maksymalna odleg³oœæ od AI, w jakiej szukany jest nastêpny LOSOWY punkt patrolu (u¿ywane, gdy lista Waypoints jest pusta).")]
    public float randomWalkPointRange = 10f;
    [Tooltip("Prêdkoœæ obrotu podczas patrolowania (stopnie/sekundê). Agent kontroluje obrót.")]
    public float patrolAngularSpeed = 120f;
    [Tooltip("Przyspieszenie podczas patrolowania.")]
    public float patrolAcceleration = 8f;
    [Tooltip("Czy AI ma siê zatrzymaæ i rozejrzeæ po dotarciu do punktu patrolu?")]
    public bool stopAndLookEnabled = true;
    [Tooltip("Jak d³ugo (w sekundach) AI ma siê rozgl¹daæ po zatrzymaniu.")]
    public float observationDuration = 3.0f;
    [Tooltip("Prêdkoœæ obrotu podczas rozgl¹dania siê (stopnie/sekundê).")]
    public float observationAngularSpeed = 90f;
    [Tooltip("K¹t sto¿ka (w stopniach), w którym AI szuka LOSOWEGO punktu patrolu po zgubieniu gracza (gdy nie u¿ywa Waypoints). 360 = w pe³ni losowo.")]
    [Range(10f, 360f)]
    public float patrolBiasConeAngle = 90f;

    [Header("Ustawienia Gonienia")]
    [Tooltip("Prêdkoœæ poruszania siê AI podczas gonienia gracza.")]
    public float chaseSpeed = 7f;
    [Tooltip("Przyspieszenie podczas gonienia (wp³ywa na szybkoœæ osi¹gania prêdkoœci).")]
    public float chaseAcceleration = 50f;

    [Header("Ustawienia Szukania")]
    [Tooltip("Prêdkoœæ podczas szukania ostatniej znanej pozycji gracza i przeszukiwania obszaru.")]
    public float searchSpeed = 5f;
    [Tooltip("Prêdkoœæ obrotu podczas szukania (stopnie/sekundê). Agent kontroluje obrót.")]
    public float searchAngularSpeed = 360f;
    [Tooltip("Przyspieszenie podczas szukania.")]
    public float searchAcceleration = 16f;
    [Tooltip("Czy AI ma przeszukaæ obszar wokó³ ostatniej znanej pozycji gracza?")]
    public bool areaSweepEnabled = true;
    [Tooltip("Promieñ obszaru do przeszukania wokó³ ostatniej znanej pozycji gracza.")]
    public float sweepRadius = 5.0f;
    [Tooltip("Ile dodatkowych punktów w obszarze ma sprawdziæ AI.")]
    public int numberOfSweepPoints = 3;

    #endregion

    #region Zmienne Prywatne

    private NavMeshAgent agent;
    private AIState currentState;

    // Patrolowanie
    private Vector3 currentPatrolTargetPosition;
    private bool patrolTargetSet;
    private int currentWaypointIndex = -1; // Indeks OSTATNIO WYBRANEGO/OSI¥GNIÊTEGO waypointa
    private bool useWaypoints = false;
    private List<int> availableWaypointIndices = new List<int>(); // NOWA ZMIENNA: Indeksy waypointów dostêpnych w bie¿¹cym cyklu
    private Vector3? nextPatrolDirectionBias = null;

    // Obserwacja
    private float observationTimer;
    private Quaternion targetObservationRotation;
    private float nextObservationTurnTime;

    // Szukanie
    private Vector3 lastKnownPlayerPosition;
    private Vector3 searchOriginPosition;
    private bool isSweepingArea = false;
    private List<Vector3> sweepPoints = new List<Vector3>();
    private int currentSweepPointIndex = -1;

    #endregion

    #region Stany AI

    private enum AIState
    {
        Patrolling,
        Observing,
        Chasing,
        Searching
    }

    #endregion

    #region Metody MonoBehaviour (Awake, Update, LateUpdate)

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
        else { Debug.LogError($"[{gameObject.name}] Nie znaleziono gracza 'Player'!", this); enabled = false; return; }

        if (agent == null) { Debug.LogError($"[{gameObject.name}] Brak NavMeshAgent!", this); enabled = false; return; }

        // Ustalenie trybu patrolowania i inicjalizacja listy dostêpnych waypointów
        InitializePatrolMode();

        agent.updateRotation = true;
        agent.stoppingDistance = 1.0f;
        TransitionToState(AIState.Patrolling);
    }

    void Update()
    {
        if (player == null)
        {
            if (currentState != AIState.Patrolling && currentState != AIState.Observing)
                TransitionToState(AIState.Patrolling);
            if (currentState == AIState.Patrolling) HandlePatrolling();
            else if (currentState == AIState.Observing) HandleObserving();
            return;
        }

        bool canSeePlayer = CheckLineOfSight();

        switch (currentState)
        {
            case AIState.Patrolling:
                HandlePatrolling();
                if (canSeePlayer) TransitionToState(AIState.Chasing);
                break;
            case AIState.Observing:
                HandleObserving();
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

    void LateUpdate()
    {
        if (currentState == AIState.Chasing && player != null)
        {
            if (agent.updateRotation == false)
            {
                Vector3 directionToPlayer = player.position - transform.position;
                directionToPlayer.y = 0;
                if (directionToPlayer.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(directionToPlayer);
                }
            }
        }
    }

    #endregion

    #region Metody Obs³uguj¹ce Stany

    // --- PATROLOWANIE ---
    void HandlePatrolling()
    {
        if (!patrolTargetSet)
        {
            FindNextPatrolTarget();
        }

        if (patrolTargetSet && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.1f * 0.1f)
            {
                patrolTargetSet = false;
                // currentWaypointIndex zosta³ ustawiony w FindNextPatrolTarget

                if (stopAndLookEnabled)
                {
                    TransitionToState(AIState.Observing);
                }
                else
                {
                    FindNextPatrolTarget();
                }
            }
        }
    }

    // --- OBSERWACJA ---
    void HandleObserving()
    {
        observationTimer -= Time.deltaTime;
        if (observationTimer <= 0f)
        {
            TransitionToState(AIState.Patrolling);
            return;
        }

        if (Time.time >= nextObservationTurnTime)
        {
            float randomAngle = Random.Range(-90f, 90f);
            targetObservationRotation = transform.rotation * Quaternion.Euler(0, randomAngle, 0);
            nextObservationTurnTime = Time.time + Random.Range(1.0f, 1.5f);
        }

        float angleDifference = Quaternion.Angle(transform.rotation, targetObservationRotation);
        if (angleDifference > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetObservationRotation, observationAngularSpeed * Time.deltaTime / angleDifference);
        }
        else
        {
            transform.rotation = targetObservationRotation;
        }
    }


    // --- GONIENIE ---
    void HandleChasing(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            agent.SetDestination(player.position);
            lastKnownPlayerPosition = player.position;
        }
        else
        {
            searchOriginPosition = transform.position;
            TransitionToState(AIState.Searching);
        }
    }

    // --- SZUKANIE ---
    void HandleSearching(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            TransitionToState(AIState.Chasing);
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.1f * 0.1f)
            {
                if (isSweepingArea)
                {
                    ProceedToNextSweepPoint();
                }
                else
                {
                    if (areaSweepEnabled) StartAreaSweep();
                    else FinishSearching();
                }
            }
        }
    }

    #endregion

    #region Metody Pomocnicze (Stany i Logika)

    // Inicjalizuje tryb patrolowania i listê dostêpnych waypointów
    void InitializePatrolMode()
    {
        useWaypoints = patrolWaypoints != null && patrolWaypoints.Count > 0;
        if (useWaypoints)
        {
            // Usuñ puste wpisy z listy waypointów
            patrolWaypoints.RemoveAll(item => item == null);

            if (patrolWaypoints.Count == 0)
            {
                Debug.LogWarning($"[{gameObject.name}] Lista waypointów jest pusta po usuniêciu nulli. U¿ywam losowego patrolowania.", this);
                useWaypoints = false;
            }
            else
            {
                // Wype³nij listê dostêpnych indeksów na starcie
                FillAvailableWaypoints();
                currentWaypointIndex = -1; // Resetuj ostatni indeks
                // Debug.Log($"[{gameObject.name}] U¿ywam waypointów. Dostêpne na starcie: {availableWaypointIndices.Count}");
            }
        }
    }

    // Wype³nia listê availableWaypointIndices wszystkimi indeksami
    void FillAvailableWaypoints()
    {
        availableWaypointIndices.Clear();
        // Dodaj indeksy od 0 do N-1
        availableWaypointIndices.AddRange(Enumerable.Range(0, patrolWaypoints.Count));
        // Alternatywnie:
        // for (int i = 0; i < patrolWaypoints.Count; i++)
        // {
        //     availableWaypointIndices.Add(i);
        // }
    }


    // Ustawia nastêpny cel patrolu (waypoint lub losowy)
    void FindNextPatrolTarget()
    {
        patrolTargetSet = false;

        if (useWaypoints)
        {
            // --- NOWA LOGIKA: LOSOWY Z DOSTÊPNYCH, RESETOWANIE CYKLU ---
            if (availableWaypointIndices.Count == 0)
            {
                // Skoñczy³ siê cykl, resetuj listê dostêpnych
                // Debug.Log($"[{gameObject.name}] Wszystkie waypointy odwiedzone. Resetujê cykl.");
                FillAvailableWaypoints();

                // WA¯NE: Usuñ z nowej listy indeks waypointa, do którego W£AŒNIE dotarliœmy,
                // aby nie wybra³ go od razu ponownie (chyba ¿e jest tylko jeden).
                if (patrolWaypoints.Count > 1 && currentWaypointIndex != -1)
                {
                    availableWaypointIndices.Remove(currentWaypointIndex);
                    // Debug.Log($"[{gameObject.name}] Usuniêto ostatnio odwiedzony indeks {currentWaypointIndex} z nowej listy dostêpnych.");
                }

                // Jeœli po usuniêciu lista znów jest pusta (co mo¿e siê zdarzyæ tylko przy 1 waypoincie),
                // to znaczy, ¿e musimy go wybraæ ponownie. Wype³nijmy j¹ jeszcze raz.
                if (availableWaypointIndices.Count == 0 && patrolWaypoints.Count > 0)
                {
                    // Debug.Log($"[{gameObject.name}] Lista dostêpnych pusta po usuniêciu ostatniego (prawdopodobnie tylko 1 waypoint). Resetujê ponownie.");
                    FillAvailableWaypoints();
                }
            }

            // Jeœli nadal mamy dostêpne waypointy (powinniœmy, chyba ¿e lista patrolWaypoints jest pusta)
            if (availableWaypointIndices.Count > 0)
            {
                // Wybierz losowy indeks Z LISTY DOSTÊPNYCH
                int randomIndexInAvailableList = Random.Range(0, availableWaypointIndices.Count);
                int nextWaypointIndex = availableWaypointIndices[randomIndexInAvailableList];

                // Usuñ wybrany indeks z listy dostêpnych
                availableWaypointIndices.RemoveAt(randomIndexInAvailableList);

                // Zapisz wybrany indeks jako bie¿¹cy
                currentWaypointIndex = nextWaypointIndex;

                // SprawdŸ, czy waypoint na tym indeksie istnieje
                if (patrolWaypoints[currentWaypointIndex] != null)
                {
                    currentPatrolTargetPosition = patrolWaypoints[currentWaypointIndex].position;
                    patrolTargetSet = true;
                    // Debug.Log($"[{gameObject.name}] Wybrano waypoint {currentWaypointIndex}: {patrolWaypoints[currentWaypointIndex].name}. Pozosta³o dostêpnych: {availableWaypointIndices.Count}", this);
                }
                else
                {
                    Debug.LogError($"[{gameObject.name}] Wybrany waypoint na indeksie {currentWaypointIndex} jest NULL! Szukam nastêpnego.", this);
                    // Nie ustawiamy celu, spróbuje ponownie w nastêpnej klatce
                    return;
                }
            }
            else
            {
                // To nie powinno siê zdarzyæ, jeœli lista patrolWaypoints nie jest pusta
                Debug.LogError($"[{gameObject.name}] Brak dostêpnych waypointów do wybrania, mimo ¿e useWaypoints=true!", this);
                return;
            }
            // --- KONIEC NOWEJ LOGIKI ---
        }
        else // Tryb losowych punktów (bez zmian)
        {
            Vector3 targetDirection = Vector3.zero;
            bool useBias = false;
            if (nextPatrolDirectionBias.HasValue)
            {
                targetDirection = nextPatrolDirectionBias.Value;
                useBias = true;
                nextPatrolDirectionBias = null;
            }

            if (SearchWalkPoint(targetDirection, !useBias, out currentPatrolTargetPosition))
            {
                patrolTargetSet = true;
            }
        }

        // Jeœli znaleziono cel, ustaw go dla agenta i sprawdŸ osi¹galnoœæ
        if (patrolTargetSet)
        {
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(currentPatrolTargetPosition, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetDestination(currentPatrolTargetPosition);
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Cel patrolu {currentPatrolTargetPosition} nieosi¹galny. Szukam nowego.", this);
                patrolTargetSet = false; // Oznacz, ¿e trzeba szukaæ ponownie
                // Jeœli u¿ywamy waypointów, powinniœmy te¿ przywróciæ w³aœnie usuniêty indeks do dostêpnych,
                // bo nie uda³o siê do niego dojœæ.
                if (useWaypoints && currentWaypointIndex != -1 && !availableWaypointIndices.Contains(currentWaypointIndex))
                {
                    // Debug.Log($"[{gameObject.name}] Przywracam indeks {currentWaypointIndex} do dostêpnych z powodu nieosi¹galnoœci celu.", this);
                    availableWaypointIndices.Add(currentWaypointIndex);
                }
            }
        }
    }

    // Rozpoczyna przeszukiwanie obszaru wokó³ LKP (z punktami w ró¿nych kierunkach)
    void StartAreaSweep()
    {
        isSweepingArea = true;
        sweepPoints.Clear();
        currentSweepPointIndex = -1;

        if (numberOfSweepPoints <= 0)
        {
            isSweepingArea = false;
            FinishSearching();
            return;
        }

        float angleIncrement = 360f / numberOfSweepPoints;
        for (int i = 0; i < numberOfSweepPoints; i++)
        {
            float targetAngle = (i * angleIncrement) + Random.Range(-angleIncrement / 4f, angleIncrement / 4f);
            float randomRadius = Random.Range(sweepRadius * 0.4f, sweepRadius);
            Vector3 direction = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
            Vector3 potentialPoint = lastKnownPlayerPosition + direction * randomRadius;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(potentialPoint, out hit, sweepRadius * 0.3f, NavMesh.AllAreas))
            {
                bool tooClose = false;
                if (Vector3.Distance(hit.position, lastKnownPlayerPosition) < agent.stoppingDistance * 1.5f) tooClose = true;
                if (!tooClose)
                {
                    foreach (Vector3 existingPoint in sweepPoints)
                    {
                        if (Vector3.Distance(hit.position, existingPoint) < agent.stoppingDistance * 2.0f) { tooClose = true; break; }
                    }
                }
                if (!tooClose) sweepPoints.Add(hit.position);
            }
        }

        if (sweepPoints.Count == 0)
        {
            isSweepingArea = false;
            FinishSearching();
            return;
        }
        ProceedToNextSweepPoint();
    }

    // Ustawia nastêpny punkt przeszukiwania jako cel lub koñczy przeszukiwanie
    void ProceedToNextSweepPoint()
    {
        currentSweepPointIndex++;
        if (currentSweepPointIndex < sweepPoints.Count)
        {
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(sweepPoints[currentSweepPointIndex], path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetDestination(sweepPoints[currentSweepPointIndex]);
            }
            else
            {
                ProceedToNextSweepPoint(); // Spróbuj nastêpny
            }
        }
        else
        {
            isSweepingArea = false;
            FinishSearching();
        }
    }

    // Koñczy stan Searching i przechodzi do Patrolling
    void FinishSearching()
    {
        Vector3 searchDirection = (lastKnownPlayerPosition - searchOriginPosition).normalized;
        if (!useWaypoints && searchDirection.sqrMagnitude > 0.1f)
        {
            nextPatrolDirectionBias = searchDirection;
        }
        else
        {
            nextPatrolDirectionBias = null;
        }
        TransitionToState(AIState.Patrolling);
    }


    // Próbuje znaleŸæ losowy punkt na NavMesh
    bool SearchWalkPoint(Vector3 directionBias, bool fullyRandom, out Vector3 result)
    {
        Vector3 randomDirection;
        if (fullyRandom || directionBias == Vector3.zero)
        {
            float randomAngle = Random.Range(0f, 360f);
            randomDirection = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;
        }
        else
        {
            float randomAngleInCone = Random.Range(-patrolBiasConeAngle / 2f, patrolBiasConeAngle / 2f);
            randomDirection = Quaternion.LookRotation(directionBias) * Quaternion.Euler(0, randomAngleInCone, 0) * Vector3.forward;
        }

        float randomDistance = Random.Range(randomWalkPointRange * 0.5f, randomWalkPointRange);
        Vector3 potentialPoint = transform.position + randomDirection.normalized * randomDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(potentialPoint, out hit, randomWalkPointRange * 0.5f, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }
        else
        {
            if (!fullyRandom) return SearchWalkPoint(Vector3.zero, true, out result);
        }
        result = transform.position;
        return false;
    }


    // Zarz¹dza zmian¹ stanu AI
    void TransitionToState(AIState newState)
    {
        if (currentState == newState && agent.isOnNavMesh) return;
        if (currentState == AIState.Observing) { agent.isStopped = false; }

        AIState previousState = currentState;
        currentState = newState;

        switch (newState)
        {
            case AIState.Patrolling:
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.speed = patrolSpeed;
                agent.angularSpeed = patrolAngularSpeed;
                agent.acceleration = patrolAcceleration;
                if (previousState != AIState.Observing) patrolTargetSet = false;
                isSweepingArea = false;
                break;

            case AIState.Observing:
                agent.isStopped = true;
                agent.updateRotation = true;
                observationTimer = observationDuration;
                nextObservationTurnTime = Time.time + Random.Range(0.1f, 0.5f);
                targetObservationRotation = transform.rotation;
                isSweepingArea = false;
                break;

            case AIState.Chasing:
                agent.isStopped = false;
                agent.updateRotation = false;
                agent.speed = chaseSpeed;
                agent.acceleration = chaseAcceleration;
                nextPatrolDirectionBias = null;
                isSweepingArea = false;
                if (player != null) { agent.SetDestination(player.position); lastKnownPlayerPosition = player.position; }
                break;

            case AIState.Searching:
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.speed = searchSpeed;
                agent.angularSpeed = searchAngularSpeed;
                agent.acceleration = searchAcceleration;
                nextPatrolDirectionBias = null;
                isSweepingArea = false;
                sweepPoints.Clear();
                currentSweepPointIndex = -1;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(lastKnownPlayerPosition, out hit, 1.0f, NavMesh.AllAreas))
                {
                    NavMeshPath path = new NavMeshPath();
                    if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete) agent.SetDestination(hit.position);
                    else { TransitionToState(AIState.Patrolling); }
                }
                else { TransitionToState(AIState.Patrolling); }
                break;
        }
    }

    // Sprawdza, czy AI widzi gracza
    bool CheckLineOfSight()
    {
        if (player == null) return false;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > sightRange) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        if (currentState != AIState.Observing)
        {
            float angle = Vector3.Angle(transform.forward, directionToPlayer);
            if (angle > fieldOfViewAngle / 2) return false;
        }

        Vector3 rayOrigin = transform.position + Vector3.up * (agent.height * 0.8f);
        Vector3 playerTargetPos = player.position + Vector3.up * 1.0f;
        Vector3 directionForRay = (playerTargetPos - rayOrigin).normalized;
        RaycastHit hitInfo;
        if (Physics.Raycast(rayOrigin, directionForRay, out hitInfo, sightRange, obstacleMask | playerMask))
        {
            if (((1 << hitInfo.collider.gameObject.layer) & playerMask) != 0)
            {
                if (hitInfo.transform == player) return true;
            }
            return false;
        }
        return false;
    }

    #endregion

    #region Gizmos (Wizualizacje w Edytorze)

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Gizmos.color = Color.blue;
        Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfViewAngle / 2, transform.up) * transform.forward * sightRange;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfViewAngle / 2, transform.up) * transform.forward * sightRange;
        Gizmos.DrawLine(transform.position, transform.position + fovLine1);
        Gizmos.DrawLine(transform.position, transform.position + fovLine2);

        if (player != null)
        {
            float eyeHeight = (agent != null ? agent.height * 0.8f : 1.5f);
            Vector3 rayOrigin = transform.position + Vector3.up * eyeHeight;
            Vector3 playerTargetPos = player.position + Vector3.up * 1.0f;
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            Vector3 directionForRay = (playerTargetPos - rayOrigin).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= sightRange)
            {
                bool inFov = (currentState == AIState.Observing) || (Vector3.Angle(transform.forward, directionToPlayer) <= fieldOfViewAngle / 2);
                if (inFov)
                {
                    RaycastHit hitInfo;
                    if (Physics.Raycast(rayOrigin, directionForRay, out hitInfo, sightRange, obstacleMask | playerMask))
                    {
                        if (((1 << hitInfo.collider.gameObject.layer) & playerMask) != 0 && hitInfo.transform == player) Gizmos.color = Color.green;
                        else Gizmos.color = Color.red;
                        Gizmos.DrawLine(rayOrigin, hitInfo.point);
                    }
                    else
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(rayOrigin, rayOrigin + directionForRay * sightRange);
                    }
                }
            }
        }


        if (patrolTargetSet && (currentState == AIState.Patrolling || currentState == AIState.Observing))
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(currentPatrolTargetPosition, 0.5f);
            if (agent != null && agent.hasPath) Gizmos.DrawLine(transform.position, agent.pathEndPosition);
        }

        if (currentState == AIState.Searching && agent != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 1.0f);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(searchOriginPosition, lastKnownPlayerPosition);

            if (isSweepingArea)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
                Gizmos.DrawSphere(lastKnownPlayerPosition, sweepRadius);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(lastKnownPlayerPosition, sweepRadius);
                Gizmos.color = Color.yellow;
                foreach (var point in sweepPoints) Gizmos.DrawSphere(point, 0.3f);

                if (currentSweepPointIndex >= 0 && currentSweepPointIndex < sweepPoints.Count)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(sweepPoints[currentSweepPointIndex], 0.4f);
                    if (agent.hasPath) Gizmos.DrawLine(transform.position, agent.pathEndPosition);
                }
            }
            else if (agent.hasPath)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, agent.pathEndPosition);
            }
        }

        if (nextPatrolDirectionBias.HasValue && !useWaypoints)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, nextPatrolDirectionBias.Value * 5f);
        }

        if (useWaypoints && patrolWaypoints.Count > 0)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolWaypoints.Count; i++)
            {
                if (patrolWaypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(patrolWaypoints[i].position, 0.5f);
                    int nextIndex = (i + 1) % patrolWaypoints.Count;
                    // Rysuj linie tylko jeœli oba punkty istniej¹
                    if (patrolWaypoints[nextIndex] != null)
                    {
                        // Zmieñ kolor linii na szary, bo kolejnoœæ jest teraz losowa
                        Gizmos.color = Color.gray;
                        Gizmos.DrawLine(patrolWaypoints[i].position, patrolWaypoints[nextIndex].position);
                        Gizmos.color = Color.blue; // Wróæ do niebieskiego dla nastêpnej kuli
                    }
                }
            }
            // Zaznacz ostatnio wybrany/osi¹gniêty waypoint
            if (currentWaypointIndex >= 0 && currentWaypointIndex < patrolWaypoints.Count && patrolWaypoints[currentWaypointIndex] != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(patrolWaypoints[currentWaypointIndex].position, 0.6f);
            }
        }
    }

    #endregion
}
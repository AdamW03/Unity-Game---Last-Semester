using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    #region Zmienne Konfiguracyjne (Inspektor)

    [Header("Referencje")]
    public Transform player;
    public List<Transform> patrolWaypoints = new List<Transform>();

    [Header("Ustawienia Widzenia")]
    public float sightRange = 15f;
    public float fieldOfViewAngle = 90f;
    public LayerMask obstacleMask;
    public LayerMask playerMask;

    [Header("Ustawienia Patrolowania")]
    public float patrolSpeed = 1f;
    public float randomWalkPointRange = 10f;
    public float patrolAngularSpeed = 120f;
    public float patrolAcceleration = 8f;
    public bool stopAndLookEnabled = true;
    public float observationDuration = 3.0f;
    public float observationAngularSpeed = 90f;
    [Range(10f, 360f)]
    public float patrolBiasConeAngle = 90f;

    [Header("Ustawienia Gonienia i Badania LKP")]
    public float chaseSpeed = 4f;
    public float chaseAcceleration = 40f;
    [Tooltip("Jak d³ugo (w sekundach) AI ma kontynuowaæ ruch w kierunku gracza (nawet przez œciany) po dotarciu do LKP.")]
    public float anticipationDuration = 2.0f;

    // NOWE ZMIENNE DLA TELEPORTACJI WAYPOINTÓW
    [Header("Waypoint Teleportation")]
    public List<WaypointTeleportLink> teleportLinks = new List<WaypointTeleportLink>();

    // NOWE ZMIENNE DLA WYKRYWANIA ZABLOKOWANIA
    [Header("Bug Detection")]
    [Tooltip("Po jakim czasie bez znacz¹cego ruchu AI zostanie zresetowane.")]
    public float stuckTimeThreshold = 5.0f;
    [Tooltip("Minimalna prêdkoœæ (jednostki/sekundê), poni¿ej której ruch uznawany jest za nieznacz¹cy.")]
    public float stuckSpeedThreshold = 0.1f;
    [Tooltip("Jak czêsto (w sekundach) sprawdzaæ, czy AI siê nie zablokowa³o.")]
    public float stuckCheckInterval = 1.0f;

    #endregion

    #region Zmienne Prywatne

    private NavMeshAgent agent;
    private AIState currentState;

    // Patrolowanie
    private Vector3 currentPatrolTargetPosition;
    private bool patrolTargetSet;
    private int currentWaypointIndex = -1;
    private bool useWaypoints = false;
    private List<int> availableWaypointIndices = new List<int>();
    private Vector3? nextPatrolDirectionBias = null;

    // Obserwacja
    private float observationTimer;
    private Quaternion targetObservationRotation;
    private float nextObservationTurnTime;

    // Gonienie i Badanie LKP
    private Vector3 lastKnownPlayerPosition;
    private float anticipationTimer;
    private bool reachedLKPInInvestigation;
    private Vector3 investigationOriginPosition;

    // NOWE ZMIENNE DLA WYKRYWANIA ZABLOKOWANIA
    private float timeSinceLastStuckCheck = 0f;
    private Vector3 lastPositionForStuckCheck;
    private float currentStuckTimer = 0f;

    #endregion

    #region Struktury Pomocnicze

    // NOWA STRUKTURA DLA LINKÓW TELEPORTACJI
    [System.Serializable]
    public struct WaypointTeleportLink
    {
        public Transform fromWaypoint;
        public Transform toWaypoint;
    }

    #endregion

    #region Stany AI

    private enum AIState
    {
        Patrolling,
        Observing,
        Chasing,
        InvestigatingLKP
    }

    #region Animacja

    private Animator animator;

    #endregion

    #endregion

    #region Metody MonoBehaviour (Awake, Update, LateUpdate)

    void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
        else { Debug.LogError($"[{gameObject.name}] Nie znaleziono gracza 'Player'!", this); enabled = false; return; }

        if (agent == null) { Debug.LogError($"[{gameObject.name}] Brak NavMeshAgent!", this); enabled = false; return; }

        InitializePatrolMode();
        agent.updateRotation = true;
        agent.stoppingDistance = 1.0f; // Upewnij siê, ¿e stoppingDistance jest rozs¹dne

        // Inicjalizacja dla wykrywania zablokowania
        lastPositionForStuckCheck = transform.position;

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

        // --- LOGIKA WYKRYWANIA ZABLOKOWANIA ---
        bool isMovingState = currentState == AIState.Patrolling ||
                             currentState == AIState.Chasing ||
                             currentState == AIState.InvestigatingLKP;

        if (agent.isOnNavMesh && isMovingState && !agent.isStopped)
        {
            timeSinceLastStuckCheck += Time.deltaTime;
            if (timeSinceLastStuckCheck >= stuckCheckInterval)
            {
                float distanceMoved = Vector3.Distance(transform.position, lastPositionForStuckCheck);
                bool potentiallyStuck = false;

                if (timeSinceLastStuckCheck > Mathf.Epsilon) // Unikaj dzielenia przez zero
                {
                    if ((distanceMoved / timeSinceLastStuckCheck) < stuckSpeedThreshold)
                    {
                        potentiallyStuck = true;
                    }
                }
                // Jeœli interwa³ jest bardzo ma³y, a ruch minimalny, te¿ uznaj za potencjalne utkniêcie
                else if (distanceMoved < (stuckSpeedThreshold * stuckCheckInterval * 0.1f))
                {
                    potentiallyStuck = true;
                }


                if (potentiallyStuck)
                {
                    currentStuckTimer += timeSinceLastStuckCheck;
                }
                else
                {
                    currentStuckTimer = 0f; // Resetuj, jeœli AI siê poruszy³o
                }

                lastPositionForStuckCheck = transform.position;
                timeSinceLastStuckCheck = 0f;

                if (currentStuckTimer >= stuckTimeThreshold)
                {
                    Debug.LogWarning($"[{gameObject.name}] AI wydaje siê byæ zablokowane (licznik: {currentStuckTimer}s)! Teleportowanie do pierwszego waypointu.");
                    ResetAIToFirstWaypoint();
                    return; // Pomiñ resztê Update w tej klatce po resecie
                }
            }
        }
        else
        {
            // Resetuj liczniki wykrywania zablokowania, jeœli AI nie jest w stanie ruchu lub jest zatrzymane
            lastPositionForStuckCheck = transform.position;
            timeSinceLastStuckCheck = 0f;
            currentStuckTimer = 0f;
        }
        // --- KONIEC LOGIKI WYKRYWANIA ZABLOKOWANIA ---


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
            case AIState.InvestigatingLKP:
                HandleInvestigatingLKP(canSeePlayer);
                break;
        }
    }

    void LateUpdate()
    {
        if ((currentState == AIState.Chasing || currentState == AIState.InvestigatingLKP) && player != null)
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
            if (!patrolTargetSet) return; // Nie mo¿na znaleŸæ celu, mo¿e poczekaæ lub zalogowaæ b³¹d
        }

        // SprawdŸ, czy cel zosta³ osi¹gniêty
        // U¿yj agent.stoppingDistance + ma³y bufor dla pewnoœci
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            // Dodatkowe sprawdzenie, czy agent rzeczywiœcie dotar³ (np. prêdkoœæ bliska zeru)
            if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.1f) // Zwiêkszono próg prêdkoœci dla pewnoœci
            {
                // --- LOGIKA TELEPORTACJI WAYPOINTÓW ---
                if (useWaypoints && currentWaypointIndex != -1 && currentWaypointIndex < patrolWaypoints.Count)
                {
                    Transform reachedWaypointTransform = patrolWaypoints[currentWaypointIndex];
                    foreach (var link in teleportLinks)
                    {
                        if (link.fromWaypoint == reachedWaypointTransform && link.toWaypoint != null)
                        {
                            int teleportDestinationIndex = patrolWaypoints.IndexOf(link.toWaypoint);
                            if (teleportDestinationIndex != -1)
                            {
                                Debug.Log($"[{gameObject.name}] Osi¹gniêto waypoint teleportuj¹cy {reachedWaypointTransform.name}, teleportacja do {link.toWaypoint.name}");
                                agent.Warp(link.toWaypoint.position); // Teleportacja NavMeshAgent
                                currentWaypointIndex = teleportDestinationIndex; // Zaktualizuj obecny indeks do celu teleportacji

                                patrolTargetSet = false; // Wymuœ ponowne znalezienie celu z nowej lokalizacji

                                // Zresetuj dostêpne waypointy, aby uwzglêdniæ now¹ pozycjê
                                FillAvailableWaypoints();
                                if (patrolWaypoints.Count > 1 && availableWaypointIndices.Contains(currentWaypointIndex))
                                {
                                    // Usuñ waypoint, do którego w³aœnie siê teleportowano, aby FindNextPatrolTarget wybra³ inny
                                    availableWaypointIndices.Remove(currentWaypointIndex);
                                }
                                return; // WyjdŸ z HandlePatrolling, nastêpny Update wywo³a FindNextPatrolTarget
                            }
                            else
                            {
                                Debug.LogError($"[{gameObject.name}] Cel teleportacji '{link.toWaypoint.name}' (z '{link.fromWaypoint.name}') nie znajduje siê na g³ównej liœcie 'patrolWaypoints'! Teleportacja przerwana. AI bêdzie kontynuowaæ jak przy normalnym waypoincie.", this);
                                // Nie teleportuj, pozwól AI przejœæ do obserwacji lub nastêpnego waypointu normalnie
                            }
                        }
                    }
                }
                // --- KONIEC LOGIKI TELEPORTACJI WAYPOINTÓW ---

                // Brak teleportacji, kontynuuj normalnie
                patrolTargetSet = false;
                if (stopAndLookEnabled)
                {
                    TransitionToState(AIState.Observing);
                }
                // else FindNextPatrolTarget(); // Zostanie wywo³ane na pocz¹tku nastêpnego wywo³ania HandlePatrolling
                return; // WyjdŸ po obs³u¿eniu dotarcia do celu
            }
        }
    }


    // --- OBSERWACJA ---
    void HandleObserving()
    {
        observationTimer -= Time.deltaTime;
        if (observationTimer <= 0f)
        {
            PrepareForPatrol();
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
            transform.rotation = Quaternion.Slerp(transform.rotation, targetObservationRotation, observationAngularSpeed * Time.deltaTime / angleDifference);
        else
            transform.rotation = targetObservationRotation;
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
            investigationOriginPosition = transform.position;
            TransitionToState(AIState.InvestigatingLKP);
        }
    }

    // --- BADANIE LKP ---
    void HandleInvestigatingLKP(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            TransitionToState(AIState.Chasing);
            return;
        }

        if (!reachedLKPInInvestigation)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f)
                {
                    reachedLKPInInvestigation = true;
                    anticipationTimer = anticipationDuration;
                    if (player != null) agent.SetDestination(player.position);
                }
            }
        }
        else
        {
            anticipationTimer -= Time.deltaTime;
            if (player != null) agent.SetDestination(player.position);

            if (anticipationTimer <= 0f)
            {
                TransitionToState(AIState.Observing);
            }
        }
    }

    #endregion

    #region Metody Pomocnicze (Stany i Logika)

    void InitializePatrolMode()
    {
        useWaypoints = patrolWaypoints != null && patrolWaypoints.Count > 0;
        if (useWaypoints)
        {
            patrolWaypoints.RemoveAll(item => item == null);
            if (patrolWaypoints.Count == 0) useWaypoints = false;
            else { FillAvailableWaypoints(); currentWaypointIndex = -1; }
        }
    }

    void FillAvailableWaypoints()
    {
        availableWaypointIndices.Clear();
        availableWaypointIndices.AddRange(Enumerable.Range(0, patrolWaypoints.Count));
    }

    void FindNextPatrolTarget()
    {
        patrolTargetSet = false;
        if (useWaypoints)
        {
            if (availableWaypointIndices.Count == 0)
            {
                FillAvailableWaypoints();
                if (patrolWaypoints.Count > 1 && currentWaypointIndex != -1 && availableWaypointIndices.Contains(currentWaypointIndex))
                    availableWaypointIndices.Remove(currentWaypointIndex); // Nie wybieraj od razu tego samego waypointu
                if (availableWaypointIndices.Count == 0 && patrolWaypoints.Count > 0) // Zabezpieczenie, jeœli currentWaypointIndex by³ jedynym
                    FillAvailableWaypoints();
            }

            if (availableWaypointIndices.Count > 0)
            {
                int randomIndexInAvailableList = Random.Range(0, availableWaypointIndices.Count);
                int nextWaypointIndex = availableWaypointIndices[randomIndexInAvailableList];
                availableWaypointIndices.RemoveAt(randomIndexInAvailableList);
                currentWaypointIndex = nextWaypointIndex;

                if (patrolWaypoints[currentWaypointIndex] != null)
                {
                    currentPatrolTargetPosition = patrolWaypoints[currentWaypointIndex].position;
                    patrolTargetSet = true;
                }
                else { Debug.LogWarning($"[{gameObject.name}] Wybrany waypoint (indeks: {currentWaypointIndex}) jest null. Próba znalezienia innego.", this); return; } // WyjdŸ, aby spróbowaæ ponownie
            }
            else { Debug.LogWarning($"[{gameObject.name}] Brak dostêpnych waypointów do wybrania.", this); return; } // WyjdŸ, jeœli nie ma co wybraæ
        }
        else // Tryb losowego chodzenia
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
                patrolTargetSet = true;
        }

        if (patrolTargetSet)
        {
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(currentPatrolTargetPosition, path) && path.status == NavMeshPathStatus.PathComplete)
                agent.SetDestination(currentPatrolTargetPosition);
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Nie mo¿na obliczyæ œcie¿ki do {currentPatrolTargetPosition}. Cel patrolu nieustawiony.", this);
                patrolTargetSet = false;
                if (useWaypoints && currentWaypointIndex != -1 && !availableWaypointIndices.Contains(currentWaypointIndex))
                    availableWaypointIndices.Add(currentWaypointIndex); // Dodaj z powrotem, jeœli œcie¿ka nieudana
            }
        }
    }

    void PrepareForPatrol()
    {
        Vector3 searchDirection = (lastKnownPlayerPosition - investigationOriginPosition).normalized;
        if (!useWaypoints && searchDirection.sqrMagnitude > 0.1f)
            nextPatrolDirectionBias = searchDirection;
        else
            nextPatrolDirectionBias = null;

        TransitionToState(AIState.Patrolling);
    }

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
        { result = hit.position; return true; }
        else { if (!fullyRandom) return SearchWalkPoint(Vector3.zero, true, out result); }
        result = transform.position; return false;
    }

    void TransitionToState(AIState newState)
    {
        if (currentState == newState && agent.isOnNavMesh && !agent.isStopped) return; // Dodano !agent.isStopped, aby umo¿liwiæ ponowne wejœcie w Observing
        if (currentState == AIState.Observing || currentState == AIState.InvestigatingLKP)
            agent.isStopped = false;

        AIState previousState = currentState;
        currentState = newState;

        if (animator != null)
        {
            animator.SetBool("isPatrolling", false);
            animator.SetBool("isObserving", false);
            animator.SetBool("isChasing", false);
            animator.SetBool("isInvestigating", false);

            switch (newState)
            {
                case AIState.Patrolling: animator.SetBool("isPatrolling", true); break;
                case AIState.Observing: animator.SetBool("isObserving", true); break;
                case AIState.Chasing: animator.SetBool("isChasing", true); break;
                case AIState.InvestigatingLKP: animator.SetBool("isInvestigating", true); break;
            }
        }

        switch (newState)
        {
            case AIState.Patrolling:
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.speed = patrolSpeed;
                agent.angularSpeed = patrolAngularSpeed;
                agent.acceleration = patrolAcceleration;
                if (previousState != AIState.Observing) patrolTargetSet = false;
                break;

            case AIState.Observing:
                agent.isStopped = true;
                agent.updateRotation = true; // Pozwól na obracanie siê podczas obserwacji
                observationTimer = observationDuration;
                nextObservationTurnTime = Time.time + Random.Range(0.1f, 0.5f);
                targetObservationRotation = transform.rotation;
                break;

            case AIState.Chasing:
                agent.isStopped = false;
                agent.updateRotation = false;
                agent.speed = chaseSpeed;
                agent.acceleration = chaseAcceleration;
                nextPatrolDirectionBias = null;
                reachedLKPInInvestigation = false;
                if (player != null) { agent.SetDestination(player.position); lastKnownPlayerPosition = player.position; }
                break;

            case AIState.InvestigatingLKP:
                agent.isStopped = false;
                agent.updateRotation = false;
                agent.speed = chaseSpeed;
                agent.acceleration = chaseAcceleration;
                nextPatrolDirectionBias = null;
                reachedLKPInInvestigation = false;
                anticipationTimer = 0f;

                NavMeshHit hitLKP;
                if (NavMesh.SamplePosition(lastKnownPlayerPosition, out hitLKP, 1.0f, NavMesh.AllAreas))
                {
                    NavMeshPath pathLKP = new NavMeshPath();
                    if (agent.CalculatePath(hitLKP.position, pathLKP) && pathLKP.status == NavMeshPathStatus.PathComplete)
                        agent.SetDestination(hitLKP.position);
                    else
                        TransitionToState(AIState.Observing);
                }
                else
                    TransitionToState(AIState.Observing);
                break;
        }
    }

    bool CheckLineOfSight()
    {
        if (player == null) return false;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > sightRange) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        if (currentState != AIState.Observing && currentState != AIState.InvestigatingLKP)
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

    // NOWA METODA DO RESETOWANIA AI
    void ResetAIToFirstWaypoint()
    {
        if (useWaypoints && patrolWaypoints.Count > 0 && patrolWaypoints[0] != null)
        {
            Transform firstWaypoint = patrolWaypoints[0];
            Debug.Log($"[{gameObject.name}] Resetowanie AI do pierwszego waypointu: {firstWaypoint.name}");

            if (agent.isOnNavMesh)
            {
                agent.Warp(firstWaypoint.position); // Teleportuj agenta
            }
            else
            {
                transform.position = firstWaypoint.position; // Teleportuj transform, jeœli agent nie jest na NavMesh
                Debug.LogWarning($"[{gameObject.name}] Agent nie by³ na NavMesh podczas resetu. Teleportowano transform.");
            }


            // PrzejdŸ do stanu patrolowania. To ustawi parametry agenta (prêdkoœæ itp.)
            // i ustawi patrolTargetSet = false (chyba ¿e poprzedni stan to Observing, co jest ma³o prawdopodobne, jeœli AI utknê³o).
            TransitionToState(AIState.Patrolling);

            // Ustaw pierwszy waypoint jako bie¿¹c¹ lokalizacjê dla logiki patrolu,
            // a FindNextPatrolTarget wybierze *nastêpny*.
            currentWaypointIndex = 0;
            // patrolTargetSet jest ju¿ false z TransitionToState, wiêc FindNextPatrolTarget zostanie uruchomione.

            // Zresetuj dostêpne waypointy dla nowego cyklu patrolu, zaczynaj¹c od waypointu 0.
            FillAvailableWaypoints();
            if (patrolWaypoints.Count > 1 && availableWaypointIndices.Contains(0))
            {
                availableWaypointIndices.Remove(0); // Aby nastêpne FindNextPatrolTarget wybra³o coœ innego.
            }

            // Zresetuj inne istotne zmienne stanu
            reachedLKPInInvestigation = false;
            anticipationTimer = 0f;
            observationTimer = 0f; // Zresetuj równie¿ timer obserwacji
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Próbowano zresetowaæ do pierwszego waypointu, ale brak zdefiniowanych waypointów lub pierwszy jest null. Próba losowego punktu patrolowego.", this);
            patrolTargetSet = false; // Wymuœ znalezienie nowego losowego celu
            TransitionToState(AIState.Patrolling);
        }

        // Zresetuj liczniki wykrywania zablokowania
        currentStuckTimer = 0f;
        timeSinceLastStuckCheck = 0f;
        lastPositionForStuckCheck = transform.position; // Zaktualizuj ostatni¹ pozycjê do nowej, teleportowanej pozycji
        if (agent.isOnNavMesh && agent.isStopped) agent.isStopped = false; // Upewnij siê, ¿e agent mo¿e siê poruszaæ
    }


    #endregion

    #region Gizmos

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
                bool inFov = (currentState == AIState.Observing || currentState == AIState.InvestigatingLKP) || (Vector3.Angle(transform.forward, directionToPlayer) <= fieldOfViewAngle / 2);
                if (inFov)
                {
                    RaycastHit hitInfo;
                    if (Physics.Raycast(rayOrigin, directionForRay, out hitInfo, sightRange, obstacleMask | playerMask))
                    {
                        if (((1 << hitInfo.collider.gameObject.layer) & playerMask) != 0 && hitInfo.transform == player) Gizmos.color = Color.green;
                        else Gizmos.color = Color.red;
                        Gizmos.DrawLine(rayOrigin, hitInfo.point);
                    }
                    else { Gizmos.color = Color.red; Gizmos.DrawLine(rayOrigin, rayOrigin + directionForRay * sightRange); }
                }
            }
        }

        if (patrolTargetSet && (currentState == AIState.Patrolling || currentState == AIState.Observing))
        { Gizmos.color = Color.cyan; Gizmos.DrawSphere(currentPatrolTargetPosition, 0.5f); if (agent != null && agent.hasPath) Gizmos.DrawLine(transform.position, agent.pathEndPosition); }

        if (currentState == AIState.InvestigatingLKP && agent != null)
        {
            Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(lastKnownPlayerPosition, 1.0f);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(investigationOriginPosition, lastKnownPlayerPosition);

            if (reachedLKPInInvestigation)
            {
                Gizmos.color = Color.Lerp(Color.red, Color.magenta, anticipationTimer / anticipationDuration);
                if (player != null) Gizmos.DrawLine(transform.position, player.position);
            }
            else if (agent.hasPath)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, agent.pathEndPosition);
            }
        }
        if (nextPatrolDirectionBias.HasValue && !useWaypoints)
        { Gizmos.color = Color.green; Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, nextPatrolDirectionBias.Value * 5f); }

        if (useWaypoints && patrolWaypoints.Count > 0)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolWaypoints.Count; i++)
            {
                if (patrolWaypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(patrolWaypoints[i].position, 0.5f);
                    // Rysuj linie miêdzy kolejnymi waypointami (opcjonalnie)
                    // int nextIndex = (i + 1) % patrolWaypoints.Count;
                    // if (patrolWaypoints[nextIndex] != null) { Gizmos.color = Color.gray; Gizmos.DrawLine(patrolWaypoints[i].position, patrolWaypoints[nextIndex].position); Gizmos.color = Color.blue; }
                }
            }
            if (currentWaypointIndex >= 0 && currentWaypointIndex < patrolWaypoints.Count && patrolWaypoints[currentWaypointIndex] != null)
            { Gizmos.color = Color.green; Gizmos.DrawSphere(patrolWaypoints[currentWaypointIndex].position, 0.6f); }
        }

        // Gizmos dla linków teleportacji
        Gizmos.color = Color.red;
        foreach (var link in teleportLinks)
        {
            if (link.fromWaypoint != null && link.toWaypoint != null)
            {
                Gizmos.DrawLine(link.fromWaypoint.position + Vector3.up * 0.2f, link.toWaypoint.position + Vector3.up * 0.2f);
                // Strza³ka wskazuj¹ca kierunek teleportacji
                Vector3 direction = (link.toWaypoint.position - link.fromWaypoint.position).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion rotation = Quaternion.LookRotation(direction);
                    Gizmos.DrawRay(link.toWaypoint.position + Vector3.up * 0.2f - direction * 0.5f, rotation * Quaternion.Euler(0, 20, 0) * Vector3.back * 0.3f);
                    Gizmos.DrawRay(link.toWaypoint.position + Vector3.up * 0.2f - direction * 0.5f, rotation * Quaternion.Euler(0, -20, 0) * Vector3.back * 0.3f);
                }
            }
        }
    }
    #endregion
}
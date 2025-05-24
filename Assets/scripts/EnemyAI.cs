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
    public float patrolSpeed = 1.5f;
    public float randomWalkPointRange = 10f;
    public float patrolAngularSpeed = 120f;
    public float patrolAcceleration = 8f;
    public bool stopAndLookEnabled = true; 
    public float observationDuration = 3.0f;
    public float observationAngularSpeed = 90f;
    [Range(10f, 360f)]
    public float patrolBiasConeAngle = 90f;

    [Header("Ustawienia Gonienia i Badania LKP")]
    public float chaseSpeed = 5f;
    public float chaseAcceleration = 40f;
    [Tooltip("Jak d³ugo (w sekundach) AI ma kontynuowaæ ruch w kierunku gracza (nawet przez œciany) po dotarciu do LKP.")]
    public float anticipationDuration = 2.0f;

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
        if (!patrolTargetSet) FindNextPatrolTarget();

        if (patrolTargetSet && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f)
            {
                patrolTargetSet = false;
                if (stopAndLookEnabled) TransitionToState(AIState.Observing);
                else FindNextPatrolTarget();
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
            // Zapisz pozycjê AI, gdy zaczyna badaæ LKP
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
                if (patrolWaypoints.Count > 1 && currentWaypointIndex != -1)
                    availableWaypointIndices.Remove(currentWaypointIndex);
                if (availableWaypointIndices.Count == 0 && patrolWaypoints.Count > 0)
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
                else return;
            }
            else return;
        }
        else
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
                patrolTargetSet = false;
                if (useWaypoints && currentWaypointIndex != -1 && !availableWaypointIndices.Contains(currentWaypointIndex))
                    availableWaypointIndices.Add(currentWaypointIndex);
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
        if (currentState == newState && agent.isOnNavMesh) return;
        if (currentState == AIState.Observing || currentState == AIState.InvestigatingLKP)
            agent.isStopped = false;

        AIState previousState = currentState;
        currentState = newState;

        // --- ZARZ¥DZANIE PARAMETRAMI ANIMATORA ---
        if (animator != null)
        {
            // Najpierw zresetuj wszystkie flagi
            animator.SetBool("isPatrolling", false);
            animator.SetBool("isObserving", false);
            animator.SetBool("isChasing", false);
            animator.SetBool("isInvestigating", false); // Upewnij siê, ¿e nazwa parametru jest poprawna

            // Nastêpnie ustaw flagê dla nowego stanu
            switch (newState)
            {
                case AIState.Patrolling:
                    animator.SetBool("isPatrolling", true);
                    break;
                case AIState.Observing:
                    animator.SetBool("isObserving", true);
                    break;
                case AIState.Chasing:
                    animator.SetBool("isChasing", true);
                    break;
                case AIState.InvestigatingLKP:
                    // Jeœli chcesz, aby InvestigatingLKP u¿ywa³o animacji biegania,
                    // mo¿esz ustawiæ isChasing na true lub stworzyæ dedykowan¹ animacjê/parametr.
                    // Na razie zak³adam, ¿e masz parametr "isInvestigating" i chcesz go u¿yæ.
                    // Jeœli ma to byæ animacja biegania, u¿yj: animator.SetBool("isChasing", true);
                    animator.SetBool("isInvestigating", true); // LUB animator.SetBool("isChasing", true);
                    break;
            }
        }
        // --- KONIEC ZARZ¥DZANIA PARAMETRAMI ANIMATORA ---


        // Logika specyficzna dla przejœcia stanu AI (bez zmian)
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
                agent.updateRotation = true;
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
            // Linia od miejsca, gdzie AI zaczê³o badaæ, do LKP
            Gizmos.color = Color.white;
            Gizmos.DrawLine(investigationOriginPosition, lastKnownPlayerPosition);


            if (reachedLKPInInvestigation) // Jeœli jest w fazie "przeczuwania"
            {
                Gizmos.color = Color.Lerp(Color.red, Color.magenta, anticipationTimer / anticipationDuration);
                if (player != null) Gizmos.DrawLine(transform.position, player.position); // Linia "przeczuwania" do aktualnej pozycji gracza
            }
            else if (agent.hasPath) // Jeœli idzie do LKP
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
                    int nextIndex = (i + 1) % patrolWaypoints.Count;
                    if (patrolWaypoints[nextIndex] != null) { Gizmos.color = Color.gray; Gizmos.DrawLine(patrolWaypoints[i].position, patrolWaypoints[nextIndex].position); Gizmos.color = Color.blue; }
                }
            }
            if (currentWaypointIndex >= 0 && currentWaypointIndex < patrolWaypoints.Count && patrolWaypoints[currentWaypointIndex] != null)
            { Gizmos.color = Color.green; Gizmos.DrawSphere(patrolWaypoints[currentWaypointIndex].position, 0.6f); }
        }
    }
    #endregion
}
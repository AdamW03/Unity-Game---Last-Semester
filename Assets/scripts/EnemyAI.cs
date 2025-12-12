using UnityEngine;
using UnityEngine.AI;
using System.Collections; // Potrzebne dla Coroutine

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Referencje")]
    public Transform player;

    [Header("Ustawienia Widzenia")]
    public float sightRange = 15f;
    public float fieldOfViewAngle = 90f;
    public LayerMask obstacleMask; // Warstwa dla przeszkód (œciany, meble itp.)
    public LayerMask playerMask;   // Warstwa TYLKO dla gracza

    [Header("Ustawienia Patrolowania")]
    public float patrolSpeed = 3f;
    public float walkPointRange = 10f; // Jak daleko AI szuka kolejnego punktu patrolu

    [Header("Ustawienia Gonienia")]
    public float chaseSpeed = 6f;

    [Header("Ustawienia Szukania")]
    public float searchSpeed = 4f; // Prêdkoœæ podczas dochodzenia do miejsca szukania
    public float timeToLosePlayer = 5f; // Czas szukania *po dotarciu* do ostatniej znanej pozycji
    public bool lookAroundWhileSearching = true; // Czy AI ma siê obracaæ w miejscu szukaj¹c?
    public float searchRotationSpeed = 120f; // Prêdkoœæ obrotu podczas szukania (stopnie/sek)
    public float lookAngleSide = 90f; // Jak daleko w bok siê rozejrzy (stopnie)
    public float lookPauseDuration = 0.75f; // Jak d³ugo pauzuje w ka¿dej pozycji rozgl¹dania

    private NavMeshAgent agent;
    private AIState currentState;
    private Vector3 walkPoint;
    private bool walkPointSet;
    private float searchTimer; // Timer odliczaj¹cy czas w stanie Searching
    private Vector3 lastKnownPlayerPosition; // Ostatnia pozycja, gdzie widziano gracza
    private Coroutine lookAroundCoroutine; // Referencja do aktywnej korutyny rozgl¹dania

    // Stany, w jakich mo¿e byæ AI
    private enum AIState
    {
        Patrolling, // Chodzenie po okolicy
        Chasing,    // Gonienie gracza
        Searching   // Szukanie gracza po utracie z oczu
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // ZnajdŸ gracza po tagu
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError("Nie znaleziono obiektu gracza z tagiem 'Player'! AI nie bêdzie dzia³aæ poprawnie.", this);
            enabled = false; // Wy³¹cz ten komponent AI
            return;
        }

        // Ustawienia pocz¹tkowe agenta
        currentState = AIState.Patrolling;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 1.0f; // Pozwól agentowi zatrzymaæ siê trochê przed celem (wa¿ne dla szukania)
    }

    void Update()
    {
        // SprawdŸ, czy AI widzi gracza w tej klatce
        bool canSeePlayer = CheckLineOfSight();

        // Wykonaj akcje zale¿ne od aktualnego stanu
        switch (currentState)
        {
            case AIState.Patrolling:
                HandlePatrolling();
                // Jeœli zobaczysz gracza podczas patrolowania, zacznij goniæ
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

    // --- Obs³uga Stanów ---

    void HandlePatrolling()
    {
        // Ustaw prêdkoœæ patrolowania i upewnij siê, ¿e agent siê porusza
        if (agent.speed != patrolSpeed) agent.speed = patrolSpeed;
        if (agent.isStopped) agent.isStopped = false;

        // Jeœli nie mamy celu patrolu, znajdŸ nowy
        if (!walkPointSet) SearchWalkPoint();

        // Jeœli mamy cel, idŸ do niego
        if (walkPointSet) agent.SetDestination(walkPoint);

        // SprawdŸ, czy dotar³ blisko celu (lub utkn¹³)
        // !agent.pathPending upewnia siê, ¿e agent ma ju¿ obliczon¹ œcie¿kê (nie jest w trakcie)
        if (walkPointSet && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            // Dotar³ - zresetuj flagê, aby w nastêpnej klatce szukaæ nowego celu
            walkPointSet = false;
        }
    }

    void HandleChasing(bool canSeePlayer)
    {
        // Ustaw prêdkoœæ gonienia i upewnij siê, ¿e agent siê porusza
        if (agent.speed != chaseSpeed) agent.speed = chaseSpeed;
        if (agent.isStopped) agent.isStopped = false;

        if (canSeePlayer)
        {
            // Widzimy gracza - aktualizuj cel i ostatni¹ znan¹ pozycjê
            agent.SetDestination(player.position);
            lastKnownPlayerPosition = player.position;
            // Resetuj timer szukania na wszelki wypadek (gdybyœmy wrócili z Searching)
            searchTimer = 0f;
        }
        else
        {
            // Straciliœmy gracza z oczu *w tej klatce*!
            // Natychmiast przejdŸ do szukania w ostatnim znanym miejscu.
            Debug.Log("AI: Straci³em gracza z oczu, przechodzê do szukania!");
            TransitionToState(AIState.Searching);
            // `lastKnownPlayerPosition` ma wartoœæ z poprzedniej klatki, gdy gracz by³ widoczny.
        }
    }

    void HandleSearching(bool canSeePlayer)
    {
        // Jeœli zobaczysz gracza podczas szukania, natychmiast wróæ do gonienia
        if (canSeePlayer)
        {
            TransitionToState(AIState.Chasing);
            return; // Zakoñcz dzia³anie w tej klatce
        }

        // Ustaw prêdkoœæ dochodzenia do miejsca szukania
        if (agent.speed != searchSpeed) agent.speed = searchSpeed;

        // Odliczaj czas szukania *zawsze* gdy jesteœmy w stanie Searching
        searchTimer += Time.deltaTime;
        // Debug.Log($"AI: Szukam, czas: {searchTimer:F1}/{timeToLosePlayer}");

        // SprawdŸ, czy czas na szukanie ju¿ min¹³
        if (searchTimer >= timeToLosePlayer)
        {
            // Czas min¹³, nie znaleziono gracza - wracaj do patrolowania
            Debug.Log("AI: Czas na szukanie min¹³, wracam do patrolowania.");
            TransitionToState(AIState.Patrolling);
            return; // Zakoñcz dzia³anie w tej klatce
        }

        // SprawdŸ, czy dotarliœmy do ostatniej znanej pozycji LUB czy ju¿ tam stoimy
        // Sprawdzamy `hasPath` i `pathPending` aby upewniæ siê, ¿e agent nie jest w trakcie obliczania nowej œcie¿ki
        // i `remainingDistance` aby sprawdziæ czy jest blisko celu.
        bool reachedDestination = !agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance;
        // Lub jeœli nie mamy œcie¿ki (np. cel jest tu¿ pod nami lub nieosi¹galny, lub ju¿ tam stoimy)
        bool noPathOrAlreadyThere = !agent.pathPending && !agent.hasPath;

        if (reachedDestination || noPathOrAlreadyThere)
        {
            // Dotarliœmy na miejsce lub ju¿ tu byliœmy - zatrzymaj agenta (jeœli siê rusza³)
            if (!agent.isStopped)
            {
                agent.velocity = Vector3.zero; // Natychmiast zatrzymaj ruch pêdu
                agent.isStopped = true; // Zatrzymaj ruch NavMeshAgent
                                        // Debug.Log("AI: Dotar³em do miejsca szukania, zatrzymujê siê.");
            }

            // Rozpocznij rozgl¹danie siê (jeœli w³¹czone i jeszcze nie dzia³a)
            if (lookAroundWhileSearching && lookAroundCoroutine == null)
            {
                // Rozpoczynamy rozgl¹danie, ale timer `searchTimer` nadal p³ynie globalnie
                lookAroundCoroutine = StartCoroutine(LookAroundRoutine());
            }
            // Jeœli rozgl¹danie jest wy³¹czone, AI po prostu postoi w miejscu przez `timeToLosePlayer`
        }
        else
        {
            // Jeszcze idziemy do celu - upewnij siê, ¿e agent siê porusza
            if (agent.isStopped)
            {
                agent.isStopped = false;
                // Debug.Log("AI: Wznawiam ruch do miejsca szukania.");
            }
            // Upewnij siê, ¿e cel jest nadal ustawiony (na wypadek problemów z NavMesh)
            if (agent.destination != lastKnownPlayerPosition)
            {
                agent.SetDestination(lastKnownPlayerPosition);
            }
        }
    }


    // --- Przejœcia Miêdzy Stanami ---

    void TransitionToState(AIState newState)
    {
        if (currentState == newState) return; // Ju¿ jesteœmy w tym stanie

        // Czyszczenie przed zmian¹ stanu
        // Zatrzymaj korutynê rozgl¹dania, jeœli by³a aktywna
        if (lookAroundCoroutine != null)
        {
            StopCoroutine(lookAroundCoroutine);
            lookAroundCoroutine = null;
            // Debug.Log("AI: Zatrzymano korutynê rozgl¹dania z powodu zmiany stanu.");
        }
        // Domyœlnie agent ma siê ruszaæ po zmianie stanu (chyba ¿e nowy stan go zatrzyma)
        agent.isStopped = false;

        Debug.Log($"AI zmienia stan z {currentState} na {newState}");
        currentState = newState;

        // Inicjalizacja nowego stanu
        switch (newState)
        {
            case AIState.Patrolling:
                agent.speed = patrolSpeed;
                walkPointSet = false; // Bêdzie szukaæ nowego punktu patrolu
                break;
            case AIState.Chasing:
                agent.speed = chaseSpeed;
                // `lastKnownPlayerPosition` jest aktualizowane w `HandleChasing`
                // `searchTimer` jest resetowany w `HandleChasing`
                Debug.Log("AI: Widzê gracza! Zaczynam poœcig!");
                break;
            case AIState.Searching:
                agent.speed = searchSpeed; // Ustaw prêdkoœæ dochodzenia
                searchTimer = 0f; // Resetuj timer szukania przy wejœciu w ten stan
                agent.SetDestination(lastKnownPlayerPosition); // Ustaw cel na ostatni¹ znan¹ pozycjê
                agent.isStopped = false; // Upewnij siê, ¿e rusza do celu
                Debug.Log($"AI: Idê sprawdziæ ostatni¹ znan¹ pozycjê: {lastKnownPlayerPosition}");
                break;
        }
    }

    // --- Funkcje Pomocnicze ---

    void SearchWalkPoint()
    {
        // ZnajdŸ losowy kierunek
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);
        Vector3 randomDirection = new Vector3(randomX, 0, randomZ);

        // Oblicz potencjalny punkt w pewnej odleg³oœci
        Vector3 potentialPoint = transform.position + randomDirection.normalized * Random.Range(walkPointRange * 0.5f, walkPointRange);

        NavMeshHit hit;
        // Spróbuj znaleŸæ najbli¿szy punkt na NavMesh w rozs¹dnym promieniu od potencjalnego punktu
        if (NavMesh.SamplePosition(potentialPoint, out hit, walkPointRange * 0.5f, NavMesh.AllAreas))
        {
            walkPoint = hit.position;
            walkPointSet = true;
            // Debug.Log($"AI: Nowy punkt patrolu: {walkPoint}");
        }
        else
        {
            // Jeœli nie znaleziono punktu w losowym kierunku, spróbuj znaleŸæ jakikolwiek blisko AI
            if (NavMesh.SamplePosition(transform.position, out hit, walkPointRange, NavMesh.AllAreas))
            {
                // ZnajdŸ losowy punkt w promieniu od aktualnej pozycji AI na NavMesh
                Vector3 randomNavPoint = transform.position + Random.insideUnitSphere * walkPointRange;
                if (NavMesh.SamplePosition(randomNavPoint, out hit, walkPointRange, NavMesh.AllAreas))
                {
                    walkPoint = hit.position;
                    walkPointSet = true;
                    // Debug.Log($"AI: Znalaz³em alternatywny punkt patrolu blisko: {walkPoint}");
                }
                else
                {
                    walkPointSet = false; // Nadal nie znaleziono
                                          // Debug.LogWarning("AI: Nie uda³o siê znaleŸæ losowego punktu na NavMesh w pobli¿u.", this);
                }
            }
            else
            {
                walkPointSet = false; // Nie znaleziono punktu na NavMesh blisko AI
                Debug.LogWarning("AI: Nie uda³o siê znaleŸæ punktu na NavMesh blisko AI.", this);
            }
        }
    }


    bool CheckLineOfSight()
    {
        if (player == null) return false; // Jeœli gracz nie istnieje

        // SprawdŸ dystans
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > sightRange) return false; // Gracz za daleko

        // SprawdŸ k¹t widzenia
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, directionToPlayer) > fieldOfViewAngle / 2) return false; // Gracz poza k¹tem widzenia

        // SprawdŸ przeszkody za pomoc¹ Raycast
        // Pozycja "oczu" AI (trochê poni¿ej szczytu agenta)
        Vector3 rayOrigin = transform.position + Vector3.up * (agent.height * 0.8f);
        // Celuj w œrodek cia³a gracza (mo¿na dostosowaæ)
        Vector3 playerTargetPos = player.position + Vector3.up * 1.0f;
        Vector3 directionForRay = (playerTargetPos - rayOrigin).normalized;

        RaycastHit hit;
        // Wystrzel promieñ sprawdzaj¹cy warstwy przeszkód LUB gracza
        if (Physics.Raycast(rayOrigin, directionForRay, out hit, sightRange, obstacleMask | playerMask))
        {
            // SprawdŸ, czy trafiony obiekt jest na warstwie gracza
            // U¿ywamy operatora przesuniêcia bitowego (1 << layer) i operatora bitowego AND (&)
            if (((1 << hit.collider.gameObject.layer) & playerMask) != 0)
            {
                // Dodatkowo upewnij siê, ¿e trafiony obiekt to faktycznie ten gracz (na wypadek innych obiektów na tej warstwie)
                if (hit.transform == player)
                {
                    // Widzimy gracza!
                    // Debug.DrawLine(rayOrigin, hit.point, Color.green);
                    return true;
                }
            }
            // Trafiono przeszkodê na drodze do gracza
            // Debug.DrawLine(rayOrigin, hit.point, Color.red);
        }
        // else
        // {
        //     // Promieñ nic nie trafi³ w zasiêgu (ale gracz jest w zasiêgu i k¹cie - ma³o prawdopodobne przy poprawnych maskach)
        //     Debug.DrawRay(rayOrigin, directionForRay * sightRange, Color.yellow);
        // }

        // Gracz jest zas³oniêty lub coœ posz³o nie tak
        return false;
    }

    // Coroutine do rozgl¹dania siê w miejscu
    IEnumerator LookAroundRoutine()
    {
        Debug.Log("AI: Rozpoczynam rozgl¹danie siê (w tym za siebie)...");
        Quaternion startRotation = transform.rotation; // Zapamiêtaj kierunek, w którym patrzy³ AI po dotarciu

        // Definiuj sekwencjê obrotów wzglêdem pocz¹tkowego kierunku
        Quaternion lookLeft = startRotation * Quaternion.Euler(0, -lookAngleSide, 0);
        Quaternion lookRight = startRotation * Quaternion.Euler(0, lookAngleSide, 0);
        Quaternion lookBehind = startRotation * Quaternion.Euler(0, 180f, 0);

        // Sekwencja: Lewo -> Pauza -> Prawo -> Pauza -> Ty³ -> Pauza -> Powrót do startu -> Pauza
        Quaternion[] targets = { lookLeft, lookRight, lookBehind, startRotation };
        string[] targetNames = { "w lewo", "w prawo", "za siebie", "do przodu" }; // Dla debugowania

        for (int i = 0; i < targets.Length; i++)
        {
            Quaternion targetRotation = targets[i];
            // Debug.Log($"AI: Obracam siê {targetNames[i]}...");

            // Obrót do celu
            // Sprawdzamy timer *przed* rozpoczêciem obrotu, bo móg³ min¹æ podczas poprzedniej pauzy
            if (searchTimer >= timeToLosePlayer)
            {
                Debug.Log("AI: Czas na szukanie min¹³ przed rozpoczêciem kolejnego obrotu.");
                lookAroundCoroutine = null; // Zresetuj referencjê
                yield break; // Zakoñcz korutynê
            }

            while (Quaternion.Angle(transform.rotation, targetRotation) > 5f) // Obracaj, a¿ k¹t bêdzie ma³y
            {
                // Sprawdzaj czy nie widaæ gracza lub czy czas nie min¹³ *podczas* obrotu
                if (CheckLineOfSight())
                {
                    Debug.Log("AI: Zauwa¿y³em gracza podczas rozgl¹dania!");
                    lookAroundCoroutine = null; // Zresetuj referencjê
                    yield break; // Zakoñcz korutynê (stan zmieni siê w Update)
                }
                if (searchTimer >= timeToLosePlayer)
                {
                    Debug.Log("AI: Czas na szukanie min¹³ podczas obrotu.");
                    lookAroundCoroutine = null; // Zresetuj referencjê
                    yield break; // Zakoñcz korutynê (stan zmieni siê w HandleSearching)
                }

                // P³ynny obrót w kierunku celu
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, searchRotationSpeed * Time.deltaTime);
                yield return null; // Czekaj na nastêpn¹ klatkê
            }

            // Dotarliœmy do celu obrotu - krótka pauza
            // Debug.Log("AI: Pauza w rozgl¹daniu.");
            float pauseEndTime = Time.time + lookPauseDuration;
            while (Time.time < pauseEndTime)
            {
                // Sprawdzaj czy nie widaæ gracza lub czy czas nie min¹³ *podczas* pauzy
                if (CheckLineOfSight())
                {
                    Debug.Log("AI: Zauwa¿y³em gracza podczas pauzy w rozgl¹daniu!");
                    lookAroundCoroutine = null;
                    yield break;
                }
                if (searchTimer >= timeToLosePlayer)
                {
                    Debug.Log("AI: Czas na szukanie min¹³ podczas pauzy w rozgl¹daniu.");
                    lookAroundCoroutine = null;
                    yield break;
                }
                yield return null; // Czekaj na nastêpn¹ klatkê
            }
        }

        Debug.Log("AI: Zakoñczy³em pe³n¹ sekwencjê rozgl¹dania.");
        lookAroundCoroutine = null; // Zresetuj referencjê po normalnym zakoñczeniu
        // Stan na Patrolling zmieni siê w HandleSearching, gdy timer ostatecznie minie (jeœli jeszcze nie min¹³)
    }


    // Rysowanie Gizmos w edytorze dla ³atwiejszego debugowania
    void OnDrawGizmosSelected()
    {
        // Zasiêg widzenia (¿ó³ty okr¹g)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // K¹t widzenia (niebieskie linie)
        Gizmos.color = Color.blue;
        Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfViewAngle / 2, transform.up) * transform.forward * sightRange;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfViewAngle / 2, transform.up) * transform.forward * sightRange;
        Gizmos.DrawLine(transform.position, transform.position + fovLine1);
        Gizmos.DrawLine(transform.position, transform.position + fovLine2);

        // Linia wzroku do gracza (jeœli istnieje)
        if (player != null)
        {
            // U¿yj wysokoœci agenta jeœli dostêpna, inaczej domyœlna wartoœæ
            float eyeHeight = (agent != null ? agent.height * 0.8f : 1.5f);
            Vector3 rayOrigin = transform.position + Vector3.up * eyeHeight;
            Vector3 playerTargetPos = player.position + Vector3.up * 1.0f; // Celuj w œrodek gracza
            Vector3 directionForRay = (playerTargetPos - rayOrigin).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // SprawdŸ warunki widzenia (dystans i k¹t) przed rysowaniem Raycast Gizmo
            if (distanceToPlayer <= sightRange && Vector3.Angle(transform.forward, (player.position - transform.position).normalized) <= fieldOfViewAngle / 2)
            {
                RaycastHit hit;
                // Rysuj liniê Raycast tylko jeœli warunki s¹ spe³nione
                if (Physics.Raycast(rayOrigin, directionForRay, out hit, sightRange, obstacleMask | playerMask))
                {
                    // Jeœli trafiono gracza - zielona linia do punktu trafienia
                    if (((1 << hit.collider.gameObject.layer) & playerMask) != 0 && hit.transform == player)
                        Gizmos.color = Color.green;
                    // Jeœli trafiono przeszkodê - czerwona linia do punktu trafienia
                    else
                        Gizmos.color = Color.red;
                    Gizmos.DrawLine(rayOrigin, hit.point);
                }
                else
                {
                    // Jeœli nic nie trafiono w zasiêgu (a gracz jest w zasiêgu/k¹cie) - ¿ó³ta linia na ca³¹ d³ugoœæ
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(rayOrigin, rayOrigin + directionForRay * sightRange);
                }
            }
        }

        // Punkt patrolu (jeœli ustawiony) - cyjanowa sfera i linia
        if (walkPointSet)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(walkPoint, 0.5f);
            Gizmos.DrawLine(transform.position, walkPoint);
        }

        // Ostatnia znana pozycja gracza (gdy AI szuka) - magenta sfera i linia
        if (currentState == AIState.Searching)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastKnownPlayerPosition, 0.7f);
            // Rysuj liniê do celu agenta (który powinien byæ lastKnownPlayerPosition)
            if (agent != null && agent.hasPath)
            {
                Gizmos.DrawLine(transform.position, agent.pathEndPosition);
            }
            else
            {
                Gizmos.DrawLine(transform.position, lastKnownPlayerPosition); // Jeœli nie ma œcie¿ki, rysuj bezpoœrednio
            }
        }
    }
}
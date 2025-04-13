using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]

public class MovementController : MonoBehaviour
{
    public Transform head;
    public float playerSpeed = 5.0f;
    public float playerAcceleration = 2.0f;
    public float jumpForce = 4.0f;
    public LayerMask groundLayer;

    public float sprintSpeed = 8.0f;
    public float crouchSpeed = 2.0f;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    // Zmienne do zarz¹dzania wysokoœci¹ gracza przy kucaniu
    public float crouchHeightMultiplier = 0.5f; // Jak bardzo zmniejszyæ wysokoœæ (0.5 = o po³owê)
    private float standingColliderHeight;
    private Vector3 standingColliderCenter;
    private float crouchingColliderHeight;
    private Vector3 crouchingColliderCenter;
    private float standingHeadY;
    private float crouchingHeadY;
    private bool isCrouching = false; // Flaga stanu kucania

    // --- Komponenty ---
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    // --- Zmienne Wewnêtrzne ---
    private Vector3 direction;
    private float currentTargetSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // Zapisz oryginalne wymiary kolidera
        standingColliderHeight = capsuleCollider.height;
        standingColliderCenter = capsuleCollider.center;

        // --- ZAPISZ ORYGINALN¥ POZYCJÊ G£OWY ---
        if (head != null) // Sprawdzenie, czy head jest przypisane
        {
            standingHeadY = head.localPosition.y;
        }
        else
        {
            Debug.LogError("Head transform nie jest przypisany w MovementController!", this);
            // Mo¿na ustawiæ domyœln¹ wartoœæ lub wy³¹czyæ komponent
            standingHeadY = standingColliderHeight * 0.85f; // Przyk³adowa wartoœæ domyœlna
        }

        // Oblicz wymiary kolidera podczas kucania
        crouchingColliderHeight = standingColliderHeight * crouchHeightMultiplier;
        // Oblicz nowy œrodek kolidera, aby "dó³" pozosta³ w miejscu
        crouchingColliderCenter = standingColliderCenter - new Vector3(0, (standingColliderHeight - crouchingColliderHeight) / 2.0f, 0);

        float heightDifference = standingColliderHeight - crouchingColliderHeight;
        crouchingHeadY = standingHeadY - heightDifference;

        // Dobra praktyka: zablokuj obrót Rigidbody, aby unikn¹æ przewracania siê
        rb.freezeRotation = true;
    }

    private void Update()
    {
        direction = Input.GetAxisRaw("Horizontal") * head.right + Input.GetAxisRaw("Vertical") * head.forward;

        // --- Sprawdzenie Stanu Kucania i Sprintu ---
        bool wantsToCrouch = Input.GetKey(crouchKey);
        // Mo¿na sprintowaæ tylko gdy nie kucamy
        bool wantsToSprint = Input.GetKey(sprintKey) && !wantsToCrouch && direction.magnitude > 0; // Sprint tylko gdy siê ruszamy

        // --- Ustalenie Docelowej Prêdkoœci Poziomej ---
        if (wantsToCrouch)
        {
            currentTargetSpeed = crouchSpeed;
            isCrouching = true;
        }
        else if (wantsToSprint)
        {
            currentTargetSpeed = sprintSpeed;
            isCrouching = false;
        }
        else
        {
            currentTargetSpeed = playerSpeed;
            isCrouching = false;
        }

        HandleCrouchState();

        Vector3 targetHorizontalVelocity = direction.normalized * currentTargetSpeed;
        Vector3 targetVelocity = targetHorizontalVelocity + Vector3.up * rb.linearVelocity.y;

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, playerAcceleration * Time.deltaTime);


        // old
        //rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, direction.normalized * playerSpeed
        //    + rb.linearVelocity.y * Vector3.up, playerAcceleration * Time.deltaTime);


        // --- Skok ---
        if (Input.GetButtonDown("Jump") && isTouchingGround() && !isCrouching) {
            //rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            // old
            rb.linearVelocity += jumpForce * Vector3.up;
        }
    }
     
    private void HandleCrouchState()
    {
        bool canStand = CanStandUp();

        if (isCrouching)
        {
            // Zmieñ rozmiar i pozycjê kolidera na kucaj¹cy
            capsuleCollider.height = crouchingColliderHeight;
            capsuleCollider.center = crouchingColliderCenter;
            // Opcjonalnie: Jeœli kamera (head) jest dzieckiem, mo¿na j¹ te¿ obni¿yæ
            if (head != null)
            {
                head.localPosition = new Vector3(head.localPosition.x, crouchingHeadY, head.localPosition.z);
            }
        }
        else if (!Input.GetKey(crouchKey) && canStand) // Za³ó¿my, ¿e crouchKey to zmienna przechowuj¹ca klawisz kucania
        {
            // Przywróæ rozmiar i pozycjê kolidera do stanu stoj¹cego
            capsuleCollider.height = standingColliderHeight;
            capsuleCollider.center = standingColliderCenter;

            // Przywróæ pozycjê kamery do oryginalnej wartoœci stoj¹cej
            if (head != null)
            {
                head.localPosition = new Vector3(head.localPosition.x, standingHeadY, head.localPosition.z);
            }
        }
    }

    private bool CanStandUp()
    {
        // Tutaj powinna byæ logika sprawdzaj¹ca, czy jest miejsce nad g³ow¹
        // Na przyk³ad, rzutowanie kapsu³y w górê
        // Physics.CheckCapsule(...)

        // Na razie zwracamy true dla uproszczenia
        return true; // ZAST¥P PRAWDZIW¥ LOGIK¥
    }

    private bool isTouchingGround()
    {
        // Oblicz punkt trochê poni¿ej dolnej czêœci kolidera
        //float checkDistance = 0.1f; // Jak daleko poni¿ej sprawdzaæ
        //// U¿ywamy aktualnego œrodka i wysokoœci kolidera
        //Vector3 checkCenter = transform.position + capsuleCollider.center + Vector3.down * (capsuleCollider.height / 2f - capsuleCollider.radius + checkDistance / 2f);
        //// Rozmiar pude³ka sprawdzaj¹cego - nieco wê¿szy ni¿ promieñ kolidera
        //Vector3 halfExtents = new Vector3(capsuleCollider.radius * 0.9f, checkDistance / 2f, capsuleCollider.radius * 0.9f);

        //// Wykonaj CheckBox
        //return Physics.CheckBox(checkCenter, halfExtents, Quaternion.identity, groundLayer, QueryTriggerInteraction.Ignore);
        // old
         return Physics.CheckBox(transform.position, new Vector3(0.1f, 0.1f, 0.1f), Quaternion.identity, groundLayer);
    }
}

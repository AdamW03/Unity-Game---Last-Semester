using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class FirstPersonMovement : MonoBehaviour
{
    public float speed = 3;

    [Header("Player Step Climb: ")]
    Rigidbody rigidbody;
    [SerializeField] GameObject stepRayUpper;
    [SerializeField] GameObject stepRayLower;
    [SerializeField] float stepHeight = 0.3f;
    [SerializeField] float stepSmooth = 2f;


    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 6;
    public KeyCode runningKey = KeyCode.LeftShift;

    [Header("Custom Gravity")] // Dodajemy nową sekcję
    public float gravityMultiplier = 3f; // Mnożnik grawitacji

    /// <summary> Functions to override movement speed. Will use the last added override. </summary>
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.useGravity = false; // Wyłączamy standardową grawitację Unity
        }
        else
        {
            Debug.LogError("Rigidbody component not found on this GameObject!", this);
        }
        stepRayUpper.transform.position = new Vector3(stepRayUpper.transform.position.x, stepHeight, stepRayUpper.transform.position.z);
    }

    void FixedUpdate()
    {
        // Zastosuj niestandardową grawitację
        ApplyCustomGravity();

        // Update IsRunning from input.
        IsRunning = canRun && Input.GetKey(runningKey);

        // Get targetMovingSpeed.
        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        // Get targetVelocity from input.
        Vector2 targetVelocity = new Vector2(Input.GetAxis("Horizontal") * targetMovingSpeed, Input.GetAxis("Vertical") * targetMovingSpeed);

        // Apply movement.
        if (rigidbody != null)
        {
            // Zachowujemy obecną prędkość pionową (na którą wpływa nasza niestandardowa grawitacja)
            // i ustawiamy prędkości poziome na podstawie inputu.
            Vector3 currentHorizontalVelocity = transform.rotation * new Vector3(targetVelocity.x, 0, targetVelocity.y);
            rigidbody.linearVelocity = new Vector3(currentHorizontalVelocity.x, rigidbody.linearVelocity.y, currentHorizontalVelocity.z);
        }


        stepClimb();
    }

    void ApplyCustomGravity()
    {
        if (rigidbody != null)
        {
            // Physics.gravity to standardowy wektor grawitacji w Unity (zazwyczaj (0, -9.81, 0))
            // Mnożymy go przez nasz modyfikator
            Vector3 customGravity = Physics.gravity * gravityMultiplier;
            rigidbody.AddForce(customGravity, ForceMode.Acceleration);
            // ForceMode.Acceleration sprawia, że siła działa niezależnie od masy obiektu,
            // tak jak prawdziwa grawitacja.
        }
    }

    void stepClimb()
    {
        RaycastHit hitLower;
        if (Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(Vector3.forward), out hitLower, 0.1f))
        {
            RaycastHit hitUpper;
            if (!Physics.Raycast(stepRayUpper.transform.position, transform.TransformDirection(Vector3.forward), out hitUpper, 0.2f))
            {
                rigidbody.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
            }
        }

        RaycastHit hitLower45;
        if (Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(1.5f, 0, 1), out hitLower45, 0.1f))
        {

            RaycastHit hitUpper45;
            if (!Physics.Raycast(stepRayUpper.transform.position, transform.TransformDirection(1.5f, 0, 1), out hitUpper45, 0.2f))
            {
                rigidbody.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
            }
        }

        RaycastHit hitLowerMinus45;
        if (Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(-1.5f, 0, 1), out hitLowerMinus45, 0.1f))
        {

            RaycastHit hitUpperMinus45;
            if (!Physics.Raycast(stepRayUpper.transform.position, transform.TransformDirection(-1.5f, 0, 1), out hitUpperMinus45, 0.2f))
            {
                rigidbody.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
            }
        }
    }
}
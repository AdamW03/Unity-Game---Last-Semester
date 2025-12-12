using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class StarClimb : MonoBehaviour
{

    [Header("Player Step Climb: ")]
    Rigidbody rigidbody;
    [SerializeField] GameObject stepRayUpperFront;
    [SerializeField] GameObject stepRayLowerFront;
    [SerializeField] GameObject stepRayUpperBack;
    [SerializeField] GameObject stepRayLowerBack;
    [SerializeField] GameObject stepRayUpperLeft;
    [SerializeField] GameObject stepRayLowerLeft;
    [SerializeField] GameObject stepRayUpperRight;
    [SerializeField] GameObject stepRayLowerRight;
    [SerializeField] float stepHeight = 0.45f;
    [SerializeField] float stepSmooth = 2f;

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.useGravity = false; 
        }
        else
        {
            Debug.LogError("Rigidbody component not found on this GameObject!", this);
        }
        stepRayUpperFront.transform.position = new Vector3(stepRayUpperFront.transform.position.x, stepHeight, stepRayUpperFront.transform.position.z);
    }

    void FixedUpdate()
    {

        stepClimb();
    }


    void stepClimb()
    {
        RaycastHit hitLower;
        if (Physics.Raycast(stepRayLowerFront.transform.position, transform.TransformDirection(Vector3.forward), out hitLower, 0.1f))
        {
            RaycastHit hitUpper;
            if (!Physics.Raycast(stepRayUpperFront.transform.position, transform.TransformDirection(Vector3.forward), out hitUpper, 0.2f))
            {
                rigidbody.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
            }
        }

        RaycastHit hitLower45;
        if (Physics.Raycast(stepRayLowerFront.transform.position, transform.TransformDirection(1.5f, 0, 1), out hitLower45, 0.1f))
        {

            RaycastHit hitUpper45;
            if (!Physics.Raycast(stepRayUpperFront.transform.position, transform.TransformDirection(1.5f, 0, 1), out hitUpper45, 0.2f))
            {
                rigidbody.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
            }
        }

        RaycastHit hitLowerMinus45;
        if (Physics.Raycast(stepRayLowerFront.transform.position, transform.TransformDirection(-1.5f, 0, 1), out hitLowerMinus45, 0.1f))
        {

            RaycastHit hitUpperMinus45;
            if (!Physics.Raycast(stepRayUpperFront.transform.position, transform.TransformDirection(-1.5f, 0, 1), out hitUpperMinus45, 0.2f))
            {
                rigidbody.position -= new Vector3(0f, -stepSmooth * Time.deltaTime, 0f);
            }
        }

    }
    void OnDrawGizmos()
    {
        // Sprawdzenie, czy obiekty stepRayLower i stepRayUpper s¹ przypisane w Inspektorze,
        // aby unikn¹æ b³êdów NullReferenceException w edytorze.
        if (stepRayLowerFront == null || stepRayUpperFront == null)
        {
            return;
        }

        // D³ugoœci promieni zdefiniowane w stepClimb
        float lowerRayLength = 0.1f;
        float upperRayLength = 0.2f;

        // Pozycje startowe promieni
        Vector3 lowerRayOrigin = stepRayLowerFront.transform.position;
        Vector3 upperRayOrigin = stepRayUpperFront.transform.position;

        // --- 1. Promienie skierowane do przodu ---
        Vector3 forwardDirWorld = transform.TransformDirection(Vector3.forward);

        // Dolny promieñ do przodu
        Gizmos.color = Color.red; // Czerwony dla dolnych promieni
        Gizmos.DrawRay(lowerRayOrigin, forwardDirWorld.normalized * lowerRayLength);

        // Górny promieñ do przodu
        Gizmos.color = Color.blue; // Niebieski dla górnych promieni
        Gizmos.DrawRay(upperRayOrigin, forwardDirWorld.normalized * upperRayLength);

        // --- 2. Promienie skierowane do przodu-prawo (lokalny kierunek: 1.5f, 0, 1f) ---
        Vector3 forwardRightDirLocal = new Vector3(1.5f, 0, 1f);
        Vector3 forwardRightDirWorld = transform.TransformDirection(forwardRightDirLocal);

        // Dolny promieñ do przodu-prawo
        Gizmos.color = Color.red;
        Gizmos.DrawRay(lowerRayOrigin, forwardRightDirWorld.normalized * lowerRayLength);

        // Górny promieñ do przodu-prawo
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(upperRayOrigin, forwardRightDirWorld.normalized * upperRayLength);

        // --- 3. Promienie skierowane do przodu-lewo (lokalny kierunek: -1.5f, 0, 1f) ---
        Vector3 forwardLeftDirLocal = new Vector3(-1.5f, 0, 1f);
        Vector3 forwardLeftDirWorld = transform.TransformDirection(forwardLeftDirLocal);

        // Dolny promieñ do przodu-lewo
        Gizmos.color = Color.red;
        Gizmos.DrawRay(lowerRayOrigin, forwardLeftDirWorld.normalized * lowerRayLength);

        // Górny promieñ do przodu-lewo
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(upperRayOrigin, forwardLeftDirWorld.normalized * upperRayLength);
    }
}
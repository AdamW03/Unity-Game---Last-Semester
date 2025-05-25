using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class FirstPersonMovement : MonoBehaviour
{
    public float speed = 3;

    Rigidbody rigidbodyComponent; // Zmieniona nazwa, żeby uniknąć konfliktu z właściwością Component.rigidbody

    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 5;
    public KeyCode runningKey = KeyCode.LeftShift;

    [Header("Custom Gravity")]
    public float gravityMultiplier = 2f;

    [Header("Movement Control")] // NOWA SEKCJA
    public bool canMove = true;  // Flaga do kontrolowania ruchu

    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Awake()
    {
        rigidbodyComponent = GetComponent<Rigidbody>(); // Używamy zmienionej nazwy
        if (rigidbodyComponent != null)
        {
            rigidbodyComponent.useGravity = false;
        }
        else
        {
            Debug.LogError("Rigidbody component not found on this GameObject!", this);
        }
    }

    void FixedUpdate()
    {
        ApplyCustomGravity();

        // Jeśli ruch jest zablokowany, nie przetwarzaj inputu i zatrzymaj ruch poziomy
        if (!canMove)
        {
            if (rigidbodyComponent != null)
            {
                // Zerujemy tylko prędkość poziomą, grawitacja nadal działa
                rigidbodyComponent.linearVelocity = new Vector3(0, rigidbodyComponent.linearVelocity.y, 0);
            }
            IsRunning = false; // Upewnij się, że nie jest oznaczony jako biegnący
            return; // Zakończ FixedUpdate wcześniej
        }

        IsRunning = canRun && Input.GetKey(runningKey);

        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        Vector2 targetVelocity = new Vector2(Input.GetAxis("Horizontal") * targetMovingSpeed, Input.GetAxis("Vertical") * targetMovingSpeed);

        if (rigidbodyComponent != null)
        {
            Vector3 currentHorizontalVelocity = transform.rotation * new Vector3(targetVelocity.x, 0, targetVelocity.y);
            rigidbodyComponent.linearVelocity = new Vector3(currentHorizontalVelocity.x, rigidbodyComponent.linearVelocity.y, currentHorizontalVelocity.z);
        }
    }

    void ApplyCustomGravity()
    {
        if (rigidbodyComponent != null)
        {
            Vector3 customGravity = Physics.gravity * gravityMultiplier;
            rigidbodyComponent.AddForce(customGravity, ForceMode.Acceleration);
        }
    }

    // NOWA METODA do włączania/wyłączania ruchu z zewnątrz
    public void SetMovementEnabled(bool isEnabled)
    {
        canMove = isEnabled;
        Debug.Log($"FirstPersonMovement: SetMovementEnabled called. canMove is now {canMove}. Rigidbody: {(rigidbodyComponent == null ? "NULL" : "Assigned")}");
        if (!isEnabled && rigidbodyComponent != null)
        {
            rigidbodyComponent.linearVelocity = new Vector3(0, rigidbodyComponent.linearVelocity.y, 0);
            IsRunning = false;
        }
    }
}
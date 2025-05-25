using UnityEngine;

public class FirstPersonLook : MonoBehaviour
{
    [SerializeField]
    Transform character;
    public float sensitivity = 2;
    public float smoothing = 1.5f;

    Vector2 velocity;
    Vector2 frameVelocity;

    [Header("Look Control")] // NOWA SEKCJA
    public bool canLook = true; // Flaga do kontrolowania rozglądania się

    void Reset()
    {
        FirstPersonMovement fpm = GetComponentInParent<FirstPersonMovement>();
        if (fpm != null)
        {
            character = fpm.transform;
        }
        else
        {
            // Jeśli skrypt jest na kamerze, która jest dzieckiem gracza,
            // a gracz nie ma FirstPersonMovement, spróbuj wziąć transform rodzica.
            if (transform.parent != null)
            {
                character = transform.parent;
                Debug.LogWarning("FirstPersonMovement nie znaleziony w rodzicu. Ustawiono 'character' na transform rodzica. Sprawdź, czy to poprawne.", this);
            }
            else
            {
                Debug.LogWarning("FirstPersonMovement nie znaleziony w rodzicu, a obiekt nie ma rodzica. Przypisz 'character' ręcznie w Inspektorze.", this);
            }
        }
    }

    void Start()
    {
        // Cursor.lockState = CursorLockMode.Locked; // Zarządzane przez PauseMenuManager lub inne skrypty
    }

    void Update()
    {
        // Jeśli rozglądanie jest zablokowane przez intro, wyjdź
        if (!canLook)
        {
            return;
        }

        // Sprawdź, czy gra jest spauzowana (zakładając, że PauseMenuManager istnieje i działa)
        // Upewnij się, że masz skrypt PauseMenuManager w scenie i że ma on publiczną statyczną zmienną GameIsPaused.
        // Jeśli nie używasz PauseMenuManager lub ta zmienna nie istnieje, zakomentuj/usuń ten blok.
        if (PauseMenuManager.GameIsPaused) // Ta linia wymaga istnienia PauseMenuManager.GameIsPaused
        {
            // Jeśli kursor nie jest zablokowany (np. menu pauzy jest widoczne), nie obracaj kamery
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }
        }


        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
        velocity += frameVelocity;
        velocity.y = Mathf.Clamp(velocity.y, -90, 90);

        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);

        if (character != null)
        {
            character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
        }
    }

    // NOWA METODA do włączania/wyłączania rozglądania się z zewnątrz
    public void SetLookEnabled(bool isEnabled)
    {
        canLook = isEnabled;
        Debug.Log($"FirstPersonLook: SetLookEnabled called. canLook is now {canLook}");
    }
}
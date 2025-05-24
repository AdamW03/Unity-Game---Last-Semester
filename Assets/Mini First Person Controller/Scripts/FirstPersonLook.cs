using UnityEngine;

public class FirstPersonLook : MonoBehaviour
{
    [SerializeField]
    Transform character;
    public float sensitivity = 2;
    public float smoothing = 1.5f;

    Vector2 velocity;
    Vector2 frameVelocity;

    void Reset()
    {
        // Get the character from the FirstPersonMovement in parents.
        // Upewnij się, że ten komponent istnieje, inaczej będzie błąd przy pierwszym dodaniu skryptu
        FirstPersonMovement fpm = GetComponentInParent<FirstPersonMovement>();
        if (fpm != null)
        {
            character = fpm.transform;
        }
        else
        {
            Debug.LogWarning("FirstPersonMovement nie znaleziony w rodzicu. Przypisz 'character' ręcznie w Inspektorze.", this);
        }
    }

    void Start()
    {
        // Lock the mouse cursor to the game screen.
        // Ta linia jest teraz zarządzana przez PauseMenuManager,
        // ale pozostawienie jej tutaj nie zaszkodzi, PauseMenuManager nadpisze to ustawienie.
        // Możesz ją zakomentować lub usunąć, jeśli chcesz mieć czystszy kod.
        // Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // --- POCZĄTEK MODYFIKACJI ---
        // Sprawdź, czy gra jest spauzowana. Jeśli tak, nie przetwarzaj ruchu myszy dla kamery.
        // Upewnij się, że masz skrypt PauseMenuManager w scenie i że ma on publiczną statyczną zmienną GameIsPaused.
        if (PauseMenuManager.GameIsPaused)
        {
            return; // Wyjdź z metody Update, nic więcej nie rób
        }
        // --- KONIEC MODYFIKACJI ---

        // Get smooth velocity.
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
        velocity += frameVelocity;
        velocity.y = Mathf.Clamp(velocity.y, -90, 90);

        // Rotate camera up-down and controller left-right from velocity.
        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);

        // Upewnij się, że 'character' jest przypisany, aby uniknąć NullReferenceException
        if (character != null)
        {
            character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
        }
    }
}
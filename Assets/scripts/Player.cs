using UnityEngine;

public class Player1 : MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform orientation;

    float xRotation;
    float yRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        //mouse input
        float mouseX = Input.GetAxisRaw("Mause X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mause Y") * Time.deltaTime * sensY;
    }
}

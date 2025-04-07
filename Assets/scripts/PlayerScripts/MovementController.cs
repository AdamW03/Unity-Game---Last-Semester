using UnityEngine;

public class MovementController : MonoBehaviour
{
    public Transform head;
    public float playerSpeed = 5.0f;
    public float playerAcceleration = 2.0f;
    public float jumpForce = 6.0f;
    public LayerMask groundLayer;

    private Rigidbody rb;
    private Vector3 direction;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        direction = Input.GetAxisRaw("Horizontal") * head.right + Input.GetAxisRaw("Vertical") * head.forward;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, direction.normalized * playerAcceleration
            + rb.linearVelocity.y * Vector3.up, playerAcceleration * Time.deltaTime);

        if(Input.GetButtonDown("Jump") && isTouchingGround()) {
            rb.linearVelocity += jumpForce * Vector3.up;
        }
    }

    private bool isTouchingGround()
    {
        return Physics.CheckBox(transform.position, new Vector3(0.1f, 0.1f, 0.1f), Quaternion.identity, groundLayer);
    }
}

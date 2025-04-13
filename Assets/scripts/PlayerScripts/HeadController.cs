using UnityEngine;

public class HeadController : MonoBehaviour
{
    public Transform _camera;
    public Transform hand;
    public float cameraSensitivity = 200.0f;
    public float cameraAcceleration = 5.0f;

    private float rotation_x_axis;
    private float rotation_y_axis;

    void Start()
    {

    }
    
    void Update()
    {
        rotation_x_axis += Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;
        rotation_y_axis += Input.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;

        rotation_x_axis = Mathf.Clamp(rotation_x_axis, -90.0f, 90.0f);

        hand.localRotation = Quaternion.Euler(-rotation_x_axis, rotation_y_axis, 0);

        transform.localRotation = Quaternion.Lerp(transform.localRotation,
            Quaternion.Euler(0, rotation_y_axis, 0), cameraAcceleration * Time.deltaTime);

        _camera.localRotation = Quaternion.Lerp(_camera.localRotation,
            Quaternion.Euler(-rotation_x_axis, 0, 0), cameraAcceleration * Time.deltaTime);
    }
}

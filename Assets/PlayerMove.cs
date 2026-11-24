using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float speed = 4f;
    public float lookSpeed = 2f;

    Rigidbody rb;
    Transform cam;
    float pitch;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        transform.Rotate(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * lookSpeed, -80f, 80f);
        cam.localEulerAngles = new Vector3(pitch, 0, 0);

        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;
    }

    void FixedUpdate()
    {
        Vector3 move = (transform.forward * Input.GetAxis("Vertical")
                      + transform.right * Input.GetAxis("Horizontal")) * speed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }
}
using UnityEngine;

public class MechMovement : MonoBehaviour
{
    public float speed = 5f;
    public float rotationSpeed = 1f;
    public Rigidbody rb;
    public float deadZone = 0.1f; 

    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
    }

    void FixedUpdate()
    {
        float movex = Input.GetAxis("Horizontal");
        float movey = Input.GetAxis("Vertical");
        
        float mouseX = (Input.mousePosition.x - Screen.width / 2f) / (Screen.width / 2f);
        if (Mathf.Abs(mouseX) < deadZone) mouseX = 0f;
        mouseX = Mathf.Clamp(mouseX, -1f, 1f);

        rb.position += new Vector3(movex, 0, movey) * speed * Time.deltaTime;

        float rotation = mouseX * rotationSpeed * Time.deltaTime;
        rb.rotation = Quaternion.Euler(0, rb.rotation.eulerAngles.y + rotation, 0);
    }
}
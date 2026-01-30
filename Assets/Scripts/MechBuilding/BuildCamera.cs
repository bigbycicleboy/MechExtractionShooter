using UnityEngine;

public class BuildCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 10f;
    public float sensitivity = 3f;

    float x, y;
    Quaternion rot;

    void Start()
    {
        target = FindFirstObjectByType<MechBuilder>().transform;
    }

    void Update()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (Input.GetMouseButton(2))
        {
            x += Input.GetAxis("Mouse X") * sensitivity;
            y -= Input.GetAxis("Mouse Y") * sensitivity;
            y = Mathf.Clamp(y, -40, 80);
        }

        rot = Quaternion.Euler(y, x, 0);

        transform.position =
            target.position - rot * Vector3.forward * distance;

        transform.LookAt(target);
    }
}

using UnityEngine;

public class MechCameraController : MonoBehaviour
{
    public float speed;
    public Vector3 offset;
    public Transform target;

    void FixedUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, target.position + offset, speed * Time.deltaTime);
        transform.LookAt(target, Vector3.up);
    }
}
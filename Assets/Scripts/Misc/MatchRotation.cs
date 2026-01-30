using UnityEngine;

public class MatchRotation : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;

    void Update()
    {
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x + offset.x, target.rotation.eulerAngles.y + 180 + offset.y, transform.eulerAngles.z + offset.z);
    }
}
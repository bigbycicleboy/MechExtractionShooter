using UnityEngine;

public class MatchRotation : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, target.rotation.eulerAngles.y, transform.eulerAngles.z);
    }
}
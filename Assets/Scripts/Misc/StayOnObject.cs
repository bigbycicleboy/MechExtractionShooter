using UnityEngine;

public class StayOnObject : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        transform.position = target.position;
        transform.rotation = target.rotation;
    }
}
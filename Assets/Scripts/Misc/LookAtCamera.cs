using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    public bool Lerp;

    void Update()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 directionToCamera = mainCamera.transform.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);

        if (Lerp)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }
}
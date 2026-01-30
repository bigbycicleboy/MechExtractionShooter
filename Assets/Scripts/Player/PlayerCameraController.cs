using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerCameraController : MonoBehaviourPunCallbacks
{
    public float mouseSensitivity = 2f;
    public Transform target;
    public Vector3 offset;

    float xRotation = 0f;
    float yRotation = 0f;

    private int GetLocalPlayerViewID()
    {
        PhotonView[] allPhotonViews = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);
        
        foreach (PhotonView pv in allPhotonViews)
        {
            if (pv.IsMine && pv.GetComponent<PlayerMovement>() != null)
            {
                return pv.ViewID;
            }
        }
        
        return -1;
    }

    void Start()
    {
        target = PhotonView.Find(GetLocalPlayerViewID()).transform;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        yRotation += mouseX;

        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        transform.position = target.position + offset;
    }
}
using UnityEngine;
using Photon.Pun;

public class CameraManager : MonoBehaviourPun
{
    public MonoBehaviour mechCamera;
    public MonoBehaviour playerCamera;

    void Update()
    {
        if (!photonView.IsMine || !PhotonNetwork.IsConnectedAndReady) return;

        if (mechCamera == null)
        {
            mechCamera = FindFirstObjectByType<MechCameraController>();
        }
        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<PlayerCameraController>();
        }
    }

    public void SwitchCamera(string cameraControllerName)
    {
        if (cameraControllerName == "Mech")
        {
            mechCamera.enabled = true;
            playerCamera.enabled = false;
        }
        else if (cameraControllerName == "Player")
        {
            playerCamera.enabled = true;
            mechCamera.enabled = false;
        }
    }
}
using UnityEngine;
using Photon.Pun;

public class CameraManager : MonoBehaviourPunCallbacks
{
    public MonoBehaviour mechCamera;
    public MonoBehaviour playerCamera;

    public override void OnJoinedRoom()
    {
        mechCamera = FindFirstObjectByType<MechCameraController>();
        playerCamera = FindFirstObjectByType<PlayerCameraController>();
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
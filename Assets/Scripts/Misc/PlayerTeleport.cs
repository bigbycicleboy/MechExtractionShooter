using UnityEngine;

public class PlayerTeleport : MonoBehaviour
{
    public GameObject player;
    public Transform teleportTarget;

    public void OnTriggerEnter(Collider other)
    {
        if(other.gameObject == player)
        {
            player.transform.position = teleportTarget.position;
            player.transform.rotation = teleportTarget.rotation;
        }
    }
}
using UnityEngine;
using Photon.Pun;

public class Projectile : MonoBehaviourPun
{
    public float damage = 10f;
    public float lifetime = 5f;
    public GameObject impactEffect;
    public AudioSource explodeSound;

    void Start()
    {
        // Only the owner destroys the projectile to avoid duplicate destruction
        if (photonView.IsMine)
        {
            Destroy(gameObject, lifetime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Only the owner processes collisions to avoid duplicate hits
        if (!photonView.IsMine)
            return;

        // Play sound locally for everyone
        if (explodeSound != null)
        {
            explodeSound.Play();
        }

        // Check if we hit a mech and apply damage via RPC
        PhotonView targetPhotonView = collision.transform.root.GetComponent<PhotonView>();
        if (targetPhotonView != null)
        {
            MechHealth targetHealth = collision.transform.root.GetComponentInChildren<MechHealth>();
            if (targetHealth != null)
            {
                // Call RPC on the target to apply damage
                targetPhotonView.RPC("TakeDamage", RpcTarget.All, damage);
            }
        }

        // Spawn impact effect for everyone
        if (impactEffect != null)
        {
            Vector3 impactPosition = transform.position;
            Quaternion impactRotation = Quaternion.LookRotation(collision.contacts[0].normal);
            PhotonNetwork.Instantiate(impactEffect.name, impactPosition, impactRotation);
        }

        // Disable StayOnObject component if it exists
        StayOnObject stayOnObject = transform.root.GetComponent<StayOnObject>();
        if (stayOnObject != null)
        {
            stayOnObject.enabled = false;
        }
        
        // Destroy the projectile across the network
        PhotonNetwork.Destroy(gameObject);
    }
}
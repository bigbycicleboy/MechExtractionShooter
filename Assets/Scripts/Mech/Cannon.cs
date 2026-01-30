using UnityEngine;
using Photon.Pun;

public class Cannon : MonoBehaviourPun
{
    public PlayerMovement playerMovement;
    public float moveSpeed;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireForce = 700f;
    public float Cooldown;
    public AudioSource firesound;
    float lastFireTime;
    bool isFireable;
    private int currentGunnerID = -1;

    void Update()
    {
        if (!isFireable || currentGunnerID != PhotonNetwork.LocalPlayer.ActorNumber)
            return;

        float movex = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float movey = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;

        movex = Mathf.Clamp(movex, -20f, 20f);
        movey = Mathf.Clamp(movey, -50f, 50f);

        if (Input.GetButtonDown("Fire1") && Time.time > lastFireTime + Cooldown)
        {
            Fire();
        }

        transform.Rotate(0f, movex, -movey);
    }

    void Fire()
    {
        GameObject projectile = PhotonNetwork.Instantiate(
            projectilePrefab.name, 
            firePoint.position, 
            firePoint.rotation
        );
        
        Rigidbody rb = projectile.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(firePoint.forward * fireForce, ForceMode.Force);
        }

        photonView.RPC("PlayFireEffects", RpcTarget.All);
        
        lastFireTime = Time.time;
    }

    [PunRPC]
    void PlayFireEffects()
    {
        if (firesound != null)
        {
            firesound.PlayOneShot(firesound.clip);
        }

        if (currentGunnerID == PhotonNetwork.LocalPlayer.ActorNumber && CameraShake.instance != null)
        {
            CameraShake.instance.ShakeCamera(0.15f, 0.05f);
        }
    }

    public void MakeFireable()
    {
        photonView.RPC("SetGunner", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
        PhotonView.Find(GetLocalPlayerViewID()).GetComponent<PlayerMovement>().MakeUnableToWalk();
        isFireable = true;
    }

    public void MakeUnfireable()
    {
        photonView.RPC("SetGunner", RpcTarget.AllBuffered, -1);
        PhotonView.Find(GetLocalPlayerViewID()).GetComponent<PlayerMovement>().MakeAbleToWalk();
        isFireable = false;
    }

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

    [PunRPC]
    void SetGunner(int playerActorNumber)
    {
        currentGunnerID = playerActorNumber;
    }

    public bool IsLocalPlayerGunner()
    {
        return currentGunnerID == PhotonNetwork.LocalPlayer.ActorNumber;
    }

    public bool IsBeingUsed()
    {
        return currentGunnerID != -1;
    }
}
using Photon.Pun;

public class DestroyAfterTime : MonoBehaviourPun
{
    public float lifetime = 5f;

    void Start()
    {
        if (photonView.IsMine)
        {
            Invoke(nameof(DestroyObject), lifetime);
        }
    }

    void DestroyObject()
    {
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
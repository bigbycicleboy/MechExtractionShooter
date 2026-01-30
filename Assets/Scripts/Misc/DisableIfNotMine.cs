using Photon.Pun;

public class DisableIfNotMine : MonoBehaviourPun
{
    public bool disableIfMineInstead;

    void Start()
    {
        Apply();
    }

    void Apply()
    {
        if (disableIfMineInstead)
        {
            if (photonView.IsMine)
                gameObject.SetActive(false);
        }
        else
        {
            if (!photonView.IsMine)
                gameObject.SetActive(false);
        }
    }
}

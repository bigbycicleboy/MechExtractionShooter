using Photon.Pun;
using TMPro;

public class PlayerName : MonoBehaviourPun
{
    public TMP_InputField nameInput;
    public TextMeshProUGUI playerNameText;

    public void UpdatePlayerName()
    {
        string newName = nameInput.text;


        if (string.IsNullOrWhiteSpace(newName))
            return;

        if (photonView.IsMine)
        {
            PhotonNetwork.NickName = newName;
            playerNameText.text = photonView.Owner.NickName;
        }
    }
}

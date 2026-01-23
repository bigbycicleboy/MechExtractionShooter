using UnityEngine;
using Photon.Pun;

public class PhotonBootStrap : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;
    public GameObject mechPrefab;
    public GameObject cameraPrefab;

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(null, new Photon.Realtime.RoomOptions { MaxPlayers = 4 });
    }

    public override void OnJoinedRoom()
    {
        Vector3 spawnPosition = new Vector3(Random.Range(130, 800), 20, Random.Range(130, 800));
        PhotonNetwork.Instantiate(mechPrefab.name, spawnPosition, Quaternion.identity);
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition + new Vector3(10, 0, 10), Quaternion.identity);
        PhotonNetwork.Instantiate(cameraPrefab.name, Vector3.zero, Quaternion.identity);
    }
}
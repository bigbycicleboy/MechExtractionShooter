using UnityEngine;
using Photon.Pun;

public class PhotonBootStrap : MonoBehaviourPunCallbacks
{
    public Transform mechBuilder;
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
        // Spawn mech at random position
        Vector3 mechSpawnPosition = GetRandomGroundPosition();
        //PhotonNetwork.Instantiate(mechPrefab.name, mechSpawnPosition, Quaternion.identity);
        
        // Spawn player close to the mech
        Vector3 playerSpawnPosition = GetGroundPositionNear(mechBuilder.position);
        PhotonNetwork.Instantiate(playerPrefab.name, playerSpawnPosition, Quaternion.identity);
        
        PhotonNetwork.Instantiate(cameraPrefab.name, Vector3.zero, Quaternion.identity);
    }

    private Vector3 GetRandomGroundPosition()
    {
        Vector3 randomXZ = new Vector3(Random.Range(130, 800), 1000, Random.Range(130, 800));
        RaycastHit hit;
        
        if (Physics.Raycast(randomXZ, Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            return hit.point + Vector3.up * 5f;
        }
        
        return new Vector3(randomXZ.x, 5, randomXZ.z);
    }

    private Vector3 GetGroundPositionNear(Vector3 referencePosition)
    {
        Vector2 randomCircle = Random.insideUnitCircle * 10;
        Vector3 offsetPosition = new Vector3(referencePosition.x + randomCircle.x, 1000, referencePosition.z + randomCircle.y);
        
        RaycastHit hit;
        
        if (Physics.Raycast(offsetPosition, Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            return hit.point + Vector3.up * 5;
        }
        
        return new Vector3(offsetPosition.x, referencePosition.y, offsetPosition.z);
    }
}
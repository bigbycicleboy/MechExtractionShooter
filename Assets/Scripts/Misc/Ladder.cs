using UnityEngine;
using Photon.Pun;

public class Ladder : MonoBehaviour
{
    [Header("Ladder Settings")]
    [SerializeField] private float climbSpeed = 4f;
    [SerializeField] private float snapStrength = 15f;

    [Header("Limits")]
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private Transform topPoint;

    [Header("Exit Timing")]
    [SerializeField] private float exitGraceTime = 0.15f;

    private Rigidbody playerRb;
    private PhotonView playerView;
    private bool playerOnLadder;

    private float ladderEnterTime;
    private float lastVerticalInput;

    // ─────────────────────────────
    // CALLED BY YOUR INTERACTION SYSTEM
    // ─────────────────────────────
    public void EnterLadder()
    {
        if (playerOnLadder)
            return;

        FindLocalPlayer();
        if (playerRb == null)
            return;

        playerOnLadder = true;
        ladderEnterTime = Time.time;

        playerRb.useGravity = false;
        playerRb.linearVelocity = Vector3.zero;
    }

    public void ExitLadder()
    {
        if (!playerOnLadder)
            return;

        playerOnLadder = false;

        playerRb.useGravity = true;
        playerRb = null;
        playerView = null;
    }

    void FixedUpdate()
    {
        if (!playerOnLadder || playerRb == null)
            return;

        // ---- INPUT ----
        float vertical = 0f;
        if (Input.GetKey(KeyCode.W)) vertical = 1f;
        if (Input.GetKey(KeyCode.S)) vertical = -1f;
        lastVerticalInput = vertical;

        // ---- SNAP TO LADDER ----
        Vector3 ladderCenter = new Vector3(
            transform.position.x,
            playerRb.position.y,
            transform.position.z
        );

        Vector3 snapForce = (ladderCenter - playerRb.position) * snapStrength;
        playerRb.AddForce(snapForce, ForceMode.Acceleration);

        // ---- CLIMB ----
        playerRb.linearVelocity = new Vector3(
            0f,
            vertical * climbSpeed,
            0f
        );

        // ---- AUTO EXIT (AFTER GRACE PERIOD) ----
        if (Time.time < ladderEnterTime + exitGraceTime)
            return;

        if (lastVerticalInput > 0f && playerRb.position.y >= topPoint.position.y)
        {
            ForceExit(Vector3.forward);
        }
        else if (lastVerticalInput < 0f && playerRb.position.y <= bottomPoint.position.y)
        {
            ForceExit(Vector3.back);
        }
    }

    void ForceExit(Vector3 pushDirection)
    {
        ExitLadder();

        if (playerRb != null)
        {
            playerRb.AddForce(pushDirection * 2f, ForceMode.VelocityChange);
        }
    }

    void FindLocalPlayer()
    {
        PhotonView[] views = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);
        foreach (PhotonView view in views)
        {
            if (!view.IsMine)
                continue;
            
            if(!view.gameObject.CompareTag("Player"))
                continue;

            Rigidbody rb = view.GetComponent<Rigidbody>();
            if (rb != null)
            {
                playerView = view;
                playerRb = rb;
                return;
            }
        }
    }
}

using UnityEngine;

public class Ladder : MonoBehaviour
{
    [Header("Ladder Settings")]
    [SerializeField] private float climbSpeed = 5f;
    [SerializeField] private bool requireInputToClimb = true;
    [SerializeField] private KeyCode climbKey = KeyCode.W;
    [SerializeField] private KeyCode descendKey = KeyCode.S;
    
    [Header("Optional - Auto-Align")]
    [SerializeField] private bool snapPlayerToLadder = false;
    [SerializeField] private float snapSpeed = 5f;
    
    private PlayerMovement playerInZone;
    private CharacterController playerController;
    private bool isPlayerOnLadder;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Get the PlayerMovement component
            playerInZone = other.GetComponent<PlayerMovement>();
            
            // Get CharacterController from player
            playerController = other.GetComponent<CharacterController>();
            
            if (playerInZone != null)
            {
                isPlayerOnLadder = true;
            }
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && isPlayerOnLadder)
        {
            // Get vertical input
            float verticalInput = 0f;
            
            if (requireInputToClimb)
            {
                // Climb only when pressing up/down keys
                if (Input.GetKey(climbKey))
                {
                    verticalInput = 1f;
                }
                else if (Input.GetKey(descendKey))
                {
                    verticalInput = -1f;
                }
            }
            else
            {
                // Auto-climb when in trigger zone
                verticalInput = 1f;
            }
            
            // Apply climbing movement
            if (verticalInput != 0 && playerController != null)
            {
                Vector3 climbMovement = Vector3.up * verticalInput * climbSpeed * Time.deltaTime;
                playerController.Move(climbMovement);
            }
            
            // Optional: Snap player to center of ladder for better alignment
            if (snapPlayerToLadder)
            {
                Vector3 targetPosition = other.transform.position;
                targetPosition.x = transform.position.x;
                targetPosition.z = transform.position.z;
                
                other.transform.position = Vector3.Lerp(
                    other.transform.position, 
                    targetPosition, 
                    snapSpeed * Time.deltaTime
                );
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = null;
            playerController = null;
            isPlayerOnLadder = false;
        }
    }
    
    // Optional: Public method to check if player is on this ladder
    public bool IsPlayerOnLadder()
    {
        return isPlayerOnLadder;
    }
}
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MechWalker : MonoBehaviourPun
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float rotationSmoothTime = 0.1f;
    [SerializeField] private float rotationSensitivity = 2f;
    
    [Header("Surface Alignment")]
    [SerializeField] private float alignmentSpeed = 5f;
    [SerializeField] private float groundCheckDistance = 2f;
    [SerializeField] private float hoverHeight = 0.5f;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Gravity")]
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float maxFallSpeed = 20f;
    
    [Header("Ground Check Points")]
    [SerializeField] private Transform[] footPoints; // Assign 4 foot positions
    
    [Header("Cursor Settings")]
    [SerializeField] private bool lockCursor = true;
    
    private Vector3 targetNormal = Vector3.up;
    private float currentYawVelocity;
    private float targetYaw;
    private float verticalVelocity = 0f;
    private bool isGrounded = false;

    bool isDrivable;
    private int currentDriverID = -1; // Stores the PhotonView ID of the current driver

    void Start()
    {
        // Only lock cursor if this player is driving
        if (isDrivable && currentDriverID == PhotonNetwork.LocalPlayer.ActorNumber && lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void FixedUpdate()
    {
        CheckIfGrounded();
        
        if (isGrounded)
        {
            AlignToSurface();
        }

        // Only allow input from the current driver
        if (isDrivable && currentDriverID == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            HandleMovement();
            HandleRotation();
        }

        HandleGravity();    
    }

    public void MakeDrivable()
    {
        // Transfer ownership to the player sitting down
        photonView.RPC("SetDriver", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
        PhotonView.Find(GetLocalPlayerViewID()).GetComponent<PlayerMovement>().MakeUnableToWalk();
        FindAnyObjectByType<CameraManager>().SwitchCamera("Mech");
        isDrivable = true;
    }

    public void MakeUndrivable()
    {
        // Clear the driver
        photonView.RPC("SetDriver", RpcTarget.AllBuffered, -1);
        PhotonView.Find(GetLocalPlayerViewID()).GetComponent<PlayerMovement>().MakeAbleToWalk();
        FindAnyObjectByType<CameraManager>().SwitchCamera("Player");
        isDrivable = false;
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
    void SetDriver(int playerActorNumber)
    {
        currentDriverID = playerActorNumber;
        
        // Transfer ownership to the driving player
        if (playerActorNumber != -1)
        {
            PhotonView targetView = PhotonNetwork.CurrentRoom.GetPlayer(playerActorNumber).TagObject as PhotonView;
            if (targetView != null)
            {
                photonView.TransferOwnership(playerActorNumber);
            }
        }
    }
    
    void CheckIfGrounded()
    {
        // Check if mech is on the ground using foot points or center
        if (footPoints != null && footPoints.Length > 0)
        {
            int groundedFeet = 0;
            foreach (Transform foot in footPoints)
            {
                if (foot == null) continue;
                
                RaycastHit hit;
                if (Physics.Raycast(foot.position, Vector3.down, out hit, groundCheckDistance, groundLayer))
                {
                    groundedFeet++;
                    Debug.DrawRay(foot.position, Vector3.down * hit.distance, Color.green);
                }
                else
                {
                    Debug.DrawRay(foot.position, Vector3.down * groundCheckDistance, Color.red);
                }
            }
            
            // Consider grounded if at least 2 feet are touching ground
            isGrounded = groundedFeet >= 2;
        }
        else
        {
            // Fallback to single raycast from center
            RaycastHit hit;
            isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayer);
            
            if (isGrounded)
            {
                Debug.DrawRay(transform.position, Vector3.down * hit.distance, Color.green);
            }
            else
            {
                Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, Color.red);
            }
        }
    }
    
    void HandleGravity()
    {
        Vector3 pos = transform.position;

        if (isGrounded)
        {
            // Stop falling immediately when grounded
            verticalVelocity = 0f;

            // Calculate average ground height from feet
            float heightSum = 0f;
            int hitCount = 0;

            foreach (Transform foot in footPoints)
            {
                if (foot == null) continue;

                if (Physics.Raycast(foot.position, Vector3.down, out RaycastHit hit, groundCheckDistance * 2f, groundLayer))
                {
                    heightSum += hit.point.y;
                    hitCount++;
                }
            }

            if (hitCount > 0)
            {
                float avgGroundY = heightSum / hitCount;
                float desiredY = avgGroundY + hoverHeight;

                // Smoothly move toward hover height (NO snapping)
                pos.y = Mathf.MoveTowards(
                    pos.y,
                    desiredY,
                    6f * Time.fixedDeltaTime
                );
            }
        }
        else
        {
            // Apply gravity while airborne
            verticalVelocity -= gravity * Time.fixedDeltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);

            pos.y += verticalVelocity * Time.fixedDeltaTime;
        }

        transform.position = pos;
    }

    void HandleMovement()
    {
        // Forward movement
        float v = Input.GetAxis("Vertical");
        
        // Clamp to only allow forward movement (0 to 1)
        v = Mathf.Clamp(v, 0, 1);
        
        Vector3 moveDir = Vector3.ProjectOnPlane(transform.forward, targetNormal).normalized;
        transform.position += moveDir * moveSpeed * v * Time.fixedDeltaTime;
    }

    void HandleRotation()
    {
        float h = Input.GetAxis("Horizontal");
        
        // Calculate input-based yaw
        float inputYaw = h * rotationSensitivity;
        inputYaw += h * 0.5f;
        
        // Smooth the yaw input
        targetYaw = Mathf.SmoothDamp(targetYaw, inputYaw * turnSpeed, ref currentYawVelocity, rotationSmoothTime);
        
        // Apply rotation for this frame
        float yawThisFrame = targetYaw * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.AngleAxis(yawThisFrame, transform.up);
        
        transform.rotation = turnRotation * transform.rotation;
    }

    void AlignToSurface()
    {
        // Calculate average ground normal from foot points
        if (footPoints == null || footPoints.Length == 0)
        {
            // Fallback to single raycast if no foot points assigned
            CalculateSingleGroundNormal();
        }
        else
        {
            CalculateAverageGroundNormal();
        }
        
        // Smoothly rotate to align with surface
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, targetNormal) * transform.rotation;
        Quaternion alignedRotation = Quaternion.Slerp(transform.rotation, targetRotation, alignmentSpeed * Time.fixedDeltaTime);
        
        transform.rotation = alignedRotation;
    }

    void CalculateSingleGroundNormal()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            targetNormal = hit.normal;
            Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, Color.green);
        }
        else
        {
            targetNormal = Vector3.up;
            Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, Color.red);
        }
    }

    void CalculateAverageGroundNormal()
    {
        Vector3 normalSum = Vector3.zero;
        int hitCount = 0;
        
        foreach (Transform foot in footPoints)
        {
            if (foot == null) continue;
            
            RaycastHit hit;
            if (Physics.Raycast(foot.position, Vector3.down, out hit, groundCheckDistance, groundLayer))
            {
                normalSum += hit.normal;
                hitCount++;
                
                Debug.DrawRay(foot.position, Vector3.down * hit.distance, Color.green);
            }
            else
            {
                Debug.DrawRay(foot.position, Vector3.down * groundCheckDistance, Color.red);
            }
        }
        
        if (hitCount > 0)
        {
            targetNormal = (normalSum / hitCount).normalized;
        }
        else
        {
            targetNormal = Vector3.up;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize ground check rays
        if (footPoints != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform foot in footPoints)
            {
                if (foot != null)
                {
                    Gizmos.DrawLine(foot.position, foot.position + Vector3.down * groundCheckDistance);
                }
            }
        }
        
        // Visualize target normal
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + targetNormal * 2f);
        
        // Visualize forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3f);
    }
    
    // Public getters for other scripts
    public bool IsMoving()
    {
        if (currentDriverID != PhotonNetwork.LocalPlayer.ActorNumber)
            return false;
            
        return Input.GetAxis("Vertical") > 0.1f;
    }
    
    public float GetCurrentSpeed()
    {
        if (currentDriverID != PhotonNetwork.LocalPlayer.ActorNumber)
            return 0f;
            
        return Input.GetAxis("Vertical") * moveSpeed;
    }
    
    public bool IsGrounded()
    {
        return isGrounded;
    }
    
    public float GetVerticalVelocity()
    {
        return verticalVelocity;
    }

    public bool IsBeingDriven()
    {
        return currentDriverID != -1;
    }

    public bool IsLocalPlayerDriving()
    {
        return currentDriverID == PhotonNetwork.LocalPlayer.ActorNumber;
    }
}
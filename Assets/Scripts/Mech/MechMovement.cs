using UnityEngine;
using Photon.Pun;

public class MechWalker : MonoBehaviourPun
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float turnSpeed = 7f;
    [SerializeField] private float rotationSmoothTime = 0.35f;
    [SerializeField] private float rotationSensitivity = 2f;

    [Header("Surface Alignment")]
    [SerializeField] private float alignmentSpeed = 5f;
    [SerializeField] private float groundCheckDistance = 0.5f;
    [SerializeField] private float groundedGraceTime = 0.12f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float maxSlopeAngle = 40f;

    [Header("Rigidbody Settings")]
    [SerializeField] private float groundDrag = 5f;
    [SerializeField] private float airDrag = 0.5f;
    [SerializeField] private float mechMass = 1000f;

    [Header("Ground Check Points")]
    [SerializeField] private Transform[] footPoints;

    [Header("Platform Settings")]
    [SerializeField] private Transform platformPoint;
    [SerializeField] private float platformCheckRadius = 3f;
    [SerializeField] private float platformRaycastHeight = 5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private bool debugPlatformMovement = false;
    [SerializeField] private bool ignorePlayerCollisions = true;

    [Header("Cursor Settings")]
    [SerializeField] private bool lockCursor = true;

    // Rigidbody component
    private Rigidbody rb;

    // internal state
    private Vector3 smoothedNormal = Vector3.up;
    private float currentYawVelocity;
    private float targetYaw;
    private bool isGrounded = false;
    private float groundedTimer = 0f;
    private bool isDrivable = false;
    private int currentDriverID = -1;
    private bool autoPilotEnabled;
    
    void Start()
    {
        smoothedNormal = transform.up;
        groundedTimer = 0f;
        
        rb = GetComponent<Rigidbody>();
        
        // Configure mech rigidbody
        rb.mass = mechMass;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        if (debugPlatformMovement)
        {
            Debug.Log($"[MechWalker] Player Layer Mask: {playerLayer.value}");
        }
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.L))
        {
            autoPilotEnabled = !autoPilotEnabled;
        }
    }

    void FixedUpdate()
    {
        // 1) Update grounded state first
        CheckIfGrounded();

        // 2) Update drag based on grounded state
        rb.linearDamping = isGrounded ? groundDrag : airDrag;

        // 3) Compute/refresh smoothed normal based on stable raycasts (if grounded)
        if (isGrounded)
        {
            Vector3 groundNormal = CalculateGroundNormal();

            // limit steep slopes
            float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
            if (slopeAngle > maxSlopeAngle)
            {
                groundNormal = Vector3.up;
            }

            // smooth the normal
            smoothedNormal = Vector3.Slerp(smoothedNormal, groundNormal, alignmentSpeed * Time.fixedDeltaTime);
        }
        else
        {
            smoothedNormal = Vector3.Slerp(smoothedNormal, Vector3.up, alignmentSpeed * 0.5f * Time.fixedDeltaTime);
        }

        if (isDrivable && currentDriverID == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            HandleRotationAndAlignment();
        }
        else if (isGrounded)
        {
            AlignToSurfaceOnly();
        }

        if (
            (isDrivable && currentDriverID == PhotonNetwork.LocalPlayer.ActorNumber)
            || autoPilotEnabled
        )
        {
            HandleMovement();
        }
    }

    // -------------------------
    // Driver/ownership helpers
    // -------------------------
    public void MakeDrivable()
    {
        photonView.RPC("SetDriver", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
        PhotonView.Find(GetLocalPlayerViewID()).GetComponent<PlayerMovement>().MakeUnableToWalk();
        FindAnyObjectByType<CameraManager>().SwitchCamera("Mech");
        
        if(autoPilotEnabled)
            return;

        isDrivable = true;
    }

    public void MakeUndrivable()
    {
        FindAnyObjectByType<CameraManager>().SwitchCamera("Player");
        photonView.RPC("SetDriver", RpcTarget.AllBuffered, -1);
        PhotonView.Find(GetLocalPlayerViewID()).GetComponent<PlayerMovement>().MakeAbleToWalk();

        if(autoPilotEnabled)
            return;
        
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

    // -------------------------
    // Grounding / Raycasts
    // -------------------------
    void CheckIfGrounded()
    {
        bool newIsGrounded = false;

        // Use per-foot checks if available for more precise detection
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

            newIsGrounded = groundedFeet >= 2;
            if (newIsGrounded)
            {
                groundedTimer = groundedGraceTime;
            }
            else
            {
                groundedTimer -= Time.fixedDeltaTime;
            }

            isGrounded = groundedTimer > 0f;
        }
        else
        {
            // Fallback to single raycast from center
            RaycastHit hit;
            newIsGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayer);
            
            if (newIsGrounded)
            {
                groundedTimer = groundedGraceTime;
                Debug.DrawRay(transform.position, Vector3.down * hit.distance, Color.green);
            }
            else
            {
                groundedTimer -= Time.fixedDeltaTime;
                Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, Color.red);
            }

            isGrounded = groundedTimer > 0f;
        }
    }

    // -------------------------
    // Movement & Rotation
    // -------------------------
    void HandleMovement()
    {
        float v = Input.GetAxis("Vertical");
        v = Mathf.Clamp(v, 0, 1);

        if(autoPilotEnabled)
            v = 1f;

        if (isGrounded)
        {
            // Move along the current forward (which has been projected onto smoothed normal in rotation)
            Vector3 moveDir = transform.forward;
            Vector3 targetVelocity = moveDir * moveSpeed * v;
            
            // Preserve vertical velocity component
            targetVelocity.y = rb.linearVelocity.y;
            
            // Smoothly interpolate to target velocity
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);
        }
        else
        {
            // Allow some air control but maintain momentum
            Vector3 moveDir = transform.forward;
            Vector3 airControl = moveDir * moveSpeed * v * 0.3f;
            
            Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            Vector3 targetHorizontalVel = currentHorizontalVel + airControl * Time.fixedDeltaTime;
            
            rb.linearVelocity = new Vector3(targetHorizontalVel.x, rb.linearVelocity.y, targetHorizontalVel.z);
        }
    }

    void HandleRotationAndAlignment()
    {
        float h = Input.GetAxis("Horizontal");

        // Calculate desired yaw change
        float inputYaw = h * rotationSensitivity;
        inputYaw += h * 0.5f;

        targetYaw = Mathf.SmoothDamp(targetYaw, inputYaw * turnSpeed, ref currentYawVelocity, rotationSmoothTime);
        float yawDelta = targetYaw * Time.fixedDeltaTime;

        // Get current forward projected onto the surface plane
        Vector3 currentForward = Vector3.ProjectOnPlane(transform.forward, smoothedNormal).normalized;

        if (currentForward.sqrMagnitude < 0.001f)
        {
            currentForward = Vector3.ProjectOnPlane(transform.right, smoothedNormal).normalized;
        }

        // Rotate the forward around the surface normal
        Quaternion yawRotation = Quaternion.AngleAxis(yawDelta, smoothedNormal);
        Vector3 newForward = yawRotation * currentForward;

        // Create target rotation: forward points in new direction, up points along surface normal
        Quaternion targetRotation = Quaternion.LookRotation(newForward, smoothedNormal);

        // Use MoveRotation for physics-based rotation
        rb.MoveRotation(targetRotation);
    }

    void AlignToSurfaceOnly()
    {
        Vector3 currentForward = Vector3.ProjectOnPlane(transform.forward, smoothedNormal).normalized;

        if (currentForward.sqrMagnitude < 0.001f)
        {
            currentForward = Vector3.ProjectOnPlane(transform.right, smoothedNormal).normalized;
        }

        Quaternion targetRotation = Quaternion.LookRotation(currentForward, smoothedNormal);
        Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, alignmentSpeed * Time.fixedDeltaTime);
        
        // Use MoveRotation for physics-based rotation
        rb.MoveRotation(newRotation);
    }

    // -------------------------
    // Ground normal estimator
    // -------------------------
    Vector3 CalculateGroundNormal()
    {
        if (footPoints == null || footPoints.Length < 3)
            return Vector3.up;

        Vector3 sum = Vector3.zero;
        int count = 0;
        float rayLen = groundCheckDistance + 0.2f;

        foreach (Transform foot in footPoints)
        {
            if (foot == null) continue;

            if (Physics.Raycast(foot.position, Vector3.down, out RaycastHit hit, rayLen, groundLayer))
            {
                sum += hit.normal;
                count++;
                Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.cyan);
            }
        }

        if (count == 0)
            return Vector3.up;

        return sum.normalized;
    }

    void OnDrawGizmosSelected()
    {
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

        // Draw platform check area
        Vector3 checkPos = platformPoint != null ? platformPoint.position : transform.position + Vector3.up * 1f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(checkPos, platformCheckRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + smoothedNormal * 3f);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 3f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.right * 1.5f);

#if UNITY_EDITOR
        float angle = Vector3.Angle(smoothedNormal, Vector3.up);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 4f,
            $"Slope Angle: {angle:F1}°\nNormal: {smoothedNormal}\nGrounded: {isGrounded}\nVelocity: {(rb != null ? rb.linearVelocity.magnitude : 0):F1}");
#endif
    }

    // -------------------------
    // Public getters
    // -------------------------
    public bool IsMoving()
    {
        if (currentDriverID != PhotonNetwork.LocalPlayer.ActorNumber)
            return false;

        return Input.GetAxis("Vertical") > 0.1f;
    }

    public float GetCurrentSpeed()
    {
        if (rb == null) return 0f;
        
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        return horizontalVelocity.magnitude;
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    public float GetVerticalVelocity()
    {
        if (rb == null) return 0f;
        return rb.linearVelocity.y;
    }

    public bool IsBeingDriven()
    {
        return currentDriverID != -1;
    }

    public bool IsLocalPlayerDriving()
    {
        return currentDriverID == PhotonNetwork.LocalPlayer.ActorNumber;
    }

    public Vector3 GetSmoothedNormal()
    {
        return smoothedNormal;
    }
    
    public Rigidbody GetRigidbody()
    {
        return rb;
    }
}
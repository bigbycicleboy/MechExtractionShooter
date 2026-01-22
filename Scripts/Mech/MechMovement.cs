using UnityEngine;

public class MechWalker : MonoBehaviour
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

    void Start()
    {
        // Lock and hide cursor for better control
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void FixedUpdate()
    {
        CheckIfGrounded();
        HandleGravity();
        HandleMovement();
        HandleRotation();
        
        // Only align to surface when grounded
        if (isGrounded)
        {
            AlignToSurface();
        }
    }

    public void MakeDrivable()
    {
        isDrivable = true;
    }

    public void MakeUndrivable()
    {
        isDrivable = false;
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
        if (isGrounded)
        {
            // Reset falling velocity when grounded
            verticalVelocity = 0f;
            
            // Keep mech at correct hover height above ground
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance * 2f, groundLayer))
            {
                // Calculate desired Y position
                float desiredY = hit.point.y + hoverHeight;
                float currentY = transform.position.y;
                
                // Smoothly move to desired height
                float newY = Mathf.Lerp(currentY, desiredY, 15f * Time.fixedDeltaTime);
                
                Vector3 newPosition = transform.position;
                newPosition.y = newY;
                transform.position = newPosition;
            }
        }
        else
        {
            // Apply gravity when in air
            verticalVelocity -= gravity * Time.fixedDeltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);
            
            // Move down
            Vector3 newPosition = transform.position;
            newPosition.y += verticalVelocity * Time.fixedDeltaTime;
            transform.position = newPosition;
        }
    }

    void HandleMovement()
    {
        if(!isDrivable) return;

        // Forward movement
        float v = Input.GetAxis("Vertical");
        
        // Clamp to only allow forward movement (0 to 1)
        v = Mathf.Clamp(v, 0, 1);
        
        Vector3 moveDir = transform.forward * v;
        transform.position += moveDir * moveSpeed * Time.fixedDeltaTime;
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
        return Input.GetAxis("Vertical") > 0.1f;
    }
    
    public float GetCurrentSpeed()
    {
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
}
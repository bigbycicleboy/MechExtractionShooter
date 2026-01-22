using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float maxFallSpeed = 20f;
    
    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer = -1;
    
    [Header("Crouch Settings")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CharacterController characterController;
    
    // Movement state
    private Vector3 moveVelocity;
    private float verticalVelocity;
    private bool isGrounded;
    private bool isCrouching;
    private float currentHeight;
    bool ableToWalk = true;
    
    // Input
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool sprintHeld;
    private bool crouchHeld;
    
    void Start()
    {
        // Auto-setup if references not assigned
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
        }
        
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
            
            // If no CharacterController exists, add one
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
                characterController.height = standHeight;
                characterController.radius = 0.5f;
            }
        }
        
        currentHeight = standHeight;
    }
    
    void Update()
    {
        // Gather input
        GatherInput();
        
        // Handle crouching
        HandleCrouch();
        
        // Check if grounded (CharacterController has built-in ground detection)
        CheckGrounded();
        
        // Calculate movement
        CalculateMovement();
        
        // Handle jumping and gravity
        HandleVerticalMovement();
        
        if(!ableToWalk) return;
        
        // Apply final movement
        ApplyMovement();
    }

    public void MakeAbleToWalk()
    {
        ableToWalk = true;
    }

    public void MakeUnableToWalk()
    {
        ableToWalk = false;
    }
    
    void GatherInput()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        
        jumpPressed = Input.GetButtonDown("Jump");
        sprintHeld = Input.GetKey(KeyCode.LeftShift);
        crouchHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
    }
    
    void CheckGrounded()
    {
        // Use CharacterController's built-in ground detection
        isGrounded = characterController.isGrounded;
        
        // Additional raycast check for more reliable detection
        if (!isGrounded)
        {
            Vector3 rayStart = transform.position + characterController.center - new Vector3(0, characterController.height / 2f, 0);
            Debug.DrawRay(rayStart, Vector3.down * groundCheckDistance, Color.red);
            
            isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance, groundLayer);
        }
        else
        {
            Vector3 rayStart = transform.position + characterController.center - new Vector3(0, characterController.height / 2f, 0);
            Debug.DrawRay(rayStart, Vector3.down * groundCheckDistance, Color.green);
        }
    }
    
    void CalculateMovement()
    {
        // Get camera-relative directions (flattened to horizontal plane)
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        // Calculate desired move direction
        Vector3 desiredMoveDirection = (right * moveInput.x + forward * moveInput.y).normalized;
        
        // Determine target speed based on state
        float targetSpeed = walkSpeed;
        
        if (isCrouching)
        {
            targetSpeed = crouchSpeed;
        }
        else if (sprintHeld && moveInput.y > 0) // Only sprint when moving forward
        {
            targetSpeed = sprintSpeed;
        }
        
        // Calculate target velocity
        Vector3 targetVelocity = desiredMoveDirection * targetSpeed;
        
        // Smoothly interpolate to target velocity
        float currentAcceleration = (moveInput.sqrMagnitude > 0) ? acceleration : deceleration;
        moveVelocity = Vector3.Lerp(moveVelocity, targetVelocity, currentAcceleration * Time.deltaTime);
    }
    
    void HandleVerticalMovement()
    {
        if (isGrounded)
        {
            // Reset vertical velocity when grounded
            if (verticalVelocity < 0)
            {
                verticalVelocity = -2f; // Small downward force to stay grounded
            }
            
            // Handle jump
            if (jumpPressed && !isCrouching)
            {
                verticalVelocity = jumpForce;
            }
        }
        else
        {
            // Apply gravity when in air
            verticalVelocity -= gravity * Time.deltaTime;
            
            // Clamp fall speed
            verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);
        }
    }
    
    void HandleCrouch()
    {
        // Toggle or hold crouch logic
        if (crouchHeld)
        {
            isCrouching = true;
        }
        else if (isCrouching)
        {
            // Check if we can stand up (no ceiling above)
            if (CanStandUp())
            {
                isCrouching = false;
            }
        }
        
        // Smoothly transition height
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        
        // Update CharacterController height
        characterController.height = currentHeight;
        
        // Adjust center to keep feet on ground
        Vector3 center = characterController.center;
        center.y = currentHeight / 2f;
        characterController.center = center;
    }
    
    bool CanStandUp()
    {
        // Check for obstacles above
        Vector3 rayStart = transform.position + characterController.center;
        float checkDistance = (standHeight - crouchHeight) / 2f + 0.2f;
        
        Debug.DrawRay(rayStart, Vector3.up * checkDistance, Color.yellow);
        
        return !Physics.Raycast(rayStart, Vector3.up, checkDistance, groundLayer);
    }
    
    void ApplyMovement()
    {
        // Combine horizontal and vertical movement
        Vector3 finalMovement = moveVelocity + Vector3.up * verticalVelocity;
        
        // Use CharacterController.Move for collision detection
        // This automatically handles collisions and works inside moving parents
        characterController.Move(finalMovement * Time.deltaTime);
    }
    
    // Public getters for other scripts
    public bool IsGrounded() => isGrounded;
    public bool IsCrouching() => isCrouching;
    public bool IsSprinting() => sprintHeld && !isCrouching && moveInput.y > 0;
    public Vector3 GetVelocity() => moveVelocity + Vector3.up * verticalVelocity;
    public float GetSpeedPercentage() => moveVelocity.magnitude / sprintSpeed;
}
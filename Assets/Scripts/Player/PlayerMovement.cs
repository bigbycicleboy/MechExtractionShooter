using UnityEngine;
using Photon.Pun;

public class PlayerMovement : MonoBehaviourPun
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float acceleration = 10f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundDrag = 5f;
    [SerializeField] private float airDrag = 0.5f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    
    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private LayerMask groundLayer = -1;
    [SerializeField] private Transform groundCheckPoint;
    
    [Header("Crouch Settings")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    
    [Header("Rigidbody Settings")]
    [SerializeField] private float playerMass = 70f;
    [SerializeField] private float maxSlopeAngle = 45f;
    
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CapsuleCollider capsuleCollider;
    [SerializeField] private AudioSource footstepAudioSource;
    
    // Components
    private Rigidbody rb;
    
    // Movement state
    private bool isGrounded;
    private bool isCrouching;
    private float currentHeight;
    private bool ableToWalk = true;
    private float jumpBufferCounter;
    
    // Input
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool sprintHeld;
    private bool crouchHeld;
    
    // Platform tracking
    private Transform currentPlatform;
    
    void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.mass = playerMass;
        rb.linearDamping = groundDrag;
        
        capsuleCollider = GetComponentInChildren<CapsuleCollider>();
        
        currentHeight = standHeight;
    }

    public void ToggleMouseVisibility(bool visible)
    {
        if (visible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        // Only process input for local player
        if (!photonView.IsMine)
            return;
        
        if(cameraTransform == null)
            cameraTransform = Camera.main.transform;
        
        // Gather input
        GatherInput();
        
        // Handle crouching
        HandleCrouch();

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if(Input.GetKeyDown(KeyCode.F1))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    void FixedUpdate()
    {
        // Only process physics for local player
        if (!photonView.IsMine)
            return;
        
        // Check if grounded
        CheckGrounded();
        
        // Update drag based on grounded state
        rb.linearDamping = isGrounded ? groundDrag : airDrag;
        
        if(!ableToWalk) return;
        
        // Handle movement
        HandleMovement();
        
        jumpBufferCounter -= Time.fixedDeltaTime;

        // Handle jumping
        HandleJump();
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
        
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        sprintHeld = Input.GetKey(KeyCode.LeftShift);
        crouchHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);

        if(sprintHeld)
        {
            footstepAudioSource.pitch = 1.2f;
        }
        else
        {
            footstepAudioSource.pitch = 1f;
        }
        if(footstepAudioSource != null)
        {
            bool isMoving = moveInput.magnitude > 0.1f && isGrounded && ableToWalk;
            if (isMoving && !footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Play();
            }
            else if (!isMoving && footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Pause();
            }
        }
    }
    
    void CheckGrounded()
    {
        // Raycast from ground check point
        Vector3 rayStart = groundCheckPoint.position;
        Debug.DrawRay(rayStart, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
        
        RaycastHit hit;
        isGrounded = Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance, groundLayer);
        
        // Track platform if we're on one
        if (isGrounded && hit.collider != null)
        {
            // Check if we hit a moving platform (has rigidbody)
            Rigidbody platformRb = hit.collider.attachedRigidbody;
            if (platformRb != null && !platformRb.isKinematic)
            {
                currentPlatform = platformRb.transform;
            }
            else
            {
                currentPlatform = null;
            }
        }
        else
        {
            currentPlatform = null;
        }
    }
    
    void HandleMovement()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        Vector3 desiredMoveDirection = (right * moveInput.x + forward * moveInput.y).normalized;
        
        float targetSpeed = walkSpeed;
        
        if (isCrouching)
        {
            targetSpeed = crouchSpeed;
        }
        else if (sprintHeld && moveInput.y > 0)
        {
            targetSpeed = sprintSpeed;
        }

        Vector3 targetVelocity = desiredMoveDirection * targetSpeed;
            
        targetVelocity.y = rb.linearVelocity.y;
            
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
    }
    
    void HandleJump()
    {
        if (jumpBufferCounter > 0f && isGrounded && !isCrouching)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            jumpBufferCounter = 0f;
        }
    }

    void HandleCrouch()
    {
        if (crouchHeld)
        {
            isCrouching = true;
        }
        else if (isCrouching)
        {
            if (CanStandUp())
            {
                isCrouching = false;
            }
        }
        
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        
        capsuleCollider.height = currentHeight;
        capsuleCollider.center = new Vector3(0, currentHeight / 2f - 1, 0);
    }
    
    bool CanStandUp()
    {
        Vector3 rayStart = transform.position + new Vector3(0, currentHeight / 2f, 0);
        float checkDistance = (standHeight - crouchHeight) / 2f + 0.2f;
        
        Debug.DrawRay(rayStart, Vector3.up * checkDistance, Color.yellow);
        
        return !Physics.Raycast(rayStart, Vector3.up, checkDistance, groundLayer);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        Rigidbody otherRb = collision.rigidbody;
        if (otherRb != null && otherRb.mass > playerMass * 5f)
        {
        }
    }

    public bool IsGrounded() => isGrounded;
    public bool IsCrouching() => isCrouching;
    public bool IsSprinting() => sprintHeld && !isCrouching && moveInput.y > 0;
    public Vector3 GetVelocity() => rb != null ? rb.linearVelocity : Vector3.zero;
    public float GetSpeedPercentage() 
    { 
        if (rb == null) return 0f;
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        return horizontalVel.magnitude / sprintSpeed;
    }
    public Rigidbody GetRigidbody() => rb;
    public Transform GetCurrentPlatform() => currentPlatform;
}
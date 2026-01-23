using UnityEngine;
using Photon.Pun;

public class StayOnWalker : MonoBehaviourPun
{
    [Header("Walker Settings")]
    [SerializeField] private Transform walker;
    [SerializeField] private bool autoFindWalker = true;
    
    [Header("Smooth Transition")]
    [SerializeField] private bool smoothParenting = true;
    [SerializeField] private float transitionSpeed = 10f;
    
    private bool isOnWalker = false;
    private CharacterController characterController;
    private Vector3 lastWalkerPosition;
    private Quaternion lastWalkerRotation;
    private bool useManualTracking = false;

    void Start()
    {
        // Get CharacterController component
        characterController = GetComponent<CharacterController>();
        
        // CharacterController can cause issues with parenting, so we'll track manually if needed
        if (characterController != null)
        {
            useManualTracking = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Only process for local player
        if (!photonView.IsMine)
            return;
        
        if (other.CompareTag("Walker"))
        {
            isOnWalker = true;
            
            // Auto-find walker transform if not assigned
            if (walker == null && autoFindWalker)
            {
                walker = other.transform;
            }
            
            // If we don't have a CharacterController, use traditional parenting
            if (!useManualTracking)
            {
                transform.parent = walker;
            }
            else
            {
                // Store initial walker position/rotation for tracking
                if (walker != null)
                {
                    lastWalkerPosition = walker.position;
                    lastWalkerRotation = walker.rotation;
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Only process for local player
        if (!photonView.IsMine)
            return;
        
        if (other.CompareTag("Walker"))
        {
            isOnWalker = false;
            
            if (!useManualTracking)
            {
                transform.parent = null;
            }
        }
    }

    void FixedUpdate()
    {
        // Only process for local player
        if (!photonView.IsMine)
            return;
        
        // Manual tracking for CharacterController (since parenting doesn't work well with it)
        if (isOnWalker && useManualTracking && walker != null)
        {
            // Calculate walker movement delta
            Vector3 walkerMovement = walker.position - lastWalkerPosition;
            
            // Calculate walker rotation delta
            Quaternion walkerRotationDelta = walker.rotation * Quaternion.Inverse(lastWalkerRotation);
            
            // Apply movement instantly (no smoothing needed, causes lag)
            characterController.Move(walkerMovement);
            
            // Apply rotation tracking
            Vector3 pivotOffset = transform.position - walker.position;
            Vector3 newPivotOffset = walkerRotationDelta * pivotOffset;
            Vector3 rotationMovement = newPivotOffset - pivotOffset;
            
            characterController.Move(rotationMovement);
            transform.rotation = walkerRotationDelta * transform.rotation;
            
            // Update last known walker transform
            lastWalkerPosition = walker.position;
            lastWalkerRotation = walker.rotation;
        }
    }
    
    void Update()
    {
        // Only process for local player
        if (!photonView.IsMine)
            return;
        
        // Update walker transform every frame for smoother tracking
        if (isOnWalker && walker != null && useManualTracking)
        {
            // Store current walker state
            lastWalkerPosition = walker.position;
            lastWalkerRotation = walker.rotation;
        }
    }

    // Public methods for other scripts
    public bool IsOnWalker()
    {
        return isOnWalker;
    }

    public Transform GetWalker()
    {
        return walker;
    }

    public void SetWalker(Transform newWalker)
    {
        walker = newWalker;
        if (walker != null && isOnWalker)
        {
            lastWalkerPosition = walker.position;
            lastWalkerRotation = walker.rotation;
        }
    }
}
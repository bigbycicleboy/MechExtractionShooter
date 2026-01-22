using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float checkRadius = 3f;
    [SerializeField] private LayerMask interactableLayer = -1;
    
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("UI")]
    [SerializeField] private InteractionUI interactionUI;
    
    private Interactable currentInteractable;
    
    void Start()
    {
        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
        }
        
        // Auto-find UI if not assigned
        if (interactionUI == null)
        {
            interactionUI = FindFirstObjectByType<InteractionUI>();
        }
    }
    
    void Update()
    {
        CheckForInteractable();
        HandleInput();
    }
    
    void CheckForInteractable()
    {
        Interactable closestInteractable = null;
        float closestDistance = float.MaxValue;
        
        // Find all interactables in range
        Collider[] colliders = Physics.OverlapSphere(transform.position, checkRadius, interactableLayer);
        
        foreach (Collider col in colliders)
        {
            Interactable interactable = col.GetComponent<Interactable>();
            
            if (interactable != null && interactable.CanInteract())
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                
                // Check if within interactable's range
                if (distance <= interactable.GetRange() && distance < closestDistance)
                {
                    // Optional: Check if player is looking at it
                    if (IsLookingAt(col.transform))
                    {
                        closestDistance = distance;
                        closestInteractable = interactable;
                    }
                }
            }
        }
        
        // Update current interactable
        if (closestInteractable != currentInteractable)
        {
            currentInteractable = closestInteractable;
            UpdateUI();
        }
    }
    
    bool IsLookingAt(Transform target)
    {
        if (cameraTransform == null) return true;
        
        Vector3 directionToTarget = (target.position - cameraTransform.position).normalized;
        float dotProduct = Vector3.Dot(cameraTransform.forward, directionToTarget);
        
        // Return true if target is roughly in front of camera (within 60 degrees)
        return dotProduct > 0.5f;
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(interactKey) && currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }
    
    void UpdateUI()
    {
        if (interactionUI != null)
        {
            if (currentInteractable != null)
            {
                interactionUI.Show(currentInteractable.GetPrompt());
            }
            else
            {
                interactionUI.Hide();
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Visualize interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}
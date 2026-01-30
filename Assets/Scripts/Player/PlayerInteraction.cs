using UnityEngine;
using Photon.Pun;

public class PlayerInteraction : MonoBehaviourPun
{
    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float checkRadius = 3f;
    [SerializeField] private LayerMask interactableLayer = -1;
    [SerializeField] int maxInteractables = 16;
    [SerializeField] float checkInterval = 0.1f;
    
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("UI")]
    [SerializeField] private InteractionUI interactionUI;
    
    private Interactable currentInteractable;
    Collider[] overlapResults;
    float checkTimer;
    
    void Start()
    {
        // Only setup for local player
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }
        
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

        overlapResults = new Collider[maxInteractables];
    }
    
    void Update()
    {
        if (!photonView.IsMine)
            return;

        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = checkInterval;
            CheckForInteractable();
        }

        HandleInput();
    }
    
    void CheckForInteractable()
    {
        Interactable closestInteractable = null;
        float closestSqrDistance = float.MaxValue;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            checkRadius,
            overlapResults,
            interactableLayer
        );

        Vector3 playerPos = transform.position;
        Vector3 camForward = cameraTransform ? cameraTransform.forward : Vector3.forward;
        Vector3 camPos = cameraTransform ? cameraTransform.position : playerPos;

        for (int i = 0; i < count; i++)
        {
            Collider col = overlapResults[i];
            if (col == null) continue;

            // TryGetComponent is faster & GC-free
            if (!col.TryGetComponent(out Interactable interactable))
                continue;

            if (!interactable.CanInteract())
                continue;

            Vector3 toTarget = col.transform.position - playerPos;
            float sqrDist = toTarget.sqrMagnitude;

            if (sqrDist > interactable.GetRange() * interactable.GetRange())
                continue;

            // Looking check (cheap dot, no normalize)
            if (cameraTransform)
            {
                Vector3 toCamTarget = col.transform.position - camPos;
                float dot = Vector3.Dot(camForward, toCamTarget.normalized);
                if (dot < 0.5f)
                    continue;
            }

            if (sqrDist < closestSqrDistance)
            {
                closestSqrDistance = sqrDist;
                closestInteractable = interactable;
            }
        }

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
    
    // Public method to get current interactable (for other scripts)
    public Interactable GetCurrentInteractable()
    {
        return currentInteractable;
    }
    
    // Public method to force clear current interactable
    public void ClearInteractable()
    {
        currentInteractable = null;
        UpdateUI();
    }
}
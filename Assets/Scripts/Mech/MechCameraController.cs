using UnityEngine;
using Photon.Pun;

public class MechCameraController : MonoBehaviourPun
{
    [Header("Follow Settings")]
    public float speed = 5f;
    public Vector3 offset = new Vector3(0f, 2.68f, -3.33f);
    public Transform target;
    
    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public float verticalLookLimit = 30f;
    public float horizontalLookLimit = 60f;
    
    [Header("Auto-Find Settings")]
    [SerializeField] private bool autoFindMech = true;
    [SerializeField] private float searchInterval = 0.5f; // How often to search for mech

    private float xRotation = 0f;
    private float yRotation = 0f;
    private float lastSearchTime = 0f;
    private MechWalker currentMech;

    void Start()
    {
        // Only run for local player
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }
    }

    void Update()
    {
        // Only process for local player
        if (!photonView.IsMine)
            return;
        
        // Auto-find mech if enabled and no target set
        if (autoFindMech && (target == null || currentMech == null))
        {
            if (Time.time - lastSearchTime > searchInterval)
            {
                FindDrivenMech();
                lastSearchTime = Time.time;
            }
        }
        
        // If no target, don't update camera
        if (target == null)
            return;

        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Update rotation values
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalLookLimit, verticalLookLimit);
        
        yRotation += mouseX;
        yRotation = Mathf.Clamp(yRotation, -horizontalLookLimit, horizontalLookLimit);

        // Calculate camera position behind mech (relative to mech's rotation)
        Vector3 rotatedOffset = target.rotation * offset;
        Vector3 desiredPosition = target.position + rotatedOffset;
        
        // Smoothly move to position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, speed * Time.deltaTime);
        
        // Calculate rotation: mech's forward + camera look offset
        Quaternion targetRotation = target.rotation * Quaternion.Euler(xRotation, yRotation, 0f);
        transform.rotation = targetRotation;
    }
    
    void FindDrivenMech()
    {
        // Find all MechWalker instances in the scene
        MechWalker[] allMechs = FindObjectsByType<MechWalker>(FindObjectsSortMode.None);
        
        foreach (MechWalker mech in allMechs)
        {
            // Check if this is the mech the local player is driving
            if (mech.IsLocalPlayerDriving())
            {
                currentMech = mech;
                target = mech.transform;
                Debug.Log($"MechCameraController: Found and targeting mech - {mech.name}");
                return;
            }
        }
        
        // If no mech found, clear target
        if (currentMech != null && !currentMech.IsLocalPlayerDriving())
        {
            currentMech = null;
            target = null;
            ResetCameraRotation();
            Debug.Log("MechCameraController: Player exited mech, clearing target");
        }
    }
    
    // Public method to manually set the target mech
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        currentMech = newTarget?.GetComponent<MechWalker>();
        ResetCameraRotation();
    }
    
    // Public method to clear the target
    public void ClearTarget()
    {
        target = null;
        currentMech = null;
        ResetCameraRotation();
    }
    
    // Reset camera look rotation when switching targets
    void ResetCameraRotation()
    {
        xRotation = 0f;
        yRotation = 0f;
    }
    
    // Public getters
    public bool HasTarget()
    {
        return target != null;
    }
    
    public Transform GetTarget()
    {
        return target;
    }
}
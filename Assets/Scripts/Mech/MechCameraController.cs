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
    [SerializeField] private float searchInterval = 0.5f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private float lastSearchTime = 0f;
    private MechWalker currentMech;

    void Start()
    {
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (!photonView.IsMine)
            return;
        
        if (autoFindMech && (target == null || currentMech == null))
        {
            if (Time.time - lastSearchTime > searchInterval)
            {
                FindDrivenMech();
                lastSearchTime = Time.time;
            }
        }
        
        if (target == null)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalLookLimit, verticalLookLimit);
        
        yRotation += mouseX;
        yRotation = Mathf.Clamp(yRotation, -horizontalLookLimit, horizontalLookLimit);

        Vector3 rotatedOffset = target.rotation * offset;
        Vector3 desiredPosition = target.position + rotatedOffset;
        
        transform.position = Vector3.Lerp(transform.position, desiredPosition, speed * Time.deltaTime);
        
        Quaternion targetRotation = target.rotation * Quaternion.Euler(xRotation, yRotation, 0f);
        transform.rotation = targetRotation;
    }
    
    void FindDrivenMech()
    {
        MechWalker[] allMechs = FindObjectsByType<MechWalker>(FindObjectsSortMode.None);
        
        foreach (MechWalker mech in allMechs)
        {
            if (mech.IsLocalPlayerDriving())
            {
                currentMech = mech;
                target = mech.transform;
                Debug.Log($"Found and targeting mech {mech.name}");
                return;
            }
        }
        
        if (currentMech != null && !currentMech.IsLocalPlayerDriving())
        {
            currentMech = null;
            target = null;
            ResetCameraRotation();
            Debug.Log("Player exited mech clearing target");
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        currentMech = newTarget?.GetComponent<MechWalker>();
        ResetCameraRotation();
    }
    
    public void ClearTarget()
    {
        target = null;
        currentMech = null;
        ResetCameraRotation();
    }
    
    void ResetCameraRotation()
    {
        xRotation = 0f;
        yRotation = 0f;
    }
    
    public bool HasTarget()
    {
        return target != null;
    }
    
    public Transform GetTarget()
    {
        return target;
    }
}
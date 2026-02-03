using UnityEngine;

public class MenuCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How far the camera can move from its original position")]
    public float movementRange = 2f;
    
    [Tooltip("How smoothly the camera follows the mouse (lower = smoother)")]
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.1f;
    
    [Header("Optional Settings")]
    [Tooltip("Invert horizontal movement")]
    public bool invertX = false;
    
    [Tooltip("Invert vertical movement")]
    public bool invertY = false;
    
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    
    void Start()
    {
        // Store the camera's starting position
        originalPosition = transform.position;
        targetPosition = originalPosition;
    }
    
    void Update()
    {
        // Get mouse position normalized to -1 to 1 range
        Vector2 mousePos = GetNormalizedMousePosition();
        
        // Apply inversion if needed
        if (invertX) mousePos.x = -mousePos.x;
        if (invertY) mousePos.y = -mousePos.y;
        
        // Calculate target position based on mouse
        targetPosition = originalPosition + new Vector3(
            mousePos.x * movementRange,
            mousePos.y * movementRange,
            0f
        );
        
        // Smoothly move camera towards target position
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed
        );
    }
    
    /// <summary>
    /// Returns mouse position normalized to -1 to 1 range
    /// (0, 0) is center of screen
    /// </summary>
    private Vector2 GetNormalizedMousePosition()
    {
        Vector2 mousePos = Input.mousePosition;
        
        // Normalize to -1 to 1 range
        float normalizedX = (mousePos.x / Screen.width) * 2f - 1f;
        float normalizedY = (mousePos.y / Screen.height) * 2f - 1f;
        
        return new Vector2(normalizedX, normalizedY);
    }
    
    /// <summary>
    /// Optional: Call this to reset camera to original position
    /// </summary>
    public void ResetPosition()
    {
        targetPosition = originalPosition;
    }
}
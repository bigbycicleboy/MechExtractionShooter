using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private string interactionPrompt = "Press E to interact";
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private bool canInteractMultipleTimes = true;
    [SerializeField] private float cooldownTime = 1f;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onInteract;
    
    private bool hasInteracted = false;
    private float lastInteractionTime = -999f;
    
    // Call this when player interacts
    public void Interact()
    {
        // Check cooldown
        if (Time.time - lastInteractionTime < cooldownTime)
        {
            return;
        }
        
        // Check if can interact again
        if (!canInteractMultipleTimes && hasInteracted)
        {
            return;
        }
        
        // Trigger the interaction event
        onInteract?.Invoke();
        
        hasInteracted = true;
        lastInteractionTime = Time.time;
        
        Debug.Log($"Interacted with {gameObject.name}");
    }
    
    // Public getters
    public string GetPrompt() => interactionPrompt;
    public float GetRange() => interactionRange;
    public bool CanInteract() 
    {
        if (!canInteractMultipleTimes && hasInteracted) return false;
        if (Time.time - lastInteractionTime < cooldownTime) return false;
        return true;
    }
    
    // Optional: Reset interaction state
    public void ResetInteraction()
    {
        hasInteracted = false;
    }
    
    // Optional: Method examples that can be called from Unity Events
    public void ExampleOpenDoor()
    {
        Debug.Log("Door opened!");
        // Add your door opening code here
    }
    
    public void ExampleCollectItem()
    {
        Debug.Log("Item collected!");
        // Add your item collection code here
        // gameObject.SetActive(false); // Hide the object
    }
    
    public void ExamplePlaySound()
    {
        Debug.Log("Playing sound!");
        // GetComponent<AudioSource>()?.Play();
    }
}
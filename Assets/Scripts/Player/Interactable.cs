using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;

public class Interactable : MonoBehaviourPun
{
    [Header("Interaction Settings")]
    [SerializeField] private string interactionPrompt = "Press E to interact";
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private bool canInteractMultipleTimes = true;
    [SerializeField] private float cooldownTime = 1f;
    [SerializeField] private bool useToggleMode = false;
    [SerializeField] private string togglePromptOff = "Press E to sit";
    [SerializeField] private string togglePromptOn = "Press E to stand";
    
    [Header("Network Settings")]
    [SerializeField] private bool syncInteraction = true;
    [SerializeField] private bool singleUserOnly = false; // Only one player can use at a time
    
    [Header("Events")]
    [SerializeField] private UnityEvent onInteract;
    [SerializeField] private UnityEvent onToggleOn;
    [SerializeField] private UnityEvent onToggleOff;

    private bool hasInteracted = false;
    private float lastInteractionTime = -999f;
    private bool isToggled = false;
    private int currentUserID = -1; // Tracks who is currently using (for seats, etc.)
    
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
        
        // Check if someone else is using it (for single user mode)
        if (singleUserOnly && currentUserID != -1 && currentUserID != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Debug.Log("Someone else is using this!");
            return;
        }
        
        // Send interaction across network if enabled
        if (syncInteraction)
        {
            photonView.RPC("RPC_Interact", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            // Local only interaction
            ProcessInteraction(PhotonNetwork.LocalPlayer.ActorNumber);
        }
        
        lastInteractionTime = Time.time;
    }
    
    [PunRPC]
    void RPC_Interact(int playerActorNumber)
    {
        ProcessInteraction(playerActorNumber);
    }
    
    void ProcessInteraction(int playerActorNumber)
    {
        // Handle toggle mode
        if (useToggleMode)
        {
            // If single user mode, track who's using it
            if (singleUserOnly)
            {
                if (currentUserID == playerActorNumber)
                {
                    // Same player toggling off
                    isToggled = false;
                    currentUserID = -1;
                    onToggleOff?.Invoke();
                    Debug.Log($"Player {playerActorNumber} toggled OFF: {gameObject.name}");
                }
                else if (currentUserID == -1)
                {
                    // No one using it, toggle on
                    isToggled = true;
                    currentUserID = playerActorNumber;
                    onToggleOn?.Invoke();
                    Debug.Log($"Player {playerActorNumber} toggled ON: {gameObject.name}");
                }
                // else: someone else is using it, do nothing
            }
            else
            {
                // Regular toggle without user tracking
                isToggled = !isToggled;
                if (isToggled)
                {
                    onToggleOn?.Invoke();
                    Debug.Log($"Toggled ON: {gameObject.name}");
                }
                else
                {
                    onToggleOff?.Invoke();
                    Debug.Log($"Toggled OFF: {gameObject.name}");
                }
            }
        }
        else
        {
            // Regular interaction mode
            onInteract?.Invoke();
            Debug.Log($"Player {playerActorNumber} interacted with {gameObject.name}");
        }
        
        hasInteracted = true;
    }
    
    // Public getters
    public string GetPrompt() 
    {
        if (useToggleMode)
        {
            // If single user mode and someone else is using it
            if (singleUserOnly && currentUserID != -1 && currentUserID != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                return "In use by another player";
            }
            
            return isToggled && currentUserID == PhotonNetwork.LocalPlayer.ActorNumber ? togglePromptOn : togglePromptOff;
        }
        return interactionPrompt;
    }
    
    public float GetRange() => interactionRange;
    
    public bool CanInteract() 
    {
        if (!canInteractMultipleTimes && hasInteracted) return false;
        if (Time.time - lastInteractionTime < cooldownTime) return false;
        
        // If single user mode and someone else is using it
        if (singleUserOnly && currentUserID != -1 && currentUserID != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            // Only allow interaction if toggling off
            return false;
        }
        
        return true;
    }
    
    // Toggle mode helpers
    public bool IsToggled() => isToggled;
    
    public void SetToggled(bool toggled)
    {
        if (syncInteraction)
        {
            photonView.RPC("RPC_SetToggled", RpcTarget.AllBuffered, toggled);
        }
        else
        {
            isToggled = toggled;
        }
    }
    
    [PunRPC]
    void RPC_SetToggled(bool toggled)
    {
        isToggled = toggled;
    }
    
    public void ResetToggle()
    {
        if (syncInteraction)
        {
            photonView.RPC("RPC_ResetToggle", RpcTarget.AllBuffered);
        }
        else
        {
            isToggled = false;
            currentUserID = -1;
        }
    }
    
    [PunRPC]
    void RPC_ResetToggle()
    {
        isToggled = false;
        currentUserID = -1;
    }
    
    // Optional: Reset interaction state
    public void ResetInteraction()
    {
        hasInteracted = false;
    }
    
    // Check if local player is currently using this
    public bool IsLocalPlayerUsing()
    {
        return currentUserID == PhotonNetwork.LocalPlayer.ActorNumber;
    }
    
    public bool IsBeingUsed()
    {
        return currentUserID != -1;
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
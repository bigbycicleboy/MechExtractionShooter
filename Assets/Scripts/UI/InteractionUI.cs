using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class InteractionUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMPro.TextMeshProUGUI promptTextTMP; 
    
    [Header("Animation")]
    [SerializeField] private float fadeSpeed = 5f;
    
    private CanvasGroup canvasGroup;
    private bool isVisible = false;
    
    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }
    
    void Update()
    {
        // Smooth fade in/out
        float targetAlpha = isVisible ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
    }
    
    public void Show(string prompt)
    {
        isVisible = true;
        
        if (promptTextTMP != null)
        {
            promptTextTMP.text = prompt;
        }
    }
    
    public void Hide()
    {
        isVisible = false;
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Makes UI buttons float down from the top of the screen when mouse is within radius
/// </summary>
public class FloatingUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Float Settings")]
    [Tooltip("Radius around the button that triggers the float effect")]
    public float hoverRadius = 100f;
    
    [Tooltip("How far down the button should float (in pixels)")]
    public float floatDistance = 50f;
    
    [Tooltip("How fast the button should float down")]
    public float floatSpeed = 5f;
    
    [Tooltip("How fast the button should return to original position")]
    public float returnSpeed = 8f;
    
    [Tooltip("Easing curve for the float animation")]
    public AnimationCurve floatCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Visual Effects")]
    [Tooltip("Should the button scale up slightly when floating?")]
    public bool scaleOnFloat = true;
    
    [Tooltip("Scale multiplier when floating (1 = normal size)")]
    public float floatScale = 1.1f;
    
    [Tooltip("Should the button fade in when floating?")]
    public bool fadeOnFloat = false;
    
    [Tooltip("Minimum alpha when not floating (0 = invisible, 1 = fully visible)")]
    [Range(0f, 1f)]
    public float minAlpha = 0.3f;
    
    [Header("Advanced")]
    [Tooltip("Check this to use screen space instead of world space for radius calculation")]
    public bool useScreenSpace = false; // Default to false for World Space Canvas
    
    [Tooltip("Offset from the top of the screen where buttons start (in pixels)")]
    public float topOffset = 20f;
    
    [Tooltip("Delay before the button starts floating (in seconds)")]
    public float floatDelay = 0.1f;
    
    [Tooltip("Delay before the button starts returning (in seconds)")]
    public float returnDelay = 0.2f;
    
    [Header("Debug")]
    [Tooltip("Enable debug logging to help troubleshoot issues")]
    public bool debugMode = true;
    
    [Tooltip("Show debug info in the scene view")]
    public bool showDebugInfo = true;
    
    // Private variables
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Vector3 originalScale;
    private float originalAlpha;
    private bool isFloating = false;
    private bool isHovering = false;
    private float currentFloatTime = 0f;
    private float currentReturnTime = 0f;
    private float delayTimer = 0f;
    private bool delayStarted = false;
    private Camera uiCamera;
    private PhysicsRaycaster physicsRaycaster;
    
    void Start()
    {
        // Get components
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // Find UI camera
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            uiCamera = canvas.worldCamera;
        }
        else if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            uiCamera = canvas.worldCamera;
            if (uiCamera == null)
            {
                uiCamera = Camera.main;
            }
        }
        else
        {
            uiCamera = Camera.main;
        }
        
        // Ensure we have a PhysicsRaycaster for World Space
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            physicsRaycaster = uiCamera.GetComponent<PhysicsRaycaster>();
            if (physicsRaycaster == null)
            {
                physicsRaycaster = uiCamera.gameObject.AddComponent<PhysicsRaycaster>();
                if (debugMode)
                {
                    Debug.Log($"[FloatingUIButton] Added PhysicsRaycaster to {uiCamera.name}");
                }
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"[FloatingUIButton] Initialized on {gameObject.name}. Canvas: {canvas?.name}, Camera: {uiCamera?.name}, RenderMode: {canvas?.renderMode}");
            if (canvas?.renderMode == RenderMode.WorldSpace && useScreenSpace)
            {
                Debug.LogWarning($"[FloatingUIButton] {gameObject.name} - World Space Canvas detected but useScreenSpace is true. Consider setting useScreenSpace to false for better accuracy.");
            }
            else if (canvas?.renderMode == RenderMode.ScreenSpaceOverlay && !useScreenSpace)
            {
                Debug.LogWarning($"[FloatingUIButton] {gameObject.name} - Screen Space Canvas detected but useScreenSpace is false. Consider setting useScreenSpace to true for better accuracy.");
            }
        }
        
        // Store original values
        originalPosition = rectTransform.anchoredPosition;
        originalScale = transform.localScale; // Use the GameObject's actual scale
        originalAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        
        // Set initial position (hidden at top)
        Vector2 hiddenPosition = originalPosition;
        hiddenPosition.y += floatDistance;
        rectTransform.anchoredPosition = hiddenPosition;
        
        // Set initial alpha if fading is enabled
        if (fadeOnFloat && canvasGroup != null)
        {
            canvasGroup.alpha = minAlpha;
        }
        
        if (debugMode)
        {
            Debug.Log($"[FloatingUIButton] {gameObject.name} - Original position: {originalPosition}, Original scale: {originalScale}, Hidden position: {hiddenPosition}");
        }
    }
    
    void Update()
    {
        // Check if mouse is within radius
        bool mouseInRadius = IsMouseInRadius();
        
        if (debugMode && showDebugInfo)
        {
            // Log distance info occasionally
            if (Time.frameCount % 60 == 0) // Every 60 frames
            {
                float distance = GetMouseDistance();
                Debug.Log($"[FloatingUIButton] {gameObject.name} - Mouse distance: {distance:F1}, Radius: {hoverRadius}, In radius: {mouseInRadius}, UseScreenSpace: {useScreenSpace}");
            }
        }
        
        // Handle hover state changes
        if (mouseInRadius && !isHovering)
        {
            OnHoverStart();
        }
        else if (!mouseInRadius && isHovering)
        {
            OnHoverEnd();
        }
        
        // Update floating animation
        UpdateFloatingAnimation();
    }
    
    float GetMouseDistance()
    {
        if (useScreenSpace)
        {
            // Use screen space coordinates
            Vector2 mouseScreenPos = Input.mousePosition;
            Vector2 buttonScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, rectTransform.position);
            
            return Vector2.Distance(mouseScreenPos, buttonScreenPos);
        }
        else
        {
            // Use world space coordinates for World Space Canvas
            if (uiCamera != null)
            {
                // Get mouse position in world space at the same Z depth as the button
                Vector3 mouseWorldPos = uiCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Distance(uiCamera.transform.position, rectTransform.position)));
                Vector3 buttonWorldPos = rectTransform.position;
                
                // Calculate 2D distance (ignore Z axis for UI)
                Vector2 mouse2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
                Vector2 button2D = new Vector2(buttonWorldPos.x, buttonWorldPos.y);
                
                return Vector2.Distance(mouse2D, button2D);
            }
            else
            {
                return float.MaxValue; // No camera available
            }
        }
    }
    
    bool IsMouseInRadius()
    {
        float distance = GetMouseDistance();
        return distance <= hoverRadius;
    }
    
    void OnHoverStart()
    {
        isHovering = true;
        delayTimer = 0f;
        delayStarted = true;
        
        if (debugMode)
        {
            Debug.Log($"[FloatingUIButton] {gameObject.name} - Hover started!");
        }
    }
    
    void OnHoverEnd()
    {
        isHovering = false;
        delayTimer = 0f;
        delayStarted = true;
        
        if (debugMode)
        {
            Debug.Log($"[FloatingUIButton] {gameObject.name} - Hover ended!");
        }
    }
    
    void UpdateFloatingAnimation()
    {
        if (delayStarted)
        {
            delayTimer += Time.deltaTime;
            
            if (isHovering && delayTimer >= floatDelay)
            {
                // Start floating
                if (!isFloating)
                {
                    isFloating = true;
                    currentFloatTime = 0f;
                    
                    if (debugMode)
                    {
                        Debug.Log($"[FloatingUIButton] {gameObject.name} - Started floating!");
                    }
                }
                delayStarted = false;
            }
            else if (!isHovering && delayTimer >= returnDelay)
            {
                // Start returning
                if (isFloating)
                {
                    isFloating = false;
                    currentReturnTime = 0f;
                    
                    if (debugMode)
                    {
                        Debug.Log($"[FloatingUIButton] {gameObject.name} - Started returning!");
                    }
                }
                delayStarted = false;
            }
        }
        
        if (isFloating)
        {
            // Float down animation
            currentFloatTime += Time.deltaTime * floatSpeed;
            float progress = Mathf.Clamp01(currentFloatTime);
            float easedProgress = floatCurve.Evaluate(progress);
            
            // Update position
            Vector2 targetPosition = originalPosition;
            Vector2 currentPosition = rectTransform.anchoredPosition;
            currentPosition.y = Mathf.Lerp(originalPosition.y + floatDistance, originalPosition.y, easedProgress);
            rectTransform.anchoredPosition = currentPosition;
            
            // Update scale - use original scale as base
            if (scaleOnFloat)
            {
                Vector3 targetScale = originalScale * floatScale;
                Vector3 currentScale = Vector3.Lerp(originalScale, targetScale, easedProgress);
                transform.localScale = currentScale;
            }
            
            // Update alpha
            if (fadeOnFloat && canvasGroup != null)
            {
                float targetAlpha = Mathf.Lerp(minAlpha, originalAlpha, easedProgress);
                canvasGroup.alpha = targetAlpha;
            }
        }
        else
        {
            // Return animation
            currentReturnTime += Time.deltaTime * returnSpeed;
            float progress = Mathf.Clamp01(currentReturnTime);
            float easedProgress = floatCurve.Evaluate(progress);
            
            // Update position
            Vector2 targetPosition = originalPosition;
            Vector2 currentPosition = rectTransform.anchoredPosition;
            currentPosition.y = Mathf.Lerp(currentPosition.y, originalPosition.y + floatDistance, easedProgress);
            rectTransform.anchoredPosition = currentPosition;
            
            // Update scale - return to original scale
            if (scaleOnFloat)
            {
                Vector3 currentScale = transform.localScale;
                Vector3 targetScale = Vector3.Lerp(currentScale, originalScale, easedProgress);
                transform.localScale = targetScale;
            }
            
            // Update alpha
            if (fadeOnFloat && canvasGroup != null)
            {
                float targetAlpha = Mathf.Lerp(canvasGroup.alpha, minAlpha, easedProgress);
                canvasGroup.alpha = targetAlpha;
            }
        }
    }
    
    // Interface implementations for mouse events
    public void OnPointerEnter(PointerEventData eventData)
    {
        // This is handled by the radius check in Update()
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // This is handled by the radius check in Update()
    }
    
    // Public methods for external control
    public void ForceFloat()
    {
        isHovering = true;
        isFloating = true;
        currentFloatTime = 0f;
        delayStarted = false;
        
        if (debugMode)
        {
            Debug.Log($"[FloatingUIButton] {gameObject.name} - Force float called!");
        }
    }
    
    public void ForceReturn()
    {
        isHovering = false;
        isFloating = false;
        currentReturnTime = 0f;
        delayStarted = false;
        
        if (debugMode)
        {
            Debug.Log($"[FloatingUIButton] {gameObject.name} - Force return called!");
        }
    }
    
    public void SetFloatDistance(float distance)
    {
        floatDistance = distance;
    }
    
    public void SetHoverRadius(float radius)
    {
        hoverRadius = radius;
    }
    
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && rectTransform != null)
        {
            // Draw radius in scene view
            Vector3 center = rectTransform.position;
            Gizmos.color = isHovering ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(center, hoverRadius);
            
            // Draw float distance
            Gizmos.color = Color.blue;
            Vector3 topPos = center + Vector3.up * floatDistance;
            Gizmos.DrawLine(center, topPos);
            Gizmos.DrawWireSphere(topPos, 5f);
            
            // Draw mouse position and ray
            if (uiCamera != null)
            {
                Ray mouseRay = uiCamera.ScreenPointToRay(Input.mousePosition);
                Gizmos.color = Color.red;
                Gizmos.DrawRay(mouseRay.origin, mouseRay.direction * 10f);
                
                // Draw line to button
                Gizmos.color = Color.white;
                Gizmos.DrawLine(mouseRay.origin, center);
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (showDebugInfo && Application.isPlaying && rectTransform != null)
        {
            // Always show debug info when enabled
            Vector3 center = rectTransform.position;
            Gizmos.color = isHovering ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(center, hoverRadius);
        }
    }

#endif
} 
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class UIHoverController : MonoBehaviour
{
    [System.Serializable]
    public class UIRoot
    {
        public RectTransform rootTransform;
        public Vector2 hiddenPosition = new Vector2(0, 100f);
        public Vector2 visiblePosition = Vector2.zero;
    }

    [Header("UI Roots")]
    [SerializeField] private UIRoot frameRoot;
    [SerializeField] private UIRoot hairRoot;
    [SerializeField] private UIRoot dotRoot;
    [SerializeField] private UIRoot settingsRoot;

    [Header("Hitbox Settings")]
    [SerializeField] private Canvas canvas;
    
    [Header("Main UI Hitbox")]
    [SerializeField] private Vector2 mainHitboxPosition = new Vector2(0, 0);
    [SerializeField] private Vector2 mainHitboxSize = new Vector2(200, 50);
    private Rect mainHitboxRect;
    
    [Header("Settings Hitbox")]
    [SerializeField] private Vector2 settingsHitboxPosition = new Vector2(0, 0);
    [SerializeField] private Vector2 settingsHitboxSize = new Vector2(200, 50);
    private Rect settingsHitboxRect;

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    // Main UI state
    private UIRoot activeMainRoot = null;
    private UIRoot targetMainRoot = null;
    private Vector2 mainStartPos;
    private Vector2 mainTargetPos;
    private float mainAnimTimer = 0f;
    private bool isMainAnimating = false;
    private bool isHoveringMain = false;
    
    // Settings state
    private UIRoot activeSettingsRoot = null;
    private UIRoot targetSettingsRoot = null;
    private Vector2 settingsStartPos;
    private Vector2 settingsTargetPos;
    private float settingsAnimTimer = 0f;
    private bool isSettingsAnimating = false;
    private bool isHoveringSettings = false;
    
    private Camera uiCamera;

    private void Start()
    {
        // Get the camera used for UI
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = null;
        }
        else
        {
            uiCamera = canvas.worldCamera ?? Camera.main;
        }

        // Initialize hitbox rects
        mainHitboxRect = new Rect(
            mainHitboxPosition.x - mainHitboxSize.x * 0.5f,
            mainHitboxPosition.y - mainHitboxSize.y * 0.5f,
            mainHitboxSize.x,
            mainHitboxSize.y
        );
        
        settingsHitboxRect = new Rect(
            settingsHitboxPosition.x - settingsHitboxSize.x * 0.5f,
            settingsHitboxPosition.y - settingsHitboxSize.y * 0.5f,
            settingsHitboxSize.x,
            settingsHitboxSize.y
        );

        // Initialize all roots to hidden position
        HideAllRoots();
        Debug.Log("UIHoverController initialized with manual hitboxes");
    }

    // Queues for each UI section
    private bool pendingMainHide = false;
    private UIRoot pendingMainRoot = null;
    private bool pendingSettingsHide = false;
    private UIRoot pendingSettingsRoot = null;
    
    private void Update()
    {
        if (!Input.mousePresent) return;
        
        // Convert mouse position to canvas space
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            Input.mousePosition,
            uiCamera,
            out localPoint
        );
        
        // Check if mouse is over either hitbox
        bool wasHoveringMain = isHoveringMain;
        bool wasHoveringSettings = isHoveringSettings;
        
        isHoveringMain = mainHitboxRect.Contains(localPoint);
        isHoveringSettings = settingsHitboxRect.Contains(localPoint);
        
        // Debug draw the hitboxes
        DebugDrawRect(mainHitboxRect, isHoveringMain ? Color.green : Color.red);
        DebugDrawRect(settingsHitboxRect, isHoveringSettings ? Color.blue : Color.yellow);

        // Handle main UI state changes
        if (isHoveringMain != wasHoveringMain && !isMainAnimating)
        {
            if (isHoveringMain)
                ShowActiveRoot();
            else if (!isHoveringSettings)
                HideMainRoot();
        }

        // Handle settings state changes
        if (isHoveringSettings != wasHoveringSettings && !isSettingsAnimating)
        {
            if (isHoveringSettings)
                ShowSettings();
            else if (!isHoveringMain)
                HideSettingsRoot();
        }
        
        // Update animations
        UpdateMainAnimation();
        UpdateSettingsAnimation();
    }

    private void DebugDrawRect(Rect rect, Color color)
    {
        Vector3[] corners = new Vector3[4];
        corners[0] = new Vector3(rect.x, rect.y, 0);
        corners[1] = new Vector3(rect.x + rect.width, rect.y, 0);
        corners[2] = new Vector3(rect.x + rect.width, rect.y + rect.height, 0);
        corners[3] = new Vector3(rect.x, rect.y + rect.height, 0);
        
        // Convert to world space if needed
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            for (int i = 0; i < 4; i++)
            {
                corners[i] = canvas.transform.TransformPoint(corners[i]);
            }
        }
        
        // Draw the rectangle
        for (int i = 0; i < 4; i++)
        {
            Debug.DrawLine(corners[i], corners[(i + 1) % 4], color);
        }
    }

    private void UpdateMainAnimation()
    {
        if (targetMainRoot == null || targetMainRoot.rootTransform == null)
        {
            isMainAnimating = false;
            return;
        }
        
        // Update animation
        mainAnimTimer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(mainAnimTimer / slideDuration);
        t = slideCurve.Evaluate(t);
        
        // Calculate new position
        Vector2 newPosition = Vector2.Lerp(mainStartPos, mainTargetPos, t);
        targetMainRoot.rootTransform.anchoredPosition = newPosition;
        
        // Check if animation is complete
        if (t >= 1.0f)
        {
            targetMainRoot.rootTransform.anchoredPosition = mainTargetPos;
            activeMainRoot = targetMainRoot;
            isMainAnimating = false;
            
            // Process pending actions
            if (pendingMainHide)
            {
                pendingMainHide = false;
                HideMainRoot();
            }
            else if (pendingMainRoot != null)
            {
                var rootToShow = pendingMainRoot;
                pendingMainRoot = null;
                ShowRoot(rootToShow, isSettings: false);
            }
        }
    }
    
    private void UpdateSettingsAnimation()
    {
        if (targetSettingsRoot == null || targetSettingsRoot.rootTransform == null)
        {
            isSettingsAnimating = false;
            return;
        }
        
        // Update animation
        settingsAnimTimer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(settingsAnimTimer / slideDuration);
        t = slideCurve.Evaluate(t);
        
        // Calculate new position
        Vector2 newPosition = Vector2.Lerp(settingsStartPos, settingsTargetPos, t);
        targetSettingsRoot.rootTransform.anchoredPosition = newPosition;
        
        // Check if animation is complete
        if (t >= 1.0f)
        {
            targetSettingsRoot.rootTransform.anchoredPosition = settingsTargetPos;
            activeSettingsRoot = targetSettingsRoot;
            isSettingsAnimating = false;
            
            // Process pending actions
            if (pendingSettingsHide)
            {
                pendingSettingsHide = false;
                HideSettingsRoot();
            }
            else if (pendingSettingsRoot != null)
            {
                var rootToShow = pendingSettingsRoot;
                pendingSettingsRoot = null;
                ShowRoot(rootToShow, isSettings: true);
            }
        }
    }

    private void ShowActiveRoot()
    {
        // Default to frame root if no active root
        if (activeMainRoot == null)
        {
            ShowRoot(frameRoot, isSettings: false);
        }
        else
        {
            ShowRoot(activeMainRoot, isSettings: false);
        }
    }

    private void ShowSettings()
    {
        ShowRoot(settingsRoot, isSettings: true);
    }

    private void ShowRoot(UIRoot root, bool isSettings)
    {
        if (root == null || root.rootTransform == null) return;
        
        // Determine which animation system to use
        bool isAnimating = isSettings ? isSettingsAnimating : isMainAnimating;
        
        // If already showing this root, do nothing
        if ((isSettings ? targetSettingsRoot : targetMainRoot) == root && !isAnimating) 
            return;
        
        // If currently animating, queue this show request
        if (isAnimating)
        {
            if (isSettings)
            pendingSettingsRoot = root;
            else
                pendingMainRoot = root;
            return;
        }
        
        Debug.Log($"Showing {(isSettings ? "settings" : "main")} root: {root.rootTransform.name}");
        
        // Set up the appropriate animation
        if (isSettings)
        {
            settingsStartPos = root.rootTransform.anchoredPosition;
            settingsTargetPos = root.visiblePosition;
            targetSettingsRoot = root;
            activeSettingsRoot = root;
            settingsAnimTimer = 0f;
            isSettingsAnimating = true;
        }
        else
        {
            mainStartPos = root.rootTransform.anchoredPosition;
            mainTargetPos = root.visiblePosition;
            targetMainRoot = root;
            activeMainRoot = root;
            mainAnimTimer = 0f;
            isMainAnimating = true;
        }
    }

    private void HideMainRoot()
    {
        if (targetMainRoot == null) return;
        
        // If currently animating, queue the hide
        if (isMainAnimating)
        {
            pendingMainHide = true;
            pendingMainRoot = null;
            return;
        }
        
        Debug.Log("Hiding main UI root");
        
        // Set up hide animation
        mainStartPos = targetMainRoot.rootTransform.anchoredPosition;
        mainTargetPos = targetMainRoot.hiddenPosition;
        mainAnimTimer = 0f;
        isMainAnimating = true;
        
        // Clear references after animation completes
        StartCoroutine(ClearAfterDelay(slideDuration, isSettings: false));
    }
    
    private void HideSettingsRoot()
    {
        if (targetSettingsRoot == null) return;
        
        // If currently animating, queue the hide
        if (isSettingsAnimating)
        {
            pendingSettingsHide = true;
            pendingSettingsRoot = null;
            return;
        }
        
        Debug.Log("Hiding settings UI root");
        
        // Set up hide animation
        settingsStartPos = targetSettingsRoot.rootTransform.anchoredPosition;
        settingsTargetPos = targetSettingsRoot.hiddenPosition;
        settingsAnimTimer = 0f;
        isSettingsAnimating = true;
        
        // Clear references after animation completes
        StartCoroutine(ClearAfterDelay(slideDuration, isSettings: true));
    }
    
    private void HideAllRoots()
    {
        HideMainRoot();
        HideSettingsRoot();
    }
    
    private IEnumerator ClearAfterDelay(float delay, bool isSettings)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (isSettings)
        {
            activeSettingsRoot = null;
            targetSettingsRoot = null;
        }
        else
        {
            activeMainRoot = null;
            targetMainRoot = null;
        }
    }

    // Required interface implementation (not used directly)
    public void OnPointerEnter(PointerEventData eventData) { }
    public void OnPointerExit(PointerEventData eventData) { }
}

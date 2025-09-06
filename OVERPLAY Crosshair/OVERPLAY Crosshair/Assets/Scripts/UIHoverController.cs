using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

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
    
    [Header("Main Toggle")]
    [SerializeField] private Toggle mainToggle;
    [Tooltip("The GameObject with SpriteRenderer for the main toggle")]
    [SerializeField] private GameObject mainToggleObject;
    [Tooltip("Sprite to show when main toggle is active (locked in place)")]
    [SerializeField] private Sprite mainToggleActiveSprite;
    [Tooltip("Sprite to show when main toggle is inactive (default behavior)")]
    [SerializeField] private Sprite mainToggleInactiveSprite;
    
    [Header("Settings Toggle")]
    [SerializeField] private Toggle settingsToggle;
    [Tooltip("The GameObject with SpriteRenderer for the settings toggle")]
    [SerializeField] private GameObject settingsToggleObject;
    [Tooltip("Sprite to show when settings toggle is active (locked in place)")]
    [SerializeField] private Sprite settingsToggleActiveSprite;
    [Tooltip("Sprite to show when settings toggle is inactive (default behavior)")]
    [SerializeField] private Sprite settingsToggleInactiveSprite;
    
    // Toggle states
    private bool isMainToggleOn = false; // When true, prevents main UI from moving up
    private bool isSettingsToggleOn = false; // When true, prevents settings from moving up
    private bool isFirstSettingsOpen = true; // Track first time settings are opened
    private SpriteRenderer mainToggleRenderer;
    private SpriteRenderer settingsToggleRenderer;
    
    // Main UI states for each root
    private UIRoot activeMainRoot = null;
    private UIRoot targetMainRoot = null;
    private Dictionary<UIRoot, Vector2> rootStartPositions = new Dictionary<UIRoot, Vector2>();
    private Dictionary<UIRoot, Vector2> rootTargetPositions = new Dictionary<UIRoot, Vector2>();
    private Dictionary<UIRoot, float> rootAnimTimers = new Dictionary<UIRoot, float>();
    private HashSet<UIRoot> animatingRoots = new HashSet<UIRoot>();
    private List<UIRoot> mainRoots = new List<UIRoot>();
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
        // Initialize hover states to false by default
        isHoveringMain = false;
        isHoveringSettings = false;
        
        // Get the camera used for UI
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = null;
        }
        else
        {
            uiCamera = canvas.worldCamera ?? Camera.main;
        }

        // Initialize hitbox rects - only for settings since main UI won't move
        settingsHitboxRect = new Rect(
            settingsHitboxPosition.x - settingsHitboxSize.x * 0.5f,
            settingsHitboxPosition.y - settingsHitboxSize.y * 0.5f,
            settingsHitboxSize.x,
            settingsHitboxSize.y
        );
        
        // Initialize main roots - maintain editor positions
        if (frameRoot != null) 
        {
            mainRoots.Add(frameRoot);
            frameRoot.rootTransform.gameObject.SetActive(true);
            
            if (hairRoot != null) 
            {
                hairRoot.rootTransform.gameObject.SetActive(false);
                mainRoots.Add(hairRoot);
            }
            
            if (dotRoot != null) 
            {
                dotRoot.rootTransform.gameObject.SetActive(false);
                mainRoots.Add(dotRoot);
            }
            
            // Set frame as active by default
            activeMainRoot = frameRoot;
        }
        
        // Initialize settings toggle UI
        if (settingsToggle != null)
        {
            settingsToggle.isOn = isSettingsToggleOn;
            settingsToggle.onValueChanged.AddListener(OnSettingsToggleChanged);
            if (settingsToggleObject != null)
            {
                settingsToggleRenderer = settingsToggleObject.GetComponent<SpriteRenderer>();
                UpdateToggleVisual(settingsToggleRenderer, isSettingsToggleOn, settingsToggleActiveSprite, settingsToggleInactiveSprite);
            }
        }
        
        // Disable main toggle since we're removing its functionality
        if (mainToggle != null)
        {
            mainToggle.gameObject.SetActive(false);
        }
        
        Debug.Log("UIHoverController initialized - Main UI is static");
    }

    // Queues for UI sections
    private Queue<UIRoot> pendingMainShows = new Queue<UIRoot>();
    private bool pendingMainHide = false;
    private Queue<UIRoot> pendingSettingsShows = new Queue<UIRoot>();
    private bool pendingSettingsHide = false;
    
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
        
        // Check if mouse is over settings hitbox (main UI doesn't move anymore)
        bool wasHoveringSettings = isHoveringSettings;
        isHoveringSettings = settingsHitboxRect.Contains(localPoint);
        
        // Debug draw the settings hitbox
        DebugDrawRect(settingsHitboxRect, isHoveringSettings ? Color.blue : Color.yellow);

        // Handle settings state changes only
        if (isHoveringSettings != wasHoveringSettings)
        {
            if (isHoveringSettings)
            {
                ShowSettings();
            }
            // Only allow hiding if it's not the first time opening or if it's already locked
            else if (!isFirstSettingsOpen || isSettingsToggleOn)
            {
                HideSettingsRoot();
            }
        }
        
        // Only update settings animation (main UI doesn't animate)
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

    private void UpdateRootAnimations()
    {
        // Create a list of roots to process to avoid collection modified errors
        var rootsToProcess = new List<UIRoot>(animatingRoots);
        
        foreach (var root in rootsToProcess)
        {
            if (root == null || root.rootTransform == null) 
            {
                animatingRoots.Remove(root);
                continue;
            }
            
            // Update animation timer
            float currentTime = rootAnimTimers[root] + Time.unscaledDeltaTime;
            rootAnimTimers[root] = currentTime;
            
            // Calculate progress
            float t = Mathf.Clamp01(currentTime / slideDuration);
            t = slideCurve.Evaluate(t);
            
            // Check if we should reverse the animation due to hover state
            bool shouldBeVisible = isHoveringMain || isMainToggleOn;
            if (shouldBeVisible && rootTargetPositions[root] != root.visiblePosition)
            {
                // Reverse the animation
                rootStartPositions[root] = root.rootTransform.anchoredPosition;
                rootTargetPositions[root] = root.visiblePosition;
                rootAnimTimers[root] = 0f;
                continue;
            }
            
            // Update position
            Vector2 startPos = rootStartPositions[root];
            Vector2 targetPos = rootTargetPositions[root];
            root.rootTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            
            // Check if animation is complete
            if (t >= 1.0f)
            {
                root.rootTransform.anchoredPosition = targetPos;
                animatingRoots.Remove(root);
                
                // If this was showing a root, set it as active
                if (targetPos == root.visiblePosition)
                {
                    activeMainRoot = root;
                }
                
                // Process any pending actions
                ProcessPendingActions();
                
                // Force update hover state after animation
                ForceUpdateHoverState();
            }
        }
    }
    
    private void ProcessPendingActions()
    {
        // Process pending shows first
        if (pendingMainShows.Count > 0 && animatingRoots.Count == 0)
        {
            var rootToShow = pendingMainShows.Dequeue();
            ShowRoot(rootToShow, isSettings: false);
        }
        // Then process pending hides
        else if (pendingMainHide && animatingRoots.Count == 0)
        {
            pendingMainHide = false;
            HideMainRoots();
        }
    }
    
    private void StartSettingsAnimation(UIRoot root)
    {
        if (root == null || root.rootTransform == null) return;
        
        isSettingsAnimating = true;
        settingsStartPos = root.rootTransform.anchoredPosition;
        settingsTargetPos = root.visiblePosition;
        settingsAnimTimer = 0f;
        targetSettingsRoot = root;
        
        // Show the root if it's hidden
        root.rootTransform.gameObject.SetActive(true);
        
        // Start coroutine to lock settings after animation
        StartCoroutine(LockSettingsAfterAnimation());
    }
    
    private IEnumerator LockSettingsAfterAnimation()
    {
        // Only lock on first open
        if (isFirstSettingsOpen)
        {
            // Wait for the animation to complete
            yield return new WaitForSeconds(slideDuration);
            
            // Lock the settings
            isSettingsToggleOn = true;
            
            if (settingsToggle != null)
            {
                settingsToggle.isOn = true;
                UpdateToggleVisual(settingsToggleRenderer, true, settingsToggleActiveSprite, settingsToggleInactiveSprite);
            }
            
            // Only set first open to false after we've locked it
            isFirstSettingsOpen = false;
        }
        else
        {
            yield return null; // No need to wait if not first open
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
            else if (pendingSettingsShows.Count > 0)
            {
                var rootToShow = pendingSettingsShows.Dequeue();
                StartSettingsAnimation(rootToShow);
            }
        }
    }

    private void ShowActiveRoot()
    {
        // Only handle visibility, no position changes
        foreach (var root in mainRoots)
        {
            if (root != null && root.rootTransform != null)
            {
                // Only modify visibility, not position
                root.rootTransform.gameObject.SetActive(root == activeMainRoot);
            }
        }
    }

    private void ShowSettings()
    {
        // If toggle is on, force show settings in visible position
        if (isSettingsToggleOn && settingsRoot != null && settingsRoot.rootTransform != null)
        {
            settingsRoot.rootTransform.anchoredPosition = settingsRoot.visiblePosition;
            settingsRoot.rootTransform.gameObject.SetActive(true);
            targetSettingsRoot = settingsRoot;
            activeSettingsRoot = settingsRoot;
            return;
        }
        
        // Otherwise, show with animation
        ShowRoot(settingsRoot, isSettings: true);
    }

    private void ShowRoot(UIRoot root, bool isSettings)
    {
        if (root == null || root.rootTransform == null) return;
        
        // For settings, use the queue system
        if (isSettings)
        {
            if (isSettingsAnimating)
            pendingSettingsShows.Enqueue(root);
            else
                StartSettingsAnimation(root);
            return;
        }
        
        // For main UI roots, use the new animation system
        if (animatingRoots.Contains(root))
        {
            // If this root is already animating, queue the show
            if (!pendingMainShows.Contains(root))
                pendingMainShows.Enqueue(root);
            return;
        }
        
        Debug.Log($"Showing root: {root.rootTransform.name}");
        
        // Set up animation
        rootStartPositions[root] = root.rootTransform.anchoredPosition;
        rootTargetPositions[root] = root.visiblePosition;
        rootAnimTimers[root] = 0f;
        animatingRoots.Add(root);
        
        // If this is a main root, update the active root
        if (mainRoots.Contains(root))
        {
            activeMainRoot = root;
        }
    }

    private void HideMainRoots()
    {
        // No hiding - main UI is static
    }
    
    private void HideSettingsRoot()
    {
        if (targetSettingsRoot == null) return;
        
        // Don't hide if toggle is on (locked in place)
        if (isSettingsToggleOn) return;
        
        // If currently animating, queue the hide
        if (isSettingsAnimating)
        {
            pendingSettingsHide = true;
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
        HideMainRoots();
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
            // Don't clear targetMainRoot as we need it for multiple roots
            
            // Clear any pending shows since we're hiding
            pendingMainShows.Clear();
            pendingMainHide = false;
        }
    }

    // Main toggle is disabled - this method is kept for compatibility
    private void OnMainToggleChanged(bool isOn)
    {
        // No functionality - main toggle is disabled
    }
    
    private void OnSettingsToggleChanged(bool isOn)
    {
        isSettingsToggleOn = isOn;
        Debug.Log($"Settings UI movement {(isOn ? "locked" : "unlocked")}");
        
        // Update toggle visual
        UpdateToggleVisual(settingsToggleRenderer, isOn, settingsToggleActiveSprite, settingsToggleInactiveSprite);
        
        // If toggle is turned off and we're not hovering, hide the settings
        if (!isOn && !isHoveringSettings)
        {
            HideSettingsRoot();
        }
    }
    
    private void UpdateToggleVisual(SpriteRenderer renderer, bool isOn, Sprite activeSprite, Sprite inactiveSprite)
    {
        if (renderer != null)
        {
            renderer.sprite = isOn ? activeSprite : inactiveSprite;
        }
    }

    private void ForceUpdateHoverState()
    {
        // Force update the hover state to check for changes
        bool wasHoveringMain = isHoveringMain;
        bool wasHoveringSettings = isHoveringSettings;
        
        // This will trigger the hover state checks in the next frame
        isHoveringMain = false;
        isHoveringSettings = false;
        
        // Restore the hover state to trigger proper updates
        isHoveringMain = wasHoveringMain;
        isHoveringSettings = wasHoveringSettings;
    }
    
    // Required interface implementation (not used directly)
    public void OnPointerEnter(PointerEventData eventData) { }
    public void OnPointerExit(PointerEventData eventData) { }
}

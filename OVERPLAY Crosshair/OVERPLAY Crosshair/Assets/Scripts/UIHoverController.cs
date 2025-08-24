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
        
        // Initialize main roots list
        if (frameRoot != null) mainRoots.Add(frameRoot);
        if (hairRoot != null) mainRoots.Add(hairRoot);
        if (dotRoot != null) mainRoots.Add(dotRoot);
        
        // Initialize all roots to hidden position
        HideAllRoots();
        Debug.Log("UIHoverController initialized with manual hitboxes");
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
        
        // Check if mouse is over either hitbox
        bool wasHoveringMain = isHoveringMain;
        bool wasHoveringSettings = isHoveringSettings;
        
        isHoveringMain = mainHitboxRect.Contains(localPoint);
        isHoveringSettings = settingsHitboxRect.Contains(localPoint);
        
        // Debug draw the hitboxes
        DebugDrawRect(mainHitboxRect, isHoveringMain ? Color.green : Color.red);
        DebugDrawRect(settingsHitboxRect, isHoveringSettings ? Color.blue : Color.yellow);

        // Handle main UI state changes
        if (isHoveringMain != wasHoveringMain)
        {
            if (isHoveringMain)
                ShowActiveRoot();
            else if (!isHoveringSettings)
                HideMainRoots();
        }

        // Handle settings state changes
        if (isHoveringSettings != wasHoveringSettings)
        {
            if (isHoveringSettings)
                ShowSettings();
            else if (!isHoveringMain)
                HideSettingsRoot();
        }
        
        // Update animations for all roots
        UpdateRootAnimations();
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
        settingsStartPos = root.rootTransform.anchoredPosition;
        settingsTargetPos = root.visiblePosition;
        targetSettingsRoot = root;
        activeSettingsRoot = root;
        settingsAnimTimer = 0f;
        isSettingsAnimating = true;
    }

    private void UpdateSettingsAnimation()
    {
        if (targetSettingsRoot == null || targetSettingsRoot.rootTransform == null)
        {
            isSettingsAnimating = false;
            // Process any pending shows if animation is complete
            if (pendingSettingsShows.Count > 0)
            {
                var rootToShow = pendingSettingsShows.Dequeue();
                StartSettingsAnimation(rootToShow);
            }
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
        // Show all main roots
        foreach (var root in mainRoots)
        {
            if (root != null)
            {
                ShowRoot(root, isSettings: false);
            }
        }
    }

    private void ShowSettings()
    {
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
        // If any roots are animating, queue the hide
        if (animatingRoots.Count > 0)
        {
            pendingMainHide = true;
            return;
        }
        
        Debug.Log("Hiding all main UI roots");
        
        // Start hide animation for all main roots
        foreach (var root in mainRoots)
        {
            if (root != null && root.rootTransform != null)
            {
                rootStartPositions[root] = root.rootTransform.anchoredPosition;
                rootTargetPositions[root] = root.hiddenPosition;
                rootAnimTimers[root] = 0f;
                animatingRoots.Add(root);
            }
        }
        
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

    // Required interface implementation (not used directly)
    public void OnPointerEnter(PointerEventData eventData) { }
    public void OnPointerExit(PointerEventData eventData) { }
}

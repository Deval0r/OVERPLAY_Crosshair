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
    
    private UIRoot activeRoot = null;
    private UIRoot targetRoot = null;
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private float animationTimer = 0f;
    private bool isHoveringMain = false;
    private bool isHoveringSettings = false;
    private bool wasHoveringMain = false;
    private bool wasHoveringSettings = false;
    private Camera uiCamera;
    private Vector2 startPosition;
    private Vector2 targetPosition;

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
    
    private void UpdateAnimation()
    {
        if (targetRoot == null || targetRoot.rootTransform == null) return;
        
        // If we're at the target position, nothing to do
        if (Vector2.Distance(targetRoot.rootTransform.anchoredPosition, targetPosition) < 0.1f)
        {
            targetRoot.rootTransform.anchoredPosition = targetPosition;
            activeRoot = targetRoot;
            return;
        }
        
        // Update animation
        animationTimer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(animationTimer / slideDuration);
        t = slideCurve.Evaluate(t);
        
        Vector2 newPosition = Vector2.Lerp(startPosition, targetPosition, t);
        targetRoot.rootTransform.anchoredPosition = newPosition;
        
        Debug.Log($"Animating {targetRoot.rootTransform.name} from {startPosition} to {targetPosition} (t={t}, pos={newPosition})");
    }

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
        bool isHoveringMainNow = mainHitboxRect.Contains(localPoint);
        bool isHoveringSettingsNow = settingsHitboxRect.Contains(localPoint);
        
        // Debug draw the hitboxes
        DebugDrawRect(mainHitboxRect, isHoveringMainNow ? Color.green : Color.red);
        DebugDrawRect(settingsHitboxRect, isHoveringSettingsNow ? Color.blue : Color.yellow);
        }

        // Check for state changes
        if (isHoveringMainNow != wasHoveringMain)
        {
            wasHoveringMain = isHoveringMainNow;
            if (isHoveringMainNow)
            {
                Debug.Log("Main hitbox entered");
                ShowActiveRoot();
            }
            else if (!isHoveringSettingsNow)
            {
                Debug.Log("Main hitbox exited");
                HideAllRoots();
            }
        }

        if (isHoveringSettingsNow != wasHoveringSettings)
        {
            wasHoveringSettings = isHoveringSettingsNow;
            if (isHoveringSettingsNow)
            {
                Debug.Log("Settings hitbox entered");
                ShowSettings();
            }
            else if (!isHoveringMainNow)
            {
                Debug.Log("Settings hitbox exited");
                HideAllRoots();
            }
        }

        // Handle animation
        UpdateAnimation();
    }

    private void SetupHitbox(RectTransform hitbox, System.Action<PointerEventData> onEnter, System.Action<PointerEventData> onExit)
    {
        if (hitbox == null) return;
        
        var trigger = hitbox.GetComponent<EventTrigger>();
        if (trigger == null) trigger = hitbox.gameObject.AddComponent<EventTrigger>();
        
        // Clear existing triggers to avoid duplicates
        trigger.triggers.Clear();
        
        // Add enter event
        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((data) => onEnter?.Invoke((PointerEventData)data));
        trigger.triggers.Add(enterEntry);
        
        // Add exit event
        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((data) => onExit?.Invoke((PointerEventData)data));
        trigger.triggers.Add(exitEntry);
    }

    private void OnMainHitboxEnter(PointerEventData eventData)
    {
        Debug.Log("Main hitbox entered");
        isHoveringMain = true;
        ShowActiveRoot();
    }

    private void OnMainHitboxExit(PointerEventData eventData)
    {
        Debug.Log("Main hitbox exited");
        isHoveringMain = false;
        if (!isHoveringSettings)
        {
            Debug.Log("Hiding all roots");
            HideAllRoots();
        }
    }

    private void OnSettingsHitboxEnter(PointerEventData eventData)
    {
        Debug.Log("Settings hitbox entered");
        isHoveringSettings = true;
        ShowSettings();
    }

    private void OnSettingsHitboxExit(PointerEventData eventData)
    {
        Debug.Log("Settings hitbox exited");
        isHoveringSettings = false;
        if (!isHoveringMain)
        {
            Debug.Log("Hiding all roots");
            HideAllRoots();
        }
    }

    private void ShowActiveRoot()
    {
        // Determine which root to show based on active tab
        // For now, default to frameRoot - we'll add tab switching later
        ShowRoot(frameRoot);
    }

    private void ShowSettings()
    {
        ShowRoot(settingsRoot);
    }

    private void ShowRoot(UIRoot root)
    {
        if (root == null)
        {
            Debug.LogError("Tried to show null root!");
            return;
        }
        if (root.rootTransform == null)
        {
            Debug.LogError("Root transform is null for: " + root);
            return;
        }
        
        Debug.Log($"Showing root: {root.rootTransform.name} (Active: {activeRoot?.rootTransform?.name ?? "none"}, Target: {targetRoot?.rootTransform?.name ?? "none"})");
        
        // If this root is already the active one, don't do anything
        if (activeRoot == root && targetRoot == root)
        {
            Debug.Log("Root is already active");
            return;
        }
        
        // Set up animation
        startPosition = root.rootTransform.anchoredPosition;
        targetPosition = root.visiblePosition;
        targetRoot = root;
        animationTimer = 0f;
        
        // Force update on first frame
        UpdateAnimation();
    }

    private void HideAllRoots()
    {
        Debug.Log("Hiding all UI roots");
        
        // Only hide if we're not already hiding
        if (targetRoot == null) return;
        
        // Set up animation to hide the current target
        startPosition = targetRoot.rootTransform.anchoredPosition;
        targetPosition = targetRoot.hiddenPosition;
        animationTimer = 0f;
        
        Debug.Log($"Hiding {targetRoot.rootTransform.name} from {startPosition} to {targetPosition}");
        
        // Clear references after animation completes
        StartCoroutine(ClearAfterDelay(slideDuration));
    }
    
    private System.Collections.IEnumerator ClearAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        activeRoot = null;
        targetRoot = null;
    }

    // Required interface implementation (not used directly)
    public void OnPointerEnter(PointerEventData eventData) { }
    public void OnPointerExit(PointerEventData eventData) { }
}

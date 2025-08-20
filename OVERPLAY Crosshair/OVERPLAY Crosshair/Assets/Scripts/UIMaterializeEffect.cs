using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Provides a materializing effect for UI elements when they open and close
/// </summary>
public class UIMaterializeEffect : MonoBehaviour
{
    [Header("Materialize Settings")]
    [SerializeField] private float materializeDuration = 0.3f;
    [SerializeField] private AnimationCurve materializeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool useScaleEffect = true;
    [SerializeField] private bool useAlphaEffect = true;
    [SerializeField] private Vector3 startScale = new Vector3(0.8f, 0.8f, 0.8f);
    [SerializeField] private Vector3 endScale = Vector3.one;
    
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine materializeCoroutine;
    
    void Awake()
    {
        // Get or add CanvasGroup for alpha effects
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null && useAlphaEffect)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        rectTransform = GetComponent<RectTransform>();
        
        // Set initial state
        if (useAlphaEffect && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        if (useScaleEffect && rectTransform != null)
        {
            rectTransform.localScale = startScale;
        }
    }
    
    void OnEnable()
    {
        // Start materialize in effect when enabled
        StartMaterializeIn();
    }
    
    public void StartMaterializeIn()
    {
        if (materializeCoroutine != null)
        {
            StopCoroutine(materializeCoroutine);
        }
        materializeCoroutine = StartCoroutine(MaterializeIn());
    }
    
    public void StartMaterializeOut()
    {
        if (materializeCoroutine != null)
        {
            StopCoroutine(materializeCoroutine);
        }
        materializeCoroutine = StartCoroutine(MaterializeOut());
    }
    
    private IEnumerator MaterializeIn()
    {
        float elapsed = 0f;
        
        // Set initial state
        if (useAlphaEffect && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        if (useScaleEffect && rectTransform != null)
        {
            rectTransform.localScale = startScale;
        }
        
        while (elapsed < materializeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = materializeCurve.Evaluate(elapsed / materializeDuration);
            
            // Apply alpha effect
            if (useAlphaEffect && canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            }
            
            // Apply scale effect
            if (useScaleEffect && rectTransform != null)
            {
                rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            }
            
            yield return null;
        }
        
        // Ensure final state
        if (useAlphaEffect && canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        
        if (useScaleEffect && rectTransform != null)
        {
            rectTransform.localScale = endScale;
        }
        
        materializeCoroutine = null;
    }
    
    private IEnumerator MaterializeOut()
    {
        float elapsed = 0f;
        
        // Set initial state
        if (useAlphaEffect && canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        
        if (useScaleEffect && rectTransform != null)
        {
            rectTransform.localScale = endScale;
        }
        
        while (elapsed < materializeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = materializeCurve.Evaluate(elapsed / materializeDuration);
            
            // Apply alpha effect
            if (useAlphaEffect && canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }
            
            // Apply scale effect
            if (useScaleEffect && rectTransform != null)
            {
                rectTransform.localScale = Vector3.Lerp(endScale, startScale, t);
            }
            
            yield return null;
        }
        
        // Ensure final state
        if (useAlphaEffect && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        if (useScaleEffect && rectTransform != null)
        {
            rectTransform.localScale = startScale;
        }
        
        materializeCoroutine = null;
        
        // Disable the GameObject after materialize out
        gameObject.SetActive(false);
    }
    
    // Public method to set materialize duration
    public void SetMaterializeDuration(float duration)
    {
        materializeDuration = Mathf.Max(0.1f, duration);
    }
    
    // Public method to set materialize curve
    public void SetMaterializeCurve(AnimationCurve curve)
    {
        materializeCurve = curve;
    }
}

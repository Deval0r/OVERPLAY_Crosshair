using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Utility script to easily set up dissolve effects on UI elements
/// </summary>
public class UIDissolveSetup : MonoBehaviour
{
    [Header("Setup Settings")]
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private Vector2 gridSize = new Vector2(16, 12);
    [SerializeField] private float dissolveDuration = 0.4f;
    [SerializeField] private Vector2 dissolveDirection = new Vector2(0.5f, 0.5f);
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupDissolveEffect();
        }
    }
    
    [ContextMenu("Setup Dissolve Effect")]
    public void SetupDissolveEffect()
    {
        // Check if UIDissolveEffect already exists
        UIDissolveEffect existingEffect = GetComponent<UIDissolveEffect>();
        if (existingEffect != null)
        {
            Debug.Log("UIDissolveEffect already exists on " + gameObject.name);
            return;
        }
        
        // Add CanvasGroup if it doesn't exist
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Debug.Log("Added CanvasGroup to " + gameObject.name);
        }
        
        // Add UIDissolveEffect component
        UIDissolveEffect dissolveEffect = gameObject.AddComponent<UIDissolveEffect>();
        
        // Set up the dissolve effect properties using reflection since they're private
        var gridSizeField = typeof(UIDissolveEffect).GetField("gridSize", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (gridSizeField != null)
        {
            gridSizeField.SetValue(dissolveEffect, gridSize);
        }
        
        var durationField = typeof(UIDissolveEffect).GetField("dissolveDuration", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (durationField != null)
        {
            durationField.SetValue(dissolveEffect, dissolveDuration);
        }
        
        var directionField = typeof(UIDissolveEffect).GetField("dissolveDirection", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (directionField != null)
        {
            directionField.SetValue(dissolveEffect, dissolveDirection);
        }
        
        Debug.Log("Successfully set up UIDissolveEffect on " + gameObject.name);
    }
    
    [ContextMenu("Remove Dissolve Effect")]
    public void RemoveDissolveEffect()
    {
        UIDissolveEffect dissolveEffect = GetComponent<UIDissolveEffect>();
        if (dissolveEffect != null)
        {
            DestroyImmediate(dissolveEffect);
            Debug.Log("Removed UIDissolveEffect from " + gameObject.name);
        }
        
        // Optionally remove CanvasGroup if it was added by this script
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            DestroyImmediate(canvasGroup);
            Debug.Log("Removed CanvasGroup from " + gameObject.name);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UISoundHandler : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private Transform uiRoot; // Assign the main UI root transform
    
    private HashSet<GameObject> processedObjects = new HashSet<GameObject>();
    
    void Start()
    {
        // If no UI root is assigned, try to find it automatically
        if (uiRoot == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            if (canvases.Length > 0)
            {
                uiRoot = canvases[0].transform;
            }
        }
        
        // Initial scan of all UI elements
        ScanAndAddSoundsToAllUIElements();
        
        // Set up periodic scanning for newly enabled UI elements
        InvokeRepeating("ScanForNewUIElements", 1f, 2f);
    }
    
    void ScanForNewUIElements()
    {
        if (uiRoot != null)
        {
            ScanAndAddSoundsToAllUIElements();
        }
    }
    
    void ScanAndAddSoundsToAllUIElements()
    {
        // Scan for buttons
        Button[] buttons = FindObjectsOfType<Button>();
        foreach (Button button in buttons)
        {
            if (!processedObjects.Contains(button.gameObject))
            {
                AddButtonSounds(button);
                processedObjects.Add(button.gameObject);
            }
        }
        
        // Scan for sliders
        Slider[] sliders = FindObjectsOfType<Slider>();
        foreach (Slider slider in sliders)
        {
            if (!processedObjects.Contains(slider.gameObject))
            {
                AddSliderSounds(slider);
                processedObjects.Add(slider.gameObject);
            }
        }
        
        // Scan for toggles
        Toggle[] toggles = FindObjectsOfType<Toggle>();
        foreach (Toggle toggle in toggles)
        {
            if (!processedObjects.Contains(toggle.gameObject))
            {
                AddToggleSounds(toggle);
                processedObjects.Add(toggle.gameObject);
            }
        }
        
        // Scan for dropdowns
        TMPro.TMP_Dropdown[] dropdowns = FindObjectsOfType<TMPro.TMP_Dropdown>();
        foreach (TMPro.TMP_Dropdown dropdown in dropdowns)
        {
            if (!processedObjects.Contains(dropdown.gameObject))
            {
                AddDropdownSounds(dropdown);
                processedObjects.Add(dropdown.gameObject);
            }
        }
    }
    
    private void AddButtonSounds(Button button)
    {
        // Check if sound events are already added
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // Check if pointer down event already exists
        bool hasPointerDown = false;
        bool hasPointerUp = false;
        
        foreach (EventTrigger.Entry entry in trigger.triggers)
        {
            if (entry.eventID == EventTriggerType.PointerDown)
                hasPointerDown = true;
            if (entry.eventID == EventTriggerType.PointerUp)
                hasPointerUp = true;
        }
        
        // Add pointer down event for click sound if it doesn't exist
        if (!hasPointerDown)
        {
            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => { 
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick(); 
            });
            trigger.triggers.Add(pointerDown);
        }
        
        // Add pointer up event for release sound if it doesn't exist
        if (!hasPointerUp)
        {
            EventTrigger.Entry pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => { 
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonRelease(); 
            });
            trigger.triggers.Add(pointerUp);
        }
    }
    
    private void AddSliderSounds(Slider slider)
    {
        // Add sound listener to existing onValueChanged without removing others
        slider.onValueChanged.AddListener((value) => {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySliderChange();
        });
        
        // Check if release sound event is already added
        EventTrigger trigger = slider.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = slider.gameObject.AddComponent<EventTrigger>();
        }
        
        // Check if pointer up event already exists
        bool hasPointerUp = false;
        foreach (EventTrigger.Entry entry in trigger.triggers)
        {
            if (entry.eventID == EventTriggerType.PointerUp)
                hasPointerUp = true;
        }
        
        // Add pointer up event for release sound if it doesn't exist
        if (!hasPointerUp)
        {
            EventTrigger.Entry pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => { 
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySliderRelease(); 
            });
            trigger.triggers.Add(pointerUp);
        }
    }
    
    private void AddToggleSounds(Toggle toggle)
    {
        // Add sound listener to existing onValueChanged without removing others
        toggle.onValueChanged.AddListener((value) => {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        });
    }
    
    private void AddDropdownSounds(TMPro.TMP_Dropdown dropdown)
    {
        // Add sound listener to existing onValueChanged without removing others
        dropdown.onValueChanged.AddListener((value) => {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        });
    }
    
    // Public method to manually trigger a scan (useful for when you know UI has changed)
    public void ForceScan()
    {
        ScanAndAddSoundsToAllUIElements();
    }
}

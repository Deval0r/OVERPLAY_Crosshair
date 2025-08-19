using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UISoundHandler : MonoBehaviour
{
    void Start()
    {
        // Add sound effects to all buttons
        Button[] buttons = FindObjectsOfType<Button>();
        foreach (Button button in buttons)
        {
            AddButtonSounds(button);
        }
        
        // Add sound effects to all sliders
        Slider[] sliders = FindObjectsOfType<Slider>();
        foreach (Slider slider in sliders)
        {
            AddSliderSounds(slider);
        }
        
        // Add sound effects to all toggles
        Toggle[] toggles = FindObjectsOfType<Toggle>();
        foreach (Toggle toggle in toggles)
        {
            AddToggleSounds(toggle);
        }
        
        // Add sound effects to all dropdowns
        TMPro.TMP_Dropdown[] dropdowns = FindObjectsOfType<TMPro.TMP_Dropdown>();
        foreach (TMPro.TMP_Dropdown dropdown in dropdowns)
        {
            AddDropdownSounds(dropdown);
        }
    }
    
    private void AddButtonSounds(Button button)
    {
        // Add pointer down event for click sound
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => { 
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick(); 
        });
        trigger.triggers.Add(pointerDown);
        
        // Add pointer up event for release sound
        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => { 
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonRelease(); 
        });
        trigger.triggers.Add(pointerUp);
    }
    
    private void AddSliderSounds(Slider slider)
    {
        // Add onValueChanged event for slider change sound
        slider.onValueChanged.AddListener((value) => {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySliderChange();
        });
        
        // Add pointer up event for slider release sound
        EventTrigger trigger = slider.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = slider.gameObject.AddComponent<EventTrigger>();
        }
        
        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => { 
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySliderRelease(); 
        });
        trigger.triggers.Add(pointerUp);
    }
    
    private void AddToggleSounds(Toggle toggle)
    {
        // Add onValueChanged event for toggle sound
        toggle.onValueChanged.AddListener((value) => {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        });
    }
    
    private void AddDropdownSounds(TMPro.TMP_Dropdown dropdown)
    {
        // Add onValueChanged event for dropdown sound
        dropdown.onValueChanged.AddListener((value) => {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        });
    }
}

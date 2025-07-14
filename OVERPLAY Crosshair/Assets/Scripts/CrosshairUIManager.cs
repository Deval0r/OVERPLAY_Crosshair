using UnityEngine;
using UnityEngine.UI;

public class CrosshairUIManager : MonoBehaviour
{
    public GameObject settingsPanel;
    public KeyCode toggleKey = KeyCode.F1; // Default keybind

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    // Call this from a UI button to change the keybind
    public void SetToggleKey(string keyName)
    {
        if (System.Enum.TryParse(keyName, out KeyCode newKey))
        {
            toggleKey = newKey;
            PlayerPrefs.SetString("CrosshairToggleKey", keyName);
        }
    }

    void Start()
    {
        if (PlayerPrefs.HasKey("CrosshairToggleKey"))
        {
            string keyName = PlayerPrefs.GetString("CrosshairToggleKey");
            if (System.Enum.TryParse(keyName, out KeyCode savedKey))
                toggleKey = savedKey;
        }
    }
}
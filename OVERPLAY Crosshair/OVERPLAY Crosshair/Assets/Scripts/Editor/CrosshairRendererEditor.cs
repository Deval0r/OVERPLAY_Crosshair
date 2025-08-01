using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CrosshairRenderer))]
public class CrosshairRendererEditor : Editor
{
    private CrosshairRenderer crosshairRenderer;
    private bool showHSVModifiers = true; // Always show by default
    
    // HSV values for each theme - these will be loaded from actual theme colors
    private float[] themeHues = new float[9];
    private float[] themeSaturations = new float[9];
    private float[] themeValues = new float[9];
    
    private void OnEnable()
    {
        crosshairRenderer = (CrosshairRenderer)target;
        LoadHSVValuesFromThemes();
    }
    
    private void LoadHSVValuesFromThemes()
    {
        // Load HSV values from the actual theme colors
        for (int i = 0; i < 9; i++)
        {
            var themeColors = GetThemeColorsForTheme((UITheme)i);
            if (themeColors != null)
            {
                // Use the primary color's HSV values as the base
                themeHues[i] = themeColors.primaryHue;
                themeSaturations[i] = themeColors.primarySaturation;
                themeValues[i] = themeColors.primaryValue;
            }
            else
            {
                themeHues[i] = 0f;
                themeSaturations[i] = 1f;
                themeValues[i] = 1f;
            }
        }
    }
    
    public override void OnInspectorGUI()
    {
        // Draw the default inspector but hide ALL theme-related controls
        serializedObject.Update();
        
        // Get the serialized object
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            
            // Skip ALL theme-related properties since we have our own HSV controls
            if (ShouldSkipProperty(iterator.name))
            {
                continue;
            }
            
            EditorGUILayout.PropertyField(iterator, true);
        }
        
        serializedObject.ApplyModifiedProperties();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Theme HSV Controls", EditorStyles.boldLabel);
        
        DrawHSVControls();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Asset Folder Theming", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select Asset Folder", GUILayout.Width(150)))
        {
            SelectAssetFolder();
        }
        
        if (GUILayout.Button("Theme Assets in Folder"))
        {
            ThemeAssetsInFolder();
        }
        
        if (GUILayout.Button("Preview Assets in Folder"))
        {
            PreviewAssetsInFolder();
        }
        EditorGUILayout.EndHorizontal();
        
        // Show current folder path
        var folderPathProperty = serializedObject.FindProperty("assetFolderPath");
        if (folderPathProperty != null && !string.IsNullOrEmpty(folderPathProperty.stringValue))
        {
            EditorGUILayout.LabelField($"Current folder: {folderPathProperty.stringValue}", EditorStyles.miniLabel);
        }
    }
    
    private bool ShouldSkipProperty(string propertyName)
    {
        // Skip ALL theme-related properties
        string[] skipProperties = {
            // Individual HSV controls
            "primaryHue", "primarySaturation", "primaryValue",
            "secondaryHue", "secondarySaturation", "secondaryValue", 
            "accentHue", "accentSaturation", "accentValue",
            "backgroundHue", "backgroundSaturation", "backgroundValue",
            "textHue", "textSaturation", "textValue",
            "sliderFillHue", "sliderFillSaturation", "sliderFillValue",
            "sliderHandleHue", "sliderHandleSaturation", "sliderHandleValue",
            "dropdownHue", "dropdownSaturation", "dropdownValue",
            
            // Theme color objects
            "coralThemeColors", "emberThemeColors", "goldThemeColors", "mossThemeColors", 
            "tideThemeColors", "amethystThemeColors", "lotusThemeColors", "shadowThemeColors", "snowThemeColors",
            
            // Theme base colors
            "coralBaseColor", "emberBaseColor", "goldBaseColor", "mossBaseColor",
            "tideBaseColor", "amethystBaseColor", "lotusBaseColor", "shadowBaseColor", "snowBaseColor",
            
            // Theme settings
            "useCustomColors", "isMasterTheme",
            
            // Generated colors (read-only)
            "primaryColor", "secondaryColor", "accentColor", "backgroundColor", "textColor",
            "sliderFillColor", "sliderHandleColor", "dropdownColor"
        };
        
        return System.Array.Exists(skipProperties, prop => propertyName.Contains(prop));
    }
    
    private void DrawHSVControls()
    {
        EditorGUILayout.BeginVertical("box");
        
        string[] themeNames = { "Coral", "Ember", "Gold", "Moss", "Tide", "Amethyst", "Lotus", "Shadow", "Snow" };
        
        for (int i = 0; i < themeNames.Length; i++)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(themeNames[i], EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Hue", GUILayout.Width(30));
            float newHue = EditorGUILayout.Slider(themeHues[i], 0f, 1f, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sat", GUILayout.Width(30));
            float newSaturation = EditorGUILayout.Slider(themeSaturations[i], 0f, 1f, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Val", GUILayout.Width(30));
            float newValue = EditorGUILayout.Slider(themeValues[i], 0f, 1f, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();
            
            // Check if values changed
            if (newHue != themeHues[i] || newSaturation != themeSaturations[i] || newValue != themeValues[i])
            {
                themeHues[i] = newHue;
                themeSaturations[i] = newSaturation;
                themeValues[i] = newValue;
                
                // Apply HSV values directly to the theme
                ApplyHSVValuesToTheme((UITheme)i, newHue, newSaturation, newValue);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void ApplyHSVValuesToTheme(UITheme theme, float hue, float saturation, float value)
    {
        var themeColors = GetThemeColorsForTheme(theme);
        if (themeColors == null) return;
        
        // Set useCustomColors to true to prevent overrides
        themeColors.useCustomColors = true;
        
        // Apply the HSV values directly to all color types in the theme
        themeColors.primaryHue = hue;
        themeColors.primarySaturation = saturation;
        themeColors.primaryValue = value;
        
        themeColors.secondaryHue = hue;
        themeColors.secondarySaturation = saturation;
        themeColors.secondaryValue = value;
        
        themeColors.accentHue = hue;
        themeColors.accentSaturation = saturation;
        themeColors.accentValue = value;
        
        themeColors.backgroundHue = hue;
        themeColors.backgroundSaturation = saturation;
        themeColors.backgroundValue = value;
        
        themeColors.textHue = hue;
        themeColors.textSaturation = saturation;
        themeColors.textValue = value;
        
        themeColors.sliderFillHue = hue;
        themeColors.sliderFillSaturation = saturation;
        themeColors.sliderFillValue = value;
        
        themeColors.sliderHandleHue = hue;
        themeColors.sliderHandleSaturation = saturation;
        themeColors.sliderHandleValue = value;
        
        themeColors.dropdownHue = hue;
        themeColors.dropdownSaturation = saturation;
        themeColors.dropdownValue = value;
        
        // Generate the RGB colors from HSV
        themeColors.GenerateColorsFromHSV();
        
        // Save HSV values to storage
        string themeName = GetThemeName(theme);
        SaveThemeHSVToStorage(themeName, themeColors);
        
        // Mark the object as dirty so changes are saved
        EditorUtility.SetDirty(crosshairRenderer);
        
        // Force the scene to update
        if (Application.isPlaying)
        {
            // If in play mode, force the theme to be reapplied
            var applyMethod = typeof(CrosshairRenderer).GetMethod("OnUIThemeChanged", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (applyMethod != null)
            {
                var currentThemeField = typeof(CrosshairRenderer).GetField("currentUITheme", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (currentThemeField != null)
                {
                    var currentTheme = (UITheme)currentThemeField.GetValue(crosshairRenderer);
                    applyMethod.Invoke(crosshairRenderer, new object[] { (int)currentTheme });
                }
            }
        }
    }
    
    private UIThemeColors GetThemeColorsForTheme(UITheme theme)
    {
        string fieldName = "";
        switch (theme)
        {
            case UITheme.Coral:
                fieldName = "coralThemeColors";
                break;
            case UITheme.Ember:
                fieldName = "emberThemeColors";
                break;
            case UITheme.Gold:
                fieldName = "goldThemeColors";
                break;
            case UITheme.Moss:
                fieldName = "mossThemeColors";
                break;
            case UITheme.Tide:
                fieldName = "tideThemeColors";
                break;
            case UITheme.Amethyst:
                fieldName = "amethystThemeColors";
                break;
            case UITheme.Lotus:
                fieldName = "lotusThemeColors";
                break;
            case UITheme.Shadow:
                fieldName = "shadowThemeColors";
                break;
            case UITheme.Snow:
                fieldName = "snowThemeColors";
                break;
        }
        
        var field = typeof(CrosshairRenderer).GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (UIThemeColors)field.GetValue(crosshairRenderer);
        }
        return null;
    }
    
    private string GetThemeName(UITheme theme)
    {
        switch (theme)
        {
            case UITheme.Coral: return "Coral";
            case UITheme.Ember: return "Ember";
            case UITheme.Gold: return "Gold";
            case UITheme.Moss: return "Moss";
            case UITheme.Tide: return "Tide";
            case UITheme.Amethyst: return "Amethyst";
            case UITheme.Lotus: return "Lotus";
            case UITheme.Shadow: return "Shadow";
            case UITheme.Snow: return "Snow";
            default: return "Coral";
        }
    }
    
    private void SaveThemeHSVToStorage(string themeName, UIThemeColors themeColors)
    {
        string prefix = $"ThemeHSV_{themeName}_";
        
        // Save HSV values to PlayerPrefs
        PlayerPrefs.SetFloat(prefix + "Hue", themeColors.primaryHue);
        PlayerPrefs.SetFloat(prefix + "Saturation", themeColors.primarySaturation);
        PlayerPrefs.SetFloat(prefix + "Value", themeColors.primaryValue);
        
        PlayerPrefs.Save();
    }
    
    private void SelectAssetFolder()
    {
        string folderPath = EditorUtility.OpenFolderPanel("Select Asset Folder to Theme", "Assets", "");
        
        if (!string.IsNullOrEmpty(folderPath))
        {
            // Convert absolute path to relative path
            string relativePath = "Assets" + folderPath.Substring(Application.dataPath.Length);
            
            var folderPathProperty = serializedObject.FindProperty("assetFolderPath");
            if (folderPathProperty != null)
            {
                folderPathProperty.stringValue = relativePath;
                serializedObject.ApplyModifiedProperties();
                Debug.Log($"Selected asset folder: {relativePath}");
            }
        }
    }
    
    private void ThemeAssetsInFolder()
    {
        if (crosshairRenderer != null)
        {
            var method = typeof(CrosshairRenderer).GetMethod("ApplyThemeToAssetFolder", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                // Get current theme colors
                var getCurrentColorsMethod = typeof(CrosshairRenderer).GetMethod("GetCurrentThemeColors", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (getCurrentColorsMethod == null)
                {
                    // If that method doesn't exist, create colors from current theme
                    var currentThemeField = typeof(CrosshairRenderer).GetField("currentUITheme", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var paletteField = typeof(CrosshairRenderer).GetField("themeColorPalettes", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (currentThemeField != null && paletteField != null)
                    {
                        var currentTheme = currentThemeField.GetValue(crosshairRenderer);
                        var palettes = paletteField.GetValue(crosshairRenderer);
                        
                        if (palettes != null)
                        {
                            var paletteDict = palettes as System.Collections.IDictionary;
                            if (paletteDict != null && paletteDict.Contains(currentTheme))
                            {
                                var colors = paletteDict[currentTheme];
                                method.Invoke(crosshairRenderer, new object[] { colors });
                                Debug.Log("Applied theme to assets in selected folder.");
                            }
                        }
                    }
                }
                else
                {
                    var colors = getCurrentColorsMethod.Invoke(crosshairRenderer, null);
                    method.Invoke(crosshairRenderer, new object[] { colors });
                    Debug.Log("Applied theme to assets in selected folder.");
                }
            }
        }
    }
    
    private void PreviewAssetsInFolder()
    {
        var folderPathProperty = serializedObject.FindProperty("assetFolderPath");
        if (folderPathProperty == null || string.IsNullOrEmpty(folderPathProperty.stringValue))
        {
            EditorUtility.DisplayDialog("No Folder Selected", "Please select an asset folder first.", "OK");
            return;
        }
        
        string folderPath = folderPathProperty.stringValue;
        var includeSubfoldersProperty = serializedObject.FindProperty("includeSubfolders");
        bool includeSubfolders = includeSubfoldersProperty?.boolValue ?? true;
        
        string[] searchFolders = { folderPath };
        string[] assetGUIDs = AssetDatabase.FindAssets("", searchFolders);
        
        System.Text.StringBuilder preview = new System.Text.StringBuilder();
        preview.AppendLine($"Assets in folder: {folderPath}");
        preview.AppendLine($"Include subfolders: {includeSubfolders}");
        preview.AppendLine();
        
        int materialCount = 0;
        int prefabCount = 0;
        int otherCount = 0;
        
        foreach (string guid in assetGUIDs)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            
            // Skip if not including subfolders and asset is in a subfolder
            if (!includeSubfolders && assetPath.Replace(folderPath + "/", "").Contains("/"))
            {
                continue;
            }
            
            System.Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            
            if (assetType == typeof(Material))
            {
                materialCount++;
                preview.AppendLine($"[Material] {assetPath}");
            }
            else if (assetType == typeof(GameObject))
            {
                prefabCount++;
                preview.AppendLine($"[Prefab] {assetPath}");
            }
            else
            {
                otherCount++;
                preview.AppendLine($"[{assetType.Name}] {assetPath}");
            }
        }
        
        preview.AppendLine();
        preview.AppendLine($"Summary: {materialCount} materials, {prefabCount} prefabs, {otherCount} other assets");
        preview.AppendLine("Materials and prefabs will be themed.");
        
        EditorUtility.DisplayDialog("Asset Preview", preview.ToString(), "OK");
    }
}
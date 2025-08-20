# UI Dissolve Effect

This system provides a shader-based dissolve effect for UI elements, creating a smooth grid-based transition when showing/hiding UI components.

## Features

- **Shader-based dissolve effect** with customizable grid pattern
- **Supports multiple UI element types**:
  - Unity UI Images
  - TextMeshPro text elements
  - SpriteRenderer components
- **Custom sound effects** for UI opening and closing
- **Smooth animations** with configurable duration and easing
- **Automatic material management** and restoration

## Setup Instructions

### 1. Add the UIDissolveEffect Component

1. Select your UI root GameObject in the hierarchy
2. Add the `UIDissolveEffect` component via `Add Component > Scripts > UIDissolveEffect`
3. The component will automatically add a `CanvasGroup` if one doesn't exist

### 2. Configure the Effect

In the Inspector, you can configure:

#### Dissolve Settings
- **Dissolve Duration**: How long the animation takes (default: 2.0 seconds)
- **Grid Size**: Number of grid cells for the dissolve pattern (default: 16x12)
- **Dissolve Speed**: Speed multiplier for the effect (default: 1.0)
- **Dissolve Direction**: Center point for the dissolve effect (default: 0.5, 0.5)

#### Sound Settings ⭐ **WHERE TO ASSIGN SOUNDS**
- **UI Open Sound**: Drag any AudioClip here to play when the UI opens
- **UI Close Sound**: Drag any AudioClip here to play when the UI closes
- **Sound Volume**: Volume level for the sound effects (0.0 to 1.0)

> **💡 How to assign sounds:**
> 1. In the Inspector, find the `UIDissolveEffect` component
> 2. Expand the **"Sound Settings"** section
> 3. Drag your AudioClip files from the Project window into the **"UI Open Sound"** and **"UI Close Sound"** fields
> 4. Adjust the **"Sound Volume"** slider as needed

#### References
- **Dissolve Shader**: The shader to use for the effect (auto-detected if not assigned)

### 3. Update Your Code

Replace your existing UI show/hide logic with calls to the dissolve effect:

```csharp
// Get the UIDissolveEffect component
UIDissolveEffect dissolveEffect = uiRoot.GetComponent<UIDissolveEffect>();

// Show UI with dissolve effect
dissolveEffect.DissolveIn();

// Hide UI with dissolve effect
dissolveEffect.DissolveOut();
```

## How It Works

### Element Detection
The system automatically detects and applies the effect to:
- All `Image` components (UI panels, buttons, etc.)
- All `TextMeshProUGUI` components (text elements)
- All `TMP_Text` components (any TextMeshPro variant)
- All `Text` components (legacy Unity UI text)
- All `SpriteRenderer` components (2D sprites)

### Dual Effect System
For maximum compatibility, the system uses two approaches:
1. **Shader-based dissolve**: Applies the custom shader to create the grid dissolve effect
2. **Alpha fade**: As a backup, elements also get their alpha color faded during animation

### Sound Integration
- Custom sounds play automatically when `DissolveIn()` or `DissolveOut()` is called
- Uses `AudioSource.PlayClipAtPoint()` for immediate playback
- Falls back to `AudioManager` if no custom sounds are assigned

## Advanced Usage

### Programmatic Configuration

You can configure the effect at runtime:

```csharp
UIDissolveEffect effect = GetComponent<UIDissolveEffect>();

// Adjust dissolve settings
effect.SetGridSize(new Vector2(20, 15));
effect.SetDissolveDirection(new Vector2(0.5f, 0.5f));
effect.SetDissolveDuration(1.5f);

// Set custom sounds
effect.SetUIOpenSound(openSoundClip);
effect.SetUICloseSound(closeSoundClip);
effect.SetSoundVolume(0.8f);
```

### Material Management
The system automatically:
- Stores original materials and colors for all elements
- Applies the dissolve shader during animations
- Restores original materials when needed
- Cleans up instance materials on destruction

## Troubleshooting

### Effect Not Visible
1. Check that the shader "Custom/UIDissolveShader" is available in your project
2. Verify that UI elements have the correct components (Image, TextMeshProUGUI, or SpriteRenderer)
3. Check the Console for debug messages about material application

### Child Text Not Fading
1. The system now detects all text types including deeply nested ones
2. Check the Console for debug messages showing how many text elements were found
3. If text still doesn't fade, it might be using a custom shader that overrides the effect

### Sounds Not Playing
1. **Make sure you've assigned AudioClips in the Inspector** under "Sound Settings"
2. Check that the sound volume is not set to 0
3. Verify that the AudioClips are properly imported and not corrupted
4. Check the Console for "Played UI open/close sound" messages

### Performance Issues
1. Reduce the Grid Size for better performance
2. Consider reducing the number of UI elements affected
3. Use shorter dissolve durations for faster transitions

## File Structure

- `UIDissolveEffect.cs` - Main component script
- `UIDissolveShader.shader` - Custom shader for the dissolve effect
- `UIDissolveSetup.cs` - Utility script for easy setup
- `README_DissolveEffect.md` - This documentation file

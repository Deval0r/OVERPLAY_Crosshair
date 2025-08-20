# UI Dissolve Effect Setup Guide

This guide explains how to set up the shader-based dissolve effect for UI elements.

## Overview

The dissolve effect creates a grid-based transparency effect that makes UI elements appear to dissolve in and out using squares that become transparent. This replaces the previous GameObject-based block animation.

## Files Created

1. **UIDissolveShader.shader** - The shader that handles the dissolve effect
2. **UIDissolveEffect.cs** - Script that manages the dissolve animation
3. **UIDissolveSetup.cs** - Utility script for easy setup

## Setup Instructions

### Method 1: Automatic Setup (Recommended)

1. Add the `UIDissolveSetup` component to your UI root GameObject
2. Configure the settings in the inspector:
   - **Grid Size**: Number of squares (default: 16x12)
   - **Dissolve Duration**: Animation duration in seconds (default: 0.4)
   - **Dissolve Direction**: Center point for the effect (default: 0.5, 0.5)
3. The script will automatically add the required components on Start

### Method 2: Manual Setup

1. Add a `CanvasGroup` component to your UI root GameObject
2. Add the `UIDissolveEffect` component
3. Assign the shader in the inspector (or let it auto-detect)
4. Configure the dissolve settings

## Integration with CrosshairRenderer

The `CrosshairRenderer.cs` has been updated to automatically use the dissolve effect if the `UIDissolveEffect` component is present on the UI root. If not found, it falls back to simple show/hide.

## Shader Properties

- **Dissolve Amount**: Controls the dissolve progress (0 = fully dissolved, 1 = fully visible)
- **Grid Size**: Number of squares in the grid
- **Dissolve Speed**: Animation speed multiplier
- **Dissolve Direction**: Center point for the dissolve effect

## Usage

Once set up, the dissolve effect will automatically be used when:
- Opening the UI (dissolve in)
- Closing the UI (dissolve out)

The effect works with the existing audio system, so opening/closing sounds will still play.

## Troubleshooting

1. **Shader not found**: Make sure `UIDissolveShader.shader` is in the Assets/Shaders folder
2. **No effect visible**: Check that the UI root has a `CanvasGroup` component
3. **Performance issues**: Reduce the grid size for better performance

## Customization

You can modify the shader to change the visual effect:
- Adjust the noise function for different dissolve patterns
- Change the distance calculation for different dissolve directions
- Modify the grid calculation for different square sizes

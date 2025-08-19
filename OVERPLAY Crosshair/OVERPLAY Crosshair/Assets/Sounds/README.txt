Sound Effects

This directory is for sound effect files. Currently, the application generates simple tone-based sound effects programmatically for:

- Button clicks (800Hz tone)
- Button releases (600Hz tone) 
- Slider changes (400Hz tone)
- Slider releases (500Hz tone)

If you want to use custom sound files, you can:
1. Add .wav or .mp3 files to this directory
2. Assign them in the AudioManager component in the scene
3. The programmatically generated sounds will be overridden

The sound effects are automatically applied to all UI elements (buttons, sliders, toggles, dropdowns) when the application starts.

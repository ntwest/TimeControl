Version 2.9.6 (KSP 1.6.1)
- KSP 1.6.1 Recompile

Version 2.9.5 (KSP 1.5.1)
- KSP 1.5.1 Recompile
- Allow warp directly from PAUSE when paused via TimeControl
- Fix IndexOutOfRange exception on scene changes

Version 2.9.4 (KSP 1.4.5)
- KSP 1.4.5 Recompile

Version 2.9.3 (KSP 1.4.4)
- KSP 1.4.4 Recompile

Version 2.9.2 (KSP 1.4.3)
- KSP 1.4.3 Recompile

Version 2.9.1 (KSP 1.4.2)
- KSP 1.4.2 Recompile

Version 2.9 (KSP 1.4.1)
- KSP 1.4.1 Compatibility
- Removed MiniAVC (kept Version file)

Version 2.8.3 (KSP 1.3.1)
- Automatically reset maximumDeltaTime to the PHYSICS_FRAME_DT_LIMIT after physics warp bug enhancement
- Fixed Physics Accuracy +/- not working correctly on slider
- Fixed Hyper-Warp/Slow Mo Screen messages do not include stats when GUI is disabled (toggle performance stats on ? screen)
- Hyper Warp now allows you to set very high MaximumDeltaTime values that only apply during hyper warp. This will reduce your frame rate but possibly speed up Hyper Warp a little bit so long as the engine is not physics-limited.

Version 2.8.2 (KSP 1.3.1)
- Massive Keybindings update for the Discerning Gamer.
- Bind nearly every TimeControl action to a hotkey or key combination. Bind multiple actions to the same set of keys e.g. (activate hyper warp + set rate to 8). Key binds for quick warp to specific times / orbit locations.
- Also includes a bug fix to resource converters at high time warps using code graciously provided by @linuxgurugamer.

Version 2.8.1 (KSP 1.3.1)
- Release for KSP 1.3.1. Fixed version file and compile issue with prior 2.8 release.

Version 2.7 (KSP 1.3)
- Major Refactoring. Updated GUI, Settings, added quickwarp and warp-to tabs.
- KSP 1.3 compatibility.

Version 2.5 (KSP 1.2.2)
- Added back MiniAVC
- Thanks to @linuxgurugamer who updated for KSP 1.2.2. Also fixed up the close button on the window so it closes instead of minimizing.

Version 2.4 (KSP 1.2.1)

- KSP 1.2.1 Compatibility.
- MiniAVC removed until AVC is updated for 1.2.1.
- Fixes bug for "Homeworld Time" breaking RSS Time Formatter. Will rethink this feature...

Version 2.3.1 (KSP 1.2)
- Fixes toolbar integration code

Version 2.3 (KSP 1.2)

- KSP 1.2 Compatibility.
- New KSP 1.2 version of MiniAVC.
- Fixes bug where debris leaving the physics area causes hyper warp to cut out on current vessel.
- Fixes bug where it was saving the game settings in the editor view, which was breaking editor extensions redux. Thanks to linuxgurugamer.

Version 2.2.2 (KSP 1.1.3)
- 2.2.1 had a slow-motion bug when returning to 1x. Corrected.

Version 2.2.1 (KSP 1.1.3)
- Bug fix: TC will no longer interfere with the stock warp to on rails.
- Remove some string concatenation from the OnGUI calls.

Version 2.2 (KSP 1.1.3)

- Logging mode now is properly set up with the settings file as well as in the Settings GUI. Defaulting to INFO mode.
- Save warp rates, altitudes, window positions, and settings every X seconds is now in the Settings GUI as well. Defaults to 5 seconds. Note that if you have made no changes, it does not perform a save.
- "Homeworld Timekeeping" option added. If you have modified things with Kopernicus and this is turned on, "Use Kerbin Time" will set up your calendar so that 1 day = 1 sidereal day, and 1 year = 1 sidereal year.

Version 2.1 (KSP 1.1.3)

- Screen Messages now stay on until you change warp. They can be toggled off in the settings menu.
- Replaced label at top right of TC window with a button that cancels the warp type as well as cancelling PAUSE.
- Auto Rails Warp now uses custom code, it appears to be very accurate.
- Auto Rails warp now has the ability to pause when you reach the time requested.
- Rails warp when you first go to the flight window now uses the correct warp rates.
- Logging mode now is properly set up with the settings file. Defaulting to INFO mode.
- Corrected issue when changing a scene, if you had time paused, it would break the game.


Version 2.0.2 (KSP 1.1.3)

- New Icon from @Avera9eJoe
- Settings window is available from the space center as well as in flight.
- Rails Warp
 - Bugs around rails warp should be somewhat corrected, leveraging the new builtin rails warp to function
- Hyper Warp
 - Hyper warp accuracy now scales to 1/6 (Physics delta time of .12 seconds). Using this, I was able to launch the Stock ship "ComSat LX" to 500km in about a second.
 - A new box has been added to the left of the slider for the accuracy, it can go from 1 to 6. This is the multiplier that is applied to the internal Fixed Delta Time settings in Unity.
- Slow Motion
 - FPS Keeper is now controlled from the Slow-Motion tab.
- Keybindings
 - Work again. Speed Up/Slow Down work a bit differently - if you are Hyper Warping, they increment/decrement the hyper warp max value. Otherwise they work with slow-motion as usual.
 - Assign a binding by left clicking the button, then pressing the key combination. Clear a binding by right clicking the button.
 - You can bind a key combination to a key binding instead of a single key. Note that for most bindings the final key pressed 'down' is what triggers the key.
 - If you bind keys that are also used by KSP, you will be triggering both your stuff and the KSP keys.
- I did a massive code refactoring for hopeful performance improvement. Also added a ton of instrumentation, which by default is mostly turned off (but for bugs it will be invaluable).
- FPS display is gone (for now). I added FPS in the header of the TimeControl Window, along with the current warp rate, which now shows time as a % for Hyper/Slow-Mo and a 1x-100x etc for Rails.
- CamFix removed (it was causing problems with other mods). Please let me know if you experience any issues or used this feature.
- Config file is sane now, handling Celestial Bodies by Name instead of 0,1,2,3,4 etc.
- Version checking using MiniAVC
- Removed FPS toggle
- Toggle Time Control-generated on-screen messages. Sorry, normal ones will still appear.
- Rails auto warp will use hyper warp if you are 'FLYING' in an atmosphere. It won't automatically change to rails warp when you leave the atmosphere (this is a todo)

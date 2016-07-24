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

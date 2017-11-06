
TIME CONTROL



MAIN WINDOW

The pause button freezes physics, and allows you to step forward one physics frame at a time, for even more precise control.

DETAILS (?)
Displays some detailed statistics about the current game state. Also allows you to change the max delta time slider.

QUICK WARP
A selection of buttons to immediately warp for a specific amount of seconds/minutes/hours/days/years/orbits, or to a vessel's Ap/Pe/AN/DN/SOI/Manuver Node and then stop.

WARP-TO UT
Has a similar selection of buttons to Quick Warp, but these add/subtract from a Target UT value, to allow you to specify exactly the time. Left click adds to the target UT, Right click subtracts (like the PreciseNode mod does). The Vessel buttons reset the Target UT directly to that point. Also has fields where a user can enter an exact amount of years, days, hours, minutes, and seconds

RAILS EDITOR
The rails editor gives you complete control over every part of rails warping. You can fully customize the warp rates available, as well as add more if you like, as well as set the altitude limits for each warp level for each celestial body. There are several buttons that set these to defaults which make time warping a breese. Warp Rates and Altitude limits are now set per savegame, not globally.

Buttons that setup warp rates:
Kerbin Time: 5s/s 15s/s, 45s/s, 60s/s, 1m/s, 5m/s, 15m/s, 45m/s, 1hr/s, 3hr/s, 1d/s, 5ds/s, 15d/s, 45d/s, 135d/s, 1yr/s
(Note that Kerbin-Days are 21650.8s, And Kerbin-Years are 9203544.6s, but since rate is stored as a 32bit floating point value, I use 9203544x for the 1yr/s rate.
Earth Time: 5s/s 15s/s, 45s/s, 60s/s, 1m/s, 5m/s, 15m/s, 45m/s, 1hr/s, 3hr/s, 12hr/s, 1d/s, 5d/s, 15d/s, 30d/s, 90d/s, 180d/s, 1yr/s

KEY BINDINGS
Assign a binding by left clicking the button, then pressing the key combination. Clear a binding by right clicking the button. You can bind a key combination to a key binding instead of a single key. Note that for most bindings the final key pressed 'down' is what triggers the key. If you bind keys that are also used by KSP, you will be triggering both your stuff and the KSP keys. This should support joystick buttons but I have no joystick to test with.

Toggle GUI - toggles the GUI window(s)
Realtime: ends all warp and returns to realtime
Pause - Stops or Resumes time.
Step - Run until next physics update.
Speed Up / Slow Down - If in Slow-Motion, functions to slow down or speed up time. If in Hyper Warp, increments/decrements the max hyper warp rate by 1.
Slow Motion - Toggles Slow Motion on/off
Hyper Warp - Toggles Hyper Warp on/off

HYPER-WARP
The hyper warp menu gives you the ability to speed up time without sacrificing physics accuracy like phys-warp does. This has a myriad of uses, like speeding up launches or burns (especially with lower TWR craft), flying planes around the world, running Kerbals long distances, etc. The first slider sets the maximum attempted speed - note that it is unlikely that you will be able to attain that speed unless you have a very powerful computer or a very small craft. The second slider sets the minimum accuracy, from 1 to 1/6. If you know your ship can hold together in phys-warp, you can reduce the accuracy to attain better speeds and FPS. You can either manually control when hyper warp is active, or you can set it to warp for a period of time, which is particularly useful for long burns with ION or Nuclear engines. If you like, have it pause when it finishes so you can AFK while it goes and not worry about missing anything. Also provided in the hyper warp window is a throttle slider, so you can precisely control your throttle even when the standard throttle response is sped up.


SLOW-MOTION
The slow motion menu gives you the ability to slow down time, or completely pause time and step forward frame by frame. By default, the slider slows down both the game speed and the physics delta, resulting in a smooth slow motion. Note that this will change how the physics of your vessel behaves, joints will stiffen and become more rigid (the opposite of what happens in phys-warp). This can sometimes cause problems with launch clamps or clipped parts, so beware. If you flip on the "lock physics delta" option, time will slow down, but the physics calculations won't change, so parts will appear to stutter and motion will be choppy. This allows you to see how your ship behaves at a much slower pace, so you can determine what might be going wrong. It is also very useful because even with high part counts where your computer is struggling with physics calculations, your frame-rate will NOT be slowed, so you can maintain full control of your camera, parts on your ship, and anything else.  Also provided in the slow-motion window is a throttle slider, so you can precisely control your throttle even when the standard throttle response is slowed.




KERBAL ALARM CLOCK INTEGRATION
Both Quick Warp and Warp-To UT are integrated with KAC to warp to the upcoming KAC alarm. Recommendation is to disable the auto-warp stop in KAC, as the TimeControl one is faster and will jump nearly instantaneously if you have a good amount of warp rates spaced out mostly evenly (I recommend using the 'standard kerbin time' settings).




Features NOT enabled in current release:
FPS-Keeper 
The FPS Keeper is a handy little tool that can automatically manage your settings based on your computer's performance and edit the maxDeltaTime and slow your time rate to keep your FPS above a limit while maximizing the time rate of your game. Note that is is still very beta technology, it isn't perfect and it won't work for every situation. Usage is fairly straight forward, but performance is highly dependent on your computer. First off, determine what kind of FPS your computer gets with a very small craft. You shouldn't set it above that because it will just slam you down to 1/64x trying to boost your FPS hopelessly. Also, note that you should give a margin for error, if you run at 60 fps, set it to 50-55, as 60 will just confuse it. Second, play around with what kind of minimum FPS you can stand, trying to set it too high will just end up making everything take ages. The main idea of it is that it can adjust on the fly to changing conditions without the typical FPS spikes and drops, so you can for example fly around in orbit at good FPS, and then when you approach your station, it will automatically account for the higher load and slow down time a bit while you dock, or have it automatically adjust the speed as your massive lifter drops off stages keeping you at the best speed without having to deal with terrible fps. Also note that results per computer may be drastically different. A computer that struggles to get 30 fps won't be able to keep the FPS when the load gets high no matter what it does, and whether your computer is CPU or GPU limited also can have a large effect.


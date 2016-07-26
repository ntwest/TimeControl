using System;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
using KSP.UI.Dialogs;
using KSPPluginFramework;

namespace TimeControl
{    
    /// <summary>
    /// Time control input manager
    /// </summary>
    [KSPAddon( KSPAddon.Startup.MainMenu, true )]
    internal class KeyboardInputManager : MonoBehaviour
    {
        #region Singleton
        private static KeyboardInputManager instance;
        internal static KeyboardInputManager Instance { get { return instance; } }
        #endregion
        
        // Considered a HashSet for these...Did some performance calcs for common situations (a few adds and lots of "contains") - lists perform better!
        private List<KeyCode> keysPressed;
        private List<KeyCode> keysPressedDown;
        private List<KeyCode> keysReleased;

        #region MonoBehavior

        private void Awake()
        {
            string logCaller = "KeyboardInputManager.Awake()";
            Log.Trace( "method start", logCaller );

            DontDestroyOnLoad( this ); //Don't go away on scene changes
            instance = this;

            Log.Trace( "method end", logCaller );
        }

        private void Start()
        {
            string logCaller = "KeyboardInputManager.Start()";
            Log.Trace( "method start", logCaller );

            keysPressed = new List<KeyCode>();
            keysPressedDown = new List<KeyCode>();
            keysReleased = new List<KeyCode>();

            UpdateKSPKeyBindings();

            Log.Trace( "method end", logCaller );
        }

        private void Update()
        {
            // Don't do anything until the Settings, TimeController, and TCGUI Objects are ready
            if (!Settings.IsReady || !TimeController.IsReady || !TCGUI.IsReady)
                return;            
            // Only check for keypresses when we can actually do something
            if (TimeController.Instance.CanControlWarpType == TimeControllable.None)
                return;

            // Only run during this frame if a key is actually pressed down
            if (Input.anyKey)
            {
                LoadKeyPresses();
                CheckKeyBindings();
            }
            else
            {
                keysPressed.Clear();
                keysPressedDown.Clear();
                keysReleased.Clear();
            }
        }

        #endregion

        private void UpdateKSPKeyBindings()
        {
            // TODO UpdateKSPKeyBindings. Maybe make a full fledged input manager

            /*

            GameSettings.AbortActionGroup;
            GameSettings.BRAKES;
            GameSettings.CAMERA_MODE;
            GameSettings.CAMERA_MOUSE_TOGGLE;
            GameSettings.CAMERA_NEXT;
            GameSettings.CAMERA_ORBIT_DOWN;
            GameSettings.CAMERA_ORBIT_LEFT;
            GameSettings.CAMERA_ORBIT_RIGHT;
            GameSettings.CAMERA_ORBIT_UP;
            GameSettings.CAMERA_RESET;
            GameSettings.CustomActionGroup1; // to 10
            GameSettings.Docking_toggleRotLin;
            GameSettings.EVA_back;
            GameSettings.EVA_Board;
            GameSettings.EVA_forward;
            GameSettings.EVA_Jump;
            GameSettings.EVA_left;
            GameSettings.EVA_Lights;
            GameSettings.EVA_Orient;
            GameSettings.EVA_Pack_back;
            GameSettings.EVA_Pack_down;
            GameSettings.EVA_Pack_forward;
            GameSettings.EVA_Pack_left;
            GameSettings.EVA_Pack_right;
            GameSettings.EVA_Pack_up;
            GameSettings.EVA_right;
            GameSettings.EVA_Run;
            GameSettings.EVA_ToggleMovementMode;
            GameSettings.EVA_TogglePack;
            GameSettings.EVA_Use;
            GameSettings.EVA_yaw_left;
            GameSettings.EVA_yaw_right;
            GameSettings.FOCUS_NEXT_VESSEL;
            GameSettings.FOCUS_PREV_VESSEL;
            GameSettings.HEADLIGHT_TOGGLE;
            GameSettings.LANDING_GEAR;
            GameSettings.LAUNCH_STAGES;
            GameSettings.MAP_VIEW_TOGGLE;
            GameSettings.MODIFIER_KEY;
            GameSettings.NAVBALL_TOGGLE;
            GameSettings.PAUSE;
            GameSettings.PITCH_DOWN;
            GameSettings.PITCH_UP;
            GameSettings.PRECISION_CTRL;
            GameSettings.QUICKLOAD;
            GameSettings.QUICKSAVE;
            GameSettings.RCS_TOGGLE;
            GameSettings.ROLL_LEFT;
            GameSettings.ROLL_RIGHT;
            GameSettings.SAS_HOLD;
            GameSettings.SAS_TOGGLE;
            GameSettings.SCROLL_ICONS_DOWN;
            GameSettings.SCROLL_ICONS_UP;
            GameSettings.SCROLL_VIEW_DOWN;
            GameSettings.SCROLL_VIEW_UP;
            GameSettings.TAKE_SCREENSHOT;
            GameSettings.THROTTLE_CUTOFF;
            GameSettings.THROTTLE_DOWN;
            GameSettings.THROTTLE_FULL;
            GameSettings.THROTTLE_UP;
            GameSettings.TIME_WARP_DECREASE;
            GameSettings.TIME_WARP_INCREASE;
            GameSettings.TIME_WARP_STOP;
            GameSettings.TOGGLE_FLIGHT_FORCES;



            */



            //        if (HighLogic.LoadedScene == GameScenes.FLIGHT && GameSettings.SAS_TOGGLE.primary == KeyCode.T)
            //       {
            //              FlightGlobals.ActiveVessel.ActionGroups.ToggleGroup( KSPActionGroup.SAS );
            //            }
        }

        private void LoadKeyPresses()
        {
            // Save us from the Mono Garbarge Collector, oh Lord!
            keysPressed.Clear();
            keysPressedDown.Clear();
            keysReleased.Clear();

            foreach (KeyCode kc in System.Enum.GetValues( typeof( KeyCode ) ))
            {
                // Ignore mouse button presses
                if (kc == KeyCode.Mouse0
                    || kc == KeyCode.Mouse1
                    || kc == KeyCode.Mouse2
                    || kc == KeyCode.Mouse3
                    || kc == KeyCode.Mouse4
                    || kc == KeyCode.Mouse5
                    || kc == KeyCode.Mouse6)
                    continue;

                KeyCode setKC = kc;

                // Ignore Some Keys
                if (kc == KeyCode.Escape
                    || kc == KeyCode.CapsLock
                    || kc == KeyCode.LeftWindows
                    || kc == KeyCode.RightWindows
                    || kc == KeyCode.LeftApple
                    || kc == KeyCode.RightApple)
                    continue;


                // Modifier Keys
                switch (kc)
                {
                    case KeyCode.LeftShift:
                    case KeyCode.RightShift:
                        setKC = KeyCode.LeftShift;
                        break;
                    case KeyCode.LeftControl:
                    case KeyCode.RightControl:
                        setKC = KeyCode.LeftControl;
                        break;
                    case KeyCode.LeftAlt:
                    case KeyCode.RightAlt:
                    case KeyCode.AltGr:
                        setKC = KeyCode.LeftAlt;
                        break;
                    case KeyCode.LeftCommand:
                    case KeyCode.RightCommand:
                        setKC = KeyCode.LeftCommand;
                        break;
                }

                if (Input.GetKey( kc ) && !keysPressed.Contains( setKC ) )
                    keysPressed.Add( setKC );

                if (Input.GetKeyDown( kc ) && !keysPressedDown.Contains( setKC ))
                    keysPressedDown.Add( setKC );

                if (Input.GetKeyUp( kc ) && !keysReleased.Contains( setKC ))
                    keysReleased.Add( setKC );
            }
        }

        private void CheckKeyBindings()
        {
            foreach (TCKeyBinding k in Settings.Instance.KeyBinds)
            {
                // Only if the keys we care about (and no others) are pressed. Otherwise continue the loop
                if (!
                    (k.KeyCombination.Count() != 0
                    && k.KeyCombination.All( x => keysPressed.Contains( x ) )
                    && k.KeyCombination.Count() == keysPressed.Count())
                   )
                    continue;
                
                switch (k.TCUserAction)
                {
                    case TimeControlUserAction.SpeedUp:
                        SpeedUpKeyPress( k );
                        break;
                    case TimeControlUserAction.SlowDown:
                        SlowDownKeyPress( k );
                        break;
                    case TimeControlUserAction.Realtime:
                        RealtimeKeyPress( k );
                        break;
                    case TimeControlUserAction.SlowMo64:
                        SlowMo64KeyPress( k );
                        break;
                    case TimeControlUserAction.CustomKeySlider:
                        CustomKeySliderKeyPress( k );
                        break;
                    case TimeControlUserAction.Pause:
                        PauseKeyPress( k );
                        break;
                    case TimeControlUserAction.Step:
                        StepKeyPress( k );
                        break;
                    case TimeControlUserAction.HyperWarp:
                        HyperWarpKeyPress( k );
                        break;
                    case TimeControlUserAction.ToggleGUI:
                        ToggleGUIKeyPress( k );
                        break;
                }
            }
        }

        private void LogKeyPress(TCKeyBinding k, string caller)
        {
            Log.Trace( String.Format( "Key Pressed {0} : {1}", k.KeyCombination.Select( x => x.ToString() ).Aggregate( (current, next) => current + " + " + next ), k.Description ), "KeyboardInputManager."+ caller );
        }

        private void SpeedUpKeyPress(TCKeyBinding k)
        {
            if (keysPressedDown.Contains( k.KeyCombination.Last() ))            
                LogKeyPress( k, "SpeedUpKeyPress Started" );
            if (keysReleased.Contains( k.KeyCombination.Last() ))
                LogKeyPress( k, "SpeedUpKeyPress Ended" );

            if (TimeController.Instance.CurrentWarpState == TimeControllable.Hyper)
            {
                if (keysPressedDown.Contains( k.KeyCombination.Last() ))
                    TimeController.Instance.SpeedUpTime();
            }
            else
            {
                TimeController.Instance.SpeedUpTime();
            }
        }

        private void SlowDownKeyPress(TCKeyBinding k)
        {
            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
                LogKeyPress( k, "SlowDownKeyPress Started" );
            if (keysReleased.Contains( k.KeyCombination.Last() ))
                LogKeyPress( k, "SlowDownKeyPress Ended" );

            if (TimeController.Instance.CurrentWarpState == TimeControllable.Hyper)
            {
                if (keysPressedDown.Contains( k.KeyCombination.Last() ))
                    TimeController.Instance.SlowDownTime();
            }
            else
            {
                TimeController.Instance.SlowDownTime();
            }            
        }

        private void RealtimeKeyPress(TCKeyBinding k)
        {
            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
            {
                LogKeyPress( k, "RealtimeKeyPress" );

                TimeController.Instance.Realtime();
            }
        }

        private void SlowMo64KeyPress(TCKeyBinding k)
        {
            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
            {
                LogKeyPress( k, "SetSlowMo64" );
                TimeController.Instance.UpdateTimeSlider( 1 );
            }
        }

        private void CustomKeySliderKeyPress(TCKeyBinding k)
        {
            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
            {
                LogKeyPress( k, "SetCustomKeySlider" );
                TimeController.Instance.UpdateTimeSlider( Settings.Instance.CustomKeySlider );
            }
        }

        private void PauseKeyPress(TCKeyBinding k)
        {
            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
            {
                LogKeyPress( k, "PauseKeyPress" );
                TimeController.Instance.TogglePause();
            }
        }    

        private void StepKeyPress(TCKeyBinding k)
        {
            if (keysPressedDown.Contains(k.KeyCombination.Last()))
            {
                LogKeyPress( k, "StepKeyPress" );

                TimeController.Instance.IncrementTimeStep();
            }
        }

        private void HyperWarpKeyPress(TCKeyBinding k)
        {
            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
            {
                LogKeyPress( k, "HyperWarp" );
                TimeController.Instance.ToggleHyperWarp();
            }
        }

        private void ToggleGUIKeyPress(TCKeyBinding k)
        {
            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
            {
                LogKeyPress( k, "ToggleGUIKeyPress" );
                TCGUI.Instance.ToggleGUIVisibility();
            }   
        }

        /// <summary>
        /// Asynchronously gets a pressed key combination.
        /// Waits for keypresses in a coroutine, and then once no keys are pressed, returns the list of pressed keycodes to the callback function
        /// </summary>
        internal void GetPressedKeyCombination(Action<List<KeyCode>> callback)
        {
            string logCaller = "KeyboardInputManager.GetPressedKeyCombination";
            Log.Trace( "method start", logCaller );

            List<KeyCode> lkc = new List<KeyCode>();
            StartCoroutine( GetPressedKeyCombinationRepeat( lkc, callback ) );

            Log.Trace( "method end", logCaller );
        }

        internal IEnumerator GetPressedKeyCombinationRepeat(List<KeyCode> lkc, Action<List<KeyCode>> callback)
        {
            string logCaller = "KeyboardInputManager.GetPressedKeyCombinationRepeat";
            Log.Trace( "method start", logCaller );

            // Wait unti a key is pressed
            while (keysPressed.Count == 0)
            {
                yield return null;
            }

            // Add keys as they are pressed and held down
            while (keysPressed.Count != 0)
            {
                foreach (KeyCode kc in keysPressed)
                {
                    if (!lkc.Contains( kc ))
                        lkc.Add( kc );
                }

                yield return null;
            }

            // Finally execute the callback function to send back the key combination
            callback.Invoke( lkc );

            Log.Trace( "method end", logCaller );

            yield break;
        }

        internal static string GetKeyCombinationString(IEnumerable<KeyCode> lkc)
        {
            string logCaller = "KeyboardInputManager.GetKeyCombinationString";
            Log.Trace( "method start", logCaller );

            string s = "";
            foreach (KeyCode kc in lkc)
            {
                if (kc == KeyCode.LeftShift || kc == KeyCode.RightShift)
                {
                    s += "[Shift]";
                    continue;
                }
                if (kc == KeyCode.LeftControl || kc == KeyCode.RightControl)
                {
                    s += "[Ctrl]";
                    continue;
                }
                if (kc == KeyCode.LeftAlt || kc == KeyCode.RightAlt)
                {
                    s += "[Alt]";
                    continue;
                }
                if (kc == KeyCode.LeftCommand || kc == KeyCode.RightCommand)
                {
                    s += "[Cmd]";
                    continue;
                }

                s += "[" + kc.ToString() + "]";
            }

            Log.Trace( "method end", logCaller );

            return s;
        }

        internal static List<KeyCode> GetKeyCombinationFromString(string s)
        {
            string logCaller = "KeyboardInputManager.GetKeyCombinationFromString";
            Log.Trace( "method start", logCaller );

            string parse = s.Trim();

            // Must start with [ and end with ]
            if (parse[0] != '[' || parse[parse.Length - 1] != ']')
            {
                Log.Warning( "Key Codes must be surrounded by [ ] ", logCaller );
            }
            
            // Strip start and end characters
            parse = parse.Substring( 1, parse.Length - 2 );

            IEnumerable<string> keys;
            if (s.Contains( "][" ))
            {
                // Split On ][
                keys = s.Split( new string[] { "][" }, StringSplitOptions.None ).Select( x => x.Trim() );
            } else
            {
                keys = new List<string>() { parse };
            }
            
            List<KeyCode> lkc = new List<KeyCode>();

            bool AllKeysDefined = true;
            foreach (string key in keys)
            {
                if (key == "Ctrl")
                {
                    lkc.Add( KeyCode.LeftControl );
                    Log.Trace( "Adding Control to key list from string " + s, logCaller );
                    continue;
                }
                if (key == "Alt")
                {
                    lkc.Add( KeyCode.LeftAlt );
                    Log.Trace( "Adding LeftAlt to key list from string " + s, logCaller );
                    continue;
                }
                if (key == "Cmd")
                {
                    lkc.Add( KeyCode.LeftCommand );
                    Log.Trace( "Adding LeftCommand to key list from string " + s, logCaller );
                    continue;
                }
                if (key == "Shift")
                {
                    lkc.Add( KeyCode.LeftShift );
                    Log.Trace( "Adding LeftShift to key list from string " + s, logCaller );
                    continue;
                }

                if (!Enum.IsDefined( typeof( KeyCode ), key ))
                {
                    AllKeysDefined = false;
                    break;
                }
                KeyCode parsedKeyCode = (KeyCode)Enum.Parse( typeof( KeyCode ), key );
                Log.Trace( "Adding "+ parsedKeyCode.ToString() + " to key list from string " + s, logCaller );
                lkc.Add( parsedKeyCode );
            }
            
            if (!AllKeysDefined)
                lkc = null;

            Log.Trace( "method end", logCaller );
            return lkc;
        }
    }
}

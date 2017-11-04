/*
All code in this file Copyright(c) 2016 Nate West

The MIT License (MIT)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

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
        internal static KeyboardInputManager Instance { get; private set; }
        public static bool IsReady { get; private set; } = false;
        #endregion

        // Considered a HashSet for these...Did some performance calcs for common situations (a few adds and lots of "contains") - lists perform better!
        private List<KeyCode> keysPressed;
        private List<KeyCode> keysPressedDown;
        private List<KeyCode> keysReleased;

        private List<KeyBinding> keyBinds;

        

        public List<KeyBinding> GetKeyBinds()
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( GetKeyBinds );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                return keyBinds.ToList();
            }
        }


        /// <summary>
        /// List of keys to check for presses
        /// </summary>
        private Dictionary<KeyCode, KeyCode> checkKeys = new Dictionary<KeyCode, KeyCode>();

        bool clearKeys = false;

        #region MonoBehavior

        private void Awake()
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( Awake );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                DontDestroyOnLoad( this ); //Don't go away on scene changes
                KeyboardInputManager.Instance = this;

                keyBinds = new List<KeyBinding>();

                List<KeyCode> ignoreKeys = new List<KeyCode>() { KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Mouse2, KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6,
                    KeyCode.Escape, KeyCode.CapsLock, KeyCode.ScrollLock, KeyCode.Numlock, KeyCode.Break,
                    KeyCode.LeftWindows, KeyCode.RightWindows, KeyCode.LeftApple, KeyCode.RightApple };

                List<KeyCode> checkKeysl = System.Enum.GetValues( typeof( KeyCode ) ).Cast<KeyCode>().ToList();
                checkKeysl.RemoveAll( k => ignoreKeys.Contains( k ) );

                foreach (KeyCode kc in checkKeysl)
                {
                    KeyCode setKC = kc;

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

                    this.checkKeys.Add( kc, setKC );
                }
            }
        }

        private void Start()
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( Start );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {

                keysPressed = new List<KeyCode>();
                keysPressedDown = new List<KeyCode>();
                keysReleased = new List<KeyCode>();

                UpdateKSPKeyBindings();
                ResetKeyBindingsToDefault();

                IsReady = true;
            }
        }

        private void Update()
        {
            // Don't do anything until the time controller objects are ready
            if (!TimeController.IsReady || !TimeControlIMGUI.IsReady || !HyperWarpController.IsReady || !RailsWarpController.IsReady || !SlowMoController.IsReady)
                return;
            // Only check for keypresses when we can actually do something
            if (   (!HyperWarpController.Instance?.CanHyperWarp ?? false)
                || (!RailsWarpController.Instance?.CanRailsWarp ?? false)
                || (!SlowMoController.Instance?.CanSlowMo ?? false)
                )
                return;

            // Only run during this frame if a key is actually pressed down
            if (Input.anyKey)
            {
                LoadKeyPresses();
                CheckKeyBindings();

                clearKeys = true;
            }
            else if (clearKeys)
            {
                keysPressed.Clear();
                keysPressedDown.Clear();
                keysReleased.Clear();

                clearKeys = false;
            }
        }

        #endregion

        #region Configuration
        public void ResetKeyBindingsToDefault()
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( ResetKeyBindingsToDefault );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                keyBinds = keyBinds ?? new List<KeyBinding>();
                keyBinds.Clear();

                keyBinds.Add( new KeyBinding { TCUserAction = TimeControlUserAction.SpeedUp, Description = "Speed Up", KeyCombination = new List<KeyCode>() } );
                keyBinds.Add( new KeyBinding { TCUserAction = TimeControlUserAction.SlowDown, Description = "Slow Down", KeyCombination = new List<KeyCode>() } );
                keyBinds.Add( new KeyBinding { TCUserAction = TimeControlUserAction.Realtime, Description = "Realtime", KeyCombination = new List<KeyCode>() } );
                //keyBinds.Add( new KeyBinding { TCUserAction = TimeControlUserAction.SlowMo64, Description = "1/64x", KeyCombination = new List<KeyCode>() } );
                //keyBinds.Add( new KeyBinding { TCUserAction = TimeControlUserAction.CustomKeySlider, Description = "Custom-#x", KeyCombination = new List<KeyCode>() } );
                keyBinds.Add( new KeyBinding { TCUserAction = TimeControlUserAction.Pause, Description = "Pause", KeyCombination = new List<KeyCode>() } );
                keyBinds.Add( new KeyBinding { TCUserAction = TimeControlUserAction.Step, Description = "Step", KeyCombination = new List<KeyCode>() } );
                keyBinds.Add( new KeyBinding { TCUserAction = TimeControlUserAction.HyperWarp, Description = "Hyper Warp", KeyCombination = new List<KeyCode>() } );
                keyBinds.Add( new KeyBinding { TCUserAction = TimeControlUserAction.ToggleGUI, Description = "Toggle GUI", KeyCombination = new List<KeyCode>() } );
            }
        }

        public void ConfigCreateOrUpdateKeyBinds(ConfigNode cn)
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( ConfigCreateOrUpdateKeyBinds );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                string kb = "KeyBinds";
                // Rebuild the keybinds node
                if (cn.HasNode( kb ))
                    cn.RemoveNode( kb );
                ConfigNode keyBindsNode = cn.AddNode( kb );
                foreach (KeyBinding k in keyBinds)
                {
                    keyBindsNode.SetValue( k.TCUserAction.ToString(), k.IsKeyAssigned ? k.KeyCombinationString : "[" + KeyCode.None.ToString() + "]", true );
                }
            }
        }

        public void ConfigLoadKeyBinds(ConfigNode cn)
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( ConfigCreateOrUpdateKeyBinds );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                string kb = "KeyBinds";
                ConfigNode keyBindsNode;
                if (!cn.HasNode( kb ))
                {
                    string message = "No KeyBinds node found in config. This error is not fatal to the load process. Default keybinds will be used instead.";
                    Log.Warning( message, logBlockName );
                    return;
                }
                keyBindsNode = cn.GetNode( kb );

                ConfigNode.ValueList vl = keyBindsNode.values;

                for (int i = 0; i < keyBinds.Count; i++)
                {
                    KeyBinding k = keyBinds[i];
                    string userAction = k.TCUserAction.ToString();
                    if (vl.Contains( userAction ))
                    {
                        string keycombo = vl.GetValue( userAction );
                        List<KeyCode> iekc = KeyboardInputManager.GetKeyCombinationFromString( keycombo );
                        if (iekc == null)
                        {
                            Log.Warning( "Key combination is not defined correctly: " + keycombo + " - Using default for user action " + userAction, logBlockName );
                            continue;
                        }
                        if (iekc.Contains( KeyCode.None ))
                        {
                            k.KeyCombination = new List<KeyCode>();
                        }
                        else
                        {
                            k.KeyCombination = new List<KeyCode>( iekc );
                            k.KeyCombinationString = keycombo;
                        }
                    }
                }

            }
        }
        #endregion Configuration

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

        public void AssignKeyBinding(KeyBinding kb)
        {
            int index = keyBinds.FindIndex( k => k.TCUserAction == kb.TCUserAction );
            if (index >= 0)
            {
                keyBinds[index] = kb;
            }
        }
        
        private void LoadKeyPresses()
        {
            keysPressed.Clear();
            keysPressedDown.Clear();
            keysReleased.Clear();

            foreach (KeyValuePair<KeyCode, KeyCode> kvp in this.checkKeys)
            {
                KeyCode kc = kvp.Key;
                KeyCode setKC = kvp.Value;

                if (Input.GetKey( kc ) && !keysPressed.Contains( kvp.Value ))
                    keysPressed.Add( setKC );

                if (Input.GetKeyDown( kc ) && !keysPressedDown.Contains( setKC ))
                    keysPressedDown.Add( setKC );

                if (Input.GetKeyUp( kc ) && !keysReleased.Contains( setKC ))
                    keysReleased.Add( setKC );
            }
        }

        private void CheckKeyBindings()
        {
            foreach (KeyBinding k in this.keyBinds)
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

        private void LogKeyPress(KeyBinding k, string caller)
        {
            if (Log.LoggingLevel == LogSeverity.Trace)
            {
                Log.Trace( String.Format( "Key Pressed {0} : {1}", k.KeyCombination.Select( x => x.ToString() ).Aggregate( (current, next) => current + " + " + next ), k.Description ), "KeyboardInputManager." + caller );
            }
        }

        private void SpeedUpKeyPress(KeyBinding k)
        {
            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
                LogKeyPress( k, "SpeedUpKeyPress Started" );
            if (keysReleased.Contains( k.KeyCombination.Last() ))
                LogKeyPress( k, "SpeedUpKeyPress Ended" );

            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
            {
                if (HyperWarpController.IsReady && HyperWarpController.Instance.IsHyperWarping)
                {
                    HyperWarpController.Instance.SpeedUpHyper();
                }
                else if (SlowMoController.IsReady && SlowMoController.Instance.IsSlowMo)
                {
                    SlowMoController.Instance.SpeedUp();
                }
            }
        }

        private void SlowDownKeyPress(KeyBinding k)
        {
            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
                LogKeyPress( k, "SlowDownKeyPress Started" );
            if (keysReleased.Contains( k.KeyCombination.Last() ))
                LogKeyPress( k, "SlowDownKeyPress Ended" );

            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
            {
                if (HyperWarpController.IsReady && HyperWarpController.Instance.IsHyperWarping)
                {
                    HyperWarpController.Instance.SlowDownHyper();
                }
                else if (SlowMoController.IsReady && SlowMoController.Instance.IsSlowMo)
                {
                    SlowMoController.Instance.SlowDown();
                }
            }
        }

        private void RealtimeKeyPress(KeyBinding k)
        {
            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
            {
                LogKeyPress( k, "RealtimeKeyPress" );
                TimeController.Instance.GoRealTime();
            }
        }

        private void SlowMo64KeyPress(KeyBinding k)
        {
            //if (keysPressedDown.Contains( k.KeyCombination.Last() ))
            //{
            //    LogKeyPress( k, "SetSlowMo64" );
            //    TimeController.Instance.UpdateTimeSlider( 1 );
            //}
        }

        private void CustomKeySliderKeyPress(KeyBinding k)
        {
            //if (keysPressedDown.Contains( k.KeyCombination.Last() ))
            //{
            //    LogKeyPress( k, "SetCustomKeySlider" );
            //    TimeController.Instance.UpdateTimeSlider( Settings.Instance.CustomKeySlider );
            //}
        }

        private void PauseKeyPress(KeyBinding k)
        {
            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
            {
                LogKeyPress( k, "PauseKeyPress" );
                TimeController.Instance.TogglePause();
            }
        }

        private void StepKeyPress(KeyBinding k)
        {
            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
            {
                LogKeyPress( k, "StepKeyPress" );

                TimeController.Instance.IncrementTimeStep();
            }
        }

        private void HyperWarpKeyPress(KeyBinding k)
        {
            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
            {
                LogKeyPress( k, "HyperWarp" );

                if (HyperWarpController.IsReady && HyperWarpController.Instance.CanHyperWarp)
                {
                    HyperWarpController.Instance.ToggleHyper();
                }
            }
        }

        private void ToggleGUIKeyPress(KeyBinding k)
        {
            if (keysPressedDown.Contains( k.KeyCombination.Last() ))
            {
                LogKeyPress( k, "ToggleGUIKeyPress" );
                TimeControlIMGUI.Instance.ToggleGUIVisibility();
            }
        }

        /// <summary>
        /// Asynchronously gets a pressed key combination.
        /// Waits for keypresses in a coroutine, and then once no keys are pressed, returns the list of pressed keycodes to the callback function
        /// </summary>
        internal void GetPressedKeyCombination(Action<List<KeyCode>> callback)
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( GetPressedKeyCombination );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                List<KeyCode> lkc = new List<KeyCode>();
                StartCoroutine( GetPressedKeyCombinationRepeat( lkc, callback ) );
            }
        }

        internal IEnumerator GetPressedKeyCombinationRepeat(List<KeyCode> lkc, Action<List<KeyCode>> callback)
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( GetPressedKeyCombinationRepeat );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
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
                
                yield break;
            }
        }

        internal static string GetKeyCombinationString(IEnumerable<KeyCode> lkc)
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( GetKeyCombinationString );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
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

                Log.Trace( "returning string " + s, logBlockName );
                return s;
            }            
        }

        internal static List<KeyCode> GetKeyCombinationFromString(string s)
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( GetKeyCombinationFromString );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                string parse = s.Trim();

                // Must start with [ and end with ]
                if (parse[0] != '[' || parse[parse.Length - 1] != ']')
                {
                    Log.Warning( "Key Codes must be surrounded by [ ] ", logBlockName );
                }

                // Strip start and end characters
                parse = parse.Substring( 1, parse.Length - 2 );

                IEnumerable<string> keys;
                if (s.Contains( "][" ))
                {
                    // Split On ][
                    keys = s.Split( new string[] { "][" }, StringSplitOptions.None ).Select( x => x.Trim() );
                }
                else
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
                        Log.Trace( "Adding Control to key list from string " + s, logBlockName );
                        continue;
                    }
                    if (key == "Alt")
                    {
                        lkc.Add( KeyCode.LeftAlt );
                        Log.Trace( "Adding LeftAlt to key list from string " + s, logBlockName );
                        continue;
                    }
                    if (key == "Cmd")
                    {
                        lkc.Add( KeyCode.LeftCommand );
                        Log.Trace( "Adding LeftCommand to key list from string " + s, logBlockName );
                        continue;
                    }
                    if (key == "Shift")
                    {
                        lkc.Add( KeyCode.LeftShift );
                        Log.Trace( "Adding LeftShift to key list from string " + s, logBlockName );
                        continue;
                    }

                    if (!Enum.IsDefined( typeof( KeyCode ), key ))
                    {
                        AllKeysDefined = false;
                        break;
                    }
                    KeyCode parsedKeyCode = (KeyCode)Enum.Parse( typeof( KeyCode ), key );
                    Log.Trace( "Adding " + parsedKeyCode.ToString() + " to key list from string " + s, logBlockName );
                    lkc.Add( parsedKeyCode );
                }

                if (!AllKeysDefined)
                    lkc = null;

                return lkc;
            }
        }
    }
}

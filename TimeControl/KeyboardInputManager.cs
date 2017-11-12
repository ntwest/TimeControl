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

using TimeControl.KeyBindings;

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

        private List<TimeControlKeyBinding> activeKeyBinds;
        
        public List<TimeControlKeyBinding> GetActiveKeyBinds()
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( GetActiveKeyBinds );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                return activeKeyBinds.ToList();
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

                activeKeyBinds = new List<TimeControlKeyBinding>();
                
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
            //if (   (!HyperWarpController.Instance?.CanHyperWarp ?? false)
            //    || (!HyperWarpController.Instance?.IsHyperWarping ?? false)
            //    || (!RailsWarpController.Instance?.CanRailsWarp ?? false)
            //    || (!RailsWarpController.Instance?.IsRailsWarping ?? false)
            //    || (!SlowMoController.Instance?.CanSlowMo ?? false)
            //    || (!SlowMoController.Instance?.IsSlowMo ?? false)
            //    )
            //    return;

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
                activeKeyBinds = activeKeyBinds ?? new List<TimeControlKeyBinding>();
                activeKeyBinds.Clear();

                activeKeyBinds.Add( new GUIToggle() );
                activeKeyBinds.Add( new Realtime() );
                activeKeyBinds.Add( new PauseToggle() );
                activeKeyBinds.Add( new TimeStep() );
                activeKeyBinds.Add( new HyperToggle() );
                activeKeyBinds.Add( new SlowMoToggle() );
                activeKeyBinds.Add( new HyperRateSpeedUp() { V = 1f } );
                activeKeyBinds.Add( new HyperRateSlowDown() { V = 1f } );
                activeKeyBinds.Add( new SlowMoSpeedUp() { V = 0.05f } );
                activeKeyBinds.Add( new SlowMoSlowDown() { V = 0.05f } );
                activeKeyBinds.Add( new HyperPhysicsAccuracyUp() { V = 0.5f } );
                activeKeyBinds.Add( new HyperPhysicsAccuracyDown() { V = 0.5f } );

                if (GlobalSettings.IsReady)
                {
                    GlobalSettings.Instance.Save();
                }
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
                {
                    cn.RemoveNode( kb );
                }
                ConfigNode keyBindsNode = cn.AddNode( kb );
                foreach (TimeControlKeyBinding k in activeKeyBinds)
                {
                    ConfigNode newNode = keyBindsNode.AddNode( "KeyBind" );
                    newNode.AddValue( "Action", k.TimeControlKeyActionName );
                    string combo = k.IsKeyAssigned ? k.KeyCombinationDescription : "[" + KeyCode.None.ToString() + "]";
                    newNode.AddValue( "KeyCombination", combo);

                    if (k is TimeControlKeyBindingValue k2)
                    {
                        newNode.AddValue( "V", k2.V );
                    }
                }
            }
        }

        static List<TimeControlKeyAction> ActionsWithValues =
            new List<TimeControlKeyAction>()
            {
                TimeControlKeyAction.HyperPhysicsAccuracyDown,
                TimeControlKeyAction.HyperPhysicsAccuracySet,
                TimeControlKeyAction.HyperPhysicsAccuracyUp,
                TimeControlKeyAction.HyperRateSetRate,
                TimeControlKeyAction.HyperRateSlowDown,
                TimeControlKeyAction.HyperRateSpeedUp,
                TimeControlKeyAction.SlowMoSetRate,
                TimeControlKeyAction.SlowMoSlowDown,
                TimeControlKeyAction.SlowMoSpeedUp
            };

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

                    ResetKeyBindingsToDefault();
                    return;
                }
                keyBindsNode = cn.GetNode( kb );

                List<ConfigNode> lcn = keyBindsNode.GetNodes( "KeyBind" ).ToList();
                
                for (int i = 0; i < lcn.Count; i++)
                {
                    ConfigNode ccn = lcn[i];

                    string s = ccn.GetValue( "Action" );
                    string s_tcka = Enum.GetNames( typeof( TimeControlKeyAction ) ).ToList().Where( e => e == s ).FirstOrDefault();
                    TimeControlKeyAction tcka;
                    if (Enum.IsDefined( typeof( TimeControlKeyAction ), s_tcka))
                    {
                        tcka = (TimeControlKeyAction)Enum.Parse( typeof( TimeControlKeyAction ), s_tcka );
                    }
                    else
                    {
                        Log.Warning( "Action " + s_tcka + " not found. Ignoring definition.", logBlockName );
                        continue;
                    }

                    string keycombo = ccn.GetValue( "KeyCombination" );
                    List<KeyCode> iekc = KeyboardInputManager.GetKeyCombinationFromString( keycombo );
                    if (iekc == null)
                    {
                        Log.Warning( "Key combination is not defined correctly: " + keycombo + " - Using default for user action " + tcka.ToString(), logBlockName );
                        continue;
                    }

                    float v = 0f;
                    if (ActionsWithValues.Contains(tcka))
                    {
                        if (ccn.HasValue("V"))
                        {
                            if (!(ccn.TryAssignFromConfigFloat("V", ref v)))
                            {
                                Log.Warning( "Key does not have a value assigned in config. " + tcka.ToString(), logBlockName );
                                continue;
                            }
                        }
                    }

                    TimeControlKeyBinding tckb = new GUIToggle();
                    switch (tcka)
                    {
                        case TimeControlKeyAction.GUIToggle:
                            tckb = new GUIToggle() { KeyCombination = iekc };
                            break;
                        case TimeControlKeyAction.Realtime:
                            tckb = new Realtime() { KeyCombination = iekc };
                            break;
                        case TimeControlKeyAction.PauseToggle:
                            tckb = new PauseToggle() { KeyCombination = iekc };
                            break;
                        case TimeControlKeyAction.TimeStep:
                            tckb = new TimeStep() { KeyCombination = iekc };
                            break;
                        case TimeControlKeyAction.HyperToggle:
                            tckb = new HyperToggle() { KeyCombination = iekc };
                            break;
                        case TimeControlKeyAction.SlowMoToggle:
                            tckb = new SlowMoToggle() { KeyCombination = iekc };
                            break;
                        case TimeControlKeyAction.HyperRateSetRate:
                            tckb = new HyperRateSetRate() { KeyCombination = iekc, V = v };
                            break;
                        case TimeControlKeyAction.HyperRateSlowDown:
                            tckb = new HyperRateSlowDown() { KeyCombination = iekc, V = v };
                            break;
                        case TimeControlKeyAction.HyperRateSpeedUp:
                            tckb = new HyperRateSpeedUp() { KeyCombination = iekc, V = v };
                            break;
                        case TimeControlKeyAction.HyperPhysicsAccuracySet:
                            tckb = new HyperPhysicsAccuracySet() { KeyCombination = iekc, V = v };
                            break;
                        case TimeControlKeyAction.HyperPhysicsAccuracyDown:
                            tckb = new HyperPhysicsAccuracyDown() { KeyCombination = iekc, V = v };
                            break;
                        case TimeControlKeyAction.HyperPhysicsAccuracyUp:
                            tckb = new HyperPhysicsAccuracyUp() { KeyCombination = iekc, V = v };
                            break;
                        case TimeControlKeyAction.SlowMoSetRate:
                            tckb = new SlowMoSetRate() { KeyCombination = iekc, V = v };
                            break;
                        case TimeControlKeyAction.SlowMoSlowDown:
                            tckb = new SlowMoSlowDown() { KeyCombination = iekc, V = v };
                            break;
                        case TimeControlKeyAction.SlowMoSpeedUp:
                            tckb = new SlowMoSpeedUp() { KeyCombination = iekc, V = v };
                            break;
                    }

                    if (tckb is TimeControlKeyBindingValue tckbv)
                    {
                        var keys = activeKeyBinds
                            .Where( k => k.TimeControlKeyActionName == tcka && k is TimeControlKeyBindingValue )
                            .Select( f => (TimeControlKeyBindingValue)f )
                            .Where( k => k.V == v );
                        if (keys.Count() == 0)
                        {
                            tckb.IsUserDefined = true;
                            activeKeyBinds.Add( tckb );
                        }
                        else
                        {
                            TimeControlKeyBindingValue tckbvOrig = keys.First();
                            tckbvOrig.KeyCombination = tckb.KeyCombination.ToList();
                        }
                    }
                    else
                    {
                        var keys = activeKeyBinds.Where( k => k.TimeControlKeyActionName == tcka );
                        if (keys.Count() == 0)
                        {
                            tckb.IsUserDefined = true;
                            activeKeyBinds.Add( tckb );
                        }
                        else
                        {
                            TimeControlKeyBinding tckbOrig = keys.First();
                            tckbOrig.KeyCombination = tckb.KeyCombination.ToList();
                        }
                    }
                }
            }
        }
        #endregion Configuration

        public void AddKeyBinding(TimeControlKeyBinding kb)
        {
            if (!activeKeyBinds.Contains( kb ))
            {
                activeKeyBinds.Add( kb );

                if (GlobalSettings.IsReady)
                {
                    GlobalSettings.Instance.Save();
                }
            }
        }

        public void DeleteKeyBinding(TimeControlKeyBinding kb)
        {
            if (activeKeyBinds.Contains(kb))
            {
                activeKeyBinds.Remove( kb );

                if (GlobalSettings.IsReady)
                {
                    GlobalSettings.Instance.Save();
                }
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
                {
                    keysPressed.Add( setKC );
                }

                if (Input.GetKeyDown( kc ) && !keysPressedDown.Contains( setKC ))
                {
                    keysPressedDown.Add( setKC );
                }

                if (Input.GetKeyUp( kc ) && !keysReleased.Contains( setKC ))
                {
                    keysReleased.Add( setKC );
                }
            }
        }

        private void CheckKeyBindings()
        {
            foreach (TimeControlKeyBinding k in this.activeKeyBinds)
            {
                // Only if the keys we care about (and no others) are pressed. Otherwise continue the loop
                if (!
                    (k.KeyCombination.Count() != 0
                    && k.KeyCombination.All( x => keysPressed.Contains( x ) )
                    && k.KeyCombination.Count() == keysPressed.Count())
                   )
                {
                    continue;
                }

                if (keysPressedDown.Contains( k.KeyCombination.Last() ))
                {
                    if (Log.LoggingLevel == LogSeverity.Trace)
                    {
                        Log.Trace( String.Format( "Key Pressed {0} : {1}", k.KeyCombination.Select( x => x.ToString() ).Aggregate( (current, next) => current + " + " + next ), k.Description ), "KeyboardInputManager.CheckKeyBindings" );
                    }

                    k.Press();
                }                
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



        internal static List<KeyCode> GetKeyCombinationFromString(string s)
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( GetKeyCombinationFromString );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                string parse = s.Trim();

                Log.Trace( "Parsing string ".MemoizedConcat( parse ), logBlockName );

                // Must start with [ and end with ]
                if (parse[0] != '[' || parse[parse.Length - 1] != ']')
                {
                    Log.Warning( "Key Codes must be surrounded by [ ] ", logBlockName );
                    return null;
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
                
                foreach (string key in keys)
                {
                    Log.Trace( "Parsing key ".MemoizedConcat( key ), logBlockName );

                    if (key == "None")
                    {
                        Log.Trace( "'None' key found, not adding to key list", logBlockName );
                        continue;
                    }
                    if (key == "Ctrl")
                    {
                        lkc.Add( KeyCode.LeftControl );
                        Log.Trace( "Added LeftControl to key list", logBlockName );
                        continue;
                    }
                    if (key == "Alt")
                    {
                        lkc.Add( KeyCode.LeftAlt );
                        Log.Trace( "Added LeftAlt to key list", logBlockName );
                        continue;
                    }
                    if (key == "Cmd")
                    {
                        lkc.Add( KeyCode.LeftCommand );
                        Log.Trace( "Added LeftCommand to key list", logBlockName );
                        continue;
                    }
                    if (key == "Shift")
                    {
                        lkc.Add( KeyCode.LeftShift );
                        Log.Trace( "Added LeftShift to key list", logBlockName );
                        continue;
                    }

                    if (!Enum.IsDefined( typeof( KeyCode ), key ))
                    {
                        Log.Warning( "Key ".MemoizedConcat(key).MemoizedConcat(" not found!"), logBlockName );
                        return null;
                    }
                    KeyCode parsedKeyCode = (KeyCode)Enum.Parse( typeof( KeyCode ), key );                    
                    lkc.Add( parsedKeyCode );
                    Log.Trace( "Added ".MemoizedConcat( parsedKeyCode.ToString() ).MemoizedConcat(" to key list"), logBlockName );
                }

                return lkc;
            }
        }
    }
}

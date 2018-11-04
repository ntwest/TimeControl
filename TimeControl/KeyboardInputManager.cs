using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
using TimeControl.KeyBindings;

namespace TimeControl
{
    /// <summary>
    /// Time control input manager
    /// </summary>
    [KSPAddon( KSPAddon.Startup.Instantly, true )]
    internal class KeyboardInputManager : MonoBehaviour
    {
        #region Singleton
        internal static KeyboardInputManager Instance { get; private set; }
        public static bool IsReady { get; private set; } = false;
        public int KeyRepeatStart { get; private set; } = 500;
        public int KeyRepeatInterval { get; private set; } = 15;
        #endregion

        // Considered a HashSet for these...Did some performance calcs for common situations (a few adds and lots of "contains") - lists perform better!
        private List<KeyCode> keysPressed;
        private List<KeyCode> keysPressedDown;
        private List<KeyCode> keysReleased;

        private bool isAssigningKey = false;
        private bool clearKeys = false;
        private float pressedDownStart = 0f;
        private float lastInterval = 0f;

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

        

        #region MonoBehavior

        private void Awake()
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( Awake );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                DontDestroyOnLoad( this ); //Don't go away on scene changes
                KeyboardInputManager.Instance = this;
                
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
                StartCoroutine( Configure() );
            }
        }

        private void Update()
        {
            // Don't do anything until the time controller objects are ready
            if (!TimeController.IsReady || !TimeControlIMGUI.IsReady || !HyperWarpController.IsReady || !RailsWarpController.IsReady || !SlowMoController.IsReady)
            {
                return;
            }

            // Only run during this frame if a key is actually pressed
            if (Input.anyKey)
            {
                LoadKeyPresses();

                if (!isAssigningKey)
                {
                    ProcessKeyPressActions();
                }

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
        private IEnumerator Configure()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( Configure );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                keysPressed = new List<KeyCode>();
                keysPressedDown = new List<KeyCode>();
                keysReleased = new List<KeyCode>();

                activeKeyBinds = GlobalSettings.Instance.GetActiveKeyBinds().ToList();
                
                while (!GlobalSettings.IsReady || !HighLogic.LoadedSceneIsGame || TimeWarp.fetch == null || FlightGlobals.Bodies == null || FlightGlobals.Bodies.Count <= 0)
                {
                    yield return new WaitForSeconds( 1f );
                }

                GameEvents.OnGameSettingsApplied.Add( OnGameSettingsApplied );

                Log.Info( nameof( KeyboardInputManager ) + " is Ready!", logBlockName );
                IsReady = true;
                yield break;
            }
        }

        public void ResetKeyBindingsToDefault()
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( ResetKeyBindingsToDefault );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                GlobalSettings.Instance.ResetKeybindsToDefaultSettings();
                activeKeyBinds = GlobalSettings.Instance.GetActiveKeyBinds().ToList();
            }
        }

        #endregion Configuration

        private void OnGameSettingsApplied()
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( OnGameSettingsApplied );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                TimeControlParameterNode TCPN = null;
                try
                {
                    TCPN = HighLogic.CurrentGame.Parameters.CustomParams<TimeControlParameterNode>();
                }
                catch (NullReferenceException) { }
                if (TCPN != null)
                {
                    KeyRepeatStart = TCPN.KeyRepeatStart;
                    KeyRepeatInterval = TCPN.KeyRepeatInterval;
                }
            }
        }

        public void AddKeyBinding(TimeControlKeyBinding kb)
        {
            if (!activeKeyBinds.Contains( kb ))
            {
                var l = activeKeyBinds.FindAll( tckb => tckb.TimeControlKeyActionName == kb.TimeControlKeyActionName && tckb.ID == kb.ID );
                if (l != null && l.Count > 0)
                {
                    kb.ID = l.Select( x => x.ID ).Max() + 1;
                }
                activeKeyBinds.Add( kb );

                if (GlobalSettings.IsReady)
                {
                    GlobalSettings.Instance.SetActiveKeyBinds( activeKeyBinds );
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
                    GlobalSettings.Instance.SetActiveKeyBinds( activeKeyBinds );
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

        private void ProcessKeyPressActions()
        {
            if (FlightDriver.Pause)
            {
                return;
            }

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
                
                if (k.FireWhileHoldingKeyDown)
                {
                    if (keysPressedDown.Contains( k.KeyCombination.Last() ))
                    {
                        pressedDownStart = Time.realtimeSinceStartup;
                        lastInterval = pressedDownStart;
                        k.Press();
                    }
                    else
                    {
                        float timeNow = Time.realtimeSinceStartup;
                        if ((timeNow > pressedDownStart + (((float)this.KeyRepeatStart)) / 1000f ) // wait for initial hold down
                            && (timeNow > lastInterval + (1.0f / ((float)(this.KeyRepeatInterval)))) // repeats per real time second
                            ) 
                        {
                            lastInterval = timeNow;
                            k.Press();
                        }
                    }
                }
                else if (keysPressedDown.Contains( k.KeyCombination.Last() ))
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
                isAssigningKey = true;
                StartCoroutine( CRGetPressedKeyCombination( lkc, callback ) );
            }
        }

        internal IEnumerator CRGetPressedKeyCombination(List<KeyCode> lkc, Action<List<KeyCode>> callback)
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( CRGetPressedKeyCombination );
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

                isAssigningKey = false;

                yield break;
            }
        }
    }
}

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

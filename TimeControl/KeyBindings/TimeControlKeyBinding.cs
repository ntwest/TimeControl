using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public abstract class TimeControlKeyBinding
    {
        public string Description { get; set; }

        private List<KeyCode> keyCombination = new List<KeyCode>();

        public List<KeyCode> KeyCombination
        {
            get => keyCombination;
            set
            {
                keyCombination = value;
                UpdateKeyCombinationDescription();
            }
        }

        public string KeyCombinationDescription { get; private set; }

        public bool IsKeyAssigned { get { return KeyCombination.Count != 0; } }

        public bool IsUserDefined { get; set; } = false;

        public bool FireWhilePressedDown { get; set; } = false;

        public TimeControlKeyAction TimeControlKeyActionName;

        private void UpdateKeyCombinationDescription()
        {
            const string logBlockName = nameof( TimeControlKeyBinding ) + "." + nameof( KeyCombinationDescription );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                string s = "";

                if (KeyCombination.Count == 0)
                {
                    s = "[None]";
                }
                else
                {
                    foreach (KeyCode kc in KeyCombination)
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
                }

                Log.Trace( "Set Key Combination to " + s, logBlockName );
                KeyCombinationDescription = s;
            }
        }
        
        abstract public void Press();
    }
}

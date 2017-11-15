using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public abstract class TimeControlKeyBinding
    {
        protected const string kbNodeName = "KeyBind";

        public string Description { get; set; }
        public string SetDescription { get; set; } = "";

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

        public string KeyCombinationDescription { get; private set; } = "[None]";

        public bool IsKeyAssigned { get { return KeyCombination.Count != 0; } }

        public bool IsUserDefined { get; set; } = false;

        public bool FireWhileHoldingKeyDown { get; set; } = false;

        public TimeControlKeyAction TimeControlKeyActionName { get; set; }
        
        private void UpdateKeyCombinationDescription()
        {
            const string logBlockName = nameof( TimeControlKeyBinding ) + "." + nameof( UpdateKeyCombinationDescription );
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
        
        public virtual ConfigNode GetConfigNode()
        {
            const string logBlockName = nameof( TimeControlKeyBinding ) + "." + nameof( GetConfigNode );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Trace( "Getting ConfigNode For Action "
                    .MemoizedConcat( this.TimeControlKeyActionName.MemoizedToString() )
                    .MemoizedConcat( " with Key Combination " )
                    .MemoizedConcat( this.KeyCombinationDescription ) );

                ConfigNode newNode = new ConfigNode( kbNodeName );
                newNode.AddValue( "Action", this.TimeControlKeyActionName );
                newNode.AddValue( "IsUserDefined", this.IsUserDefined );
                newNode.AddValue( "KeyCombination", this.KeyCombinationDescription );
                return newNode;
            }
        }

        public abstract void Press();
    }
}

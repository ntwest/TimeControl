using System.Collections.Generic;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public abstract class TimeControlKeyBinding
    {
        protected const string kbNodeName = "KeyBind";

        public int ID { get; set; } = 0;

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
                newNode.AddValue( "ID", this.ID );
                newNode.AddValue( "IsUserDefined", this.IsUserDefined );
                newNode.AddValue( "KeyCombination", this.KeyCombinationDescription );
                
                return newNode;
            }
        }

        public abstract void Press();
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

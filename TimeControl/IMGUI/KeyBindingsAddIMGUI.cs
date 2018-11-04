using UnityEngine;
using TimeControl.KeyBindings;

namespace TimeControl
{
    internal class KeyBindingsAddIMGUI
    {
        private string sCurrentValue;
        private string sValue;
        private bool usePercentage;
        private bool valueParsed = false;

        TimeControlKeyBinding kb;

        public KeyBindingsAddIMGUI(TimeControlKeyBinding kb)
        {
            this.kb = kb;

            if (kb is TimeControlKeyBindingValue tckbv)
            {
                usePercentage = (tckbv is SlowMoSetRate || tckbv is SlowMoSlowDown || tckbv is SlowMoSpeedUp);

                if (!usePercentage)
                {
                    sValue = sCurrentValue = tckbv.V.MemoizedToString();
                }
                else
                {
                    sValue = sCurrentValue = Mathf.RoundToInt( tckbv.V * 100.0f ).MemoizedToString();
                }

                parseValue();
            }
        }

        private void parseValue()
        {
            if (kb is TimeControlKeyBindingValue tckbv)
            {
                valueParsed = float.TryParse( sValue, out float f );
                if (valueParsed)
                {
                    if (!usePercentage)
                    {
                        tckbv.V = f;
                        sValue = sCurrentValue = tckbv.V.MemoizedToString();
                    }
                    else
                    {
                        tckbv.V = (f / 100.0f);
                        sValue = sCurrentValue = Mathf.RoundToInt( tckbv.V * 100.0f ).MemoizedToString();
                    }
                }
            }
        }

        public bool KeyBindingsAddGUI()
        {
            if (!KeyboardInputManager.IsReady)
            {
                return true;
            }

            bool guiPriorEnabled = GUI.enabled;
            Color guiPriorColor = GUI.contentColor;

            GUILayout.BeginHorizontal();
            {
                if (kb is TimeControlKeyBindingValue tckbv)
                {
                    GUILayout.Label( kb.SetDescription, GUILayout.Width( 225 ) );
                    sValue = GUILayout.TextField( sValue, GUILayout.Width( 50 ) );
                    if (sValue != sCurrentValue)
                    {
                        parseValue();
                    }
                    GUI.enabled = guiPriorEnabled && valueParsed;
                }
                else
                {
                    GUILayout.Label( kb.SetDescription, GUILayout.Width( 280 ) );
                }
                if (GUILayout.Button( "ADD", GUILayout.Width( 40 ) ))
                {
                    var newkb = TimeControlKeyBindingFactory.LoadFromConfigNode( kb.GetConfigNode() );
                    KeyboardInputManager.Instance.AddKeyBinding( newkb );
                    return true;
                }
            }
            GUILayout.EndHorizontal();

            GUI.contentColor = guiPriorColor;
            GUI.enabled = guiPriorEnabled;
            return false;
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

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
using System.Collections;
using System.Collections.Generic;
using SC = System.ComponentModel;
using System.Reflection;
using System.Linq;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
using KSP.UI.Dialogs;
using KSPPluginFramework;


namespace TimeControl
{
    internal class KeyBindingsEditorIMGUI
    {
        private bool currentlyAssigningKey = false;
        private bool refreshKBCache = true;
        private List<TimeControlKeyBinding> keyBindings = new List<TimeControlKeyBinding>();
        
        public KeyBindingsEditorIMGUI()
        {
        }
        
        private void CacheKeyBinds()
        {
            if (keyBindings == null || refreshKBCache)
            {
                keyBindings.Clear();
                keyBindings.AddRange(KeyboardInputManager.Instance.GetKeyBinds());
                refreshKBCache = false;
            }
        }

        public void KeyBindingsEditorGUI()
        {
            if (!KeyboardInputManager.IsReady)
            {
                return;
            }

            bool guiPriorEnabled = GUI.enabled;

            GUI.enabled = guiPriorEnabled
                && HyperWarpController.IsReady
                && !HyperWarpController.Instance.IsHyperWarping
                && RailsWarpController.IsReady
                && !RailsWarpController.Instance.IsRailsWarping
                && SlowMoController.IsReady
                && !SlowMoController.Instance.IsSlowMo;
            
            GUILayout.Label( "Key Bindings:" );

            GUILayout.BeginVertical();
            {
                //Keys
                Color c = GUI.contentColor;
                CacheKeyBinds();

                foreach (TimeControlKeyBinding kb in keyBindings)
                {
                    if (kb.IsKeyAssigned)
                    {
                        GUI.contentColor = Color.yellow;
                    }
                    else
                    {
                        GUI.contentColor = c;
                    }

                    string buttonDesc = "";
                    //if (kb.TCUserAction == TimeControlUserAction.CustomKeySlider)
                    //{
                    //    string pos = (Settings.Instance.CustomKeySlider.ConvertToExp64() != 1) ? ("1/" + Settings.Instance.CustomKeySlider.ConvertToExp64().ToString()) : "1";
                    //    buttonDesc = "Custom-" + pos + 'x';
                    //}
                    //else
                    //{
                    buttonDesc = kb.Description;
                    //}

                    buttonDesc = buttonDesc + ": " + (kb.IsKeyAssigned ? kb.KeyCombinationString : "None");

                    //if (kb.TCUserAction == TimeControlUserAction.CustomKeySlider)
                    //    Settings.Instance.CustomKeySlider = GUILayout.HorizontalSlider( Settings.Instance.CustomKeySlider, 0f, 1f );

                    if (currentlyAssigningKey)
                    {
                        GUI.enabled = false;
                    }

                    bool assignKey = GUILayout.Button( buttonDesc );
                    if (assignKey)
                    {
                        GUIAssignKey( buttonDesc, kb );
                    }

                    GUI.enabled = true;
                }
                GUI.contentColor = c;
            }
            GUILayout.EndVertical();

            GUI.enabled = guiPriorEnabled;
        }

        private void GUIAssignKey(string buttonDesc, TimeControlKeyBinding kb)
        {
            const string logBlockName = nameof ( KeyBindingsEditorIMGUI ) + "." + nameof( KeyBindingsEditorIMGUI.GUIAssignKey );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                // Left Mouse Button, Assign Key
                if (Event.current.button == 0 && !currentlyAssigningKey)
                {
                    currentlyAssigningKey = true;
                    KeyboardInputManager.Instance.GetPressedKeyCombination( (lkc) =>
                    {
                        const string logBlockName2 = nameof( KeyBindingsEditorIMGUI ) + "." + nameof( KeyBindingsEditorIMGUI.GUIAssignKey ) + " - " + nameof ( KeyboardInputManager.Instance.GetPressedKeyCombination ) + " Callback";
                        using (EntryExitLogger.EntryExitLog( logBlockName2, EntryExitLoggerOptions.All ))
                        {
                            currentlyAssigningKey = false;
                            kb.KeyCombination = new List<KeyCode>( lkc );
                            kb.KeyCombinationString = KeyboardInputManager.GetKeyCombinationString( lkc );
                            KeyboardInputManager.Instance.AssignKeyBinding( kb );
                            refreshKBCache = true;
                            Log.Info( "Key Combination " + kb.KeyCombinationString + " assigned to button " + buttonDesc, logBlockName2 );
                            if (GlobalSettings.IsReady)
                            {
                                GlobalSettings.Instance.Save();
                            }
                        }
                    } );
                }
                // Right Mouse Button, Clear Assigned Key
                else if (Event.current.button == 1 && !currentlyAssigningKey)
                {
                    kb.KeyCombination.Clear();
                    kb.KeyCombinationString = "";
                }
            }
        }
    }
}

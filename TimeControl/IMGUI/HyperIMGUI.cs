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
using TimeControl.Framework;

namespace TimeControl
{
    internal class HyperIMGUI
    {
        private string hyperWarpHours = "0";
        private string hyperWarpMinutes = "0";
        private string hyperWarpSeconds = "0";
        
        SharedIMGUI sharedGUI;

        public HyperIMGUI()
        {
            sharedGUI = new SharedIMGUI();
        }

        public void HyperGUI()
        {
            if (!HyperWarpController.IsReady)
            {
                return;
            }

            GUI.enabled = HyperWarpController.Instance.CanHyperWarp;

            {
                GUILayout.BeginVertical();
                {
                    GUIMaxRate();
                    GUIMinPhys();
                    GUIButtons();
                    GUILayout.Label( "", GUILayout.Height( 5 ) );
                    GUIWarpTime();
                    GUILayout.Label( "", GUILayout.Height( 5 ) );
                    sharedGUI.GUIThrottleControl();
                }
                GUILayout.EndVertical();
            }
            GUI.enabled = true;
        }

        private void GUIMaxRate()
        {
            string hyperMaxRateLabel = "Attempted Rate: ".MemoizedConcat( Mathf.Round( HyperWarpController.Instance.MaxAttemptedRate ).MemoizedToString() );
            Action<float> updateHyperMaxRate = delegate (float f) { HyperWarpController.Instance.MaxAttemptedRate = f; };
            // Force slider to select integer values between min and max
            Func<float, float> modifyFieldHyperMaxRate = delegate (float f) { return Mathf.Floor( f ); };

            IMGUIExtensions.floatTextBoxSliderPlusMinus( hyperMaxRateLabel, HyperWarpController.Instance.MaxAttemptedRate, HyperWarpController.Instance.AttemptedRateMin, HyperWarpController.Instance.AttemptedRateMax, 1f, updateHyperMaxRate, modifyFieldHyperMaxRate );
            //IMGUIExtensions.floatTextBoxAndSliderCombo( hyperMaxRateLabel, HyperWarpController.Instance.MaxAttemptedRate, HyperWarpController.Instance.AttemptedRateMin, HyperWarpController.Instance.AttemptedRateMax, updateHyperMaxRate, modifyFieldHyperMaxRate );
        }

        private void GUIMinPhys()
        {
            const float physIncrement = 0.1f;

            string hyperMinPhysLabel = "Physics Accuracy: ".MemoizedConcat( HyperWarpController.Instance.PhysicsAccuracy.MemoizedToString() );
            
            Action<float> updatehyperMinPhys = delegate (float f) {
                HyperWarpController.Instance.PhysicsAccuracy = f;
            };
            
            Func<float, float> modifyFieldMinPhys = delegate (float f) { return Mathf.Floor( f * (1f/physIncrement)) / (1f/physIncrement); };

            IMGUIExtensions.floatTextBoxSliderPlusMinus( hyperMinPhysLabel, HyperWarpController.Instance.PhysicsAccuracy, HyperWarpController.Instance.PhysicsAccuracyMin, HyperWarpController.Instance.PhysicsAccuracyMax, physIncrement, updatehyperMinPhys, modifyFieldMinPhys );
            //IMGUIExtensions.floatTextBoxAndSliderCombo( hyperMinPhysLabel, HyperWarpController.Instance.PhysicsAccuracy, HyperWarpController.Instance.PhysicsAccuracyMin, HyperWarpController.Instance.PhysicsAccuracyMax, updatehyperMinPhys, modifyFieldMinPhys );
        }

        private void GUIButtons()
        {
            GUILayout.BeginHorizontal();
            {
                if (!HyperWarpController.Instance.IsHyperWarping)
                {
                    if (GUILayout.Button( "HyperWarp" ))
                    {
                        HyperWarpController.Instance.ToggleHyper();
                    }
                }
                else
                {
                    if (GUILayout.Button( "End HyperWarp" ))
                    {
                        HyperWarpController.Instance.DeactivateHyper();
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private void GUIWarpTime()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Timed Warp:" );
                hyperWarpHours = GUILayout.TextField( hyperWarpHours, GUILayout.Width( 35 ) );
                GUILayout.Label( "h " );
                hyperWarpMinutes = GUILayout.TextField( hyperWarpMinutes, GUILayout.Width( 35 ) );
                GUILayout.Label( "m " );
                hyperWarpSeconds = GUILayout.TextField( hyperWarpSeconds, GUILayout.Width( 35 ) );
                GUILayout.Label( "s" );
            }
            GUILayout.EndHorizontal();

            HyperWarpController.Instance.HyperPauseOnTimeReached = GUILayout.Toggle( HyperWarpController.Instance.HyperPauseOnTimeReached, "Pause on time reached" );

            if (GUILayout.Button( "Timed Warp" ))
            {
                int hrs = int.TryParse( hyperWarpHours, out hrs ) ? hrs : -1;
                int min = int.TryParse( hyperWarpMinutes, out min ) ? min : -1;
                int sec = int.TryParse( hyperWarpSeconds, out sec ) ? sec : -1;

                bool result = HyperWarpController.Instance.HyperWarpForDuration( hrs, min, sec );
                if (result)
                {
                    hyperWarpHours = "0";
                    hyperWarpMinutes = "0";
                    hyperWarpSeconds = "0";
                }
            }
        }
    }
}

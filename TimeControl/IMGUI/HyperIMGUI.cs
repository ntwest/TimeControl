using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeControl
{
    internal class HyperIMGUI
    {
        private string hyperWarpHours = "0";
        private string hyperWarpMinutes = "0";
        private string hyperWarpSeconds = "0";

        private List<float> maxDeltaButtons = new List<float>() { 0.02f, 0.08f, 0.2f };
        private List<float> maxRateButtons = new List<float>() { 5, 10, 20, 50 };
        private List<float> phyAccuracyButtons = new List<float>() { 1, 3, 6 };

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

            bool priorGUIEnabled = GUI.enabled;
            GUI.enabled = priorGUIEnabled && HyperWarpController.Instance.CanHyperWarp;

            {
                GUILayout.BeginVertical();
                {
                    GUIMaxRate();
                    GUIMinPhys();
                    GUIMaxDelta();
                    GUIButtons();
                    GUILayout.Label( "", GUILayout.Height( 5 ) );
                    GUIWarpTime();
                    GUILayout.Label( "", GUILayout.Height( 5 ) );
                    sharedGUI.GUIThrottleControl();
                }
                GUILayout.EndVertical();
            }
            GUI.enabled = priorGUIEnabled;
        }

        private void GUIMaxRate()
        {
            string hyperMaxRateLabel = "Attempted Rate: ".MemoizedConcat( Mathf.Round( HyperWarpController.Instance.MaxAttemptedRate ).MemoizedToString() );
            Action<float> updateHyperMaxRate = delegate (float f) { HyperWarpController.Instance.MaxAttemptedRate = f; };
            // Force slider to select integer values between min and max
            Func<float, float> modifyFieldHyperMaxRate = delegate (float f) { return Mathf.Round( f ); };

            IMGUIExtensions.floatTextBoxSliderPlusMinusWithButtonList( hyperMaxRateLabel, HyperWarpController.Instance.MaxAttemptedRate, HyperWarpController.AttemptedRateMin, HyperWarpController.AttemptedRateMax, 1f, updateHyperMaxRate, maxRateButtons, modifyFieldHyperMaxRate );
        }


        private void GUIMinPhys()
        {
            const float physIncrement = 0.1f;

            string hyperMinPhysLabel = "Physics Accuracy: ".MemoizedConcat( HyperWarpController.Instance.PhysicsAccuracy.MemoizedToString() );

            if (HyperWarpController.Instance.PhysicsAccuracy > 4f)
            {
                hyperMinPhysLabel = hyperMinPhysLabel.MemoizedConcat( " !!! DANGER !!!" );
            }

            Action<float> updatehyperMinPhys = delegate (float f)
            {
                HyperWarpController.Instance.PhysicsAccuracy = f;
            };

            Func<float, float> modifyFieldMinPhys = delegate (float f) { return Mathf.Round( f * (1f / physIncrement) ) / (1f / physIncrement); };

            IMGUIExtensions.floatTextBoxSliderPlusMinusWithButtonList( hyperMinPhysLabel, HyperWarpController.Instance.PhysicsAccuracy, HyperWarpController.PhysicsAccuracyMin, HyperWarpController.PhysicsAccuracyMax, physIncrement, updatehyperMinPhys, phyAccuracyButtons, modifyFieldMinPhys );
        }

        private void GUIMaxDelta()
        {
            const float deltaIncrement = 0.01f;

            string hyperMaxRateLabel = "Max Delta Time During Hyper-Warp: ".MemoizedConcat( HyperWarpController.Instance.MaximumDeltaTime.MemoizedToString() );

            if (HyperWarpController.Instance.MaximumDeltaTime > 0.12f)
            {
                hyperMaxRateLabel = hyperMaxRateLabel.MemoizedConcat( " - Low FPS Likely" );
            }

            Action<float> updateHyperMaxDelta = delegate (float f) { HyperWarpController.Instance.MaximumDeltaTime = f; };
            // Force slider to select integer values between min and max
            Func<float, float> modifyFieldHyperMaxDelta = delegate (float f) { return Mathf.Round( f * (1f / deltaIncrement) ) / (1f / deltaIncrement); };

            IMGUIExtensions.floatTextBoxSliderPlusMinus( hyperMaxRateLabel, HyperWarpController.Instance.MaximumDeltaTime, TimeController.MaximumDeltaTimeMin, TimeController.MaximumDeltaTimeMax, deltaIncrement, updateHyperMaxDelta, modifyFieldHyperMaxDelta );
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

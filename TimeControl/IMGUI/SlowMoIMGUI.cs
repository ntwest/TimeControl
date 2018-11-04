using System;
using UnityEngine;

namespace TimeControl
{
    internal class SlowMoIMGUI
    {
        //private int fpsMin = 5;
        //private int fpsKeeperFactor = 0;
        //private bool fpsKeeperActive;

        SharedIMGUI sharedGUI;
        //float ts = 0f;
        bool deltaLocked = true;

        public SlowMoIMGUI()
        {
            sharedGUI = new SharedIMGUI();
            deltaLocked = SlowMoController.Instance?.DeltaLocked ?? false;
        }

        public void SlowMoGUI()
        {
            //modeSlowmoFPSKeeper();            
            GUITimeScale();
            GUIButtons();
        }
        #region Slow-Mo GUI

        //private void modeSlowmoFPSKeeper()
        //{
        //    GUI.enabled = (TimeController.Instance.IsOperational
        //        && (TimeController.Instance.CurrentWarpState == TimeControllable.None || TimeController.Instance.CurrentWarpState == TimeControllable.SlowMo));

        //    bool fpsKeeperActive = GUILayout.Toggle( TimeController.Instance.IsFpsKeeperActive, "FPS Keeper: " + Mathf.Round( Settings.Instance.FpsMinSlider / 5 ) * 5 + " fps" );
        //    if (fpsKeeperActive != TimeController.Instance.IsFpsKeeperActive)
        //        TimeController.Instance.SetFPSKeeper( fpsKeeperActive );

        //    Settings.Instance.FpsMinSlider = (int)GUILayout.HorizontalSlider( Settings.Instance.FpsMinSlider, 5, 60 );

        //    GUI.enabled = true;
        //}

        private void GUITimeScale()
        {
            bool priorGUIEnabled = GUI.enabled;

            GUI.enabled = priorGUIEnabled && (SlowMoController.Instance?.CanSlowMo ?? false);

            GUILayout.BeginVertical();
            {
                SlowMoController.Instance.DeltaLocked = GUILayout.Toggle( SlowMoController.Instance.DeltaLocked, "Lock physics delta to default" );

                float ratePct = (float)Math.Round( SlowMoController.Instance.SlowMoRate * 100f, 0 );
                string slowMoSliderLabel = "Slow Motion Rate: ".MemoizedConcat( ratePct.MemoizedToString().MemoizedConcat( "%" ) );

                Action<float> updateSlowMo = delegate (float f)
                {
                    SlowMoController.Instance.SlowMoRate = (float)Math.Round( f / 100f, 2 );
                };

                Func<float, float> modifySlowMo = delegate (float f) { return Mathf.Floor( f ); };
                IMGUIExtensions.floatTextBoxSliderPlusMinus( slowMoSliderLabel, ratePct, 0f, 100f, 1f, updateSlowMo, modifySlowMo, true );

                GUILayout.Label( "", GUILayout.Height( 5 ) );

                sharedGUI.GUIThrottleControl();
            }
            GUILayout.EndVertical();

            GUI.enabled = priorGUIEnabled;
        }

        private void GUIButtons()
        {
            GUILayout.BeginHorizontal();
            {
                if (!SlowMoController.Instance.IsSlowMo)
                {
                    if (GUILayout.Button( "Activate Slow-Motion" ))
                    {
                        SlowMoController.Instance.ActivateSlowMo();
                    }
                }
                else
                {
                    if (GUILayout.Button( "Deactivate Slow-Motion" ))
                    {
                        SlowMoController.Instance.DeactivateSlowMo();
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        #endregion

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

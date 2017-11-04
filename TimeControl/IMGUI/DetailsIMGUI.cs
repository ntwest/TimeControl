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
    internal class DetailsIMGUI
    {
        private bool currentlyAssigningKey = false;
        double priorRT;

        public DetailsIMGUI()
        {
            priorRT = Time.realtimeSinceStartup;
        }
        
        public void DetailsGUI()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Label( "UT: " + Planetarium.GetUniversalTime() ); // Creates garbage, but not worth caching since it's monotonically increasing
                GUILayout.Label( "Time Scale: ".MemoizedConcat( TimeController.Instance.TimeScale.MemoizedToString() ) );
                GUILayout.Label( "Physics Delta: ".MemoizedConcat( TimeController.Instance.FixedDeltaTime.MemoizedToString() ) );
                // GUILayout.Label( "UT passing per sec: " + ((PerformanceManager.Instance?.PerformanceCountersOn ?? false) ? Math.Round(PerformanceManager.Instance.GametimeToRealtimeRatio).MemoizedToString() : "Not Enabled") );
                GUILayout.Label( "Physics Updates per sec: " + ( (PerformanceManager.Instance?.PerformanceCountersOn ?? false) ? PerformanceManager.Instance.PhysicsUpdatesPerSecond.MemoizedToString() : "Not Enabled" ) );
                GUILayout.Label( "Max Delta Time: ".MemoizedConcat( TimeController.Instance.MaxDeltaTime.MemoizedToString() ) );                
                TimeController.Instance.MaxDeltaTime = GUILayout.HorizontalSlider(TimeController.Instance.MaxDeltaTime, TimeController.MaxDeltaTimeSliderMax, TimeController.MaxDeltaTimeSliderMin);
                // PerformanceManager.Instance.PerformanceCountersOn = GUILayout.Toggle( PerformanceManager.Instance.PerformanceCountersOn, "Performance Counters" );
            }
            GUILayout.EndVertical();
        }
    }
}

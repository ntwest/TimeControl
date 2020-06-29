using System;
using UnityEngine;

namespace TimeControl
{
    internal class DetailsIMGUI
    {
        public DetailsIMGUI()
        {
        }

        public void DetailsGUI()
        {
            GUILayout.BeginVertical();
            {
                PerformanceManager.Instance.PerformanceCountersOn = GUILayout.Toggle( PerformanceManager.Instance.PerformanceCountersOn, "Performance Counters" );

                GUILayout.Label( "UT: " + Math.Round( Planetarium.GetUniversalTime(), 0 ) ); // Creates garbage, but not worth caching since it's monotonically increasing

                GUILayout.Label( "Time Scale: ".MemoizedConcat( TimeController.Instance.TimeScale.MemoizedToString() ) );
                GUILayout.Label( "Physics Delta: ".MemoizedConcat( TimeController.Instance.FixedDeltaTime.MemoizedToString() ) );

                if ((PerformanceManager.Instance?.PerformanceCountersOn ?? false))
                {
                    GUILayout.Label( "UT passing per sec: ".MemoizedConcat( Math.Round( PerformanceManager.Instance.GametimeToRealtimeRatio, 2 ).MemoizedToString() ) );
                    GUILayout.Label( "Physics Updates per sec: ".MemoizedConcat( Math.Round( PerformanceManager.Instance.PhysicsUpdatesPerSecond, 2 ).MemoizedToString() ) );
                }
                else
                {
                    GUILayout.Label( "UT passing per sec: N/A" );
                    GUILayout.Label( "Physics Updates per sec: N/A" );
                }

                GUILayout.Label( "Current Max Delta Time: ".MemoizedConcat( TimeController.Instance.MaximumDeltaTime.MemoizedToString() ) );
                GUILayout.Label( "Max Delta Time Setting: ".MemoizedConcat( TimeController.Instance.MaximumDeltaTimeSetting.MemoizedToString() ) );
                

                TimeController.Instance.MaximumDeltaTimeSetting = GUILayout.HorizontalSlider( TimeController.Instance.MaximumDeltaTimeSetting, TimeController.MaximumDeltaTimeMax, TimeController.MaximumDeltaTimeMin );
            }
            GUILayout.EndVertical();
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

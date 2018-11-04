using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeControl
{
    internal class SharedIMGUI
    {
        private List<float> throttleRateButtons = new List<float>() { 0, 50, 100 };

        public SharedIMGUI()
        {

        }

        bool throttleToggle = false;
        float throttleSet = 0f;

        internal void GUIThrottleControl()
        {
            throttleToggle = GUILayout.Toggle( throttleToggle, "Throttle Control: " + Mathf.Round( throttleSet * 100 ) + "%" );

            Action<float> updateThrottle = delegate (float f)
            {
                throttleSet = f / 100.0f;
            };

            if (FlightInputHandler.state != null && throttleToggle && FlightInputHandler.state.mainThrottle != throttleSet)
            {
                FlightInputHandler.state.mainThrottle = throttleSet;
            }

            // Force slider to select 1 decimal place values between min and max
            Func<float, float> modifyFieldThrottle = delegate (float f)
            {
                return (Mathf.Floor( f ));
            };

            IMGUIExtensions.floatTextBoxSliderPlusMinusWithButtonList( null, (throttleSet * 100f), 0.0f, 100.0f, 1f, updateThrottle, throttleRateButtons, modifyFieldThrottle );
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

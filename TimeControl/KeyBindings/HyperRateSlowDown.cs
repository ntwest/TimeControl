using System;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class HyperRateSlowDown : TimeControlKeyBindingValue
    {
        private float v = 1f;

        private void UpdateDescription()
        {
            Description = String.Format( "Hyper-Warp Rate -{0}x", v );
        }

        public HyperRateSlowDown()
        {
            TimeControlKeyActionName = TimeControlKeyAction.HyperRateSlowDown;
            SetDescription = "Hyper-Warp Decrease Rate By: ";
            FireWhileHoldingKeyDown = true;
            UpdateDescription();
        }

        public override float VMax
        {
            get => 50f;
        }

        public override float VMin
        {
            get => 1f;
        }

        public override float V
        {
            get => v;
            set
            {
                if (value >= VMax)
                {
                    v = VMax;
                }
                else if (value <= VMin)
                {
                    v = VMin;
                }
                else
                {
                    v = (float)Mathf.RoundToInt( value );
                }

                UpdateDescription();
            }
        }

        public override void Press()
        {
            if (HyperWarpController.IsReady)
            {
                HyperWarpController.Instance.SlowDown( v );
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

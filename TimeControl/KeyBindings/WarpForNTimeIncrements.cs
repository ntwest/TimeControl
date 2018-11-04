using System;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class WarpForNTimeIncrements : TimeControlKeyBindingValue
    {
        public enum TimeIncrement
        {
            Seconds = 1,
            Minutes = 2,
            Hours = 3,
            Days = 4,
            Years = 5
        }

        private TimeIncrement ti = TimeIncrement.Seconds;
        public TimeIncrement TI
        {
            get => ti;
            set
            {
                ti = value;
                UpdateDescription();
            }
        }

        private float v = 1f;

        private double CurrentUT
        {
            get => Planetarium.GetUniversalTime();
        }

        private void UpdateDescription()
        {
            Description = String.Format( "Rails Warp for {0} {1}", v, ti.ToString() );
            SetDescription = String.Format( "Rails Warp for # {0}", ti.ToString() ); ;
        }

        public WarpForNTimeIncrements()
        {
            TimeControlKeyActionName = TimeControlKeyAction.WarpForNTimeIncrements;
            UpdateDescription();
        }

        public WarpForNTimeIncrements(TimeIncrement pti)
        {
            ti = pti;
            TimeControlKeyActionName = TimeControlKeyAction.WarpForNTimeIncrements;
            UpdateDescription();
        }

        public override float VMax
        {
            get => Mathf.Infinity;
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

        public override ConfigNode GetConfigNode()
        {
            ConfigNode newNode = base.GetConfigNode();
            newNode.AddValue( "TI", TI );
            return newNode;
        }

        public override void Press()
        {
            if (!RailsWarpController.IsReady)
            {
                return;
            }

            switch (ti)
            {
                case TimeIncrement.Seconds:
                    RailsWarpController.Instance.RailsWarpForDuration( 0, 0, 0, 0, V );
                    break;
                case TimeIncrement.Minutes:
                    RailsWarpController.Instance.RailsWarpForDuration( 0, 0, 0, V, 0 );
                    break;
                case TimeIncrement.Hours:
                    RailsWarpController.Instance.RailsWarpForDuration( 0, 0, V, 0, 0 );
                    break;
                case TimeIncrement.Days:
                    RailsWarpController.Instance.RailsWarpForDuration( 0, V, 0, 0, 0 );
                    break;
                case TimeIncrement.Years:
                    RailsWarpController.Instance.RailsWarpForDuration( V, 0, 0, 0, 0 );
                    break;
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

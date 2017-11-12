using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            UpdateDescription();
        }

        override public float VMax
        {
            get => 50f;
        }

        override public float VMin
        {
            get => 1f;
        }

        override public float V
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

        override public void Press()
        {
            if (HyperWarpController.IsReady)
            {
                HyperWarpController.Instance.SlowDown( v );
            }
        }
    }
}

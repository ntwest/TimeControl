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

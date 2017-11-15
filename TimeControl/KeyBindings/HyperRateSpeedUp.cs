using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class HyperRateSpeedUp : TimeControlKeyBindingValue
    {
        private float v = 1f;

        private void UpdateDescription()
        {
            Description = String.Format( "Hyper-Warp Rate +{0}x", v );
        }

        public HyperRateSpeedUp()
        {
            TimeControlKeyActionName = TimeControlKeyAction.HyperRateSpeedUp;
            SetDescription = "Hyper-Warp Increase Rate By: ";
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
                HyperWarpController.Instance.SpeedUp( v );
            }
        }
    }
}

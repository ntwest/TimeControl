using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class HyperRateSetRate : TimeControlKeyBindingValue
    {
        private float v = 2f;

        private void UpdateDescription()
        {
            Description = String.Format( "Set Hyper-Warp Rate to {0}", v );
        }

        public HyperRateSetRate()
        {
            TimeControlKeyActionName = TimeControlKeyAction.HyperRateSetRate;
            SetDescription = "Hyper-Warp Set Rate To: ";
            UpdateDescription();
        }

        public override float VMax
        {
            get => HyperWarpController.AttemptedRateMax;
        }

        public override float VMin
        {
            get => HyperWarpController.AttemptedRateMin;
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
                HyperWarpController.Instance.MaxAttemptedRate = v;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class WarpForNSeconds : TimeControlKeyBindingValue
    {
        private float v = 1f;

        private double CurrentUT
        {
            get => Planetarium.GetUniversalTime();
        }

        private void UpdateDescription()
        {
            Description = String.Format( "Rails Warp for {0} Seconds", v );
        }    

        public WarpForNSeconds()
        {            
            TimeControlKeyActionName = TimeControlKeyAction.WarpForNSeconds;
            SetDescription = "Rails Warp for # Seconds: ";
            UpdateDescription();
        }

        override public float VMax
        {
            get => Mathf.Infinity;
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
            if (!RailsWarpController.IsReady)
            {
                return;
            }

            double TargetUT = CurrentUT + V;
            RailsWarpController.Instance.RailsWarpToUT( TargetUT );
        }
    }
}

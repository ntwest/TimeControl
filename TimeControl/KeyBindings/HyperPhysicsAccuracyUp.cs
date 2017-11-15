using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class HyperPhysicsAccuracyUp : TimeControlKeyBindingValue
    {
        private float v = 0.5f;

        private void UpdateDescription()
        {
            Description = String.Format( "Hyper-Warp Accuracy +{0}", v );
        }

        public HyperPhysicsAccuracyUp()
        {
            TimeControlKeyActionName = TimeControlKeyAction.HyperPhysicsAccuracyUp;
            SetDescription = "Hyper-Warp Increase Accuracy By: ";
            UpdateDescription();
        }

        public override float VMax
        {
            get => 3f;
        }

        public override float VMin
        {
            get => 0.05f;
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
                    v = (float)Math.Round( value, 2 );
                }

                UpdateDescription();
            }
        }

        public override void Press()
        {
            if (HyperWarpController.IsReady)
            {
                HyperWarpController.Instance.IncreasePhysicsAccuracy( v );
            }
        }
    }
}

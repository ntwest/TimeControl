using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class SlowMoSpeedUp : TimeControlKeyBindingValue
    {
        private float v = 0.01f;

        private float VPercent
        {
            get => Mathf.RoundToInt( v * 100 );
        }

        private void UpdateDescription()
        {
            Description = String.Format( "Slow-Mo Rate +{0}%", VPercent );
        }

        public SlowMoSpeedUp()
        {
            TimeControlKeyActionName = TimeControlKeyAction.SlowMoSpeedUp;
            FireWhilePressedDown = true;
            SetDescription = "Slow-Motion Increase Rate By: ";
            UpdateDescription();
        }

        override public float VMax
        {
            get => 0.5f;
        }

        override public float VMin
        {
            get => 0.01f;
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
                    v = (float)Math.Round( value, 2 );
                }

                UpdateDescription();
            }
        }

        override public void Press()
        {
            if (SlowMoController.IsReady)
            {
                SlowMoController.Instance.SpeedUp( v );
            }
        }
    }
}

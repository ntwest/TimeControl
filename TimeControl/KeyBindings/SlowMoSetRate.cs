using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class SlowMoSetRate : TimeControlKeyBindingValue
    {
        private float v = 1f;

        private float VPercent
        {
            get => Mathf.RoundToInt( v * 100 );
        }

        private void UpdateDescription()
        {
            Description = String.Format( "Set Slow-Mo Rate to {0}%", VPercent );
        }

        public SlowMoSetRate()
        {
            TimeControlKeyActionName = TimeControlKeyAction.SlowMoSetRate;
            SetDescription = "Slow-Motion Set Rate To: ";
            UpdateDescription();
        }

        public override float VMax
        {
            get => 1f;
        }

        public override float VMin
        {
            get => 0.01f;
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
            if (SlowMoController.IsReady)
            {
                SlowMoController.Instance.SlowMoRate = v;
            }
        }
    }
}

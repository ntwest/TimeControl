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
            Description = String.Format( "Set Slow-Mo Rate to {0}", VPercent );
        }

        public SlowMoSetRate()
        {
            TimeControlKeyActionName = TimeControlKeyAction.SlowMoSetRate;
            SetDescription = "Slow-Motion Set Rate To: ";
            UpdateDescription();
        }

        override public float VMax
        {
            get => 1f;
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
                SlowMoController.Instance.SlowMoRate = v;
            }
        }
    }
}

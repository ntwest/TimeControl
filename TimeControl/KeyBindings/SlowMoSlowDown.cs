using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class SlowMoSlowDown : TimeControlKeyBindingValue
    {
        private float v = 0.01f;

        private float fireDelta = 0.05f;
        private float timeDelay = 0.0f;

        private float VPercent
        {
            get => Mathf.RoundToInt( v * 100 );
        }

        private void UpdateDescription()
        {
            Description = String.Format( "Slow-Mo Rate -{0}%", VPercent );
        }

        public SlowMoSlowDown()
        {            
            TimeControlKeyActionName = TimeControlKeyAction.SlowMoSlowDown;
            SetDescription = "Slow-Motion Decrease Rate By: ";
            FireWhilePressedDown = true;
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
            timeDelay = timeDelay + Time.deltaTime;
            if (timeDelay > fireDelta)
            {
                timeDelay = 0.0f;
                if (SlowMoController.IsReady)
                {
                    SlowMoController.Instance.SlowDown( v );
                }
            }
        }
    }
}

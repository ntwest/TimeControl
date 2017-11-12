using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class TimeStep : TimeControlKeyBinding
    {
        public TimeStep()
        {
            TimeControlKeyActionName = TimeControlKeyAction.TimeStep;
            Description = "Increment Time Step";
            IsUserDefined = false;
        }

        override public void Press()
        {
            if (TimeController.IsReady)
            {
                TimeController.Instance.IncrementTimeStep();
            }
        }
    }
}

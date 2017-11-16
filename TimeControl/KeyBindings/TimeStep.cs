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
            SetDescription = Description = "Increment Time Step";
        }

        public override void Press()
        {
            if (TimeController.IsReady)
            {
                TimeController.Instance.IncrementTimeStep();
            }
        }
    }
}

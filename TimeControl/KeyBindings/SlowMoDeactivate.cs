using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class SlowMoDeactivate : TimeControlKeyBinding
    {
        public SlowMoDeactivate()
        {
            TimeControlKeyActionName = TimeControlKeyAction.SlowMoDeactivate;
            Description = "Deactivate Slow-Motion";
        }

        public override void Press()
        {
            if (SlowMoController.IsReady)
            {
                SlowMoController.Instance.DeactivateSlowMo();
            }
        }
    }
}

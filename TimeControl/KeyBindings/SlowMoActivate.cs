using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class SlowMoActivate : TimeControlKeyBinding
    {
        public SlowMoActivate()
        {
            TimeControlKeyActionName = TimeControlKeyAction.SlowMoActivate;
            Description = "Activate Slow-Motion";
        }

        public override void Press()
        {
            if (SlowMoController.IsReady)
            {
                SlowMoController.Instance.ActivateSlowMo();
            }
        }
    }
}

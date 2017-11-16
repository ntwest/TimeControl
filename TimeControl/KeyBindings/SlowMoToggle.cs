using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class SlowMoToggle : TimeControlKeyBinding
    {
        public SlowMoToggle()
        {
            TimeControlKeyActionName = TimeControlKeyAction.SlowMoToggle;
            SetDescription = Description = "Toggle Slow-Motion";
        }

        public override void Press()
        {
            if (SlowMoController.IsReady)
            {
                SlowMoController.Instance.ToggleSlowMo();
            }
        }
    }
}

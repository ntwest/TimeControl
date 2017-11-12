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
            Description = "Toggle Slow-Motion";
            IsUserDefined = false;
        }

        override public void Press()
        {
            if (SlowMoController.IsReady)
            {
                SlowMoController.Instance.ToggleSlowMo();
            }
        }
    }
}

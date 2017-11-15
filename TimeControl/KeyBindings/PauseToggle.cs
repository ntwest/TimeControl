using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class PauseToggle : TimeControlKeyBinding
    {
        public PauseToggle()
        {
            TimeControlKeyActionName = TimeControlKeyAction.PauseToggle;
            Description = "Toggle Pause";
        }

        public override void Press()
        {
            if (TimeController.IsReady)
            {
                TimeController.Instance.TogglePause();
            }
        }
    }
}

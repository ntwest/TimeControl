using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class GUIToggle : TimeControlKeyBinding
    {
        public GUIToggle()
        {
            TimeControlKeyActionName = TimeControlKeyAction.GUIToggle;
            SetDescription = Description = "Toggle GUI";
        }

        public override void Press()
        {
            if (TimeControlIMGUI.IsReady)
            {
                TimeControlIMGUI.Instance.ToggleGUIVisibility();
            }
        }
    }
}

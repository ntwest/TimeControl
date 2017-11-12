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
            Description = "Toggle GUI";
            IsUserDefined = false;
        }

        override public void Press()
        {
            if (TimeControlIMGUI.IsReady)
            {
                TimeControlIMGUI.Instance.ToggleGUIVisibility();
            }
        }
    }
}

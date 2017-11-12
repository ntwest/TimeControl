using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class HyperToggle : TimeControlKeyBinding
    {
        public HyperToggle()
        {
            TimeControlKeyActionName = TimeControlKeyAction.HyperToggle;
            Description = "Toggle Hyper-Warp";
            IsUserDefined = false;
        }

        override public void Press()
        {
            if (HyperWarpController.IsReady)
            {
                HyperWarpController.Instance.ToggleHyper();
            }
        }
    }
}

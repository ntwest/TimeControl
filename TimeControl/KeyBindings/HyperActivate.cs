using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class HyperActivate : TimeControlKeyBinding
    {
        public HyperActivate()
        {
            TimeControlKeyActionName = TimeControlKeyAction.HyperActivate;
            Description = "Activate Hyper-Warp";
        }

        public override void Press()
        {
            if (HyperWarpController.IsReady)
            {
                HyperWarpController.Instance.ActivateHyper();
            }
        }
    }
}

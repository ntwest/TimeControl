using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class HyperDeactivate : TimeControlKeyBinding
    {
        public HyperDeactivate()
        {
            TimeControlKeyActionName = TimeControlKeyAction.HyperDeactivate;
            SetDescription = Description = "Dectivate Hyper-Warp";
        }

        public override void Press()
        {
            if (HyperWarpController.IsReady)
            {
                HyperWarpController.Instance.DeactivateHyper();
            }
        }
    }
}

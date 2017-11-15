using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class Realtime: TimeControlKeyBinding
    {
        public Realtime()
        {
            TimeControlKeyActionName = TimeControlKeyAction.Realtime;
            Description = "Realtime";
        }

        public override void Press()
        {
            if (TimeController.IsReady)
            {
                TimeController.Instance.GoRealTime();
            }
        }
    }
}

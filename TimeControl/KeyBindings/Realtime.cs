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
            IsUserDefined = false;
        }

        override public void Press()
        {
            if (TimeController.IsReady)
            {
                TimeController.Instance.GoRealTime();
            }
        }
    }
}

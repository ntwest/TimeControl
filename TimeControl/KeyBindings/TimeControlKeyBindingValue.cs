using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public abstract class TimeControlKeyBindingValue : TimeControlKeyBinding
    {
        abstract public float VMax
        {
            get;
        }

        abstract public float VMin
        {
            get;
        }

        abstract public float V
        {
            get; set;
        }
    }
}

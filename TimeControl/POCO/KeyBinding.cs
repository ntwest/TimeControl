using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeControl
{
    public struct TimeControlKeyBinding
    { 
        public string Description { get; set; }
        public string KeyCombinationString { get; set; }
        public List<KeyCode> KeyCombination { get; set; }
        public bool IsKeyAssigned { get { return KeyCombination.Count != 0; } }
        public TimeControlUserAction TCUserAction;

        public TimeControlKeyBinding Copy()
        {
            List<KeyCode> newKeyCombination = new List<KeyCode>();
            newKeyCombination.AddRange( KeyCombination );

            return new TimeControlKeyBinding { Description = Description, KeyCombination = newKeyCombination, KeyCombinationString = KeyCombinationString, TCUserAction = TCUserAction };
        }
    }
}

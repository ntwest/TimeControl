using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeControl
{
    public class TCKeyBinding
    { 
        public bool IsKeyAssigned { get { return KeyCombination.Count != 0; } }        
        public string Description { get; set; }
        public string KeyCombinationString { get; set; }
        public List<KeyCode> KeyCombination { get; set; }
        public TimeControlUserAction TCUserAction { get; set; }
    }
}

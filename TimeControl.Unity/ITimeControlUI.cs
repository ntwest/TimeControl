using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeControl.Unity
{
    public interface ITimeControlUI
    {
        string Version { get; }

        bool ShowOrbit { get; set; }

        float Alpha { get; set; }
    }
}

using System.Collections;
using System.Reflection;

namespace TimeControl
{
    public class TimeControlParameterNode : GameParameters.CustomParameterNode
    {
        public override string Section { get { return "Time Control"; } }
        public override string DisplaySection { get { return Section; } }
        public override int SectionOrder { get { return 1; } }
        public override string Title { get { return "Time Control"; } }

        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }                
        public override bool HasPresets { get { return true; } }

        [GameParameters.CustomParameterUI("Use Stock Toolbar", toolTip = "")]
        public bool UseStockToolbar = true;
        [GameParameters.CustomParameterUI("Use Blizzy Toolbar", toolTip = "")]
        public bool UseBlizzyToolbar = true;
        [GameParameters.CustomParameterUI( "Use Kerbin Time", toolTip = "" )]
        public bool UseKerbinTime = GameSettings.KERBIN_TIME;
        [GameParameters.CustomParameterUI("Show Hyper-Warp Onscreen Messages", toolTip = "")]
        public bool ShowHyperOnscreenMessages = true;
        [GameParameters.CustomParameterUI( "Show Slow-Motion Onscreen Messages", toolTip = "" )]
        public bool ShowSlowMoOnscreenMessages = true;
        [GameParameters.CustomParameterUI( "Camera Zoom Fix", toolTip = "" )]
        public bool CameraZoomFix = true;

#if DEBUG
        [GameParameters.CustomParameterUI( "Debug Logging Level", toolTip = "" )]
        public LogSeverity LoggingLevel = LogSeverity.Trace;
#else
        [GameParameters.CustomParameterUI("Debug Logging Level", toolTip = "")]
        public LogSeverity LoggingLevel = LogSeverity.Warning;
#endif

        [GameParameters.CustomIntParameterUI( "Key Repeat Start", minValue = 0, maxValue = 1000, stepSize = 100, toolTip = "For repeatable key bindings, the time in milliseconds the key must be held down before starting to repeat." )]
        public int KeyRepeatStart = 500;
        [GameParameters.CustomIntParameterUI( "Key Repeat Interval", minValue = 1, maxValue = 60, stepSize = 1, toolTip = "For repeatable key bindings, the number of times key is repeated per second." )]
        public int KeyRepeatInterval = 15;

        [GameParameters.CustomStringParameterUI("UIExperimentalFeaturesString", autoPersistance = true, lines = 2, title = "Experimental Features", toolTip = "")]
        public string UIExperimentalFeaturesString = "";

        [GameParameters.CustomParameterUI("Supress the Flight Results Dialog", toolTip = "")]
        public bool SupressFlightResultsDialog = false;

        //[GameParameters.CustomParameterUI("Custom TimeControl Date Formatter", toolTip = "")]
        //public bool UseCustomDateTimeFormatter = true;
        
        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            /*
            if (member.Name == "MyBool") //This Field must always be enabled.
                return true;
            if (MyBool == false) //Otherwise it depends on the value of MyBool if it's false return false
            {
                if (member.Name == "UIstring" || member.Name == "MyFloat") // Example these fields are Enabled (visible) all the time.
                    return true;
                return false;
            }
            */

            return true; //otherwise return true
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            /*
            if (member.Name == "MyBool") //This Field must always be Interactible.
                return true;
            if (MyBool == false)  //Otherwise it depends on the value of MyBool if it's false return false
                return false;
            */

            return true; //otherwise return true
        }

        public override IList ValidValues(MemberInfo member)
        {
            /*
            if (member.Name == "MyIlist")
            {
                List<string> myList = new List<string>();
                foreach (CelestialBody cb in FlightGlobals.Bodies)
                {
                    myList.Add(cb.name);
                }
                IList myIlist = myList;
                return myIlist;
            }
            else
            {
                return null;
            }
            */
            return null;
        }
        public override void OnSave(ConfigNode node)
        {
            const string logBlockName = nameof( TimeControlParameterNode ) + "." + nameof( OnSave );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                
            }
        }
        
        public override void OnLoad(ConfigNode node)
        {
            
            const string logBlockName = nameof( TimeControlParameterNode ) + "." + nameof( OnLoad );           
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (GlobalSettings.IsReady)
                {
                    LoggingLevel = GlobalSettings.Instance.LoggingLevel;
                    CameraZoomFix = GlobalSettings.Instance.CameraZoomFix;
                    KeyRepeatStart = GlobalSettings.Instance.KeyRepeatStart;
                    KeyRepeatInterval = GlobalSettings.Instance.KeyRepeatInterval;
                }

                UseKerbinTime = GameSettings.KERBIN_TIME;
            }
        }
        
        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            const string logBlockName = nameof( TimeControlParameterNode ) + "." + nameof( SetDifficultyPreset );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {

            }
        }
    }
}


/*
All code in this file Copyright(c) 2016 Nate West

The MIT License (MIT)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

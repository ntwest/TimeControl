using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

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
        [GameParameters.CustomParameterUI("Debug Logging Level", toolTip = "")]
        public LogSeverity LoggingLevel = LogSeverity.Trace;
        


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

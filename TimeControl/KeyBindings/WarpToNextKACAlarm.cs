using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class WarpToNextKACAlarm : TimeControlKeyBinding
    {
        private bool TriedToHookToKAC = false;

        private double CurrentUT
        {
            get => Planetarium.GetUniversalTime();
        }

        private void UpdateDescription()
        {
            Description = "Rails Warp to Next KAC Alarm";
        }

        public WarpToNextKACAlarm()
        {
            TimeControlKeyActionName = TimeControlKeyAction.WarpToNextKACAlarm;
            UpdateDescription();

            GameEvents.onLevelWasLoaded.Add( onLevelWasLoaded );

        }

        private void onLevelWasLoaded(GameScenes gs)
        {
            if (!KACWrapper.InstanceExists)
            {
                TriedToHookToKAC = false;
            }
        }
        
        override public void Press()
        {
            if (!TimeController.IsReady || !RailsWarpController.IsReady || !KACWrapper.InstanceExists || !(HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION))
            {
                return;
            }
            
            if (TimeController.Instance.ClosestKACAlarm != null)
            {
                double TargetUT = TimeController.Instance.ClosestKACAlarm.AlarmTime;
                RailsWarpController.Instance.RailsWarpToUT( TargetUT );
            }
        }
    }
}

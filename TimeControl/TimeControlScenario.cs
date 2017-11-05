using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP;
using UnityEngine;
using KSPPluginFramework;

namespace TimeControl
{

    [KSPScenario( ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.TRACKSTATION )]
    internal class TimeControlScenario : ScenarioModule
    {
        public override void OnLoad(ConfigNode gameNode)
        {
            base.OnLoad( gameNode );

            RailsWarpController.gameNode = gameNode;

            if (RailsWarpController.IsReady)
            {
                RailsWarpController.Instance.Load( gameNode );
            }
        }

        public override void OnSave(ConfigNode gameNode)
        {
            base.OnSave( gameNode );
            
            if (RailsWarpController.IsReady)
            {
                RailsWarpController.Instance.Save( gameNode );
            }
        }
    }
}
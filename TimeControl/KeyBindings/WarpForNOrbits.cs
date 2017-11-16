using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class WarpForNOrbits : TimeControlKeyBindingValue
    {
        private static List<Orbit.PatchTransitionType> UnstableOrbitTransitions = new List<Orbit.PatchTransitionType> { Orbit.PatchTransitionType.ENCOUNTER, Orbit.PatchTransitionType.ESCAPE, Orbit.PatchTransitionType.IMPACT };

        private float v = 1f;

        private double CurrentUT
        {
            get => Planetarium.GetUniversalTime();
        }

        private void UpdateDescription()
        {
            Description = String.Format( "Rails Warp for {0} Orbits", v );
        }    

        public WarpForNOrbits()
        {            
            TimeControlKeyActionName = TimeControlKeyAction.WarpForNOrbits;
            SetDescription = "Rails Warp for # Orbits: ";
            UpdateDescription();
        }

        public override float VMax
        {
            get => Mathf.Infinity;
        }

        public override float VMin
        {
            get => 1f;
        }

        public override float V
        {
            get => v;
            set
            {
                if (value >= VMax)
                {
                    v = VMax;
                }
                else if (value <= VMin)
                {
                    v = VMin;
                }
                else
                {
                    v = (float)Mathf.RoundToInt( value );
                }

                UpdateDescription();
            }
        }
        
        public override void Press()
        {
            Vessel vsl = FlightGlobals.ActiveVessel;

            // If no vessel or stable orbit, don't do anything
            if (!RailsWarpController.IsReady || vsl?.orbit == null || vsl.Landed || UnstableOrbitTransitions.Contains( vsl.orbit.patchEndTransition ))
            {
                return;
            }

            double TargetUT = CurrentUT + (vsl.orbit.period * this.V);
            RailsWarpController.Instance.RailsWarpToUT( TargetUT );
        }
    }
}

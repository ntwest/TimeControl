using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public class WarpToVesselOrbitLocation : TimeControlKeyBindingValue
    {
        private static List<Orbit.PatchTransitionType> SOITransitions = new List<Orbit.PatchTransitionType> { Orbit.PatchTransitionType.ENCOUNTER, Orbit.PatchTransitionType.ESCAPE };

        public enum VesselOrbitLocation
        {
            Ap = 1,
            Pe = 2,
            AN = 3,
            DN = 4,
            SOI = 5,
            ManuverNode = 6
        }

        
        private VesselOrbitLocation vesselLocation;
        public VesselOrbitLocation VesselLocation
        {
            get => vesselLocation;
            set
            {
                vesselLocation = value;
                UpdateDescription();
            }
        }

        private double CurrentUT
        {
            get => Planetarium.GetUniversalTime();
        }

        private void UpdateDescription()
        {
            if (Mathf.Approximately( v, 0 ))
            {
                Description = String.Format( "Rails Warp to {0}", VesselLocation.ToString() );
            }
            else
            {
                Description = String.Format( "Rails Warp to {0} - {1} seconds", VesselLocation.ToString(), v );
            }
            SetDescription = String.Format( "Rails Warp to {0} (- X sec): ", VesselLocation.ToString() );
        }

        public WarpToVesselOrbitLocation()
        {
            TimeControlKeyActionName = TimeControlKeyAction.WarpToVesselOrbitLocation;            
            UpdateDescription();
        }

        public WarpToVesselOrbitLocation(VesselOrbitLocation vol)
        {
            TimeControlKeyActionName = TimeControlKeyAction.WarpToVesselOrbitLocation;
            VesselLocation = vol;
            UpdateDescription();
        }

        public override float VMax
        {
            get => Mathf.Infinity;
        }

        public override float VMin
        {
            get => 0f;
        }

        private float v = 0f;
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

        public override ConfigNode GetConfigNode()
        {
            ConfigNode newNode = base.GetConfigNode();
            newNode.AddValue( "VesselOrbitLocation", VesselLocation );
            return newNode;
        }

        public override void Press()
        {
            Vessel vsl = FlightGlobals.ActiveVessel;

            // If no vessel or orbit, don't do anything
            if (!RailsWarpController.IsReady || vsl?.orbit == null || vsl.Landed)
            {
                return;
            }

            Orbit tgtOrbit = vsl?.targetObject?.GetOrbit();
            switch (VesselLocation)
            {
                case VesselOrbitLocation.Ap:
                    if ((vsl.orbit.ApA >= 0))
                    {
                        double TargetUT = CurrentUT + vsl.orbit.timeToAp - V;
                        RailsWarpController.Instance.RailsWarpToUT( TargetUT );
                    }
                    break;
                case VesselOrbitLocation.Pe:
                    if ((vsl.orbit.PeA >= 0))
                    {
                        double TargetUT = CurrentUT + vsl.orbit.timeToPe - V;
                        RailsWarpController.Instance.RailsWarpToUT( TargetUT );
                    }
                    break;
                case VesselOrbitLocation.AN:
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                    {
                        if (tgtOrbit == null)
                        {
                            if ((vsl.orbit.AscendingNodeEquatorialExists()))
                            {
                                double TargetUT = vsl.orbit.TimeOfAscendingNodeEquatorial( CurrentUT ) - V;
                                RailsWarpController.Instance.RailsWarpToUT( TargetUT );
                            }
                        }
                        else
                        {
                            if ((vsl.orbit.AscendingNodeExists( tgtOrbit )))
                            {
                                double TargetUT = vsl.orbit.TimeOfAscendingNode( tgtOrbit, CurrentUT ) - V;
                                RailsWarpController.Instance.RailsWarpToUT( TargetUT );
                            }
                        }
                    }
                    break;
                case VesselOrbitLocation.DN:
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                    {
                        if (tgtOrbit == null)
                        {
                            if ((vsl.orbit.DescendingNodeEquatorialExists()))
                            {
                                double TargetUT = vsl.orbit.TimeOfDescendingNodeEquatorial( CurrentUT ) - V;
                                RailsWarpController.Instance.RailsWarpToUT( TargetUT );
                            }
                        }
                        else
                        {
                            if ((vsl.orbit.DescendingNodeExists( tgtOrbit )))
                            {
                                double TargetUT = vsl.orbit.TimeOfDescendingNode( tgtOrbit, CurrentUT ) - V;
                                RailsWarpController.Instance.RailsWarpToUT( TargetUT );
                            }
                        }
                    }
                    break;
                case VesselOrbitLocation.SOI:
                    if ((SOITransitions.Contains( vsl.orbit.patchEndTransition )))
                    {
                        double TargetUT = vsl.orbit.EndUT - V;
                        RailsWarpController.Instance.RailsWarpToUT( TargetUT );
                    }
                    break;
                case VesselOrbitLocation.ManuverNode:
                    var mn = vsl?.FirstUpcomingManuverNode( this.CurrentUT );
                    if ((mn != null))
                    {
                        double TargetUT = mn.UT - V;
                        RailsWarpController.Instance.RailsWarpToUT( TargetUT );
                    }
                    break;
            }
        }
    }
}

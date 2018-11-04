using System.Collections.Generic;
using UnityEngine;

namespace TimeControl
{
    internal class RailsWarpToIMGUI
    {
        private static List<Orbit.PatchTransitionType> SOITransitions = new List<Orbit.PatchTransitionType> { Orbit.PatchTransitionType.ENCOUNTER, Orbit.PatchTransitionType.ESCAPE };
        private static List<Orbit.PatchTransitionType> UnstableOrbitTransitions = new List<Orbit.PatchTransitionType> { Orbit.PatchTransitionType.ENCOUNTER, Orbit.PatchTransitionType.ESCAPE, Orbit.PatchTransitionType.IMPACT };

        private string warpYears = "0";
        private string warpDays = "0";
        private string warpHours = "0";
        private string warpMinutes = "0";
        private string warpSeconds = "0";

        private double targetUT = 0;
        private string targetUTtextfield = "0";

        private double TargetUT
        {
            get
            {
                return targetUT;
            }
            set
            {
                targetUT = value;
                if (targetUT < CurrentUT)
                {
                    targetUT = CurrentUT;
                }
                targetUTtextfield = targetUT.ToString();
            }
        }

        private Vessel currentV;
        private ITargetable currentTarget;
        private CelestialBody currentCB;

        private double CurrentUT
        {
            get => Planetarium.GetUniversalTime();
        }

        private IDateTimeFormatter CurrentDTF
        {
            get => KSPUtil.dateTimeFormatter;
        }

        public RailsWarpToIMGUI()
        {
            TargetUT = Planetarium.GetUniversalTime();

            GameEvents.onPlanetariumTargetChanged.Add( onPlanetariumTargetChanged );
            GameEvents.onVesselLoaded.Add( onVesselLoaded );
            GameEvents.onVesselSOIChanged.Add( onVesselSOIChanged );
            GameEvents.onVesselSwitching.Add( onVesselSwitching );
            GameEvents.onVesselChange.Add( onVesselChange );
            GameEvents.onLevelWasLoaded.Add( onLevelWasLoaded );
        }

        ~RailsWarpToIMGUI()
        {
            GameEvents.onPlanetariumTargetChanged.Remove( onPlanetariumTargetChanged );
            GameEvents.onVesselLoaded.Remove( onVesselLoaded );
            GameEvents.onVesselSOIChanged.Remove( onVesselSOIChanged );
            GameEvents.onVesselSwitching.Remove( onVesselSwitching );
            GameEvents.onVesselChange.Remove( onVesselChange );
            GameEvents.onLevelWasLoaded.Add( onLevelWasLoaded );
        }

        private void onPlanetariumTargetChanged(MapObject m)
        {
            UpdateVessel();
        }

        private void onVesselLoaded(Vessel v)
        {
            UpdateVessel();
        }

        private void onVesselSOIChanged(GameEvents.HostedFromToAction<Vessel, CelestialBody> vcb)
        {
            UpdateVessel();
        }

        private void onVesselSwitching(Vessel from, Vessel to)
        {
            UpdateVessel();
        }

        private void onVesselChange(Vessel v)
        {
            UpdateVessel();
        }

        private void onLevelWasLoaded(GameScenes gs)
        {
            UpdateVessel();
        }

        internal void UpdateVessel()
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ActiveVessel != null)
            {
                currentV = FlightGlobals.ActiveVessel;
                currentCB = currentV.mainBody;
                currentTarget = currentV.targetObject;
            }
            else if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                PlanetariumCamera pc = PlanetariumCamera.fetch;
                if (pc.target != null && pc.target.type == MapObject.ObjectType.Vessel)
                {
                    currentV = pc.target.vessel;
                    currentCB = currentV.mainBody;
                    currentTarget = currentV.targetObject;
                }
                else if (pc.target != null && pc.target.type == MapObject.ObjectType.CelestialBody)
                {
                    currentV = null;
                    currentCB = pc.target.celestialBody;
                    currentTarget = null;
                }
                else
                {
                    currentV = null;
                    currentCB = null;
                    currentTarget = null;
                }
            }
            else
            {
                currentV = null;
                currentCB = null;
                currentTarget = null;
            }
        }

        private void GUIBreak()
        {
            GUILayout.Label( "", GUILayout.Height( 5 ) );
        }

        public void WarpToGUI()
        {
            if (!RailsWarpController.IsReady)
            {
                return;
            }

            bool priorEnabled = GUI.enabled;

            GUILayout.BeginVertical();
            {
                GUIHeader();

                GUI.enabled = priorEnabled && !RailsWarpController.Instance.IsRailsWarping;

                GUITargetUT();

                GUIAddRemove();

                GUIVesselSetUT();

                GUICustomTime();

                GUINextKAC();
            }
            GUILayout.EndVertical();

            GUI.enabled = priorEnabled;
        }

        /// <summary>
        /// Header with current UT / warping to UT, and toggle for pause on time reached
        /// </summary>
        private void GUIHeader()
        {
            GUILayout.BeginHorizontal();
            {
                RailsWarpController.Instance.RailsPauseOnUTReached = GUILayout.Toggle( RailsWarpController.Instance.RailsPauseOnUTReached, "Pause on Time Reached" );
            }
            GUILayout.EndHorizontal();

            GUIBreak();
        }

        /// <summary>
        /// Target UT Field and Buttons
        /// </summary>
        private void GUITargetUT()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Target UT:" );
                targetUTtextfield = GUILayout.TextField( targetUTtextfield, GUILayout.Width( 120 ) );
                if (double.TryParse( targetUTtextfield, out double newTargetUT ) && TargetUT != newTargetUT)
                {
                    TargetUT = newTargetUT;
                }

                if (GUILayout.Button( "Warp To", GUILayout.Width( 80 ) ))
                {
                    if (TargetUT > this.CurrentUT)
                    {
                        RailsWarpController.Instance.RailsWarpToUT( TargetUT );
                    }
                }

                if (GUILayout.Button( "Current UT", GUILayout.Width( 80 ) ))
                {
                    TargetUT = this.CurrentUT;
                }
            }
            GUILayout.EndHorizontal();
            GUIBreak();
        }


        private void CheckTargetUT()
        {

        }
        /// <summary>
        /// Add or remove X seconds, minutes, hours, days, years
        /// </summary>
        private void GUIAddRemove()
        {
            const int labelWidth = 80;
            const int buttonWidth = 40;
            const int rightMouseButton = 1;

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Seconds +/-", GUILayout.Width( labelWidth ) );

                foreach (int x in new List<int>() { 1, 5, 10, 15, 30, 45 })
                {
                    if (GUILayout.Button( x.MemoizedToString(), GUILayout.Width( buttonWidth ) ))
                    {
                        CheckTargetUT();
                        TargetUT = TargetUT + (Event.current.button == rightMouseButton ? -x : x);
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Minutes +/-", GUILayout.Width( labelWidth ) );

                foreach (int x in new List<int>() { 1, 5, 10, 15, 30, 45 })
                {
                    if (GUILayout.Button( x.MemoizedToString(), GUILayout.Width( buttonWidth ) ))
                    {
                        CheckTargetUT();
                        if (GameSettings.KERBIN_TIME)
                        {
                            TargetUT = TargetUT + ((Event.current.button == rightMouseButton ? -x : x) * CurrentDTF.Minute);
                        }
                        else
                        {
                            TargetUT = TargetUT + ((Event.current.button == rightMouseButton ? -x : x) * 60);
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Hours +/-", GUILayout.Width( labelWidth ) );

                foreach (int x in new List<int>() { 1, 3, 6, 12, 18, 24 })
                {
                    if (GUILayout.Button( x.MemoizedToString(), GUILayout.Width( buttonWidth ) ))
                    {
                        CheckTargetUT();
                        if (GameSettings.KERBIN_TIME)
                        {
                            TargetUT = TargetUT + ((Event.current.button == rightMouseButton ? -x : x) * CurrentDTF.Hour);
                        }
                        else
                        {
                            TargetUT = TargetUT + ((Event.current.button == rightMouseButton ? -x : x) * 60 * 60);
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                if (GameSettings.KERBIN_TIME)
                {
                    GUILayout.Label( "K-Days +/-", GUILayout.Width( labelWidth ) );

                    foreach (int x in new List<int>() { 1, 5, 10, 35, 106, 425 })
                    {
                        if (GUILayout.Button( x.MemoizedToString(), GUILayout.Width( buttonWidth ) ))
                        {
                            CheckTargetUT();
                            TargetUT = TargetUT + ((Event.current.button == rightMouseButton ? -x : x) * CurrentDTF.Day);
                        }
                    }
                }
                else
                {
                    GUILayout.Label( "E-Days +/-", GUILayout.Width( labelWidth ) );

                    foreach (int x in new List<int>() { 1, 5, 10, 31, 91, 365 })
                    {
                        if (GUILayout.Button( x.MemoizedToString(), GUILayout.Width( buttonWidth ) ))
                        {
                            CheckTargetUT();
                            TargetUT = TargetUT + ((Event.current.button == rightMouseButton ? -x : x) * 60 * 60 * 24);
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                if (GameSettings.KERBIN_TIME)
                {
                    GUILayout.Label( "K-Years +/-", GUILayout.Width( labelWidth ) );

                    foreach (int x in new List<int>() { 1, 5, 10, 20, 50, 100 })
                    {
                        if (GUILayout.Button( x.MemoizedToString(), GUILayout.Width( buttonWidth ) ))
                        {
                            CheckTargetUT();
                            TargetUT = TargetUT + ((Event.current.button == rightMouseButton ? -x : x) * CurrentDTF.Year);
                        }
                    }
                }
                else
                {
                    GUILayout.Label( "E-Years +/-", GUILayout.Width( labelWidth ) );

                    foreach (int x in new List<int>() { 1, 5, 10, 20, 50, 100 })
                    {
                        if (GUILayout.Button( x.MemoizedToString(), GUILayout.Width( buttonWidth ) ))
                        {
                            CheckTargetUT();
                            TargetUT = TargetUT + ((Event.current.button == rightMouseButton ? -x : x) * 60 * 60 * 24 * 365);
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUIBreak();
        }

        /// <summary>
        /// Set UT to time at various orbital locations
        /// </summary>
        private void GUIVesselSetUT()
        {
            const int labelWidth = 80;
            const int buttonWidth = 40;
            const int rightMouseButton = 1;

            bool priorEnabled = GUI.enabled;

            Vessel v = this.currentV;
            bool vesselHasOrbit = !(v?.orbit == null || v.Landed);

            GUI.enabled = priorEnabled && vesselHasOrbit && !UnstableOrbitTransitions.Contains( v.orbit.patchEndTransition );
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Orbits", GUILayout.Width( labelWidth ) );

                foreach (int x in new List<int>() { 1, 2, 3, 5, 10, 50 })
                {
                    if (GUILayout.Button( x.MemoizedToString(), GUILayout.Width( buttonWidth ) ))
                    {
                        double p = v.orbit.period * x;
                        TargetUT = CurrentUT + (Event.current.button == rightMouseButton ? -p : p);
                    }
                }
            }
            GUILayout.EndHorizontal();
            GUI.enabled = priorEnabled;

            GUIBreak();

            GUILayout.BeginHorizontal();
            {
                GUI.enabled = priorEnabled && v != null;
                GUILayout.Label( "Vessel", GUILayout.Width( labelWidth ) );
                GUI.enabled = priorEnabled;

                GUI.enabled = priorEnabled && vesselHasOrbit && (v.orbit.ApA >= 0);
                if (GUILayout.Button( "Ap", GUILayout.Width( buttonWidth ) ))
                {
                    TargetUT = CurrentUT + v.orbit.timeToAp;
                }
                GUI.enabled = priorEnabled;

                GUI.enabled = priorEnabled && vesselHasOrbit && (v.orbit.PeA >= 0);
                if (GUILayout.Button( "Pe", GUILayout.Width( buttonWidth ) ))
                {
                    TargetUT = CurrentUT + v.orbit.timeToPe;
                }
                GUI.enabled = priorEnabled;


                var tgtOrbit = v?.targetObject?.GetOrbit();

                if (tgtOrbit == null)
                {
                    GUI.enabled = priorEnabled && HighLogic.LoadedScene == GameScenes.FLIGHT && vesselHasOrbit && (v.orbit.AscendingNodeEquatorialExists());
                    if (GUILayout.Button( "AN", GUILayout.Width( buttonWidth ) ))
                    {
                        TargetUT = v.orbit.TimeOfAscendingNodeEquatorial( CurrentUT );
                    }
                    GUI.enabled = priorEnabled;


                    GUI.enabled = priorEnabled && HighLogic.LoadedScene == GameScenes.FLIGHT && vesselHasOrbit && (v.orbit.DescendingNodeEquatorialExists());
                    if (GUILayout.Button( "DN", GUILayout.Width( buttonWidth ) ))
                    {
                        TargetUT = v.orbit.TimeOfDescendingNodeEquatorial( CurrentUT );
                    }
                    GUI.enabled = priorEnabled;
                }
                else
                {
                    GUI.enabled = priorEnabled && HighLogic.LoadedScene == GameScenes.FLIGHT && vesselHasOrbit && (v.orbit.AscendingNodeExists( tgtOrbit ));
                    if (GUILayout.Button( "AN", GUILayout.Width( buttonWidth ) ))
                    {
                        TargetUT = v.orbit.TimeOfAscendingNode( tgtOrbit, CurrentUT );
                    }
                    GUI.enabled = priorEnabled;


                    GUI.enabled = priorEnabled && HighLogic.LoadedScene == GameScenes.FLIGHT && vesselHasOrbit && (v.orbit.DescendingNodeExists( tgtOrbit ));
                    if (GUILayout.Button( "DN", GUILayout.Width( buttonWidth ) ))
                    {
                        TargetUT = v.orbit.TimeOfDescendingNode( tgtOrbit, CurrentUT );
                    }
                    GUI.enabled = priorEnabled;
                }


                GUI.enabled = priorEnabled && vesselHasOrbit && (SOITransitions.Contains( v.orbit.patchEndTransition ));
                if (GUILayout.Button( "SOI", GUILayout.Width( buttonWidth ) ))
                {
                    TargetUT = v.orbit.EndUT;
                }
                GUI.enabled = priorEnabled;

                var mn = v?.FirstUpcomingManuverNode( this.CurrentUT );
                GUI.enabled = priorEnabled && vesselHasOrbit && (mn != null);
                if (GUILayout.Button( "Mnv", GUILayout.Width( buttonWidth ) ))
                {
                    TargetUT = mn.UT;
                }
                GUI.enabled = priorEnabled;
            }
            GUILayout.EndHorizontal();

            GUIBreak();
        }

        /// <summary>
        /// Set UT to a time based on y m d h s
        /// </summary>
        private void GUICustomTime()
        {
            const int rightMouseButton = 1;

            GUILayout.BeginHorizontal();
            {
                bool computeBtn = GUILayout.Button( "+/-", GUILayout.Width( 80 ) );

                warpYears = GUILayout.TextField( warpYears, GUILayout.Width( 35 ) );
                GUILayout.Label( "y " );
                warpDays = GUILayout.TextField( warpDays, GUILayout.Width( 35 ) );
                GUILayout.Label( "d " );
                warpHours = GUILayout.TextField( warpHours, GUILayout.Width( 35 ) );
                GUILayout.Label( "h " );
                warpMinutes = GUILayout.TextField( warpMinutes, GUILayout.Width( 35 ) );
                GUILayout.Label( "m " );
                warpSeconds = GUILayout.TextField( warpSeconds, GUILayout.Width( 35 ) );
                GUILayout.Label( "s" );

                if (computeBtn)
                {
                    if (int.TryParse( warpYears, out int years )
                        && int.TryParse( warpDays, out int days )
                        && int.TryParse( warpHours, out int hrs )
                        && int.TryParse( warpMinutes, out int min )
                        && int.TryParse( warpSeconds, out int sec )
                        )
                    {
                        CheckTargetUT();

                        double computedOffset = 0f;
                        if (GameSettings.KERBIN_TIME)
                        {
                            computedOffset = (((double)years * CurrentDTF.Year) + ((double)days * CurrentDTF.Day) + ((double)hrs * CurrentDTF.Hour) + ((double)min * CurrentDTF.Minute) + (double)sec);
                        }
                        else
                        {
                            computedOffset = (((double)years * 365 * 24 * 60 * 60) + ((double)days * 24 * 60 * 60) + ((double)hrs * 60 * 60) + ((double)min * 60) + (double)sec);
                        }

                        TargetUT = TargetUT + (Event.current.button == rightMouseButton ? -computedOffset : computedOffset);
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Warp to next KAC Alarm
        /// </summary>
        private void GUINextKAC()
        {
            if (!KACWrapper.InstanceExists)
            {
                return;
            }

            bool priorEnabled = GUI.enabled;

            GUI.enabled = priorEnabled && !(TimeController.Instance.ClosestKACAlarm == null);
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button( "Upcoming KAC Alarm" ))
                {
                    TargetUT = TimeController.Instance.ClosestKACAlarm.AlarmTime;
                }
            }
            GUILayout.EndHorizontal();
            GUI.enabled = priorEnabled;
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

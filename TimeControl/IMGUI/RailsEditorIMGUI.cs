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

using System;
using System.Collections;
using System.Collections.Generic;
using SC = System.ComponentModel;
using System.Reflection;
using System.Linq;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
using KSP.UI.Dialogs;
using KSPPluginFramework;
using TimeControl.Framework;

namespace TimeControl
{
    internal class RailsEditorIMGUI
    {
        private Vector2 warpScroll;
        private bool SOISelect = false;

        private List<float> warpRates = null;
        private Dictionary<CelestialBody, List<float>> altitudeLimits;

        private CelestialBody selectedGUISOI = null;
        private CelestialBody priorGUISOI = null;
        private bool warpRatesChangedByGUI = false;
        private bool altitudeLimitsChangedByGUI = false;

        private float altitudeHeight = 1000f;
        private string sAltitudeHeight = "1000";

        private EventData<bool> OnTimeControlCustomWarpRatesChangedEvent;

        public RailsEditorIMGUI()
        {
            altitudeLimits = new Dictionary<CelestialBody, List<float>>();

            SubscribeEvents();
        }

        ~RailsEditorIMGUI()
        {
            UnsubscribeEvents();
        }

        private void UnsubscribeEvents()
        {
            OnTimeControlCustomWarpRatesChangedEvent?.Remove( OnTimeControlCustomWarpRatesChanged );
        }

        private void SubscribeEvents()
        {
            OnTimeControlCustomWarpRatesChangedEvent = GameEvents.FindEvent<EventData<bool>>( nameof( TimeControlEvents.OnTimeControlCustomWarpRatesChanged ) );
            OnTimeControlCustomWarpRatesChangedEvent?.Add( OnTimeControlCustomWarpRatesChanged );
        }


        private void OnTimeControlCustomWarpRatesChanged (bool d)
        {
            const string logBlockName = nameof( RailsEditorIMGUI ) + "." + nameof( OnTimeControlCustomWarpRatesChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (!RailsWarpController.IsReady || !TimeController.IsReady)
                {
                    return;
                }

                warpRates = RailsWarpController.Instance.GetCustomWarpRates();

                if (selectedGUISOI == null)
                {
                    selectedGUISOI = TimeController.Instance.CurrentGameSOI;
                    priorGUISOI = selectedGUISOI;
                }
                
                if (!altitudeLimits.ContainsKey( selectedGUISOI ))
                {
                    altitudeLimits.Add( selectedGUISOI, null );                    
                }

                foreach (CelestialBody cb in altitudeLimits.Keys.ToList())
                {
                    altitudeLimits[cb] = RailsWarpController.Instance?.GetCustomAltitudeLimitsForBody( cb );
                }
            }
        }

        public void RailsEditorGUI()
        {
            if (!RailsWarpController.IsReady || !TimeController.IsReady)
            {
                return;
            }

            bool guiPriorEnabled = GUI.enabled;
            if (RailsWarpController.Instance.IsRailsWarping)
            {
                GUI.enabled = false;
            }

            if (selectedGUISOI == null)
            {
                selectedGUISOI = TimeController.Instance.CurrentGameSOI;
                priorGUISOI = selectedGUISOI;
            }

            if (warpRates == null)
            {
                warpRates = RailsWarpController.Instance?.GetCustomWarpRates();
            }
            
            if (!altitudeLimits.ContainsKey( selectedGUISOI ))
            {
                altitudeLimits.Add( selectedGUISOI, null );
            }

            if (altitudeLimits[selectedGUISOI] == null)
            {
                altitudeLimits[selectedGUISOI] = RailsWarpController.Instance?.GetCustomAltitudeLimitsForBody( selectedGUISOI );
            }
            
            GUI.enabled = true;
            GUILayout.BeginVertical();
            {
                GUIHeader();
                warpScroll = GUILayout.BeginScrollView( warpScroll, GUILayout.Height( 210 ) );
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUIWarpRatesList();
                        if (!SOISelect)
                            GUIAltitudeLimitsList();
                        else
                            GUISoiSelector();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();

                GUIActions();
            }
            GUILayout.EndVertical();

            GUI.enabled = guiPriorEnabled;
        }

        private void GUIHeader()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Current SOI: " + TimeController.Instance.CurrentGameSOI.name );
                GUILayout.FlexibleSpace();                
            }
            GUILayout.EndHorizontal();
            GUILayout.Label( "", GUILayout.Height( 5 ) );
            GUILayout.BeginHorizontal();
            {
                GUIWarpLevelsButtons();
                GUILayout.Label( "Altitude Limit" );
                GUILayout.FlexibleSpace();
                string s = selectedGUISOI.name;
                SOISelect = GUILayout.Toggle( SOISelect, s, "button", GUILayout.Width( 80 ) );
            }
            GUILayout.EndHorizontal();
        }

        private void GUIWarpLevelsButtons()
        {
            GUILayout.BeginHorizontal( GUILayout.Width( 175 ) );
            {
                GUILayout.Label( "Warp Rate" );
                if (GUILayout.Button( "+", GUILayout.Width( 20 ) ))
                {
                    if (RailsWarpController.Instance.NumberOfWarpLevels < 99)
                    {
                        RailsWarpController.Instance.AddWarpLevel();
                    }
                }
                if (GUILayout.Button( "-", GUILayout.Width( 20 ) ))
                {
                    if (RailsWarpController.Instance.NumberOfWarpLevels > 8)
                    {
                        RailsWarpController.Instance.RemoveWarpLevel();
                    }
                }
            }
            GUILayout.EndHorizontal();
        }


        private void GUIWarpRatesList()
        {
            int WRCount = warpRates?.Count ?? -1;

            if (WRCount <= 0)
            {
                return;
            }

            GUILayout.BeginVertical( GUILayout.Width( 20 ) );
            {
                for (int i = 0; i < WRCount; i++)
                {
                    GUILayout.Label( i + 1 + ":" );
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical( GUILayout.Width( 145 ) );
            {
                for (int i = 0; i < WRCount; i++)
                {
                    string curRate = warpRates[i].MemoizedToString();
                    if (i == 0)
                    {
                        GUI.enabled = false;
                    }
                    string newRate = GUILayout.TextField( curRate, 10 );
                    if (i == 0)
                    {
                        GUI.enabled = true;
                    }
                    if (newRate != curRate)
                    {
                        float rateConv = float.TryParse( newRate, out rateConv ) ? rateConv : -1;
                        if (rateConv != -1)
                        {
                            warpRatesChangedByGUI = true;
                            warpRates[i] = (float)rateConv;
                        }
                        curRate = newRate;
                    }
                }
            }
            GUILayout.EndVertical();
        }

        private void GUIAltitudeLimitsList()
        {
            int ALCount = altitudeLimits[selectedGUISOI]?.Count ?? -1;
            if (ALCount <= 0)
            {
                return;
            }
            
            GUILayout.BeginVertical( GUILayout.Width( 145 ) );
            {
                for (int i = 0; i < ALCount; i++)
                {
                    string curAL = altitudeLimits[selectedGUISOI][i].MemoizedToString();
                    if (i == 0)
                    {
                        GUI.enabled = false;
                    }
                    string newAL = GUILayout.TextField( curAL, 10 );
                    if (i == 0)
                    {
                        GUI.enabled = true;
                    }
                    if (newAL != curAL)
                    {
                        float alConv = float.TryParse( newAL, out alConv ) ? alConv : -1;
                        if (alConv != -1)
                        {
                            altitudeLimitsChangedByGUI = true;
                            altitudeLimits[selectedGUISOI][i] = (float)alConv;
                        }
                        curAL = newAL;
                    }
                }                
            }
            GUILayout.EndVertical();
        }

        private void GUISoiSelector()
        {
            GUILayout.BeginVertical( GUILayout.Width( 150 ) );
            {
                if (GUILayout.Button( "Current" ))
                {
                    selectedGUISOI = TimeController.Instance.CurrentGameSOI;
                    SOISelect = false;
                    warpScroll.y = 0;
                }

                for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
                {
                    CelestialBody c = FlightGlobals.Bodies[i];
                    if (GUILayout.Button( c.name ))
                    {
                        selectedGUISOI = c;
                        SOISelect = false;
                        warpScroll.y = 0;
                    }
                }
            }
            GUILayout.EndVertical();
        }

        private void GUIActions()
        {
            bool guiPriorEnabled = GUI.enabled;

            GUI.enabled = !(RailsWarpController.Instance?.IsRailsWarping ?? true);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button( "Apply Rates/Limits" ))
                {
                    if (warpRatesChangedByGUI)
                    {
                        RailsWarpController.Instance?.SetCustomWarpRates( warpRates );
                        warpRatesChangedByGUI = false;
                    }
                    if (altitudeLimitsChangedByGUI)
                    {
                        foreach (var cb in altitudeLimits.Keys)
                        {
                            RailsWarpController.Instance?.SetCustomAltitudeLimitsForBody( cb, altitudeLimits[cb] );
                        }
                        altitudeLimitsChangedByGUI = false;
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Label( "", GUILayout.Height( 5 ) );

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button( "Reset Warp Rates" ))
                {
                    RailsWarpController.Instance.ResetWarpRates();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Warp Rates: Kerbin-Multiples", GUILayout.Width(300));
                if (GUILayout.Button( "Set" ))
                {
                    RailsWarpController.Instance.SetWarpRatesToKerbinTimeMultiples();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Warp Rates: Earth-Multiples", GUILayout.Width( 300 ) );
                if (GUILayout.Button( "Set" ))
                {
                    RailsWarpController.Instance.SetWarpRatesToEarthTimeMultiples();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Label( "", GUILayout.Height( 5 ) );
            
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "All Altitudes to Atmo or ", GUILayout.Width( 210 ) );

                string curSAltitudeHeight = this.altitudeHeight.MemoizedToString();
                this.sAltitudeHeight = GUILayout.TextField( this.sAltitudeHeight, GUILayout.Width(60) );

                if (this.sAltitudeHeight != curSAltitudeHeight)
                {
                    this.altitudeHeight = float.TryParse( this.sAltitudeHeight, out float alH ) ? alH : -1;
                }                
                GUILayout.Label( "m", GUILayout.Width(30) );
                if (this.altitudeHeight < 0)
                {
                    GUI.enabled = false;
                }
                if (GUILayout.Button( "Set" ))
                {
                    RailsWarpController.Instance.SetAltitudeLimitsToAtmo( this.altitudeHeight );
                }
                GUI.enabled = guiPriorEnabled;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Reset ".MemoizedConcat( selectedGUISOI.name ).MemoizedConcat( " Altitude Limits" ), GUILayout.Width( 300 ) );
                if (GUILayout.Button( "Set" ))
                {
                    RailsWarpController.Instance.ResetAltitudeLimits( selectedGUISOI );
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Reset All Altitude Limits", GUILayout.Width( 300 ) );
                if (GUILayout.Button( "Set" ))
                {
                    RailsWarpController.Instance.ResetAltitudeLimits();
                }
            }
            GUILayout.EndHorizontal();
            
            GUI.enabled = guiPriorEnabled;
        }        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

            if (!GlobalSettings.IsReady)
            {
                Log.Error( "Global Settings not ready. Cannot create Rails Editor GUI." );
                throw new InvalidOperationException();
            }

            altitudeHeight = GlobalSettings.Instance.ResetAltitudeToValue;
            sAltitudeHeight = altitudeHeight.ToString();

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


        private void OnTimeControlCustomWarpRatesChanged(bool d)
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

            GUILayout.BeginVertical();
            {
                GUIHeader();

                GUI.enabled = guiPriorEnabled && !(RailsWarpController.Instance?.IsRailsWarping ?? true);

                GUIEditor();

                GUILayout.Label( "", GUILayout.Height( 5 ) );

                GUIActions();
            }
            GUILayout.EndVertical();

            GUI.enabled = guiPriorEnabled;
        }

        private void GUIHeader()
        {
            bool guiPriorEnabled = GUI.enabled;

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Current SOI: " + TimeController.Instance.CurrentGameSOI.name );
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label( "", GUILayout.Height( 5 ) );

            GUI.enabled = guiPriorEnabled;
        }

        private void GUIActions()
        {
            bool guiPriorEnabled = GUI.enabled;

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button( "Reset Warp Rates to Defaults" ))
                {
                    RailsWarpController.Instance.ResetWarpRates();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button( "Warp Rates: Kerbin-Multiples" ))
                {
                    RailsWarpController.Instance.SetWarpRatesToKerbinTimeMultiples();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button( "Warp Rates: Earth-Multiples" ))
                {
                    RailsWarpController.Instance.SetWarpRatesToEarthTimeMultiples();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button( "Reset ".MemoizedConcat( selectedGUISOI.name ).MemoizedConcat( " Altitude Limits" ) ))
                {
                    RailsWarpController.Instance.ResetAltitudeLimits( selectedGUISOI );
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button( "Reset All Altitude Limits" ))
                {
                    RailsWarpController.Instance.ResetAltitudeLimits();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Set ".MemoizedConcat( selectedGUISOI.name ).MemoizedConcat( " Altitudes to Atmo or " ), GUILayout.Width( 200 ) );
                string curSAltitudeHeight = this.altitudeHeight.MemoizedToString();
                this.sAltitudeHeight = GUILayout.TextField( this.sAltitudeHeight, GUILayout.Width( 60 ) );
                if (this.sAltitudeHeight != curSAltitudeHeight)
                {
                    this.altitudeHeight = float.TryParse( this.sAltitudeHeight, out float alH ) ? alH : -1;
                    if (this.altitudeHeight >= 0)
                    {
                        GlobalSettings.Instance.ResetAltitudeToValue = altitudeHeight;
                    }
                }
                GUILayout.Label( "m" );

                GUI.enabled = guiPriorEnabled && this.altitudeHeight >= 0;
                if (GUILayout.Button( "SET", GUILayout.Width( 40 ) ))
                {
                    RailsWarpController.Instance.SetAltitudeLimitsToAtmoForBody( selectedGUISOI, this.altitudeHeight );
                }
                GUI.enabled = guiPriorEnabled;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Set All Altitudes to Atmo or ", GUILayout.Width( 200 ) );
                string curSAltitudeHeight = this.altitudeHeight.MemoizedToString();
                this.sAltitudeHeight = GUILayout.TextField( this.sAltitudeHeight, GUILayout.Width( 60 ) );
                if (this.sAltitudeHeight != curSAltitudeHeight)
                {
                    this.altitudeHeight = float.TryParse( this.sAltitudeHeight, out float alH ) ? alH : -1;
                    if (this.altitudeHeight >= 0)
                    {
                        GlobalSettings.Instance.ResetAltitudeToValue = altitudeHeight;
                    }
                }
                GUILayout.Label( "m" );

                GUI.enabled = guiPriorEnabled && this.altitudeHeight >= 0;
                if (GUILayout.Button( "SET", GUILayout.Width( 40 ) ))
                {
                    RailsWarpController.Instance.SetAltitudeLimitsToAtmo( this.altitudeHeight );
                }
                GUI.enabled = guiPriorEnabled;
            }
            GUILayout.EndHorizontal();


            GUI.enabled = guiPriorEnabled;
        }


        private void GUIEditor()
        {
            bool guiPriorEnabled = GUI.enabled;

            GUILayout.BeginHorizontal();
            {
                GUI.enabled = guiPriorEnabled && (warpRatesChangedByGUI || altitudeLimitsChangedByGUI);
                if (GUILayout.Button( "Apply Changes" ))
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
                GUI.enabled = guiPriorEnabled;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUIWarpLevelsButtons();
                GUILayout.Label( "Altitude Limit" );
                GUILayout.FlexibleSpace();
                string s = selectedGUISOI.name;
                SOISelect = GUILayout.Toggle( SOISelect, s, "button", GUILayout.Width( 80 ) );
            }
            GUILayout.EndHorizontal();

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

            GUI.enabled = guiPriorEnabled;
        }

        private void GUIWarpLevelsButtons()
        {
            bool guiPriorEnabled = GUI.enabled;

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

            GUI.enabled = guiPriorEnabled;
        }

        private void GUIWarpRatesList()
        {
            bool guiPriorEnabled = GUI.enabled;

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
                    GUI.enabled = guiPriorEnabled && (i != 0);
                    string newRate = GUILayout.TextField( curRate, 10 );
                    GUI.enabled = guiPriorEnabled;
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

            GUI.enabled = guiPriorEnabled;
        }

        private void GUIAltitudeLimitsList()
        {
            bool guiPriorEnabled = GUI.enabled;

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
                    GUI.enabled = guiPriorEnabled && (i != 0);
                    string newAL = GUILayout.TextField( curAL, 10 );
                    GUI.enabled = guiPriorEnabled;
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

            GUI.enabled = guiPriorEnabled;
        }

        private void GUISoiSelector()
        {
            bool guiPriorEnabled = GUI.enabled;

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

            GUI.enabled = guiPriorEnabled;
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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TimeControl
{
    internal class HyperEditorIMGUI
    {
        private Vector2 warpScroll;

        private List<float> hyperWarpRates = null;
        private List<float> hyperWarpPhysicsAccuracyRates = null;

        private bool hyperWarpRatesChangedByGUI = false;      

        private EventData<bool> OnTimeControlCustomHyperWarpRatesChangedEvent;

        public HyperEditorIMGUI()
        {
            if (!GlobalSettings.IsReady)
            {
                Log.Error( "Global Settings not ready. Cannot create Hyper Editor GUI." );
                throw new InvalidOperationException();
            }

            SubscribeEvents();
        }

        ~HyperEditorIMGUI()
        {
            UnsubscribeEvents();
        }

        private void UnsubscribeEvents()
        {
            OnTimeControlCustomHyperWarpRatesChangedEvent?.Remove( OnTimeControlCustomHyperWarpRatesChanged );
        }

        private void SubscribeEvents()
        {
            OnTimeControlCustomHyperWarpRatesChangedEvent = GameEvents.FindEvent<EventData<bool>>( nameof( TimeControlEvents.OnTimeControlCustomHyperWarpRatesChanged ) );
            OnTimeControlCustomHyperWarpRatesChangedEvent?.Add( OnTimeControlCustomHyperWarpRatesChanged );
        }

        private void OnTimeControlCustomHyperWarpRatesChanged(bool d)
        {
            const string logBlockName = nameof( HyperEditorIMGUI ) + "." + nameof( OnTimeControlCustomHyperWarpRatesChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (!RailsWarpController.IsReady || !TimeController.IsReady)
                {
                    return;
                }

                hyperWarpRates = HyperWarpController.Instance.GetCustomHyperWarpRates();
                hyperWarpPhysicsAccuracyRates = HyperWarpController.Instance.GetCustomHyperWarpPhysicsAccuracyRates();
            }
        }

        public void HyperEditorGUI()
        {
            if (!RailsWarpController.IsReady || !TimeController.IsReady)
            {
                return;
            }

            bool guiPriorEnabled = GUI.enabled;

            if (hyperWarpRates == null)
            {
                hyperWarpRates = HyperWarpController.Instance?.GetCustomHyperWarpRates();                
            }

            if (hyperWarpPhysicsAccuracyRates == null)
            {
                hyperWarpPhysicsAccuracyRates = HyperWarpController.Instance?.GetCustomHyperWarpPhysicsAccuracyRates();
            }

            GUILayout.BeginVertical();
            {
                GUIHeader();

                GUI.enabled = guiPriorEnabled && !(HyperWarpController.Instance?.IsHyperWarping ?? true);

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
                GUILayout.Label( "Hyper Warp Rates & Accuracy" );
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
                if (GUILayout.Button( "Reset Hyper Rates to Defaults" ))
                {
                    HyperWarpController.Instance.ResetHyperWarpRates();
                    hyperWarpRates = HyperWarpController.Instance.GetCustomHyperWarpRates();
                    hyperWarpPhysicsAccuracyRates = HyperWarpController.Instance.GetCustomHyperWarpPhysicsAccuracyRates();
                }
            }
            GUILayout.EndHorizontal();

            GUI.enabled = guiPriorEnabled;
        }


        private void GUIEditor()
        {
            bool guiPriorEnabled = GUI.enabled;

            GUILayout.BeginHorizontal();
            {
                GUI.enabled = guiPriorEnabled && (hyperWarpRatesChangedByGUI);
                if (GUILayout.Button( "Apply Changes" ))
                {
                    if (hyperWarpRatesChangedByGUI)
                    {
                        // Sort by rates and tag the phsyics settings along
                        List<Tuple<float, float>> f = new List<Tuple<float, float>>();

                        int i = 0;
                        hyperWarpRates.ForEach( r =>
                        {
                            f.Add( new Tuple<float, float> (r, hyperWarpPhysicsAccuracyRates[i]) );
                            ++i;
                        } );

                        f.Sort( (a, b) => a.Item1.CompareTo( b.Item1 ) );

                        hyperWarpRates = new List<float>();
                        hyperWarpPhysicsAccuracyRates = new List<float>();

                        f.ForEach( x =>
                        {
                            hyperWarpRates.Add( x.Item1 );
                            hyperWarpPhysicsAccuracyRates.Add( x.Item2 );
                        } );

                        f.Clear();
                        f = null;

                        HyperWarpController.Instance?.SetCustomHyperWarpRates( hyperWarpRates, hyperWarpPhysicsAccuracyRates );
                        hyperWarpRatesChangedByGUI = false;
                    }
                }
                GUI.enabled = guiPriorEnabled;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUIWarpLevelsButtons();
            }
            GUILayout.EndHorizontal();

            warpScroll = GUILayout.BeginScrollView( warpScroll, GUILayout.Height( 260 ) );
            {
                GUILayout.BeginHorizontal();
                {
                    GUIWarpRatesList();
                    GUIPhysicsAccuracyList();
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
                GUILayout.Label( "Add/Remove Rate" );
                if (GUILayout.Button( "+", GUILayout.Width( 20 ) ))
                {
                    if (hyperWarpRates.Count < 99)
                    {
                        hyperWarpRates.Add( hyperWarpRates.Max() + 1.0f );
                        hyperWarpPhysicsAccuracyRates.Add( hyperWarpPhysicsAccuracyRates.Max() );
                    }
                }
                if (GUILayout.Button( "-", GUILayout.Width( 20 ) ))
                {
                    if (hyperWarpRates.Count > 2)
                    {
                        hyperWarpRates.RemoveAt( hyperWarpRates.Count - 1 );
                        hyperWarpPhysicsAccuracyRates.RemoveAt( hyperWarpPhysicsAccuracyRates.Count - 1 );
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUI.enabled = guiPriorEnabled;
        }

        private void GUIWarpRatesList()
        {
            bool guiPriorEnabled = GUI.enabled;

            int WRCount = hyperWarpRates?.Count ?? -1;

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

            GUILayout.BeginVertical( GUILayout.Width( 115 ) );
            {
                for (int i = 0; i < WRCount; i++)
                {
                    string curRate = hyperWarpRates[i].MemoizedToString();
                    GUI.enabled = guiPriorEnabled && (i != 0);
                    string newRate = GUILayout.TextField( curRate, 10 );
                    GUI.enabled = guiPriorEnabled;
                    if (newRate != curRate)
                    {
                        float rateConv = float.TryParse( newRate, out rateConv ) ? rateConv : -1;
                        if (rateConv != -1)
                        {
                            hyperWarpRatesChangedByGUI = true;
                            hyperWarpRates[i] = (float)rateConv;
                        }
                        curRate = newRate;
                    }
                }
            }
            GUILayout.EndVertical();

            GUI.enabled = guiPriorEnabled;
        }

        private void GUIPhysicsAccuracyList()
        {
            bool guiPriorEnabled = GUI.enabled;

            int WRCount = hyperWarpPhysicsAccuracyRates?.Count ?? -1;

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

            GUILayout.BeginVertical( GUILayout.Width( 115 ) );
            {
                for (int i = 0; i < WRCount; i++)
                {
                    string curRate = hyperWarpPhysicsAccuracyRates[i].MemoizedToString();
                    GUI.enabled = guiPriorEnabled && (i != 0);
                    string newRate = GUILayout.TextField( curRate, 10 );
                    GUI.enabled = guiPriorEnabled;
                    if (newRate != curRate)
                    {
                        float rateConv = float.TryParse( newRate, out rateConv ) ? rateConv : -1;
                        if (rateConv != -1)
                        {
                            hyperWarpRatesChangedByGUI = true;
                            hyperWarpPhysicsAccuracyRates[i] = (float)rateConv;
                        }
                        curRate = newRate;
                    }
                }
            }
            GUILayout.EndVertical();

            GUI.enabled = guiPriorEnabled;
        }

    }
}
/*
All code in this file Copyright(c) 2020 Nate West

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

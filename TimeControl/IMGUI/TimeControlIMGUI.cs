/*
All code in this file Copyright(c) 2016 Nate West
Rewritten from scratch, but based on code Copyright(c) 2014 Xaiier using the same license as below

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
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    internal class TimeControlIMGUI : MonoBehaviour
    {

        #region Singleton
        private static TimeControlIMGUI instance;
        internal static TimeControlIMGUI Instance { get { return instance; } }
        public static bool IsReady { get; private set; } = false;
        #endregion Singleton

        #region Public Properties
        public bool WindowVisible { get; set; } = false;
        public bool SettingsWindowOpen { get; set; } = false;
        public bool GUITempHidden { get => tempGUIHidden.Count != 0; }
        #endregion Public Properties

        #region Public Methods
        public void ToggleGUIVisibility()
        {
            WindowVisible = !WindowVisible;
        }

        public void SetGUIVisibility(bool v)
        {
            WindowVisible = v;
        }

        public void TempHideGUI(string lockedBy)
        {
            tempGUIHidden.Add(lockedBy);
        }

        public void TempUnHideGUI(string lockedBy)
        {
            tempGUIHidden.RemoveAll(x => x == lockedBy);
        }

        public void TempUnHideGUI()
        {
            tempGUIHidden.Clear();
        }
        #endregion

        private enum GUIMode
        {
            RailsEditor = 1,            
            HyperWarp = 2,
            SlowMotion = 3,
            RailsWarpTo = 4,
            Details = 5,
            KeyBindingsEditor = 6,
            QuickWarp = 7
        }
        
        #region Fields
        // Temp Hide/Show GUI Windows
        private List<string> tempGUIHidden = new List<string>();
        
        //private bool windowsVisible = false;
        //private int windowSelectedFlightMode = 0;
        
        //private bool useCustomDateTimeFormatter = false;
        
        // Date Time Formatter
        //private TCDateTimeFormatter customDTFormatter = new TCDateTimeFormatter();
        //private IDateTimeFormatter defaultDTFormatter;
        
        //GUI Layout        
        private static Rect mode0Button = new Rect(10, -1, 25, 20);
        private static Rect mode1Button = new Rect(25, -1, 25, 20);
        private static Rect mode2Button = new Rect(40, -1, 25, 20);
        private static Rect mode3Button = new Rect( 55, -1, 25, 20 );
        private static Rect mode4Button = new Rect( 70, -1, 25, 20 );
        private static Rect mode5Button = new Rect( 85, -1, 25, 20 );

        private Rect windowRect = new Rect(100, 100, 375, 0);

        private int tcsWindowHashCode = "Time Control IMGUI".GetHashCode();
        
        private RailsEditorIMGUI railsGUI;
        private HyperIMGUI hyperGUI;
        private DetailsIMGUI detailsGUI;
        private SlowMoIMGUI slomoGUI;
        private RailsWarpToIMGUI railsWarpToGUI;
        private KeyBindingsEditorIMGUI keyBindingsGUI;
        
        #endregion

        //private GUIMode priorWindowSelectedMode = GUIMode.RailsWarpTo;
        private GUIMode windowSelectedMode = GUIMode.RailsWarpTo;
        private GUIMode WindowSelectedMode
        {
            get => windowSelectedMode;
            set
            {
                if (windowSelectedMode != value)
                {
                    windowSelectedMode = value;
                }
            }
        }

        #region Private Methods

        private void SetDateTimeFormatter()
        {
            /*
            if (UseCustomDateTimeFormatter)
            {
                Log.Info( "Changing Date Time Formatter to customDTFormatter" );
                KSPUtil.dateTimeFormatter = customDTFormatter;
                // Only run this test in trace mode
                if (Log.LoggingLevel == LogSeverity.Trace)
                    TestDateTimeDisplay.RunDateTimeDisplayTest(HighLogic.CurrentGame.UniversalTime);
            }
            else
            {
                Log.Info( "Changing Date Time Formatter to defaultDTFormatter" );
                KSPUtil.dateTimeFormatter = defaultDTFormatter;
                // Only run this test in trace mode
                if (Log.LoggingLevel == LogSeverity.Trace)
                    TestDateTimeDisplay.RunDateTimeDisplayTest(HighLogic.CurrentGame.UniversalTime);
            }
            */
        }

        private bool SupressFlightResultsDialog
        {
            get => HighLogic.CurrentGame?.Parameters?.CustomParams<TimeControlParameterNode>()?.SupressFlightResultsDialog ?? true;
        }
        
        private double PhysicsTimeRatio
        {
            get => (PerformanceManager.IsReady ? PerformanceManager.Instance?.PhysicsTimeRatio ?? 0.0 : 0.0);
        }

        private double FramesPerSecond
        {
            get => (PerformanceManager.IsReady ? PerformanceManager.Instance?.FramesPerSecond ?? 0.0 : 0.0);
        }

        public bool KACAPIIntegrated { get; set; } = false;
        public bool TriedToLoadKAC { get; set; } = false;

        #endregion
        #region MonoBehavior and related private methods
        #region One-Time
        private void Awake()
        {
            const string logBlockName = nameof( TimeControlIMGUI ) + "." + nameof( Awake );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                UnityEngine.Object.DontDestroyOnLoad( this ); //Don't go away on scene changes
                instance = this;
            }
        }

        private void Start()
        {
            const string logBlockName = nameof( TimeControlIMGUI ) + "." + nameof( Start );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                // Hide / Show UI on these events
                global::GameEvents.onGameSceneLoadRequested.Add( this.onGameSceneLoadRequested );
                global::GameEvents.onLevelWasLoaded.Add( this.onLevelWasLoaded );
                global::GameEvents.onHideUI.Add( this.onHideUI );
                global::GameEvents.onShowUI.Add( this.onShowUI );
                //global::GameEvents.OnGameSettingsApplied.Add( this.OnGameSettingsApplied );

                //defaultDTFormatter = KSPUtil.dateTimeFormatter;

                StartCoroutine( StartAfterSettingsAndControllerAreReady() );
            }
        }

        /// <summary>
        /// Configures the GUI once the Settings are loaded and the TimeController is ready to operate
        /// </summary>
        private IEnumerator StartAfterSettingsAndControllerAreReady()
        {
            const string logBlockName = nameof( TimeControlIMGUI ) + "." + nameof( StartAfterSettingsAndControllerAreReady );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                /*
                Log.Info( "Setting Up GUI Window Postions", logBlockName );
                SetFlightWindowPosition( Settings.Instance.FlightWindowX, Settings.Instance.FlightWindowY );
                SetSpaceCenterWindowPosition( Settings.Instance.SpaceCenterWindowX, Settings.Instance.SpaceCenterWindowY );
                SetSettingsWindowPosition( Settings.Instance.SettingsWindowX, Settings.Instance.SettingsWindowY );            
                */


                OnGUIUpdateConfigWithWindowLocations();
                
                // Wait for TimeController object to be ready
                while (TimeController.Instance == null || !TimeController.IsReady || !RailsWarpController.IsReady || !SlowMoController.IsReady || !HyperWarpController.IsReady)
                {
                    yield return null;
                }
                    

                railsWarpToGUI = new RailsWarpToIMGUI();
                railsGUI = new RailsEditorIMGUI();
                slomoGUI = new SlowMoIMGUI();
                hyperGUI = new HyperIMGUI();
                detailsGUI = new DetailsIMGUI();
                keyBindingsGUI = new KeyBindingsEditorIMGUI();

                Log.Info( "TCGUI.Instance is Ready!", logBlockName );
                IsReady = true;

            }
            yield break;
        }
        #endregion

        #region Event Handlers
        private void onGameSceneLoadRequested(GameScenes gs)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( onGameSceneLoadRequested );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                onHideUI();
            }
        }

        private void onLevelWasLoaded(GameScenes gs)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( onLevelWasLoaded );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                onShowUI();
            }
        }

        private void onHideUI()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( onHideUI );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Hiding GUI for Settings Lock", logBlockName );
                TempHideGUI( "GameEventsUI" );
            }
        }

        private void onShowUI()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( onShowUI );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Unhiding GUI for Settings Lock", logBlockName );
                TempUnHideGUI( "GameEventsUI" );
            }
        }
        #endregion

        #region Update Methods
        private void Update()
        {
            if (!IsReady || GUITempHidden || !TimeController.IsReady || !WindowVisible)
            {
                return;
            }

            if (!TriedToLoadKAC)
            {
                SetupKACAlarms();
            }
        }

        internal KACWrapper.KACAPI.KACAlarm ClosestKACAlarm { get; private set; }

        private void SetupKACAlarms()
        {
            const string logBlockName = nameof( TimeControlIMGUI ) + "." + nameof( SetupKACAlarms );

            TriedToLoadKAC = true;
            KACAPIIntegrated = KACWrapper.InitKACWrapper();
            if (KACAPIIntegrated)
            {
                StartCoroutine( CheckKACAlarms() );
                Log.Info( "KAC Integrated With TimeControl", logBlockName );
            }
            else
            {
                Log.Info( "KAC Not Integrated With TimeControl", logBlockName );
            }
        }

        private IEnumerator CheckKACAlarms()
        {
            const string logBlockName = nameof( TimeControlIMGUI ) + "." + nameof( CheckKACAlarms );

            while (true)
            {
                if (KACAPIIntegrated && (WindowSelectedMode == GUIMode.RailsWarpTo || WindowSelectedMode == GUIMode.QuickWarp))
                {
                    var list = KACWrapper.KAC.Alarms.Where( f => f.AlarmTime > Planetarium.GetUniversalTime() && f.AlarmType != KACWrapper.KACAPI.AlarmTypeEnum.EarthTime ).OrderBy( f => f.AlarmTime );
                    if (list != null && list.Count() != 0)
                    {
                        var upNextAlarm = list.First();
                        if (ClosestKACAlarm == null || ClosestKACAlarm.ID != upNextAlarm.ID)
                        {
                            Log.Info( "Updating Next KAC Alarm", logBlockName );
                            ClosestKACAlarm = upNextAlarm;
                        }
                    }
                    else if (ClosestKACAlarm != null)
                    {
                        Log.Info( "Clearing Next KAC Alarm", logBlockName );
                        ClosestKACAlarm = null;
                    }
                }

                yield return new WaitForSeconds( 1f );
            }
        }
        #endregion

        #region GUI Methods
        private void OnGUI()
        {
            // Don't do anything until the settings are loaded or we can actally warp
            if (!IsReady || GUITempHidden || !TimeController.IsReady)
            {
                return;
            }

            // Don't show GUI unless we are in the appropriate scene
            if (HighLogic.LoadedScene != GameScenes.FLIGHT && HighLogic.LoadedScene != GameScenes.SPACECENTER && HighLogic.LoadedScene != GameScenes.TRACKSTATION)
            {
                return;
            }

            UnityEngine.GUI.skin = null;
            if (WindowVisible)
            {
                if (PerformanceManager.IsReady)
                {
                    PerformanceManager.Instance.PerformanceCountersOn = true;
                }
                OnGUIWindow();
            }
            else
            {
                if (PerformanceManager.IsReady)
                {
                    PerformanceManager.Instance.PerformanceCountersOn = false;
                }
            }
            UnityEngine.GUI.skin = HighLogic.Skin;
            OnGUIUpdateConfigWithWindowLocations(); // Only trigger a config save if the values change
        }

        private void OnGUIWindow()
        {
            windowRect = GUILayout.Window(tcsWindowHashCode, windowRect, MainGUI, "Time Control");
        }

        private void OnGUIUpdateConfigWithWindowLocations()
        {            
            //if (!Settings.IsReady)
            //    return;

            //Settings.Instance.FlightWindowX = (int)flightWindowRect.x;
            //Settings.Instance.FlightWindowY = (int)flightWindowRect.y;
            //Settings.Instance.SpaceCenterWindowX = (int)spaceCenterWindowRect.x;
            //Settings.Instance.SpaceCenterWindowY = (int)spaceCenterWindowRect.y;
        }

        #region Main GUI
        private void MainGUI(int windowId)
        {
            UnityEngine.GUI.enabled = true;

            GUIHeaderButtons();
            GUIHeaderCurrentWarpState();


            if ((HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER) && (WindowSelectedMode == GUIMode.SlowMotion || WindowSelectedMode == GUIMode.HyperWarp))
            {
                WindowSelectedMode = GUIMode.RailsWarpTo;
            }

            //if (priorWindowSelectedMode != windowSelectedMode)
            //{
            //    switch (WindowSelectedMode)
            //    {
            //        case GUIMode.SlowMotion:
            //            slomoGUI = new SlowMoIMGUI();
            //            break;
            //        case GUIMode.HyperWarp:
            //            hyperGUI = new HyperIMGUI();
            //            break;
            //        case GUIMode.RailsEditor:
            //            railsGUI = new RailsEditorIMGUI();
            //            break;
            //        case GUIMode.RailsWarpTo:
            //            railsWarpToGUI = new RailsWarpToIMGUI();
            //            break;
            //        case GUIMode.Details:
            //            detailsGUI = new DetailsIMGUI();
            //            break;
            //        case GUIMode.KeyBindingsEditor:
            //            keyBindingsGUI = new KeyBindingsEditorIMGUI();
            //            break;
            //    }
            //}        
            //priorWindowSelectedMode = WindowSelectedMode;

            switch (WindowSelectedMode)
            {
                case GUIMode.SlowMotion:
                    slomoGUI.SlowMoGUI();
                    break;
                case GUIMode.HyperWarp:
                    hyperGUI.HyperGUI();
                    break;
                case GUIMode.RailsEditor:
                    railsGUI.RailsEditorGUI();
                    break;
                case GUIMode.RailsWarpTo:
                    railsWarpToGUI.WarpToGUI();
                    break;
                case GUIMode.Details:
                    detailsGUI.DetailsGUI();
                    break;
                case GUIMode.KeyBindingsEditor:
                    keyBindingsGUI.KeyBindingsEditorGUI();
                    break;
            }
            
            UnityEngine.GUI.enabled = true;

            if (Event.current.button > 0 && Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) //Ignore right & middle clicks
                Event.current.Use();

            UnityEngine.GUI.DragWindow();
        }

        private void GUIHeaderCurrentWarpState()
        {
            GUILayout.BeginHorizontal();
            {
                if (RailsWarpController.Instance.IsRailsWarpingNoPhys)
                {
                    string rate = RailsWarpController.Instance.CurrentWarpRate.MemoizedToString().MemoizedConcat( "x" );
                    GUILayout.Label( "Rails: ".MemoizedConcat( rate ) );
                }
                else if (RailsWarpController.Instance.IsRailsWarpingPhys)
                {
                    string rate = RailsWarpController.Instance.CurrentWarpRate.MemoizedToString().MemoizedConcat( "x" );
                    GUILayout.Label( "KSP-Phys: ".MemoizedConcat( rate ) );
                }
                else
                {
                    if (PerformanceManager.Instance?.PerformanceCountersOn ?? false)
                    {
                        string rate = ((PhysicsTimeRatio / 1 * 100).MemoizedToString( "0" )).MemoizedConcat( "%" );
                        GUILayout.Label( "PTR: ".MemoizedConcat( rate ) );
                    }
                    else
                    {
                        GUILayout.Label( "PTR: N/A" );
                    }
                }

                if (PerformanceManager.Instance?.PerformanceCountersOn ?? false)
                {
                    GUILayout.Label( "FPS: ".MemoizedConcat( (Mathf.Floor( Convert.ToSingle( FramesPerSecond ) )).MemoizedToString() ) );
                }
                else
                {
                    GUILayout.Label( "FPS: N/A" );
                }

                GUILayout.FlexibleSpace();

                GUIPauseOrResumeButton();
                GUITimeStepButton();
                GUIReturnToRealtimeButton();
            }
            GUILayout.EndHorizontal();
        }

        private void GUIHeaderButtons()
        {
            Color bc = UnityEngine.GUI.backgroundColor;
            Color cc = UnityEngine.GUI.contentColor;

            UnityEngine.GUI.backgroundColor = Color.clear;
            
            //Details mode
            {
                if (WindowSelectedMode != GUIMode.Details)
                {
                    UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                }
                if (UnityEngine.GUI.Button( mode0Button, "?" ))
                {
                    WindowSelectedMode = GUIMode.Details;
                }
                UnityEngine.GUI.contentColor = cc;
            }
            //Rails Warp-To mode
            {
                if (WindowSelectedMode != GUIMode.RailsWarpTo)
                {
                    UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                }
                if (UnityEngine.GUI.Button( mode1Button, "W" ))
                {
                    WindowSelectedMode = GUIMode.RailsWarpTo;
                    windowRect.height = 0;
                }
                UnityEngine.GUI.contentColor = cc;
            }

            //Rails Editor mode
            {
                if (WindowSelectedMode != GUIMode.RailsEditor)
                {
                    UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                }
                if (UnityEngine.GUI.Button( mode2Button, "R" ))
                {
                    WindowSelectedMode = GUIMode.RailsEditor;
                    windowRect.height = 0;
                }
                UnityEngine.GUI.contentColor = cc;
            }

            //Key Bindings Editor mode
            {
                if (WindowSelectedMode != GUIMode.KeyBindingsEditor)
                {
                    UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                }
                if (UnityEngine.GUI.Button( mode3Button, "K" ))
                {
                    WindowSelectedMode = GUIMode.KeyBindingsEditor;
                    windowRect.height = 0;
                }
                UnityEngine.GUI.contentColor = cc;
            }

            // Only allow hyper warp and slow motion when in flight
            if (HighLogic.LoadedSceneIsFlight)
            {
                //Slow-mo mode
                {
                    if (WindowSelectedMode != GUIMode.SlowMotion)
                    {
                        UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                    }
                    if (UnityEngine.GUI.Button( mode4Button, "S" ))
                    {
                        WindowSelectedMode = GUIMode.SlowMotion;
                        windowRect.height = 0;
                    }
                    UnityEngine.GUI.contentColor = cc;
                }

                //Hyper mode
                {
                    if (WindowSelectedMode != GUIMode.HyperWarp)
                    {
                        UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                    }
                    if (UnityEngine.GUI.Button( mode5Button, "H" ))
                    {
                        WindowSelectedMode = GUIMode.HyperWarp;
                        windowRect.height = 0;
                    }
                    UnityEngine.GUI.contentColor = cc;
                }
            }            
            UnityEngine.GUI.backgroundColor = bc;
            
            GUI.enabled = true;
        }

        private void GUIReturnToRealtimeButton()
        {
            bool returnButton = false;
            if (FlightDriver.Pause)
            {
                GUILayout.Label( "PAUSED-KSP" );
            }
            else if (TimeController.Instance.TimePaused)
            {
                GUILayout.Label( "PAUSED-TC" );
            }
            else if (HyperWarpController.Instance.IsHyperWarping)
            {
                returnButton = GUILayout.Button( "HYPER" );
            }
            else if (RailsWarpController.Instance.IsRailsWarpingNoPhys)
            {
                returnButton = GUILayout.Button( "RAILS" );
            }
            else if (RailsWarpController.Instance.IsRailsWarpingPhys)
            {
                returnButton = GUILayout.Button( "PHYS" );
            }
            else if (SlowMoController.Instance.IsSlowMo)
            {
                returnButton = GUILayout.Button( "SLOWMO" );
            }
            else
            {
                GUILayout.Label( "NORMAL" );
            }
            if (returnButton)
            {
                TimeController.Instance.GoRealTime();
            }
        }

        private void GUIPauseOrResumeButton()
        {
            if (GUILayout.Button( (TimeController.Instance.TimePaused ? "Resume" : "Pause"), GUILayout.Width( 60 ) ))
            {
                TimeController.Instance?.TogglePause();
            }
        }

        private void GUITimeStepButton()
        {
            if (GUILayout.Button( ">", GUILayout.Width( 20 ) ))
            {
                TimeController.Instance?.IncrementTimeStep();
            }
        }


        #endregion

        #endregion

        #endregion
    }
}

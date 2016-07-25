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

namespace TimeControl
{
    [KSPAddon( KSPAddon.Startup.MainMenu, true )]
    internal class TCGUI : MonoBehaviour
    {
        #region Singleton
        private static TCGUI instance;
        internal static TCGUI Instance { get { return instance; } }
        #endregion
        #region GUI State
        public bool IsReady { get; private set; } = false;
        #endregion
        #region Fields
        // Control the throttle with this GUI
        private bool throttleToggle;
        private float throttleSet;
        // Option to suppress the Flight Results Dialog
        private FlightResultsDialog fld;
        private bool supressFlightResultsDialog = false;
        // GUI toggles and values
        private bool settingsOpen = false;
        private bool SOISelect = false;
        bool currentlyAssigningKey = false;
        private bool fpsVisible = true;
        private int currentFlightMode;
        private CelestialBody selectedSOI;
        private string warpYears = "0"; private string warpDays = "0"; private string warpHours = "0"; private string warpMinutes = "0"; private string warpSeconds = "0";
        // On-Screen Messages
        private ScreenMessages screenMsgs;
        private Color screenMsgsColor;
        private ScreenMessage warpScreenMessage;
        //GUI Layout
        private static Rect minimizeButton = new Rect( 5, 5, 10, 10 );
        private static Rect closeButton = new Rect( 5, 5, 10, 10 );
        private static Rect settingsButton = new Rect( 360, -1, 25, 20 );
        private static Rect mode0Button = new Rect( 10, -1, 25, 20 );
        private static Rect mode1Button = new Rect( 25, -1, 25, 20 );
        private static Rect mode2Button = new Rect( 40, -1, 25, 20 );
        private Rect flightWindowRect = new Rect( 100, 100, 375, 0 );
        private Rect spaceCenterWindowRect = new Rect( 100, 100, 375, 0 );
        private Rect settingsWindowRect = new Rect( 100, 100, 220, 0 );
        private int tcsMainWindowHashCode = "Time Control Main".GetHashCode();
        private int tcsFlightWindowHashCode = "Time Control Flight".GetHashCode();
        private int tcsSettingsWindowHashCode = "Time Control Settings".GetHashCode();
        private Vector2 warpScroll;
        #endregion
        #region Private Methods
        private void SetFlightWindowPosition(float x, float y)
        {
            flightWindowRect.x = x;
            flightWindowRect.y = y;
            flightWindowRect = flightWindowRect.ClampToScreen();
        }
        private void SetMainWindowPosition(float x, float y)
        {
            spaceCenterWindowRect.x = x;
            spaceCenterWindowRect.y = y;
            spaceCenterWindowRect = spaceCenterWindowRect.ClampToScreen();
        }
        private void SetSettingsWindowPosition(float x, float y)
        {
            settingsWindowRect.x = x;
            settingsWindowRect.y = y;
            settingsWindowRect = settingsWindowRect.ClampToScreen();
        }
        private void UpdateFlightWindowRectSize(bool force = false)
        {
            if (currentFlightMode == Settings.Instance.WindowSelectedFlightMode && !force)
                return;

            currentFlightMode = Settings.Instance.WindowSelectedFlightMode;
            flightWindowRect.height = 0;
            switch (Settings.Instance.WindowSelectedFlightMode)
            {
                case 0:
                    flightWindowRect.width = 220;
                    break;
                case 1:
                    flightWindowRect.width = 250;
                    break;
                case 2:
                    flightWindowRect.width = 375;
                    break;
            }
        }
        #endregion
        #region MonoBehavior and related private methods
        #region One-Time
        private void Awake()
        {
            Log.Write( "method start", "TCWindow.Awake", LogSeverity.Trace );

            UnityEngine.Object.DontDestroyOnLoad( this ); //Don't go away on scene changes
            instance = this;

            Log.Write( "method end", "TCWindow.Awake", LogSeverity.Trace );
        }
        private void Start()
        {
            Log.Write( "method start", "TCWindow.Start", LogSeverity.Trace );

            ScreenMessages screenMsgs = FindObjectOfType<ScreenMessages>();
            screenMsgsColor = screenMsgs.defaultColor;

            StartCoroutine( StartAfterSettingsAndControllerAreReady() );

            Log.Write( "method end", "TCWindow.Start", LogSeverity.Trace );
        }
        /// <summary>
        /// Configures the GUI once the Settings are loaded and the TimeController is ready to operate
        /// </summary>
        private IEnumerator StartAfterSettingsAndControllerAreReady()
        {
            string logCaller = "TimeController.StartAfterSettingsAndControllerAreReady";
            Log.Trace( "coroutine start", logCaller );

            while (!Settings.IsReady)
                yield return null;

            Log.Info( "Setting Up GUI Window Postions", logCaller );
            SetFlightWindowPosition( Settings.Instance.FlightWindowX, Settings.Instance.FlightWindowY );
            SetMainWindowPosition( Settings.Instance.MainWindowX, Settings.Instance.MainWindowY );
            SetSettingsWindowPosition( Settings.Instance.SettingsWindowX, Settings.Instance.SettingsWindowY );
            OnGUIUpdateConfigWithWindowLocations();

            currentFlightMode = Settings.Instance.WindowSelectedFlightMode;
            UpdateFlightWindowRectSize( true );

            while (TimeController.Instance == null || !TimeController.IsReady)
                yield return null;

            Log.Info( "Getting selected SOI as Current SOI", logCaller );
            if (selectedSOI == null)
                selectedSOI = TimeController.Instance.CurrentSOI;

            Log.Info( "TCGUI.Instance is Ready!", logCaller );
            IsReady = true;

            Log.Trace( "coroutine end", logCaller );
            yield break;
        }
        #endregion
        #region Update Methods
        private void Update()
        {
            // Don't do anything until the settings are loaded
            if (!IsReady || TimeController.Instance.CanControlWarpType == TimeControllable.None)
                return;

            UpdateThrottle();
            UpdateWarpMessage();
        }
        private void UpdateThrottle()
        {
            if (FlightInputHandler.state != null && throttleToggle && FlightInputHandler.state.mainThrottle != throttleSet)
                FlightInputHandler.state.mainThrottle = throttleSet;
        }
        private void UpdateWarpMessage()
        {
            // Display Warp Screen Message
            if (warpScreenMessage != null)
            {
                if (!(Settings.Instance.ShowScreenMessages))
                {
                    if (ScreenMessages.Instance.ActiveMessages.Contains( warpScreenMessage ))
                    {
                        ScreenMessages.RemoveMessage( warpScreenMessage );
                    }
                    warpScreenMessage = null;
                    return;
                }

                if (ScreenMessages.Instance.ActiveMessages.Contains( warpScreenMessage ))
                {
                    if (TimeController.Instance.CurrentControllerMessage != warpScreenMessage.message)
                    {
                        ScreenMessages.RemoveMessage( warpScreenMessage );
                        warpScreenMessage = null;

                        if (TimeController.Instance.CurrentControllerMessage != "")
                        {
                            warpScreenMessage = ScreenMessages.PostScreenMessage( TimeController.Instance.CurrentControllerMessage, Mathf.Infinity, ScreenMessageStyle.UPPER_CENTER );
                        }
                    }
                }
                else
                {
                    warpScreenMessage = null;
                }
            }
            else if (TimeController.Instance.CurrentControllerMessage != "")
            {
                if (Settings.Instance.ShowScreenMessages)
                    warpScreenMessage = ScreenMessages.PostScreenMessage( TimeController.Instance.CurrentControllerMessage, Mathf.Infinity, ScreenMessageStyle.UPPER_CENTER );
            }
        }

        private void LateUpdate()
        {
            LateUpdateFlightResultsDialog();
        }

        private void LateUpdateFlightResultsDialog()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (supressFlightResultsDialog)
                FlightResultsDialog.Close();
        }
        #endregion
        #region GUI Methods
        private void OnGUI()
        {
            // Don't do anything until the settings are loaded
            if (!IsReady
                || TimeController.Instance.CanControlWarpType == TimeControllable.None
                || Settings.Instance.GUITempHidden
                )
                return;

            UnityEngine.GUI.skin = null;
            if (Settings.Instance.WindowVisible)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    OnGUIFlightWindow();
                }
                else if (HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    OnGUISpaceCenterWindow();
                }
                if (settingsOpen && !Settings.Instance.WindowMinimized)
                {
                    OnGUISettingsWindow();
                }
            }
            UnityEngine.GUI.skin = HighLogic.Skin;
            OnGUIUpdateConfigWithWindowLocations(); // Settings class setters only actually trigger a config save if the values change
        }
        private void OnGUIFlightWindow()
        {
            UpdateFlightWindowRectSize();
            settingsButton.x = flightWindowRect.xMax - flightWindowRect.xMin - 25; //Move the ?
            flightWindowRect = GUILayout.Window( tcsFlightWindowHashCode, flightWindowRect, MainGUI, "Time Control" );
        }
        private void OnGUISpaceCenterWindow()
        {
            settingsButton.x = spaceCenterWindowRect.xMax - spaceCenterWindowRect.xMin - 25; //Move the ?
            spaceCenterWindowRect = GUILayout.Window( tcsMainWindowHashCode, spaceCenterWindowRect, MainGUI, "Time Control" );
        }
        private void OnGUISettingsWindow()
        {
            settingsWindowRect = GUILayout.Window( tcsSettingsWindowHashCode, settingsWindowRect, SettingsGUI, "Time Control Settings" );
        }
        private void OnGUIUpdateConfigWithWindowLocations()
        {
            if (!Settings.IsReady)
                return;

            Settings.Instance.FlightWindowX = (int)flightWindowRect.x;
            Settings.Instance.FlightWindowY = (int)flightWindowRect.y;
            Settings.Instance.MainWindowX = (int)spaceCenterWindowRect.x;
            Settings.Instance.MainWindowY = (int)spaceCenterWindowRect.y;
            Settings.Instance.SettingsWindowX = (int)settingsWindowRect.x;
            Settings.Instance.SettingsWindowY = (int)settingsWindowRect.y;
        }
        #region Shared GUI
        private void MainGUI(int windowId)
        {
            UnityEngine.GUI.enabled = true;

            //Minimize button
            if (UnityEngine.GUI.Button( minimizeButton, "" ))
                Settings.Instance.WindowMinimized = !Settings.Instance.WindowMinimized;

            if (!Settings.Instance.WindowMinimized)
            {
                modeButtons();

                GUIHeaderCurrentWarpState();

                if (HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    modeRails();
                }
                else if (HighLogic.LoadedSceneIsFlight)
                {
                    switch (Settings.Instance.WindowSelectedFlightMode)
                    {
                        case 0:
                            modeSlowmo();
                            break;
                        case 1:
                            modeHyper();
                            break;
                        case 2:
                            modeRails();
                            break;
                    }
                }
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
                string rate = "";

                if (TimeController.Instance.CurrentWarpState == TimeControllable.Rails || TimeController.Instance.CurrentWarpState == TimeControllable.Physics)
                {
                    rate = TimeController.Instance.CurrentRailsWarpRateText + "x";
                }
                else
                {
                    rate = (PerformanceManager.ptr / 1 * 100).ToString( "0" ) + "%";
                }

                GUILayout.Label( "Time: " + rate );
                GUILayout.Label( "FPS: " + Mathf.Floor( PerformanceManager.fps ) );
                GUILayout.FlexibleSpace();

                // Button to resturn to realtime
                bool returnButton = false;
                if (TimeController.Instance.TimePaused || FlightDriver.Pause)
                {
                    returnButton = GUILayout.Button( "PAUSED" );
                }
                else if (TimeController.Instance.CurrentWarpState == TimeControllable.Hyper)
                {
                    returnButton = GUILayout.Button( "HYPER" );
                }
                else if (TimeController.Instance.CurrentWarpState == TimeControllable.Rails)
                {
                    returnButton = GUILayout.Button( "RAILS" );
                }
                else if (TimeController.Instance.CurrentWarpState == TimeControllable.Physics)
                {
                    returnButton = GUILayout.Button( "PHYS" );
                }
                else if (TimeController.Instance.CurrentWarpState == TimeControllable.SlowMo)
                {
                    returnButton = GUILayout.Button( "SLOWMO" );
                }
                else
                {
                    GUILayout.Label( "NORM" );
                }
                if (returnButton)
                {
                    TimeController.Instance.Realtime();
                }
            }
            GUILayout.EndHorizontal();
        }
        private void modeButtons()
        {
            Color bc = UnityEngine.GUI.backgroundColor;
            Color cc = UnityEngine.GUI.contentColor;

            UnityEngine.GUI.backgroundColor = Color.clear;

            //Settings Window Toggle
            {
                if (!settingsOpen)
                    UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );

                if (UnityEngine.GUI.Button( settingsButton, "?" ))
                    settingsOpen = !settingsOpen;

                UnityEngine.GUI.contentColor = cc;
            }

            // Only allow switching modes when in flight
            if (HighLogic.LoadedSceneIsFlight)
            {
                //Slow-mo mode
                {
                    if (Settings.Instance.WindowSelectedFlightMode != 0)
                        UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                    if (UnityEngine.GUI.Button( mode0Button, "S" ))
                        Settings.Instance.WindowSelectedFlightMode = 0;
                    UnityEngine.GUI.contentColor = cc;
                }

                //Hyper mode
                {
                    if (Settings.Instance.WindowSelectedFlightMode != 1)
                        UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                    if (UnityEngine.GUI.Button( mode1Button, "H" ))
                        Settings.Instance.WindowSelectedFlightMode = 1;
                    UnityEngine.GUI.contentColor = cc;
                }

                //Rails mode
                {
                    if (Settings.Instance.WindowSelectedFlightMode != 2)
                        UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                    if (UnityEngine.GUI.Button( mode2Button, "R" ))
                        Settings.Instance.WindowSelectedFlightMode = 2;
                    UnityEngine.GUI.contentColor = cc;
                }
            }

            UnityEngine.GUI.backgroundColor = bc;

            GUI.enabled = true;
        }
        private void GUIPauseOrResumeButton()
        {
            GUI.enabled = TimeController.Instance.IsOperational;
            if (!TimeController.Instance.TimePaused)
            {
                if (GUILayout.Button( "Pause", GUILayout.Width( 60 ) ))
                    TimeController.Instance.TogglePause();
            }
            else
            {
                if (GUILayout.Button( "Resume", GUILayout.Width( 60 ) ))
                    TimeController.Instance.TogglePause();
            }
            GUI.enabled = true;
        }
        private void GUITimeStepButton()
        {
            GUI.enabled = TimeController.Instance.TimePaused;
            if (GUILayout.Button( ">", GUILayout.Width( 20 ) ))
                TimeController.Instance.IncrementTimeStep();
            GUI.enabled = true;
        }
        private void GUIThrottleControl()
        {
            throttleToggle = GUILayout.Toggle( throttleToggle, "Throttle Control: " + Mathf.Round( throttleSet * 100 ) + "%" );
            
            Action<float> updateThrottle = delegate (float f) { throttleSet = f / 100.0f; };
            // Force slider to select 1 decimal place values between min and max
            Func<float, float> modifyFieldThrottle = delegate (float f) { return (Mathf.Floor( f * 10 ) / 10); };
            floatTextBoxAndSliderCombo( null, (throttleSet * 100f), 0.0f, 100.0f, updateThrottle, modifyFieldThrottle );
        }

        /// <summary>
        /// Creates a text box + slider that both update the same backing field
        /// </summary>
        /// <param name="comboLabel">label for this control</param>
        /// <param name="backingFieldFloat">Value of the backing field</param>
        /// <param name="sliderMin">Minimim value for the backing field</param>
        /// <param name="sliderMax">Maximum value for the backing field</param>
        /// <param name="updateBackingField">Action that is called when we want to update the backing field</param>
        /// <param name="modifyField">Function that is applied to the GUI input prior to updating the backing field</param>
        private void floatTextBoxAndSliderCombo(string comboLabel, float backingFieldFloat, float sliderMin, float sliderMax, Action<float> updateBackingField, Func<float, float> modifyField = null)
        {
            string backingFieldStr = backingFieldFloat.ToString();
            float fieldFloat;
            string fieldStr;

            if (comboLabel != null && comboLabel != "")
                GUILayout.Label( comboLabel );

            GUILayout.BeginHorizontal();
            {
                // Text Box to enter values
                fieldStr = GUILayout.TextField( backingFieldStr, GUILayout.Width( 35 ) );
                if (fieldStr != backingFieldStr && float.TryParse( fieldStr, out fieldFloat ))
                {
                    backingFieldStr = fieldStr;
                    if (modifyField != null)
                        fieldFloat = modifyField( fieldFloat );
                    updateBackingField( fieldFloat );
                }

                // Slider to enter values
                fieldFloat = GUILayout.HorizontalSlider( backingFieldFloat, sliderMin, sliderMax );
                if (modifyField != null)
                    fieldFloat = modifyField( fieldFloat );
                if (fieldFloat != backingFieldFloat)
                {
                    updateBackingField( fieldFloat );
                }
            }
            GUILayout.EndHorizontal();
        }

        #endregion
        #region Rails GUI
        private void modeRails()
        {
            GUI.enabled = true;
            GUILayout.BeginVertical();
            {
                modeRailsHeader();
                warpScroll = GUILayout.BeginScrollView( warpScroll, GUILayout.Height( 210 ) );
                {
                    GUILayout.BeginHorizontal();
                    {
                        modeRailsWarpRatesList();
                        if (!SOISelect)
                            modeRailsAltitudeLimitsList();
                        else
                            modeRailsSoiSelector();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                GUILayout.BeginHorizontal();
                {
                    modeRailsResetRates();
                }
                GUILayout.EndHorizontal();
                GUILayout.Label( "", GUILayout.Height( 5 ) );
                modeRailsWarpTo();
            }
            GUILayout.EndVertical();
        }
        private void modeRailsHeader()
        {
            if (selectedSOI == null)
                selectedSOI = TimeController.Instance.CurrentSOI;

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Current SOI: " + TimeController.Instance.CurrentSOI.name );
                GUILayout.FlexibleSpace();
                GameSettings.KERBIN_TIME = GUILayout.Toggle( GameSettings.KERBIN_TIME, "Use Kerbin Time" );
            }
            GUILayout.EndHorizontal();
            GUILayout.Label( "", GUILayout.Height( 5 ) );
            GUILayout.BeginHorizontal();
            {
                modeRailsWarpLevelsButtons();
                GUILayout.Label( "Altitude Limit" );
                GUILayout.FlexibleSpace();
                string s = (TimeController.Instance.CurrentSOI == selectedSOI) ? TimeController.Instance.CurrentSOI.name : selectedSOI.name;
                SOISelect = GUILayout.Toggle( SOISelect, s, "button", GUILayout.Width( 80 ) );
            }
            GUILayout.EndHorizontal();
        }
        private void modeRailsWarpLevelsButtons()
        {
            GUILayout.BeginHorizontal( GUILayout.Width( 175 ) );
            {
                GUILayout.Label( "Warp Rate" );
                if (GUILayout.Button( "+", GUILayout.Width( 20 ) ))
                {
                    if (Settings.Instance.WarpLevels < 99)
                    {
                        Settings.Instance.AddWarpLevel();
                    }
                }
                if (GUILayout.Button( "-", GUILayout.Width( 20 ) ))
                {
                    if (Settings.Instance.WarpLevels > 8)
                    {
                        Settings.Instance.RemoveWarpLevel();
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
        private void modeRailsWarpRatesList()
        {
            GUILayout.BeginVertical( GUILayout.Width( 20 ) );
            {
                for (int i = 0; i < Settings.Instance.CustomWarpRates.Count; i++)
                {
                    GUILayout.Label( i + 1 + ":" );
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical( GUILayout.Width( 145 ) );
            {
                GUI.enabled = false;
                Settings.Instance.CustomWarpRates[0].WarpRate = GUILayout.TextField( Settings.Instance.CustomWarpRates[0].WarpRate, 10 );
                GUI.enabled = true;

                for (int i = 1; i < Settings.Instance.CustomWarpRates.Count; i++)
                {
                    string wr = Settings.Instance.CustomWarpRates[i].WarpRate;
                    Settings.Instance.CustomWarpRates[i].WarpRate = GUILayout.TextField( Settings.Instance.CustomWarpRates[i].WarpRate, 10 );
                    if (wr != Settings.Instance.CustomWarpRates[i].WarpRate)
                    {
                        Settings.Instance.SetNeedsSavedFlag();
                        TimeController.Instance.UpdateInternalTimeWarpArrays();
                    }
                }
            }
            GUILayout.EndVertical();
        }
        private void modeRailsAltitudeLimitsList()
        {
            GUILayout.BeginVertical( GUILayout.Width( 145 ) );
            {
                GUI.enabled = false;
                try
                {
                    Settings.Instance.CustomAltitudeLimits[selectedSOI][0].AltitudeLimit = GUILayout.TextField( Settings.Instance.CustomAltitudeLimits[selectedSOI][0].AltitudeLimit, 20 );
                }
                catch (Exception e)
                {
                    Log.Error( "Exception " + e.Message, "TCGUI.altitudeLimitsList" );
                    Log.Error( "Altitiude 0" );
                    IsReady = false;
                }
                GUI.enabled = true;

                for (int i = 1; i < Settings.Instance.CustomAltitudeLimits[selectedSOI].Count; i++)
                {
                    try
                    {
                        string al = Settings.Instance.CustomAltitudeLimits[selectedSOI][i].AltitudeLimit;
                        Settings.Instance.CustomAltitudeLimits[selectedSOI][i].AltitudeLimit = GUILayout.TextField( Settings.Instance.CustomAltitudeLimits[selectedSOI][i].AltitudeLimit, 20 );
                        if (al != Settings.Instance.CustomAltitudeLimits[selectedSOI][i].AltitudeLimit)
                        {
                            Settings.Instance.SetNeedsSavedFlag();
                            TimeController.Instance.UpdateInternalTimeWarpArrays();
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error( "Exception " + e.Message, "TCGUI.altitudeLimitsList" );
                        Log.Error( "Altitiude " + i.ToString() );
                        IsReady = false;
                    }
                }
            }
            GUILayout.EndVertical();
        }
        private void modeRailsSoiSelector()
        {
            GUILayout.BeginVertical( GUILayout.Width( 150 ) );
            {
                if (GUILayout.Button( "Current" ))
                {
                    selectedSOI = TimeController.Instance.CurrentSOI;
                    SOISelect = false;
                    warpScroll.y = 0;
                }
                int i = 0;

                foreach (CelestialBody c in FlightGlobals.Bodies)
                {
                    if (GUILayout.Button( c.name ))
                    {
                        selectedSOI = c;
                        SOISelect = false;
                        warpScroll.y = 0;
                    }
                    i++;
                }
            }
            GUILayout.EndVertical();
        }
        private void modeRailsWarpTo()
        {
            GUI.enabled = (TimeWarp.CurrentRateIndex == 0);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Time Warp:" );

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
            }
            GUILayout.EndHorizontal();

            TimeController.Instance.RailsPauseOnTimeReached = GUILayout.Toggle( TimeController.Instance.RailsPauseOnTimeReached, "Pause on time reached" );

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button( "Auto Warp", GUILayout.Width( 174 ) ))
                {
                    bool result = TimeController.Instance.AutoWarpForDuration( warpYears, warpDays, warpHours, warpMinutes, warpSeconds );
                    if (result)
                    {
                        warpYears = "0";
                        warpDays = "0";
                        warpHours = "0";
                        warpMinutes = "0";
                        warpSeconds = "0";
                    }
                }

                GUI.enabled = (TimeWarp.CurrentRateIndex != 0);
                if (GUILayout.Button( "Cancel Warp" ))
                {
                    TimeController.Instance.CancelRailsWarp();
                }
            }
            GUILayout.EndHorizontal();

            GUI.enabled = true;
        }
        private void modeRailsResetRates()
        {
            if (GUILayout.Button( "Reset warp rates", GUILayout.Width( 174 ) ))
                Settings.Instance.ResetWarpRates();
            if (GUILayout.Button( "Reset body altitude limits" ))
                Settings.Instance.ResetCustomAltitudeLimitsForBody( selectedSOI );
        }
        #endregion Rails GUI
        #region Slow-Mo GUI
        private void modeSlowmo()
        {
            modeSlowmoFPSKeeper();
            modeSlowmoTimeScale();
        }

        private void modeSlowmoFPSKeeper()
        {
            bool fpsKeeperActive = GUILayout.Toggle( TimeController.Instance.IsFpsKeeperActive, "FPS Keeper: " + Mathf.Round( Settings.Instance.FpsMinSlider / 5 ) * 5 + " fps" );
            if (fpsKeeperActive != TimeController.Instance.IsFpsKeeperActive)
                TimeController.Instance.SetFPSKeeper( fpsKeeperActive );

            GUI.enabled = true;
            Settings.Instance.FpsMinSlider = (int)GUILayout.HorizontalSlider( Settings.Instance.FpsMinSlider, 5, 60 );
        }
        private void modeSlowmoTimeScale()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUI.enabled = (!TimeController.Instance.IsFpsKeeperActive);
                    {
                        if (TimeController.Instance.TruePOS != 1)
                            GUILayout.Label( "Time Scale: 1/" + TimeController.Instance.TruePOS.ToString() + "x" );
                        else
                            GUILayout.Label( "Time Scale: " + TimeController.Instance.TruePOS.ToString() + "x" );
                    }
                    GUIPauseOrResumeButton();
                    GUITimeStepButton();
                }
                GUILayout.EndHorizontal();

                GUI.enabled = (TimeController.Instance.IsOperational && !TimeController.Instance.IsFpsKeeperActive);
                {
                    float ts = GUILayout.HorizontalSlider( TimeController.Instance.TimeSlider, 0f, 1f );
                    if (TimeController.Instance.TimeSlider != ts)
                        TimeController.Instance.UpdateTimeSlider( ts );

                    TimeController.Instance.DeltaLocked = (TimeController.Instance.IsFpsKeeperActive
                        ? GUILayout.Toggle( TimeController.Instance.IsFpsKeeperActive, "Lock physics delta to default" )
                        : GUILayout.Toggle( TimeController.Instance.DeltaLocked, "Lock physics delta to default" ));

                    GUILayout.Label( "", GUILayout.Height( 5 ) );

                    GUIThrottleControl();
                }
                GUI.enabled = true;
            }
            GUILayout.EndVertical();
        }
        #endregion
        #region Hyper GUI
        private void modeHyper()
        {
            GUI.enabled = (TimeController.Instance.IsOperational && (TimeController.Instance.CurrentWarpState == TimeControllable.None || TimeController.Instance.CurrentWarpState == TimeControllable.Hyper));
            {
                GUILayout.BeginVertical();
                {
                    modeHyperHyperMaxRate();
                    modeHyperHyperMinPhys();
                    modeHyperButtons();
                    GUILayout.Label( "", GUILayout.Height( 5 ) );
                    modeHyperWarpTime();
                    GUILayout.Label( "", GUILayout.Height( 5 ) );
                    GUIThrottleControl();
                }
                GUILayout.EndVertical();
            }
            GUI.enabled = true;
        }
        private void modeHyperHyperMaxRate()
        {
            string hyperMaxRateLabel = "Max Warp Rate: " + Mathf.Round( TimeController.Instance.HyperMaxRate );
            Action<float> updateHyperMaxRate = delegate (float f) { TimeController.Instance.HyperMaxRate = f; };
            // Force slider to select integer values between min and max
            Func<float, float> modifyFieldHyperMaxRate = delegate (float f) { return Mathf.Floor(f); };
            floatTextBoxAndSliderCombo( hyperMaxRateLabel, TimeController.Instance.HyperMaxRate, TimeController.HyperMaxRateMin, TimeController.HyperMaxRateMax, updateHyperMaxRate, modifyFieldHyperMaxRate);
        }

        private void modeHyperHyperMinPhys()
        {
            string hyperMinPhysLabel = "Min Physics Accuracy: " + 1 / TimeController.Instance.HyperMinPhys;
            Action<float> updatehyperMinPhys = delegate (float f) { TimeController.Instance.HyperMinPhys = f; };
            floatTextBoxAndSliderCombo( hyperMinPhysLabel, TimeController.Instance.HyperMinPhys, TimeController.HyperMinPhysMin, TimeController.HyperMinPhysMax, updatehyperMinPhys );
        }

        private void modeHyperButtons()
        {
            GUILayout.BeginHorizontal();
            {
                if (TimeController.Instance.CurrentWarpState == TimeControllable.None)
                {
                    if (GUILayout.Button( "HyperWarp" ))
                    {
                        TimeController.Instance.ToggleHyperWarp();
                    }
                }
                else if (TimeController.Instance.CurrentWarpState == TimeControllable.Hyper)
                {
                    if (GUILayout.Button( "End HyperWarp" ))
                    {
                        TimeController.Instance.CancelHyperWarp();
                    }
                }
                GUIPauseOrResumeButton();
                GUITimeStepButton();
            }
            GUILayout.EndHorizontal();
        }


        private string hyperWarpHours = "0";
        private string hyperWarpMinutes = "0";
        private string hyperWarpSeconds = "0";
        private void modeHyperWarpTime()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Timed Warp:" );
                hyperWarpHours = GUILayout.TextField( hyperWarpHours, GUILayout.Width( 35 ) );
                GUILayout.Label( "h " );
                hyperWarpMinutes = GUILayout.TextField( hyperWarpMinutes, GUILayout.Width( 35 ) );
                GUILayout.Label( "m " );
                hyperWarpSeconds = GUILayout.TextField( hyperWarpSeconds, GUILayout.Width( 35 ) );
                GUILayout.Label( "s" );
            }
            GUILayout.EndHorizontal();

            TimeController.Instance.HyperPauseOnTimeReached = GUILayout.Toggle( TimeController.Instance.HyperPauseOnTimeReached, "Pause on time reached" );

            if (GUILayout.Button( "Timed Warp" ))
            {
                bool result = TimeController.Instance.HyperWarpForDuration( hyperWarpHours, hyperWarpMinutes, hyperWarpSeconds );
                if (result)
                {
                    hyperWarpHours = "0";
                    hyperWarpMinutes = "0";
                    hyperWarpSeconds = "0";
                }
            }
        }

        #endregion
        #region Settings GUI
        private void SettingsGUI(int windowId)
        {
            bool closeButton = UnityEngine.GUI.Button( minimizeButton, "" );
            //close button
            if (closeButton)
                settingsOpen = !settingsOpen;

            GUILayout.BeginVertical();
            {
                GUILayout.Label( "Physics Time Ratio: " + PerformanceManager.ptr.ToString( "0.0000" ) );
                GUILayout.Label( "UT: " + Planetarium.GetUniversalTime() );
                GUILayout.Label( "Time Scale: " + Time.timeScale );
                GUILayout.Label( "Physics Delta: " + Time.fixedDeltaTime );
                GUILayout.Label( "Max Delta Time: " + Time.maximumDeltaTime );

                GUI.enabled = !TimeController.Instance.IsFpsKeeperActive;
                TimeController.Instance.MaxDeltaTimeSlider = GUILayout.HorizontalSlider( TimeController.Instance.MaxDeltaTimeSlider, TimeController.MaxDeltaTimeSliderMax, TimeController.MaxDeltaTimeSliderMin );
                GUI.enabled = true;

                //GUILayout.BeginHorizontal();
                //{
                //    GUILayout.Label( "FPS: " + Mathf.Floor( PerformanceManager.fps ), GUILayout.Width( 50 ) );
                //    Settings.Instance.ShowFPS = GUILayout.Toggle( Settings.Instance.ShowFPS, "Show" );
                //    string sfpsx = GUILayout.TextField( Settings.Instance.FpsX.ToString(), 5, GUILayout.Width( 30 ) );
                //    string sfpsy = GUILayout.TextField( Settings.Instance.FpsY.ToString(), 5, GUILayout.Width( 30 ) );
                //    int FpsX = Settings.Instance.FpsX;
                //    int FpsY = Settings.Instance.FpsY;
                //    if (int.TryParse( sfpsx, out FpsX ))
                //        Settings.Instance.FpsX = FpsX;
                //    if (int.TryParse( sfpsy, out FpsY ))
                //        Settings.Instance.FpsY = FpsY;
                //}
                //GUILayout.EndHorizontal();                

                GUILayout.Label( "PPS: " + PerformanceManager.pps );
                supressFlightResultsDialog = GUILayout.Toggle( supressFlightResultsDialog, "Supress Results Dialog" );

                Settings.Instance.UseStockToolbar = GUILayout.Toggle( Settings.Instance.UseStockToolbar, "Use Stock Toolbar" );

                Settings.Instance.ShowScreenMessages = GUILayout.Toggle( Settings.Instance.ShowScreenMessages, "Show Onscreen Messages" );

                string saveIntervalLabel = "Save Settings Every " + Mathf.Round( Settings.Instance.SaveInterval ) + "s";
                Action<float> updateSaveInterval = delegate (float f) { Settings.Instance.SaveInterval = f; };
                floatTextBoxAndSliderCombo( saveIntervalLabel, Settings.Instance.SaveInterval, Settings.Instance.SaveIntervalMin, Settings.Instance.SaveIntervalMax, updateSaveInterval );

                GUILayout.Label( "", GUILayout.Height( 5 ) );
                GUILayout.Label( "Key Bindings:" );

                //Keys
                Color c = UnityEngine.GUI.contentColor;
                foreach (TCKeyBinding kb in Settings.Instance.KeyBinds)
                {
                    if (kb.IsKeyAssigned)
                        UnityEngine.GUI.contentColor = Color.yellow;
                    else
                        UnityEngine.GUI.contentColor = c;

                    string buttonDesc = "";
                    if (kb.TCUserAction == TimeControlUserAction.CustomKeySlider)
                    {
                        string pos = (PluginUtilities.convertToExponential( Settings.Instance.CustomKeySlider ) != 1) ? ("1/" + PluginUtilities.convertToExponential( Settings.Instance.CustomKeySlider ).ToString()) : "1";
                        buttonDesc = "Custom-" + pos + 'x';
                    }
                    else
                    {
                        buttonDesc = kb.Description;
                    }

                    buttonDesc = buttonDesc + ": " + (kb.IsKeyAssigned ? kb.KeyCombinationString : "None");

                    if (kb.TCUserAction == TimeControlUserAction.CustomKeySlider)
                        Settings.Instance.CustomKeySlider = GUILayout.HorizontalSlider( Settings.Instance.CustomKeySlider, 0f, 1f );

                    if (currentlyAssigningKey)
                        UnityEngine.GUI.enabled = false;

                    bool assignKey = GUILayout.Button( buttonDesc );
                    if (assignKey)
                    {
                        settingsGUIAssignKey( buttonDesc, kb );
                    }

                    UnityEngine.GUI.enabled = true;
                }
                UnityEngine.GUI.contentColor = c;
            }
            GUILayout.EndVertical();

            if (Event.current.button > 0 && Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) //Ignore right & middle clicks to drag the window
                Event.current.Use();
            UnityEngine.GUI.DragWindow();
        }

        private void settingsGUIAssignKey(string buttonDesc, TCKeyBinding kb)
        {
            // Left Mouse Button, Assign Key
            if (Event.current.button == 0 && !currentlyAssigningKey)
            {
                currentlyAssigningKey = true;
                KeyboardInputManager.Instance.GetPressedKeyCombination( (lkc) =>
                {
                    string logCaller = "OnSettingsGUI Key Binding Callback for button " + buttonDesc;
                    Log.Trace( "method start", logCaller );
                    currentlyAssigningKey = false;
                    kb.KeyCombination = new List<KeyCode>( lkc );
                    kb.KeyCombinationString = KeyboardInputManager.GetKeyCombinationString( lkc );
                    Settings.Instance.SetNeedsSavedFlag();
                    Log.Info( "Key Combination " + kb.KeyCombinationString + " assigned to button " + buttonDesc, logCaller );
                    Log.Trace( "method end", logCaller );
                } );
            }
            // Right Mouse Button, Clear Assigned Key
            else if (Event.current.button == 1 && !currentlyAssigningKey)
            {
                kb.KeyCombination.Clear();
                kb.KeyCombinationString = "";
                Settings.Instance.SetNeedsSavedFlag();
            }
        }

        #endregion
        #endregion
        #endregion
    }
}

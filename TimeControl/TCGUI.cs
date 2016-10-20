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
    [KSPAddon( KSPAddon.Startup.MainMenu, true )]
    internal class TCGUI : MonoBehaviour
    {
        #region Singleton
        private static TCGUI instance;
        internal static TCGUI Instance { get { return instance; } }
        #endregion
        #region GUI State
        public static bool IsReady { get; private set; } = false;
        #endregion

        #region Public Properties
        public bool WindowsVisible {
            get {
                return windowsVisible;
            }
            set {
                if (windowsVisible != value)
                {
                    windowsVisible = value;
                    if (Settings.IsReady)
                        Settings.Instance.WindowsVisible = value;
                }
            }
        }
        public bool ShowScreenMessages {
            get {
                return showScreenMessages;
            }
            set {
                if (showScreenMessages != value)
                {
                    showScreenMessages = value;
                    if (Settings.IsReady)
                        Settings.Instance.ShowScreenMessages = value;
                }
            }
        }
        public bool UseStockToolbar {
            get {
                return useStockToolbar;
            }
            set {
                if (useStockToolbar != value)
                {
                    useStockToolbar = value;
                    if (Settings.IsReady)
                        Settings.Instance.UseStockToolbar = value;
                }
            }
        }
        public int WindowSelectedFlightMode { 
            get {
                return windowSelectedFlightMode;
            }
            set {
                if (windowSelectedFlightMode != value)
                {
                    windowSelectedFlightMode = value;
                    UpdateFlightWindowRectSize();
                    if (Settings.IsReady)
                        Settings.Instance.WindowSelectedFlightMode = value;
                }
            }
        }
        public bool SupressFlightResultsDialog {
            get {
                return supressFlightResultsDialog;
            }

            set {
                if (supressFlightResultsDialog != value)
                {
                    supressFlightResultsDialog = value;
                    if (Settings.IsReady)
                        Settings.Instance.SupressFlightResultsDialog = value;
                }
            }
        }
        public bool SettingsWindowOpen {
            get {
                return settingsWindowOpen;
            }

            set {
                if (settingsWindowOpen != value)
                {
                    settingsWindowOpen = value;
                    if (Settings.IsReady)
                        Settings.Instance.SettingsWindowOpen = value;
                }
            }
        }
        public bool WindowMinimized {
            get {
                return windowMinimized;
            }

            set {
                if (windowMinimized != value)
                {
                    windowMinimized = value;
                    if (Settings.IsReady)
                        Settings.Instance.WindowMinimized = value;
                }
            }
        }
        public float SaveInterval {
            get {
                return saveInterval;
            }
            set {
                if (saveInterval != value)
                {
                    saveInterval = value;
                    if (Settings.IsReady)
                        Settings.Instance.SaveInterval = saveInterval;
                }
            }
        }
        public bool UseCustomDateTimeFormatter {
            get {
                return useCustomDateTimeFormatter;
            }
            set {
                if (useCustomDateTimeFormatter != value)
                {
                    useCustomDateTimeFormatter = value;
                    SetDateTimeFormatter();
                    if (Settings.IsReady)
                        Settings.Instance.UseCustomDateTimeFormatter = useCustomDateTimeFormatter;
                }
            }
        }
        #endregion
        #region Public Methods
        public void ToggleGUIVisibility()
        {
            WindowsVisible = !WindowsVisible;
        }
        public void SetGUIVisibility(bool v)
        {
            WindowsVisible = v;
        }
        public bool GUITempHidden {
            get {
                return (TempGUIHidden.Count != 0);
            }
        }
        public void TempHideGUI(string lockedBy)
        {
            TempGUIHidden.Add( lockedBy );
        }
        public void TempUnHideGUI(string lockedBy)
        {
            TempGUIHidden.RemoveAll( x => x == lockedBy );
        }
        public void TempUnHideGUI()
        {
            TempGUIHidden.Clear();
        }
        #endregion        
        #region Fields
        // Temp Hide/Show GUI Windows
        private List<string> TempGUIHidden = new List<string>();

        // Fields backed by properties, updated by SETTINGS object
        private bool windowsVisible = false;
        private bool windowMinimized = false;
        private bool showScreenMessages = false;
        private bool useStockToolbar = false;
        private int windowSelectedFlightMode = 0;
        private bool supressFlightResultsDialog = false;
        private float saveInterval = Settings.SaveIntervalDefault;
        private float saveIntervalMin = Settings.SaveIntervalMin;
        private float saveIntervalMax = Settings.SaveIntervalMax;
        private bool settingsWindowOpen = false;
        private bool useCustomDateTimeFormatter = false;

        // Control the throttle with this GUI
        private bool throttleToggle;
        private float throttleSet;

        // Date Time Formatter
        private TCDateTimeFormatter customDTFormatter = new TCDateTimeFormatter();
        private IDateTimeFormatter defaultDTFormatter;

        // GUI toggles and values        
        private bool SOISelect = false;
        bool currentlyAssigningKey = false;
        private CelestialBody selectedSOI;
        private string warpYears = "0"; private string warpDays = "0"; private string warpHours = "0"; private string warpMinutes = "0"; private string warpSeconds = "0";
        private string hyperWarpHours = "0"; private string hyperWarpMinutes = "0"; private string hyperWarpSeconds = "0";

        // On-Screen Messages
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
        private int tcsSpaceCenterWindowHashCode = "Time Control Space Center".GetHashCode();
        private int tcsFlightWindowHashCode = "Time Control Flight".GetHashCode();
        private int tcsSettingsWindowHashCode = "Time Control Settings".GetHashCode();
        private Vector2 warpScroll;

        #endregion
        #region Private Methods
        /// <summary>
        /// Bind when a change to the Settings object happens, update the GUI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case Settings.PropertyStrings.CustomKeySlider:
                    break;
                case Settings.PropertyStrings.FlightWindowX:
                    flightWindowRect.x = ((Settings)sender).FlightWindowX;
                    flightWindowRect = flightWindowRect.ClampToScreen();
                    break;
                case Settings.PropertyStrings.FlightWindowY:
                    flightWindowRect.y = ((Settings)sender).FlightWindowY;
                    flightWindowRect = flightWindowRect.ClampToScreen();
                    break;
                case Settings.PropertyStrings.FpsMinSlider:
                    break;
                case Settings.PropertyStrings.FpsX:
                    break;
                case Settings.PropertyStrings.FpsY:
                    break;
                case Settings.PropertyStrings.LoggingLevel:
                    break;
                case Settings.PropertyStrings.MaxDeltaTimeSlider:
                    break;
                case Settings.PropertyStrings.SaveInterval:
                    saveInterval = ((Settings)sender).SaveInterval;
                    break;
                case Settings.PropertyStrings.SettingsWindowOpen:
                    settingsWindowOpen = ((Settings)sender).SettingsWindowOpen;
                    break;
                case Settings.PropertyStrings.SettingsWindowX:
                    settingsWindowRect.x = ((Settings)sender).SettingsWindowX;
                    settingsWindowRect = settingsWindowRect.ClampToScreen();
                    break;
                case Settings.PropertyStrings.SettingsWindowY:
                    settingsWindowRect.y = ((Settings)sender).SettingsWindowY;
                    settingsWindowRect = settingsWindowRect.ClampToScreen();
                    break;
                case Settings.PropertyStrings.ShowFPS:
                    break;
                case Settings.PropertyStrings.ShowScreenMessages:
                    showScreenMessages = ((Settings)sender).ShowScreenMessages;
                    break;
                case Settings.PropertyStrings.SpaceCenterWindowX:
                    spaceCenterWindowRect.x = ((Settings)sender).SpaceCenterWindowX;
                    spaceCenterWindowRect = spaceCenterWindowRect.ClampToScreen();
                    break;
                case Settings.PropertyStrings.SpaceCenterWindowY:
                    spaceCenterWindowRect.y = ((Settings)sender).SpaceCenterWindowY;
                    spaceCenterWindowRect = spaceCenterWindowRect.ClampToScreen();
                    break;
                case Settings.PropertyStrings.SupressFlightResultsDialog:
                    supressFlightResultsDialog = ((Settings)sender).SupressFlightResultsDialog;
                    break;
                case Settings.PropertyStrings.UseBlizzyToolbar:
                    break;
                case Settings.PropertyStrings.UseCustomDateTimeFormatter:
                    useCustomDateTimeFormatter = ((Settings)sender).UseCustomDateTimeFormatter;
                    SetDateTimeFormatter();
                    break;
                case Settings.PropertyStrings.UseStockToolbar:
                    useStockToolbar = ((Settings)sender).UseStockToolbar;
                    break;
                case Settings.PropertyStrings.WindowMinimized:
                    windowMinimized = ((Settings)sender).WindowMinimized;
                    UpdateFlightWindowRectSize();
                    break;
                case Settings.PropertyStrings.WindowSelectedFlightMode:
                    windowSelectedFlightMode = ((Settings)sender).WindowSelectedFlightMode;
                    UpdateFlightWindowRectSize();
                    break;
                case Settings.PropertyStrings.WindowsVisible:
                    windowsVisible = ((Settings)sender).WindowsVisible;
                    break;
                default:
                    break;
            }
        }
        private void SetDateTimeFormatter()
        {
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
        }        
        private void UpdateFlightWindowRectSize()
        {
            flightWindowRect.height = 0;
            switch (WindowSelectedFlightMode)
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
            Log.Write( "method start", "TCGUI.Awake", LogSeverity.Trace );

            UnityEngine.Object.DontDestroyOnLoad( this ); //Don't go away on scene changes
            instance = this;

            Log.Write( "method end", "TCGUI.Awake", LogSeverity.Trace );
        }
        private void Start()
        {
            Log.Write( "method start", "TCGUI.Start", LogSeverity.Trace );

            // Hide / Show UI on these events
            GameEvents.onGameSceneLoadRequested.Add( onGameSceneLoadRequested );
            GameEvents.onLevelWasLoaded.Add( onLevelWasLoaded );
            GameEvents.onHideUI.Add( onHideUI );
            GameEvents.onShowUI.Add( onShowUI );

            ScreenMessages screenMsgs = FindObjectOfType<ScreenMessages>();
            screenMsgsColor = screenMsgs.defaultColor;

            defaultDTFormatter = new KSPUtil.DefaultDateTimeFormatter();

            StartCoroutine( StartAfterSettingsAndControllerAreReady() );

            Log.Write( "method end", "TCGUI.Start", LogSeverity.Trace );
        }
        /// <summary>
        /// Configures the GUI once the Settings are loaded and the TimeController is ready to operate
        /// </summary>
        private IEnumerator StartAfterSettingsAndControllerAreReady()
        {
            string logCaller = "TCGUI.StartAfterSettingsAndControllerAreReady";
            Log.Trace( "coroutine start", logCaller );

            // Wait for the Settings object to be ready
            while (!Settings.IsReady)
                yield return null;

            // Assign fields from settings

            /*
            Log.Info( "Setting Up GUI Window Postions", logCaller );
            SetFlightWindowPosition( Settings.Instance.FlightWindowX, Settings.Instance.FlightWindowY );
            SetSpaceCenterWindowPosition( Settings.Instance.SpaceCenterWindowX, Settings.Instance.SpaceCenterWindowY );
            SetSettingsWindowPosition( Settings.Instance.SettingsWindowX, Settings.Instance.SettingsWindowY );            
            */

            // Get all the property strings from the Settings object, and call the SettingsPropertyChanged function in order to load up the initial settings           
            IEnumerable<string> propStrings =
                typeof( Settings.PropertyStrings ).GetFields( BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy )
                .Where( f => f.IsLiteral && f.FieldType == typeof( string ) )
                .Select( f => (string)f.GetValue( null ) );

            Log.Trace( "TCGUI Property String Count : " + propStrings.Count(), logCaller );
            foreach (string s in propStrings)
            {
                Log.Trace( "Setting TCGUI Property From Settings: " + s, logCaller );
                SettingsPropertyChanged( Settings.Instance, new SC.PropertyChangedEventArgs( s ) );
            }

            OnGUIUpdateConfigWithWindowLocations();

            Log.Info( "Wire Up Settings Property Changed Event Subscription", logCaller );
            Settings.Instance.PropertyChanged += SettingsPropertyChanged;

            // Wait for TimeController object to be ready
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

        #region Event Handlers
        private void onGameSceneLoadRequested(GameScenes gs)
        {
            string logCaller = "TCGUI.onGameSceneLoadRequested";
            Log.Trace( "method start", logCaller );

            onHideUI();

            Log.Trace( "method end", logCaller );
        }

        private void onLevelWasLoaded(GameScenes gs)
        {
            string logCaller = "TCGUI.onLevelWasLoaded";
            Log.Trace( "method start", logCaller );

            onShowUI();

            Log.Trace( "method end", logCaller );
        }

        private void onHideUI()
        {
            string logCaller = "TCGUI.onHideUI";
            Log.Trace( "method start", logCaller );

            Log.Info( "Hiding GUI for Settings Lock", logCaller );
            TempHideGUI( "GameEventsUI" );

            Log.Trace( "method end", logCaller );
        }
        private void onShowUI()
        {
            string logCaller = "TCGUI.onShowUI";
            Log.Trace( "method start", logCaller );

            Log.Info( "Unhiding GUI for Settings Lock", logCaller );
            TempUnHideGUI( "GameEventsUI" );

            Log.Trace( "method end", logCaller );
        }

        #endregion

        #region Update Methods
        private void Update()
        {
            // Don't do anything until we are initialized, and we can control warp
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
                if (!(showScreenMessages))
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
                if (showScreenMessages)
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

            if (SupressFlightResultsDialog)
                FlightResultsDialog.Close();
        }
        #endregion
        #region GUI Methods
        private void OnGUI()
        {
            // Don't do anything until the settings are loaded or we can actally warp
            if (!IsReady || TimeController.Instance.CanControlWarpType == TimeControllable.None || GUITempHidden)
                return;

            UnityEngine.GUI.skin = null;
            if (WindowsVisible)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    OnGUIFlightWindow();
                }
                else if (HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    OnGUISpaceCenterWindow();
                }
                if (SettingsWindowOpen && !WindowMinimized)
                {
                    OnGUISettingsWindow();
                }
            }
            UnityEngine.GUI.skin = HighLogic.Skin;
            OnGUIUpdateConfigWithWindowLocations(); // Only trigger a config save if the values change
        }
        private void OnGUIFlightWindow()
        {
            settingsButton.x = flightWindowRect.xMax - flightWindowRect.xMin - 25; //Move the ?
            flightWindowRect = GUILayout.Window( tcsFlightWindowHashCode, flightWindowRect, MainGUI, "Time Control" );
        }
        private void OnGUISpaceCenterWindow()
        {
            settingsButton.x = spaceCenterWindowRect.xMax - spaceCenterWindowRect.xMin - 25; //Move the ?
            spaceCenterWindowRect = GUILayout.Window( tcsSpaceCenterWindowHashCode, spaceCenterWindowRect, MainGUI, "Time Control" );
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
            Settings.Instance.SpaceCenterWindowX = (int)spaceCenterWindowRect.x;
            Settings.Instance.SpaceCenterWindowY = (int)spaceCenterWindowRect.y;
            Settings.Instance.SettingsWindowX = (int)settingsWindowRect.x;
            Settings.Instance.SettingsWindowY = (int)settingsWindowRect.y;
        }
        #region Shared GUI
        private void MainGUI(int windowId)
        {
            UnityEngine.GUI.enabled = true;

            //Minimize button
            if (UnityEngine.GUI.Button( minimizeButton, "" ))
                WindowMinimized = !WindowMinimized;

            if (!WindowMinimized)
            {
                modeButtons();

                GUIHeaderCurrentWarpState();

                if (HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    modeRails();
                }
                else if (HighLogic.LoadedSceneIsFlight)
                {
                    switch (WindowSelectedFlightMode)
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
                
                // Cache all string concatenations
                if (TimeController.Instance.CurrentWarpState == TimeControllable.Rails || TimeController.Instance.CurrentWarpState == TimeControllable.Physics)
                {
                    rate = TimeController.Instance.CurrentRailsWarpRateText.MemoizedConcat("x");
                }
                else
                {
                    rate = ((PerformanceManager.ptr / 1 * 100).MemoizedToString( "0" )).MemoizedConcat("%");
                }

                GUILayout.Label( "Time: ".MemoizedConcat(rate) );
                GUILayout.Label( "FPS: ".MemoizedConcat((Mathf.Floor( PerformanceManager.fps )).MemoizedToString()) );
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
                    GUILayout.Label( "NORMAL" );
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
                if (!SettingsWindowOpen)
                    UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );

                if (UnityEngine.GUI.Button( settingsButton, "?" ))
                    SettingsWindowOpen = !SettingsWindowOpen;

                UnityEngine.GUI.contentColor = cc;
            }

            // Only allow switching modes when in flight
            if (HighLogic.LoadedSceneIsFlight)
            {
                //Slow-mo mode
                {
                    if (WindowSelectedFlightMode != 0)
                        UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                    if (UnityEngine.GUI.Button( mode0Button, "S" ))
                        WindowSelectedFlightMode = 0;
                    UnityEngine.GUI.contentColor = cc;
                }

                //Hyper mode
                {
                    if (WindowSelectedFlightMode != 1)
                        UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                    if (UnityEngine.GUI.Button( mode1Button, "H" ))
                        WindowSelectedFlightMode = 1;
                    UnityEngine.GUI.contentColor = cc;
                }

                //Rails mode
                {
                    if (WindowSelectedFlightMode != 2)
                        UnityEngine.GUI.contentColor = new Color( 0.5f, 0.5f, 0.5f );
                    if (UnityEngine.GUI.Button( mode2Button, "R" ))
                        WindowSelectedFlightMode = 2;
                    UnityEngine.GUI.contentColor = cc;
                }
            }

            UnityEngine.GUI.backgroundColor = bc;

            GUI.enabled = true;
        }
        private void GUIPauseOrResumeButton()
        {
            bool priorEnabled = GUI.enabled;
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
            GUI.enabled = priorEnabled;
        }
        private void GUITimeStepButton()
        {
            bool priorEnabled = GUI.enabled;
            GUI.enabled = TimeController.Instance.TimePaused;
            if (GUILayout.Button( ">", GUILayout.Width( 20 ) ))
                TimeController.Instance.IncrementTimeStep();
            GUI.enabled = priorEnabled;
        }
        private void GUIThrottleControl()
        {
            throttleToggle = GUILayout.Toggle( throttleToggle, "Throttle Control: " + Mathf.Round( throttleSet * 100 ) + "%" );
            
            Action<float> updateThrottle = delegate (float f) { throttleSet = f / 100.0f; };
            // Force slider to select 1 decimal place values between min and max
            Func<float, float> modifyFieldThrottle = delegate (float f) { return (Mathf.Floor( f ) ); };
            IMGUIExtensions.floatTextBoxAndSliderCombo( null, (throttleSet * 100f), 0.0f, 100.0f, updateThrottle, modifyFieldThrottle );
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
            int WRCount = Settings.Instance.CustomWarpRates.Count;
            List<TCWarpRate> cwr = Settings.Instance.CustomWarpRates;

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
                GUI.enabled = false;
                cwr[0].WarpRate = GUILayout.TextField( cwr[0].WarpRate, 10 );
                GUI.enabled = true;

                for (int i = 1; i < WRCount; i++)
                {
                    string wr = cwr[i].WarpRate;
                    cwr[i].WarpRate = GUILayout.TextField( cwr[i].WarpRate, 10 );
                    if (wr != cwr[i].WarpRate)
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
            List<TCAltitudeLimit> calSOI = Settings.Instance.CustomAltitudeLimits[selectedSOI];

            GUILayout.BeginVertical( GUILayout.Width( 145 ) );
            {
                GUI.enabled = false;
                try
                {
                    calSOI[0].AltitudeLimit = GUILayout.TextField( calSOI[0].AltitudeLimit, 20 );
                }
                catch (Exception e)
                {
                    Log.Error( "Exception " + e.Message, "TCGUI.altitudeLimitsList" );
                    Log.Error( "Altitude 0" );
                }
                GUI.enabled = true;

                for (int i = 1; i < calSOI.Count; i++)
                {
                    TCAltitudeLimit tcal = calSOI[i];
                    try
                    {
                        string al = tcal.AltitudeLimit;
                        tcal.AltitudeLimit = GUILayout.TextField( tcal.AltitudeLimit, 20 );
                        if (al != tcal.AltitudeLimit)
                        {
                            Settings.Instance.SetNeedsSavedFlag();
                            TimeController.Instance.UpdateInternalTimeWarpArrays();
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error( "Exception " + e.Message, "TCGUI.altitudeLimitsList" );
                        Log.Error( "Altitude " + i.ToString() );
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
                
                for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
                {
                    CelestialBody c = FlightGlobals.Bodies[i];
                    if (GUILayout.Button( c.name ))
                    {
                        selectedSOI = c;
                        SOISelect = false;
                        warpScroll.y = 0;
                    }
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
            GUI.enabled = (TimeController.Instance.IsOperational
                && (TimeController.Instance.CurrentWarpState == TimeControllable.None || TimeController.Instance.CurrentWarpState == TimeControllable.SlowMo));

            bool fpsKeeperActive = GUILayout.Toggle( TimeController.Instance.IsFpsKeeperActive, "FPS Keeper: " + Mathf.Round( Settings.Instance.FpsMinSlider / 5 ) * 5 + " fps" );
            if (fpsKeeperActive != TimeController.Instance.IsFpsKeeperActive)
                TimeController.Instance.SetFPSKeeper( fpsKeeperActive );
            
            Settings.Instance.FpsMinSlider = (int)GUILayout.HorizontalSlider( Settings.Instance.FpsMinSlider, 5, 60 );

            GUI.enabled = true;
        }
        private void modeSlowmoTimeScale()
        {
            GUI.enabled = (TimeController.Instance.IsOperational
                && (TimeController.Instance.CurrentWarpState == TimeControllable.None || TimeController.Instance.CurrentWarpState == TimeControllable.SlowMo))
                && !TimeController.Instance.IsFpsKeeperActive;
            
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {                    
                    if (TimeController.Instance.TruePOS != 1)
                        GUILayout.Label( "Time Scale: 1/".MemoizedConcat(TimeController.Instance.TruePOS.MemoizedToString()).MemoizedConcat("x") );
                    else
                        GUILayout.Label( "Time Scale: ".MemoizedConcat(TimeController.Instance.TruePOS.MemoizedToString()).MemoizedConcat("x"));
                    
                    GUIPauseOrResumeButton();
                    GUITimeStepButton();
                }
                GUILayout.EndHorizontal();

                float ts = GUILayout.HorizontalSlider( TimeController.Instance.TimeSlider, 0f, 1f );
                if (TimeController.Instance.TimeSlider != ts)
                    TimeController.Instance.UpdateTimeSlider( ts );

                TimeController.Instance.DeltaLocked = (TimeController.Instance.IsFpsKeeperActive
                    ? GUILayout.Toggle( TimeController.Instance.IsFpsKeeperActive, "Lock physics delta to default" )
                    : GUILayout.Toggle( TimeController.Instance.DeltaLocked, "Lock physics delta to default" ));

                GUILayout.Label( "", GUILayout.Height( 5 ) );

                GUIThrottleControl();
            }
            GUILayout.EndVertical();
            
            GUI.enabled = true;
        }
        #endregion
        #region Hyper GUI
        private void modeHyper()
        {
            GUI.enabled = (TimeController.Instance.IsOperational 
                && (TimeController.Instance.CurrentWarpState == TimeControllable.None || TimeController.Instance.CurrentWarpState == TimeControllable.Hyper));
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
            IMGUIExtensions.floatTextBoxAndSliderCombo( hyperMaxRateLabel, TimeController.Instance.HyperMaxRate, TimeController.HyperMaxRateMin, TimeController.HyperMaxRateMax, updateHyperMaxRate, modifyFieldHyperMaxRate);
        }

        private void modeHyperHyperMinPhys()
        {
            string hyperMinPhysLabel = "Min Physics Accuracy: " + 1 / TimeController.Instance.HyperMinPhys;
            Action<float> updatehyperMinPhys = delegate (float f) { TimeController.Instance.HyperMinPhys = f; };
            IMGUIExtensions.floatTextBoxAndSliderCombo( hyperMinPhysLabel, TimeController.Instance.HyperMinPhys, TimeController.HyperMinPhysMin, TimeController.HyperMinPhysMax, updatehyperMinPhys );
        }

        private void modeHyperButtons()
        {
            GUILayout.BeginHorizontal();
            {
                if (TimeController.Instance.CurrentWarpState != TimeControllable.Hyper)
                {
                    if (GUILayout.Button( "HyperWarp" ))
                    {
                        TimeController.Instance.ToggleHyperWarp();
                    }
                }
                else
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
            // Close window button
            if (GUI.Button( minimizeButton, "" ))
                SettingsWindowOpen = !SettingsWindowOpen;

            GUILayout.BeginVertical();
            {
                GUILayout.Label( "Physics Time Ratio: " + PerformanceManager.ptr.ToString( "0.0000" ) );
                GUILayout.Label( "UT: " + Planetarium.GetUniversalTime() ); // Creates garbage, but not worth caching since it's monotonically increasing
                GUILayout.Label( "Time Scale: ".MemoizedConcat(Time.timeScale.MemoizedToString()) );
                GUILayout.Label( "Physics Delta: ".MemoizedConcat(Time.fixedDeltaTime.MemoizedToString()) );                
                GUILayout.Label( "PPS: " + PerformanceManager.pps );

                GUILayout.BeginHorizontal();
                GUILayout.Label( "Max Delta Time: ");
                GUILayout.Label( Time.maximumDeltaTime.MemoizedToString() );
                GUILayout.EndHorizontal();
                GUI.enabled = !TimeController.Instance.IsFpsKeeperActive;
                TimeController.Instance.MaxDeltaTimeSlider = GUILayout.HorizontalSlider( TimeController.Instance.MaxDeltaTimeSlider, TimeController.MaxDeltaTimeSliderMax, TimeController.MaxDeltaTimeSliderMin );
                GUI.enabled = true;

                
                SupressFlightResultsDialog = GUILayout.Toggle( SupressFlightResultsDialog, "Supress Results Dialog" );
                UseStockToolbar = GUILayout.Toggle( UseStockToolbar, "Use Stock Toolbar" );
                ShowScreenMessages = GUILayout.Toggle( ShowScreenMessages, "Show Onscreen Messages" );

                // Disable for now
                // UseCustomDateTimeFormatter = GUILayout.Toggle( UseCustomDateTimeFormatter, "Homeworld Timekeeping" );

                settingsGUISaveInterval();
                settingsGUILoggingLevel();
                settingsGUIKeyBinding();
            }
            GUILayout.EndVertical();

            if (Event.current.button > 0 && Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) //Ignore right & middle clicks to drag the window
                Event.current.Use();
            UnityEngine.GUI.DragWindow();
        }

        private void settingsGUISaveInterval()
        {
            string saveIntervalLabel = "Save Settings Every ".MemoizedConcat( (Mathf.Round( SaveInterval )).MemoizedToString().MemoizedConcat("s") );
            Action<float> updateSaveInterval = delegate (float f) { SaveInterval = f; };
            IMGUIExtensions.floatTextBoxAndSliderCombo( saveIntervalLabel, SaveInterval, saveIntervalMin, saveIntervalMax, updateSaveInterval );
        }

        bool settingsLoggingSeveritySelect = false;
        List<LogSeverity> lsList = Enum.GetValues( typeof( LogSeverity ) ).Cast<LogSeverity>().ToList();
        private void settingsGUILoggingLevel()
        {
            string s = "Logging: ".MemoizedConcat(Settings.Instance.LoggingLevel.MemoizedToString());

            if (!settingsLoggingSeveritySelect)
            {
                settingsLoggingSeveritySelect = GUILayout.Toggle( settingsLoggingSeveritySelect, s, "button" );
            }
            if (settingsLoggingSeveritySelect)
            {
                GUILayout.BeginHorizontal();
                {
                    for (int i = 0; i < lsList.Count; i++)
                    {
                        LogSeverity ls = lsList[i];
                        if (GUILayout.Button( ls.ToString() ))
                        {
                            Settings.Instance.LoggingLevel = ls;
                            settingsLoggingSeveritySelect = false;
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        private void settingsGUIKeyBinding()
        {
            GUILayout.Label( "Key Bindings:" );

            //Keys
            Color c = GUI.contentColor;
            foreach (TCKeyBinding kb in Settings.Instance.KeyBinds)
            {
                if (kb.IsKeyAssigned)
                    GUI.contentColor = Color.yellow;
                else
                    GUI.contentColor = c;

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
                    GUI.enabled = false;

                bool assignKey = GUILayout.Button( buttonDesc );
                if (assignKey)
                {
                    settingsGUIAssignKey( buttonDesc, kb );
                }

                GUI.enabled = true;
            }
            GUI.contentColor = c;
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

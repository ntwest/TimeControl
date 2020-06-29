using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using TimeControl.KeyBindings;

namespace TimeControl
{
    [KSPAddon( KSPAddon.Startup.Instantly, true )]
    public class GlobalSettings : MonoBehaviour
    {
        public class GUIWindowSettings
        {
            GameScenes scene;
            float x = 100;
            float y = 100;
            bool isDisplayed = false;

            private ConfigNode cn;

            public event EventHandler OnChanged;

            public GameScenes GS
            {
                get => scene;
            }

            public ConfigNode GetConfigNode()
            {
                return cn;
            }

            public GUIWindowSettings(GameScenes pGS, float pX, float pY, bool pIsDisplayed)
            {
                scene = pGS;                
                x = pX;
                y = pY;
                isDisplayed = pIsDisplayed;
                cn = new ConfigNode( pGS.ToString() );
                cn.SetValue( nameof( X ), x, true );
                cn.SetValue( nameof( Y ), y, true );
                cn.SetValue( nameof( IsDisplayed ), isDisplayed, true );
            }

            public float X
            {
                get => x;
                set
                {
                    x = value;
                    cn.SetValue( nameof( X ), value, true );
                    OnChanged?.Invoke( this, EventArgs.Empty );
                }
            }
            public float Y
            {
                get => y;
                set
                {
                    y = value;
                    cn.SetValue( nameof( Y ), value, true );                    
                    OnChanged?.Invoke( this, EventArgs.Empty );
                }
            }
            public bool IsDisplayed
            {
                get => isDisplayed;
                set
                {
                    isDisplayed = value;
                    cn.SetValue( nameof( IsDisplayed ), value, true );
                    OnChanged?.Invoke( this, EventArgs.Empty );
                }
            }

            public bool AssignFromConfigNode(ConfigNode pCN)
            {
                if (pCN.TryAssignFromConfigFloat( nameof( X ), out float tmpX )
                    && pCN.TryAssignFromConfigFloat( nameof( Y ), out float tmpY )
                    && pCN.TryAssignFromConfigBool( nameof( IsDisplayed ), out bool tmpIsDisplayed )
                    )
                {
                    x = tmpX;
                    y = tmpY;
                    isDisplayed = tmpIsDisplayed;
                    cn.SetValue( nameof( X ), x, true );
                    cn.SetValue( nameof( Y ), y, true );
                    cn.SetValue( nameof( IsDisplayed ), isDisplayed, true );

                    OnChanged?.Invoke( this, EventArgs.Empty );
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        #region Singleton
        public static bool IsReady { get; private set; } = false;
        public static GlobalSettings Instance { get; private set; }
        #endregion

        private EventData<float> OnTimeControlHyperWarpMaximumDeltaTimeChangedEvent { get; set; }
        private EventData<float> OnTimeControlHyperWarpMaxAttemptedRateChangedEvent { get; set; }
        private EventData<float> OnTimeControlHyperWarpPhysicsAccuracyChangedEvent { get; set; }
        private EventData<float> OnTimeControlSlowMoRateChangedEvent { get; set; }
        private EventData<TimeControlKeyBinding> OnTimeControlKeyBindingsChangedEvent;
        private EventData<bool> OnTimeControlSlowMoDeltaLockedChangedEvent { get; set; }

        private const float saveTimeDelta = 10f;
        private float saveTimeDelay = 0f;
        private bool saveOnNextUpdate = false;

        private const string loggingLevelNodeName = "LoggingLevel";
        private const string topNodeName = "GlobalSettings";

        private readonly string globalSettingsFilePath = string.Format( "{0}/GlobalSettings.cfg", PluginAssemblyUtilities.PathPluginData );

        private ConfigNode baseNode;
        private ConfigNode mainNode;

        public GUIWindowSettings SpaceCenterWindow;
        public GUIWindowSettings TrackStationWindow;
        public GUIWindowSettings FlightWindow;


        private List<TimeControlKeyBinding> activeKeyBinds;

        public List<TimeControlKeyBinding> GetActiveKeyBinds()
        {
            return activeKeyBinds.ToList();
        }

        public void SetActiveKeyBinds(List<TimeControlKeyBinding> value)
        {
            activeKeyBinds = value;
            RebuildKeybindsNode();
            saveOnNextUpdate = true;
        }

        private float hyperWarpMaximumDeltaTime = GameSettings.PHYSICS_FRAME_DT_LIMIT;
        public float HyperWarpMaximumDeltaTime
        {
            get => hyperWarpMaximumDeltaTime;
            set
            {
                if (hyperWarpMaximumDeltaTime != value)
                {
                    hyperWarpMaximumDeltaTime = value;
                    mainNode?.SetValue( nameof( HyperWarpMaximumDeltaTime ), value, true );
                    saveOnNextUpdate = true;
                }
            }
        }

        private float hyperWarpPhysicsAccuracy = 1f;
        public float HyperWarpPhysicsAccuracy
        {
            get => hyperWarpPhysicsAccuracy;
            set
            {
                if (hyperWarpPhysicsAccuracy != value)
                {
                    hyperWarpPhysicsAccuracy = value;
                    mainNode?.SetValue( nameof( HyperWarpPhysicsAccuracy ), value, true );
                    saveOnNextUpdate = true;
                }
            }
        }

        private float hyperWarpMaxAttemptedRate = 2f;
        public float HyperWarpMaxAttemptedRate
        {
            get => hyperWarpMaxAttemptedRate;
            set
            {
                if (hyperWarpMaxAttemptedRate != value)
                {
                    hyperWarpMaxAttemptedRate = value;
                    mainNode?.SetValue( nameof( HyperWarpMaxAttemptedRate ), value, true );
                    saveOnNextUpdate = true;
                }
            }
        }

        private float slowMoRate = 0.5f;
        public float SlowMoRate
        {
            get => slowMoRate;
            set
            {
                if (slowMoRate != value)
                {
                    slowMoRate = value;
                    mainNode?.SetValue( nameof( SlowMoRate ), value, true );
                    saveOnNextUpdate = true;
                }
            }
        }

        private bool deltaLocked = true;
        public bool DeltaLocked
        {
            get => deltaLocked;
            set
            {
                if (deltaLocked != value)
                {
                    deltaLocked = value;
                    mainNode?.SetValue( nameof( DeltaLocked ), value, true );
                    saveOnNextUpdate = true;
                }
            }
        }
        private float resetAltitudeToValue = 1000f;
        public float ResetAltitudeToValue
        {
            get => resetAltitudeToValue;
            set
            {
                if (resetAltitudeToValue != value)
                {
                    resetAltitudeToValue = value;
                    mainNode?.SetValue( nameof( ResetAltitudeToValue ), value, true );
                    saveOnNextUpdate = true;
                }
            }
        }
        
        // This is managed in the time control parameters screen, but is applied globally by the settings
        private bool cameraZoomFix = true;
        public bool CameraZoomFix
        {
            get => cameraZoomFix;
            set
            {
                if (cameraZoomFix != value)
                {
                    cameraZoomFix = value;
                    try
                    {
                        HighLogic.CurrentGame.Parameters.CustomParams<TimeControlParameterNode>().CameraZoomFix = value;
                    }
                    catch (NullReferenceException) { }
                    mainNode?.SetValue( nameof( CameraZoomFix ), value, true );
                    saveOnNextUpdate = true;
                }
            }
        }

        private int keyRepeatStart = 500;
        public int KeyRepeatStart
        {
            get => keyRepeatStart;
            set
            {
                if (keyRepeatStart != value)
                {
                    keyRepeatStart = value;
                    mainNode?.SetValue( nameof( KeyRepeatStart ), value, true );
                    saveOnNextUpdate = true;
                }
            }
        }
        private int keyRepeatInterval = 15;
        public int KeyRepeatInterval
        {
            get => keyRepeatInterval;
            set
            {
                if (keyRepeatInterval != value)
                {
                    keyRepeatInterval = value;
                    mainNode?.SetValue( nameof( KeyRepeatInterval ), value, true );
                    saveOnNextUpdate = true;
                }
            }
        }

        // This is managed in the time control parameters screen, but is applied globally by the settings
        private LogSeverity loggingLevel = LogSeverity.Warning;
        public LogSeverity LoggingLevel
        {
            get => loggingLevel;
            set
            {
                if (loggingLevel != value)
                {
                    loggingLevel = value;
                    Log.LoggingLevel = value;
                    try
                    {
                        HighLogic.CurrentGame.Parameters.CustomParams<TimeControlParameterNode>().LoggingLevel = value;
                    }
                    catch (NullReferenceException) { }
                    mainNode?.SetValue( nameof( LoggingLevel ), value.ToString(), true );
                    saveOnNextUpdate = true;
                }
            }
        }

        // This is managed in the time control parameters screen, applied globally by the game, and persisted in the standard KSP settings
        private bool useKerbinTime;
        public bool UseKerbinTime
        {
            get => useKerbinTime;
            set
            {
                if (useKerbinTime != value)
                {
                    useKerbinTime = value;
                    GameSettings.KERBIN_TIME = value;
                    try
                    {
                        HighLogic.CurrentGame.Parameters.CustomParams<TimeControlParameterNode>().UseKerbinTime = value;
                    }
                    catch (NullReferenceException) { }
                    //mainNode?.SetValue( nameof( KerbinTime ), value.ToString(), true );
                    //saveOnNextUpdate = true;
                }
            }
        }


        private string version = TimeControl.PluginAssemblyUtilities.VERSION;
        
        #region MonoBehavior
        private void Awake()
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( Awake );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                DontDestroyOnLoad( this );
                Instance = this;
            }
        }
        private void Start()
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( Start );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                baseNode = new ConfigNode( "TimeControl" );
                mainNode = baseNode.AddNode( topNodeName );
                mainNode.SetValue( "Version", version, true );
                
                SpaceCenterWindow = new GUIWindowSettings( GameScenes.SPACECENTER, 100, 100, false );                
                TrackStationWindow = new GUIWindowSettings( GameScenes.TRACKSTATION, 100, 100, false );
                FlightWindow = new GUIWindowSettings( GameScenes.FLIGHT, 100, 100, false );

                mainNode.AddNode( SpaceCenterWindow.GetConfigNode() );
                mainNode.AddNode( TrackStationWindow.GetConfigNode() );
                mainNode.AddNode( FlightWindow.GetConfigNode() );

                mainNode.SetValue( nameof( HyperWarpPhysicsAccuracy ), HyperWarpPhysicsAccuracy, true );
                mainNode.SetValue( nameof( HyperWarpMaxAttemptedRate ), HyperWarpMaxAttemptedRate, true );
                mainNode.SetValue( nameof( HyperWarpMaximumDeltaTime ), HyperWarpMaximumDeltaTime, true );
                mainNode.SetValue( nameof( SlowMoRate ), SlowMoRate, true );
                mainNode.SetValue( nameof( DeltaLocked ), DeltaLocked, true );
                mainNode.SetValue( nameof( ResetAltitudeToValue ), ResetAltitudeToValue, true );
                mainNode.SetValue( nameof( LoggingLevel ), LoggingLevel.ToString(), true );
                mainNode.SetValue( nameof( CameraZoomFix ), CameraZoomFix, true );
                mainNode.SetValue( nameof( KeyRepeatInterval ), KeyRepeatInterval, true );
                mainNode.SetValue( nameof( KeyRepeatStart ), KeyRepeatStart, true );

                InitKeybinds();

                //ResetSettingsToDefaults();

                LoadFromConfig();

                SubscribeToEvents();

                saveOnNextUpdate = true;
                Log.Info( nameof( GlobalSettings ) + " is Ready!", logBlockName );
                IsReady = true;
            }
        }
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        private void Update()
        {
            if (saveOnNextUpdate)
            {
                saveTimeDelay = saveTimeDelay + Time.deltaTime;
                if (saveTimeDelay > saveTimeDelta)
                {
                    saveTimeDelay = 0.0f;
                    SaveToFile();
                }
            }
        }
        private void SubscribeToEvents()
        {
            global::GameEvents.OnGameSettingsApplied.Add( this.OnGameSettingsApplied );
            global::GameEvents.onGameStateSaved.Add( this.onGameStateSaved );
            //global::GameEvents.onGameStatePostLoad.Add( this.onGameStatePostLoad );
            //global::GameEvents.onLevelWasLoaded.Add( this.onLevelWasLoaded );

            OnTimeControlKeyBindingsChangedEvent = GameEvents.FindEvent<EventData<TimeControlKeyBinding>>( nameof( TimeControlEvents.OnTimeControlKeyBindingsChanged ) );
            OnTimeControlKeyBindingsChangedEvent?.Add( OnTimeControlKeyBindingsChanged );

            OnTimeControlHyperWarpMaxAttemptedRateChangedEvent = GameEvents.FindEvent<EventData<float>>( nameof( TimeControlEvents.OnTimeControlHyperWarpMaxAttemptedRateChanged ) );
            OnTimeControlHyperWarpMaxAttemptedRateChangedEvent?.Add( OnTimeControlHyperWarpMaxAttemptedRateChanged );

            OnTimeControlHyperWarpPhysicsAccuracyChangedEvent = GameEvents.FindEvent<EventData<float>>( nameof( TimeControlEvents.OnTimeControlHyperWarpPhysicsAccuracyChanged ) );
            OnTimeControlHyperWarpPhysicsAccuracyChangedEvent?.Add( OnTimeControlHyperWarpPhysicsAccuracyChanged );

            OnTimeControlHyperWarpMaximumDeltaTimeChangedEvent = GameEvents.FindEvent<EventData<float>>( nameof( TimeControlEvents.OnTimeControlHyperWarpMaximumDeltaTimeChanged ) );
            OnTimeControlHyperWarpMaximumDeltaTimeChangedEvent?.Add( OnTimeControlHyperWarpMaximumDeltaTimeChanged );

            OnTimeControlSlowMoRateChangedEvent = GameEvents.FindEvent<EventData<float>>( nameof( TimeControlEvents.OnTimeControlSlowMoRateChanged ) );
            OnTimeControlSlowMoRateChangedEvent?.Add( OnTimeControlSlowMoRateChanged );

            OnTimeControlSlowMoDeltaLockedChangedEvent = GameEvents.FindEvent<EventData<bool>>( nameof( TimeControlEvents.OnTimeControlSlowMoDeltaLockedChanged ) );
            OnTimeControlSlowMoDeltaLockedChangedEvent?.Add( OnTimeControlSlowMoDeltaLockedChanged );

            SpaceCenterWindow.OnChanged += GUIWindowsChanged;
            TrackStationWindow.OnChanged += GUIWindowsChanged;
            FlightWindow.OnChanged += GUIWindowsChanged;
        }
        private void UnsubscribeFromEvents()
        {
            global::GameEvents.OnGameSettingsApplied.Remove( this.OnGameSettingsApplied );
            global::GameEvents.onGameStateSaved.Remove( this.onGameStateSaved );
            //global::GameEvents.onGameStatePostLoad.Remove( this.onGameStatePostLoad );
            //global::GameEvents.onLevelWasLoaded.Remove( this.onLevelWasLoaded );

            OnTimeControlKeyBindingsChangedEvent?.Remove( OnTimeControlKeyBindingsChanged );
            OnTimeControlHyperWarpMaxAttemptedRateChangedEvent?.Remove( OnTimeControlHyperWarpMaxAttemptedRateChanged );
            OnTimeControlHyperWarpPhysicsAccuracyChangedEvent?.Remove( OnTimeControlHyperWarpPhysicsAccuracyChanged );
            OnTimeControlHyperWarpMaximumDeltaTimeChangedEvent?.Remove( OnTimeControlHyperWarpMaximumDeltaTimeChanged );
            OnTimeControlSlowMoRateChangedEvent?.Remove( OnTimeControlSlowMoRateChanged );

            SpaceCenterWindow.OnChanged -= GUIWindowsChanged;
            TrackStationWindow.OnChanged -= GUIWindowsChanged;
            FlightWindow.OnChanged -= GUIWindowsChanged;
        }
        #endregion

        #region GameEvents
        //private void onLevelWasLoaded(GameScenes gs)
        //{
        //    const string logBlockName = nameof( GlobalSettings ) + "." + nameof( OnGameSettingsApplied );
        //    using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
        //    {
        //        Load();
        //    }
        //}

        //private void onGameStatePostLoad(ConfigNode node)
        //{
        //    const string logBlockName = nameof( GlobalSettings ) + "." + nameof( onGameStatePostLoad );
        //    using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
        //    {
        //        Load();
        //    }
        //}

        private void OnGameSettingsApplied()
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( OnGameSettingsApplied );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                TimeControlParameterNode TCPN = null;
                try
                {
                    TCPN = HighLogic.CurrentGame.Parameters.CustomParams<TimeControlParameterNode>();
                }
                catch (NullReferenceException) { }
                if (TCPN != null)
                {
                    CameraZoomFix = TCPN.CameraZoomFix;
                    LoggingLevel = TCPN.LoggingLevel;
                    UseKerbinTime = TCPN.UseKerbinTime;
                    KeyRepeatStart = TCPN.KeyRepeatStart;
                    KeyRepeatInterval = TCPN.KeyRepeatInterval;
                    saveOnNextUpdate = true;
                }
            }
        }

        private void onGameStateSaved(Game data)
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( onGameStateSaved );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                saveOnNextUpdate = true;
            }
        }

        private void OnTimeControlHyperWarpPhysicsAccuracyChanged(float data)
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( OnTimeControlHyperWarpPhysicsAccuracyChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (HyperWarpController.IsReady)
                {
                    HyperWarpPhysicsAccuracy = HyperWarpController.Instance.PhysicsAccuracy;
                }
            }
        }

        private void OnTimeControlHyperWarpMaxAttemptedRateChanged(float data)
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( OnTimeControlHyperWarpMaxAttemptedRateChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (HyperWarpController.IsReady)
                {
                    HyperWarpMaxAttemptedRate = HyperWarpController.Instance.MaxAttemptedRate;
                }
            }
        }

        private void OnTimeControlHyperWarpMaximumDeltaTimeChanged(float data)
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( OnTimeControlHyperWarpMaximumDeltaTimeChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (HyperWarpController.IsReady)
                {
                    HyperWarpMaximumDeltaTime = HyperWarpController.Instance.MaximumDeltaTime;
                }
            }
        }

        private void OnTimeControlSlowMoRateChanged(float data)
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( OnTimeControlSlowMoRateChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (SlowMoController.IsReady)
                {
                    SlowMoRate = SlowMoController.Instance.SlowMoRate;
                }
            }
        }

        private void OnTimeControlSlowMoDeltaLockedChanged(bool data)
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( OnTimeControlSlowMoDeltaLockedChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (SlowMoController.IsReady)
                {
                    DeltaLocked = SlowMoController.Instance.DeltaLocked;
                }
            }
        }
        
        private void OnTimeControlKeyBindingsChanged(TimeControlKeyBinding tckb)
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( OnTimeControlKeyBindingsChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                SetActiveKeyBinds( KeyboardInputManager.Instance.GetActiveKeyBinds() );
            }
        }
        #endregion GameEvents

        #region Save and Load
        internal void Save()
        {
            saveOnNextUpdate = true;
        }

        private void SaveToFile()
        {
            this.SaveToFile( false );
        }

        /// <summary>
        /// Save global time control settings to file
        /// </summary>
        private void SaveToFile(bool force)
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( SaveToFile );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                try
                {
                    if (!GlobalSettings.IsReady && !force)
                    {
                        Log.Warning( "Global Settings Not Ready", logBlockName );
                        return;
                    }

                    if (baseNode == null)
                    {
                        Log.Error( "Cannot save global settings, configs are NULL!!", logBlockName );
                        return;
                    }

                    if (!System.IO.Directory.Exists( PluginAssemblyUtilities.PathPluginData ))
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory( PluginAssemblyUtilities.PathPluginData );
                        }
                        catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException)
                        {
                            Log.Error( "Unable to create directory " + PluginAssemblyUtilities.PathPluginData, logBlockName );
                            return;
                        }
                    }

                    if (System.IO.File.Exists( globalSettingsFilePath ))
                    {        
                        try
                        {
                            System.IO.File.Delete( globalSettingsFilePath );
                        }
                        catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException)
                        {
                            Log.Error( "Unable to save settings file at this time! File exists and cannot delete it.", logBlockName );
                            return;
                        }
                    }

                    bool results = baseNode.Save( globalSettingsFilePath );
                    if (!results)
                    {
                        Log.Error( "Settings file save failed!", logBlockName );
                        return;
                    }

                    Log.Info( "Global Settings Saved to File ".MemoizedConcat( globalSettingsFilePath ), logBlockName );
                    saveOnNextUpdate = false;

                    TimeControlEvents.OnTimeControlGlobalSettingsSaved.Fire( true );
                }
                catch (Exception e)
                {
                    Log.Error( "SERIOUS ERROR, Please report in the KSP forums.", logBlockName );
                    Log.Error( e.Message );
                    Log.Error( e.StackTrace );
                }
            }
        }

        private ConfigNode ResetFileAndGetConfig()
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( ResetFileAndGetConfig );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                SaveToFile(true);
                ConfigNode tmpConfigBase = ConfigNode.Load( globalSettingsFilePath );
                if (tmpConfigBase == null)
                {
                    const string message = "Serious Error. Please review your output.log and if necessary report this error on the KSP Forum. Cannot load global settings file even after trying to create it. Failing.";
                    Log.PopupError( message );
                    Log.Error( message, logBlockName, true );

                    throw new InvalidOperationException( message );
                }
                return tmpConfigBase;
            }
        }

        /// <summary>
        /// Load global time control settings from file and apply to objects
        /// </summary>
        private void LoadFromConfig()
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( LoadFromConfig );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Loading Global Settings File", logBlockName, true );

                ConfigNode tmpConfigBase = ConfigNode.Load( globalSettingsFilePath );
                if (tmpConfigBase == null)
                {
                    tmpConfigBase = ResetFileAndGetConfig();
                }
                
                ConfigNode tmpConfigMain;
                if (!tmpConfigBase.HasNode( topNodeName ))
                {
                    string message = "No top level node found in config. Default settings will be used instead.";
                    Log.Error( message, logBlockName );
                    return;
                }
                tmpConfigMain = tmpConfigBase.GetNode( topNodeName );

                ////////////////////////////////////////////////
                // Check Version
                ////////////////////////////////////////////////
                if (tmpConfigMain.HasValue("Version"))
                {
                    // Placeholder for code to upgrade versions if necessary
                }
                else
                {
                    string message = "Old version of settings file detected. Resetting and using defaults.";
                    Log.Error( message, logBlockName );
                    return;
                }

                ////////////////////////////////////////////////
                // HyperWarpMaxAttemptedRate
                ////////////////////////////////////////////////
                if (tmpConfigMain.TryAssignFromConfigFloat( nameof( HyperWarpMaxAttemptedRate ), out float tmpHyperWarpMaxAttemptedRate ))
                {
                    HyperWarpMaxAttemptedRate = tmpHyperWarpMaxAttemptedRate;
                }
                else
                {
                    Log.Warning( nameof( HyperWarpMaxAttemptedRate ) + " has error in configuration file. Using default.", logBlockName );
                }

                ////////////////////////////////////////////////
                // HyperWarpPhysicsAccuracy
                ////////////////////////////////////////////////
                if (tmpConfigMain.TryAssignFromConfigFloat( nameof( HyperWarpPhysicsAccuracy ), out float tmpHyperWarpPhysicsAccuracy ))
                {
                    HyperWarpPhysicsAccuracy = tmpHyperWarpPhysicsAccuracy;
                }
                else
                {
                    Log.Warning( nameof( HyperWarpPhysicsAccuracy ) + " has error in configuration file. Using default.", logBlockName );
                }

                ////////////////////////////////////////////////
                // HyperWarpPhysicsAccuracy
                ////////////////////////////////////////////////
                if (tmpConfigMain.TryAssignFromConfigFloat( nameof( HyperWarpMaximumDeltaTime ), out float tmpHyperWarpMaximumDeltaTime ))
                {
                    HyperWarpMaximumDeltaTime = tmpHyperWarpMaximumDeltaTime;
                }
                else
                {
                    Log.Warning( nameof( HyperWarpMaximumDeltaTime ) + " has error in configuration file. Using default.", logBlockName );
                }

                ////////////////////////////////////////////////
                // SlowMoRate
                ////////////////////////////////////////////////
                if (tmpConfigMain.TryAssignFromConfigFloat( nameof( SlowMoRate ), out float tmpSlowMoRate ))
                {
                    SlowMoRate = tmpSlowMoRate;
                }
                else
                {
                    Log.Warning( nameof( SlowMoRate ) + " has error in configuration file. Using default.", logBlockName );
                }

                ////////////////////////////////////////////////
                // DeltaLocked
                ////////////////////////////////////////////////
                if (tmpConfigMain.TryAssignFromConfigBool( nameof( DeltaLocked ), out bool tmpDeltaLocked ))
                {
                    DeltaLocked = tmpDeltaLocked;
                }
                else
                {
                    Log.Warning( nameof( DeltaLocked ) + " has error in configuration file. Using default.", logBlockName );
                }
                
                ////////////////////////////////////////////////
                // ResetAltitudeToValue
                ////////////////////////////////////////////////
                if (tmpConfigMain.TryAssignFromConfigFloat( nameof( ResetAltitudeToValue ), out float tmpResetAltitudeToValue ))
                {
                    ResetAltitudeToValue = tmpResetAltitudeToValue;
                }
                else
                {
                    Log.Warning( nameof( ResetAltitudeToValue ) + " has error in configuration file. Using default.", logBlockName );
                }

                ////////////////////////////////////////////////
                // LoggingLevel
                ////////////////////////////////////////////////
                if (tmpConfigMain.TryAssignFromConfigEnum( nameof( LoggingLevel ), out LogSeverity tmpLoggingLevel ))
                {
                    LoggingLevel = tmpLoggingLevel;
                }
                else
                {
                    Log.Warning( nameof( LoggingLevel ) + " has error in configuration file. Using default.", logBlockName );
                }

                ////////////////////////////////////////////////
                // CameraZoomFix
                ////////////////////////////////////////////////
                if (tmpConfigMain.TryAssignFromConfigBool( nameof( CameraZoomFix ), out bool tmpCameraZoomFix ))
                {
                    CameraZoomFix = tmpCameraZoomFix;
                }
                else
                {
                    Log.Warning( nameof( CameraZoomFix ) + " has error in configuration file. Using default.", logBlockName );
                }

                ////////////////////////////////////////////////
                // KeyRepeatInterval
                ////////////////////////////////////////////////
                if (tmpConfigMain.TryAssignFromConfigInt( nameof( KeyRepeatInterval ), out int tmpKeyRepeatInterval ))
                {
                    KeyRepeatInterval = tmpKeyRepeatInterval;
                }
                else
                {
                    Log.Warning( nameof( KeyRepeatInterval ) + " has error in configuration file. Using default.", logBlockName );
                }

                ////////////////////////////////////////////////
                // KeyRepeatStart
                ////////////////////////////////////////////////
                if (tmpConfigMain.TryAssignFromConfigInt( nameof( KeyRepeatStart ), out int tmpKeyRepeatStart ))
                {
                    KeyRepeatStart = tmpKeyRepeatStart;
                }
                else
                {
                    Log.Warning( nameof( KeyRepeatStart ) + " has error in configuration file. Using default.", logBlockName );
                }

                ////////////////////////////////////////////////
                // SpaceCenterWindow
                ////////////////////////////////////////////////
                string spaceCenterWindowNode = this.SpaceCenterWindow.GS.ToString();
                if (tmpConfigMain.HasNode( spaceCenterWindowNode ))
                {
                    if (!this.SpaceCenterWindow.AssignFromConfigNode( tmpConfigMain.GetNode( spaceCenterWindowNode ) ))
                    {
                        Log.Warning( spaceCenterWindowNode + " has error in configuration file. Using default.", logBlockName );
                    }
                }
                else
                {
                    Log.Warning( spaceCenterWindowNode + " does not exist in configuration file. Using default.", logBlockName );
                }

                ////////////////////////////////////////////////
                // TrackStationWindow
                ////////////////////////////////////////////////
                string trackStationWindowNode = this.TrackStationWindow.GS.ToString();
                if (tmpConfigMain.HasNode( trackStationWindowNode ))
                {
                    if (!this.TrackStationWindow.AssignFromConfigNode( tmpConfigMain.GetNode( trackStationWindowNode ) ))
                    {
                        Log.Warning( trackStationWindowNode + " has error in configuration file. Using default.", logBlockName );
                    }
                }
                else
                {
                    Log.Warning( trackStationWindowNode + " does not exist in configuration file. Using default.", logBlockName );
                }

                ////////////////////////////////////////////////
                // FlightWindow
                ////////////////////////////////////////////////
                string flightWindowNode = this.FlightWindow.GS.ToString();
                if (tmpConfigMain.HasNode( flightWindowNode ))
                {
                    if (!this.FlightWindow.AssignFromConfigNode( tmpConfigMain.GetNode( flightWindowNode ) ))
                    {
                        Log.Warning( flightWindowNode + " has error in configuration file. Using default.", logBlockName );
                    }
                }
                else
                {
                    Log.Warning( flightWindowNode + " does not exist in configuration file. Using default.", logBlockName );
                }

                ConfigLoadKeyBinds( tmpConfigMain );
                RebuildKeybindsNode();

                Log.Info( "Key Binds Loaded", logBlockName );

                TimeControlEvents.OnTimeControlGlobalSettingsChanged.Fire( true );
                
                SaveToFile(true);
            }
        }

        #endregion

        #region Private
        //private void ResetSettingsToDefaults()
        //{
        //    const string logBlockName = nameof( GlobalSettings ) + "." + nameof( ResetSettingsToDefaults );
        //    using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
        //    {
        //        //ResetWindowLocationSettings();
        //        ResetKeybindsToDefaultSettings();
        //    }
        //}

        //private void ResetWindowLocationSettings()
        //{
        //    const string logBlockName = nameof( GlobalSettings ) + "." + nameof( ResetWindowLocationSettings );
        //    using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
        //    {
        //        if (SpaceCenterWindow != null)
        //        {
        //            SpaceCenterWindow.OnChanged -= GUIWindowsChanged;
        //            SpaceCenterWindow.X = 100;
        //            SpaceCenterWindow.Y = 100;
        //            SpaceCenterWindow.IsDisplayed = false;
        //            SpaceCenterWindow.OnChanged += GUIWindowsChanged;
        //        }
        //        if (TrackStationWindow != null)
        //        {
        //            TrackStationWindow.OnChanged -= GUIWindowsChanged;
        //            TrackStationWindow.X = 100;
        //            TrackStationWindow.Y = 100;
        //            TrackStationWindow.IsDisplayed = false;
        //            TrackStationWindow.OnChanged += GUIWindowsChanged;
        //        }               
        //        if (FlightWindow != null)
        //        {
        //            FlightWindow.OnChanged -= GUIWindowsChanged;
        //            FlightWindow.X = 100;
        //            FlightWindow.Y = 100;
        //            FlightWindow.IsDisplayed = false;
        //            FlightWindow.OnChanged += GUIWindowsChanged;
        //        }
        //        saveOnNextUpdate = true;
        //    }
        //}

        private void GUIWindowsChanged(object sender, EventArgs e)
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( GUIWindowsChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                saveOnNextUpdate = true;
            }
        }

        private void InitKeybinds()
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( InitKeybinds );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                activeKeyBinds = activeKeyBinds ?? new List<TimeControlKeyBinding>();
                activeKeyBinds.Clear();
                activeKeyBinds.Add( new GUIToggle() );
                activeKeyBinds.Add( new Realtime() );
                activeKeyBinds.Add( new PauseToggle() );
                activeKeyBinds.Add( new TimeStep() );
                activeKeyBinds.Add( new HyperToggle() );
                activeKeyBinds.Add( new SlowMoToggle() );
                activeKeyBinds.Add( new WarpToNextKACAlarm() );
                activeKeyBinds.Add( new WarpForNTimeIncrements(WarpForNTimeIncrements.TimeIncrement.Seconds) { V = 30f } );
                activeKeyBinds.Add( new HyperRateSlowDown() { V = 1f } );
                activeKeyBinds.Add( new HyperRateSpeedUp() { V = 1f } );
                activeKeyBinds.Add( new HyperRateChangeToLowerRate() );
                activeKeyBinds.Add( new HyperRateChangeToHigherRate() );
                activeKeyBinds.Add( new HyperPhysicsAccuracyDown() { V = 0.5f } );
                activeKeyBinds.Add( new HyperPhysicsAccuracyUp() { V = 0.5f } );
                activeKeyBinds.Add( new SlowMoSlowDown() { V = 0.05f } );
                activeKeyBinds.Add( new SlowMoSpeedUp() { V = 0.05f } );                
                RebuildKeybindsNode();
            }
        }

        public void ResetKeybindsToDefaultSettings()
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( ResetKeybindsToDefaultSettings );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                InitKeybinds();
                saveOnNextUpdate = true;
            }
        }

        private void RebuildKeybindsNode()
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( RebuildKeybindsNode );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                const string kbsNodeName = "KeyBinds";
                if (mainNode.HasNode( kbsNodeName ))
                {
                    mainNode.RemoveNode( kbsNodeName );
                }
                ConfigNode kbNode = mainNode.AddNode( kbsNodeName );
                foreach (TimeControlKeyBinding k in activeKeyBinds)
                {
                    kbNode.AddNode( k.GetConfigNode() );
                }
            }
        }

        public void ConfigLoadKeyBinds(ConfigNode cn)
        {
            const string logBlockName = nameof( KeyboardInputManager ) + "." + nameof( ConfigLoadKeyBinds );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                string kb = "KeyBinds";
                ConfigNode keyBindsNode = new ConfigNode();

                if (!cn.TryGetNode( kb, ref keyBindsNode ))
                {
                    string message = "No KeyBinds node found in config. This error is not fatal to the load process. Default keybinds will be used instead.";
                    Log.Warning( message, logBlockName );
                    return;
                }

                List<ConfigNode> lcn = keyBindsNode.GetNodes( "KeyBind" ).ToList();

                for (int i = 0; i < lcn.Count; i++)
                {
                    var tckb = TimeControlKeyBindingFactory.LoadFromConfigNode( lcn[i] );
                    if (tckb is null)
                    {
                        Log.Error( "Unable to parse a keybind Config Node. Skipping.", logBlockName );
                        continue;
                    }

                    Log.Trace( "Parsing key binding node."
                        .MemoizedConcat( " TimeControlKeyActionName = " )
                        .MemoizedConcat( tckb.TimeControlKeyActionName.MemoizedToString()
                        .MemoizedConcat( " ID = " )
                        .MemoizedConcat( tckb.ID.MemoizedToString() )
                        ), logBlockName );

                    var keys = activeKeyBinds.Where( k => k.TimeControlKeyActionName == tckb.TimeControlKeyActionName && k.ID == tckb.ID );
                    if (keys.Count() == 0)
                    {
                        activeKeyBinds.Add( tckb );
                    }
                    else
                    {
                        TimeControlKeyBinding tckbOrig = keys.First();
                        tckbOrig.KeyCombination = tckb.KeyCombination?.ToList() ?? new List<KeyCode>();
                    }

                    //if (tckb is WarpToVesselOrbitLocation wtvol)
                    //{
                    //    Log.Trace( "Parsing key binding node."
                    //        .MemoizedConcat( " TimeControlKeyActionName = " )
                    //        .MemoizedConcat( wtvol.TimeControlKeyActionName.MemoizedToString() )
                    //        .MemoizedConcat( " ID= " )
                    //        .MemoizedConcat( wtvol.TimeControlKeyActionName.MemoizedToString() )
                    //        .MemoizedConcat( " IsUserDefined = " )
                    //        .MemoizedConcat( wtvol.IsUserDefined.MemoizedToString() )
                    //        .MemoizedConcat( " V = " )
                    //        .MemoizedConcat( wtvol.V.MemoizedToString() )
                    //        .MemoizedConcat( " VesselLocation = ")
                    //        .MemoizedConcat( wtvol.VesselLocation.MemoizedToString() ), logBlockName );

                    //    var keys = activeKeyBinds
                    //        .Where( k => k is TimeControlKeyBindingValue && k.TimeControlKeyActionName == wtvol.TimeControlKeyActionName && k.IsUserDefined == wtvol.IsUserDefined)
                    //        .Select( k => (TimeControlKeyBindingValue)k )
                    //        .Where( k => k.V == wtvol.V )
                    //        .Select( k => (WarpToVesselOrbitLocation)k )
                    //        .Where( k => k.VesselLocation == wtvol.VesselLocation );

                    //if (keys.Count() == 0)
                    //    {
                    //        activeKeyBinds.Add( tckb );
                    //    }
                    //    else
                    //    {
                    //        TimeControlKeyBinding tckbOrig = keys.First();
                    //        tckbOrig.KeyCombination = tckb.KeyCombination?.ToList() ?? new List<KeyCode>();
                    //    }
                    //}
                    //else if (tckb is WarpForNTimeIncrements wfnti)
                    //{
                    //    Log.Trace( "Parsing key binding node."
                    //        .MemoizedConcat( " TimeControlKeyActionName = " )
                    //        .MemoizedConcat( wfnti.TimeControlKeyActionName.MemoizedToString() )
                    //        .MemoizedConcat( " IsUserDefined = " )
                    //        .MemoizedConcat( wfnti.IsUserDefined.MemoizedToString() )
                    //        .MemoizedConcat( " V = " )
                    //        .MemoizedConcat( wfnti.V.MemoizedToString() )
                    //        .MemoizedConcat( " TI = " )
                    //        .MemoizedConcat( wfnti.TI.MemoizedToString() ), logBlockName );

                    //    var keys = activeKeyBinds
                    //        .Where( k => k is TimeControlKeyBindingValue && k.TimeControlKeyActionName == wfnti.TimeControlKeyActionName && k.IsUserDefined == wfnti.IsUserDefined )
                    //        .Select( k => (TimeControlKeyBindingValue)k )
                    //        .Where( k => k.V == wfnti.V )
                    //        .Select( k => (WarpForNTimeIncrements)k )
                    //        .Where( k => k.TI == wfnti.TI );

                    //    if (keys.Count() == 0)
                    //    {
                    //        activeKeyBinds.Add( tckb );
                    //    }
                    //    else
                    //    {
                    //        TimeControlKeyBinding tckbOrig = keys.First();
                    //        tckbOrig.KeyCombination = tckb.KeyCombination?.ToList() ?? new List<KeyCode>();
                    //    }
                    //}
                    //else if (tckb is TimeControlKeyBindingValue tckbv)
                    //{
                    //    Log.Trace( "Parsing key binding node."
                    //        .MemoizedConcat( " TimeControlKeyActionName = " )
                    //        .MemoizedConcat( tckbv.TimeControlKeyActionName.MemoizedToString() )
                    //        .MemoizedConcat( " IsUserDefined = " )
                    //        .MemoizedConcat( tckbv.IsUserDefined.MemoizedToString() )
                    //        .MemoizedConcat( " V = " )
                    //        .MemoizedConcat( tckbv.V.MemoizedToString() ), logBlockName );

                    //    var keys = activeKeyBinds
                    //        .Where( k => k is TimeControlKeyBindingValue && k.TimeControlKeyActionName == tckbv.TimeControlKeyActionName && k.IsUserDefined == tckbv.IsUserDefined )
                    //        .Select( k => (TimeControlKeyBindingValue)k )
                    //        .Where( k => k.V == tckbv.V );
                    //    if (keys.Count() == 0)
                    //    {
                    //        activeKeyBinds.Add( tckb );
                    //    }
                    //    else
                    //    {
                    //        TimeControlKeyBindingValue tckbvOrig = keys.First();
                    //        tckbvOrig.KeyCombination = tckb.KeyCombination?.ToList() ?? new List<KeyCode>();
                    //    }
                    //}
                    //else
                    //{

                    //}
                }
            }
        }

        #endregion Private
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

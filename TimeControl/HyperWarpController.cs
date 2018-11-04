using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TimeControl
{
    [KSPAddon( KSPAddon.Startup.Instantly, true )]
    internal class HyperWarpController : MonoBehaviour
    {
        #region Singleton
        public static HyperWarpController Instance { get; private set; }
        public static bool IsReady { get; private set; } = false;
        //private static ConfigNode settingsCN;
        //public static ConfigNode SettingsCN
        //{
        //    get => settingsCN;
        //    set
        //    {
        //        settingsCN = value;
        //        if (Instance != null && IsReady)
        //        {
        //            Instance.LoadInitialValues( settingsCN );
        //        }
        //    }
        //}
        #endregion

        static public float PhysicsAccuracyMin
        {
            get => 1f;
        }
        static public float PhysicsAccuracyMax
        {
            get => 10f;
        }
        static public float AttemptedRateMin
        {
            get => 1f;
        }
        static public float AttemptedRateMax
        {
            get => 100f;
        }

        #region Private Fields

        private EventData<float> OnTimeControlHyperWarpMaximumDeltaTimeChangedEvent;
        private EventData<float> OnTimeControlHyperWarpMaxAttemptedRateChangedEvent;
        private EventData<float> OnTimeControlHyperWarpPhysicsAccuracyChangedEvent;
        private EventData<float> OnTimeControlDefaultFixedDeltaTimeChangedEvent;
        private EventData<bool> OnTimeControlTimePausedEvent;
        private EventData<bool> OnTimeControlTimeUnpausedEvent;

        private double hyperWarpingToUT = Mathf.Infinity;
        private bool isHyperWarpingToUT = false;
        
        
        //private bool isHyperWarpPaused = false;

        private ScreenMessage currentScreenMessage;
        private ScreenMessage defaultScreenMessage;
        private ScreenMessageStyle currentScreenMessageStyle = ScreenMessageStyle.UPPER_CENTER;
        private float currentScreenMessageDuration = Mathf.Infinity;
        private string currentScreenMessagePrefix = "HYPER-WARP";
        
        private List<ScreenMessage> HyperWarpMessagesCache = new List<ScreenMessage>();

        private FlightCamera cam;
        private bool isGamePaused;
        private bool needToUpdateTimeScale = true;

        #endregion

        #region Properties
        private double CurrentUT
        {
            get => Planetarium.GetUniversalTime();
        }

        private bool ShowOnscreenMessages
        {
            get => HighLogic.CurrentGame?.Parameters?.CustomParams<TimeControlParameterNode>()?.ShowHyperOnscreenMessages ?? true;
        }

        private bool PerformanceCountersOn
        {
            get => PerformanceManager.IsReady && (PerformanceManager.Instance?.PerformanceCountersOn ?? false);
        }

        private bool CurrentScreenMessageOn
        {
            get => currentScreenMessage != null;
        }

        private float DefaultFixedDeltaTime
        {
            get => TimeController.Instance?.DefaultFixedDeltaTime ?? 0.02f;
        }

        private double PhysicsTimeRatio
        {
            get => PerformanceManager.Instance?.PhysicsTimeRatio ?? 0.0f;
        }

        public bool CanHyperWarp
        {
            get => (TimeWarp.fetch != null && TimeWarp.CurrentRateIndex == 0 && (Mathf.Approximately( Time.timeScale, 1f ) || IsHyperWarping) && HighLogic.LoadedSceneIsFlight);
        }

        private bool hyperPauseOnTimeReached = false;
        public bool HyperPauseOnTimeReached
        {
            get => this.hyperPauseOnTimeReached;
            set
            {
                if (this.hyperPauseOnTimeReached != value)
                {
                    this.hyperPauseOnTimeReached = value;
                }
            }
        }

        private float maximumDeltaTime = GameSettings.PHYSICS_FRAME_DT_LIMIT;
        public float MaximumDeltaTime
        {
            get => this.maximumDeltaTime;
            set
            {
                if (!Mathf.Approximately( this.maximumDeltaTime, value ))
                {
                    this.maximumDeltaTime = Mathf.Clamp( value, TimeController.MaximumDeltaTimeMin, TimeController.MaximumDeltaTimeMax );
                    TimeControlEvents.OnTimeControlHyperWarpMaximumDeltaTimeChanged?.Fire( this.maximumDeltaTime );
                }
            }
        }

        private float physicsAccuracy = 1f;
        public float PhysicsAccuracy
        {
            get => this.physicsAccuracy;
            set
            {
                if (!Mathf.Approximately(this.physicsAccuracy, value))
                {
                    this.physicsAccuracy = Mathf.Clamp( value, PhysicsAccuracyMin, PhysicsAccuracyMax ); ;
                    TimeControlEvents.OnTimeControlHyperWarpPhysicsAccuracyChanged?.Fire( this.physicsAccuracy );
                }
            }
        }

        private float maxAttemptedRate = 2f;
        public float MaxAttemptedRate
        {
            get => this.maxAttemptedRate;
            set
            {
                if (!Mathf.Approximately( this.maxAttemptedRate, value ))
                {
                    this.maxAttemptedRate = Mathf.Clamp( value, AttemptedRateMin, AttemptedRateMax );
                    TimeControlEvents.OnTimeControlHyperWarpMaxAttemptedRateChanged?.Fire( this.maxAttemptedRate );
                }
            }
        }
        
        private bool isHyperWarping = false;
        public bool IsHyperWarping
        {
            get => isHyperWarping;
            private set
            {
                if (isHyperWarping != value)
                {
                    isHyperWarping = value;                    
                }
            }
        }

        public bool IsHyperWarpingToUT
        {
            get => isHyperWarpingToUT;
            private set
            {
                if (isHyperWarpingToUT != value)
                {
                    isHyperWarpingToUT = value;
                }
            }
        }

        public double HyperWarpingToUT
        {
            get
            {
                if (!IsHyperWarpingToUT)
                {
                    return Mathf.Infinity;
                }
                else
                {
                    return hyperWarpingToUT;
                }
            }
            private set
            {
                if (hyperWarpingToUT != value)
                {
                    hyperWarpingToUT = value;
                }
            }
        }

        public ScreenMessageStyle CurrentScreenMessageStyle
        {
            get => currentScreenMessageStyle;
            set
            {
                if (currentScreenMessageStyle != value)
                {
                    currentScreenMessageStyle = value;
                    UpdateDefaultScreenMessage();
                }
            }
        }

        public float CurrentScreenMessageDuration
        {
            get => currentScreenMessageDuration;
            set
            {
                if (currentScreenMessageDuration != value)
                {
                    currentScreenMessageDuration = value;
                    UpdateDefaultScreenMessage();
                }
            }
        }

        public string CurrentScreenMessagePrefix
        {
            get => currentScreenMessagePrefix;
            set
            {
                if (currentScreenMessagePrefix != value)
                {
                    currentScreenMessagePrefix = value;
                    UpdateDefaultScreenMessage();
                }
            }
        }

        #endregion Properties

        #region MonoBehavior

        private void Awake()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( Awake );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                DontDestroyOnLoad( this );
                Instance = this;
            }
        }

        private void Start()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( Start );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                StartCoroutine( Configure() );
            }
        }

        private void OnDestroy()
        {
            OnTimeControlDefaultFixedDeltaTimeChangedEvent?.Remove( OnTimeControlDefaultFixedDeltaTimeChanged );
            OnTimeControlHyperWarpMaxAttemptedRateChangedEvent?.Remove( OnTimeControlHyperWarpMaxAttemptedRateChanged );
            OnTimeControlHyperWarpPhysicsAccuracyChangedEvent?.Remove( OnTimeControlHyperWarpPhysicsAccuracyChanged );
            OnTimeControlHyperWarpMaximumDeltaTimeChangedEvent?.Remove( OnTimeControlHyperWarpMaximumDeltaTimeChanged );
        }

        private IEnumerator Configure()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( Configure );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                while (!GlobalSettings.IsReady || !IsValidScene() || TimeWarp.fetch == null || FlightGlobals.Bodies == null || FlightGlobals.Bodies.Count <= 0)
                {
                    yield return new WaitForSeconds( 1f );
                }

                OnTimeControlDefaultFixedDeltaTimeChangedEvent = GameEvents.FindEvent<EventData<float>>( nameof( TimeControlEvents.OnTimeControlDefaultFixedDeltaTimeChanged ) );
                OnTimeControlDefaultFixedDeltaTimeChangedEvent?.Add( OnTimeControlDefaultFixedDeltaTimeChanged );

                OnTimeControlHyperWarpMaxAttemptedRateChangedEvent = GameEvents.FindEvent<EventData<float>>( nameof( TimeControlEvents.OnTimeControlHyperWarpMaxAttemptedRateChanged ) );
                OnTimeControlHyperWarpMaxAttemptedRateChangedEvent?.Add( OnTimeControlHyperWarpMaxAttemptedRateChanged );

                OnTimeControlHyperWarpPhysicsAccuracyChangedEvent = GameEvents.FindEvent<EventData<float>>( nameof( TimeControlEvents.OnTimeControlHyperWarpPhysicsAccuracyChanged ) );
                OnTimeControlHyperWarpPhysicsAccuracyChangedEvent?.Add( OnTimeControlHyperWarpPhysicsAccuracyChanged );

                OnTimeControlHyperWarpMaximumDeltaTimeChangedEvent = GameEvents.FindEvent<EventData<float>>( nameof( TimeControlEvents.OnTimeControlHyperWarpMaximumDeltaTimeChanged ) );
                OnTimeControlHyperWarpMaximumDeltaTimeChangedEvent?.Add( OnTimeControlHyperWarpMaximumDeltaTimeChanged );

                OnTimeControlTimePausedEvent = GameEvents.FindEvent<EventData<bool>>( nameof( TimeControlEvents.OnTimeControlTimePaused ) );
                OnTimeControlTimePausedEvent?.Add( this.OnTimeControlTimePaused );

                OnTimeControlTimeUnpausedEvent = GameEvents.FindEvent<EventData<bool>>( nameof( TimeControlEvents.OnTimeControlTimeUnpaused ) );
                OnTimeControlTimeUnpausedEvent?.Add( this.OnTimeControlTimeUnpaused );

                currentScreenMessage = null;
                currentScreenMessageStyle = ScreenMessageStyle.UPPER_CENTER;
                currentScreenMessageDuration = Mathf.Infinity;
                currentScreenMessagePrefix = "HYPER-WARP";
                UpdateDefaultScreenMessage();
                CacheHyperWarpMessages();

                GameEvents.onGameSceneLoadRequested.Add( this.onGameSceneLoadRequested );
                GameEvents.onPartDestroyed.Add( this.onPartDestroyed );
                GameEvents.onVesselDestroy.Add( this.onVesselDestroy );
                GameEvents.onLevelWasLoaded.Add( this.onLevelWasLoaded );

                GameEvents.OnGameSettingsApplied.Add( this.OnGameSettingsApplied );

                FlightCamera[] cams = FlightCamera.FindObjectsOfType( typeof( FlightCamera ) ) as FlightCamera[];
                if (cams.Length > 0)
                {
                    cam = cams[0];
                }

                this.maxAttemptedRate = GlobalSettings.Instance.HyperWarpMaxAttemptedRate;
                this.physicsAccuracy = GlobalSettings.Instance.HyperWarpPhysicsAccuracy;

                Log.Info( nameof( HyperWarpController ) + " is Ready!", logBlockName );
                IsReady = true;
                yield break;
            }
        }

        private void Update()
        {
            if (isHyperWarping && !isGamePaused)
            {
                if (GlobalSettings.Instance.CameraZoomFix)
                {
                    cam.SetDistanceImmediate( cam.Distance );
                }

                if (!CanHyperWarp)
                {
                    DeactivateHyper();
                    return;
                }

                if (needToUpdateTimeScale)
                {
                    SetHyperTimeScale();
                    SetHyperFixedDeltaTime();
                    SetHyperMaximumDeltaTime();
                    needToUpdateTimeScale = false;
                }
            }

            UpdateScreenMessage();
        }

        private void UpdateScreenMessage()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( UpdateScreenMessage );
            if ((!this.isHyperWarping) || isGamePaused || !this.ShowOnscreenMessages)
            {
                RemoveCurrentScreenMessage();
                return;
            }

            ScreenMessage s;
            if (PerformanceCountersOn)
            {
                string msg = "HYPER-WARP ".MemoizedConcat( (Math.Round( this.PhysicsTimeRatio, 1 )).MemoizedToString() ).MemoizedConcat( "x" );
                if (msg != (this.currentScreenMessage?.message ?? ""))
                {
                    s = new ScreenMessage( msg, Mathf.Infinity, ScreenMessageStyle.UPPER_CENTER );
                }
                else
                {
                    s = this.currentScreenMessage;
                }
            }
            else
            {
                s = this.defaultScreenMessage;
            }

            if (s.message != (this.currentScreenMessage?.message ?? ""))
            {
                RemoveCurrentScreenMessage();
                this.currentScreenMessage = ScreenMessages.PostScreenMessage( s );
                if (Log.LoggingLevel == LogSeverity.Trace)
                {
                    Log.Trace( "Posting new screen message ".MemoizedConcat( this.currentScreenMessage.message ), logBlockName );
                }
            }
        }
        #endregion

        #region GameEvents
        private void OnTimeControlTimePaused(bool b)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( OnTimeControlTimePaused );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                isGamePaused = true;
                RemoveCurrentScreenMessage();
            }
        }

        private void OnTimeControlTimeUnpaused(bool b)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( OnTimeControlTimeUnpaused );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                isGamePaused = false;
                needToUpdateTimeScale = true;
            }
        }

        private void onLevelWasLoaded(GameScenes gs)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( onGameSceneLoadRequested );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                FlightCamera[] cams = FlightCamera.FindObjectsOfType( typeof( FlightCamera ) ) as FlightCamera[];
                if (cams.Length > 0)
                {
                    cam = cams[0];
                }

                DeactivateHyper();
            }
        }

        private void onGameSceneLoadRequested(GameScenes gs)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( onGameSceneLoadRequested );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {                
                DeactivateHyper();
            }
        }

        private void onPartDestroyed(Part p)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( onPartDestroyed );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (this.isHyperWarping && (HighLogic.LoadedSceneIsFlight && (p.vessel != null && FlightGlobals.ActiveVessel != null) && p.vessel == FlightGlobals.ActiveVessel))
                {
                    Log.Info( "Part on Active Vessel Destroyed. Cancelling Hyper-Warp." );
                    DeactivateHyper();
                }
            }
        }

        private void onVesselDestroy(Vessel v)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( onVesselDestroy );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (this.isHyperWarping && (HighLogic.LoadedSceneIsFlight && (FlightGlobals.ActiveVessel == null || v == FlightGlobals.ActiveVessel)))
                {
                    Log.Info( "Active Vessel Destroyed. Cancelling Hyper-Warp." );
                    DeactivateHyper();
                }
            }
        }

        private void OnGameSettingsApplied()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( OnGameSettingsApplied );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {

            }
        }

        // CUSTOM EVENTS



        private void OnTimeControlHyperWarpMaxAttemptedRateChanged(float rate)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( OnTimeControlHyperWarpMaxAttemptedRateChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (isHyperWarping && !isGamePaused)
                {
                    SetHyperTimeScale();
                }
            }
        }

        private void OnTimeControlHyperWarpPhysicsAccuracyChanged(float rate)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( OnTimeControlHyperWarpPhysicsAccuracyChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (isHyperWarping && !isGamePaused)
                {
                    SetHyperFixedDeltaTime();
                }
            }
        }

        private void OnTimeControlDefaultFixedDeltaTimeChanged(float rate)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( OnTimeControlDefaultFixedDeltaTimeChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (isHyperWarping && !isGamePaused)
                {
                    SetHyperFixedDeltaTime();
                }
            }
        }

        private void OnTimeControlHyperWarpMaximumDeltaTimeChanged(float rate)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( OnTimeControlHyperWarpMaximumDeltaTimeChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (isHyperWarping && !isGamePaused)
                {
                    SetHyperMaximumDeltaTime();
                }
            }
        }

        #endregion

        public void ActivateHyper()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( ActivateHyper );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (!CanHyperWarp)
                {
                    Log.Error( "Cannot hyper warp in this game state.", logBlockName );
                    DeactivateHyper();
                    return;
                }
                if (isHyperWarping)
                {
                    Log.Info( "Already hyper warping.", logBlockName );
                    return;
                }

                if (TimeController.Instance.IsTimeControlPaused)
                {
                    TimeController.Instance.Unpause();
                }

                SetCanRailsWarp( false );
                isHyperWarping = true;

                SetHyperTimeScale();
                SetHyperFixedDeltaTime();
                SetHyperMaximumDeltaTime();
            }
        }

        public void DeactivateHyper()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( DeactivateHyper );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ResetWarpToUT();
                
                if (!this.isHyperWarping)
                {
                    Log.Info( "Hyper warp not currently running.", logBlockName );
                    return;
                }

                ResetTimeScale();
                ResetFixedDeltaTime();
                ResetMaximumDeltaTime();

                isHyperWarping = false;
                SetCanRailsWarp( true );
            }
        }

        public void ToggleHyper()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( ToggleHyper );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (isHyperWarping)
                {
                    DeactivateHyper();
                }
                else
                {
                    if (CanHyperWarp)
                    {
                        ActivateHyper();
                    }
                    else
                    {
                        Log.Error( "Cannot hyper warp in this game state.", logBlockName );
                        DeactivateHyper();
                    }
                }
            }
        }

        public void SlowDown(float step = 1f)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( SlowDown );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Trying to decrease Hyper Warp Rate", logBlockName );
                MaxAttemptedRate -= step;
                return;
            }
        }

        public void SpeedUp(float step = 1f)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( SpeedUp );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Trying to increase Hyper Warp Rate", logBlockName );
                MaxAttemptedRate += step;
            }
        }

        public void DecreasePhysicsAccuracy(float step = 0.5f)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( DecreasePhysicsAccuracy );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Trying to decrease Hyper Warp Physics Accuracy", logBlockName );
                PhysicsAccuracy += step;
                return;
            }
        }

        public void IncreasePhysicsAccuracy(float step = 0.5f)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( IncreasePhysicsAccuracy );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Trying to increase Physics Accuracy", logBlockName );
                PhysicsAccuracy -= step;
            }
        }

        public bool HyperWarpForDuration(int warpHours, int warpMinutes, int warpSeconds)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( HyperWarpForDuration );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( String.Format( "Trying to Hyper Warp for {0} : {1} : {2} ", warpHours, warpMinutes, warpSeconds ), logBlockName );
                double totalSeconds = (warpHours * KSPUtil.dateTimeFormatter.Hour) + (warpMinutes * KSPUtil.dateTimeFormatter.Minute) + warpSeconds;
                return HyperWarpForSeconds( totalSeconds );
            }
        }

        public bool HyperWarpForSeconds(double seconds)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( HyperWarpForSeconds );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Trying to Hyper Warp for " + seconds.ToString() + " seconds", logBlockName );
                double UT = CurrentUT + seconds;
                return HyperWarpToUT( UT );
            }
        }

        public bool HyperWarpToUT(double UT)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( HyperWarpToUT );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                StartCoroutine( ExecuteWarpToUT( UT ) );
                return true;
            }
        }
        
        #region Private Methods
        
        /// <summary>
        /// Create a cache of screen message objects in-memory from 0.0x to 200.0x so we aren't creating and destroying screenmessage objects on the fly as physics updates
        /// </summary>
        private void CacheHyperWarpMessages()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( CacheHyperWarpMessages );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                for (int i = 0; i <= 2000; i++)
                {
                    this.HyperWarpMessagesCache.Add( new ScreenMessage( "HYPER-WARP " + String.Format( "{0:0.0}", Math.Round( ((float)i / 10f), 1 ) ) + "x", Mathf.Infinity, ScreenMessageStyle.UPPER_CENTER ) );
                }
            }
        }

        /// <summary>
        /// Change the default message displayed on the screen when you hyper warp
        /// </summary>
        private void UpdateDefaultScreenMessage()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( UpdateDefaultScreenMessage );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                defaultScreenMessage = new ScreenMessage( currentScreenMessagePrefix, currentScreenMessageDuration, currentScreenMessageStyle );
            }
        }

        /// <summary>
        /// Modify the Unity time scale
        /// </summary>
        private void SetHyperTimeScale()
        {
            TimeController.Instance.TimeScale = Mathf.Round( MaxAttemptedRate );
        }

        /// <summary>
        /// Modify the Unity fixed delta time
        /// </summary>
        private void SetHyperFixedDeltaTime()
        {
            TimeController.Instance.FixedDeltaTime = (DefaultFixedDeltaTime * PhysicsAccuracy);
        }

        /// <summary>
        /// Modify the Unity max delta time
        /// </summary>
        private void SetHyperMaximumDeltaTime()
        {
            TimeController.Instance.MaximumDeltaTime = MaximumDeltaTime;
        }

        /// <summary>
        ///  Reset the time scale
        /// </summary>
        private void ResetTimeScale()
        {
            TimeController.Instance.ResetTimeScale();
        }

        /// <summary>
        /// Reset the fixedDeltaTime to the defaults
        /// </summary>
        private void ResetFixedDeltaTime()
        {
            TimeController.Instance.ResetFixedDeltaTime();
        }

        /// <summary>
        /// Reset the maximumDeltaTime to the setting value
        /// </summary>
        private void ResetMaximumDeltaTime()
        {
            TimeController.Instance.ResetMaximumDeltaTime();
        }

        /// <summary>
        /// Turn off ability to rails warp
        /// </summary>
        /// <param name="canWarp"></param>
        private void SetCanRailsWarp(bool canWarp)
        {
            RailsWarpController.Instance.CanRailsWarp = canWarp;
        }

        private bool IsValidScene()
        {
            return (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION);
        }

        #region CoRoutines

        private IEnumerator ExecuteWarpToUT(double UT)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( ExecuteWarpToUT );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                double CurrentUT = Planetarium.GetUniversalTime();

                if (CurrentUT >= UT)
                {
                    Log.Warning( "Cannot warp to UT " + UT.ToString() + ". Already passed! Current UT is " + CurrentUT, logBlockName );
                    yield break;
                }

                IsHyperWarpingToUT = true;
                HyperWarpingToUT = UT;

                Log.Info( "Trying to Hyper Warp to UT " + UT, logBlockName );

                ActivateHyper();
                if (this.isHyperWarping && (Planetarium.GetUniversalTime() < UT))
                {
                    yield return new WaitForSeconds( (float)(UT - Planetarium.GetUniversalTime()) );
                }
                
                if (this.isHyperWarping)
                {
                    TimeController.Instance.PauseOnNextFixedUpdate = HyperPauseOnTimeReached;
                }

                DeactivateHyper();

                yield break;
            }
        }

        private void ResetWarpToUT()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( ResetWarpToUT );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (this.isHyperWarpingToUT)
                {
                    this.isHyperWarpingToUT = false;
                    this.hyperWarpingToUT = Mathf.Infinity;
                }
            }
        }

        private void RemoveCurrentScreenMessage()
        {
            if (ScreenMessages.Instance?.ActiveMessages?.Contains( this.currentScreenMessage ) ?? false)
            {
                ScreenMessages.RemoveMessage( this.currentScreenMessage );
            }
            this.currentScreenMessage = null;
        }


        #endregion

        #endregion

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

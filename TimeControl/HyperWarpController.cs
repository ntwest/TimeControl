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

        public static ConfigNode gameNode;

        #region Private Fields

        private const string customHyperWarpRatesNodeName = "customHyperWarpRates";
        private const string customHyperWarpRateNodeName = "customHyperWarpRate";
        private const string customHyperWarpPhysicsAccuracyRatesNodeName = "customHyperWarpPhysicsAccuracyRates";
        private const string customHyperWarpPhysicsAccuracyRateNodeName = "customHyperWarpPhysicsAccuracyRate";

        private bool RatesNeedUpdatedAndSaved { get; set; } = false;

        private EventData<float> OnTimeControlHyperWarpMaximumDeltaTimeChangedEvent;
        private EventData<float> OnTimeControlHyperWarpMaxAttemptedRateChangedEvent;
        private EventData<float> OnTimeControlHyperWarpPhysicsAccuracyChangedEvent;
        private EventData<float> OnTimeControlDefaultFixedDeltaTimeChangedEvent;
        private EventData<bool> OnTimeControlTimePausedEvent;
        private EventData<bool> OnTimeControlTimeUnpausedEvent;

        private double hyperWarpingToUT = Mathf.Infinity;
        private bool isHyperWarpingToUT = false;

        private List<float> defaultCustomHyperWarpRates = new List<float>()
        {
            1.0f, 2.0f, 3.0f, 4.0f, 6.0f, 8.0f, 10.0f, 15.0f, 20.0f, 50.0f
        };

        private List<float> customHyperWarpRates = new List<float>();

        private List<float> defaultCustomHyperWarpPhysicsAccuracyRates = new List<float>()
        {
            1.0f, 1.0f, 1.0f, 1.5f, 1.5f, 2.0f, 2.0f, 2.0f, 3.0f, 3.0f
        };

        private List<float> customHyperWarpPhysicsAccuracyRates = new List<float>();

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

                while (HyperWarpController.gameNode == null)
                {
                    Log.Info( "Scenario Object has not loaded the necessary config node yet", logBlockName );
                    yield return new WaitForSeconds( 1f );
                }

                Log.Info( "Setting Custom Hyper Warp Rates and Physics Accuracy to Defaults", logBlockName );
                customHyperWarpRates = defaultCustomHyperWarpRates;
                customHyperWarpPhysicsAccuracyRates = defaultCustomHyperWarpPhysicsAccuracyRates;

                Load();

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

            if (RatesNeedUpdatedAndSaved)
            {
                SaveWarpRatesSettings();
            }
        }

        /*
        private void FixedUpdate()
        {
            
            //            if (isHyperWarping && !isGamePaused)
            //{
            if (HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ActiveVessel != null)
            { 
                //var pos = FlightGlobals.ActiveVessel.orbit.pos;
                //var vel = FlightGlobals.ActiveVessel.orbit.vel;
                Log.Trace( String.Format( "{0},{1},{2}", Planetarium.GetUniversalTime().ToString(), FlightGlobals.ActiveVessel.orbit.orbitalEnergy, FlightGlobals.ActiveVessel.orbit.semiMajorAxis ) );
            }
            //}
            //if (isHyperWarping && !isGamePaused)
            //{
                //var ptr = Convert.ToSingle(this.PhysicsTimeRatio);
                //Planetarium.TimeScale = ptr;
                //var prop = TimeWarp.fetch.GetType().GetField( "tgt_rate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
                //prop.SetValue( TimeWarp.fetch, ptr );
            //}
            
    }
    */


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
                string msg = "HYPER-WARP ".MemoizedConcat( (Math.Round( this.PhysicsTimeRatio, 1 )).MemoizedToString("0.0") ).MemoizedConcat( "x" );
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

        private void SetPhysicsRateFromList()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( SetPhysicsRateFromList );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                int newRateIndex = customHyperWarpRates.FindIndex( x => Mathf.Approximately( x, MaxAttemptedRate ) );
                float newPhysRate = customHyperWarpPhysicsAccuracyRates[newRateIndex];

                Log.Info( String.Format( "Trying to set Physics Rate to {0}.", newPhysRate ), logBlockName );
                PhysicsAccuracy = newPhysRate;
            }
        }

        public void ChangeToLowerRate()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( ChangeToLowerRate );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                float currentMaxAttemptedRate = MaxAttemptedRate;
                float newRate = customHyperWarpRates.FindLast( x => x < currentMaxAttemptedRate );
                if (Mathf.Approximately(newRate, 0.0f))
                {
                    // Do nothing, rate is already lowest it can go
                    return;
                }

                Log.Info( String.Format( "Trying to set Hyper Warp Rate to {0}.", newRate ), logBlockName );
                MaxAttemptedRate = newRate;

                SetPhysicsRateFromList();
            }
        }

        public void ChangeToHigherRate()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( ChangeToHigherRate );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                float currentMaxAttemptedRate = MaxAttemptedRate;
                float newRate = customHyperWarpRates.Find( x => x > currentMaxAttemptedRate );
                if (Mathf.Approximately( newRate, 0.0f ))
                {
                    Log.Info( "Maximum Hyper Warp Rate in List Reached.", logBlockName );
                    return;
                }
                Log.Info( String.Format( "Trying to set Hyper Warp Rate to {0}.", newRate ), logBlockName );
                MaxAttemptedRate = newRate;

                SetPhysicsRateFromList();
            }
        }

        public List<float> GetCustomHyperWarpRates()
        {
            var v = new List<float>();
            v.AddRange( customHyperWarpRates );
            return v;
        }

        public List<float> GetCustomHyperWarpPhysicsAccuracyRates()
        {
            var v = new List<float>();
            v.AddRange( customHyperWarpPhysicsAccuracyRates );
            return v;
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

        /// <summary>
        /// Updates the custom hyper warp rate list
        ///  </summary>
        /// <param name="wr">Must have between 2 and 99 elements</param>
        /// <param name="ar">Must have same # of elements as <paramref name="wr"/></param>
        /// <exception cref="ArgumentNullException">Thrown when parameter <paramref name="wr"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown when parameter <paramref name="wr"/> has too few or too many elements</exception>
        public void SetCustomHyperWarpRates(List<float> wr, List<float> ar)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( SetCustomHyperWarpRates );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (wr == null)
                {
                    const string message = "New Custom Hyper Warp Rate List cannot be null";
                    Log.Error( message, logBlockName );
                    throw new ArgumentNullException( "wr", message );
                }
                if (ar == null)
                {
                    const string message = "New Custom Hyper Warp Physics Accuracy List cannot be null";
                    Log.Error( message, logBlockName );
                    throw new ArgumentNullException( "wr", message );
                }
                if (wr.Count < 2)
                {
                    const string message = "New Custom Hyper Warp Rate List must have at least 2 warp rates (one of which is 1.0f)";
                    Log.Error( message, logBlockName );
                    throw new ArgumentException( message, "wr" );
                }
                if (wr.Count > 99)
                {
                    const string message = "New Custom Hyper Warp Rate List can only have max of 99 warp rates";
                    Log.Error( message, logBlockName );
                    throw new ArgumentException( message, "wr" );
                }
                if (ar.Count != wr.Count)
                {
                    const string message = "Custom Hyper Warp Rate List and Custom Hyper Warp Physics Accuracy List must have the same number of elements";
                    Log.Error( message, logBlockName );
                    throw new ArgumentException( message, "wr" );
                }

                customHyperWarpRates = new List<float>();
                customHyperWarpRates.AddRange( wr );

                customHyperWarpPhysicsAccuracyRates = new List<float>();
                customHyperWarpPhysicsAccuracyRates.AddRange( ar );
                
                RatesNeedUpdatedAndSaved = true;
                Log.Info( "New Custom Hyper Warp Rates Set", logBlockName );
            }
        }

        public void ResetHyperWarpRates()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( SetCustomHyperWarpRates );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                this.SetCustomHyperWarpRatesToDefault();
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


        #region Saving/Loading to ConfigNode
        public void Load()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( Load );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Loading from Internal Config", logBlockName );
                this.LoadCustomHyperWarpRates( gameNode );
            }
        }

        public void Load(ConfigNode gameNode)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( Load );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Loading from Config Node", logBlockName );
                this.LoadCustomHyperWarpRates( gameNode );
            }
        }

        private void SetCustomHyperWarpRatesToDefault()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( SetCustomHyperWarpRatesToDefault );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                customHyperWarpRates = new List<float>();
                customHyperWarpRates.AddRange( defaultCustomHyperWarpRates );

                customHyperWarpPhysicsAccuracyRates = new List<float>();
                customHyperWarpPhysicsAccuracyRates.AddRange( defaultCustomHyperWarpPhysicsAccuracyRates );
            }
        }

        /// <summary>
        /// Load custom warp rates (and physics accuracy rates) into this object from a config node
        /// </summary>
        /// <param name="cn"></param>
        private void LoadCustomHyperWarpRates(ConfigNode cn)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( LoadCustomHyperWarpRates );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (cn == null)
                {
                    Log.Error( "ConfigNode " + nameof( cn ) + " is NULL, and cannot find existing settings config node.", logBlockName );
                    return;
                }

                // Rates List
                if (!cn.HasNode( customHyperWarpRatesNodeName ))
                {
                    const string message = "No custom hyper warp rates node in config. Using defaults.";
                    Log.Warning( message, logBlockName );
                    SetCustomHyperWarpRatesToDefault();
                    return;
                }

                ConfigNode customHyperWarpRatesNode = cn.GetNode( customHyperWarpRatesNodeName );
                List<float> ltcwr = new List<float>();

                bool hyperWarpRatesParseError = false;
                foreach (string s in customHyperWarpRatesNode.GetValuesStartsWith( customHyperWarpRateNodeName ))
                {
                    float num;
                    if (!(float.TryParse( s, out num )))
                    {
                        string message = "A custom hyper warp rate is not defined as a number (value in the config was " + s + ").";
                        Log.Warning( message, logBlockName );
                        hyperWarpRatesParseError = true;
                        ltcwr = null;
                        break;
                    }
                    ltcwr.Add( num );
                }

                if (hyperWarpRatesParseError)
                {
                    string message = "Error loading custom hyper warp rates from config. Using defaults.";
                    Log.Warning( message, logBlockName );
                    SetCustomHyperWarpRatesToDefault();
                    return;
                }

                try
                {
                    customHyperWarpRates = new List<float>();
                    customHyperWarpRates.AddRange( ltcwr );
                }
                catch (Exception ex)
                {
                    if (ex is ArgumentException || ex is ArgumentNullException)
                    {
                        string message = "Argument Exception when setting custom warp rates. Using defaults.";
                        Log.Warning( message, logBlockName );
                        SetCustomHyperWarpRatesToDefault();
                    }
                    else
                        throw;
                }

                // Physics Accuracy Rates List
                if (!cn.HasNode( customHyperWarpPhysicsAccuracyRatesNodeName ))
                {
                    const string message = "No custom hyper warp physics accuracy rates node in config. Using defaults.";
                    Log.Warning( message, logBlockName );
                    SetCustomHyperWarpRatesToDefault();
                    return;
                }

                ConfigNode customHyperWarpPhysicsAccuracyRatesNode = cn.GetNode( customHyperWarpPhysicsAccuracyRatesNodeName );
                List<float> ltcar = new List<float>();

                bool hyperWarpPhysicsAccuracyRatesParseError = false;
                foreach (string s in customHyperWarpPhysicsAccuracyRatesNode.GetValuesStartsWith( customHyperWarpPhysicsAccuracyRatesNodeName ))
                {
                    float num;
                    if (!(float.TryParse( s, out num )))
                    {
                        string message = "A custom hyper warp physics accuracy rate is not defined as a number (value in the config was " + s + ").";
                        Log.Warning( message, logBlockName );
                        hyperWarpPhysicsAccuracyRatesParseError = true;
                        ltcar = null;
                        break;
                    }
                    ltcar.Add( num );
                }

                if (hyperWarpPhysicsAccuracyRatesParseError)
                {
                    string message = "Error loading custom hyper warp physics accuracy rates from config. Using defaults.";
                    Log.Warning( message, logBlockName );
                    SetCustomHyperWarpRatesToDefault();
                    return;
                }

                try
                {
                    customHyperWarpPhysicsAccuracyRates  = new List<float>();
                    customHyperWarpPhysicsAccuracyRates.AddRange( ltcar );
                }
                catch (Exception ex)
                {
                    if (ex is ArgumentException || ex is ArgumentNullException)
                    {
                        string message = "Argument Exception when setting custom hyper warp physics accuracy rates. Using defaults.";
                        Log.Warning( message, logBlockName );
                        SetCustomHyperWarpRatesToDefault();
                    }
                    else
                        throw;
                }

                if (customHyperWarpPhysicsAccuracyRates.Count != customHyperWarpRates.Count || customHyperWarpRates.Count < 2)
                {
                    string message = "Problem using rates in config node. Switching to defaults.";
                    Log.Warning( message, logBlockName );
                    SetCustomHyperWarpRatesToDefault();
                }
            }
        }

        public void Save(ConfigNode gameNode)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( Save );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Saving to Config", logBlockName );
                this.SaveCustomHyperWarpRates( gameNode );
            }
        }

        /// <summary>
        /// Save custom warp rates
        /// </summary>
        private void SaveCustomHyperWarpRates(ConfigNode cn)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( SaveCustomHyperWarpRates );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (cn == null)
                {
                    Log.Error( "ConfigNode " + nameof( cn ) + " is NULL, and cannot find existing settings config node.", logBlockName );
                    return;
                }

                // Rate List

                // Rebuild the node
                if (cn.HasNode( customHyperWarpRatesNodeName ))
                {
                    cn.RemoveNode( customHyperWarpRatesNodeName );
                }
                ConfigNode customHyperWarpRatesNode = cn.AddNode( customHyperWarpRatesNodeName );

                if (customHyperWarpRates == null)
                {
                    Log.Error( "No custom hyper warp rates defined. Cannot save", logBlockName );
                    return;
                }

                Log.Trace( "creating custom hyper warp rates node", logBlockName );
                for (int i = 0; i < customHyperWarpRates.Count; i++)
                {
                    customHyperWarpRatesNode.AddValue( customHyperWarpRateNodeName + i, customHyperWarpRates[i] );
                }

                // Physics Accuracy List

                // Rebuild the node
                if (cn.HasNode( customHyperWarpPhysicsAccuracyRatesNodeName ))
                {
                    cn.RemoveNode( customHyperWarpPhysicsAccuracyRatesNodeName );
                }
                ConfigNode customHyperWarpPhysicsAccuracyRatesNode = cn.AddNode( customHyperWarpPhysicsAccuracyRatesNodeName );

                if (customHyperWarpPhysicsAccuracyRates == null)
                {
                    Log.Error( "No custom hyper warp physics accuracy rates defined. Cannot save", logBlockName );
                    return;
                }

                Log.Trace( "creating custom hyper warp physics accuracy node", logBlockName );
                for (int i = 0; i < customHyperWarpPhysicsAccuracyRates.Count; i++)
                {
                    customHyperWarpPhysicsAccuracyRatesNode.AddValue( customHyperWarpPhysicsAccuracyRatesNodeName + i, customHyperWarpPhysicsAccuracyRates[i] );
                }
            }
        }

        /// <summary>
        /// Update the warp rates and physics settings in the arrays
        /// </summary>
        private void SaveWarpRatesSettings()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( SaveWarpRatesSettings );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                // Force a game settings save
                GameSettings.SaveSettings();
                RatesNeedUpdatedAndSaved = false;
                TimeControlEvents.OnTimeControlCustomHyperWarpRatesChanged.Fire( true );
            }
        }

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

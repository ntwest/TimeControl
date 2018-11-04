using System;
using UnityEngine;
using System.Collections;

namespace TimeControl
{
    [KSPAddon( KSPAddon.Startup.Instantly, true )]
    internal class SlowMoController : MonoBehaviour
    {
        #region Singleton
        public static SlowMoController Instance { get; private set; }
        public static bool IsReady { get; private set; } = false;
        #endregion

        private FlightCamera cam;

        private EventData<bool> OnTimeControlTimePausedEvent;
        private EventData<bool> OnTimeControlTimeUnpausedEvent;
        private EventData<float> OnTimeControlSlowMoRateChangedEvent;

        private float defaultDeltaTime;
        
        private bool isGamePaused = false;
        private bool needToUpdateTimeScale = true;

        private ScreenMessage defaultScreenMessage;
        private ScreenMessage currentScreenMessage;
        
        
        private bool ShowOnscreenMessages
        {
            get => HighLogic.CurrentGame?.Parameters?.CustomParams<TimeControlParameterNode>()?.ShowSlowMoOnscreenMessages ?? true;
        }

        private bool PerformanceCountersOn
        {
            get => PerformanceManager.IsReady && (PerformanceManager.Instance?.PerformanceCountersOn ?? false);
        }

        private bool CurrentScreenMessageOn
        {
            get => currentScreenMessage != null && (ScreenMessages.Instance?.ActiveMessages?.Contains( currentScreenMessage ) ?? false);
        }
        
        

        /// <summary>
        /// 
        /// </summary>
        private bool SlowMoCanRailsWarp
        {
            get => slowMoCanRailsWarp && (RailsWarpController.Instance?.CanRailsWarp ?? false);
        }
        private bool slowMoCanRailsWarp = true;

        private void SetCanRailsWarp(bool canRailsWarp)
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( SetCanRailsWarp );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (RailsWarpController.Instance != null)
                {
                    if (RailsWarpController.Instance.CanRailsWarp == slowMoCanRailsWarp)
                    {
                        RailsWarpController.Instance.CanRailsWarp = canRailsWarp;
                    }
                    slowMoCanRailsWarp = canRailsWarp;
                }
                else
                {
                    Log.Error( "RailsWarpController.Instance is null! This should not happen. Please log a bug with the developer.", logBlockName );
                }
            }
        }

        /// <summary>
        /// Slow-Motion rate is set to lock the physics delta. Game will appear choppy.
        /// </summary>
        public bool DeltaLocked
        {
            get => this.deltaLocked;
            set
            {
                if (this.deltaLocked != value)
                {
                    this.deltaLocked = value;
                    TimeControlEvents.OnTimeControlSlowMoDeltaLockedChanged?.Fire( deltaLocked );
                }
            }
        }
        private bool deltaLocked = true;

        /// <summary>
        /// Game can currently switch to slow-motion
        /// </summary>
        public bool CanSlowMo
        {
            get => (TimeWarp.fetch != null && TimeWarp.CurrentRateIndex == 0 && (Mathf.Approximately( Time.timeScale, 1f ) || this.IsSlowMo) && HighLogic.LoadedSceneIsFlight);
        }

        /// <summary>
        /// Game is currently running in slow-motion
        /// </summary>
        public bool IsSlowMo
        {
            get => this.isSlowMo;
            private set
            {
                if (this.isSlowMo != value)
                {
                    this.isSlowMo = value;
                }
            }
        }
        private bool isSlowMo = false;


        /// <summary>
        /// Floating point value between 0 and 1. The lower the number the slower the rate. 0 = paused, 1 = realtime.
        /// </summary>
        public float SlowMoRate
        {
            get => this.slowMoRate;
            set
            {
                if (!Mathf.Approximately( this.slowMoRate, value ))
                {
                    this.slowMoRate = Mathf.Clamp01( value );
                    TimeControlEvents.OnTimeControlSlowMoRateChanged?.Fire( this.slowMoRate );
                }
            }
        }
        private float slowMoRate = 0.5f;
        
        public ScreenMessageStyle CurrentScreenMessageStyle
        {
            get => this.currentScreenMessageStyle;
            set
            {
                if (this.currentScreenMessageStyle != value)
                {
                    this.currentScreenMessageStyle = value;
                    UpdateDefaultScreenMessage();
                }
            }
        }
        private ScreenMessageStyle currentScreenMessageStyle;
        
        public float CurrentScreenMessageDuration
        {
            get => this.currentScreenMessageDuration;
            set
            {
                if (this.currentScreenMessageDuration != value)
                {
                    this.currentScreenMessageDuration = value;
                    UpdateDefaultScreenMessage();
                }
            }
        }
        private float currentScreenMessageDuration;
        
        public string CurrentScreenMessagePrefix
        {
            get => this.currentScreenMessagePrefix;
            set
            {
                if (this.currentScreenMessagePrefix != value)
                {
                    this.currentScreenMessagePrefix = value;
                    UpdateDefaultScreenMessage();
                }
            }
        }
        private string currentScreenMessagePrefix;

        #region MonoBehavior

        #region Initialization

        private void Awake()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( Awake );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                DontDestroyOnLoad( this );
                Instance = this;
            }
        }

        private void Start()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( Start );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                StartCoroutine( this.Configure() );
            }
        }
        
        private IEnumerator Configure()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( Configure );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                while (!GlobalSettings.IsReady || !IsValidScene() || TimeWarp.fetch == null || FlightGlobals.Bodies == null || FlightGlobals.Bodies.Count <= 0)
                {
                    yield return new WaitForSeconds( 1f );
                }

                this.slowMoRate = GlobalSettings.Instance.SlowMoRate;
                this.deltaLocked = GlobalSettings.Instance.DeltaLocked;

                this.SetDefaults();
                this.SubscribeToGameEvents();

                Log.Info( nameof( SlowMoController ) + " is Ready!", logBlockName );
                SlowMoController.IsReady = true;
                yield break;
            }
        }

        private void SetDefaults()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( SetDefaults );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                defaultDeltaTime = Time.fixedDeltaTime;
                currentScreenMessage = null;
                currentScreenMessageStyle = ScreenMessageStyle.UPPER_CENTER;
                currentScreenMessageDuration = Mathf.Infinity;
                currentScreenMessagePrefix = "SLOW-MOTION";
                UpdateDefaultScreenMessage();
            }
        }

        private void SubscribeToGameEvents()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( SubscribeToGameEvents );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                OnTimeControlTimePausedEvent = GameEvents.FindEvent<EventData<bool>>( nameof( TimeControlEvents.OnTimeControlTimePaused ) );
                OnTimeControlTimePausedEvent?.Add( this.OnTimeControlTimePaused );

                OnTimeControlTimeUnpausedEvent = GameEvents.FindEvent<EventData<bool>>( nameof( TimeControlEvents.OnTimeControlTimeUnpaused ) );
                OnTimeControlTimeUnpausedEvent?.Add( this.OnTimeControlTimeUnpaused );

                OnTimeControlSlowMoRateChangedEvent = GameEvents.FindEvent<EventData<float>>( nameof( TimeControlEvents.OnTimeControlSlowMoRateChanged ) );
                OnTimeControlSlowMoRateChangedEvent?.Add( this.OnTimeControlSlowMoRateChanged );

                GameEvents.onGameSceneLoadRequested.Add( this.onGameSceneLoadRequested );
                GameEvents.onLevelWasLoaded.Add( this.onLevelWasLoaded );
            }
        }

        #endregion Initialization

        #region Deallocation

        private void OnDestroy()
        {
            OnTimeControlSlowMoRateChangedEvent?.Remove( OnTimeControlSlowMoRateChanged );
            OnTimeControlTimePausedEvent?.Remove( OnTimeControlTimePaused );
            OnTimeControlTimeUnpausedEvent?.Remove( OnTimeControlTimeUnpaused );
        }

        #endregion Deallocation

        #region Update Functions

        private void Update()
        {
            if (this.IsSlowMo && !isGamePaused)
            {
                if (GlobalSettings.Instance.CameraZoomFix)
                {
                    this.cam.SetDistanceImmediate( cam.Distance );
                }

                if (!this.CanSlowMo)
                {
                    this.DeactivateSlowMo();
                    return;
                }

                if (needToUpdateTimeScale)
                {
                    SetSlowMoTime();
                    needToUpdateTimeScale = false;
                }
            }

            
            this.UpdateScreenMessage();            
        }

        private void UpdateScreenMessage()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( UpdateScreenMessage );

            if ((!IsSlowMo) || isGamePaused || !this.ShowOnscreenMessages)
            {
                RemoveCurrentScreenMessage();
                return;
            }
            
            ScreenMessage s;
            if (PerformanceCountersOn)
            {
                string msg = "SLOW-MOTION ".MemoizedConcat( (Math.Round( this.SlowMoRate * 100f, 1 )).MemoizedToString() ).MemoizedConcat( "%" );
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

        private void RemoveCurrentScreenMessage()
        {
            if (ScreenMessages.Instance?.ActiveMessages?.Contains( this.currentScreenMessage ) ?? false)
            {
                ScreenMessages.RemoveMessage( this.currentScreenMessage );
            }
            this.currentScreenMessage = null;
        }

        #endregion Update Functions

        #endregion MonoBehavior
        
        #region GameEvents

        private void OnTimeControlTimePaused(bool b)
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( OnTimeControlTimePaused );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                isGamePaused = true;
                RemoveCurrentScreenMessage();
            }
        }

        private void OnTimeControlTimeUnpaused(bool b)
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( OnTimeControlTimeUnpaused );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                isGamePaused = false;
                needToUpdateTimeScale = true;
            }
        }

        private void onGameSceneLoadRequested(GameScenes gs)
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( onGameSceneLoadRequested );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (this.isSlowMo)
                {
                    DeactivateSlowMo();
                }
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

                DeactivateSlowMo();
            }
        }

        private void OnTimeControlSlowMoRateChanged(float data)
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( OnTimeControlSlowMoRateChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (this.IsSlowMo && !this.isGamePaused)
                {
                    SetSlowMoTime();
                }
            }
        }

        #endregion GameEvents

        public void ActivateSlowMo()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( ActivateSlowMo );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (!this.CanSlowMo)
                {
                    Log.Error( "Cannot enter slow-motion in this game state.", logBlockName );
                    DeactivateSlowMo();
                    return;
                }
                if (this.isSlowMo)
                {
                    Log.Warning( "Already in slow-motion.", logBlockName );
                    return;
                }

                if (TimeController.Instance.IsTimeControlPaused)
                {
                    TimeController.Instance.Unpause();
                }

                SetCanRailsWarp(false);
                isSlowMo = true;

                SetSlowMoTime();
            }
        }

        public void DeactivateSlowMo()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( DeactivateSlowMo );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (!isSlowMo)
                {
                    Log.Info( "Slow-Motion not currently running.", logBlockName );
                    return;
                }

                ResetTime();

                isSlowMo = false;
                SetCanRailsWarp(true);
            }
        }

        public void ToggleSlowMo()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( ToggleSlowMo );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (isSlowMo)
                {
                    DeactivateSlowMo();
                }
                else
                {
                    if (this.CanSlowMo)
                    {
                        ActivateSlowMo();
                    }
                    else
                    {
                        Log.Error( "Cannot use Slow-Motion in this game state.", logBlockName );
                        DeactivateSlowMo();
                    }
                }
            }
        }

        public void SlowDown(float steps = 0.01f)
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( SlowDown );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Decreasing Slow-Mo Rate", logBlockName );
                SlowMoRate = SlowMoRate - steps;
                return;
            }
        }

        public void SpeedUp(float steps = 0.01f)
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( SpeedUp );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Increasing Slow-Mo Rate", logBlockName );
                SlowMoRate = SlowMoRate + steps;
            }
        }
        

        /// <summary>
        /// Set the slow motion scale and delta time
        /// </summary>
        private void SetSlowMoTime()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( SetSlowMoTime );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                SetTimeScale();
                SetFixedDeltaTime();
            }
        }

        /// <summary>
        /// Modify the Unity time scale
        /// </summary>
        private void SetTimeScale()
        {
            TimeController.Instance.TimeScale = this.SlowMoRate;
        }

        /// <summary>
        /// Modify the Unity fixed delta time
        /// </summary>
        private void SetFixedDeltaTime()
        {
            float defaultDeltaTime = TimeController.Instance.DefaultFixedDeltaTime;
            TimeController.Instance.FixedDeltaTime = DeltaLocked ? defaultDeltaTime : (defaultDeltaTime * this.SlowMoRate);
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
        /// Reset all the time components for the slow mo controller.
        /// </summary>
        private void ResetTime()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( ResetTime );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ResetTimeScale();
                ResetFixedDeltaTime();
            }
        }

        /// <summary>
        /// Change the default message displayed on the screen when you hyper warp
        /// </summary>
        private void UpdateDefaultScreenMessage()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( UpdateDefaultScreenMessage );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                defaultScreenMessage = new ScreenMessage( currentScreenMessagePrefix, currentScreenMessageDuration, currentScreenMessageStyle );
            }
        }

        private bool IsValidScene()
        {
            return (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION);
        }

        /*
        public void SetFPSKeeper(bool v)
        {
            const string logBlockName = "TimeController.SetFPSKeeper(bool)";
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {

                if (!((CanControlWarpType & TimeControllable.SlowMo) == TimeControllable.SlowMo))
                {
                    Log.Warning( "Cannot call SetFPSKeeper when we cannot Slow-Mo warp", logBlockName );
                    return;
                }

                if (!TrySetSlowMo())
                {
                    return;
                }

                IsFpsKeeperActive = v;
                CheckSlowMo();
                return;
            }
        }

        public void ToggleFPSKeeper()
        {
            const string logBlockName = "TimeController.ToggleFPSKeeper";
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (!((CanControlWarpType & TimeControllable.SlowMo) == TimeControllable.SlowMo))
                {
                    Log.Warning( "Cannot call ToggleFPSKeeper when we cannot Slow-Mo warp", logBlockName );
                    return;
                }

                if (!TrySetSlowMo())
                {
                    return;
                }

                IsFpsKeeperActive = !IsFpsKeeperActive;
                CheckSlowMo();
                return;
            }
        }
        */

    }
}


/*
All code in this file Copyright(c) 2016 Nate West
Rewritten from scratch, but based on code Copyright(c) 2014 Xaiier

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

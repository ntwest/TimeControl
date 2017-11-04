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

using System;
using System.Reflection;
using UnityEngine;
using KSPPluginFramework;
using System.Collections;
using System.Collections.Generic;
using KSP.UI.Dialogs;
using System.Linq;

namespace TimeControl
{
    [KSPAddon( KSPAddon.Startup.MainMenu, true )]
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

        #region Private Fields

        private EventData<float> OnTimeControlHyperWarpMaxAttemptedRateChangedEvent;
        private EventData<float> OnTimeControlHyperWarpPhysicsAccuracyChangedEvent;
        private EventData<float> OnTimeControlDefaultFixedDeltaTimeChangedEvent;

        private Boolean hyperPauseOnTimeReached = false;
        private float physicsAccuracy = 1f;
        private float maxAttemptedRate = 2f;
        private double hyperWarpingToUT = Mathf.Infinity;
        private bool isHyperWarpingToUT = false;

        private bool canHyperWarp = false;
        private bool isHyperWarping = false;
        //private bool isHyperWarpPaused = false;

        private ScreenMessage currentScreenMessage;
        private ScreenMessage defaultScreenMessage;
        private ScreenMessageStyle currentScreenMessageStyle = ScreenMessageStyle.UPPER_CENTER;
        private float currentScreenMessageDuration = Mathf.Infinity;
        private string currentScreenMessagePrefix = "HYPER-WARP";
        
        private List<ScreenMessage> HyperWarpMessagesCache = new List<ScreenMessage>();

        #endregion

        #region Properties
        
        // PRIVATE

        private bool ShowOnscreenMessages
        {
            get => HighLogic.CurrentGame?.Parameters?.CustomParams<TimeControlParameterNode>()?.ShowHyperOnscreenMessages ?? true;
        }

        private bool PerformanceCountersOn
        {
            get => PerformanceManager.Instance?.PerformanceCountersOn ?? false;
        }

        private bool CurrentScreenMessageOn
        {
            get => currentScreenMessage != null && (ScreenMessages.Instance?.ActiveMessages?.Contains( currentScreenMessage ) ?? false);
        }

        private float DefaultFixedDeltaTime
        {
            get => TimeController.Instance?.DefaultFixedDeltaTime ?? 0.02f;
        }

        private double PhysicsTimeRatio
        {
            get => PerformanceManager.Instance?.PhysicsTimeRatio ?? 0.0f;
        }

        // PUBLIC

        public float PhysicsAccuracyMin
        {
            get => 1f;
        }
        public float PhysicsAccuracyMax
        {
            get => 6f;
        }
        public float AttemptedRateMin
        {
            get => 2f;
        }
        public float AttemptedRateMax
        {
            get => 100f;
        }
        
        /// <summary>
        /// 
        /// </summary>
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

        public float PhysicsAccuracy
        {
            get => this.physicsAccuracy;
            set
            {
                if (this.physicsAccuracy != value)
                {
                    this.physicsAccuracy = Mathf.Clamp( value, PhysicsAccuracyMin, PhysicsAccuracyMax ); ;
                    TimeControlEvents.OnTimeControlHyperWarpPhysicsAccuracyChanged?.Fire( this.physicsAccuracy );
                }
            }
        }

        public float MaxAttemptedRate
        {
            get => this.maxAttemptedRate;
            set
            {
                if (this.maxAttemptedRate != value)
                {
                    this.maxAttemptedRate = Mathf.Clamp( value, AttemptedRateMin, AttemptedRateMax );
                    TimeControlEvents.OnTimeControlHyperWarpMaxAttemptedRateChanged?.Fire( this.maxAttemptedRate );
                }
            }
        }

        public bool CanHyperWarp
        {
            get => (TimeWarp.fetch != null && TimeWarp.CurrentRateIndex <= 1 && Time.timeScale >= 1f);
        }

        public bool IsHyperWarping
        {
            get => isHyperWarping;
            private set
            {
                if (this.isHyperWarping != value)
                {
                    this.isHyperWarping = value;
                    TimeControlEvents.OnTimeControlHyperWarpPhysicsAccuracyChanged?.Fire( this.physicsAccuracy );
                }
            }
        }

        public bool IsHyperWarpingToUT
        {
            get => isHyperWarpingToUT;
            private set => isHyperWarpingToUT = value;
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
                hyperWarpingToUT = value;
            }
        }

        public ScreenMessageStyle CurrentScreenMessageStyle
        {
            get => currentScreenMessageStyle;
            set
            {
                currentScreenMessageStyle = value;
                UpdateDefaultScreenMessage();
            }
        }

        public float CurrentScreenMessageDuration
        {
            get => currentScreenMessageDuration;
            set
            {
                currentScreenMessageDuration = value;
                UpdateDefaultScreenMessage();
            }
        }

        public string CurrentScreenMessagePrefix
        {
            get => currentScreenMessagePrefix;
            set
            {
                currentScreenMessagePrefix = value;
                UpdateDefaultScreenMessage();
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
                
                OnTimeControlDefaultFixedDeltaTimeChangedEvent = GameEvents.FindEvent<EventData<float>>( nameof( TimeControlEvents.OnTimeControlDefaultFixedDeltaTimeChanged ) );
                OnTimeControlDefaultFixedDeltaTimeChangedEvent?.Add( DefaultFixedDeltaTimeChanged );

                OnTimeControlHyperWarpMaxAttemptedRateChangedEvent = GameEvents.FindEvent<EventData<float>>( nameof (TimeControlEvents.OnTimeControlHyperWarpMaxAttemptedRateChanged ) );
                OnTimeControlHyperWarpMaxAttemptedRateChangedEvent?.Add( MaxAttemptedRateChanged );

                OnTimeControlHyperWarpPhysicsAccuracyChangedEvent = GameEvents.FindEvent<EventData<float>>( nameof ( TimeControlEvents.OnTimeControlHyperWarpPhysicsAccuracyChanged ) );
                OnTimeControlHyperWarpPhysicsAccuracyChangedEvent?.Add( PhysicsAccuracyChanged );

                currentScreenMessage = null;
                currentScreenMessageStyle = ScreenMessageStyle.UPPER_CENTER;
                currentScreenMessageDuration = Mathf.Infinity;
                currentScreenMessagePrefix = "HYPER-WARP";
                UpdateDefaultScreenMessage();
                CacheHyperWarpMessages();

                GameEvents.onGamePause.Add( this.onGamePause );
                GameEvents.onGameUnpause.Add( this.onGameUnpause );
                GameEvents.onGameSceneLoadRequested.Add( this.onGameSceneLoadRequested );
                GameEvents.onPartDestroyed.Add( this.onPartDestroyed );
                GameEvents.onVesselDestroy.Add( this.onVesselDestroy );

                GameEvents.OnGameSettingsApplied.Add( this.OnGameSettingsApplied );

                HyperWarpController.IsReady = true;
            }
        }

        private void OnDestroy()
        {
            OnTimeControlDefaultFixedDeltaTimeChangedEvent?.Remove( DefaultFixedDeltaTimeChanged );
            OnTimeControlHyperWarpMaxAttemptedRateChangedEvent?.Remove( MaxAttemptedRateChanged );
            OnTimeControlHyperWarpPhysicsAccuracyChangedEvent?.Remove( PhysicsAccuracyChanged );
        }

        private void Update()
        {
            if (isHyperWarping && !CanHyperWarp)
            {
                DeactivateHyper();
                return;
            }
        }
        #endregion

        #region GameEvents
        private void onGamePause()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( onGamePause );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
            }
        }
        
        private void onGameUnpause()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( onGameUnpause );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (this.isHyperWarping)
                {
                    if (!this.canHyperWarp)
                    {
                        DeactivateHyper();
                        return;
                    }
                    SetHyperTimeScale();
                    SetHyperFixedDeltaTime();
                }
            }
        }

        private void onGameSceneLoadRequested(GameScenes gs)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( onGameSceneLoadRequested );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (this.isHyperWarping)
                {
                    DeactivateHyper();
                }
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

        private void MaxAttemptedRateChanged(float rate)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( MaxAttemptedRateChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (isHyperWarping)
                {
                    SetHyperTimeScale();
                }
            }
        }

        private void PhysicsAccuracyChanged(float rate)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( PhysicsAccuracyChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (isHyperWarping)
                {
                    SetHyperFixedDeltaTime();
                }
            }
        }

        private void DefaultFixedDeltaTimeChanged(float rate)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( DefaultFixedDeltaTimeChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (isHyperWarping)
                {
                    SetHyperFixedDeltaTime();
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
                    Log.Warning( "Already hyper warping.", logBlockName );
                    return;
                }
                SetCanRailsWarp( false );
                isHyperWarping = true;

                SetHyperTimeScale();
                SetHyperFixedDeltaTime();

                StartCoroutine( UpdateHyperWarpScreenMessage() );
            }
        }

        public void DeactivateHyper()
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( DeactivateHyper );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ResetWarpToUT();

                if (!isHyperWarping)
                {
                    Log.Info( "Hyper warp not currently running.", logBlockName );
                    return;
                }

                ResetTimeScale();
                ResetFixedDeltaTime();                

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

        public void SlowDownHyper(int step = 1)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( SlowDownHyper );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Trying to decrease Hyper Warp Rate", logBlockName );
                MaxAttemptedRate -= step;
                return;
            }
        }

        public void SpeedUpHyper(int step = 1)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( SpeedUpHyper );
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
                PhysicsAccuracy -= step;
                return;
            }
        }

        public void IncreasePhysicsAccuracy(float step = 0.5f)
        {
            const string logBlockName = nameof( HyperWarpController ) + "." + nameof( IncreasePhysicsAccuracy );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Trying to increase Physics Accuracy", logBlockName );
                PhysicsAccuracy += step;
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
                double UT = Planetarium.GetUniversalTime() + seconds;
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
        /// Turn off ability to rails warp
        /// </summary>
        /// <param name="canWarp"></param>
        private void SetCanRailsWarp(bool canWarp)
        {
            RailsWarpController.Instance.CanRailsWarp = canWarp;
        }

        #region CoRoutines

        private IEnumerator ExecuteWarpToUT(double UT)
        {
            const string logBlockName = "HyperWarpController.ExecuteWarpToUT(double)";
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
            const string logBlockName = "HyperWarpController.UpdateHyperWarpScreenMessage()";
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (this.isHyperWarpingToUT)
                {
                    this.isHyperWarpingToUT = false;
                    this.hyperWarpingToUT = Mathf.Infinity;
                }
            }
        }

        private IEnumerator UpdateHyperWarpScreenMessage()
        {
            const string logBlockName = "HyperWarpController.UpdateHyperWarpScreenMessage()";
            const float screenMessageUpdateFrequency = 1f;

            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                while (true)
                {
                    if ((!this.isHyperWarping))
                    {
                        if ((!this.ShowOnscreenMessages) || CurrentScreenMessageOn)
                        {
                            ScreenMessages.RemoveMessage( this.currentScreenMessage );
                        }
                        yield break;
                    }
                    
                    int idx = (int)(Math.Round( this.PhysicsTimeRatio, 1 ) * 10);
                    if (CurrentScreenMessageOn && this.HyperWarpMessagesCache[idx] != this.currentScreenMessage)
                    {
                        ScreenMessages.RemoveMessage( this.currentScreenMessage );
                        if (PerformanceCountersOn)
                        {
                            this.currentScreenMessage = this.HyperWarpMessagesCache[idx];
                        }
                        else
                        {
                            this.currentScreenMessage = this.defaultScreenMessage;
                        }
                        this.currentScreenMessage = ScreenMessages.PostScreenMessage( this.currentScreenMessage );
                    }
                    yield return new WaitForSeconds( screenMessageUpdateFrequency );
                }
            }
        }
        #endregion

        #endregion

    }
}

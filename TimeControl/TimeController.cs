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

    [Flags]
    public enum TimeControllable
    {
        None = 0,
        Rails = 1,
        Physics = 2,
        Hyper = 4,
        SlowMo = 8,
        All = Rails | Hyper | SlowMo
    }

    [KSPAddon( KSPAddon.Startup.MainMenu, true )]
    internal class TimeController : MonoBehaviour
    {
        #region Singleton
        public static bool IsReady { get; private set; } = false;
        private static TimeController instance;
        public static TimeController Instance { get { return instance; } }
        #endregion

        #region Simple Properites and Fields

        //PHYSICS
        private float defaultFixedDeltaTime;
        private float timeSlider = 0f;
        private float maxDeltaTimeSlider = GameSettings.PHYSICS_FRAME_DT_LIMIT;
        private bool timePaused;
        private bool pauseOnNextFixedUpdate = false;
        private float smoothSlider = 0f;

        private bool deltaLocked = false;
        
        #endregion

        #region Properties       

        public CelestialBody CurrentGameSOI
        {
            get
            {
                // Set Home Body as default
                CelestialBody cb = FlightGlobals.GetHomeBody();

                if (HighLogic.LoadedSceneIsFlight)
                {
                    cb = FlightGlobals.currentMainBody ?? cb;
                }
                else if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                {
                    if (PlanetariumCamera.fetch?.target?.type == MapObject.ObjectType.CelestialBody)
                    {
                        cb = PlanetariumCamera.fetch?.target?.celestialBody ?? cb;
                    }
                    else if (PlanetariumCamera.fetch?.target?.type == MapObject.ObjectType.Vessel)
                    {
                        cb = PlanetariumCamera.fetch?.target?.vessel?.mainBody ?? cb;
                    }
                }

                return cb;
            }
        }

        public float TimeScale
        {
            get => Time.timeScale;
            internal set
            {
                if (Time.timeScale != value)
                {
                    Time.timeScale = value;
                    TimeControlEvents.OnTimeControlTimeScaleChanged?.Fire( Time.timeScale );
                }
            }
        }

        public float FixedDeltaTime
        {
            get => Time.fixedDeltaTime;
            internal set
            {
                if (Time.fixedDeltaTime != value || (Planetarium.fetch != null && Planetarium.fetch.fixedDeltaTime != value))
                {
                    Time.fixedDeltaTime = value;
                    if (Planetarium.fetch != null)
                    {
                        Planetarium.fetch.fixedDeltaTime = Time.fixedDeltaTime;
                    }
                    TimeControlEvents.OnTimeControlFixedDeltaTimeChanged?.Fire( Time.fixedDeltaTime );
                }
            }
        }

        public bool SupressFlightResultsDialog
        {
            get => HighLogic.CurrentGame?.Parameters?.CustomParams<TimeControlParameterNode>()?.SupressFlightResultsDialog ?? false;
        }
        
        #endregion


        #region MonoBehavior
        private void Awake()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( Awake );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                DontDestroyOnLoad( this );
                instance = this;
            }
        }
        private void Start()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( Start );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                defaultFixedDeltaTime = Time.fixedDeltaTime; // 0.02f

                GameEvents.onGamePause.Add( this.onGamePause );
                GameEvents.onGameUnpause.Add( this.onGameUnpause );
                GameEvents.OnGameSettingsApplied.Add( this.OnGameSettingsApplied );
                
                /*
                GameEvents.onGameSceneLoadRequested.Add( this.onGameSceneLoadRequested );
                GameEvents.onTimeWarpRateChanged.Add( this.onTimeWarpRateChanged );
                GameEvents.onFlightReady.Add( this.onFlightReady );
                GameEvents.onVesselGoOffRails.Add( this.onVesselGoOffRails );
                GameEvents.onLevelWasLoaded.Add( this.onLevelWasLoaded );
                */

                //FlightCamera[] cams = FlightCamera.FindObjectsOfType( typeof( FlightCamera ) ) as FlightCamera[];
                //cam = cams[0];

                Log.Info( "TimeController.Instance is Ready!", logBlockName );
                IsReady = true;
            }
        }

        private void OnGameSettingsApplied()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( OnGameSettingsApplied );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                GameSettings.KERBIN_TIME = HighLogic.CurrentGame.Parameters.CustomParams<TimeControlParameterNode>().UseKerbinTime;
                Log.LoggingLevel = HighLogic.CurrentGame.Parameters.CustomParams<TimeControlParameterNode>().LoggingLevel;

                if (GlobalSettings.Instance != null && GlobalSettings.IsReady)
                {
                    GlobalSettings.Instance.Save();
                }
            }
        }

        /// <summary>
        /// Go back to realtime
        /// </summary>
        internal void ResetTime()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( ResetFixedDeltaTime );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                this.ResetTimeScale();
                this.ResetFixedDeltaTime();
            }
        }

        /// <summary>
        /// Reset the fixedDeltaTime to default
        /// </summary>
        internal void ResetFixedDeltaTime()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( ResetFixedDeltaTime );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                this.FixedDeltaTime = DefaultFixedDeltaTime;
            }
        }

        /// <summary>
        ///  Reset the time scale to default
        /// </summary>
        internal void ResetTimeScale()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( ResetTimeScale );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                this.TimeScale = 1f;
            }
        }

        public float DefaultFixedDeltaTime
        {
            get => defaultFixedDeltaTime;
        }

        #region Update Functions
        private void FixedUpdate()
        {
            if (PauseOnNextFixedUpdate)
            {
                TimePaused = true;
                PauseOnNextFixedUpdate = false;
            }
        }

        private void LateUpdate()
        {
            LateUpdateFlightResultsDialog();
        }

        private void LateUpdateFlightResultsDialog()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }

            if (SupressFlightResultsDialog)
            {
                FlightResultsDialog.Close();
            }
        }

        #endregion

        #endregion

        #region GameEvents
        private void onGamePause()
        {
            const string logBlockName = "TimeController.onGamePause";
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
            }
        }

        private void onGameUnpause()
        {
            const string logBlockName = "TimeController.onGameUnpause";
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
            }
        }
        #endregion

        #region Properties
        #region Static Properties
        public static float MaxDeltaTimeSliderMin {
            get {
                return 0.02f;
            }
        }
        public static float MaxDeltaTimeSliderMax {
            get {
                return 0.12f;
            }
        }
        public static float HyperMinPhysMin {
            get {
                return 1f;
            }
        }
        public static float HyperMinPhysMax {
            get {
                return 6f;
            }
        }
        public static float HyperMaxRateMin {
            get {
                return 2f;
            }
        }
        public static float HyperMaxRateMax {
            get {
                return 100f;
            }
        }
        #endregion

        #region Read-Only Private Set Properties


        public float TimeSlider {
            get {
                return timeSlider;
            }
            private set {
                if (timeSlider != value)
                {
                    timeSlider = value;
                }
            }
        }

        public float SmoothSlider {
            get {
                return smoothSlider;
            }

            private set {
                if (smoothSlider != value)
                {
                    smoothSlider = value;
                }
            }
        }

        public bool TimePaused {
            get {
                return timePaused;
            }
            private set {
                if (timePaused != value)
                {
                    timePaused = value;
                    if (value)
                    {
                        Time.timeScale = 0f;
                        TimeControlEvents.OnTimeControlTimePaused?.Fire( true );
                    }
                    else
                    {
                        Time.timeScale = 1f;
                        TimeControlEvents.OnTimeControlTimeUnpaused?.Fire( true );
                    }
                }
            }
        }

        public TimeControllable CurrentWarpState {
            get {
                if (RailsWarpController.Instance?.IsRailsWarpingNoPhys ?? false)
                {
                    return TimeControllable.Rails;
                }
                if (RailsWarpController.Instance?.IsRailsWarpingPhys ?? false)
                {
                    return TimeControllable.Physics;
                }
                if (HyperWarpController.Instance?.IsHyperWarping ?? false)
                {
                    return TimeControllable.Hyper;
                }
                if (SlowMoController.Instance?.IsSlowMo ?? false)
                {
                    return TimeControllable.SlowMo;
                }
                return TimeControllable.None;
            }
        }

        public TimeControllable CanControlWarpType {
            get {
                if (HighLogic.LoadedSceneIsFlight)
                    return TimeControllable.All;
                else if (HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER)
                    return TimeControllable.Rails;
                else
                    return TimeControllable.None;
            }
        }
        #endregion
        #region Read Write Properties
        public bool DeltaLocked {
            get {
                return deltaLocked;
            }
            set {
                if (deltaLocked != value)
                {
                    deltaLocked = value;
                }
            }
        }

        public float MaxDeltaTime {
            get {
                return maxDeltaTimeSlider;
            }

            set {
                if (maxDeltaTimeSlider != value)
                {
                    // round to 2 decimal points, then clamp between min and max
                    float v = Mathf.Clamp( (Mathf.Round( value * 100f ) / 100f), MaxDeltaTimeSliderMin, MaxDeltaTimeSliderMax );
                    maxDeltaTimeSlider = v;
                    Time.maximumDeltaTime = v;
                    GameSettings.PHYSICS_FRAME_DT_LIMIT = v;
                    GameSettings.SaveSettings();
                }
            }
        }

        public bool PauseOnNextFixedUpdate {
            get {
                return pauseOnNextFixedUpdate;
            }

            set {
                if (pauseOnNextFixedUpdate != value)
                {
                    pauseOnNextFixedUpdate = value;
                }
            }
        }


        #endregion

        #endregion

        #region Private Methods 
        #endregion

        #region Public Methods        
        #region Pause
        public void TogglePause()
        {
            const string logBlockName = "TimeController.TogglePause()";
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {                
                TimePaused = !TimePaused;                
            }
        }
        public void IncrementTimeStep()
        {
            const string logBlockName = "TimeController.IncrementTimeStep()";
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                TimePaused = false;
                PauseOnNextFixedUpdate = true;
            }
        }
        public void GoRealTime()
        {
            const string logBlockName = "TimeController.GoRealTime()";
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                TimePaused = false;
                PauseOnNextFixedUpdate = false;
                RailsWarpController.Instance?.DeactivateRails();
                HyperWarpController.Instance?.DeactivateHyper();
                SlowMoController.Instance?.DeactivateSlowMo();
            }
        }


        #endregion        
        #endregion
    }
}

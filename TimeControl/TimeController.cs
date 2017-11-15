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
        private double CurrentUT
        {
            get => Planetarium.GetUniversalTime();
        }


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

        public IDateTimeFormatter CurrentDTF
        {
            get => KSPUtil.dateTimeFormatter;
        }

        public KACWrapper.KACAPI.KACAlarm ClosestKACAlarm
        {
            get;
            private set;
        }

        public float DefaultFixedDeltaTime
        {
            get => defaultFixedDeltaTime;
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

                //GameEvents.OnGameSettingsApplied.Add( this.OnGameSettingsApplied );
                GameEvents.onLevelWasLoaded.Add( this.onLevelWasLoaded );

                Log.Info( "TimeController.Instance is Ready!", logBlockName );
                IsReady = true;
            }
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
            const string logBlockName = nameof( TimeController ) + "." + nameof( onGamePause );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
            }
        }

        private void onGameUnpause()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( onGameUnpause );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
            }
        }
        
        private void onLevelWasLoaded(GameScenes gs)
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( onLevelWasLoaded );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {                
                // Retry KAC load on scene change
                if (gs == GameScenes.FLIGHT || gs == GameScenes.SPACECENTER || gs == GameScenes.TRACKSTATION)
                {
                    if (!KACWrapper.InstanceExists)
                    {
                        SetupKACAlarms();
                    }
                }
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
        private void SetupKACAlarms()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( SetupKACAlarms );

            KACWrapper.InitKACWrapper();
            if (KACWrapper.InstanceExists)
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
            const string logBlockName = nameof( TimeController ) + "." + nameof( CheckKACAlarms );

            while (true)
            {
                var list = KACWrapper.KAC.Alarms.Where( f => f.AlarmTime > CurrentUT && f.AlarmType != KACWrapper.KACAPI.AlarmTypeEnum.EarthTime ).OrderBy( f => f.AlarmTime );
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

                yield return new WaitForSeconds( 1f );
            }
        }

        #endregion

        #region Internal Methods
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
        #endregion Internal Methods

        #region Public Methods
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


        // TODO - QuickWarp / WarpTo based on ksp date time formatter settings

        // Seconds / Minute

        //public void ComputeIncrements()
        //{
        //    List<int> secondsInMinute = new List<int>() {
        //        1,
        //        (int)Math.Floor( ((CurrentDTF.Minute * 0.25) * 1.0) / 3.0 ),
        //        (int)Math.Floor( ((CurrentDTF.Minute * 0.25) * 2.0) / 3.0 ),
        //        (int)Math.Floor( CurrentDTF.Minute * 0.25 ),
        //        (int)Math.Floor( CurrentDTF.Minute * 0.5 ),
        //        (int)Math.Floor( CurrentDTF.Minute * 0.75 )
        //    };

        //    double minutesPerHour = (CurrentDTF.Hour / CurrentDTF.Minute);
        //    List<int> minutesInHour = new List<int>() {
        //        1,
        //        (int)Math.Floor( ((minutesPerHour * 0.25) * 1.0) / 3.0 ),
        //        (int)Math.Floor( ((minutesPerHour * 0.25) * 2.0) / 3.0 ),
        //        (int)Math.Floor( minutesPerHour * 0.25 ),
        //        (int)Math.Floor( minutesPerHour * 0.5 ),
        //        (int)Math.Floor( minutesPerHour * 0.75 )
        //    };

        //    double hoursPerDay = 
        //}


        // Minutes / Hour

        // Hour / Day

        // Day / Year




        
        #endregion
    }
}

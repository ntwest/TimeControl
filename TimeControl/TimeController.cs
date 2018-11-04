using UnityEngine;
using System.Collections;
using KSP.UI.Dialogs;
using System.Linq;

namespace TimeControl
{

    [KSPAddon( KSPAddon.Startup.Instantly, true )]
    internal class TimeController : MonoBehaviour
    {
        #region Singleton
        public static TimeController Instance { get; private set; }
        public static bool IsReady { get; private set; } = false;        
        #endregion

        #region Properties and fields  
        internal double CurrentUT
        {
            get => Planetarium.GetUniversalTime();
        }

        internal static float MaximumDeltaTimeMin
        {
            get
            {
                return 0.02f;
            }
        }

        internal static float MaximumDeltaTimeMax
        {
            get
            {
                return 0.35f;
            }
        }

        internal CelestialBody CurrentGameSOI
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

        internal float TimeScale
        {
            get => Time.timeScale;
            set
            {
                if (Time.timeScale != value)
                {
                    Time.timeScale = value;
                    TimeControlEvents.OnTimeControlTimeScaleChanged?.Fire( Time.timeScale );
                }
            }
        }

        private float defaultFixedDeltaTime;
        internal float DefaultFixedDeltaTime
        {
            get => defaultFixedDeltaTime;
        }

        internal float FixedDeltaTime
        {
            get => Time.fixedDeltaTime;
            set
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

        internal float MaximumDeltaTime
        {
            get => Time.maximumDeltaTime;
            set
            {
                Time.maximumDeltaTime = value;
            }
        }

        internal bool SupressFlightResultsDialog
        {
            get => HighLogic.CurrentGame?.Parameters?.CustomParams<TimeControlParameterNode>()?.SupressFlightResultsDialog ?? false;
        }

        internal IDateTimeFormatter CurrentDTF
        {
            get => KSPUtil.dateTimeFormatter;
        }

        internal KACWrapper.KACAPI.KACAlarm ClosestKACAlarm
        {
            get;
            private set;
        }
        
        private bool isTimeControlPaused;
        internal bool IsTimeControlPaused
        {
            get => isTimeControlPaused;
        }

        private float maximumDeltaTimeSetting = GameSettings.PHYSICS_FRAME_DT_LIMIT;
        internal float MaximumDeltaTimeSetting
        {
            get
            {
                return maximumDeltaTimeSetting;
            }

            set
            {
                if (!Mathf.Approximately( value, maximumDeltaTimeSetting ))
                {
                    // round to 2 decimal points, then clamp between min and max
                    float v = Mathf.Clamp( (Mathf.Round( value * 100f ) / 100f), MaximumDeltaTimeMin, MaximumDeltaTimeMax );
                    maximumDeltaTimeSetting = v;
                    MaximumDeltaTime = v;
                    GameSettings.PHYSICS_FRAME_DT_LIMIT = v;
                    GameSettings.SaveSettings();
                }
            }
        }

        private bool pauseOnNextFixedUpdate = false;
        internal bool PauseOnNextFixedUpdate
        {
            get
            {
                return pauseOnNextFixedUpdate;
            }

            set
            {
                if (pauseOnNextFixedUpdate != value)
                {
                    pauseOnNextFixedUpdate = value;
                }
            }
        }

        #endregion Properties

        #region MonoBehavior
        private void Awake()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( Awake );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                DontDestroyOnLoad( this );
                Instance = this;
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
                GameEvents.onGamePause.Add( this.onGamePause );
                GameEvents.onGameUnpause.Add( this.onGameUnpause );

                Log.Info( nameof( TimeController ) + " is Ready!", logBlockName );
                IsReady = true;
            }
        }
        #region Update Functions
        private void FixedUpdate()
        {
            if (PauseOnNextFixedUpdate)
            {
                Pause();
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
                TimeControlEvents.OnTimeControlTimePaused?.Fire( true );
            }
        }

        private void onGameUnpause()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( onGameUnpause );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                TimeControlEvents.OnTimeControlTimeUnpaused?.Fire( true );
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

                TimeControlEvents.OnTimeControlTimeUnpaused?.Fire( true );
            }
        }
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
                this.ResetMaximumDeltaTime();
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

        internal void ResetMaximumDeltaTime()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( ResetMaximumDeltaTime );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                MaximumDeltaTime = GameSettings.PHYSICS_FRAME_DT_LIMIT;
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

        public void Pause()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( Pause );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (!isTimeControlPaused)
                {                    
                    Time.timeScale = 0f;
                    isTimeControlPaused = true;
                    TimeControlEvents.OnTimeControlTimePaused?.Fire( true );
                }
            }
        }

        public void Unpause()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( Unpause );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (isTimeControlPaused)
                {                    
                    Time.timeScale = 1f;
                    isTimeControlPaused = false;
                    TimeControlEvents.OnTimeControlTimeUnpaused?.Fire( true );
                }
            }
        }

        public void TogglePause()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( TogglePause );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (this.isTimeControlPaused)
                {
                    this.Unpause();
                }
                else
                {
                    this.Pause();
                }
            }
        }

        public void IncrementTimeStep()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( IncrementTimeStep );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                this.Unpause();
                this.PauseOnNextFixedUpdate = true;
            }
        }
        
        public void GoRealTime()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( GoRealTime );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Unpause();
                PauseOnNextFixedUpdate = false;
                RailsWarpController.Instance?.DeactivateRails();
                HyperWarpController.Instance?.DeactivateHyper();
                SlowMoController.Instance?.DeactivateSlowMo();                
                ResetTime();
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

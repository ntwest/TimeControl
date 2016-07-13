using System;
using System.Reflection;
using UnityEngine;
using KSPPluginFramework;
using System.Collections;
using System.Collections.Generic;
using KSP.UI.Dialogs;

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
        private static TimeController instance;
        internal static TimeController Instance { get { return instance; } }
        #endregion

        #region MonoBehavior
        private void Awake()
        {
            DontDestroyOnLoad( this );
            instance = this;
        }
        private void Start()
        {
            GameEvents.onGamePause.Add( onGamePause );
            GameEvents.onGameUnpause.Add( onGameUnpause );
            GameEvents.onGameSceneLoadRequested.Add( onGameSceneLoadRequested );
            GameEvents.onTimeWarpRateChanged.Add( onTimeWarpRateChanged );
            GameEvents.onFlightReady.Add( onFlightReady );
            GameEvents.onPartDestroyed.Add( onPartDestroyed );
            GameEvents.onVesselDestroy.Add( onVesselDestroy );
            GameEvents.onVesselGoOffRails.Add( onVesselGoOffRails );
            GameEvents.onPlanetariumTargetChanged.Add( onPlanetariumTargetChanged );
            GameEvents.onVesselSOIChanged.Add( onVesselSOIChanged );
            GameEvents.onLevelWasLoaded.Add( onLevelWasLoaded );

            FlightCamera[] cams = FlightCamera.FindObjectsOfType( typeof( FlightCamera ) ) as FlightCamera[];
            cam = cams[0];

            StartCoroutine( StartAfterSettingsReady() );
        }

        /// <summary>
        /// Configures the Time Controller once the Settings have been loaded
        /// </summary>
        public IEnumerator StartAfterSettingsReady()
        {
            while (Settings.Instance == null || !Settings.Instance.IsReady)
                yield return null;

            Settings.Instance.PropertyChanged += SettingsPropertyChanged;

            UpdateInternalTimeWarpArrays();

            IsReady = true;

            yield break;
        }

        private void SettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

        }

        #region Update Functions

        private void Update()
        {
            // Don't do anything until the settings are loaded
            if (Settings.Instance == null || !Settings.Instance.IsReady)
                return;

            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                CurrentWarpState = TimeControllable.None;
                IsOperational = false;
                return;
            }

            if (timeWarp == null)
            {
                timeWarp = TimeWarp.fetch;
                if (timeWarp == null)
                    return;
            }

            UpdateRails();

            bool canWarp = (CurrentWarpState == TimeControllable.None || CurrentWarpState == TimeControllable.Physics || CurrentWarpState == TimeControllable.Rails);

            HighLogic.CurrentGame.Parameters.Flight.CanTimeWarpHigh = canWarp;
            HighLogic.CurrentGame.Parameters.Flight.CanTimeWarpLow = canWarp;

            if (!(TimeWarp.CurrentRate > 1))
                Planetarium.fetch.fixedDeltaTime = Time.fixedDeltaTime;

            if (IsOperational)
            {
                switch (CurrentWarpState)
                {
                    case TimeControllable.SlowMo:
                        UpdateSlowMo();
                        break;
                    case TimeControllable.Hyper:
                        UpdateHyper();
                        break;
                    case TimeControllable.None:
                        CurrentControllerMessage = "";
                        break;
                }

                UpdatePaused();
            }
            else
            {
                SmoothSlider = timeSlider;
            }
        }

        private void UpdateRails()
        {
            if (timeWarp.current_rate_index > 0)
            {
                if (CurrentWarpState == TimeControllable.Hyper)
                    CancelHyperWarp();

                if (CurrentWarpState == TimeControllable.SlowMo)
                    CancelSlowMo();

                if (timeWarp.Mode == TimeWarp.Modes.LOW)
                {
                    CurrentWarpState = TimeControllable.Physics;
                }
                else if (timeWarp.Mode == TimeWarp.Modes.HIGH)
                {
                    CurrentWarpState = TimeControllable.Rails;
                    if (timeWarp.current_rate_index >= Settings.Instance.WarpLevels)
                        TimeWarp.SetRate( Settings.Instance.WarpLevels - 1, false );
                }
            }
            else
            {
                if (CurrentWarpState == TimeControllable.Rails || CurrentWarpState == TimeControllable.Physics)
                    CancelRailsWarp();
            }
        }
        private void UpdateHyper()
        {
            Time.timeScale = Mathf.Round( HyperMaxRate );
            Time.fixedDeltaTime = defaultDeltaTime * HyperMinPhys;

            CurrentControllerMessage = ("Time Control Hyper Warp: " + Math.Round( PerformanceManager.ptr, 1 ) + "x");
        }
        private void UpdateSlowMo()
        {
            UpdateSlowMoFPSKeeper();

            if (TimeSlider == 0f)
            {
                CancelSlowMo();
                return;
            }

            SmoothSlider = Mathf.Lerp( SmoothSlider, TimeSlider, .01f ); // TCUtilitieslinearInterpolate(SmoothSlider, timeSlider, .01f);
            float inverseTimeScale = PluginUtilities.convertToExponential( SmoothSlider );
            // InverseTimeScale property is modified when SmoothSlider is changed
            Time.timeScale = 1f / inverseTimeScale;
            Time.fixedDeltaTime = DeltaLocked ? defaultDeltaTime : defaultDeltaTime * (1f / inverseTimeScale);
            CurrentControllerMessage = ("SLOW-MOTION");
        }
        private void UpdateSlowMoFPSKeeper()
        {
            if (!IsFpsKeeperActive)
                return;

            fpsMin = (int)Mathf.Round( Settings.Instance.FpsMinSlider / 5 ) * 5;
            if (Mathf.Abs( PerformanceManager.fps - fpsMin ) > 2.5)
            {
                if (PerformanceManager.fps < fpsMin)
                    fpsKeeperFactor += 1;
                else
                    fpsKeeperFactor -= 1;
            }
            fpsKeeperFactor = Mathf.Clamp( fpsKeeperFactor, 0, 73 ); //0-10 are .01 steps down with max delta, 11-74 are steps of time scale to 1/64x

            if (fpsKeeperFactor < 11)
            {
                TimeSlider = 0f;
                Settings.Instance.MaxDeltaTimeSlider = .12f - (fpsKeeperFactor * .01f);
            }
            else
            {
                Settings.Instance.MaxDeltaTimeSlider = 0.02f;
                TimeSlider = (float)(fpsKeeperFactor - 10) / 64f;
            }
        }        
        private void UpdatePaused()
        {
            if (TimePaused)
            {
                Time.timeScale = 0f;
                CurrentControllerMessage = ("PAUSED");
            }
            else if (CurrentWarpState == TimeControllable.None || CurrentWarpState == TimeControllable.Physics || CurrentWarpState == TimeControllable.Rails)
            {
                resetTime();
            }
        }


        #endregion

        #region FixedUpdate Functions

        private void FixedUpdate()
        {
            if (CurrentWarpState == TimeControllable.Hyper)
                FixedUpdateHyper();

            if (PauseOnNextFixedUpdate)
            {
                TimePaused = true;
                PauseOnNextFixedUpdate = false;
            }
        }

        private void FixedUpdateHyper()
        {
            if (Planetarium.GetUniversalTime() >= hyperWarpEndTime)
            {
                CancelHyperWarp();
                if (HyperPauseOnTimeReached)
                    PauseOnNextFixedUpdate = true;
            }
        }

        #endregion

        #endregion

        #region GameEvents
        private void onFlightReady()
        {
            CurrentWarpState = TimeControllable.None;
            udpateSOI();
            TimeSlider = 0f;
            IsOperational = true;
        }
        private void onGamePause()
        {
            IsOperational = false;
        }
        private void onGameUnpause()
        {
            IsOperational = true;
        }
        private void onGameSceneLoadRequested(GameScenes gs)
        {
            string logCaller = "onGameSceneLoadRequested";
            Log.Trace( "method start", logCaller );

            switch (CurrentWarpState)
            {
                case TimeControllable.Rails:
                case TimeControllable.Physics:
                    CancelRailsWarp();
                    break;
                case TimeControllable.SlowMo:
                    CancelSlowMo();
                    break;
                case TimeControllable.Hyper:
                    CancelHyperWarp();
                    break;
            }

            IsOperational = false;
            CurrentWarpState = TimeControllable.None;

            Log.Trace( "method end", logCaller );
        }
        private void onPartDestroyed(Part p)
        {
            if (CurrentWarpState == TimeControllable.Hyper && HighLogic.LoadedSceneIsFlight && (FlightGlobals.ActiveVessel == null || p.vessel == FlightGlobals.ActiveVessel))
                CancelHyperWarp();
        }
        private void onVesselDestroy(Vessel v)
        {
            if (CurrentWarpState == TimeControllable.Hyper && HighLogic.LoadedSceneIsFlight && (FlightGlobals.ActiveVessel == null || v == FlightGlobals.ActiveVessel))
                CancelHyperWarp();
        }
        private void onTimeWarpRateChanged()
        {
            if (TimeWarp.CurrentRateIndex > 0)
            {
                IsOperational = false;
            }
            else
            {
                IsOperational = true;
            }
        }
        private void onVesselGoOffRails(Vessel v)
        {
        }
        private void onPlanetariumTargetChanged(MapObject mo)
        {
            udpateSOI();
        }
        private void onVesselSOIChanged(GameEvents.HostedFromToAction<Vessel, CelestialBody> hfta)
        {
            udpateSOI();
        }
        private void onLevelWasLoaded(GameScenes gs)
        {
            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                CurrentWarpState = TimeControllable.None;
                IsOperational = false;
            }
            else
            {
                IsOperational = true;
                udpateSOI();
            }
        }
        #endregion

        #region Properties

        #region Read-Only Properties
        public float TruePOS {
            get {
                return PluginUtilities.convertToExponential( timeSlider );
            }
        }

        public string CurrentRailsWarpRateText {
            get {
                if (CurrentWarpState == TimeControllable.Rails)
                {
                    return Settings.Instance.CustomWarpRates[timeWarp.current_rate_index].WarpRate;
                }
                else if (CurrentWarpState == TimeControllable.Physics)
                {
                    return (timeWarp.current_rate_index + 1).ToString();
                }
                else return "0";
            }
        }
        #endregion

        #region Read-Only Private Set Properties
        public string CurrentControllerMessage {
            get {
                return currentControllerMessage;
            }
            private set {
                if (currentControllerMessage != value)
                {
                    currentControllerMessage = value;
                }
            }
        }
        public bool IsReady { get; private set; } 
        public CelestialBody CurrentSOI {
            get {
                return currentSOI;
            }
            private set {
                if (currentSOI != value)
                {
                    currentSOI = value;
                }
            }
        }
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
        public bool TimePaused {
            get {
                return timePaused;
            }
            private set {
                if (timePaused != value)
                {
                    timePaused = value;
                }
            }
        }
        public TimeControllable CurrentWarpState {
            get {
                return currentWarpState;
            }
            private set {
                if (currentWarpState != value)
                {
                    currentWarpState = value;
                }
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
        public bool IsFpsKeeperActive {
            get {
                return fpsKeeperActive;
            }
            private set {
                if (fpsKeeperActive != value)
                {
                    fpsKeeperActive = value;
                }
            }
        }
        public bool IsOperational {
            get {
                return operational;
            }
            private set {
                if (operational != value)
                {
                    operational = value;
                }
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
        public float SmoothSlider {
            get {
                return smoothSlider;
            }

            set {
                if (smoothSlider != value)
                {
                    smoothSlider = value;                    
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
        public bool HyperPauseOnTimeReached {
            get {
                return hyperPauseOnTimeReached;
            }

            set {
                if (hyperPauseOnTimeReached != value)
                {
                    hyperPauseOnTimeReached = value;
                }
            }
        }

        public float HyperMinPhys {
            get {
                return hyperMinPhys;
            }

            set {
                if (hyperMinPhys != value)
                {
                    hyperMinPhys = Mathf.Clamp( value, 1f, 6f ); ;
                }
            }
        }

        public float HyperMaxRate {
            get {
                return hyperMaxRate;
            }

            set {
                if (hyperMaxRate != value)
                {
                    hyperMaxRate = Mathf.Clamp(value, 2f, 100f);
                }
            }
        }
        #endregion

        #endregion

        #region Private Methods
        private void udpateSOI()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                CurrentSOI = FlightGlobals.currentMainBody;
            }
            else if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                CurrentSOI = FlightGlobals.GetHomeBody();  //Kerbin
            }
            else if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                if (PlanetariumCamera.fetch == null || PlanetariumCamera.fetch.target == null || PlanetariumCamera.fetch.target.type == MapObject.ObjectType.Null)
                {
                    CurrentSOI = FlightGlobals.GetHomeBody(); // kerbin
                }
                if (PlanetariumCamera.fetch.target.type == MapObject.ObjectType.CelestialBody)
                    CurrentSOI = PlanetariumCamera.fetch.target.celestialBody;
                else if (PlanetariumCamera.fetch.target.type == MapObject.ObjectType.Vessel)
                    CurrentSOI = PlanetariumCamera.fetch.target.vessel.mainBody;
            }
            else
            {
                CurrentSOI = FlightGlobals.GetHomeBody(); //Kerbin
            }
        }
        private void resetTime()
        {
            TimeSlider = 0f;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultDeltaTime;
        }
        #endregion

        #region Public Methods        
        #region Pause
        public void TogglePause()
        {
            TimePaused = !TimePaused;
        }
        public void Pause()
        {
            TimePaused = true;
        }
        public void Unpause()
        {
            TimePaused = false;
        }
        public void IncrementTimeStep()
        {
            TimePaused = false;
            PauseOnNextFixedUpdate = true;
        }
        public void Realtime()
        {
            switch (CurrentWarpState)
            {
                case TimeControllable.None:
                    break;
                case TimeControllable.Rails:
                case TimeControllable.Physics:
                    CancelRailsWarp();
                    break;
                case TimeControllable.Hyper:
                    CancelHyperWarp();
                    break;
                case TimeControllable.SlowMo:
                    CancelSlowMo();
                    break;
            }
        }


        #endregion  
              
        #region Rails Warp
        public void UpdateInternalTimeWarpArrays()
        {
            string logCaller = "TimeController.UpdateInternalTimeWarpArrays";
            Log.Trace( "method start", logCaller );

            int levels = Settings.Instance.WarpLevels;

            if (timeWarp.warpRates.Length != levels)
                Array.Resize( ref timeWarp.warpRates, levels );

            foreach (CelestialBody c in FlightGlobals.Bodies)
                if (c.timeWarpAltitudeLimits.Length != levels)
                    Array.Resize( ref c.timeWarpAltitudeLimits, levels );

            for (int i = 0; i < levels; i++)
            {
                timeWarp.warpRates[i] = Settings.Instance.CustomWarpRates[i].WarpRateInt;

                foreach (CelestialBody cb in FlightGlobals.Bodies)
                {
                    cb.timeWarpAltitudeLimits[i] = Settings.Instance.CustomAltitudeLimits[cb][i].AltitudeLimitInt;
                }
            }
            Log.Trace( "method end", logCaller );
        }

        public bool RailsWarpToTime(double warpTime)
        {
            string logCaller = "TimeController.RailsWarpToTime";
            Log.Trace( "method start", logCaller );

            if (CurrentWarpState == TimeControllable.Hyper)
            {
                CancelHyperWarp();
            }
            if (CurrentWarpState == TimeControllable.SlowMo)
            {
                CancelSlowMo();
            }

            if (warpTime > Planetarium.GetUniversalTime() && (warpTime - Planetarium.GetUniversalTime()) > Settings.Instance.CustomWarpRates[1].WarpRateInt)
            {
                Log.Info( "Auto warping to time " + warpTime, logCaller );
                CurrentWarpState = TimeControllable.Rails;
                timeWarp.WarpTo( warpTime );
                return true;
            }
            else
            {
                Log.Info( "Time " + warpTime + " has already passed. Cannot warp to it.", logCaller );
                return false;
            }
        }

        public bool RailsWarpForDuration(string warpYears = "0", string warpDays = "0", string warpHours = "0", string warpMinutes = "0", string warpSeconds = "0")
        {
            string logCaller = "TimeController.RailsWarpForDuration";
            Log.Trace( "method start", logCaller );

            int years;
            if (!int.TryParse( warpYears, out years ))
            {
                Log.Warning( "Cannot parse warpYears as an integer", logCaller );
                Log.Trace( "method end", logCaller );
                return false;
            }

            int days;
            if (!int.TryParse( warpDays, out days ))
            {
                Log.Warning( "Cannot parse warpDays as an integer", logCaller );
                Log.Trace( "method end", logCaller );
                return false;
            }

            int hours;
            if (!int.TryParse( warpHours, out hours ))
            {
                Log.Warning( "Cannot parse warpHours as an integer", logCaller );
                Log.Trace( "method end", logCaller );
                return false;
            }

            int minutes;
            if (!int.TryParse( warpMinutes, out minutes ))
            {
                Log.Warning( "Cannot parse warpMinutes as an integer", logCaller );
                Log.Trace( "method end", logCaller );
                return false;
            }

            int seconds;
            if (!int.TryParse( warpSeconds, out seconds ))
            {
                Log.Warning( "Cannot parse warpSeconds as an integer", logCaller );
                Log.Trace( "method end", logCaller );
                return false;
            }

            double warpTime;

            if (GameSettings.KERBIN_TIME)
                warpTime = years * 9201600 + days * 21600 + hours * 3600 + minutes * 60 + seconds + Planetarium.GetUniversalTime();
            else
                warpTime = years * 31536000 + days * 86400 + hours * 3600 + minutes * 60 + seconds + Planetarium.GetUniversalTime();
            
            bool result = RailsWarpToTime( warpTime );

            Log.Trace( "method end", logCaller );
            return result;
        }

        public void CancelRailsWarp()
        {
            string logCaller = "TimeController.CancelWarp";
            Log.Trace( "method start", logCaller );
            
            if (!(CurrentWarpState == TimeControllable.Rails || CurrentWarpState == TimeControllable.Physics))
            {
                if (TimeWarp.fetch != null && TimeWarp.fetch.current_rate_index > 0)
                {
                    Log.Warning( "Rails warp is running but TimeController thinks we are on warp type " + CurrentWarpState.ToString(), logCaller );
                    CancelHyperWarp();
                    CancelSlowMo();
                    CurrentWarpState = TimeControllable.Rails;
                }
                else
                {
                    return;
                }
            }
            
            Log.Info( "Cancelling auto warp if it is running.", logCaller );
            TimeWarp.fetch.CancelAutoWarp();

            Log.Info( "Setting warp rate to 0.", logCaller );
            TimeWarp.SetRate( 0, false );
            CurrentWarpState = TimeControllable.None;

            Log.Trace( "method end", logCaller );
        }

        #endregion
        #region Slow Motion and FPS Keeper
        public bool TrySetSlowMo()
        {
            if (CurrentWarpState == TimeControllable.None || CurrentWarpState == TimeControllable.SlowMo)
            {
                CurrentWarpState = TimeControllable.SlowMo;
                return true;
            }
            else
                return false;
        }
        public bool CheckSlowMo()
        {
            if (CurrentWarpState == TimeControllable.SlowMo)
            {
                if (TimeSlider == 0f)
                {
                    CurrentWarpState = TimeControllable.None;
                    return false;
                }
                return true;
            }
            return false;            
        }

        public void CancelSlowMo()
        {
            if (CurrentWarpState != TimeControllable.SlowMo)
                return;

            resetTime();            
            CurrentWarpState = TimeControllable.None;
        }
        public void SpeedUpTime()
        {
            UpdateTimeSlider( TimeSlider - 0.01f );
        }
        public void SlowDownTime()
        {
            UpdateTimeSlider( TimeSlider + 0.01f );
        }
        public void UpdateTimeSlider(float ts)
        {
            if (!TrySetSlowMo())
                return;
            TimeSlider = Mathf.Clamp01( ts );
            CheckSlowMo();
        }

        public void SetFPSKeeper(bool v)
        {
            if (!TrySetSlowMo())
                return;
            IsFpsKeeperActive = v;
            CheckSlowMo();
        }

        public void ToggleFPSKeeper()
        {
            if (!TrySetSlowMo())
                return;
            IsFpsKeeperActive = !IsFpsKeeperActive;
            CheckSlowMo();
        }

        #endregion
        #region Hyper Warp
        public bool HyperWarpToTime(double warpTime)
        {
            string logCaller = "TimeController.HyperWarpToTime";
            Log.Trace( "method start", logCaller );

            if (!(CurrentWarpState == TimeControllable.None || CurrentWarpState == TimeControllable.Hyper))
            {
                Log.Info( "Cannot hyper warp while time warp state is " + CurrentWarpState.ToString(), logCaller );
                return false;
            }

            if (Planetarium.GetUniversalTime() >= warpTime)
                return false;

            hyperWarpEndTime = warpTime;
            CurrentWarpState = TimeControllable.Hyper;

            Log.Trace( "method end", logCaller );
            return true;
        }
        public bool HyperWarpForDuration(string warpHours, string warpMinutes, string warpSeconds)
        {
            string logCaller = "TimeController.HyperWarpForDuration";
            Log.Trace( "method start", logCaller );

            int hours;
            if (!int.TryParse( warpHours, out hours ))
            {
                Log.Warning( "Cannot parse warpHours as an integer", logCaller );
                Log.Trace( "method end", logCaller );
                return false;
            }

            int minutes;
            if (!int.TryParse( warpMinutes, out minutes ))
            {
                Log.Warning( "Cannot parse warpMinutes as an integer", logCaller );
                Log.Trace( "method end", logCaller );
                return false;
            }

            int seconds;
            if (!int.TryParse( warpSeconds, out seconds ))
            {
                Log.Warning( "Cannot parse warpSeconds as an integer", logCaller );
                Log.Trace( "method end", logCaller );
                return false;
            }

            double warpTime = (hours * 3600) + (minutes * 60) + seconds + Planetarium.GetUniversalTime();

            bool result = HyperWarpToTime( warpTime );
            Log.Trace( "method end", logCaller );
            return result;
        }
        public void ToggleHyperWarp()
        {
            if (!(CurrentWarpState == TimeControllable.None || CurrentWarpState == TimeControllable.Hyper))
                return;
            
            if (CurrentWarpState == TimeControllable.Hyper)
            {
                CancelHyperWarp();
            }
            else
            {
                CurrentWarpState = TimeControllable.Hyper;
            }
        }
        public void CancelHyperWarp()
        {
            if (CurrentWarpState != TimeControllable.Hyper)
                return;

            resetTime();
            hyperWarpEndTime = Mathf.Infinity;
            CurrentWarpState = TimeControllable.None;
        }
        #endregion
        
        #endregion



        #region fields

        private TimeControllable currentWarpState = TimeControllable.None;

        //PHYSICS
        private float defaultDeltaTime = Time.fixedDeltaTime; //0.02
        private float timeSlider = 0f;
        private Boolean timePaused;
        private Boolean pauseOnNextFixedUpdate = false;
        private float smoothSlider = 0f;
        private Boolean deltaLocked = false;
        private int fpsMin = 5;
        private int fpsKeeperFactor = 0;
        private bool fpsKeeperActive;
        private Boolean operational;

        private CelestialBody currentSOI;

        private string currentControllerMessage;

        private FlightCamera cam;

        //HYPERWARP   
        private Boolean hyperPauseOnTimeReached = false;
        private double hyperWarpEndTime = Mathf.Infinity;
        private float hyperMinPhys = 1f;
        private float hyperMaxRate = 2f;
        private TimeWarp timeWarp;
        
        #endregion

    }
}

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
        public static bool IsReady { get; private set; }
        private static TimeController instance;
        public static TimeController Instance { get { return instance; } }
        #endregion

        #region MonoBehavior
        private void Awake()
        {
            DontDestroyOnLoad( this );
            instance = this;
        }
        private void Start()
        {
            string logCaller = "TimeController.Start";
            Log.Trace( "method start", logCaller );

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

            //FlightCamera[] cams = FlightCamera.FindObjectsOfType( typeof( FlightCamera ) ) as FlightCamera[];
            //cam = cams[0];

            StartCoroutine( StartAfterSettingsReady() );

            Log.Trace( "method end", logCaller );
        }

        /// <summary>
        /// Configures the Time Controller once the Settings have been loaded
        /// </summary>
        public IEnumerator StartAfterSettingsReady()
        {
            string logCaller = "TimeController.StartAfterSettingsReady";
            Log.Trace( "coroutine start", logCaller );

            while (!Settings.IsReady)
                yield return null;

            Log.Info( "Wiring Up Settings Property Changed Event Subscription", logCaller );
            Settings.Instance.PropertyChanged += SettingsPropertyChanged;

            UpdateInternalTimeWarpArrays();

            Log.Info( "TimeController.Instance is Ready!", logCaller );
            IsReady = true;

            Log.Trace( "coroutine yield break", logCaller );
            yield break;
        }

        private void SettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

        }

        #region Update Functions

        private void Update()
        {
            // Don't do anything until the settings are loaded
            if (!Settings.IsReady)
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
                    CurrentWarpState = TimeControllable.None;
                Planetarium.fetch.fixedDeltaTime = Time.fixedDeltaTime;
            }

            bool canWarp = (CurrentWarpState == TimeControllable.None || CurrentWarpState == TimeControllable.Physics || CurrentWarpState == TimeControllable.Rails);
            HighLogic.CurrentGame.Parameters.Flight.CanTimeWarpHigh = canWarp;
            HighLogic.CurrentGame.Parameters.Flight.CanTimeWarpLow = canWarp;
        }
        private void UpdateHyper()
        {
            if (CurrentWarpState != TimeControllable.Hyper)
                return;

            Time.timeScale = Mathf.Round( HyperMaxRate );
            Time.fixedDeltaTime = defaultDeltaTime * HyperMinPhys;
            Planetarium.fetch.fixedDeltaTime = Time.fixedDeltaTime;

            CurrentControllerMessage = ("HYPER-WARP " + Math.Round( PerformanceManager.ptr, 1 ) + "x");
        }
        private void UpdateSlowMo()
        {
            if (CurrentWarpState != TimeControllable.SlowMo)
                return;

            UpdateSlowMoFPSKeeper();

            if (TimeSlider == 0f)
            {
                CancelSlowMo();
                return;
            }

            SmoothSlider = Mathf.Lerp( SmoothSlider, TimeSlider, .01f );
            float inverseTimeScale = PluginUtilities.convertToExponential( SmoothSlider );
            // InverseTimeScale property is modified when SmoothSlider is changed
            Time.timeScale = 1f / inverseTimeScale;
            Time.fixedDeltaTime = DeltaLocked ? defaultDeltaTime : defaultDeltaTime * (1f / inverseTimeScale);
            Planetarium.fetch.fixedDeltaTime = Time.fixedDeltaTime;

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
                MaxDeltaTimeSlider = .12f - (fpsKeeperFactor * .01f);
            }
            else
            {
                MaxDeltaTimeSlider = 0.02f;
                TimeSlider = (float)(fpsKeeperFactor - 10) / 64f;
            }
        }
        private void UpdatePaused()
        {
            if (TimePaused)
            {
                if (Time.timeScale != 0f)
                    Time.timeScale = 0f;
                CurrentControllerMessage = ("PAUSED");
            }
        }


        #endregion

        #region FixedUpdate Functions

        private void FixedUpdate()
        {
            if (CurrentWarpState == TimeControllable.Hyper)
                FixedUpdateHyper();

            if (CurrentWarpState == TimeControllable.Rails)
                FixedUpdateRails();

            if (PauseOnNextFixedUpdate && TimeWarp.CurrentRateIndex == 0)
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

        private void FixedUpdateRails()
        {
            // Return if we aren't warping to a specific time
            if (CurrentWarpState != TimeControllable.Rails)
            {
                railsWarpEndTime = Mathf.Infinity;
            }
            if (railsWarpEndTime == Mathf.Infinity)
            {
                return;
            }
            double UT = Planetarium.GetUniversalTime();

            // If we've gone past or equal to the time we are warping to, cancel and return
            if (UT >= railsWarpEndTime)
            {
                Log.Warning( String.Format( "UT {0} is past railsWarpEndTime {1}", UT, railsWarpEndTime ), "TimeController.FixedUpdateRails" );
                CancelRailsWarp();
                if (RailsPauseOnTimeReached)
                    PauseOnNextFixedUpdate = true;
                return;
            }

            // Otherwise, speed up or slow down depending on how much longer we have to go
            // TODO - speed up code (for perhaps a combination hyper / rails warper)

            // Since we do this check each fixed update, we can have faster warp rates..? Looks like 5x seems to work well.
            // TODO - make this some kind of setting perhaps?
            int r = TimeWarp.CurrentRateIndex;
            while (r > 0 && (Settings.Instance.CustomWarpRates[r].WarpRateInt > ((railsWarpEndTime - UT) * 5)))
            {
                r -= 1;
            }
            if (r == TimeWarp.CurrentRateIndex)
                return;

            if (r == 0)
            {
                railsWarpEndTime = Mathf.Infinity;
                CancelRailsWarp();
                if (RailsPauseOnTimeReached)
                    PauseOnNextFixedUpdate = true;
            }
            else
            {
                TimeWarp.fetch.Mode = TimeWarp.Modes.HIGH;
                TimeWarp.SetRate( r, false, true );
            }
        }


        #endregion

        #endregion

        #region GameEvents
        private void onFlightReady()
        {
            string logCaller = "TimeController.onFlightReady";
            Log.Trace( "method start (event)", logCaller );

            CurrentWarpState = TimeControllable.None;

            if (CanControlWarpType != TimeControllable.None)
                UpdateInternalTimeWarpArrays();

            udpateSOI();
            TimeSlider = 0f;
            IsOperational = true;

            Log.Trace( "method end (event)", logCaller );
        }
        private void onGamePause()
        {
            string logCaller = "TimeController.onGamePause";
            Log.Trace( "method start (event)", logCaller );

            IsOperational = false;

            Log.Trace( "method end (event)", logCaller );
        }
        private void onGameUnpause()
        {
            string logCaller = "TimeController.onGameUnpause";
            Log.Trace( "method start (event)", logCaller );

            IsOperational = true;

            Log.Trace( "method end (event)", logCaller );
        }
        private void onGameSceneLoadRequested(GameScenes gs)
        {
            string logCaller = "onGameSceneLoadRequested";
            Log.Trace( "method start (event)", logCaller );

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

            resetTime();

            IsOperational = false;
            CurrentWarpState = TimeControllable.None;

            Log.Trace( "method end (event)", logCaller );
        }
        private void onPartDestroyed(Part p)
        {
            string logCaller = "onPartDestroyed";
            Log.Trace( "method start (event)", logCaller );

            if (CurrentWarpState == TimeControllable.Hyper
                && HighLogic.LoadedSceneIsFlight
                && p.vessel != null
                && FlightGlobals.ActiveVessel != null
                && p.vessel == FlightGlobals.ActiveVessel
                )
            {
                Log.Info("Part on Active Vessel Destroyed. Cancelling Hyper or Rails Warp if they are on.", logCaller);
                CancelHyperWarp();
                CancelRailsWarp();
            }

            Log.Trace( "method end (event)", logCaller );
        }
        private void onVesselDestroy(Vessel v)
        {
            string logCaller = "onVesselDestroy";
            Log.Trace( "method start (event)", logCaller );

            if (CurrentWarpState == TimeControllable.Hyper
                && HighLogic.LoadedSceneIsFlight
                && (FlightGlobals.ActiveVessel == null
                    || v == FlightGlobals.ActiveVessel)
                )
            {
                Log.Info("Active Vessel Destroyed. Cancelling Hyper or Rails Warp if they are on.", logCaller);
                CancelHyperWarp();
                CancelRailsWarp();
            }

            Log.Trace( "method end (event)", logCaller );
        }
        private void onTimeWarpRateChanged()
        {
            string logCaller = "onTimeWarpRateChanged";
            Log.Trace( "method start (event)", logCaller );

            if (TimeWarp.CurrentRateIndex > 0)
            {
                IsOperational = false;
            }
            else
            {
                IsOperational = true;
            }

            Log.Trace( "method end (event)", logCaller );
        }
        private void onVesselGoOffRails(Vessel v)
        {
            string logCaller = "onVesselGoOffRails";
            Log.Trace( "method start (event)", logCaller );
            Log.Trace( "method end (event)", logCaller );
        }
        private void onPlanetariumTargetChanged(MapObject mo)
        {
            string logCaller = "onPlanetariumTargetChanged";
            Log.Trace( "method start (event)", logCaller );
            udpateSOI();
            Log.Trace( "method end (event)", logCaller );
        }
        private void onVesselSOIChanged(GameEvents.HostedFromToAction<Vessel, CelestialBody> hfta)
        {
            string logCaller = "onVesselSOIChanged";
            Log.Trace( "method start (event)", logCaller );
            udpateSOI();
            Log.Trace( "method end (event)", logCaller );
        }
        private void onLevelWasLoaded(GameScenes gs)
        {
            string logCaller = "onLevelWasLoaded";
            Log.Trace( "method start (event)", logCaller );

            if (CanControlWarpType != TimeControllable.None)
                UpdateInternalTimeWarpArrays();

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
            Log.Trace( "method end (event)", logCaller );
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
                    string r = "";
                    // switch and return literal instead of generating new strings
                    // this way, we return interned strings.
                    switch (timeWarp.current_rate_index + 1)
                    {
                        case 1:
                            r = "1";
                            break;
                        case 2:
                            r = "2";
                            break;
                        case 3:
                            r = "3";
                            break;
                        case 4:
                            r = "4";
                            break;
                        default:
                            r = (timeWarp.current_rate_index + 1).ToString(); // If someone is messing with the physical warp rates, create garbage :-(
                            break;
                    }
                    return r;
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
                    if (value == false)
                    {
                        if (CurrentWarpState != TimeControllable.Hyper && CurrentWarpState != TimeControllable.SlowMo)
                            resetTime();
                    }
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
        public float MaxDeltaTimeSlider {
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

                    // Update the Settings. Will be automatically saved as needed
                    if (Settings.IsReady)
                        Settings.Instance.MaxDeltaTimeSlider = v;
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

        public bool RailsPauseOnTimeReached {
            get {
                return railsPauseOnTimeReached;
            }

            set {
                if (railsPauseOnTimeReached != value)
                {
                    railsPauseOnTimeReached = value;
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
                    hyperMinPhys = Mathf.Clamp( value, HyperMinPhysMin, HyperMinPhysMax ); ;
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
                    hyperMaxRate = Mathf.Clamp( value, HyperMaxRateMin, HyperMaxRateMax );
                }
            }
        }
        #endregion

        #endregion

        #region Private Methods
        private void udpateSOI()
        {
            string logCaller = "udpateSOI";
            Log.Trace( "method start", logCaller );

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

            Log.Trace( "method end", logCaller );
        }
        private void resetTime()
        {
            string logCaller = "resetTime";
            Log.Trace( "method start", logCaller );

            TimeSlider = 0f;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultDeltaTime;
            Planetarium.fetch.fixedDeltaTime = Time.fixedDeltaTime;
            timePaused = false;

            Log.Trace( "method end", logCaller );
        }
        #endregion

        #region Public Methods        
        #region Pause
        public void TogglePause()
        {
            string logCaller = "TogglePause";
            Log.Trace( "method start", logCaller );

            TimePaused = !TimePaused;

            Log.Trace( "method end", logCaller );
        }
        public void IncrementTimeStep()
        {
            string logCaller = "IncrementTimeStep";
            Log.Trace( "method start", logCaller );

            TimePaused = false;
            PauseOnNextFixedUpdate = true;

            Log.Trace( "method end", logCaller );
        }
        public void Realtime()
        {
            string logCaller = "Realtime";
            Log.Trace( "method start", logCaller );

            Log.Info( "Cancelling all warp types", logCaller );
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

            resetTime();
            Log.Trace( "method end", logCaller );
        }


        #endregion

        #region Rails Warp
        public void UpdateInternalTimeWarpArrays()
        {
            string logCaller = "TimeController.UpdateInternalTimeWarpArrays";
            Log.Trace( "method start", logCaller );

            if (CanControlWarpType == TimeControllable.None)
            {
                Log.Warning( "Tried to update internal time warp arrays, but we can't modify them right now", logCaller );
                Log.Trace( "method end", logCaller );
                return;
            }


            int levels = Settings.Instance.WarpLevels;

            Log.Info( "Resizing Internal  Warp Rates Array", logCaller );
            if (timeWarp.warpRates.Length != levels)
                Array.Resize( ref timeWarp.warpRates, levels );

            Log.Info( "Resizing Internal  Celestial Body Altitude Limits Arrays", logCaller );
            foreach (CelestialBody c in FlightGlobals.Bodies)
                if (c.timeWarpAltitudeLimits.Length != levels)
                    Array.Resize( ref c.timeWarpAltitudeLimits, levels );

            Log.Info( "Setting Internal Rates and Altitude Limits", logCaller );
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

            double UT = Planetarium.GetUniversalTime();
            double TimeDiff = warpTime - UT;

            if (HighLogic.LoadedSceneIsFlight && ((FlightGlobals.ActiveVessel.situation | Vessel.Situations.FLYING) == Vessel.Situations.FLYING))
            {
                // TODO Implement a way to rails warp until you hit atmo, then hyper warp, then rails warp again.
                Log.Info( "In atmosphere. Hyper warping instead.", logCaller );

                if (railsPauseOnTimeReached)
                {
                    railsPauseOnTimeReached = false;
                    hyperPauseOnTimeReached = true;
                }
                return HyperWarpToTime( warpTime );
            }

            if (CurrentWarpState == TimeControllable.Hyper)
            {
                Log.Info( "Currently hyperwarping. Cancelling this first.", logCaller );
                CancelHyperWarp();
            }
            if (CurrentWarpState == TimeControllable.SlowMo)
            {
                Log.Info( "Currently slow-motion. Cancelling this first.", logCaller );
                CancelSlowMo();
            }


            if (warpTime > UT && (warpTime - UT) > Settings.Instance.CustomWarpRates[1].WarpRateInt)
            {
                Log.Info( String.Format( "Auto warping to time {0} from time {1}", warpTime, UT ), logCaller );
                CurrentWarpState = TimeControllable.Rails;

                int r = Settings.Instance.CustomWarpRates.Count - 1;
                Log.Trace( String.Format( "Starting at rate {0} / {1}x", r, Settings.Instance.CustomWarpRates[r].WarpRateInt ), logCaller );
                while (r > 0 && (Settings.Instance.CustomWarpRates[r].WarpRateInt > TimeDiff * 5))
                {
                    Log.Trace( String.Format( "Time Difference: {0}", TimeDiff ), logCaller );
                    Log.Trace( String.Format( "Rate {0} / {1}x too fast, reducing r", r, Settings.Instance.CustomWarpRates[r].WarpRateInt ), logCaller );
                    r -= 1;
                }
                Log.Info( String.Format( "Warp Rate Starting At {0} / {1}x", r, Settings.Instance.CustomWarpRates[r].WarpRateInt ), logCaller );
                TimeWarp.fetch.Mode = TimeWarp.Modes.HIGH;
                TimeWarp.SetRate( r, true, true );
                railsWarpEndTime = warpTime;
                Log.Trace( "method end", logCaller );
                return true;
            }
            else
            {
                Log.Info( "Time " + warpTime + " has already passed or is sooner than the slowest rails warp rate. Cannot warp to it.", logCaller );
                Log.Trace( "method end", logCaller );
                return false;
            }
        }

        public bool AutoWarpForDuration(string warpYears = "0", string warpDays = "0", string warpHours = "0", string warpMinutes = "0", string warpSeconds = "0")
        {
            string logCaller = "TimeController.AutoWarpForDuration";
            Log.Trace( "method start", logCaller );

            if (TimePaused)
            {
                timePaused = false;
            }

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

            double warpTime = years * KSPUtil.dateTimeFormatter.Year
                + days * KSPUtil.dateTimeFormatter.Day
                + hours * KSPUtil.dateTimeFormatter.Hour
                + minutes * KSPUtil.dateTimeFormatter.Minute
                + seconds + Planetarium.GetUniversalTime();

            bool result = RailsWarpToTime( warpTime );

            Log.Trace( "method end", logCaller );
            return result;
        }

        public void CancelRailsWarp(bool cancelAuto = true)
        {
            string logCaller = "TimeController.CancelRailsWarp";
            Log.Trace( "method start", logCaller );

            railsWarpEndTime = Mathf.Infinity;

            if (!(CurrentWarpState == TimeControllable.Rails || CurrentWarpState == TimeControllable.Physics))
            {
                if (TimeWarp.fetch != null && TimeWarp.fetch.current_rate_index > 0)
                {
                    Log.Error( "Rails warp is running but TimeController thinks we are on warp type " + CurrentWarpState.ToString(), logCaller );
                    // Recover as best we can
                    CancelHyperWarp();
                    CancelSlowMo();
                    CurrentWarpState = TimeControllable.Rails;
                }
                else
                {
                    Log.Info( "Cannot cancel rails warp as we are not rails warping." );
                    Log.Trace( "method end", logCaller );
                    return;
                }
            }

            if (cancelAuto)
            {
                Log.Info( "Cancelling auto warp if it is running.", logCaller );
                TimeWarp.fetch.CancelAutoWarp();
            }

            Log.Info( "Setting warp rate to 0.", logCaller );
            TimeWarp.SetRate( 0, true, true );
            CurrentWarpState = TimeControllable.None;

            Log.Trace( "method end", logCaller );
        }

        #endregion
        #region Slow Motion and FPS Keeper
        public bool TrySetSlowMo()
        {
            string logCaller = "TimeController.TrySetSlowMo";
            Log.Trace( "method start", logCaller );

            if (CurrentWarpState == TimeControllable.None || CurrentWarpState == TimeControllable.SlowMo)
            {
                CurrentWarpState = TimeControllable.SlowMo;
                Log.Trace( "method end", logCaller );
                return true;
            }
            else
            {
                Log.Trace( "method end", logCaller );
                return false;
            }

        }
        public void CancelSlowMo()
        {
            string logCaller = "TimeController.CancelSlowMo";
            Log.Trace( "method start", logCaller );

            if (CurrentWarpState != TimeControllable.SlowMo)
            {
                Log.Warning( "Current Warp State is not SlowMo", logCaller );
                Log.Trace( "method end", logCaller );
                return;
            }

            resetTime();
            CurrentWarpState = TimeControllable.None;

            Log.Trace( "method end", logCaller );
            return;
        }
        public bool CheckSlowMo()
        {
            string logCaller = "TimeController.CheckSlowMo";
            Log.Trace( "method start", logCaller );

            if (CurrentWarpState == TimeControllable.SlowMo)
            {
                if (TimeSlider == 0f)
                {
                    Log.Info( "Slow Mo Cancelled, returning to NONE", logCaller );
                    CurrentWarpState = TimeControllable.None;
                    Log.Trace( "method end", logCaller );
                    return false;
                }
                Log.Trace( "method end", logCaller );
                return true;
            }
            Log.Trace( "method end", logCaller );
            return false;
        }


        public void SpeedUpTime()
        {
            string logCaller = "TimeController.SpeedUpTime";
            Log.Trace( "method start", logCaller );

            if (!((CanControlWarpType & TimeControllable.SlowMo) == TimeControllable.SlowMo) && !((CanControlWarpType & TimeControllable.Hyper) == TimeControllable.Hyper))
            {
                Log.Warning( "Cannot call SpeedUpTime when we cannot Slow-Mo or Hyper warp", logCaller );
                Log.Trace( "method end", logCaller );
                return;
            }

            if (((CanControlWarpType & TimeControllable.Hyper) == TimeControllable.Hyper) && CurrentWarpState == TimeControllable.Hyper)
            {
                Log.Info( "Increasing Hyper Warp Rate", logCaller );
                TimeController.Instance.HyperMaxRate += 1;
                Log.Trace( "method end", logCaller );
                return;
            }

            if ((CanControlWarpType & TimeControllable.SlowMo) == TimeControllable.SlowMo)
            {
                Log.Info( "Increasing Slow-Mo Rate", logCaller );
                UpdateTimeSlider( TimeSlider - 0.01f );
            }

            Log.Trace( "method end", logCaller );
            return;
        }
        public void SlowDownTime()
        {
            string logCaller = "TimeController.SpeedUpTime";
            Log.Trace( "method start", logCaller );


            if (!((CanControlWarpType & TimeControllable.SlowMo) == TimeControllable.SlowMo) && !((CanControlWarpType & TimeControllable.Hyper) == TimeControllable.Hyper))
            {
                Log.Warning( "Cannot call SlowDownTime when we cannot Slow-Mo or Hyper warp", logCaller );
                Log.Trace( "method end", logCaller );
                return;
            }

            if (((CanControlWarpType & TimeControllable.Hyper) == TimeControllable.Hyper) && CurrentWarpState == TimeControllable.Hyper)
            {
                Log.Info( "Decreasing Hyper Warp Rate", logCaller );
                TimeController.Instance.HyperMaxRate -= 1;
                Log.Trace( "method end", logCaller );
                return;
            }

            if ((CanControlWarpType & TimeControllable.SlowMo) == TimeControllable.SlowMo)
            {
                Log.Info( "Decreasing Slow-Mo Rate", logCaller );
                UpdateTimeSlider( TimeSlider + 0.01f );
            }
            Log.Trace( "method end", logCaller );
            return;
        }
        public void UpdateTimeSlider(float ts)
        {
            string logCaller = "TimeController.UpdateTimeSlider";
            Log.Trace( "method start", logCaller );

            if (!((CanControlWarpType & TimeControllable.SlowMo) == TimeControllable.SlowMo))
            {
                Log.Warning( "Cannot call UpdateTimeSlider when we cannot Slow-Mo warp", logCaller );
                Log.Trace( "method end", logCaller );
                return;
            }

            if (!TrySetSlowMo() && ts != 1f)
            {
                Log.Trace( "method end", logCaller );
                return;
            }
            Log.Info( "Setting Time Slider to " + ts );
            TimeSlider = Mathf.Clamp01( ts );
            CheckSlowMo();
            Log.Trace( "method end", logCaller );
            return;
        }

        public void SetFPSKeeper(bool v)
        {
            string logCaller = "TimeController.SetFPSKeeper";
            Log.Trace( "method start", logCaller );

            if (!((CanControlWarpType & TimeControllable.SlowMo) == TimeControllable.SlowMo))
            {
                Log.Warning( "Cannot call SetFPSKeeper when we cannot Slow-Mo warp", logCaller );
                Log.Trace( "method end", logCaller );
                return;
            }

            if (!TrySetSlowMo())
            {
                Log.Trace( "method end", logCaller );
                return;
            }

            IsFpsKeeperActive = v;
            CheckSlowMo();
            Log.Trace( "method end", logCaller );
            return;
        }

        public void ToggleFPSKeeper()
        {
            string logCaller = "TimeController.ToggleFPSKeeper";
            Log.Trace( "method start", logCaller );

            if (!((CanControlWarpType & TimeControllable.SlowMo) == TimeControllable.SlowMo))
            {
                Log.Warning( "Cannot call ToggleFPSKeeper when we cannot Slow-Mo warp", logCaller );
                Log.Trace( "method end", logCaller );
                return;
            }

            if (!TrySetSlowMo())
            {
                Log.Trace( "method end", logCaller );
                return;
            }

            IsFpsKeeperActive = !IsFpsKeeperActive;
            CheckSlowMo();
        }

        #endregion
        #region Hyper Warp
        public bool HyperWarpToTime(double warpTime)
        {
            string logCaller = "TimeController.HyperWarpToTime";
            Log.Trace( "method start", logCaller );

            if (!((CanControlWarpType & TimeControllable.Hyper) == TimeControllable.Hyper))
            {
                Log.Warning( "Cannot call HyperWarpToTime when we cannot Hyper warp", logCaller );
                Log.Trace( "method end", logCaller );
                return false;
            }

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

            if (!((CanControlWarpType & TimeControllable.Hyper) == TimeControllable.Hyper))
            {
                Log.Warning( "Cannot call HyperWarpForDuration when we cannot Hyper warp", logCaller );
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

            double warpTime = (hours * KSPUtil.dateTimeFormatter.Hour) + (minutes * KSPUtil.dateTimeFormatter.Minute) + seconds + Planetarium.GetUniversalTime();

            bool result = HyperWarpToTime( warpTime );
            Log.Trace( "method end", logCaller );
            return result;
        }
        public void ToggleHyperWarp()
        {
            string logCaller = "TimeController.ToggleHyperWarp";
            Log.Trace( "method start", logCaller );

            if (!((CanControlWarpType & TimeControllable.Hyper) == TimeControllable.Hyper))
            {
                Log.Warning( "Cannot call ToggleHyperWarp when we cannot Hyper warp", logCaller );
                Log.Trace( "method end", logCaller );
                return;
            }

            if (!(CurrentWarpState == TimeControllable.None || CurrentWarpState == TimeControllable.Hyper))
            {
                Log.Warning( "Currently in a different warp mode, cannot toggle hyper warp at this time.", logCaller );
                Log.Trace( "method end", logCaller );
                return;
            }

            if (CurrentWarpState == TimeControllable.Hyper)
            {
                CancelHyperWarp();
            }
            else
            {
                CurrentWarpState = TimeControllable.Hyper;
            }

            Log.Trace( "method end", logCaller );
        }
        public void CancelHyperWarp()
        {
            string logCaller = "TimeController.HyperWarpForDuration";
            Log.Trace( "method start", logCaller );

            if (CurrentWarpState != TimeControllable.Hyper)
            {
                Log.Warning( "Cannot cancel hyper warp, not currently hyperwarping", logCaller );
                Log.Trace( "method end", logCaller );
                return;
            }

            resetTime();
            hyperWarpEndTime = Mathf.Infinity;
            CurrentWarpState = TimeControllable.None;

            Log.Trace( "method end", logCaller );
        }
        #endregion

        #endregion



        #region fields

        private TimeControllable currentWarpState = TimeControllable.None;

        //PHYSICS
        private float defaultDeltaTime = Time.fixedDeltaTime; //0.02
        private float timeSlider = 0f;
        private float maxDeltaTimeSlider = GameSettings.PHYSICS_FRAME_DT_LIMIT;
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

        //HYPERWARP   
        private Boolean hyperPauseOnTimeReached = false;
        private double hyperWarpEndTime = Mathf.Infinity;

        private Boolean railsPauseOnTimeReached = false;
        private double railsWarpEndTime = Mathf.Infinity;

        private float hyperMinPhys = 1f;
        private float hyperMaxRate = 2f;
        private TimeWarp timeWarp;

        #endregion

    }
}

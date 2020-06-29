using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TimeControl
{
    /// <summary>
    /// Controls the extensions to the Rails Warp provided by Time Control. Custom warp rates and altitude limits, and 'time warp to UT' functionality
    /// </summary>
    [KSPAddon( KSPAddon.Startup.Instantly, true )]
    internal class RailsWarpController : MonoBehaviour
    {
        #region Singleton        
        public static RailsWarpController Instance { get; private set; }
        public static bool IsReady { get; private set; } = false;

        private static List<Orbit.PatchTransitionType> SOITransitions = new List<Orbit.PatchTransitionType> { Orbit.PatchTransitionType.ENCOUNTER, Orbit.PatchTransitionType.ESCAPE };
        private static List<Orbit.PatchTransitionType> UnstableOrbitTransitions = new List<Orbit.PatchTransitionType> { Orbit.PatchTransitionType.ENCOUNTER, Orbit.PatchTransitionType.ESCAPE, Orbit.PatchTransitionType.IMPACT };

        public static ConfigNode gameNode;
        
        private static void ThrowExceptionIfNotReady(string logBlockName)
        {
            if (IsReady)
            {
                return;
            }
            else
            {
                const string message = nameof( RailsWarpController ) + " not ready.";
                Log.Error( message, logBlockName );
                throw new InvalidOperationException( message );
            }
        }
        #endregion
        
        private bool isRailsWarpingToUT = false;
        private bool railsPauseOnUTReached = false;
        private double railsWarpingToUT = Mathf.Infinity;

        private List<float> defaultWarpRates;
        private List<float> customWarpRates;
        private List<float> newCustomWarpRates;

        private Dictionary<CelestialBody, List<float>> defaultAltitudeLimits;
        private Dictionary<CelestialBody, List<float>> customAltitudeLimits;
        private Dictionary<CelestialBody, List<float>> newCustomAltitudeLimits;

        private bool defaultWarpRatesCached = false;
        private bool defaultAltitudeLimitsCached = false;
        
        private int currentWarpToWarpIndex;

        private bool RatesNeedUpdatedAndSaved { get; set; } = false;

        /// <summary>
        /// Game Objects are ready to allow us to cache the default warp rates
        /// </summary>
        private bool CanCacheDefaultWarpRates
        {
            get => (TimeWarp.fetch != null);
        }

        /// <summary>
        /// Game Objects are ready to allow us to cache the default altitude limits
        /// </summary>
        private bool CanCacheDefaultAltitudeLimits
        {
            get => (TimeWarp.fetch != null && FlightGlobals.Bodies != null && FlightGlobals.Bodies.Count > 0);
        }

        private double CurrentUT
        {
            get => Planetarium.GetUniversalTime();
        }

        private float FixedDeltaTime
        {
            get => TimeController.Instance.FixedDeltaTime;
        }

        private float FixedUpdateTimeStep
        {
            get => TimeController.Instance.FixedDeltaTime * this.CurrentWarpRate;
        }

        private CelestialBody CurrentGameSOI
        {
            get => TimeController.Instance?.CurrentGameSOI;
        }
        
        /// <summary>
        /// Reset the maximumDeltaTime to the setting value
        /// </summary>
        private void ResetMaximumDeltaTime()
        {
            TimeController.Instance.ResetMaximumDeltaTime();
        }

        /// <summary>
        /// Allowed to update the internal warp rate array
        /// </summary>
        public bool CanUpdateInternalWarpRates
        {
            get => (RailsWarpController.IsReady && Mathf.Approximately( Time.timeScale, 1f ) && TimeWarp.fetch != null && TimeWarp.CurrentRateIndex == 0 && FlightGlobals.Bodies != null && FlightGlobals.Bodies.Count > 0);
        }
        
        public bool RailsPauseOnUTReached
        {
            get => railsPauseOnUTReached;
            set {
                if (railsPauseOnUTReached != value)
                {
                    railsPauseOnUTReached = value;
                }
            }
        }
        
        public bool CanRailsWarp
        {
            get
            {
                if (HighLogic.CurrentGame?.Parameters?.Flight != null)
                {
                    return (HighLogic.CurrentGame.Parameters.Flight.CanTimeWarpHigh) 
                        && (Mathf.Approximately( TimeController.Instance?.TimeScale ?? 1f, 1f ) || TimeController.Instance.IsTimeControlPaused)
                        && TimeWarp.fetch != null;
                }
                //else if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                //{
                //    return (Mathf.Approximately( TimeController.Instance?.TimeScale ?? 1f, 1f ) || TimeController.Instance.IsTimeControlPaused)
                //        && TimeWarp.fetch != null;
                //}
                {
                    return false;
                }
            }
            internal set
            {
                if (HighLogic.CurrentGame?.Parameters?.Flight != null && (HighLogic.CurrentGame.Parameters.Flight.CanTimeWarpHigh != value || HighLogic.CurrentGame.Parameters.Flight.CanTimeWarpLow != value) )
                {
                    HighLogic.CurrentGame.Parameters.Flight.CanTimeWarpHigh = value;
                    HighLogic.CurrentGame.Parameters.Flight.CanTimeWarpLow = value;
                }
            }
        }

        public bool IsRailsWarping
        {
            get => (TimeWarp.fetch != null && TimeWarp.CurrentRateIndex > 0);
        }

        public bool IsRailsWarpingNoPhys
        {
            get => IsRailsWarping && TimeWarp.WarpMode == TimeWarp.Modes.HIGH;
        }

        public bool IsRailsWarpingPhys
        {
            get => IsRailsWarping && TimeWarp.WarpMode == TimeWarp.Modes.LOW;
        }

        public float CurrentWarpRate
        {
            get => TimeWarp.CurrentRate;
        }

        public double RailsWarpingToUT
        {
            get
            {
                if (!IsRailsWarpingToUT)
                {
                    return 0f;
                }
                else
                {
                    return railsWarpingToUT;
                }
            }
            private set
            {
                if (railsWarpingToUT != value)
                {
                    railsWarpingToUT = value;
                }
            }
        }

        public bool IsRailsWarpingToUT
        {
            get => isRailsWarpingToUT;
            private set
            {
                if (isRailsWarpingToUT != value)
                {
                    isRailsWarpingToUT = value;
                }
            }
        }
        
        public int NumberOfWarpLevels
        {
            get => customWarpRates?.Count ?? 8;
        }

        private IDateTimeFormatter CurrentDTF
        {
            get => KSPUtil.dateTimeFormatter;
        }

        /// <summary>
        /// Returns a copy of the default warp rates list
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when default warp rates are not currently cached</exception>
        public List<float> GetDefaultWarpRates()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( GetDefaultWarpRates );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ThrowExceptionIfNotReady( logBlockName );
                return defaultWarpRates.ToList();
            }
        }

        /// <summary>
        /// Returns a copy of the custom warp rates list
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when custom warp rates are not currently cached</exception>
        public List<float> GetCustomWarpRates()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( GetCustomWarpRates );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ThrowExceptionIfNotReady( logBlockName );
                return customWarpRates.ToList();               
            }
        }

        /// <summary>
        /// Returns a copy of the default altitude limits for a specific celestial body
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="cb"/> is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when default altitude limits are not currently cached or <paramref name="cb"/> doesn't exist in the list</exception>
        public List<float> GetDefaultAltitudeLimitsForBody(CelestialBody cb)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( GetDefaultAltitudeLimitsForBody );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ThrowExceptionIfNotReady( logBlockName );

                if (cb == null)
                {
                    throw new ArgumentNullException( nameof( cb ), "Celestial Body Cannot Be Null" );
                }

                if (defaultAltitudeLimits.ContainsKey( cb ))
                {
                    return defaultAltitudeLimits[cb].ToList();
                } else
                {
                    string message = "Celestial Body " + cb.name + " does not exist in the default altitude limits list.";
                    Log.Error( message, logBlockName );
                    throw new InvalidOperationException( message );
                }
            }
        }

        /// <summary>
        /// Returns a copy of the custom altitude limits for a specific celestial body
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="cb"/> is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when custom altitude limits are not currently cached or <paramref name="cb"/> doesn't exist in the list</exception>
        public List<float> GetCustomAltitudeLimitsForBody(CelestialBody cb)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( GetCustomAltitudeLimitsForBody );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ThrowExceptionIfNotReady( logBlockName );

                if (cb == null)
                {
                    throw new ArgumentNullException( nameof( cb ), "Celestial Body Cannot Be Null" );
                }
                if (customAltitudeLimits.ContainsKey( cb ))
                {
                    return customAltitudeLimits[cb].ToList();
                }
                else
                {
                    string message = "Celestial Body " + cb.name + " does not exist in the custom altitude limits list.";
                    Log.Error( message, logBlockName );
                    throw new InvalidOperationException( message );
                }
            }
        }


        #region MonoBehavior

        private void Awake()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( Awake );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                DontDestroyOnLoad( this );
                Instance = this;
            }
        }

        private void Start()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( Start );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                StartCoroutine( Configure() );
            }
        }

        private void Update()
        {
            if (!IsReady)
            {
                return;
            }

            if (RatesNeedUpdatedAndSaved && CanUpdateInternalWarpRates)
            {
                ExecRateUpdateAndSave();
            }
        }

        private void FixedUpdate()
        {
            if (!IsReady)
            {
                return;
            }

            FixedUpdateRailsWarpToUT();
        }

        #region Initialization
        /// <summary>
        /// Configures the Rails Warp Controller once game state is ready to go
        /// </summary>
        private IEnumerator Configure()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( Configure );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                while (!GlobalSettings.IsReady || !IsValidScene())
                {
                    yield return new WaitForSeconds( 1f );
                }

                while (!(this.CanCacheDefaultWarpRates && this.CanCacheDefaultAltitudeLimits))
                {
                    yield return new WaitForSeconds( 1f );
                }

                while (RailsWarpController.gameNode == null)
                {
                    Log.Info( "Scenario Object has not loaded the necessary config node yet", logBlockName );
                    yield return new WaitForSeconds( 1f );
                }

                Log.Info( "Caching Warp Rates and Altitude Limits", logBlockName );
                this.CacheDefaultWarpRates();
                this.CacheDefaultAltitudeLimits();

                if (!(defaultWarpRatesCached && defaultAltitudeLimitsCached))
                {
                    Log.Error( "Something went wrong when caching default warp rates and altitude limits! Failing...", logBlockName );
                    yield break;
                }

                Log.Info( "Setting Custom Warp Rates and Limits to Defaults", logBlockName );
                customWarpRates = defaultWarpRates.ToList();
                customAltitudeLimits = new Dictionary<CelestialBody, List<float>>();
                foreach (var kp in defaultAltitudeLimits)
                {
                    customAltitudeLimits.Add( kp.Key, kp.Value.ToList() );
                }

                newCustomWarpRates = customWarpRates.ToList();
                newCustomAltitudeLimits = new Dictionary<CelestialBody, List<float>>();
                foreach (var kp in customAltitudeLimits)
                {
                    newCustomAltitudeLimits.Add( kp.Key, kp.Value.ToList() );
                }

                Load();

                ExecRateUpdateAndSave();

                CanRailsWarp = true;

                GameEvents.onTimeWarpRateChanged.Add( onTimeWarpRateChanged );

                Log.Info( nameof( RailsWarpController ) + " is Ready!", logBlockName );
                IsReady = true;

                yield break;
            }
        }


        #endregion Initialization

        #endregion MonoBehavior

        #region GameEvents
        private void onTimeWarpRateChanged()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( onTimeWarpRateChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (TimeWarp.CurrentRateIndex == 0)
                {
                    ResetMaximumDeltaTime();
                }
            }
        }
        #endregion GameEvents

        /// <summary>
        /// Update the warp rates and altitude limits in the arrays
        /// </summary>
        private void ExecRateUpdateAndSave()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( ExecRateUpdateAndSave );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Updating Internal Time Warp Arrays", logBlockName );

                var tw = TimeWarp.fetch;

                customWarpRates.Clear();
                customWarpRates.AddRange( newCustomWarpRates );

                customAltitudeLimits.Clear();
                foreach (var kp in newCustomAltitudeLimits)
                {
                    customAltitudeLimits.Add( kp.Key, kp.Value.ToList() );
                }

                if (tw.warpRates.Length != this.customWarpRates.Count)
                {
                    Log.Trace( "Resizing Internal Warp Rates Array", logBlockName );
                    Array.Resize( ref tw.warpRates, this.customWarpRates.Count );
                }

                for (int i = 0; i < this.customWarpRates.Count; i++)
                {
                    tw.warpRates[i] = customWarpRates[i];
                    Log.Trace( string.Format( "Setting Warp Level {0}: {1}x", i, tw.warpRates[i] ), logBlockName );
                }

                Log.Trace( "Updating Internal Celestial Body Altitude Limits Arrays", logBlockName );
                foreach (CelestialBody cb in FlightGlobals.Bodies)
                {
                    Log.Trace( "Setting Altitude Limits for Body " + cb.bodyName + ":", logBlockName );
                    if (cb.timeWarpAltitudeLimits.Length != this.customWarpRates.Count)
                    {
                        Log.Trace( "Resizing Internal Celestial Body Array for " + cb.bodyName, logBlockName );
                        Array.Resize( ref cb.timeWarpAltitudeLimits, this.customWarpRates.Count );
                    }

                    for (int i = 0; i < this.customWarpRates.Count; i++)
                    {
                        cb.timeWarpAltitudeLimits[i] = this.customAltitudeLimits[cb][i];
                        Log.Trace( String.Format( "Altitude Level {0}: {1}", i, cb.timeWarpAltitudeLimits[i] ), logBlockName );
                    }
                }

                // Force a game settings save
                GameSettings.SaveSettings();
                
                RatesNeedUpdatedAndSaved = false;

                TimeControlEvents.OnTimeControlCustomWarpRatesChanged.Fire( true );
            }
        }

        /// <summary>
        /// Cache the default warp rates in memory
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when game is invalid state</exception>
        private void CacheDefaultWarpRates()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( CacheDefaultWarpRates );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (defaultWarpRatesCached)
                {
                    Log.Info( "Default Warp Rates Already Cached", logBlockName );
                    return;
                }

                if (!this.CanCacheDefaultWarpRates)
                {
                    const string message = "Cannot cache default warp rates, invalid game state";
                    Log.Error( message, logBlockName );
                    throw new InvalidOperationException( message );
                }

                defaultWarpRates = defaultWarpRates ?? new List<float>();
                defaultWarpRates.Clear();
                defaultWarpRates.AddRange( TimeWarp.fetch.warpRates );
                defaultWarpRatesCached = true;
                
                Log.Info( "Default Warp Rates Cached", logBlockName );

                if (Log.LoggingLevel == LogSeverity.Trace)
                {
                    Log.Trace( "Default Warp Rates:", logBlockName );
                    for (int i = 0; i < defaultWarpRates.Count; i++)
                    {
                        Log.Trace( string.Format( "Warp Level {0}: {1}x", i, defaultWarpRates[i]), logBlockName );
                    }
                }
            }
        }

        /// <summary>
        /// Cache the default altitude limits in memory
        /// </summary>
        private void CacheDefaultAltitudeLimits()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( CacheDefaultAltitudeLimits );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (defaultAltitudeLimitsCached)
                {
                    Log.Info( "Default Altitude Limits Already Cached", logBlockName );
                    return;
                }
                defaultAltitudeLimits = defaultAltitudeLimits ?? new Dictionary<CelestialBody, List<float>>();
                defaultAltitudeLimits.Clear();

                if (!this.CanCacheDefaultAltitudeLimits)
                {
                    const string message = "Cannot cache default altitude limits. Invalid game state.";
                    Log.Error( message, logBlockName );
                    throw new InvalidOperationException( message );
                }

                foreach (CelestialBody cb in FlightGlobals.Bodies)
                {                    
                    if (!defaultAltitudeLimits.Keys.Contains( cb ))
                    {
                        defaultAltitudeLimits.Add( cb, new List<float>() );
                    }
                    defaultAltitudeLimits[cb].Clear();
                    defaultAltitudeLimits[cb].AddRange( cb.timeWarpAltitudeLimits );

                    Log.Info( "Default Altitude Limits for Body " + cb.name + " saved", logBlockName );
                    if (Log.LoggingLevel == LogSeverity.Trace)
                    {
                        Log.Trace( "Default Altitude Limits for Body " + cb.bodyName + ":", logBlockName );
                        for (int i = 0; i < defaultAltitudeLimits[cb].Count; i++)
                        {
                            Log.Trace( String.Format( "Altitude Level {0}: {1}", i, defaultAltitudeLimits[cb][i]), logBlockName );
                        }
                    }
                }

                defaultAltitudeLimitsCached = true;
            }
        }


        #region Modify Warp Rates and Altitude Limits
        /// <summary>
        /// Resets the custom warp rates back to the defaults
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot reset warp rates because the controller is in an invalid state</exception>
        public void ResetWarpRates()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( ResetWarpRates );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ThrowExceptionIfNotReady( logBlockName );
                SetCustomWarpRates( defaultWarpRates );
            }
        }

        /// <summary>
        /// Resets the custom altitude limits back to the defaults
        /// </summary>
        /// /// <exception cref="InvalidOperationException">Cannot reset altitude limits because the controller is in an invalid state</exception>
        public void ResetAltitudeLimits()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( ResetAltitudeLimits );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ThrowExceptionIfNotReady( logBlockName );
                SetCustomAltitudeLimits( defaultAltitudeLimits );
            }
        }

        /// <summary>
        /// Resets the custom altitude limits back to the defaults for a specific celestial body
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot reset altitude limits because the controller is in an invalid state</exception>
        public void ResetAltitudeLimits(CelestialBody cb)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( ResetAltitudeLimits );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ThrowExceptionIfNotReady( logBlockName );
                SetCustomAltitudeLimitsForBody( cb, defaultAltitudeLimits[cb] );
            }
        }

        /// <summary>
        /// Add a warp level. Cannot go above 99 warp levels
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot add a warp level because the controller is in an invalid state</exception>
        public void AddWarpLevel()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( AddWarpLevel );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ThrowExceptionIfNotReady( logBlockName );

                if (this.newCustomWarpRates.Count >= 99)
                {
                    Log.Warning( "Cannot go above 99 warp levels", logBlockName );
                    return;
                }

                newCustomWarpRates.Add( newCustomWarpRates[newCustomWarpRates.Count - 1] );
                foreach (var s in newCustomAltitudeLimits.Values)
                {
                    s.Add( s[s.Count - 1] );
                }
                
                RatesNeedUpdatedAndSaved = true;
                
                Log.Info( "Warp Level Added", logBlockName );
            }
        }

        /// <summary>
        /// Remove a warp level. Cannot go below 8 warp levels
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot remove a warp level because the controller is in an invalid state</exception>
        public void RemoveWarpLevel()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( RemoveWarpLevel );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ThrowExceptionIfNotReady( logBlockName );

                if (this.newCustomWarpRates.Count <= 8)
                {
                    Log.Warning( "Cannot go below 8 warp levels", logBlockName );
                    return;
                }

                newCustomWarpRates.RemoveAt( newCustomWarpRates.Count - 1 );
                foreach (var s in newCustomAltitudeLimits.Values)
                {
                    s.RemoveAt( s.Count - 1 );
                }
                
                RatesNeedUpdatedAndSaved = true;

                Log.Info( "Warp Level Removed", logBlockName );
            }
        }
        
        /// <summary>
        /// Updates the custom warp rate list
        ///  </summary>
        /// <param name="wr">Must have between 8 and 99 elements</param>
        /// <exception cref="ArgumentNullException">Thrown when parameter <paramref name="wr"/> is null</exception>
        /// <exception cref="ArgumentException">Thrown when parameter <paramref name="wr"/> has too few or too many elements</exception>
        public void SetCustomWarpRates(List<float> wr)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( SetCustomWarpRates );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (wr == null)
                {
                    const string message = "New Custom Warp Rate List cannot be null";
                    Log.Error( message, logBlockName );
                    throw new ArgumentNullException( "wr", message );
                }
                if (wr.Count < 8)
                {
                    const string message = "New Custom Warp Rate List must have at least 8 warp rates";
                    Log.Error( message, logBlockName );
                    throw new ArgumentException( message, "wr" );
                }
                if (wr.Count > 99)
                {
                    const string message = "New Custom Warp Rate List can only have max of 99 warp rates";
                    Log.Error( message, logBlockName );
                    throw new ArgumentException( message, "wr" );
                }
                newCustomWarpRates = new List<float>();
                newCustomWarpRates.AddRange( wr );
                
                newCustomAltitudeLimits = new Dictionary<CelestialBody, List<float>>();
                foreach (var k in customAltitudeLimits.Keys.ToList())
                {                        
                    List<float> s = customAltitudeLimits[k].ToList();

                    while (newCustomWarpRates.Count > s.Count)
                    {
                        s.Add( s[s.Count - 1] );
                    }
                    while (newCustomWarpRates.Count < s.Count)
                    {
                        s.RemoveAt( s.Count - 1 );
                    }

                    newCustomAltitudeLimits[k] = s;
                }
                
                RatesNeedUpdatedAndSaved = true;
                Log.Info( "New Custom Warp Rates Set", logBlockName );
            }
        }

        /// <summary>
        /// Updates the custom altitude limits for all celestial bodies
        /// </summary>
        /// <param name="altitudes">Each list in the dictonary must have the same number of elements as the warp rates list does. Will try to fail gracefully.</param>
        private void SetCustomAltitudeLimits(Dictionary<CelestialBody, List<float>> altitudes)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( SetCustomAltitudeLimits );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                foreach (var kp in altitudes)
                {
                    SetCustomAltitudeLimitsForBody( kp.Key, kp.Value );
                }

                Log.Info( "New Custom Altitude Limits Set", logBlockName );
            }
        }

        /// <summary>
        /// Updates the custom altitude limits for a specific celestial body
        /// </summary>
        /// <param name="cb"></param>
        /// <param name="altitudes"></param>
        /// <exception cref="ArgumentNullException">Thrown when parameter <paramref name="cb"/> or <paramref name="altitudes"/> is null</exception>        
        /// <exception cref="ArgumentException">Thrown when parameter <paramref name="altitudes"/> has zero elements</exception>
        public void SetCustomAltitudeLimitsForBody(CelestialBody cb, List<float> altitudes)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( SetCustomAltitudeLimitsForBody );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (cb == null)
                {
                    const string message = "Celestial Body cannot be null";
                    Log.Error( message, logBlockName );
                    throw new ArgumentNullException( "cb", message );
                }
                if (altitudes == null)
                {
                    const string message = "Custom Altitude Limit List cannot be null";
                    Log.Error( message, logBlockName );
                    throw new ArgumentNullException( "altitudes", message );
                }
                if (altitudes.Count == 0)
                {
                    const string message = "Custom Altitude Limit List cannot be zero-length";
                    Log.Error( message, logBlockName );
                    throw new ArgumentException( message, "cbal" );
                }
                if (altitudes.Count != this.newCustomWarpRates.Count)
                {
                    string message = "Custom Altitude Limit List for " + cb.name + " must have the same number of elements as warp rates. Trying to correct.";
                    Log.Warning( message, logBlockName );

                    while (this.newCustomWarpRates.Count > altitudes.Count)
                    {
                        altitudes.Add( altitudes[(altitudes.Count - 1)] );
                    }
                    while (this.newCustomWarpRates.Count < altitudes.Count)
                    {
                        altitudes.RemoveAt( altitudes.Count - 1 );
                    }
                }

                if (!this.newCustomAltitudeLimits.ContainsKey( cb ))
                {
                    this.newCustomAltitudeLimits.Add( cb, new List<float>() );
                }
                //this.newCustomAltitudeLimits[cb] = this.newCustomAltitudeLimits[cb] ?? new List<float>();
                this.newCustomAltitudeLimits[cb].Clear();
                this.newCustomAltitudeLimits[cb].AddRange( altitudes );
                
                RatesNeedUpdatedAndSaved = true;

                Log.Info( "New Custom Altiude Limits Set for ".MemoizedConcat(cb.name), logBlockName );
            }
        }
        
        public void SetWarpRatesToKerbinTimeMultiples()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( SetWarpRatesToKerbinTimeMultiples );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                List<float> newWr = new List<float>()
                {
                          // 1 second realtime equals X sec gametime
                    this.defaultWarpRates[0],
                    5f,        // 5 sec
                    15f,       // 15 sec
                    45f,       // 45 sec
                    60f,       // 1 min
                    300f,      // 5 min
                    900f,      // 15 min
                    2700f,     // 45 min
                    3600f,     // 1 hour
                    10800f,    // 3 hours
                    21650.8f,    // 1 Kerbal-Day
                    108254f,   // 5 Kerbal-Days
                    324762f,   // 15 Kerbal-Days
                    974286f,   // 45 Kerbal-Days
                    2922858f,  // 135 Kerbal-Days
                    9203544f   // 1 Kerbal-Year
                };

                this.SetCustomWarpRates( newWr );
            }
        }

        public void SetWarpRatesToEarthTimeMultiples()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( SetWarpRatesToEarthTimeMultiples );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                List<float> newWr = new List<float>()
                {
                          // 1 second realtime equals X sec gametime
                    this.defaultWarpRates[0],        // 1 sec
                    5f,        // 5 sec
                    15f,       // 15 sec
                    45f,       // 45 sec
                    60f,       // 1 min
                    300f,      // 5 min
                    900f,      // 15 min
                    2700f,     // 45 min
                    3600f,     // 1 hour
                    10800f,    // 3 hours
                    43200f,    // 12 hours
                    86400f,    // 1 earth-day
                    432000f,   // 5 earth-days
                    1296000f,  // 15 earth-days
                    2592000f,  // 30 earth-days / ~ 1 month
                    7776000f,  // 90 earth-days / ~ 3 months
                    15552000f, // 180 earth-days / ~ 6 months
                    31536000f  // 365 earth-days / 1 earth-year
                };

                this.SetCustomWarpRates( newWr );
            }
        }

        public void SetAltitudeLimitsToAtmoForBody(CelestialBody cb, float vacuumHeight)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( SetAltitudeLimitsToAtmo );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                List<float> curAl = this.GetCustomAltitudeLimitsForBody( cb );
                List<float> newAl = new List<float>();
                newAl.Add( curAl[0] );
                if (cb.atmosphere)
                {
                    double ceil = Math.Ceiling( (Math.Ceiling( cb.atmosphereDepth / 1000.0d ) * 1000.0d) );

                    newAl.AddRange( curAl.Skip( 1 ).Select( f => Convert.ToSingle( ceil ) ) );
                }
                else
                {
                    newAl.AddRange( curAl.Skip( 1 ).Select( f => vacuumHeight ) );
                }

                this.SetCustomAltitudeLimitsForBody( cb, newAl );
            }
        }

        public void SetAltitudeLimitsToAtmo(float vacuumHeight)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( SetAltitudeLimitsToAtmo );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Dictionary<CelestialBody, List<float>> newAlDict = new Dictionary<CelestialBody, List<float>>();

                foreach (CelestialBody cb in this.customAltitudeLimits.Keys.ToList())
                {
                    List<float> curAl = this.GetCustomAltitudeLimitsForBody( cb );
                    List<float> newAl = new List<float>();
                    newAl.Add( curAl[0] );

                    if (cb.atmosphere)
                    {
                        double ceil = Math.Ceiling( (Math.Ceiling( cb.atmosphereDepth / 1000.0d ) * 1000.0d) );

                        newAl.AddRange( curAl.Skip( 1 ).Select( f => Convert.ToSingle( ceil ) ) );
                    }
                    else
                    {
                        newAl.AddRange( curAl.Skip( 1 ).Select( f => vacuumHeight ) );
                    }

                    newAlDict.Add( cb, newAl );
                }

                this.SetCustomAltitudeLimits( newAlDict );
            }
        }

        #endregion

        #region Saving/Loading to ConfigNode
        public void Load()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( Load );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Loading from Internal Config", logBlockName );
                this.LoadCustomWarpRates( gameNode );
                this.LoadCustomAltitudeLimits( gameNode );
            }
        }

        public void Load(ConfigNode gameNode)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( Load );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Loading from Config Node", logBlockName );
                this.LoadCustomWarpRates( gameNode );
                this.LoadCustomAltitudeLimits( gameNode );
            }
        }
        
        /// <summary>
        /// Load custom warp rates into this object from a config node
        /// </summary>
        /// <param name="cn"></param>
        private void LoadCustomWarpRates(ConfigNode cn)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( LoadCustomWarpRates );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (cn == null)
                {
                    Log.Error( "ConfigNode " + nameof( cn ) + " is NULL, and cannot find existing settings config node.", logBlockName );
                    return;
                }

                if (!cn.HasNode( "customWarpRates" ))
                {
                    const string message = "No custom warp rates node in config. Using defaults.";
                    Log.Warning( message, logBlockName );
                    SetCustomWarpRates( defaultWarpRates );
                    return;
                }

                ConfigNode customWarpRatesNode = cn.GetNode( "customWarpRates" );
                List<float> ltcwr = new List<float>();

                bool warpRatesParseError = false;
                foreach (string s in customWarpRatesNode.GetValuesStartsWith( "customWarpRate" ))
                {
                    float num;
                    if (!(float.TryParse( s, out num )))
                    {
                        string message = "A custom warp rate is not defined as a number (value in the config was " + s + ").";
                        Log.Warning( message, logBlockName );
                        warpRatesParseError = true;
                        ltcwr = null;
                        break;
                    }
                    ltcwr.Add( num );
                }

                if (warpRatesParseError)
                {
                    string message = "Error loading custom warp rates from config. Using defaults.";
                    Log.Warning( message, logBlockName );
                    return;
                }

                try
                {
                    SetCustomWarpRates( ltcwr );
                }
                catch (Exception ex)
                {
                    if (ex is ArgumentException || ex is ArgumentNullException)
                    {
                        string message = "Argument Exception when setting custom warp rates. Using defaults.";
                        Log.Warning( message, logBlockName );
                    }
                    else
                        throw;
                }
            }
        }
        
        /// <summary>
        /// Load custom altitude limits into this object from a config node
        /// </summary>
        /// <param name="cn"></param>
        private void LoadCustomAltitudeLimits(ConfigNode cn)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( LoadCustomAltitudeLimits );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (cn == null)
                {
                    Log.Error( "ConfigNode " + nameof( cn ) + " is NULL, and cannot find existing settings config node.", logBlockName );
                    return;
                }

                if (!cn.HasNode( "CustomAltitudeLimits" ))
                {
                    const string message = "No CustomAltitudeLimits Node found in config. Using defaults.";
                    Log.Warning( message, logBlockName );

                    SetCustomAltitudeLimits( defaultAltitudeLimits );
                    return;
                }

                ConfigNode CustomAltitudeLimitsNode = cn.GetNode( "CustomAltitudeLimits" );

                Dictionary<CelestialBody, List<float>> customLimits = new Dictionary<CelestialBody, List<float>>();
                foreach (CelestialBody cb in FlightGlobals.Bodies)
                {
                    // Ignore bodies that exist in this game but don't exist in the file.
                    if (!CustomAltitudeLimitsNode.HasNode( cb.name ))
                    {
                        Log.Warning( "Celestial Body " + cb.name + " not found in Config Node. Default altitude limits for this body will be used instead.", logBlockName );
                        customLimits.Add( cb, defaultAltitudeLimits[cb].ToList() );
                        continue;
                    }

                    ConfigNode celestialLimitsNode = CustomAltitudeLimitsNode.GetNode( cb.name );
                    List<float> tempLimits = new List<float>();

                    string[] limits = celestialLimitsNode.GetValuesStartsWith( "customAltitudeLimit" );
                    int lc = limits.Count();

                    // Load altitude limits. Only load up to the warp limit.
                    int i = 0;
                    float limit = 0f;
                    bool invalidValueFound = false;
                    for (; ((i < lc) && (tempLimits.Count != newCustomWarpRates.Count)); i++)
                    {

                        if (float.TryParse( limits[i], out limit ))
                        {
                            tempLimits.Add( limit );
                        }
                        else
                        {
                            Log.Warning( "Celestial Body " + cb.name + " has non-numeric values in the altitude limits. Default altitude limits for this body will be used instead.", logBlockName );
                            customLimits.Add( cb, defaultAltitudeLimits[cb].ToList() );
                            invalidValueFound = true;
                            break;
                        }
                    }
                    if (invalidValueFound)
                    {
                        continue;
                    }
                    // If there are fewer altiude limits defined in the settings file than the warp limit specifies, create additional ones using the last limit
                    while (tempLimits.Count < newCustomWarpRates.Count)
                    {
                        tempLimits.Add( limit );
                    }
                    customLimits.Add( cb, tempLimits );
                }
                SetCustomAltitudeLimits( customLimits );
            }
        }

        public void Save(ConfigNode gameNode)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( Save );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Info( "Saving to Config", logBlockName );
                this.SaveCustomWarpRates( gameNode );
                this.SaveCustomAltitudeLimits( gameNode );
            }
        }

        /// <summary>
        /// Save custom warp rates
        /// </summary>
        private void SaveCustomWarpRates(ConfigNode cn)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( SaveCustomWarpRates );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (cn == null)
                {
                    Log.Error( "ConfigNode " + nameof( cn ) + " is NULL, and cannot find existing settings config node.", logBlockName );
                    return;
                }

                // Rebuild the node
                if (cn.HasNode( "CustomWarpRates" ))
                {
                    cn.RemoveNode( "CustomWarpRates" );
                }
                ConfigNode customWarpRatesNode = cn.AddNode( "customWarpRates" );

                if (customWarpRates == null)
                {
                    Log.Error( "No custom warp rates defined. Cannot save", logBlockName );
                    return;
                }

                Log.Trace( "creating custom warp rates node", logBlockName );
                for (int i = 0; i < customWarpRates.Count; i++)
                {
                    customWarpRatesNode.AddValue( "customWarpRate" + i, customWarpRates[i] );
                }
            }
        }

        /// <summary>
        /// Save custom altitude limits into a config node
        /// </summary>
        /// <param name="cn"></param>
        private void SaveCustomAltitudeLimits(ConfigNode cn)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( SaveCustomAltitudeLimits );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (cn == null)
                {
                    Log.Error( "ConfigNode " + nameof( cn ) + " is NULL, and cannot find existing settings config node.", logBlockName );
                    return;
                }

                if (customAltitudeLimits == null)
                {
                    Log.Error( "customAltitudeLimits list is empty", logBlockName );
                    return;
                }

                const string cal = "CustomAltitudeLimits";
                ConfigNode CustomAltitudeLimitsNode = (cn.HasNode( cal )) ? cn.GetNode( cal ) : cn.AddNode( cal );

                foreach (var cb in customAltitudeLimits)
                {
                    string cbName = cb.Key.name;

                    // Rebuild the celestial body node. 
                    if (CustomAltitudeLimitsNode.HasNode( cbName ))
                    {
                        CustomAltitudeLimitsNode.RemoveNode( cbName );
                    }

                    ConfigNode celestialCN = CustomAltitudeLimitsNode.AddNode( cb.Key.name );

                    float l = 0f;
                    for (int j = 0; j < customWarpRates.Count; j++)
                    {
                        try
                        {
                            l = cb.Value[j];
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Log.Warning( "The custom altitude limits list for body " + cbName + " is smaller than the number of warp rates. Padding list with final altitude limit.", logBlockName );
                            // don't reassign l, just use the prior one.
                        }

                        celestialCN.AddValue( "customAltitudeLimit" + j, l );
                    }
                    Log.Trace( "Custom altitude limits for " + cb.Key.name + " built", logBlockName );
                }

                Log.Info( "Custom altitude limits saved.", logBlockName );
            }
        }
        
        #endregion

        #region Control Rails Warp
        
        public void DeactivateRails()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( DeactivateRails );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {                
                RailsWarpingToUT = Mathf.Epsilon;
                IsRailsWarpingToUT = false;

                Log.Info( "Cancelling built-in auto warp if it is running.", logBlockName );
                TimeWarp.fetch.CancelAutoWarp();

                Log.Trace( "Current Rate Index: " + TimeWarp.CurrentRateIndex.ToString(), logBlockName );

                if (this.IsRailsWarping)
                {
                    Log.Info( "Setting warp rate to 0.", logBlockName );
                    TimeWarp.SetRate( 0, true, false );
                }

                ResetMaximumDeltaTime();
            }
        }

        public void ActivateMaxRails()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( ActivateMaxRails );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ThrowExceptionIfNotReady( logBlockName );

                DeactivateRails();

                if (HighLogic.LoadedSceneIsFlight)
                {
                    Log.Info( "Setting warp rate to current max for vessel altitude limit over this celestial body.", logBlockName );
                    
                }
                else
                {
                    Log.Info( "Setting warp rate to max.", logBlockName );
                }

                TimeWarp.SetRate( 0, true, false );                
            }
        }

        public bool RailsWarpForDuration(double warpYears, double warpDays, double warpHours, double warpMinutes, double warpSeconds, bool useKerbinDaysYears = true)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( RailsWarpForDuration );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ThrowExceptionIfNotReady( logBlockName );

                double totalSeconds = 0f;
                if (useKerbinDaysYears)
                {
                    Log.Info( String.Format( "Trying to Rails Warp for {0} Kerbin-Years, {1} Kerbin-Years  {2} : {3} : {4} ", warpYears, warpDays, warpHours, warpMinutes, warpSeconds ), logBlockName );
                    totalSeconds = (warpYears * CurrentDTF.Year) + (warpDays * CurrentDTF.Day) + (warpHours * CurrentDTF.Hour) + (warpMinutes * CurrentDTF.Minute) + warpSeconds;
                }
                else
                {
                    Log.Info( String.Format( "Trying to Rails Warp for {0} Earth-Years, {1} Earth-Days  {2} : {3} : {4} ", warpYears, warpDays, warpHours, warpMinutes, warpSeconds ), logBlockName );
                    totalSeconds = (warpYears * 365 * 24 * 60 * 60) + (warpDays * 24 * 60 * 60) + (warpHours * 60 * 60) + (warpMinutes * 60) + warpSeconds;
                }
                return RailsWarpForSeconds( totalSeconds );
            }
        }

        public bool RailsWarpForSeconds(double seconds)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( RailsWarpForSeconds );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ThrowExceptionIfNotReady( logBlockName );

                Log.Info( "Trying to Rails Warp for " + seconds.ToString() + " seconds", logBlockName );
                double UT = CurrentUT + seconds;
                return RailsWarpToUT( UT );
            }
        }

        public bool RailsWarpToUT(double warpTime)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( RailsWarpToUT );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {

                ThrowExceptionIfNotReady( logBlockName );

                Log.Info( "Trying to Rails Warp to UT " + warpTime.ToString(), logBlockName );                
                if (!CanRailsWarp)
                {
                    Log.Info( "Cannot Rails Warp at this time.", logBlockName );
                    return false;
                }

                if (CurrentUT >= warpTime)
                {
                    Log.Warning( "Cannot Rails Warp to UT " + warpTime.ToString() + ". Already passed! Current UT is " + CurrentUT, logBlockName );
                    return false;
                }

                if (TimeController.Instance.IsTimeControlPaused)
                {
                    TimeController.Instance.Unpause();
                }

                IsRailsWarpingToUT = true;
                RailsWarpingToUT = warpTime;
                
                Log.Info( "Current UT: " + CurrentUT.ToString(), logBlockName );

                currentWarpToWarpIndex = GetMaxWarpRateIndexToNotPassUT( CurrentUT, this.RailsWarpingToUT );
                
                Log.Info( "Initial Rate Rate Set to Index " + currentWarpToWarpIndex + ", Rate: " + this.customWarpRates[currentWarpToWarpIndex] + "x" );
                
                TimeWarp.SetRate( currentWarpToWarpIndex, true, false );

                return true;
            }
        }
       
        private readonly FloatComparer fc = new FloatComparer();
        
        private int GetMaxWarpRateIndexToNotPassUT(double CurrentUT, double TargetUT)
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( GetMaxWarpRateIndexToNotPassUT );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                double remainingTime = TargetUT - CurrentUT;
                if (remainingTime <= 0)
                {
                    return 0;
                }

                int idx = customWarpRates.Select( rate => rate * this.FixedDeltaTime ).ToList().BinarySearch( (float)remainingTime, fc );
                if (idx <= 0)
                {
                    idx = (~idx);
                    if (idx <= customWarpRates.Count)
                    {
                        idx--;
                    }
                    else
                    {
                        idx = customWarpRates.Count - 1;
                    }
                }

                if (idx < 0)
                {
                    idx = 0;
                }             
                
                return idx;
            }
        }
        
        private void FixedUpdateRailsWarpToUT()
        {
            const string logBlockName = nameof( RailsWarpController ) + "." + nameof( FixedUpdateRailsWarpToUT );

            if (!this.IsRailsWarpingToUT)
            {
                return;
            }
            
            if (!this.IsRailsWarping)
            {
                Log.Info( "No longer rails warping. Ending " + nameof( FixedUpdateRailsWarpToUT ), logBlockName );
                this.IsRailsWarpingToUT = false;
                return;
            }

            if (!CanRailsWarp)
            {
                Log.Info( "Can no longer rails warp. Ending " + nameof( FixedUpdateRailsWarpToUT ), logBlockName );
                this.DeactivateRails();
                this.IsRailsWarpingToUT = false;                
                return;
            }

            Log.Trace( "Current UT: " + CurrentUT.ToString(), logBlockName );

            if (CurrentUT >= this.RailsWarpingToUT)
            {
                Log.Info( "Time " + this.RailsWarpingToUT + " has passed. Ending " + nameof( FixedUpdateRailsWarpToUT ), logBlockName );
                this.IsRailsWarpingToUT = false;
                this.DeactivateRails();
                if (this.RailsPauseOnUTReached)
                {
                    TimeController.Instance.PauseOnNextFixedUpdate = true;
                }
                return;
            }

            // Check a fixed update timestep ahead to see if we are going past the time
            if ((CurrentUT + this.FixedUpdateTimeStep) > this.RailsWarpingToUT)
            {
                int newWarpIndex = GetMaxWarpRateIndexToNotPassUT( CurrentUT, this.RailsWarpingToUT );
                if (newWarpIndex == 0)
                {
                    Log.Info( "Time " + this.RailsWarpingToUT + " too close to warp. Ending " + nameof( FixedUpdateRailsWarpToUT ), logBlockName );
                    this.IsRailsWarpingToUT = false;
                    this.DeactivateRails();
                    if (this.RailsPauseOnUTReached)
                    {
                        TimeController.Instance.PauseOnNextFixedUpdate = true;
                    }
                    return;
                }
                if (newWarpIndex != this.currentWarpToWarpIndex)
                {
                    Log.Info( "Updating Rails Rate to Index " + newWarpIndex );
                    TimeWarp.fetch.Mode = TimeWarp.Modes.HIGH;
                    TimeWarp.SetRate( newWarpIndex, true, false );
                    currentWarpToWarpIndex = newWarpIndex;
                    Log.Info( "Rails Rate Updated to Index " + currentWarpToWarpIndex + ", Rate: " + this.customWarpRates[currentWarpToWarpIndex] + "x" );
                }

                TimeWarp.SetRate( currentWarpToWarpIndex, true, false );
            }
        }

        private bool IsValidScene()
        {
            return (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION);
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

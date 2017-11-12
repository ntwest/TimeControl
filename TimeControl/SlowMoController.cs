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
    internal class SlowMoController : MonoBehaviour
    {
        #region Static
        public static SlowMoController Instance { get; private set; }
        public static bool IsReady { get; private set; } = false;
        private static ConfigNode settingsCN;
        public static ConfigNode SettingsCN
        {
            get => settingsCN;
            set
            {
                settingsCN = value;
                if (Instance != null && IsReady)
                {
                    Instance.LoadSlowMoRates( settingsCN );
                }
            }
        }
        #endregion

        private FlightCamera cam;

        #region MonoBehavior
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
                StartCoroutine( CRInit() );
            }
        }

        private void Update()
        {
            if (IsSlowMo)
            {
                if (GlobalSettings.IsReady && GlobalSettings.Instance.CameraZoomFix)
                {
                    cam.SetDistanceImmediate( cam.Distance );
                }

                if (!CanSlowMo)
                {
                    DeactivateSlowMo();
                    return;
                }
            }
        }

        private void OnDestroy()
        {
            OnTimeControlSlowMoRateChangedEvent?.Remove( SlowMoRateChanged );
        }
        #endregion

        #region Initialization
        private IEnumerator CRInit()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( CRInit );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                this.SetDefaults();
                this.SubscribeToGameEvents();

                while (TimeWarp.fetch == null || FlightGlobals.Bodies == null || FlightGlobals.Bodies.Count <= 0)
                {
                    yield return new WaitForSeconds( 1f );
                }

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
                GameEvents.onGamePause.Add( this.onGamePause );
                GameEvents.onGameUnpause.Add( this.onGameUnpause );
                GameEvents.onGameSceneLoadRequested.Add( this.onGameSceneLoadRequested );
                GameEvents.onLevelWasLoaded.Add( this.onLevelWasLoaded );

                OnTimeControlSlowMoRateChangedEvent = GameEvents.FindEvent<EventData<float>>( nameof ( TimeControlEvents.OnTimeControlSlowMoRateChanged ) );
                OnTimeControlSlowMoRateChangedEvent?.Add( SlowMoRateChanged );
            }
        }
        #endregion

        #region Private Fields and Properties       

        private readonly string slowMoRateConfigNodeNamePrefix = "slowMoRate";
        private readonly string slowMoRateParentConfigNodeName = "slowMoRates";

        private EventData<float> OnTimeControlSlowMoRateChangedEvent;
        
        private float defaultDeltaTime;        
        private float slowMoRate = 0.5f;
        private bool deltaLocked = true;
        //private bool canSlowMo = false;
        private bool isSlowMo = false;
        private bool isGamePaused = false;

        private ScreenMessage defaultScreenMessage;
        private ScreenMessage currentScreenMessage;        
        private ScreenMessageStyle currentScreenMessageStyle;
        private float currentScreenMessageDuration;
        private string currentScreenMessagePrefix;
        
        /// <summary>
        /// Defines the default slow motion rates that can be selected
        /// </summary>
        private List<float> defaultSlowMotionRates = new List<float>() { 1f, 3f/4f, 1f/2f, 3f/8f, 1f/4f, 3f/16f, 1f/8f, 3f/32f, 1f/16f, 3f/64f, 1f/32f, 3f/128f, 1f/64f };
        private List<float> slowMotionRates;

        
        private bool ShowOnscreenMessages
        {
            get => HighLogic.CurrentGame?.Parameters?.CustomParams<TimeControlParameterNode>()?.ShowSlowMoOnscreenMessages ?? true;
        }

        private bool PerformanceCountersOn
        {
            get => PerformanceManager.Instance?.PerformanceCountersOn ?? false;
        }

        private bool CurrentScreenMessageOn
        {
            get => currentScreenMessage != null && (ScreenMessages.Instance?.ActiveMessages?.Contains( currentScreenMessage ) ?? false);
        }

        private bool slowMoCanRailsWarp = true;       
        private bool SlowMoCanRailsWarp
        {
            get => slowMoCanRailsWarp && (RailsWarpController.Instance?.CanRailsWarp ?? false);
            set
            {
                const string logBlockName = nameof( SlowMoController ) + "." + nameof( SlowMoCanRailsWarp ) + ":set";
                using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
                {
                    if (RailsWarpController.Instance != null)
                    {
                        if (RailsWarpController.Instance.CanRailsWarp == slowMoCanRailsWarp)
                        {
                            RailsWarpController.Instance.CanRailsWarp = value;                            
                        }
                        slowMoCanRailsWarp = value;
                    }
                    else
                    {
                        Log.Error( "RailsWarpController.Instance is null! This should not happen. Please log a bug with the developer.", logBlockName );
                    }  
                }
            }
        }

        #endregion


        public bool CanSlowMo
        {
            get => (TimeWarp.fetch != null && TimeWarp.CurrentRateIndex == 0 && (Mathf.Approximately(Time.timeScale, 1f) || isSlowMo) && HighLogic.LoadedSceneIsFlight) ;
        }
        
        public bool IsSlowMo
        {
            get => isSlowMo;
            private set => isSlowMo = value;
        }
        
        /// <summary>
        /// Floating point value between 0 and 1. The lower the number the slower the rate. 0 = paused, 1 = realtime.
        /// </summary>
        public float SlowMoRate
        {
            get => this.slowMoRate;
            set
            {
                if (value > 1f)
                {
                    this.SlowMoRate = 1f;
                }
                else if (value < 0f)
                {
                    this.SlowMoRate = 0f;
                }
                else
                { 
                    this.slowMoRate = value;
                }
                TimeControlEvents.OnTimeControlSlowMoRateChanged?.Fire( this.slowMoRate );
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

        public List<float> SlowMotionRates
        {
            get
            {
                if (slowMotionRates != null)
                {
                    return slowMotionRates.ToList();
                }
                else
                {
                    return null;
                }
            }
            private set => slowMotionRates = value;
        }

        internal bool DeltaLocked
        {
            get => deltaLocked;
            set => deltaLocked = value;
        }



        #region GameEvents
        private void onPause()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( onPause );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                isGamePaused = true;
            }
        }
        private void onUnpause()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( onUnpause );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                isGamePaused = false;

                if (this.isSlowMo)
                {
                    if (!this.CanSlowMo)
                    {
                        DeactivateSlowMo();
                        return;
                    }
                    SetSlowMoTimeScale();
                    SetSlowMoFixedDeltaTime();
                }
            }
        }
        
        private void onGamePause()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( onGamePause );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                onPause();
            }
        }
        
        private void onGameUnpause()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( onGameUnpause );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                onUnpause();
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
                cam = cams[0];

                DeactivateSlowMo();
            }
        }

        private void SlowMoRateChanged(float data)
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( SlowMoRateChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (this.isSlowMo && !this.isGamePaused)
                {
                    SetSlowMoTimeScale();
                    SetSlowMoFixedDeltaTime();                    
                }
            }
        }

        /// <summary>
        /// Modify the Unity time scale
        /// </summary>
        private void SetSlowMoTimeScale()
        {            
            TimeController.Instance.TimeScale = this.SlowMoRate;
        }

        /// <summary>
        /// Modify the Unity fixed delta time
        /// </summary>
        private void SetSlowMoFixedDeltaTime()
        {
            float defaultDeltaTime = TimeController.Instance.DefaultFixedDeltaTime;
            TimeController.Instance.FixedDeltaTime = DeltaLocked ? defaultDeltaTime : defaultDeltaTime * this.SlowMoRate;
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
        #endregion

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
                SlowMoCanRailsWarp = false;
                isSlowMo = true;

                SetSlowMoTimeScale();
                SetSlowMoFixedDeltaTime();

                StartCoroutine( UpdateSlowMoScreenMessage() );                
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
                SlowMoCanRailsWarp = true;
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

        #region Private Methods

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

        private void ResetTime()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( ResetTime );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ResetTimeScale();
                ResetFixedDeltaTime();
            }
        }

        private IEnumerator UpdateSlowMoScreenMessage()
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( UpdateSlowMoScreenMessage );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (this.ShowOnscreenMessages)
                {
                    this.currentScreenMessage = ScreenMessages.PostScreenMessage( defaultScreenMessage );
                }

                while (true)
                {
                    if ((!this.isSlowMo))
                    {
                        if ((!this.ShowOnscreenMessages) || CurrentScreenMessageOn)
                        {
                            ScreenMessages.RemoveMessage( this.currentScreenMessage );
                            currentScreenMessage = null;
                        }
                        yield break;
                    }
                    yield return null;
                }
            }
        }
        #endregion

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



        /// <summary>
        /// Updates the slow-mo rate list
        ///  </summary>
        public void SetSlowMotionRates(List<float> rates)
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( SetSlowMotionRates );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (rates == null)
                {
                    const string message = "New Slow-Mo Rate List cannot be null";
                    Log.Error( message, logBlockName );
                    throw new ArgumentNullException( nameof( rates ), message );
                }
                if (rates.Count < 1)
                {
                    const string message = "New Slow-Mo Rate List must have at least 1 slow-mo rate";
                    Log.Error( message, logBlockName );
                    throw new ArgumentException( message, nameof( rates ) );
                }

                slowMotionRates = slowMotionRates ?? new List<float>();
                slowMotionRates.Clear();
                slowMotionRates.AddRange( rates );

                Log.Info( "Slow-Motion Rates list loaded.", logBlockName );

                if (Log.LoggingLevel == LogSeverity.Trace)
                {
                    Log.Trace( "Slow-Motion Warp Rates:", logBlockName );
                    for (int i = 0; i < SlowMotionRates.Count; i++)
                    {
                        Log.Trace( String.Format( "Step {0}: {1}x", i, slowMotionRates[i], logBlockName ) );
                    }
                }
            }
        }

        #region Saving and Loading
        /// <summary>
        /// Save custom warp rates into a config node
        /// </summary>
        /// <param name="cn"></param>
        public void SaveSlowMoRates(ConfigNode cn)
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( SaveSlowMoRates );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                // Rebuild the node
                if (cn.HasNode( this.slowMoRateParentConfigNodeName ))
                {
                    cn.RemoveNode( this.slowMoRateParentConfigNodeName );
                }
                ConfigNode slowMoRatesParentConfigNode = cn.AddNode( this.slowMoRateParentConfigNodeName );

                if (this.slowMotionRates == null)
                {
                    Log.Trace( "no slow motion rates defined (yet)", logBlockName );
                    return;
                }

                Log.Trace( "creating slow motion rates node", logBlockName );
                for (int i = 0; i < this.slowMotionRates.Count; i++)
                {
                    slowMoRatesParentConfigNode.AddValue( this.slowMoRateConfigNodeNamePrefix + i, this.slowMotionRates[i] );
                }
            }
        }

        /// <summary>
        /// Load custom warp rates into this object from a config node
        /// </summary>
        /// <param name="cn"></param>
        public void LoadSlowMoRates(ConfigNode cn)
        {
            const string logBlockName = nameof( SlowMoController ) + "." + nameof( LoadSlowMoRates );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (!cn.HasNode( this.slowMoRateParentConfigNodeName ))
                {
                    const string message = "No custom warp rates node in config. Using defaults.";
                    Log.Warning( message, logBlockName );
                    return;
                }

                ConfigNode slowMoRatesNode = cn.GetNode( this.slowMoRateParentConfigNodeName );
                List<float> cnSlowMoRates = new List<float>();

                bool slowMoRatesParseError = false;
                foreach (string s in slowMoRatesNode.GetValuesStartsWith( this.slowMoRateConfigNodeNamePrefix ))
                {
                    float num;
                    if (!(float.TryParse( s, out num )))
                    {
                        string message = "A custom warp rate is not defined as a number (value in the config was " + s + ").";
                        Log.Warning( message, logBlockName );
                        slowMoRatesParseError = true;
                        cnSlowMoRates = null;
                        break;
                    }
                    float numC = Mathf.Clamp01( num );
                    if (!Mathf.Approximately(num, numC))
                    {
                        string message = "A custom slow-motion rate is not defined between 0 and 1 (value in the config was " + s + ").";
                        Log.Warning( message, logBlockName );
                        slowMoRatesParseError = true;
                        cnSlowMoRates = null;
                        break;
                    }
                    cnSlowMoRates.Add( num );
                }

                if (slowMoRatesParseError)
                {
                    string message = "Error loading slow-motion rates from config. Using defaults.";
                    Log.Warning( message, logBlockName );
                    return;
                }

                cnSlowMoRates.Sort( (a, b) => a.CompareTo( b ) );

                try
                {
                    SetSlowMotionRates( cnSlowMoRates );
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
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using TimeControl.KeyBindings;

namespace TimeControl
{
    [KSPAddon( KSPAddon.Startup.MainMenu, true )]
    public class TimeControlEvents : MonoBehaviour
    {
        public static EventData<float> OnTimeControlDefaultFixedDeltaTimeChanged;
        public static EventData<float> OnTimeControlFixedDeltaTimeChanged;
        public static EventData<float> OnTimeControlTimeScaleChanged;
        public static EventData<bool> OnTimeControlTimePaused;
        public static EventData<bool> OnTimeControlTimeUnpaused;

        public static EventData<float> OnTimeControlHyperWarpMaxAttemptedRateChanged;
        public static EventData<float> OnTimeControlHyperWarpPhysicsAccuracyChanged;        
        public static EventData<float> OnTimeControlHyperWarpStarting;
        public static EventData<float> OnTimeControlHyperWarpStarted;
        public static EventData<float> OnTimeControlHyperWarpStopping;
        public static EventData<float> OnTimeControlHyperWarpStopped;

        public static EventData<float> OnTimeControlSlowMoRateChanged;
        public static EventData<float> OnTimeControlSlowMoStarting;
        public static EventData<float> OnTimeControlSlowMoStarted;
        public static EventData<float> OnTimeControlSlowMoStopping;
        public static EventData<float> OnTimeControlSlowMoStopped;

        public static EventData<bool> OnTimeControlCustomWarpRatesChanged;

        public static EventData<bool> OnTimeControlGlobalSettingsSaved;
        public static EventData<bool> OnTimeControlGlobalSettingsLoaded;

        public static EventData<TimeControlKeyBinding> OnTimeControlKeyBindingsChanged;

        private void Awake()
        {
            const string logBlockName = nameof( Awake );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                DontDestroyOnLoad( this );

                // Common
                OnTimeControlDefaultFixedDeltaTimeChanged = new EventData<float>( nameof( OnTimeControlDefaultFixedDeltaTimeChanged ) );
                OnTimeControlFixedDeltaTimeChanged = new EventData<float>( nameof( OnTimeControlFixedDeltaTimeChanged ) );
                OnTimeControlTimeScaleChanged = new EventData<float>( nameof( OnTimeControlTimeScaleChanged ) );
                OnTimeControlTimePaused = new EventData<bool>( nameof( OnTimeControlTimePaused ) );
                OnTimeControlTimeUnpaused = new EventData<bool>( nameof( OnTimeControlTimeUnpaused ) );

                // Hyper Warp
                OnTimeControlHyperWarpMaxAttemptedRateChanged = new EventData<float>( nameof( OnTimeControlHyperWarpMaxAttemptedRateChanged ) );
                OnTimeControlHyperWarpPhysicsAccuracyChanged = new EventData<float>( nameof( OnTimeControlHyperWarpPhysicsAccuracyChanged ) );

                OnTimeControlHyperWarpStarting = new EventData<float>( nameof( OnTimeControlHyperWarpStarting ) );
                OnTimeControlHyperWarpStarted = new EventData<float>( nameof( OnTimeControlHyperWarpStarted ) );

                OnTimeControlHyperWarpStopping = new EventData<float>( nameof( OnTimeControlHyperWarpStopping ) );
                OnTimeControlHyperWarpStopped = new EventData<float>( nameof( OnTimeControlHyperWarpStopped ) );

                // Slow Motion
                OnTimeControlSlowMoRateChanged = new EventData<float>( nameof( OnTimeControlSlowMoRateChanged ) );

                OnTimeControlSlowMoStarting = new EventData<float>( nameof( OnTimeControlSlowMoStarting ) );
                OnTimeControlSlowMoStarted = new EventData<float>( nameof( OnTimeControlSlowMoStarted ) );

                OnTimeControlSlowMoStopping = new EventData<float>( nameof( OnTimeControlSlowMoStopping ) );
                OnTimeControlSlowMoStopped = new EventData<float>( nameof( OnTimeControlSlowMoStopped ) );

                // Rails Limits Changed
                OnTimeControlCustomWarpRatesChanged = new EventData<bool>( nameof( OnTimeControlCustomWarpRatesChanged ) );

                // Global Settings
                OnTimeControlGlobalSettingsSaved = new EventData<bool>( nameof( OnTimeControlGlobalSettingsSaved ) );
                OnTimeControlGlobalSettingsLoaded = new EventData<bool>( nameof( OnTimeControlGlobalSettingsLoaded ) );

                // Key Bindings
                OnTimeControlKeyBindingsChanged = new EventData<TimeControlKeyBinding>( nameof( OnTimeControlKeyBindingsChanged ) );
            } 
        }       
    }
}

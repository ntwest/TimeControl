using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using TimeControl.KeyBindings;

namespace TimeControl
{
    [KSPAddon( KSPAddon.Startup.Instantly, true )]
    public class TimeControlEvents : MonoBehaviour
    {
        public static EventData<float> OnTimeControlDefaultFixedDeltaTimeChanged;
        public static EventData<float> OnTimeControlFixedDeltaTimeChanged;
        public static EventData<float> OnTimeControlTimeScaleChanged;
        public static EventData<bool> OnTimeControlTimePaused;
        public static EventData<bool> OnTimeControlTimeUnpaused;

        public static EventData<float> OnTimeControlHyperWarpMaximumDeltaTimeChanged;
        public static EventData<float> OnTimeControlHyperWarpMaxAttemptedRateChanged;
        public static EventData<float> OnTimeControlHyperWarpPhysicsAccuracyChanged;        
        public static EventData<float> OnTimeControlHyperWarpStarting;
        public static EventData<float> OnTimeControlHyperWarpStarted;
        public static EventData<float> OnTimeControlHyperWarpStopping;
        public static EventData<float> OnTimeControlHyperWarpStopped;

        public static EventData<bool> OnTimeControlSlowMoDeltaLockedChanged;
        public static EventData<float> OnTimeControlSlowMoRateChanged;
        public static EventData<float> OnTimeControlSlowMoStarting;
        public static EventData<float> OnTimeControlSlowMoStarted;
        public static EventData<float> OnTimeControlSlowMoStopping;
        public static EventData<float> OnTimeControlSlowMoStopped;

        public static EventData<bool> OnTimeControlCustomWarpRatesChanged;
        public static EventData<bool> OnTimeControlCustomHyperWarpRatesChanged;
        public static EventData<bool> OnTimeControlCustomSlowMotionRatesChanged;

        public static EventData<bool> OnTimeControlGlobalSettingsSaved;
        public static EventData<bool> OnTimeControlGlobalSettingsChanged;

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
                OnTimeControlHyperWarpMaximumDeltaTimeChanged = new EventData<float>( nameof( OnTimeControlHyperWarpMaximumDeltaTimeChanged ) );
                OnTimeControlHyperWarpMaxAttemptedRateChanged = new EventData<float>( nameof( OnTimeControlHyperWarpMaxAttemptedRateChanged ) );
                OnTimeControlHyperWarpPhysicsAccuracyChanged = new EventData<float>( nameof( OnTimeControlHyperWarpPhysicsAccuracyChanged ) );

                OnTimeControlCustomHyperWarpRatesChanged = new EventData<bool>( nameof( OnTimeControlCustomHyperWarpRatesChanged ) );
                OnTimeControlCustomSlowMotionRatesChanged = new EventData<bool>( nameof( OnTimeControlCustomSlowMotionRatesChanged ) );

                OnTimeControlHyperWarpStarting = new EventData<float>( nameof( OnTimeControlHyperWarpStarting ) );
                OnTimeControlHyperWarpStarted = new EventData<float>( nameof( OnTimeControlHyperWarpStarted ) );

                OnTimeControlHyperWarpStopping = new EventData<float>( nameof( OnTimeControlHyperWarpStopping ) );
                OnTimeControlHyperWarpStopped = new EventData<float>( nameof( OnTimeControlHyperWarpStopped ) );

                // Slow Motion
                OnTimeControlSlowMoRateChanged = new EventData<float>( nameof( OnTimeControlSlowMoRateChanged ) );
                OnTimeControlSlowMoDeltaLockedChanged = new EventData<bool>( nameof( OnTimeControlSlowMoDeltaLockedChanged ) );

                OnTimeControlSlowMoStarting = new EventData<float>( nameof( OnTimeControlSlowMoStarting ) );
                OnTimeControlSlowMoStarted = new EventData<float>( nameof( OnTimeControlSlowMoStarted ) );

                OnTimeControlSlowMoStopping = new EventData<float>( nameof( OnTimeControlSlowMoStopping ) );
                OnTimeControlSlowMoStopped = new EventData<float>( nameof( OnTimeControlSlowMoStopped ) );

                // Rails Limits Changed
                OnTimeControlCustomWarpRatesChanged = new EventData<bool>( nameof( OnTimeControlCustomWarpRatesChanged ) );

                // Global Settings
                OnTimeControlGlobalSettingsSaved = new EventData<bool>( nameof( OnTimeControlGlobalSettingsSaved ) );
                OnTimeControlGlobalSettingsChanged = new EventData<bool>( nameof( OnTimeControlGlobalSettingsChanged ) );

                // Key Bindings
                OnTimeControlKeyBindingsChanged = new EventData<TimeControlKeyBinding>( nameof( OnTimeControlKeyBindingsChanged ) );
            } 
        }       
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

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using KSP.UI.Dialogs;
using System.Linq;

namespace TimeControl
{
    [KSPAddon( KSPAddon.Startup.Instantly, true )]
    internal class FPSKeeperController : MonoBehaviour
    {
        #region Static
        public static FPSKeeperController Instance { get; private set; }
        public static bool IsReady { get; private set; } = false;
        #endregion
        
        #region MonoBehavior
        private void Awake()
        {
            const string logBlockName = nameof( FPSKeeperController ) + "." + nameof( Awake );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                DontDestroyOnLoad( this );
                Instance = this;
            }
        }

        private void Start()
        {
            const string logBlockName = nameof( FPSKeeperController ) + "." + nameof( Start );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                StartCoroutine( Configure() );
            }
        }

        private void OnDestroy()
        {
            /*
            OnTimeControlSlowMoRateChangedEvent?.Remove( SlowMoRateChanged );
            */
        }
        #endregion

        #region Initialization
        private IEnumerator Configure()
        {
            const string logBlockName = nameof( FPSKeeperController ) + "." + nameof( Configure );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                this.SetDefaults();
                this.SubscribeToGameEvents();

                while (TimeWarp.fetch == null || FlightGlobals.Bodies == null || FlightGlobals.Bodies.Count <= 0)
                {
                    yield return new WaitForSeconds( 1f );
                }

                //Log.Info( nameof( FPSKeeperController ) + " is Ready!", logBlockName );
                //FPSKeeperController.IsReady = true;
                yield break;
            }
        }

        private void SetDefaults()
        {
            const string logBlockName = nameof( FPSKeeperController ) + "." + nameof( SetDefaults );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                /*
                defaultDeltaTime = Time.fixedDeltaTime;
                currentScreenMessage = null;
                currentScreenMessageStyle = ScreenMessageStyle.UPPER_CENTER;
                currentScreenMessageDuration = Mathf.Infinity;
                currentScreenMessagePrefix = "SLOW-MOTION";
                UpdateDefaultScreenMessage();
                */
            }
        }

        private void SubscribeToGameEvents()
        {
            const string logBlockName = nameof( FPSKeeperController ) + "." + nameof( SubscribeToGameEvents );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                /*
                GameEvents.onGamePause.Add( this.onGamePause );
                GameEvents.onGameUnpause.Add( this.onGameUnpause );
                GameEvents.onGameSceneLoadRequested.Add( this.onGameSceneLoadRequested );

                OnTimeControlSlowMoRateChangedEvent = GameEvents.FindEvent<EventData<float>>( nameof( TimeControlEvents.OnTimeControlSlowMoRateChanged ) );
                OnTimeControlSlowMoRateChangedEvent?.Add( SlowMoRateChanged );
                */
            }
        }
        #endregion

        private void Update()
        {
            UpdateFPSKeeper();
        }

        private void UpdateFPSKeeper()
        {
            /*
            if (!IsFpsKeeperActive)
                return;

            fpsMin = (int)Mathf.Round( Settings.Instance.FpsMinSlider / 5 ) * 5;
            if (Mathf.Abs( PerformanceManager.Instance.FPS - fpsMin ) > 2.5)
            {
                if (PerformanceManager.Instance.FPS < fpsMin)
                    fpsKeeperFactor += 1;
                else
                    fpsKeeperFactor -= 1;
            }
            fpsKeeperFactor = Mathf.Clamp( fpsKeeperFactor, 0, 73 ); //0-10 are .01 steps down with max delta, 11-110 are steps of time scale from 1x down to 1/100x in 1/100 increments

            if (fpsKeeperFactor < 11)
            {
                TimeSlider = 0f;
                MaxDeltaTimeSlider = .12f - (fpsKeeperFactor * .01f);

            }
            else
            {
                if 
                SlowMoController.Instance.ActivateSlowMo();

                MaxDeltaTimeSlider = 0.02f;
                TimeSlider = (float)(fpsKeeperFactor - 10) / 64f;
            }
            */
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

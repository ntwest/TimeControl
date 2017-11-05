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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using KSP.IO;

namespace TimeControl
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class PerformanceManager : MonoBehaviour
    {
        #region Singleton
        public static bool IsReady { get; private set; } = false;
        private static PerformanceManager instance;
        public static PerformanceManager Instance { get { return instance; } }
        #endregion

        private int frames = 0;
        private float lastInterval = 0f;
        private double ptrLast = 0d;
        private Queue<double> ptrRollingQ;
        //private double gsLastRT = 0d;
        //private double gsLastUT = 0d;
        //private Queue<double> gsRollingQ;
        private float updateInterval = 0.5f; //half a second
        private bool performanceCountersOn = true;

        /// <summary>
        /// Turn on and off Performance Counters
        /// </summary>
        public bool PerformanceCountersOn {
            get {
                return performanceCountersOn;
            }
            set {
                performanceCountersOn = value;
                if (!performanceCountersOn)
                {
                    this.ptrRollingQ.Clear();
                    //this.gsRollingQ.Clear();
                    frames = 0;
                }
            }
        }
        /// <summary>
        /// Frames Per Second
        /// </summary>
        public double FramesPerSecond { get; set; }

        /// <summary>
        /// Gametime to Realtime Ratio
        /// </summary>
        // public double GametimeToRealtimeRatio { get; set; }

        /// <summary>
        /// Physics Updates Per Second
        /// </summary>
        public double PhysicsUpdatesPerSecond { get; set; }

        /// <summary>
        /// Physics Time Ratio: The ratio of real time to game time. (1 means KSP can handle the physics, less than 1 means time is being slowed by KSP because it can't process all the physics)
        /// </summary>
        public double PhysicsTimeRatio { get; set; }

        private void Awake()
        {
            const string logBlockName = "PerformanceManager.Awake()";
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                DontDestroyOnLoad( this );
                instance = this;
            }
        }

        private void Start()
        {
            const string logBlockName = "PerformanceManager.Start()";
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                ptrRollingQ = new Queue<double>();
                //gsRollingQ = new Queue<double>();
                FramesPerSecond = 0f;
                PhysicsUpdatesPerSecond = 0f;
                PhysicsTimeRatio = 0d;
                PerformanceCountersOn = false;
                IsReady = true;
            }
        }

        //Functions
        private void Update()
        {
            if (!PerformanceCountersOn)
            {
                return;
            }

            float rtss = Time.realtimeSinceStartup;

            UpdateFPS( rtss );
            UpdatePPS( Time.timeScale, Time.fixedDeltaTime );
            UpdatePTR( rtss, Time.deltaTime );
            // UpdateGTRR( rtss, Planetarium.GetUniversalTime() );
        }

        private void UpdateFPS(float rtss)
        {
            //FPS calculation
            frames++;
            if (rtss > lastInterval + updateInterval)
            {
                FramesPerSecond = frames / (rtss - lastInterval);
                frames = 0;
                lastInterval = rtss;
            }
        }

        //private void UpdateGTRR(float rtss, double UT)
        //{
        //    //Time Warp calculation            
        //    gsRollingQ.Enqueue( (UT - gsLastUT) / (rtss - gsLastRT) );
        //    gsLastRT = rtss;
        //    gsLastUT = UT;

        //    while (gsRollingQ.Count > FramesPerSecond)
        //        gsRollingQ.Dequeue();

        //    if (gsRollingQ.Count > 0)
        //        GametimeToRealtimeRatio = gsRollingQ.Average();
        //}

        private void UpdatePPS(float timeScale, float fixedDeltaTime)
        {
            PhysicsUpdatesPerSecond = timeScale / fixedDeltaTime;
        }

        private void UpdatePTR(float rtss, float deltaTime)
        {
            //PTR calculation
            ptrRollingQ.Enqueue( deltaTime / ((double)rtss - ptrLast) );
            ptrLast = rtss;

            while (ptrRollingQ.Count > FramesPerSecond)
                ptrRollingQ.Dequeue();

            if (ptrRollingQ.Count > 0)
                PhysicsTimeRatio = ptrRollingQ.Average();
        }
    }
}

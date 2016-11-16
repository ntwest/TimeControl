/*
All code in this file Copyright(c) 2014 Xaiier

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
        //Public accessible
        public static float fps; //frames per second
        public static float pps; //physics updates per second
        public static double ptr = 0f; //physics time ratio

        //Settings
        private float updateInterval = 0.5f; //half a second

        //Private
        private int frames = 0;
        private float lastInterval = 0f;
        private float last = 0f;
        private float current = 0f;
        private Queue<double> ptrRollingAvg = new Queue<double>();

        //Functions
        private void Update()
        {
            //FPS calculation
            frames++;
            if (Time.realtimeSinceStartup > lastInterval + updateInterval)
            {
                fps = frames / (Time.realtimeSinceStartup - lastInterval);
                frames = 0;
                lastInterval = Time.realtimeSinceStartup;
            }

            //PPS calculation
            pps = Time.timeScale / Time.fixedDeltaTime;

            //PTR calculation
            current = Time.realtimeSinceStartup - last;
            last = Time.realtimeSinceStartup;
            ptrRollingAvg.Enqueue(Time.deltaTime / current);
            while (ptrRollingAvg.Count > fps)
            {
                ptrRollingAvg.Dequeue();
            }
            if (ptrRollingAvg.Count > 0)
            {
                ptr = ptrRollingAvg.Average<double>(num => Convert.ToDouble(num));
            }
        }
        private void Awake()
        {
            UnityEngine.Object.DontDestroyOnLoad(this); //Don't go away on scene changes
        }
    }
}

/*
 * Time Control
 * Created by Xaiier
 * License: MIT
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

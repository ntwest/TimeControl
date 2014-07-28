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
    public class Settings : MonoBehaviour
    {
        //Plugin Configuration
        PluginConfiguration config = PluginConfiguration.CreateForType<TimeControl>();
        //ConfigNode config = new ConfigNode();

        //Window Positions
        public static Rect flightWindowPosition = new Rect();
        public static Rect menuWindowPosition = new Rect();
        public static Rect settingsWindowPosition = new Rect();

        //FPS Display
        public static Rect fpsPosition = new Rect(0, 0, 100, 100);
        public static string fpsX = "155";
        public static string fpsY = "0";

        //Display
        public static Boolean minimized = false; //small mode
        public static Boolean visible = false; //toolbar and hiding when appropriate

        //Options
        public static int mode = 0;
        public static Boolean camFix = false;
        public static Boolean showFPS = false;
        public static Boolean fpsKeeperActive = false;
        public static int fpsMinSlider = 5;
        public static float maxDeltaTimeSlider = GameSettings.PHYSICS_FRAME_DT_LIMIT;

        //Rails warp stuff
        public static string[] customWarpRates = new string[8];
        public static string[,] customAltitudeLimits = new string[17, 8];
        public static string[] standardWarpRates = { "1", "5", "10", "50", "100", "1000", "10000", "100000" };
        public static string[,] standardAltitudeLimits = 
        {
        {"0","3270000","3270000","6540000","1.308E+07","2.616E+07","5.232E+07","6.54E+07"},
        {"0","30000","30000","60000","120000","240000","480000","600000"},
        {"0","5000","5000","10000","25000","50000","100000","200000"},
        {"0","3000","3000","6000","12000","24000","48000","60000"},
        {"0","10000","10000","30000","50000","100000","200000","300000"},
        {"0","30000","30000","60000","120000","240000","480000","600000"},
        {"0","30000","30000","60000","100000","300000","600000","800000"},
        {"0","5000","5000","10000","25000","50000","100000","200000"},
        {"0","0","15000","60000","150000","300000","600000","1200000"},
        {"0","30000","30000","60000","120000","240000","480000","600000"},
        {"0","24500","24500","24500","40000","60000","80000","100000"},
        {"0","24500","24500","24500","40000","60000","80000","100000"},
        {"0","30000","30000","60000","120000","240000","480000","600000"},
        {"0","8000","8000","8000","20000","40000","80000","100000"},
        {"0","5000","5000","5000","8000","12000","30000","90000"},
        {"0","10000","10000","30000","50000","100000","200000","300000"},
        {"0","4000","4000","20000","30000","40000","70000","150000"}
        };

        public static KeyBinding[] keyBinds = { new KeyBinding(KeyCode.None), new KeyBinding(KeyCode.None), new KeyBinding(KeyCode.None), new KeyBinding(KeyCode.None), new KeyBinding(KeyCode.None), new KeyBinding(KeyCode.None) };

        private void Start()
        {
            //config.load();
            //loadData();
        }
        private void Awake()
        {
            UnityEngine.Object.DontDestroyOnLoad(this); //Don't go away on scene changes
        }
        private void Update()
        {
            if ((int)Time.realtimeSinceStartup % 5 == 0) //save every 5 seconds
            {
                GameSettings.PHYSICS_FRAME_DT_LIMIT = maxDeltaTimeSlider;
                GameSettings.SaveSettings();
                //saveData();
                //config.save();
            }
        }
        private void loadData()
        {
            minimized = config.GetValue<Boolean>("Minimized", false);
            visible = config.GetValue<Boolean>("Toolbar-Visible", true);

            flightWindowPosition = config.GetValue<Rect>("Flight Window Position");
            menuWindowPosition = config.GetValue<Rect>("Menu Window Position");
            settingsWindowPosition = config.GetValue<Rect>("Settings Window Position");
            fpsPosition = config.GetValue<Rect>("FPS Position");

            mode = config.GetValue<int>("Mode", 0);
            camFix = config.GetValue<Boolean>("Cam Fix", false);
            fpsKeeperActive = config.GetValue<Boolean>("FPS Keeper", false);
            fpsMinSlider = config.GetValue<int>("FPS Keeper Setting", 5);
            showFPS = config.GetValue<Boolean>("Show FPS", false);
            //maxDeltaTimeSlider = config.GetValue<float>("MaxDelta", GameSettings.PHYSICS_FRAME_DT_LIMIT);

            for (int i = 0; i < 6; i++)
            {
                keyBinds[i] = new KeyBinding(config.GetValue<KeyCode>("Key Setting " + i.ToString(), KeyCode.None));
            }

            //rails data
            for (int i = 0; i < 8; i++)
            {
                customWarpRates[i] = config.GetValue<string>("Warp Rate " + i.ToString(), standardWarpRates[i]);

                for (int j = 0; j < 17; j++)
                {
                    customAltitudeLimits[j, i] = config.GetValue<string>("Body " + j.ToString() + " Altitude Limit " + i.ToString(), standardAltitudeLimits[j, i]);
                }
            }
        }
        private void saveData()
        {
            config.SetValue("Minimized", minimized);
            config.SetValue("Toolbar-Visible", visible);

            config.SetValue("Flight Window Position", flightWindowPosition);
            config.SetValue("Menu Window Position", menuWindowPosition);
            config.SetValue("Settings Window Position", settingsWindowPosition);
            config.SetValue("FPS Position", fpsPosition);

            config.SetValue("Mode", mode);
            config.SetValue("Cam Fix", camFix);
            config.SetValue("FPS Keeper", fpsKeeperActive);
            config.SetValue("FPS Keeper Setting", fpsMinSlider);
            config.SetValue("Show FPS", showFPS);
            //config.SetValue("MaxDelta", maxDeltaTimeSlider);

            for (int i = 0; i < 6; i++)
            {
                config.SetValue("Key Setting " + i.ToString(), keyBinds[i].primary);
            }

            //rails data
            for (int i = 0; i < 8; i++)
            {
                config.SetValue("Warp Rate " + i.ToString(), customWarpRates[i]);

                for (int j = 0; j < 17; j++)
                {
                    config.SetValue("Body " + j.ToString() + " Altitude Limit " + i.ToString(), customAltitudeLimits[j, i]);
                }
            }
        }
    }
}

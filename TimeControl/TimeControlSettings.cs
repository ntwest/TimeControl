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
        ConfigNode config;

        //Window Positions
        public static Rect flightWindowPosition = new Rect(1,1,1,1);
        public static Rect menuWindowPosition = new Rect(1,1,1,1);
        public static Rect settingsWindowPosition = new Rect(1,1,1,1);

        //FPS Display
        public static Rect fpsPosition = new Rect(155, 0, 100, 100);
        public static string fpsX = "155";
        public static string fpsY = "0";

        //Display
        public static Boolean minimized = false; //small mode
        public static Boolean visible = false; //toolbar and hiding when appropriate

        //Options
        public static int mode = 0;
        public static Boolean camFix = true;
        public static Boolean showFPS = false;
        public static Boolean fpsKeeperActive = false;
        public static int fpsMinSlider = 5;
        public static float maxDeltaTimeSlider = GameSettings.PHYSICS_FRAME_DT_LIMIT;

        //Rails warp stuff
        public static string[] customWarpRates = new string[8];
		public static string[,] customAltitudeLimits = null;
        public static string[] standardWarpRates = { "1", "5", "10", "50", "100", "1000", "10000", "100000" };
		public static string[,] standardAltitudeLimits = null;

        public static KeyBinding[] keyBinds = { new KeyBinding(KeyCode.None), new KeyBinding(KeyCode.None), new KeyBinding(KeyCode.None), new KeyBinding(KeyCode.None), new KeyBinding(KeyCode.None), new KeyBinding(KeyCode.None) };

        private string path = KSPUtil.ApplicationRootPath + "GameData/TimeControl/config.txt";

		//
		// Change by Nathaniel R. Lewis (aka Teknoman117) (linux.robotdude@gmail.com)
		// 
		// I didn't want to tweak too much of the existing code, but since this object is 
		// invoked as a behavior and not a singleton on first use, we need to guarentee that
		// the config data is alive before anything makes an attempt to access it.  Putting 
		// the config loader in Awake() achieves this (for now).
		//
		// The custom and standard altitude limit objects are also allocated in Awake() to adjust 
		// dynamically for future worlds from Squad and from planet adding mods such as the
		// upcoming Kopernicus mod.
		// 
		private void Awake()
		{
			// Don't go away on scene changes
			UnityEngine.Object.DontDestroyOnLoad(this); 

			// Allocate here to account for custom worlds
			customAltitudeLimits = new string[PSystemManager.Instance.localBodies.Count, 8];
			standardAltitudeLimits = new string[PSystemManager.Instance.localBodies.Count, 8];

			// Load standard altitude limits from worlds
			int referenceId = 0;
			foreach (CelestialBody celestialBody in PSystemManager.Instance.localBodies) 
			{
				// We need to convert the altitude limits into strings
				int index = 0;
				foreach (float limit in celestialBody.timeWarpAltitudeLimits)
				{
					standardAltitudeLimits[referenceId, index++] = Convert.ToInt64(limit).ToString();
				}

				// Increment reference id
				referenceId++;
			}

			// Load config
			config = ConfigNode.Load(path);
			if (config == null)//file does not exist
			{
				buildConfig();
				loadConfig();
			}
			else
			{
				if(loadConfig())
				{
					// Config structure was changed, need to save changes
					saveConfig();
				}
			}
		}

        private void Update()
        {
            if ((int)Time.realtimeSinceStartup % 10 == 0) //save every 10 seconds
            {
                GameSettings.PHYSICS_FRAME_DT_LIMIT = maxDeltaTimeSlider;
                GameSettings.SaveSettings();

                saveConfig();
            }
        }

        private void buildConfig()
        {
            config = new ConfigNode();

            config.AddValue("minimized", minimized);
            config.AddValue("visible", visible);

            ConfigNode flightWindowPositionNode = config.AddNode("flightWindowPosition");
            flightWindowPositionNode.AddValue("xMin", flightWindowPosition.xMin);
            flightWindowPositionNode.AddValue("xMax", flightWindowPosition.xMax);
            flightWindowPositionNode.AddValue("yMin", flightWindowPosition.yMin);
            flightWindowPositionNode.AddValue("yMax", flightWindowPosition.yMax);

            ConfigNode menuWindowPositionNode = config.AddNode("menuWindowPosition");
            menuWindowPositionNode.AddValue("xMin", menuWindowPosition.xMin);
            menuWindowPositionNode.AddValue("xMax", menuWindowPosition.xMax);
            menuWindowPositionNode.AddValue("yMin", menuWindowPosition.yMin);
            menuWindowPositionNode.AddValue("yMax", menuWindowPosition.yMax);

            ConfigNode settingsWindowPositionNode = config.AddNode("settingsWindowPosition");
            settingsWindowPositionNode.AddValue("xMin", settingsWindowPosition.xMin);
            settingsWindowPositionNode.AddValue("xMax", settingsWindowPosition.xMax);
            settingsWindowPositionNode.AddValue("yMin", settingsWindowPosition.yMin);
            settingsWindowPositionNode.AddValue("yMax", settingsWindowPosition.yMax);

            ConfigNode fpsPositionNode = config.AddNode("fpsPosition");
            fpsPositionNode.AddValue("xMin", fpsPosition.xMin);
            fpsPositionNode.AddValue("xMax", fpsPosition.xMax);
            fpsPositionNode.AddValue("yMin", fpsPosition.yMin);
            fpsPositionNode.AddValue("yMax", fpsPosition.yMax);

            config.AddValue("mode", mode);
            config.AddValue("camFix", camFix);
            config.AddValue("fpsKeeperActive", fpsKeeperActive);
            config.AddValue("fpsMinSlider", fpsMinSlider);
            config.AddValue("showFPS", showFPS);

            ConfigNode keyBindsNode = config.AddNode("keyBinds");
            for (int i = 0; i < keyBinds.Length; i++)
            {
                keyBindsNode.AddValue("keyBind"+i, keyBinds[i].primary);
            }

            ConfigNode customWarpRatesNode = config.AddNode("customWarpRates");
            for (int i = 0; i < standardWarpRates.Length; i++)
            {
                customWarpRatesNode.AddValue("customWarpRate" + i, standardWarpRates[i]);
            }

            ConfigNode customAltitudeLimitsNode = config.AddNode("customAltitudeLimits");
            for (int i = 0; i < standardAltitudeLimits.GetLength(0); i++)
            {
                ConfigNode celestial = customAltitudeLimitsNode.AddNode("celestial" + i);
                for (int j = 0; j < standardAltitudeLimits.GetLength(1); j++)
                {
                    celestial.AddValue("customAltitudeLimit" + j, standardAltitudeLimits[i, j]);
                }
            }

            config.Save(path);
        }
		private bool loadConfig()
		{
			config = ConfigNode.Load(path);
			bool updatedConfig = false;

			minimized = bool.Parse(config.GetValue("minimized"));
			visible = bool.Parse(config.GetValue("visible"));

			ConfigNode flightWindowPositionNode = config.GetNode("flightWindowPosition");
			flightWindowPosition.xMin = float.Parse(flightWindowPositionNode.GetValue("xMin"));
			flightWindowPosition.xMax = float.Parse(flightWindowPositionNode.GetValue("xMax"));
			flightWindowPosition.yMin = float.Parse(flightWindowPositionNode.GetValue("yMin"));
			flightWindowPosition.yMax = float.Parse(flightWindowPositionNode.GetValue("yMax"));

			ConfigNode menuWindowPositionNode = config.GetNode("menuWindowPosition");
			menuWindowPosition.xMin = float.Parse(menuWindowPositionNode.GetValue("xMin"));
			menuWindowPosition.xMax = float.Parse(menuWindowPositionNode.GetValue("xMax"));
			menuWindowPosition.yMin = float.Parse(menuWindowPositionNode.GetValue("yMin"));
			menuWindowPosition.yMax = float.Parse(menuWindowPositionNode.GetValue("yMax"));

			ConfigNode settingsWindowPositionNode = config.GetNode("settingsWindowPosition");
			settingsWindowPosition.xMin = float.Parse(settingsWindowPositionNode.GetValue("xMin"));
			settingsWindowPosition.xMax = float.Parse(settingsWindowPositionNode.GetValue("xMax"));
			settingsWindowPosition.yMin = float.Parse(settingsWindowPositionNode.GetValue("yMin"));
			settingsWindowPosition.yMax = float.Parse(settingsWindowPositionNode.GetValue("yMax"));

			ConfigNode fpsPositionNode = config.GetNode("fpsPosition");
			fpsPosition.xMin = float.Parse(fpsPositionNode.GetValue("xMin"));
			fpsPosition.xMax = float.Parse(fpsPositionNode.GetValue("xMax"));
			fpsPosition.yMin = float.Parse(fpsPositionNode.GetValue("yMin"));
			fpsPosition.yMax = float.Parse(fpsPositionNode.GetValue("yMax"));

			mode = int.Parse(config.GetValue("mode"));
			camFix = bool.Parse(config.GetValue("camFix"));
			fpsKeeperActive = bool.Parse(config.GetValue("fpsKeeperActive"));
			fpsMinSlider = int.Parse(config.GetValue("fpsMinSlider"));
			showFPS = bool.Parse(config.GetValue("showFPS"));

			ConfigNode keyBindsNode = config.GetNode("keyBinds");
			for (int i = 0; i < keyBinds.Length; i++)
			{
				keyBinds[i].primary = (KeyCode)Enum.Parse(typeof(KeyCode), keyBindsNode.GetValue("keyBind" + i));
			}

			ConfigNode customWarpRatesNode = config.GetNode("customWarpRates");
			for (int i = 0; i < customWarpRates.Length; i++)
			{
				customWarpRates[i] = customWarpRatesNode.GetValue("customWarpRate" + i);
			}

			//
			// Change by Nathaniel R. Lewis (aka Teknoman117) (linux.robotdude@gmail.com)
			// 
			// Previous implementation of this section of this method based assumed all celestial entries 
			// would be present.  In the event the planetary system changes between loads of this file,
			// the loader would break attempting to parse a non-existant node.
			//
			// New implementation checks if the desired altitude limit node is present.  If not, it loads
			// the standard limit for that body.  When this occurs, the flag "updatedConfig" is set to 
			// indicate that the config file needs to be saved.  This ensures compatibility with future 
			// worlds from Squad and from planet adding mods such as the upcoming Kopernicus mod.
			// 
			ConfigNode customAltitudeLimitsNode = config.GetNode("customAltitudeLimits");
			for (int i = 0; i < customAltitudeLimits.GetLength(0); i++)
			{
				string celestialName = ("celestial" + i);

				// If the config file has an entry for this celestial body, load it
				if(customAltitudeLimitsNode.HasNode(celestialName))
				{
					ConfigNode celestial = customAltitudeLimitsNode.GetNode(celestialName);
					for (int j = 0; j < customAltitudeLimits.GetLength(1); j++)
					{
						customAltitudeLimits[i,j] = celestial.GetValue("customAltitudeLimit" + j);
					}
				}

				// Otherwise we need to load it from the defaults and set the updatedConfig flag (handle planet changing)
				else
				{
					updatedConfig = true;
					ConfigNode celestial = customAltitudeLimitsNode.AddNode(celestialName);
					for (int j = 0; j < customAltitudeLimits.GetLength(1); j++)
					{
						customAltitudeLimits[i,j] = standardAltitudeLimits[i,j];
						celestial.AddValue("customAltitudeLimit" + j, standardAltitudeLimits[i,j]);
					}
				}
			}

			// Return whether we updated the config (added more bodies)
			return updatedConfig;
		}
        private void saveConfig()
        {
            config.SetValue("minimized", minimized.ToString());
            config.SetValue("visible", visible.ToString());

            ConfigNode flightWindowPositionNode = config.GetNode("flightWindowPosition");
            flightWindowPositionNode.SetValue("xMin", flightWindowPosition.xMin.ToString());
            flightWindowPositionNode.SetValue("xMax", flightWindowPosition.xMax.ToString());
            flightWindowPositionNode.SetValue("yMin", flightWindowPosition.yMin.ToString());
            flightWindowPositionNode.SetValue("yMax", flightWindowPosition.yMax.ToString());

            ConfigNode menuWindowPositionNode = config.GetNode("menuWindowPosition");
            menuWindowPositionNode.SetValue("xMin", menuWindowPosition.xMin.ToString());
            menuWindowPositionNode.SetValue("xMax", menuWindowPosition.xMax.ToString());
            menuWindowPositionNode.SetValue("yMin", menuWindowPosition.yMin.ToString());
            menuWindowPositionNode.SetValue("yMax", menuWindowPosition.yMax.ToString());

            ConfigNode settingsWindowPositionNode = config.GetNode("settingsWindowPosition");
            settingsWindowPositionNode.SetValue("xMin", settingsWindowPosition.xMin.ToString());
            settingsWindowPositionNode.SetValue("xMax", settingsWindowPosition.xMax.ToString());
            settingsWindowPositionNode.SetValue("yMin", settingsWindowPosition.yMin.ToString());
            settingsWindowPositionNode.SetValue("yMax", settingsWindowPosition.yMax.ToString());

            ConfigNode fpsPositionNode = config.GetNode("fpsPosition");
            fpsPositionNode.SetValue("xMin", fpsPosition.xMin.ToString());
            fpsPositionNode.SetValue("xMax", fpsPosition.xMax.ToString());
            fpsPositionNode.SetValue("yMin", fpsPosition.yMin.ToString());
            fpsPositionNode.SetValue("yMax", fpsPosition.yMax.ToString());

            config.SetValue("mode", mode.ToString());
            config.SetValue("camFix", camFix.ToString());
            config.SetValue("fpsKeeperActive", fpsKeeperActive.ToString());
            config.SetValue("fpsMinSlider", fpsMinSlider.ToString());
            config.SetValue("showFPS", showFPS.ToString());

            ConfigNode keyBindsNode = config.GetNode("keyBinds");
            for (int i = 0; i < keyBinds.Length; i++)
            {
                keyBindsNode.SetValue("keyBind" + i, keyBinds[i].primary.ToString());
            }

            ConfigNode customWarpRatesNode = config.GetNode("customWarpRates");
            for (int i = 0; i < customWarpRates.Length; i++)
            {
                customWarpRatesNode.SetValue("customWarpRate" + i, customWarpRates[i]);
            }

            ConfigNode customAltitudeLimitsNode = config.GetNode("customAltitudeLimits");
            for (int i = 0; i < customAltitudeLimits.GetLength(0); i++)
            {
                ConfigNode celestial = customAltitudeLimitsNode.GetNode("celestial" + i);
                for (int j = 0; j < customAltitudeLimits.GetLength(1); j++)
                {
                    celestial.SetValue("customAltitudeLimit" + j, customAltitudeLimits[i, j]);
                }
            }

            config.Save(path);
        }
    }
}

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
        public static Boolean visible = true; //toolbar and hiding

        //Options
        public static int mode = 0;
        public static Boolean camFix = true;
        public static Boolean showFPS = false;
        public static Boolean fpsKeeperActive = false;
        public static int fpsMinSlider = 5;
        public static float maxDeltaTimeSlider = GameSettings.PHYSICS_FRAME_DT_LIMIT;

        //Rails warp stuff
        public static int warpLevels = 8;
        public static string[] standardWarpRates = { "1", "5", "10", "50", "100", "1000", "10000", "100000" };
        public static List<string> customWarpRates = new List<string>(standardWarpRates);
        public static string[][] standardAltitudeLimits = null;
        public static List<List<string>> customAltitudeLimits = new List<List<string>>();

        public static KeyBinding[] keyBinds = { new KeyBinding(KeyCode.None),
                                                new KeyBinding(KeyCode.None),
                                                new KeyBinding(KeyCode.None),
                                                new KeyBinding(KeyCode.None),
                                                new KeyBinding(KeyCode.None),
                                                new KeyBinding(KeyCode.None),
                                                new KeyBinding(KeyCode.None),
                                                new KeyBinding(KeyCode.None) };
        public static float customKeySlider = 0f;

        private string path = KSPUtil.ApplicationRootPath + "GameData/TimeControl/config.txt";

		private void Start()
		{
			UnityEngine.Object.DontDestroyOnLoad(this);

			standardAltitudeLimits = new string[PSystemManager.Instance.localBodies.Count][];
            for (int i = 0; i < PSystemManager.Instance.localBodies.Count; i++)
            {
                standardAltitudeLimits[i] = new string[warpLevels];
            }

			int referenceId = 0;
			foreach (CelestialBody celestialBody in PSystemManager.Instance.localBodies)
			{
				int index = 0;
				foreach (float limit in celestialBody.timeWarpAltitudeLimits)
				{
					standardAltitudeLimits[referenceId][index++] = Convert.ToInt64(limit).ToString();
				}

				referenceId++;
			}

            for (int i = 0; i < PSystemManager.Instance.localBodies.Count; i++)
            {
                customAltitudeLimits.Add(new List<string>(standardAltitudeLimits[i]));
            }

			//LOAD CONFIG
			config = ConfigNode.Load(path);
			if (config == null)//file does not exist
			{
				buildConfig();
				loadConfig();
			}
			else
			{
                loadConfig();
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

            //INTERNAL
            config.AddValue("visible", visible);
            config.AddValue("minimized", minimized);
            config.AddValue("mode", mode);

            //WINDOW POSITIONS
            ConfigNode flightWindowPositionNode = config.AddNode("flightWindowPosition");
            flightWindowPositionNode.AddValue("xMin", flightWindowPosition.xMin);
            flightWindowPositionNode.AddValue("yMin", flightWindowPosition.yMin);

            ConfigNode menuWindowPositionNode = config.AddNode("menuWindowPosition");
            menuWindowPositionNode.AddValue("xMin", menuWindowPosition.xMin);
            menuWindowPositionNode.AddValue("yMin", menuWindowPosition.yMin);

            ConfigNode settingsWindowPositionNode = config.AddNode("settingsWindowPosition");
            settingsWindowPositionNode.AddValue("xMin", settingsWindowPosition.xMin);
            settingsWindowPositionNode.AddValue("yMin", settingsWindowPosition.yMin);

            //USER
            config.AddValue("camFix", camFix);
            config.AddValue("fpsKeeperActive", fpsKeeperActive);
            config.AddValue("fpsMinSlider", fpsMinSlider);

            config.AddValue("showFPS", showFPS);
            ConfigNode fpsPositionNode = config.AddNode("fpsPosition");
            fpsPositionNode.AddValue("xMin", fpsPosition.xMin);
            fpsPositionNode.AddValue("yMin", fpsPosition.yMin);

            //KEYBINDS
            ConfigNode keyBindsNode = config.AddNode("keyBinds");
            for (int i = 0; i < keyBinds.Length; i++)
            {
                keyBindsNode.AddValue("keyBind"+i, keyBinds[i].primary);
            }
            config.AddValue("customKeySlider", customKeySlider);

            //WARP RATES
            config.AddValue("warpLevels", warpLevels);
            ConfigNode customWarpRatesNode = config.AddNode("customWarpRates");
            for (int i = 0; i < standardWarpRates.Length; i++)
            {
                customWarpRatesNode.AddValue("customWarpRate" + i, standardWarpRates[i]);
            }

            //ALTITUDE LIMITS
            ConfigNode customAltitudeLimitsNode = config.AddNode("customAltitudeLimits");
            for (int i = 0; i < standardAltitudeLimits.Length; i++)
            {
                ConfigNode celestial = customAltitudeLimitsNode.AddNode("celestial" + i);
                for (int j = 0; j < standardAltitudeLimits[i].Length; j++)
                {
                    celestial.AddValue("customAltitudeLimit" + j, standardAltitudeLimits[i][j]);
                }
            }

            config.Save(path);
        }
		private void loadConfig()
		{
            try
            {
                config = ConfigNode.Load(path);

                //INTERNAL
                visible = bool.Parse(config.GetValue("visible"));
                minimized = bool.Parse(config.GetValue("minimized"));
                mode = int.Parse(config.GetValue("mode"));

                //WINDOW POSITIONS
                ConfigNode flightWindowPositionNode = config.GetNode("flightWindowPosition");
                flightWindowPosition.xMin = float.Parse(flightWindowPositionNode.GetValue("xMin"));
                flightWindowPosition.yMin = float.Parse(flightWindowPositionNode.GetValue("yMin"));

                ConfigNode menuWindowPositionNode = config.GetNode("menuWindowPosition");
                menuWindowPosition.xMin = float.Parse(menuWindowPositionNode.GetValue("xMin"));
                menuWindowPosition.yMin = float.Parse(menuWindowPositionNode.GetValue("yMin"));

                ConfigNode settingsWindowPositionNode = config.GetNode("settingsWindowPosition");
                settingsWindowPosition.xMin = float.Parse(settingsWindowPositionNode.GetValue("xMin"));
                settingsWindowPosition.yMin = float.Parse(settingsWindowPositionNode.GetValue("yMin"));

                //USER
                camFix = bool.Parse(config.GetValue("camFix"));
                fpsKeeperActive = bool.Parse(config.GetValue("fpsKeeperActive"));
                fpsMinSlider = int.Parse(config.GetValue("fpsMinSlider"));

                showFPS = bool.Parse(config.GetValue("showFPS"));
                ConfigNode fpsPositionNode = config.GetNode("fpsPosition");
                fpsPosition.xMin = float.Parse(fpsPositionNode.GetValue("xMin"));
                fpsPosition.yMin = float.Parse(fpsPositionNode.GetValue("yMin"));

                //KEYBINDS
                ConfigNode keyBindsNode = config.GetNode("keyBinds");
                for (int i = 0; i < keyBinds.Length; i++)
                {
                    keyBinds[i].primary = (KeyCode)Enum.Parse(typeof(KeyCode), keyBindsNode.GetValue("keyBind" + i));
                }
                customKeySlider = float.Parse(config.GetValue("customKeySlider"));

                //WARP RATES
                warpLevels = (int)Mathf.Clamp(int.Parse(config.GetValue("warpLevels")), 8, Mathf.Infinity);
                ConfigNode customWarpRatesNode = config.GetNode("customWarpRates");
                for (int i = 0; i < warpLevels; i++)
                {
                    if (customWarpRatesNode.HasValue("customWarpRate" + i))
                    {
                        customWarpRates[i] = customWarpRatesNode.GetValue("customWarpRate" + i);
                    }
                    else
                    {
                        if (i > 7)//only bother if its not already there by standard
                        {
                            customWarpRates[i] = customWarpRates[i - 1];
                            customWarpRatesNode.AddValue("customWarpRate" + i, customWarpRates[i]);
                        }
                    }
                }

                //ALTITUDE LIMITS
                //TODO properly set up altitude limits
                ConfigNode customAltitudeLimitsNode = config.GetNode("customAltitudeLimits");
                for (int i = 0; i < customAltitudeLimits.Count; i++)
                {
                    string celestialName = ("celestial" + i);

                    if (customAltitudeLimitsNode.HasNode(celestialName))
                    {
                        ConfigNode celestialLimitsNode = customAltitudeLimitsNode.GetNode(celestialName);
                        for (int j = 0; j < customAltitudeLimits[0].Count; j++)
                        {
                            customAltitudeLimits[i][j] = celestialLimitsNode.GetValue("customAltitudeLimit" + j);
                        }
                    }
                    else
                    {
                        ConfigNode celestialLimitsNode = customAltitudeLimitsNode.AddNode(celestialName);
                        for (int j = 0; j < warpLevels; j++)
                        {
                            customAltitudeLimits[i][j] = standardAltitudeLimits[i][j];
                            celestialLimitsNode.AddValue("customAltitudeLimit" + j, standardAltitudeLimits[i][j]);
                        }
                    }
                }
                saveConfig();
            }
            catch (Exception e)
            {
                if (e is ArgumentNullException || e is NullReferenceException)
                {
                    print("Time Control: config load failed, rebuilding");
                    buildConfig();
                }
                else
                {
                    print("Time Control: something went horribly wrong");
                    throw e;
                }
            }
		}
        private void saveConfig()
        {
            //INTERNAL
            config.SetValue("visible", visible.ToString());
            config.SetValue("minimized", minimized.ToString());
            config.SetValue("mode", mode.ToString());

            //WINDOW POSITIONS
            ConfigNode flightWindowPositionNode = config.GetNode("flightWindowPosition");
            flightWindowPositionNode.SetValue("xMin", flightWindowPosition.xMin.ToString());
            flightWindowPositionNode.SetValue("yMin", flightWindowPosition.yMin.ToString());

            ConfigNode menuWindowPositionNode = config.GetNode("menuWindowPosition");
            menuWindowPositionNode.SetValue("xMin", menuWindowPosition.xMin.ToString());
            menuWindowPositionNode.SetValue("yMin", menuWindowPosition.yMin.ToString());

            ConfigNode settingsWindowPositionNode = config.GetNode("settingsWindowPosition");
            settingsWindowPositionNode.SetValue("xMin", settingsWindowPosition.xMin.ToString());
            settingsWindowPositionNode.SetValue("yMin", settingsWindowPosition.yMin.ToString());

            //USER
            config.SetValue("camFix", camFix.ToString());
            config.SetValue("fpsKeeperActive", fpsKeeperActive.ToString());
            config.SetValue("fpsMinSlider", fpsMinSlider.ToString());

            config.SetValue("showFPS", showFPS.ToString());
            ConfigNode fpsPositionNode = config.GetNode("fpsPosition");
            fpsPositionNode.SetValue("xMin", fpsPosition.xMin.ToString());
            fpsPositionNode.SetValue("yMin", fpsPosition.yMin.ToString());

            //KEYBINDS
            ConfigNode keyBindsNode = config.GetNode("keyBinds");
            for (int i = 0; i < keyBinds.Length; i++)
            {
                keyBindsNode.SetValue("keyBind" + i, keyBinds[i].primary.ToString());
            }
            config.SetValue("customKeySlider", customKeySlider.ToString());

            //WARP RATES
            config.SetValue("warpLevels", warpLevels.ToString());
            ConfigNode customWarpRatesNode = config.GetNode("customWarpRates");
            for (int i = 0; i < warpLevels; i++)
            {
                if (customWarpRatesNode.HasValue("customWarpRate" + i))
                {
                    customWarpRatesNode.SetValue("customWarpRate" + i, customWarpRates[i]);
                }
                else
                {
                    customWarpRatesNode.AddValue("customWarpRate" + i, customWarpRates[i]);
                }
            }

            //ALTITUDE LIMITS
            ConfigNode customAltitudeLimitsNode = config.GetNode("customAltitudeLimits");
            for (int i = 0; i < customAltitudeLimits.Count; i++)
            {
                ConfigNode celestial = customAltitudeLimitsNode.GetNode("celestial" + i);
                for (int j = 0; j < customAltitudeLimits[0].Count; j++)
                {
                    celestial.SetValue("customAltitudeLimit" + j, customAltitudeLimits[i][j]);
                }
            }

            config.Save(path);
        }//TODO rewrite save

    }
}

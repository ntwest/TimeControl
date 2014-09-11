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

        private float lastSave = 0f;
        private float saveInterval = 10f;

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
				buildConfig(false);
				loadConfig();//reload it, just to be sure
			}
			else
			{
                loadConfig();
			}
		}

        private void Update()
        {
            float now = Time.realtimeSinceStartup;

            if (now - lastSave > saveInterval)
            {
                lastSave = now;

                GameSettings.PHYSICS_FRAME_DT_LIMIT = maxDeltaTimeSlider;
                GameSettings.SaveSettings();

                saveConfig();
            }
        }

        private void buildConfig(bool rebuildRails)
        {
            if (!rebuildRails)
            {
                config = new ConfigNode();

                //INTERNAL DATA NODE
                ConfigNode internalData = config.AddNode("internalData");

                //INTERNAL
                internalData.AddValue("visible", visible);
                internalData.AddValue("minimized", minimized);
                internalData.AddValue("mode", mode);

                //WINDOW POSITIONS
                ConfigNode flightWindowPositionNode = internalData.AddNode("flightWindowPosition");
                flightWindowPositionNode.AddValue("x", flightWindowPosition.xMin);
                flightWindowPositionNode.AddValue("y", flightWindowPosition.yMin);

                ConfigNode menuWindowPositionNode = internalData.AddNode("menuWindowPosition");
                menuWindowPositionNode.AddValue("x", menuWindowPosition.xMin);
                menuWindowPositionNode.AddValue("y", menuWindowPosition.yMin);

                ConfigNode settingsWindowPositionNode = internalData.AddNode("settingsWindowPosition");
                settingsWindowPositionNode.AddValue("x", settingsWindowPosition.xMin);
                settingsWindowPositionNode.AddValue("y", settingsWindowPosition.yMin);

                //USER
                internalData.AddValue("camFix", camFix);
                internalData.AddValue("fpsKeeperActive", fpsKeeperActive);
                internalData.AddValue("fpsMinSlider", fpsMinSlider);

                internalData.AddValue("showFPS", showFPS);
                ConfigNode fpsPositionNode = internalData.AddNode("fpsPosition");
                fpsPositionNode.AddValue("x", fpsPosition.xMin);
                fpsPositionNode.AddValue("y", fpsPosition.yMin);

                //KEYBINDS
                ConfigNode keyBindsNode = internalData.AddNode("keyBinds");
                for (int i = 0; i < keyBinds.Length; i++)
                {
                    keyBindsNode.AddValue("keyBind" + i, keyBinds[i].primary);
                }
                internalData.AddValue("customKeySlider", customKeySlider);
            }
            else //remove the rails data
            {
                warpLevels = 8;
                try
                {
                    config.RemoveNodes("railsData");
                }
                catch //no rails data means the config is screwed up
                {
                    buildConfig(false);
                    return;
                }
            }

            //RAILS DATA NODE
            ConfigNode railsData = config.AddNode("railsData");

            //WARP RATES
            railsData.AddValue("warpLevels", warpLevels);
            ConfigNode customWarpRatesNode = railsData.AddNode("customWarpRates");
            for (int i = 0; i < warpLevels; i++)
            {
                customWarpRatesNode.AddValue("customWarpRate" + i, standardWarpRates[i]);
            }

            //ALTITUDE LIMITS
            ConfigNode customAltitudeLimitsNode = railsData.AddNode("customAltitudeLimits");
            for (int i = 0; i < standardAltitudeLimits.Length; i++)
            {
                ConfigNode celestial = customAltitudeLimitsNode.AddNode("celestial" + i);
                for (int j = 0; j < warpLevels; j++)
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
                config = ConfigNode.Load(path);//redundant?

                //INTERNAL DATA NODE
                ConfigNode internalData = config.GetNode("internalData");

                //INTERNAL
                visible = bool.Parse(internalData.GetValue("visible"));
                minimized = bool.Parse(internalData.GetValue("minimized"));
                mode = int.Parse(internalData.GetValue("mode"));

                //WINDOW POSITIONS
                ConfigNode flightWindowPositionNode = internalData.GetNode("flightWindowPosition");
                flightWindowPosition.xMin = float.Parse(flightWindowPositionNode.GetValue("x"));
                flightWindowPosition.yMin = float.Parse(flightWindowPositionNode.GetValue("y"));

                ConfigNode menuWindowPositionNode = internalData.GetNode("menuWindowPosition");
                menuWindowPosition.xMin = float.Parse(menuWindowPositionNode.GetValue("x"));
                menuWindowPosition.yMin = float.Parse(menuWindowPositionNode.GetValue("y"));

                ConfigNode settingsWindowPositionNode = internalData.GetNode("settingsWindowPosition");
                settingsWindowPosition.xMin = float.Parse(settingsWindowPositionNode.GetValue("x"));
                settingsWindowPosition.yMin = float.Parse(settingsWindowPositionNode.GetValue("y"));

                //USER
                camFix = bool.Parse(internalData.GetValue("camFix"));
                fpsKeeperActive = bool.Parse(internalData.GetValue("fpsKeeperActive"));
                fpsMinSlider = int.Parse(internalData.GetValue("fpsMinSlider"));

                showFPS = bool.Parse(internalData.GetValue("showFPS"));
                ConfigNode fpsPositionNode = internalData.GetNode("fpsPosition");
                fpsPosition.xMin = float.Parse(fpsPositionNode.GetValue("x"));
                fpsPosition.yMin = float.Parse(fpsPositionNode.GetValue("y"));

                //KEYBINDS
                ConfigNode keyBindsNode = internalData.GetNode("keyBinds");
                for (int i = 0; i < keyBinds.Length; i++)
                {
                    keyBinds[i].primary = (KeyCode)Enum.Parse(typeof(KeyCode), keyBindsNode.GetValue("keyBind" + i));
                }
                customKeySlider = float.Parse(internalData.GetValue("customKeySlider"));

                try
                {
                    //RAILS DATA NODE
                    ConfigNode railsData = config.GetNode("railsData");

                    //WARP RATES
                    warpLevels = (int)Mathf.Clamp(int.Parse(railsData.GetValue("warpLevels")), 8, Mathf.Infinity);
                    ConfigNode customWarpRatesNode = railsData.GetNode("customWarpRates");
                    for (int i = 0; i < warpLevels; i++)
                    {
                        customWarpRates[i] = customWarpRatesNode.GetValue("customWarpRate" + i);
                    }

                    //ALTITUDE LIMITS
                    ConfigNode customAltitudeLimitsNode = railsData.GetNode("customAltitudeLimits");
                    for (int i = 0; i < customAltitudeLimits.Count; i++)
                    {
                        ConfigNode celestialLimitsNode = customAltitudeLimitsNode.GetNode("celestial" + i);
                        for (int j = 0; j < warpLevels; j++)
                        {
                            customAltitudeLimits[i][j] = celestialLimitsNode.GetValue("customAltitudeLimit" + j);
                        }
                    }
                }
                catch
                {
                    print("Time Control: rails data load failed, rebuilding (planetary system change?)");
                    buildConfig(true);
                }
            }
            catch (Exception e)
            {
                if (e is ArgumentNullException || e is NullReferenceException)
                {
                    print("Time Control: config load failed, rebuilding");
                    buildConfig(false);
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
            //INTERNAL DATA NODE
            ConfigNode internalData = config.GetNode("internalData");

            //INTERNAL
            internalData.SetValue("visible", visible.ToString());
            internalData.SetValue("minimized", minimized.ToString());
            internalData.SetValue("mode", mode.ToString());

            //WINDOW POSITIONS
            ConfigNode flightWindowPositionNode = internalData.GetNode("flightWindowPosition");
            flightWindowPositionNode.SetValue("x", flightWindowPosition.xMin.ToString());
            flightWindowPositionNode.SetValue("y", flightWindowPosition.yMin.ToString());

            ConfigNode menuWindowPositionNode = internalData.GetNode("menuWindowPosition");
            menuWindowPositionNode.SetValue("x", menuWindowPosition.xMin.ToString());
            menuWindowPositionNode.SetValue("y", menuWindowPosition.yMin.ToString());

            ConfigNode settingsWindowPositionNode = internalData.GetNode("settingsWindowPosition");
            settingsWindowPositionNode.SetValue("x", settingsWindowPosition.xMin.ToString());
            settingsWindowPositionNode.SetValue("y", settingsWindowPosition.yMin.ToString());

            //USER
            internalData.SetValue("camFix", camFix.ToString());
            internalData.SetValue("fpsKeeperActive", fpsKeeperActive.ToString());
            internalData.SetValue("fpsMinSlider", fpsMinSlider.ToString());

            internalData.SetValue("showFPS", showFPS.ToString());
            ConfigNode fpsPositionNode = internalData.GetNode("fpsPosition");
            fpsPositionNode.SetValue("x", fpsPosition.xMin.ToString());
            fpsPositionNode.SetValue("y", fpsPosition.yMin.ToString());

            //KEYBINDS
            ConfigNode keyBindsNode = internalData.GetNode("keyBinds");
            for (int i = 0; i < keyBinds.Length; i++)
            {
                keyBindsNode.SetValue("keyBind" + i, keyBinds[i].primary.ToString());
            }
            internalData.SetValue("customKeySlider", customKeySlider.ToString());

            //RAILS DATA NODE
            ConfigNode railsData = config.GetNode("railsData");

            //WARP RATES
            railsData.SetValue("warpLevels", warpLevels.ToString());
            ConfigNode customWarpRatesNode = railsData.GetNode("customWarpRates");
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
            ConfigNode customAltitudeLimitsNode = railsData.GetNode("customAltitudeLimits");
            for (int i = 0; i < customAltitudeLimits.Count; i++)
            {
                ConfigNode celestial = customAltitudeLimitsNode.GetNode("celestial" + i);
                for (int j = 0; j < warpLevels; j++)
                {
                    if (celestial.HasValue("customAltitudeLimit" + j))
                    {
                        celestial.SetValue("customAltitudeLimit" + j, customAltitudeLimits[i][j]);
                    }
                    else
                    {
                        celestial.AddValue("customAltitudeLimit" + j, customAltitudeLimits[i][j]);
                    }
                }
            }

            config.Save(path);
        }

    }
}

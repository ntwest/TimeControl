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
    public class TimeControl : MonoBehaviour
    {
        public static Boolean updateAvailable = false;
        public static string updateNumber = "?";
        private Boolean operational;
        ScreenMessage msg;
        ScreenMessages warpText;
        Color warpTextColor;
        private IButton toolbarButton;
        private static FlightCamera cam;
        private static TimeWarp timeWarp;
        private Boolean supressFlightResultsDialog = false;
        private FlightResultsDialog fld;
        private Boolean fpsVisible = true;

        //GUI
        private static Rect minimizeButton = new Rect(5, 5, 10, 10);
        private static Rect settingsButton = new Rect(182, -1, 20, 20);
        private static Rect mode0Button = new Rect(10, -1, 25, 20);
        private static Rect mode1Button = new Rect(25, -1, 25, 20);
        private static Rect mode2Button = new Rect(40, -1, 25, 20);
        private Rect debugWindowPosition = new Rect();
        private Boolean showDebugGUI = false;
        private Boolean settingsOpen = false;
        private string setUT = "0";

        //PHYSICS
        private float defaultDeltaTime = Time.fixedDeltaTime; //0.02
        private float timeSlider = 0f;
        private Boolean timePaused;
        private Boolean pauseOnNextFixedUpdate = false;
        private float smoothSlider = 0f;
        private float inverseTimeScale = 0f;
        private float truePos = 1f;
        private Boolean deltaLocked = false;
        private int fpsMin = 5;
        private int fpsKeeperFactor = 0;
        private float throttleSlider = 0f;
        private Boolean throttleToggle;
        private int currentSOI;

        //KEYS
        private Boolean[] keySet = new Boolean[6];
        private String[] keyLabels = { "Speed Up: ", "Slow Down: ", "Realtime: ", "1/64x: ", "Pause: ", "Step: " };

        //HYPERWARP
        private Boolean hyperWarping = false;
        private float hyperMinPhys = 1f;
        private Boolean hyperPauseOnTimeReached = false;
        private string hyperWarpHours = "0";
        private string hyperWarpMinutes = "0";
        private string hyperWarpSeconds = "0";
        private double hyperWarpTime = Mathf.Infinity;
        private float hyperMaxRate = 2f;
        
        //WARP
        private Boolean autoWarping = false;
        private string warpYears = "0";
        private string warpDays = "0";
        private string warpHours = "0";
        private string warpMinutes = "0";
        private string warpSeconds = "0";
        private double warpTime = Mathf.NegativeInfinity;
        //INIT FUNCTIONS
        internal TimeControl()
        {
            if (ToolbarManager.ToolbarAvailable)
            {
                toolbarButton = ToolbarManager.Instance.add("TimeControl", "button");
                toolbarButton.TexturePath = "TimeControl/inactive";
                toolbarButton.ToolTip = "Time Control";
                toolbarButton.Visibility = new GameScenesVisibility(GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER); //Places where the button should show up
                toolbarButton.OnClick += (e) =>
                {
                    Settings.visible = !Settings.visible;
                    toolbarButton.TexturePath = Settings.visible ? "TimeControl/active" : "TimeControl/inactive";
                };
            }
            else
            {
                Settings.visible = true;
            }
        }
        private void Start()
        {
            FlightCamera[] cams = FlightCamera.FindObjectsOfType(typeof(FlightCamera)) as FlightCamera[];
            cam = cams[0];

            warpText = FindObjectOfType<ScreenMessages>();
            warpTextColor = warpText.textStyles[1].normal.textColor;
        }
        private void Awake()
        {
            UnityEngine.Object.DontDestroyOnLoad(this); //Don't go away on scene changes

            //Check for updates
            KSVersionCheck.VersionCheck.CheckVersion(21, latest =>
            {
                if (latest.friendly_version != Assembly.GetExecutingAssembly().GetName().Version.ToString(2))
                {
                    TimeControl.updateAvailable = true;
                    TimeControl.updateNumber = latest.friendly_version;
                }
            });


            GameEvents.onFlightReady.Add(this.onFlightReady);
            GameEvents.onGameSceneLoadRequested.Add(this.onGameSceneLoadRequested);
            GameEvents.onGamePause.Add(this.onGamePause);
            GameEvents.onGameUnpause.Add(this.onGameUnpause);
            GameEvents.onHideUI.Add(this.onHideUI);
            GameEvents.onShowUI.Add(this.onShowUI);
            GameEvents.onLevelWasLoaded.Add(this.onLevelWasLoaded);
            GameEvents.onTimeWarpRateChanged.Add(this.onTimeWarpRateChanged);
            GameEvents.onPartDestroyed.Add(this.onPartDestroyed);
        }

        //EVENT MANAGERS
        private void onFlightReady()
        {  
            operational = true;
            timeSlider = 0f;
        }
        private void onGameSceneLoadRequested(GameScenes gs)
        {
            operational = false;
            hyperWarping = false;
            autoWarping = false;
            Settings.visible = false;
        }
        private void onGamePause()
        {
            operational = false;
        }
        private void onGameUnpause()
        {
            operational = true;
        }
        private void onHideUI()
        {
            Settings.visible = false;
            fpsVisible = false;
        }
        private void onShowUI()
        {
            Settings.visible = true;
            fpsVisible = true;
        }
        private void onLevelWasLoaded(GameScenes gs)
        {
            Settings.visible = true;

            if (ToolbarManager.ToolbarAvailable)
            {
                toolbarButton.TexturePath = Settings.visible ? "TimeControl/active" : "TimeControl/inactive";
            }
        }
        private void onPartDestroyed(Part p)//TODO make hyperwarp cancel more reliable (or work at all)
        {
            try
            {
                if (p.vessel == FlightGlobals.ActiveVessel)
                {
                    exitHyper();
                }
            }
            catch { }
        }
        private void onTimeWarpRateChanged()
        {
            if (TimeWarp.CurrentRateIndex > 0)
            {
                operational = false;
            }
            else
            {
                operational = true;
            }
        }

        //UPDATE FUNCTIONS
        private void Update()
        {
            sizeWindows();

            if (timeWarp != null)
            {
                if (HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    UpdateMenu();
                }

                if (HighLogic.LoadedSceneIsFlight)
                {
                    UpdateFlight();
                }
            }
            else if (HighLogic.LoadedScene != GameScenes.MAINMENU)
            {
                timeWarp = TimeWarp.fetch;
            }
            if (timeWarp == null)
                return;

            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.T))
            {
                Settings.visible = !Settings.visible;
                toolbarButton.TexturePath = Settings.visible ? "TimeControl/active" : "TimeControl/inactive";
            }

            //if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKey(KeyCode.T)) //Ctrl-Alt-T opens debug
            //{
            //    showDebugGUI = true;
            //}

            if (HighLogic.CurrentGame != null)
            {
                if (hyperWarping || autoWarping)
                {
                    HighLogic.CurrentGame.Parameters.Flight.CanTimeWarpHigh = false;
                    HighLogic.CurrentGame.Parameters.Flight.CanTimeWarpLow = false;
                }
                else
                {
                    HighLogic.CurrentGame.Parameters.Flight.CanTimeWarpHigh = true;
                    HighLogic.CurrentGame.Parameters.Flight.CanTimeWarpLow = true;
                }
            }

            if ((Planetarium.GetUniversalTime() > warpTime || Mathf.Abs((float)Planetarium.GetUniversalTime() - (float)warpTime) < 60) && autoWarping)
            {
                autoWarping = false;
            }
        }
        private void FixedUpdate()
        {
            if (Planetarium.GetUniversalTime() >= hyperWarpTime)
            {
                exitHyper();
                if (hyperPauseOnTimeReached)
                {
                    pauseOnNextFixedUpdate = true;
                }
            }
            if (pauseOnNextFixedUpdate)
            {
                timePaused = true;
                pauseOnNextFixedUpdate = false;
            }
        }
        private void UpdateMenu()
        {
            for (int i = 0; i < 8; i++)
            {
                timeWarp.warpRates[i] = parseSTOI(Settings.customWarpRates[i]);
            }
        }
        private void UpdateFlight()
        {
            if (fld == null)
            {
                fld = FindObjectOfType<FlightResultsDialog>();
            }
            if (supressFlightResultsDialog)
            {
                fld.enabled = false;
            }
            else
            {
                fld.enabled = true;
            }

            currentSOI = getPlanetaryID(FlightGlobals.ActiveVessel.mainBody.name);
            for (int i = 0; i < 8; i++)
            {
                timeWarp.warpRates[i] = parseSTOI(Settings.customWarpRates[i]);
                FlightGlobals.ActiveVessel.mainBody.timeWarpAltitudeLimits[i] = parseSTOI(Settings.customAltitudeLimits[currentSOI, i]);
            }

            warpText.textStyles[1].normal.textColor = warpTextColor; //ensures the warp text color goes back to default

            Time.maximumDeltaTime = Mathf.Round(Settings.maxDeltaTimeSlider * 100f) / 100f;

            if (Settings.camFix)
            {
                cam.SetDistanceImmediate(cam.Distance);
            }

            if (throttleToggle)
            {
                FlightInputHandler.state.mainThrottle = throttleSlider;
            }

            if (operational)
            {
                keyManager(); //handles keybindings

                if (!hyperWarping)
                {
                    truePos = convertToExponential(timeSlider);
                    if (!timePaused)
                    {
                        fpsKeeper();

                        smoothSlider = linearInterpolate(smoothSlider, timeSlider, .01f);
                        inverseTimeScale = convertToExponential(smoothSlider);
                        Time.timeScale = 1f / inverseTimeScale;
                        Time.fixedDeltaTime = deltaLocked ? defaultDeltaTime : defaultDeltaTime * (1f / inverseTimeScale);
                        //Time.maximumDeltaTime = (Mathf.Round(Settings.maxDeltaTimeSlider * 100f) / 100f)/inverseTimeScale;
                    }
                    else
                    {
                        Time.timeScale = 0f;
                    }
                }
                else if (hyperWarping) //hyperwarp
                {
                    Time.timeScale = Mathf.Round(hyperMaxRate);
                    Time.fixedDeltaTime = defaultDeltaTime * hyperMinPhys;
                    warpText.textStyles[1].normal.textColor = Color.red;
                    warpMessage("Hyper Warp: " + Math.Round(PerformanceManager.ptr, 1) + "x");
                }
                Planetarium.fetch.fixedDeltaTime = Time.fixedDeltaTime;
            }
            else
            {
                smoothSlider = timeSlider;
            }
        }

        //GUI FUNCTIONS
        private void OnGUI()
        {
            GUI.skin = HighLogic.Skin;
            if (Settings.showFPS && fpsVisible && HighLogic.LoadedSceneIsFlight)
            {
                Settings.fpsPosition.x = parseSTOI(Settings.fpsX);
                Settings.fpsPosition.y = parseSTOI(Settings.fpsY);
                GUI.Label(Settings.fpsPosition, Mathf.Floor(PerformanceManager.fps).ToString());
            }

            GUI.skin = null;

            if (showDebugGUI)
            {
                debugWindowPosition = constrain(GUILayout.Window(9001, debugWindowPosition, onDebugGUI, "Time Control: Debug"));
            }

            if (Settings.visible)
            {
                if (HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    Settings.menuWindowPosition = constrain(GUILayout.Window(12, Settings.menuWindowPosition, onMenuGUI, "Time Control"));
                }

                if (HighLogic.LoadedSceneIsFlight)
                {
                    Settings.flightWindowPosition = constrain(GUILayout.Window(10, Settings.flightWindowPosition, onFlightGUI, "Time Control"));

                    if (settingsOpen && !Settings.minimized)
                    {
                        Settings.settingsWindowPosition = constrain(GUILayout.Window(11, Settings.settingsWindowPosition, onSettingsGUI, "Time Control Settings"));
                    }
                }
            }

            GUI.skin = HighLogic.Skin;
        } 
        private void onMenuGUI(int windowId)
        {
            //Minimize button
            if (GUI.Button(minimizeButton, ""))
            {
                Settings.minimized = !Settings.minimized;
            }

            if (!Settings.minimized)
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.BeginVertical();
                        {
                            GUI.enabled = false;
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("Warp Rate " + (1).ToString() + ":");
                                Settings.customWarpRates[0] = GUILayout.TextField(Settings.customWarpRates[0], 20, GUILayout.Width(100));
                            }
                            GUILayout.EndHorizontal();

                            GUI.enabled = true;
                            for (int i = 1; i < 8; i++)
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label("Warp Rate " + (i + 1).ToString() + ":");
                                    Settings.customWarpRates[i] = GUILayout.TextField(Settings.customWarpRates[i], 20, GUILayout.Width(100));
                                }
                                GUILayout.EndHorizontal();
                            }

                            if (GUILayout.Button("Set to default values"))
                            {
                                for (int i = 0; i < 8; i++)
                                {
                                    Settings.customWarpRates[i] = Settings.standardWarpRates[i];
                                }
                            }
                        }
                        GUILayout.EndVertical();

                        GUILayout.BeginVertical();
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.FlexibleSpace();
                                GameSettings.KERBIN_TIME = GUILayout.Toggle(GameSettings.KERBIN_TIME, "Use Kerbin Time");
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.Label("");

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.FlexibleSpace();
                                GUILayout.Label("Total Time Passed:");
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.FlexibleSpace();
                                if (GameSettings.KERBIN_TIME)
                                {
                                    GUILayout.Label(Mathf.Round((float)Planetarium.GetUniversalTime() / 9201600) + " years");
                                }
                                else
                                {
                                    GUILayout.Label(Mathf.Round((float)Planetarium.GetUniversalTime() / 31536000) + " years");
                                }
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.FlexibleSpace();
                                if (GameSettings.KERBIN_TIME)
                                {
                                    GUILayout.Label(Mathf.Round((float)Planetarium.GetUniversalTime() / 21600) + " days");
                                }
                                else
                                {
                                    GUILayout.Label(Mathf.Round((float)Planetarium.GetUniversalTime() / 86400) + " days");
                                }
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.FlexibleSpace();
                                if (GameSettings.KERBIN_TIME)
                                {
                                    GUILayout.Label(Mathf.Round((float)Planetarium.GetUniversalTime() / 3600) + " hours");
                                }
                                else
                                {
                                    GUILayout.Label(Mathf.Round((float)Planetarium.GetUniversalTime() / 3600) + " hours");
                                }
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.FlexibleSpace();
                                if (GameSettings.KERBIN_TIME)
                                {
                                    GUILayout.Label(Mathf.Round((float)Planetarium.GetUniversalTime() / 60) + " minutes");
                                }
                                else
                                {
                                    GUILayout.Label(Mathf.Round((float)Planetarium.GetUniversalTime() / 60) + " minutes");
                                }
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.FlexibleSpace();
                                if (GameSettings.KERBIN_TIME)
                                {
                                    GUILayout.Label(Mathf.Round((float)Planetarium.GetUniversalTime()) + " seconds");
                                }
                                else
                                {
                                    GUILayout.Label(Mathf.Round((float)Planetarium.GetUniversalTime()) + " seconds");
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label("", GUILayout.Height(5));

                    onWarpTo();
                }
                GUILayout.EndVertical();
            }
            if (Event.current.button > 0 && Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) //Ignore right & middle clicks
                Event.current.Use();
            GUI.DragWindow();
        }
        private void onFlightGUI(int windowId)
        {
            GUI.enabled = true;

            //Minimize button
            if (GUI.Button(minimizeButton, ""))
            {
                Settings.minimized = !Settings.minimized;
            }

            if (!Settings.minimized)
            {
                modeButtons();

                switch (Settings.mode)
                {
                    case 0:
                        modeSlowmo();
                        break;
                    case 1:
                        modeHyper();
                        break;
                    case 2:
                        modeRails();
                        break;
                }
            }

            GUI.enabled = true;
            if (Event.current.button > 0 && Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) //Ignore right & middle clicks
                Event.current.Use();
            GUI.DragWindow();
        }
            private void modeButtons()
            {
                Color bc = GUI.backgroundColor;
                Color cc = GUI.contentColor;
                GUI.backgroundColor = Color.clear;
                //Settings button
                if (!settingsOpen)
                {
                        GUI.contentColor = new Color(0.5f, 0.5f, 0.5f);
                }
                if (GUI.Button(settingsButton, "?"))
                {
                    settingsOpen = !settingsOpen;
                }
                GUI.contentColor = cc;
                //Slow-mo mode
                if (Settings.mode != 0)
                {
                    GUI.contentColor = new Color(0.5f, 0.5f, 0.5f);
                }
                if (GUI.Button(mode0Button, "S"))
                {
                    Settings.mode = 0;
                }
                GUI.contentColor = cc;
                //Hyper mode
                if (Settings.mode != 1)
                {
                    GUI.contentColor = new Color(0.5f, 0.5f, 0.5f);
                }
                if (GUI.Button(mode1Button, "H"))
                {
                    Settings.mode = 1;
                }
                GUI.contentColor = cc;
                //Rails mode
                if (Settings.mode != 2)
                {
                    GUI.contentColor = new Color(0.5f, 0.5f, 0.5f);
                }
                if (GUI.Button(mode2Button, "R"))
                {
                    Settings.mode = 2;
                }
                GUI.contentColor = cc;
                GUI.backgroundColor = bc;
            }
            private void modeSlowmo()
            {
                GUI.enabled = (operational && !Settings.fpsKeeperActive);

                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        if (truePos != 1)
                        {
                            GUILayout.Label("Time Scale: 1/" + truePos.ToString() + "x");
                        }
                        else
                        {
                            GUILayout.Label("Time Scale: " + truePos.ToString() + "x");
                        }
                        GUI.enabled = operational;
                        if (!timePaused)
                        {
                            if (GUILayout.Button("Pause", GUILayout.Width(60)))
                            {
                                timePaused = true;
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Resume", GUILayout.Width(60)))
                            {
                                timePaused = false;
                            }
                        }
                        GUI.enabled = timePaused;
                        if (GUILayout.Button(">", GUILayout.Width(20)))
                        {
                            timePaused = false;
                            pauseOnNextFixedUpdate = true;
                        }
                        GUI.enabled = operational;
                    }
                    GUILayout.EndHorizontal();

                    GUI.enabled = (operational && !Settings.fpsKeeperActive);
                    timeSlider = GUILayout.HorizontalSlider(timeSlider, 0f, 1f);
                    deltaLocked = Settings.fpsKeeperActive ? GUILayout.Toggle(Settings.fpsKeeperActive, "Lock physics delta to default") : GUILayout.Toggle(deltaLocked, "Lock physics delta to default");

                    GUILayout.Label("", GUILayout.Height(5));

                    throttleToggle = GUILayout.Toggle(throttleToggle, "Throttle Control: " + Mathf.Round(throttleSlider * 100) + "%");
                    throttleSlider = GUILayout.HorizontalSlider(throttleSlider, 0.0f, 1.0f);
                }
                GUILayout.EndVertical();
            }
            private void modeHyper()
            {
                GUI.enabled = ((inverseTimeScale == 1) && operational && !Settings.fpsKeeperActive);
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Time Rate: " + (PerformanceManager.ptr / 1 * 100).ToString("0") + "%");
                        GUILayout.FlexibleSpace();
                        if (hyperWarping)
                        {
                            GUILayout.Label("HYPER");
                        }
                        else if (TimeWarp.WarpMode == TimeWarp.Modes.HIGH && TimeWarp.CurrentRateIndex > 0)
                        {
                            GUILayout.Label("RAILS");
                        }
                        else if (TimeWarp.WarpMode == TimeWarp.Modes.LOW && TimeWarp.CurrentRateIndex > 0)
                        {
                            GUILayout.Label("PHYS");
                        }
                        else if (timePaused || FlightDriver.Pause)
                        {
                            GUILayout.Label("PAUSED");
                        }
                        else if (Time.timeScale < 1)
                        {
                            GUILayout.Label("SLOWMO");
                        }
                        else
                        {
                            GUILayout.Label("NORM");
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label("Max Warp Rate: " + Mathf.Round(hyperMaxRate));
                    hyperMaxRate = GUILayout.HorizontalSlider(hyperMaxRate, 2f, 100f);

                    GUILayout.Label("Min Physics Accuracy: " + 1 / hyperMinPhys);
                    hyperMinPhys = GUILayout.HorizontalSlider(hyperMinPhys, 1f, 4f);

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Warp"))
                        {
                            hyperWarping = true;
                        }

                        if (GUILayout.Button("Resume"))
                        {
                            timePaused = false;
                            exitHyper();
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label("", GUILayout.Height(5));

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Timed Warp:");
                        hyperWarpHours = GUILayout.TextField(hyperWarpHours, GUILayout.Width(35));
                        GUILayout.Label("h ");
                        hyperWarpMinutes = GUILayout.TextField(hyperWarpMinutes, GUILayout.Width(35));
                        GUILayout.Label("m ");
                        hyperWarpSeconds = GUILayout.TextField(hyperWarpSeconds, GUILayout.Width(35));
                        GUILayout.Label("s");
                    }
                    GUILayout.EndHorizontal();

                    hyperPauseOnTimeReached = GUILayout.Toggle(hyperPauseOnTimeReached, "Pause on time reached");

                    if (GUILayout.Button("Timed Warp"))
                    {
                        hyperWarping = true;
                        int hours = parseSTOI(hyperWarpHours);
                        hyperWarpHours = "0";
                        int minutes = parseSTOI(hyperWarpMinutes);
                        hyperWarpMinutes = "0";
                        int seconds = parseSTOI(hyperWarpSeconds);
                        hyperWarpSeconds = "0";

                        hyperWarpTime = hours * 3600 +
                                        minutes * 60 +
                                        seconds +
                                        Planetarium.GetUniversalTime();
                    }

                    GUILayout.Label("", GUILayout.Height(5));

                    throttleToggle = GUILayout.Toggle(throttleToggle, "Throttle Control: " + Mathf.Round(throttleSlider * 100) + "%");
                    throttleSlider = GUILayout.HorizontalSlider(throttleSlider, 0.0f, 1.0f);
                }
                GUILayout.EndVertical();
            }
            private void modeRails()
            {
                GUI.enabled = true;

                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Current SOI: " + FlightGlobals.ActiveVessel.mainBody.name);
                        GUILayout.FlexibleSpace();
                        GameSettings.KERBIN_TIME = GUILayout.Toggle(GameSettings.KERBIN_TIME, "Use Kerbin Time");
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.BeginVertical();
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUI.enabled = false;
                                GUILayout.Label("Warp Rate " + (0 + 1).ToString() + ":");
                                Settings.customWarpRates[0] = GUILayout.TextField(Settings.customWarpRates[0], 20, GUILayout.Width(100));
                                GUI.enabled = true;
                            }
                            GUILayout.EndHorizontal();

                            for (int i = 1; i < 8; i++)
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label("Warp Rate " + (i + 1).ToString() + ":");
                                    Settings.customWarpRates[i] = GUILayout.TextField(Settings.customWarpRates[i], 20, GUILayout.Width(100));
                                }
                                GUILayout.EndHorizontal();
                            }

                            if (GUILayout.Button("Reset warp rates"))
                            {
                                for (int i = 0; i < 8; i++)
                                {
                                    Settings.customWarpRates[i] = Settings.standardWarpRates[i];
                                }
                            }
                        }
                        GUILayout.EndVertical();

                        GUILayout.BeginVertical();
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUI.enabled = false;
                                GUILayout.Label("Altitude Limit " + (0 + 1).ToString() + ":");
                                Settings.customAltitudeLimits[currentSOI, 0] = GUILayout.TextField(Settings.customAltitudeLimits[currentSOI, 0], GUILayout.Width(100));
                                GUI.enabled = true;
                            }
                            GUILayout.EndHorizontal();

                            for (int i = 1; i < 8; i++)
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label("Altitude Limit " + (i + 1).ToString() + ":");
                                    Settings.customAltitudeLimits[currentSOI, i] = GUILayout.TextField(Settings.customAltitudeLimits[currentSOI, i], GUILayout.Width(100));
                                }
                                GUILayout.EndHorizontal();
                            }

                            if (GUILayout.Button("Reset body altitude limits"))
                            {
                                for (int i = 0; i < 8; i++)
                                {
                                    Settings.customAltitudeLimits[currentSOI, i] = Settings.standardAltitudeLimits[currentSOI, i];
                                }
                            }
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label("", GUILayout.Height(5));

                    onWarpTo();
                }
                GUILayout.EndVertical();
            }
        private void onSettingsGUI(int windowId)
        {
            //close button
            if (GUI.Button(minimizeButton, ""))
            {
                settingsOpen = !settingsOpen;
            }

            GUILayout.BeginVertical();
            {
                GUILayout.Label("Physics Time Ratio: " + PerformanceManager.ptr.ToString("0.0000"));
                GUILayout.Label("UT: " + Planetarium.GetUniversalTime());
                GUILayout.Label("Time Scale: " + Time.timeScale);
                GUILayout.Label("Physics Delta: " + Time.fixedDeltaTime);
                GUILayout.Label("Max Delta Time: " + Time.maximumDeltaTime);
                GUI.enabled = !Settings.fpsKeeperActive;
                Settings.maxDeltaTimeSlider = GUILayout.HorizontalSlider(Settings.maxDeltaTimeSlider, 0.12f, 0.02f);
                GUI.enabled = true;

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("FPS: " + Mathf.Floor(PerformanceManager.fps), GUILayout.Width(50));
                    Settings.showFPS = GUILayout.Toggle(Settings.showFPS, "Show");
                    Settings.fpsX = GUILayout.TextField(Settings.fpsX, 5, GUILayout.Width(30));
                    Settings.fpsY = GUILayout.TextField(Settings.fpsY, 5, GUILayout.Width(30));
                }
                GUILayout.EndHorizontal();

                GUILayout.Label("PPS: " + PerformanceManager.pps);
                Settings.camFix = GUILayout.Toggle(Settings.camFix, "Camera Zoom Fix");
                supressFlightResultsDialog = GUILayout.Toggle(supressFlightResultsDialog, "Supress Results Dialog");

                GUILayout.Label("", GUILayout.Height(5));

                Settings.fpsKeeperActive = GUILayout.Toggle(Settings.fpsKeeperActive, "FPS Keeper: " + Mathf.Round(Settings.fpsMinSlider / 5) * 5 + " fps");
                Settings.fpsMinSlider = (int)GUILayout.HorizontalSlider(Settings.fpsMinSlider, 5, 60);

                GUILayout.Label("", GUILayout.Height(5));

                GUILayout.Label("Key Bindings:");

                //Keys
                Color c = GUI.contentColor;
                for (int i = 0; i < 6; i++)
                {
                    if (keySet[i])
                    {
                        GUI.contentColor = Color.yellow;
                    }
                    else
                    {
                        GUI.contentColor = c;
                    }
                    if (GUILayout.Button(keyLabels[i] + Settings.keyBinds[i].primary.ToString()))
                    {
                        if (keySet[i])
                        {
                            keySet[i] = false;
                            Settings.keyBinds[i] = new KeyBinding(KeyCode.None);
                        }
                        else if (!arrayTrue(keySet))
                        {
                            keySet[i] = true;
                        }
                    }
                }
                GUI.contentColor = c;

                if (updateAvailable)
                {
                    GUILayout.Label("", GUILayout.Height(5));

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Update Available: ");
                        GUILayout.Label(Assembly.GetExecutingAssembly().GetName().Version.ToString(2) + " -> " + updateNumber);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Forum"))
                        {
                            Application.OpenURL("http://forum.kerbalspaceprogram.com/threads/69363");
                        }
                        if (GUILayout.Button("KerbalStuff"))
                        {
                            Application.OpenURL("http://beta.kerbalstuff.com/mod/21/Time_Control");
                        }
                        if (GUILayout.Button("Curse"))
                        {
                            Application.OpenURL("http://kerbal.curseforge.com/ksp-mods/220204-time-control");
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            if (Event.current.button > 0 && Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) //Ignore right & middle clicks
                Event.current.Use();
            GUI.DragWindow();
        }
        private void onDebugGUI(int windowID)
        {
            GUI.enabled = true;

            if (GUI.Button(minimizeButton, ""))
            {
                showDebugGUI = false;
            }

            GUILayout.BeginVertical();
            {
                GUILayout.Label("Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString(2));
                GUILayout.Label("Update Available: " + updateAvailable);
                GUILayout.Label("Minimized: " + Settings.minimized);
                GUILayout.Label("Visible (toolbar): " + Settings.visible);
                GUILayout.Label("Operational: " + operational);
                GUILayout.Label("TimeWarp: " + timeWarp);
                GUILayout.Label("Flight Window: " + Settings.flightWindowPosition);
                GUILayout.Label("Menu Window: " + Settings.menuWindowPosition);
                GUILayout.Label("Settings Window: " + Settings.settingsWindowPosition);
                GUILayout.Label("Debug Window: " + debugWindowPosition);
                GUILayout.Label("Loaded Scene: " + HighLogic.LoadedScene);
                GUILayout.Label("Time Slider: " + timeSlider);
                GUILayout.Label("Smooth Slider: " + smoothSlider);
                GUILayout.Label("Paused: " + timePaused);
                GUILayout.Label("HyperWarping: " + hyperWarping);
                GUILayout.Label("AutoWarping: " + autoWarping);
                GUILayout.Label("Warp Text: " + warpText);
                GUILayout.Label("FPS Keeper Factor: " + fpsKeeperFactor);
                GUILayout.Label("UT: " + Planetarium.GetUniversalTime());

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("UT:");
                    setUT = GUILayout.TextField(setUT, GUILayout.Width(160));
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Set"))
                {
                    Planetarium.SetUniversalTime(parseSTOI(setUT));
                }
            }
            GUILayout.EndVertical();

            if (Event.current.button > 0 && Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) //Ignore right & middle clicks
                Event.current.Use();
            GUI.DragWindow();
        }
        private void onWarpTo()
        {
            GUI.enabled = (TimeWarp.CurrentRateIndex == 0);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Time Warp:");

                warpYears = GUILayout.TextField(warpYears, GUILayout.Width(35));
                GUILayout.Label("y ");
                warpDays = GUILayout.TextField(warpDays, GUILayout.Width(35));
                GUILayout.Label("d ");
                warpHours = GUILayout.TextField(warpHours, GUILayout.Width(35));
                GUILayout.Label("h ");
                warpMinutes = GUILayout.TextField(warpMinutes, GUILayout.Width(35));
                GUILayout.Label("m ");
                warpSeconds = GUILayout.TextField(warpSeconds, GUILayout.Width(35));
                GUILayout.Label("s");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Auto Warp"))
                {
                    int years = parseSTOI(warpYears);
                    warpYears = "0";
                    int days = parseSTOI(warpDays);
                    warpDays = "0";
                    int hours = parseSTOI(warpHours);
                    warpHours = "0";
                    int minutes = parseSTOI(warpMinutes);
                    warpMinutes = "0";
                    int seconds = parseSTOI(warpSeconds);
                    warpSeconds = "0";

                    if (GameSettings.KERBIN_TIME)
                    {
                        warpTime = years * 9201600 + days * 21600 + hours * 3600 + minutes * 60 + seconds + Planetarium.GetUniversalTime();
                    }
                    else
                    {
                        warpTime = years * 31536000 + days * 86400 + hours * 3600 + minutes * 60 + seconds + Planetarium.GetUniversalTime();
                    }
                    if (warpTime > Planetarium.GetUniversalTime() && (warpTime - Planetarium.GetUniversalTime()) > parseSTOI(Settings.customWarpRates[1]))
                    {
                        autoWarping = true;
                        timeWarp.WarpTo(warpTime);
                    }
                }
                GUI.enabled = true;
                if (GUILayout.Button("Cancel Warp"))
                {
                    autoWarping = false;
                    TimeWarp.fetch.SendMessage("StopAllCoroutines");
                    TimeWarp.SetRate(0, false);
                }
            }
            GUILayout.EndHorizontal();
        }

        //HELPER FUNCTIONS
        private Rect constrain(Rect r)
        {
            r.x = Mathf.Clamp(r.x, 0, Screen.width - r.width);
            r.y = Mathf.Clamp(r.y, 0, Screen.height - r.height);
            return r;
        }
        private void exitHyper()
        {
            hyperWarping = false;
            hyperWarpTime = Mathf.Infinity;
            ScreenMessages.RemoveMessage(this.msg);
        }
        private void fpsKeeper()
        {
            //FPS KEEPER
            fpsMin = (int)Mathf.Round(Settings.fpsMinSlider / 5) * 5;
            if (Settings.fpsKeeperActive)
            {
                if (Mathf.Abs(PerformanceManager.fps - fpsMin) > 2.5)
                {
                    if (PerformanceManager.fps < fpsMin)
                    {
                        fpsKeeperFactor += 1;
                    }
                    else
                    {
                        fpsKeeperFactor -= 1;
                    }
                }
                fpsKeeperFactor = Mathf.Clamp(fpsKeeperFactor, 0, 73); //0-10 are .01 steps down with max delta, 11-74 are steps of time scale to 1/64x

                if (fpsKeeperFactor < 11)
                {
                    timeSlider = 0f;
                    Settings.maxDeltaTimeSlider = .12f - (fpsKeeperFactor * .01f);
                }
                else
                {
                    Settings.maxDeltaTimeSlider = 0.02f;
                    timeSlider = (float)(fpsKeeperFactor - 10)/64f;
                }
            }
        }
        private void keyManager()
        {
            for (int i = 0; i < 6; i++)
            {
                if (keySet[i])
                {
                    var e = Event.current;
                    if (e.isKey && e.keyCode != KeyCode.None)
                    {
                        Settings.keyBinds[i] = new KeyBinding(e.keyCode);
                        keySet[i] = false;
                    }
                }
            }

            if (Settings.keyBinds[0].GetKey())
            {
                timeSlider -= 0.01f;
            }
            if (Settings.keyBinds[1].GetKey())
            {
                timeSlider += 0.01f;
            }
            if (Settings.keyBinds[2].GetKey())
            {
                timeSlider = 0;
            }
            if (Settings.keyBinds[3].GetKey())
            {
                timeSlider = 1;
            }
            if (Settings.keyBinds[4].GetKeyDown())
            {
                timePaused = !timePaused;
            }
            if (Settings.keyBinds[5].GetKeyDown())
            {
                timePaused = false;
                pauseOnNextFixedUpdate = true;
            }

        }
        private void warpMessage(string text)
        {
            ScreenMessages.RemoveMessage(this.msg);
            this.msg = ScreenMessages.PostScreenMessage(text, 3f, ScreenMessageStyle.UPPER_CENTER);
        }
        private void sizeWindows()
        {
            Settings.menuWindowPosition.height = 0;
            Settings.menuWindowPosition.width = 400;

            Settings.settingsWindowPosition.height = 0;
            Settings.settingsWindowPosition.width = 220;

            switch (Settings.mode)
            {
                case 0:
                    Settings.flightWindowPosition.height = 0;
                    Settings.flightWindowPosition.width = 220;
                    break;
                case 1:
                    Settings.flightWindowPosition.height = 0;
                    Settings.flightWindowPosition.width = 250;
                    break;
                case 2:
                    Settings.flightWindowPosition.height = 0;
                    Settings.flightWindowPosition.width = 400;
                    break;
            }

            settingsButton.x = Settings.flightWindowPosition.xMax - Settings.flightWindowPosition.xMin - 20; //Move the ?
        }
        private Vector3d flipYZ(Vector3d a) //Flip YZ for for orbit editing stuff
        {
            double temp = a.y;
            a.y = a.z;
            a.z = temp;
            return a;
        }
		private int getPlanetaryID(string s) //ID from name
		{
			// 
			// Change by Nathaniel R. Lewis (aka Teknoman117) (linux.robotdude@gmail.com)
			//
			// This method previously hard coded the reference IDs for each planet.  PSystemManager.Instance.localBodies is
			// a list of the celestial bodies active in KSP.  The index of the body in this list *IS* the reference id.
			// Method modified to return the reference ID with a predicate search this list.  Enables automatic compatibility
			// with future planets added by Squad or by planet adding mods such as the upcoming Kopernicus mod.
			//
			return PSystemManager.Instance.localBodies.FindIndex (p => p.bodyName.Equals(s));  
		}
        private float convertToExponential(float a) //1-64 exponential curve
        {
            return Mathf.Clamp(Mathf.Floor(Mathf.Pow(64, a)), 1, 64);
        }
        private int parseSTOI(string a) //Parses a string to an int with standard limitations
        {
            int num;
            if (!Int32.TryParse(a, out num))
            {
                return 0;
            }
            else
            {
                return Int32.Parse(a,
                                      System.Globalization.NumberStyles.AllowExponent
                                    | System.Globalization.NumberStyles.AllowLeadingWhite
                                    | System.Globalization.NumberStyles.AllowTrailingWhite
                                    | System.Globalization.NumberStyles.AllowThousands
                                    );
            }
        }
        private float linearInterpolate(float current, float target, float amount)
        {
            if (current > target && Mathf.Abs(current - target) > amount)
            {
                return current - amount;
            }
            else if (current < target && Mathf.Abs(current - target) > amount)
            {
                return current + amount;
            }
            else
            {
                return target;
            }
        }
        private Boolean arrayTrue(Boolean[] b)//return true if any true
        {
            foreach (Boolean value in b)
            {
                if (value)
                {
                    return true;
                }
            }
            return false;
        }
        private Rect addRect(Rect r1, Rect r2)
        {
            return new Rect(r1.xMin + r2.xMin, r1.yMin + r2.yMin, r1.width, r1.height);
        }

    }
}
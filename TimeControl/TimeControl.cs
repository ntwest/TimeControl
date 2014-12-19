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
        private Boolean tempInvisible = false;

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
        private Vector2 warpScroll;
        private int currentSOI;
        private int selectedSOI = -1;
        private int SOI;
        private Boolean SOISelect = false;

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

        //KEYS
        private Boolean[] keySet = new Boolean[8];
        private String[] keyLabels = { "Speed Up: ", "Slow Down: ", "Realtime: ", "1/64x: ", "Custom-#x: ", "Pause: ", "Step: ", "Hyper Warp: "};

        //HYPERWARP
        private Boolean hyperWarping = false;
        private float hyperMinPhys = 1f;
        private Boolean hyperPauseOnTimeReached = false;
        private string hyperWarpHours = "0"; private string hyperWarpMinutes = "0"; private string hyperWarpSeconds = "0";
        private double hyperWarpTime = Mathf.Infinity;
        private float hyperMaxRate = 2f;
        private string hyperMaxRateText = "2";
        
        //WARP
        private Boolean autoWarping = false;
        private string warpYears = "0"; private string warpDays = "0"; private string warpHours = "0"; private string warpMinutes = "0"; private string warpSeconds = "0";
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
        }//TODO AppToolbar implementation (waiting until it will work in the TS)
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
        }
        private void Start()
        {
            FlightCamera[] cams = FlightCamera.FindObjectsOfType(typeof(FlightCamera)) as FlightCamera[];
            cam = cams[0];

            warpText = FindObjectOfType<ScreenMessages>();
            warpTextColor = warpText.textStyles[1].normal.textColor;

            //EVENTS
            GameEvents.onFlightReady.Add(this.onFlightReady);
            GameEvents.onGameSceneLoadRequested.Add(this.onGameSceneLoadRequested);
            GameEvents.onGamePause.Add(this.onGamePause);
            GameEvents.onGameUnpause.Add(this.onGameUnpause);
            GameEvents.onHideUI.Add(this.onHideUI);
            GameEvents.onShowUI.Add(this.onShowUI);
            GameEvents.onLevelWasLoaded.Add(this.onLevelWasLoaded);
            GameEvents.onTimeWarpRateChanged.Add(this.onTimeWarpRateChanged);
            GameEvents.onPartDestroyed.Add(this.onPartDestroyed);
            GameEvents.onVesselDestroy.Add(this.onVesselDestroy);
            GameEvents.onVesselGoOffRails.Add(this.onVesselGoOffRails);
        }

        //EVENT MANAGERS
        private void onFlightReady()
        {  
            operational = true;
            timeSlider = 0f;
        }
        private void onGameSceneLoadRequested(GameScenes gs)
        {
            exitHyper();
            operational = false;
            autoWarping = false;
            tempInvisible = true;
            if (HighLogic.LoadedSceneIsFlight)
            {
                setSpontanousDestructionMechanic(true);
            }
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
            tempInvisible = true;
            fpsVisible = false;
        }
        private void onShowUI()
        {
            tempInvisible = false;
            fpsVisible = true;
        }
        private void onLevelWasLoaded(GameScenes gs)
        {
            tempInvisible = false;

            if (ToolbarManager.ToolbarAvailable)
            {
                toolbarButton.TexturePath = Settings.visible ? "TimeControl/active" : "TimeControl/inactive";
            }
        }
        private void onPartDestroyed(Part p)
        {
            try
            {
                if (HighLogic.LoadedSceneIsFlight && (FlightGlobals.ActiveVessel == null || p.vessel == FlightGlobals.ActiveVessel))
                {
                    exitHyper();
                }
            }
            catch { }
        }
        private void onVesselDestroy(Vessel v)
        {
            try
            {
                if (HighLogic.LoadedSceneIsFlight && (FlightGlobals.ActiveVessel == null || v == FlightGlobals.ActiveVessel))
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
                if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel.altitude > FlightGlobals.currentMainBody.maxAtmosphereAltitude)
                {
                    setSpontanousDestructionMechanic(false);
                }
            }
            else
            {
                operational = true;
            }
        }
        private void onVesselGoOffRails(Vessel v)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                setSpontanousDestructionMechanic(true);
            }
        }


        //UPDATE FUNCTIONS
        private void Update()
        {
            preUpdate();

            if (timeWarp != null)
            {
                if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
                {
                    flightUpdate();
                }
            }
            else if (HighLogic.LoadedScene != GameScenes.MAINMENU)
            {
                timeWarp = TimeWarp.fetch;
            }
            if (timeWarp == null)
                return;

            setWarpLevels(Settings.warpLevels);

            for (int i = 0; i < Settings.warpLevels; i++)
            {
                timeWarp.warpRates[i] = parseSTOI(Settings.customWarpRates[i]);
                for (int j = 0; j < PSystemManager.Instance.localBodies.Count; j++)
                {
                    PSystemManager.Instance.localBodies[j].timeWarpAltitudeLimits[i] = parseSTOI(Settings.customAltitudeLimits[j][i]);
                }
            }

            if (timeWarp.current_rate_index >= Settings.warpLevels && timeWarp.Mode == TimeWarp.Modes.HIGH)
            {
                TimeWarp.SetRate(Settings.warpLevels - 1, false);
            }

            if ((Planetarium.GetUniversalTime() > warpTime || Mathf.Abs((float)Planetarium.GetUniversalTime() - (float)warpTime) < 60) && autoWarping)
            {
                autoWarping = false;
            }
        }
            private void preUpdate()
            {
                if (selectedSOI == -1)
                {
                    SOI = currentSOI;
                }
                else
                {
                    SOI = selectedSOI;
                }

                if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.T))
                {
                    Settings.visible = !Settings.visible;
                    toolbarButton.TexturePath = Settings.visible ? "TimeControl/active" : "TimeControl/inactive";
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT && GameSettings.SAS_TOGGLE.primary == KeyCode.T)
                    {
                        FlightGlobals.ActiveVessel.ActionGroups.ToggleGroup(KSPActionGroup.SAS);
                    }
                }

                //if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKey(KeyCode.T))
                //{
                //    showDebugGUI = true;
                //}

                sizeWindows();

                if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    if (mouseOverAnyWindow())
                    {
                        if (!(InputLockManager.GetControlLock("KAC lock") == ControlTypes.KSC_FACILITIES))
                        {
                            InputLockManager.SetControlLock(ControlTypes.KSC_FACILITIES, "KAC lock");
                        }
                    }
                    else
                    {
                        if (InputLockManager.GetControlLock("KAC lock") == ControlTypes.KSC_FACILITIES)
                        {
                            InputLockManager.RemoveControlLock("KAC lock");
                        }
                    }
                }

                if (HighLogic.LoadedSceneIsFlight)
                {
                    if (FlightGlobals.ready)
                    {
                        currentSOI = getPlanetaryID(FlightGlobals.currentMainBody.name);
                    }
                }
                else if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    currentSOI = 1; //Kerbin
                }
                else if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                {
                    if (PlanetariumCamera.fetch.target.type == MapObject.MapObjectType.CELESTIALBODY)
                    {
                        currentSOI = getPlanetaryID(PlanetariumCamera.fetch.target.celestialBody.name);
                    }
                    else if (PlanetariumCamera.fetch.target.type == MapObject.MapObjectType.VESSEL)
                    {
                        currentSOI = getPlanetaryID(PlanetariumCamera.fetch.target.vessel.mainBody.name);
                    }
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
                    this.msg = ScreenMessages.PostScreenMessage("PAUSED", .5f, ScreenMessageStyle.UPPER_CENTER);
                }
            }
            if (pauseOnNextFixedUpdate)
            {
                timePaused = true;
                pauseOnNextFixedUpdate = false;
            }
        }
        private void flightUpdate()
        {
            UpdateCollision();

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

            if (fld == null)
            {
                fld = FlightResultsDialog.FindObjectOfType<FlightResultsDialog>();
            }
            if (supressFlightResultsDialog)
            {
                fld.enabled = false;
            }
            else
            {
                fld.enabled = true;
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

            if (operational && !(TimeWarp.CurrentRate > 1))
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

            if (showDebugGUI)//TODO remove this and replace with Kreeper
            {
                debugWindowPosition = constrainToScreen(GUILayout.Window(9001, debugWindowPosition, onDebugGUI, "Time Control: Debug"));
            }

            if (Settings.visible && !tempInvisible)
            {
                if (HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    Settings.menuWindowPosition = constrainToScreen(GUILayout.Window("Time Control".GetHashCode(), Settings.menuWindowPosition, onMenuGUI, "Time Control"));
                }

                if (HighLogic.LoadedSceneIsFlight)
                {
                    Settings.flightWindowPosition = constrainToScreen(GUILayout.Window("Time Control".GetHashCode()+1, Settings.flightWindowPosition, onFlightGUI, "Time Control"));

                    if (settingsOpen && !Settings.minimized)
                    {
                        Settings.settingsWindowPosition = constrainToScreen(GUILayout.Window("Time Control".GetHashCode()+2, Settings.settingsWindowPosition, onSettingsGUI, "Time Control Settings"));
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
                modeRails();
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
                    
                    GUILayout.BeginHorizontal();
                    
                    float lmaxWarpRatef;
                    string lmaxWarpRatestr;

                    lmaxWarpRatestr = GUILayout.TextField(hyperMaxRateText, GUILayout.Width(35));
                    if (lmaxWarpRatestr != hyperMaxRateText && float.TryParse(lmaxWarpRatestr, out lmaxWarpRatef))
                    {
                        hyperMaxRateText = lmaxWarpRatestr;
                        hyperMaxRate = lmaxWarpRatef;

                    } 

                    lmaxWarpRatef = GUILayout.HorizontalSlider(hyperMaxRate, 2f, 100f);
                    lmaxWarpRatef = (float)Math.Truncate(lmaxWarpRatef);
                    if (lmaxWarpRatef != hyperMaxRate) {
                        hyperMaxRate = lmaxWarpRatef;
                        hyperMaxRateText = lmaxWarpRatef.ToString();
                    }
                    
                    GUILayout.EndHorizontal();

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
                        GUILayout.Label("Current SOI: " + PSystemManager.Instance.localBodies[currentSOI].name);
                        GUILayout.FlexibleSpace();
                        GameSettings.KERBIN_TIME = GUILayout.Toggle(GameSettings.KERBIN_TIME, "Use Kerbin Time");
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label("", GUILayout.Height(5));

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.BeginHorizontal(GUILayout.Width(175));
                        {
                            GUILayout.Label("Warp Rate");
                            warpLevelsButtons();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.Label("Altitude Limit");
                        GUILayout.FlexibleSpace();
                        string s;
                        if (selectedSOI == -1)
                        {
                            s = "Current";
                        }
                        else
                        {
                            s = PSystemManager.Instance.localBodies[selectedSOI].name;
                        }
                        SOISelect = GUILayout.Toggle(SOISelect, s, "button", GUILayout.Width(80));
                    }
                    GUILayout.EndHorizontal();

                    warpScroll = GUILayout.BeginScrollView(warpScroll, GUILayout.Height(203));
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.BeginVertical(GUILayout.Width(20));
                            {
                                for (int i = 0; i < Settings.warpLevels; i++)
                                {
                                    GUILayout.Label(i + 1 + ":");
                                }
                            }
                            GUILayout.EndVertical();

                            GUILayout.BeginVertical(GUILayout.Width(150));
                            {
                                GUI.enabled = false;
                                Settings.customWarpRates[0] = GUILayout.TextField(Settings.customWarpRates[0], 10);
                                GUI.enabled = true;

                                for (int i = 1; i < Settings.warpLevels; i++)
                                {
                                    Settings.customWarpRates[i] = GUILayout.TextField(Settings.customWarpRates[i], 10);
                                }
                            }
                            GUILayout.EndVertical();

                            GUILayout.BeginVertical(GUILayout.Width(150));
                            {
                                if (!SOISelect)
                                {
                                    GUI.enabled = false;
                                    Settings.customAltitudeLimits[SOI][0] = GUILayout.TextField(Settings.customAltitudeLimits[SOI][0]);
                                    GUI.enabled = true;

                                    for (int i = 1; i < Settings.warpLevels; i++)
                                    {
                                        Settings.customAltitudeLimits[SOI][i] = GUILayout.TextField(Settings.customAltitudeLimits[SOI][i], 20);
                                    }
                                }
                                else
                                {
                                    onSOISelect();
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Reset warp rates", GUILayout.Width(174)))
                        {
                            for (int i = 0; i < Settings.standardWarpRates.Length; i++)
                            {
                                Settings.customWarpRates[i] = Settings.standardWarpRates[i];
                            }
                        }

                        if (GUILayout.Button("Reset body altitude limits"))
                        {
                            for (int i = 0; i < Settings.standardAltitudeLimits[SOI].Length; i++)
                            {
                                Settings.customAltitudeLimits[SOI][i] = Settings.standardAltitudeLimits[SOI][i];
                            }
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label("", GUILayout.Height(5));

                    onWarpTo();
                }
                GUILayout.EndVertical();
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
                        if (GUILayout.Button("Auto Warp", GUILayout.Width(174)))
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
                private void warpLevelsButtons()
                {
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        if (Settings.warpLevels < 99)
                        {
                            Settings.warpLevels++;
                            setWarpLevels(Settings.warpLevels);
                        }
                    }
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        if (Settings.warpLevels > 8)
                        {
                            Settings.warpLevels--;
                            setWarpLevels(Settings.warpLevels);
                        }
                    }
                }
                    private void setWarpLevels(int levels)
                    {
                        while (timeWarp.warpRates.Length > levels)//remove
                        {
                            Settings.customWarpRates.RemoveAt(Settings.customWarpRates.Count - 1);
                            Array.Resize(ref timeWarp.warpRates, levels);
                            foreach (List<string> s in Settings.customAltitudeLimits)
                            {
                                s.RemoveAt(s.Count - 1);
                            }
                            foreach (CelestialBody c in PSystemManager.Instance.localBodies)
                            {
                                Array.Resize(ref c.timeWarpAltitudeLimits, levels);
                            }
                        }
                        while (timeWarp.warpRates.Length < levels)//add
                        {
                            Settings.customWarpRates.Add(Settings.customWarpRates[Settings.customWarpRates.Count - 1]);
                            Array.Resize(ref timeWarp.warpRates, levels);
                            foreach (List<string> s in Settings.customAltitudeLimits)
                            {
                                s.Add(s[s.Count - 1]);
                            }
                            foreach (CelestialBody c in PSystemManager.Instance.localBodies)
                            {
                                Array.Resize(ref c.timeWarpAltitudeLimits, levels);
                            }
                        }
                    }
                private void onSOISelect()
                {
                    if (GUILayout.Button("Current"))
                    {
                        selectedSOI = -1;
                        SOISelect = false;
                        warpScroll.y = 0;
                    }
                    int i = 0;
                    foreach (CelestialBody c in PSystemManager.Instance.localBodies)
                    {
                        if (GUILayout.Button(c.name))
                        {
                            selectedSOI = i;
                            SOISelect = false;
                            warpScroll.y = 0;
                        }
                        i++;
                    }
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
                for (int i = 0; i < keyLabels.Length; i++)
                {
                    if (keySet[i])
                    {
                        GUI.contentColor = Color.yellow;
                    }
                    else
                    {
                        GUI.contentColor = c;
                    }
                    string pos = (convertToExponential(Settings.customKeySlider) != 1) ? ("1/" + convertToExponential(Settings.customKeySlider).ToString()) : "1";
                    if (GUILayout.Button(keyLabels[i].Replace("#", pos)  + Settings.keyBinds[i].primary.ToString()))
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
                    if (i == 4)//slider
                    {
                        Settings.customKeySlider = GUILayout.HorizontalSlider(Settings.customKeySlider, 0f, 1f);
                    }
                }
                GUI.contentColor = c;

                //update checker
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
                            Application.OpenURL("https://kerbalstuff.com/mod/21/Time_Control");
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
                GUILayout.Label("Online Version: " + updateNumber);
                GUILayout.Label("Update Available: " + updateAvailable);
                GUILayout.Label("Minimized: " + Settings.minimized);
                GUILayout.Label("Visible (toolbar): " + Settings.visible);
                GUILayout.Label("Temp Invisible: " + tempInvisible);
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
                GUILayout.Label("rates:" + Settings.customWarpRates.Count);
                GUILayout.Label("limits:" + Settings.customAltitudeLimits.Count);
                GUILayout.Label("limits[0]:" + Settings.customAltitudeLimits[0].Count);
                GUILayout.Label("levels:" + Settings.warpLevels);
                GUILayout.Label("current: " + currentSOI);

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
        private Rect constrainToScreen(Rect r)
        {
            r.x = Mathf.Clamp(r.x, 0, Screen.width - r.width);
            r.y = Mathf.Clamp(r.y, 0, Screen.height - r.height);
            return r;
        }
        private void sizeWindows()
        {
            Settings.menuWindowPosition.height = 0;
            Settings.menuWindowPosition.width = 375;

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
                    Settings.flightWindowPosition.width = 375;
                    break;
            }

            settingsButton.x = Settings.flightWindowPosition.xMax - Settings.flightWindowPosition.xMin - 20; //Move the ?
        }

        //HELPER FUNCTIONS
        private Boolean mouseOverWindow(Rect r, Boolean visible)
        {
            return visible && r.Contains(Event.current.mousePosition);
        }
        private Boolean mouseOverAnyWindow()
        {
            return (mouseOverWindow(Settings.flightWindowPosition, Settings.visible) ||
                    mouseOverWindow(Settings.menuWindowPosition, Settings.visible) ||
                    mouseOverWindow(Settings.settingsWindowPosition, Settings.visible)
                    );
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
            for (int i = 0; i < Settings.keyBinds.Length; i++)
            {
                if (keySet[i])
                {
                    var e = Event.current;
                    if (e.isKey && e.keyCode != KeyCode.None)
                    {
                        Settings.keyBinds[i] = new KeyBinding(e.keyCode);
                        keySet[i] = false;
                    }
                    for (int j = 0; j < 20; j++)
                    {
                        if (Input.GetKeyDown("joystick button " + j))
                        {
                            Settings.keyBinds[i] = new KeyBinding((KeyCode)Enum.Parse(typeof(KeyCode), "JoystickButton" + j));
                            keySet[i] = false;
                        }
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
                timeSlider = Settings.customKeySlider;
            }
            if (Settings.keyBinds[5].GetKeyDown())
            {
                timePaused = !timePaused;
            }
            if (Settings.keyBinds[6].GetKeyDown())
            {
                timePaused = false;
                pauseOnNextFixedUpdate = true;
            }
            if (Settings.keyBinds[7].GetKeyDown())
            {
                if (((inverseTimeScale == 1) && operational && !Settings.fpsKeeperActive) && !hyperWarping)
                {
                    hyperWarping = true;
                }
                else if (hyperWarping)
                {
                    exitHyper();
                }
            }

        }
        private void warpMessage(string text)
        {
            ScreenMessages.RemoveMessage(this.msg);
            this.msg = ScreenMessages.PostScreenMessage(text, 3f, ScreenMessageStyle.UPPER_CENTER);
        }
		private int getPlanetaryID(string s) //ID from name
		{
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
        private void setSpontanousDestructionMechanic(bool b) //users...
        {
            if (FlightGlobals.currentMainBody.pqsController != null)
            {
                PQS p = FlightGlobals.currentMainBody.pqsController;
                var mods = p.transform.GetComponentsInChildren(typeof(PQSMod), true);
                foreach (var m in mods)
                {
                    if (m.GetType() == typeof(PQSCity))
                    {
                        PQSCity q = (PQSCity)m;
                        q.gameObject.SetActive(b);
                    }
                }
            }
        }

        //Kragrathea's fix
        private GameObject FindLocal(string name)
        {
            try
            {
                return (LocalSpace.Transform.FindChild(name).gameObject);
            }
            catch
            {
            }
            return null;
        }
        private void SetLocalCollision(string planetName, bool enabled = true)
        {
            var localPlanet = FindLocal(planetName);
            var cols = localPlanet.GetComponentsInChildren<Collider>();
            foreach (var c in cols)
            {
                if (c.enabled != enabled)
                {
                    print("Updating collision " + c.gameObject.name + "=" + enabled);
                    c.enabled = enabled;
                }
            }
        }
        private string currentBodyName;
        private void UpdateCollision()
        {
            if (FlightGlobals.currentMainBody != null && FlightGlobals.currentMainBody.bodyName != currentBodyName)
            {
                print("Body change " + currentBodyName + " to " + FlightGlobals.currentMainBody.bodyName);
                if (currentBodyName != null)
                    SetLocalCollision(currentBodyName, false);
                currentBodyName = FlightGlobals.currentMainBody.bodyName;
                SetLocalCollision(currentBodyName, true);
            }
        }
    }
}
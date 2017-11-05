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

using System;
using UnityEngine;
using KSP.IO;
using System.IO;

using SC = System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace TimeControl
{
    [KSPAddon( KSPAddon.Startup.MainMenu, true )]
    public class GlobalSettings : MonoBehaviour
    {
        #region Singleton
        public static bool IsReady { get; private set; } = false;
        public static GlobalSettings Instance { get; private set; }
        #endregion

        private const string loggingLevelNodeName = "LoggingLevel";

        private const string topNodeName = "GlobalSettings";
        private readonly string globalSettingsFilePath = string.Format( "{0}/GlobalSettings.cfg", PluginAssemblyUtilities.PathPluginData );
        private ConfigNode config;

        private float spaceCenterWindow_x = 100;
        private float spaceCenterWindow_y = 100;
        private float trackingStationWindow_x = 100;
        private float trackingStationWindow_y = 100;
        private float flightModeWindow_x = 100;
        private float flightModeWindow_y = 100;
        private bool spaceCenterWindowIsDisplayed = false;
        private bool trackingStationWindowIsDisplayed = false;
        private bool flightModeWindowIsDisplayed = false;

        public float SpaceCenterWindow_x { get => spaceCenterWindow_x; set => spaceCenterWindow_x = value; }
        public float SpaceCenterWindow_y { get => spaceCenterWindow_y; set => spaceCenterWindow_y = value; }
        public float TrackingStationWindow_x { get => trackingStationWindow_x; set => trackingStationWindow_x = value; }
        public float TrackingStationWindow_y { get => trackingStationWindow_y; set => trackingStationWindow_y = value; }
        public float FlightModeWindow_x { get => flightModeWindow_x; set => flightModeWindow_x = value; }
        public float FlightModeWindow_y { get => flightModeWindow_y; set => flightModeWindow_y = value; }
        public bool SpaceCenterWindowIsDisplayed { get => spaceCenterWindowIsDisplayed; set => spaceCenterWindowIsDisplayed = value; }
        public bool TrackingStationWindowIsDisplayed { get => trackingStationWindowIsDisplayed; set => trackingStationWindowIsDisplayed = value; }
        public bool FlightModeWindowIsDisplayed { get => flightModeWindowIsDisplayed; set => flightModeWindowIsDisplayed = value; }

        #region MonoBehavior
        private void Awake()
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( Awake );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                DontDestroyOnLoad( this );
                Instance = this;
            }
        }
        private void Start()
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( Start );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                config = new ConfigNode( "TimeControl" );
                global::GameEvents.OnGameSettingsApplied.Add( this.OnGameSettingsApplied );
                global::GameEvents.onGameStateSaved.Add( this.onGameStateSaved );
                global::GameEvents.onGameStatePostLoad.Add( this.onGameStatePostLoad );
                global::GameEvents.onLevelWasLoaded.Add( this.onLevelWasLoaded );

                IsReady = true;
            }
        }
        #endregion
        
        private void onLevelWasLoaded(GameScenes gs)
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( OnGameSettingsApplied );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Load();
            }
        }

        private void OnGameSettingsApplied()
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( OnGameSettingsApplied );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Save();
            }
        }

        private void onGameStateSaved(Game data)
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( onGameStateSaved );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Save();
            }
        }

        private void onGameStatePostLoad(ConfigNode node)
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( onGameStatePostLoad );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Load();
            }
        }

        #region Save and Load
        /// <summary>
        /// Recreate the global settings file
        /// </summary>
        internal void Rebuild()
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( Rebuild );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (!GlobalSettings.IsReady)
                {
                    Log.Warning( "Not Ready", logBlockName );
                    return;
                }

                Log.Info( "Loading Global Settings File", logBlockName, true );

                if (System.IO.File.Exists( globalSettingsFilePath ))
                {
                    System.IO.File.Delete( globalSettingsFilePath );
                }
                Save();
            }
        }

        /// <summary>
        /// Save global time control settings to file
        /// </summary>
        internal void Save()
        {
            try
            {
                const string logBlockName = nameof( GlobalSettings ) + "." + nameof( Save );
                using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
                {
                    if (!GlobalSettings.IsReady)
                    {
                        Log.Warning( "Not Ready", logBlockName );
                        return;
                    }

                    if (config == null)
                    {
                        Log.Error( "Cannot save global settings, config is NULL", logBlockName );
                        return;
                    }

                    ConfigNode configSettings;
                    if (!config.HasNode( topNodeName ))
                        config.AddNode( topNodeName );
                    configSettings = config.GetNode( topNodeName );
                    configSettings.SetValue( loggingLevelNodeName, Log.LoggingLevel.ToString(), true );

                    configSettings.SetValue( nameof( this.spaceCenterWindow_x ), this.spaceCenterWindow_x, true );
                    configSettings.SetValue( nameof( this.spaceCenterWindow_y ), this.spaceCenterWindow_y, true );
                    configSettings.SetValue( nameof( this.trackingStationWindow_x ), this.trackingStationWindow_x, true );
                    configSettings.SetValue( nameof( this.trackingStationWindow_y ), this.trackingStationWindow_y, true );
                    configSettings.SetValue( nameof( this.flightModeWindow_x ), this.flightModeWindow_x, true );
                    configSettings.SetValue( nameof( this.flightModeWindow_y ), this.flightModeWindow_y, true );
                    configSettings.SetValue( nameof( this.spaceCenterWindowIsDisplayed ), this.spaceCenterWindowIsDisplayed, true );
                    configSettings.SetValue( nameof( this.trackingStationWindowIsDisplayed ), this.trackingStationWindowIsDisplayed, true );
                    configSettings.SetValue( nameof( this.flightModeWindowIsDisplayed ), this.flightModeWindowIsDisplayed, true );

                    config.Save( globalSettingsFilePath );

                    Log.Info( "Global Settings Saved to file " + globalSettingsFilePath, logBlockName );

                    TimeControlEvents.OnTimeControlGlobalSettingsSaved.Fire( true );
                }
            }
            catch (Exception e)
            {
                Log.Error( e.Message );
                Log.Error( e.StackTrace );
            }
        }

        private void assignFromConfigFloat(ConfigNode cn, string property, ref float v)
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( assignFromConfigFloat );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (cn.HasValue( property ) && float.TryParse( cn.GetValue( property ), out float cv ))
                {
                    v = cv;
                }
                else
                {
                    Log.Warning( property + " has error in configuration file. Using default.", logBlockName );
                }
            }
        }

        private void assignFromConfigBool(ConfigNode cn, string property, ref bool v)
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( assignFromConfigBool );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (cn.HasValue( property ) && bool.TryParse( cn.GetValue( property ), out bool cv ))
                {
                    v = cv;
                }
                else
                {
                    Log.Warning( property + " has error in configuration file. Using default.", logBlockName );
                }
            }
        }

        /// <summary>
        /// Load global time control settings from file and apply to objects
        /// </summary>
        internal void Load()
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( Load );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (!GlobalSettings.IsReady)
                {
                    Log.Warning( "Not Ready", logBlockName );
                    return;
                }

                Log.Info( "Loading Global Settings File", logBlockName, true );

                ConfigNode config = ConfigNode.Load( globalSettingsFilePath );
                if (config == null)//file does not exist
                {
                    Rebuild();
                    config = ConfigNode.Load( globalSettingsFilePath );
                    if (config == null)//file does not exist
                    {
                        const string message = "Serious Error. Please review your output.log and if necessary report this error on the KSP Forum. Cannot load global settings file even after trying to create it. Failing.";
                        Log.PopupError( message );
                        Log.Error( message, logBlockName, true );

                        throw new InvalidOperationException( message );
                    }
                }

                ConfigNode configSettings;
                if (!config.HasNode( topNodeName ))
                {
                    string message = "No top level node found in config. This error is not fatal to the load process. Default settings will be used instead.";
                    Log.Error( message, logBlockName );
                    Rebuild();
                    return;
                }
                configSettings = config.GetNode( topNodeName );

                assignFromConfigFloat( configSettings, nameof( this.spaceCenterWindow_x ), ref this.spaceCenterWindow_x );
                assignFromConfigFloat( configSettings, nameof( this.spaceCenterWindow_y ), ref this.spaceCenterWindow_y );
                assignFromConfigFloat( configSettings, nameof( this.trackingStationWindow_x ), ref this.trackingStationWindow_x );
                assignFromConfigFloat( configSettings, nameof( this.trackingStationWindow_y ), ref this.trackingStationWindow_y );
                assignFromConfigFloat( configSettings, nameof( this.flightModeWindow_x ), ref this.flightModeWindow_x );
                assignFromConfigFloat( configSettings, nameof( this.flightModeWindow_y ), ref this.flightModeWindow_y );
                assignFromConfigBool( configSettings, nameof( this.spaceCenterWindowIsDisplayed ), ref this.spaceCenterWindowIsDisplayed );
                assignFromConfigBool( configSettings, nameof( this.trackingStationWindowIsDisplayed ), ref this.trackingStationWindowIsDisplayed );
                assignFromConfigBool( configSettings, nameof( this.flightModeWindowIsDisplayed ), ref this.flightModeWindowIsDisplayed );

                if (configSettings.HasValue( loggingLevelNodeName ))
                {
                    string ll = configSettings.GetValue( loggingLevelNodeName );
                    if (Enum.IsDefined( typeof( LogSeverity ), ll ))
                    {
                        Log.LoggingLevel = (LogSeverity)Enum.Parse( typeof( LogSeverity ), ll );
                        if (HighLogic.CurrentGame?.Parameters?.CustomParams<TimeControlParameterNode>()?.LoggingLevel != null)
                        {
                            HighLogic.CurrentGame.Parameters.CustomParams<TimeControlParameterNode>().LoggingLevel = Log.LoggingLevel;
                        }
                    }
                    else
                    {
                        Log.Warning( loggingLevelNodeName + " has error in configuration file. Using default.", logBlockName );
                    }
                }
                else
                {
                    Log.Warning( loggingLevelNodeName + " not found in configuration file. Using default.", logBlockName );
                }
                
                TimeControlEvents.OnTimeControlGlobalSettingsLoaded.Fire( true );

                Log.Info( "Time Control Logging Level Set to " + Log.LoggingLevel.ToString(), logBlockName, true );

                Save();
            }
        }

        #endregion
    }
}

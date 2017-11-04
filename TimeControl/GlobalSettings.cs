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

        #region MonoBehavior
        private void Awake()
        {
            const string logBlockName = nameof( GlobalSettings ) + "." + nameof( Awake );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                DontDestroyOnLoad( this );
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

                IsReady = true;
            }
        }
        #endregion

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

                System.IO.File.Delete( globalSettingsFilePath );
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
                    config.Save( globalSettingsFilePath );

                    Log.Info( "Global Settings Saved to file " + globalSettingsFilePath, logBlockName );

                }
            }
            catch (Exception e)
            {
                Log.Error( e.Message );
                Log.Error( e.StackTrace );
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
                    Save();
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
                    string message = "No Settings node found in config. This error is not fatal to the load process. Default settings will be used instead.";
                    Log.Error( message, logBlockName );
                    Save();
                }
                else
                {
                    configSettings = config.GetNode( topNodeName );

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
                            Log.Error( "Logging Level of " + ll + " is not defined. Using default and rebuilding file.", logBlockName, true );
                            System.IO.File.Delete( globalSettingsFilePath );
                            Save();
                        }
                    }
                    else
                    {
                        Log.Error( loggingLevelNodeName + " has error in settings configuration. Using default and rebuilding file.", logBlockName, true );
                        System.IO.File.Delete( globalSettingsFilePath );
                        Save();
                    }
                }

                Log.Info( "Time Control Logging Level Set to " + Log.LoggingLevel.ToString(), logBlockName, true );
            }
        }

        #endregion
    }
}

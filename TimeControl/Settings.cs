/*
 * Time Control
 * Created by Xaiier
 * License: MIT
 */

using System;
using SC = System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using KSP.IO;
using System.IO;
using KSPPluginFramework;

namespace TimeControl
{
    [KSPAddon( KSPAddon.Startup.MainMenu, true )]
    public class Settings : MonoBehaviour, SC.INotifyPropertyChanged
    {
        #region Singleton
        public static bool IsReady { get; private set; } = false;
        private static Settings instance;
        public static Settings Instance { get { return instance; } }
        #endregion
        #region INotifyPropertyChanged        
        /// <summary>
        /// This static class defines the magic strings that are checked for the property changed events by a GUI.
        /// No real "binding" in Unity but this helps me decouple the GUI from the saving of settings, so a new GUI, in theory, could be written easier.
        /// Also makes changes a bit easier since the tooling allows renaming of constants much easier than strings.
        /// </summary>
        public static class PropertyStrings
        {
            public const string WindowsVisible = "WindowsVisible";
            public const string WindowMinimized = "WindowMinimized";
            public const string WindowSelectedFlightMode = "WindowSelectedFlightMode";
            public const string FpsMinSlider = "FpsMinSlider";
            public const string ShowFPS = "ShowFPS";
            public const string LoggingLevel = "LoggingLevel";
            public const string UseStockToolbar = "UseStockToolbar";
            public const string UseBlizzyToolbar = "UseBlizzyToolbar";
            public const string FlightWindowX = "FlightWindowX";
            public const string FlightWindowY = "FlightWindowY";
            public const string SpaceCenterWindowX = "SpaceCenterWindowX";
            public const string SpaceCenterWindowY = "SpaceCenterWindowY";
            public const string SettingsWindowX = "SettingsWindowX";
            public const string SettingsWindowY = "SettingsWindowY";
            public const string FpsX = "FpsX";
            public const string FpsY = "FpsY";
            public const string ShowScreenMessages = "ShowScreenMessages";
            public const string MaxDeltaTimeSlider = "MaxDeltaTimeSlider";
            public const string WarpLevels = "WarpLevels";
            public const string CustomKeySlider = "CustomKeySlider";
            public const string SaveInterval = "SaveInterval";
            public const string UseCustomDateTimeFormatter = "UseCustomDateTimeFormatter";
            public const string SettingsWindowOpen = "SettingsWindowOpen";
            public const string SupressFlightResultsDialog = "SupressFlightResultsDialog";
        }

        public event SC.PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            string logCaller = "Settings.OnPropertyChanged()";
            Log.Trace( "Property Changed: " + name, logCaller );
            PropertyChanged?.Invoke( this, new SC.PropertyChangedEventArgs( name ) );
        }
        #endregion


        #region MonoBehavior
        private void Awake()
        {
            string logCaller = "Settings.Awake()";
            Log.Trace( "method start", logCaller );

            DontDestroyOnLoad( this );
            instance = this;

            TCResources.loadGUIAssets();

            Log.Trace( "method end", logCaller );
        }
        private void Start()
        {
            string logCaller = "Settings.Start()";
            Log.Trace( "method start", logCaller );

            StartCoroutine( ReloadConfigWhenTimeWarpReady() );

            Log.Trace( "method end", logCaller );
        }
        private void Update()
        {
            if (!IsReady)
                return;
            if (HighLogic.LoadedScene == GameScenes.EDITOR)
                return;

            if (needsSaved && (Time.realtimeSinceStartup - lastSave > saveInterval))
            {
                string logCaller = "Settings.Update()";
                Log.Trace( "Saving Settings & Time Control Config Starting", logCaller );

                lastSave = Time.realtimeSinceStartup;

                GameSettings.PHYSICS_FRAME_DT_LIMIT = MaxDeltaTimeSlider;
                GameSettings.SaveSettings();

                buildAndSaveConfig( false );

                Log.Trace( "Saving Settings & Time Control Config Complete", logCaller );
            }
        }
        #endregion

        #region Coroutines
        public IEnumerator ReloadConfigWhenTimeWarpReady()
        {
            string logCaller = "Settings.ReloadConfig";
            Log.Trace( "coroutine start", logCaller );

            while (TimeWarp.fetch == null)
                yield return null;

            ResetConfigs();

            Log.Trace( "coroutine end", logCaller );
            yield break;
        }

        private void ResetConfigs()
        {
            string logCaller = "Settings.ResetConfigs";
            Log.Trace( "method start", logCaller );

            resetKeyBindingsToDefault();
            resetWarpRatesToDefault();
            resetAltitudeLimitsToDefault();
            loadConfig();
            warpLevels = customWarpRates.Count;

            IsReady = configLoadSuccessful;
            if (IsReady)
                Log.Info( "TimeController.Settings is Ready!", logCaller );
            else
                Log.Error( "Something went wrong loading the time controller settings!", logCaller );

            Log.Trace( "method end", logCaller );
        }

        #endregion
        #region Key Bindings
        private void resetKeyBindingsToDefault()
        {
            string logCaller = "Settings.resetKeyBindingsToDefault()";
            Log.Trace( "method start", logCaller );

            keyBinds = keyBinds ?? new List<TCKeyBinding>();
            keyBinds.Clear();

            keyBinds.Add( new TCKeyBinding { TCUserAction = TimeControlUserAction.SpeedUp, Description = "Speed Up", KeyCombination = new List<KeyCode>() } );
            keyBinds.Add( new TCKeyBinding { TCUserAction = TimeControlUserAction.SlowDown, Description = "Slow Down", KeyCombination = new List<KeyCode>() } );
            keyBinds.Add( new TCKeyBinding { TCUserAction = TimeControlUserAction.Realtime, Description = "Realtime", KeyCombination = new List<KeyCode>() } );
            keyBinds.Add( new TCKeyBinding { TCUserAction = TimeControlUserAction.SlowMo64, Description = "1/64x", KeyCombination = new List<KeyCode>() } );
            keyBinds.Add( new TCKeyBinding { TCUserAction = TimeControlUserAction.CustomKeySlider, Description = "Custom-#x", KeyCombination = new List<KeyCode>() } );
            keyBinds.Add( new TCKeyBinding { TCUserAction = TimeControlUserAction.Pause, Description = "Pause", KeyCombination = new List<KeyCode>() } );
            keyBinds.Add( new TCKeyBinding { TCUserAction = TimeControlUserAction.Step, Description = "Step", KeyCombination = new List<KeyCode>() } );
            keyBinds.Add( new TCKeyBinding { TCUserAction = TimeControlUserAction.HyperWarp, Description = "Hyper Warp", KeyCombination = new List<KeyCode>() } );
            keyBinds.Add( new TCKeyBinding { TCUserAction = TimeControlUserAction.ToggleGUI, Description = "Toggle GUI", KeyCombination = new List<KeyCode>() } );

            Log.Trace( "method end", logCaller );
        }
        private void configCreateOrUpdateKeyBinds(ConfigNode cn)
        {
            string logCaller = "Settings.configCreateOrUpdateKeyBinds(ConfigNode)";
            Log.Trace( "method start", logCaller );

            string kb = "KeyBinds";
            // Rebuild the keybinds node
            if (cn.HasNode( kb ))
                cn.RemoveNode( kb );
            ConfigNode keyBindsNode = cn.AddNode( kb );
            foreach (TCKeyBinding k in KeyBinds)
            {
                keyBindsNode.SetValue( k.TCUserAction.ToString(), k.IsKeyAssigned ? k.KeyCombinationString : "[" + KeyCode.None.ToString() + "]", true );
            }

            Log.Trace( "method end", logCaller );
        }
        private void configLoadKeyBinds(ConfigNode cn)
        {
            string logCaller = "Settings.configCreateOrUpdateKeyBinds(ConfigNode)";
            Log.Trace( "method start", logCaller );

            string kb = "KeyBinds";
            ConfigNode keyBindsNode;
            if (!config.HasNode( kb ))
            {
                string message = "No KeyBinds node found in config. This error is not fatal to the load process. Default keybinds will be used instead.";
                Log.Trace( message, logCaller );
                return;
            }
            keyBindsNode = config.GetNode( kb );

            ConfigNode.ValueList vl = keyBindsNode.values;

            foreach (TCKeyBinding k in KeyBinds)
            {
                string userAction = k.TCUserAction.ToString();
                if (vl.Contains( userAction ))
                {
                    string keycombo = vl.GetValue( userAction );
                    List<KeyCode> iekc = KeyboardInputManager.GetKeyCombinationFromString( keycombo );
                    if (iekc == null)
                    {
                        Log.Warning( "Key combination is not defined correctly: " + keycombo + " - Using default for user action " + userAction, logCaller );
                        continue;
                    }
                    if (iekc.Contains( KeyCode.None ))
                    {
                        k.KeyCombination = new List<KeyCode>();
                    }
                    else
                    {
                        k.KeyCombination = new List<KeyCode>( iekc );
                        k.KeyCombinationString = keycombo;
                    }
                }
            }

            Log.Trace( "method end", logCaller );
        }
        #endregion
        #region Settings
        private void configCreateOrUpdateSettings(ConfigNode cn)
        {
            string logCaller = "Settings.configCreateOrUpdateSettings(ConfigNode)";
            Log.Trace( "method start", logCaller );
            string cSet = "Settings";
            ConfigNode configSettings;
            if (!config.HasNode( cSet ))
                config.AddNode( cSet );
            configSettings = config.GetNode( cSet );

            configSettings.SetValue( PropertyStrings.WindowsVisible, windowsVisible.ToString(), true );
            configSettings.SetValue( PropertyStrings.WindowMinimized, windowsMinimized.ToString(), true );
            configSettings.SetValue( PropertyStrings.WindowSelectedFlightMode, windowSelectedFlightMode.ToString(), true );
            configSettings.SetValue( PropertyStrings.FpsMinSlider, fpsMinSlider.ToString(), true );
            configSettings.SetValue( PropertyStrings.ShowFPS, showFPS.ToString(), true );
            configSettings.SetValue( PropertyStrings.LoggingLevel, loggingLevel.ToString(), true );
            configSettings.SetValue( PropertyStrings.UseStockToolbar, useStockToolbar.ToString(), true );
            configSettings.SetValue( PropertyStrings.UseBlizzyToolbar, useBlizzyToolbar.ToString(), true );
            configSettings.SetValue( PropertyStrings.FlightWindowX, flightWindowX.ToString(), true );
            configSettings.SetValue( PropertyStrings.FlightWindowY, flightWindowY.ToString(), true );
            configSettings.SetValue( PropertyStrings.SpaceCenterWindowX, spaceCenterWindowX.ToString(), true );
            configSettings.SetValue( PropertyStrings.SpaceCenterWindowY, spaceCenterWindowY.ToString(), true );
            configSettings.SetValue( PropertyStrings.SettingsWindowX, settingsWindowX.ToString(), true );
            configSettings.SetValue( PropertyStrings.SettingsWindowY, settingsWindowY.ToString(), true );
            configSettings.SetValue( PropertyStrings.FpsX, fpsPosition.yMin.ToString(), true );
            configSettings.SetValue( PropertyStrings.FpsY, fpsPosition.yMin.ToString(), true );
            configSettings.SetValue( PropertyStrings.CustomKeySlider, customKeySlider.ToString(), true );
            configSettings.SetValue( PropertyStrings.ShowScreenMessages, showScreenMessages.ToString(), true );
            configSettings.SetValue( PropertyStrings.UseCustomDateTimeFormatter, useCustomDateTimeFormatter.ToString(), true );
            configSettings.SetValue( PropertyStrings.SettingsWindowOpen, settingsWindowOpen.ToString(), true );
            configSettings.SetValue( PropertyStrings.SupressFlightResultsDialog, supressFlightResultsDialog.ToString(), true );
            configSettings.SetValue( PropertyStrings.SaveInterval, saveInterval.ToString(), true );

            Log.Trace( "method end", logCaller );
        }
        private void assignFromConfigBool(ConfigNode cn, string property, ref bool v)
        {
            string logCaller = "Settings.assignFromConfigBool(ConfigNode, string, ref bool)";
            Log.Trace( "method start", logCaller );

            bool lb;
            if (cn.HasValue( property ) && bool.TryParse( cn.GetValue( property ), out lb ))
                v = lb;
            else
                Log.Warning( property + " has error in settings configuration. Using default.", logCaller );

            Log.Trace( "method end", logCaller );
        }
        private void assignFromConfigInt(ConfigNode cn, string property, ref int v)
        {
            string logCaller = "Settings.assignFromConfigInt(ConfigNode, string, ref int)";
            Log.Trace( "method start", logCaller );

            int cv;
            if (cn.HasValue( property ) && int.TryParse( cn.GetValue( property ), out cv ))
                v = cv;
            else
                Log.Warning( property + " has error in settings configuration. Using default.", logCaller );

            Log.Trace( "method end", logCaller );
        }
        private void assignFromConfigFloat(ConfigNode cn, string property, ref float v)
        {
            string logCaller = "Settings.assignFromConfigFloat(ConfigNode, string, ref int)";
            Log.Trace( "method start", logCaller );

            float cv;
            if (cn.HasValue( property ) && float.TryParse( cn.GetValue( property ), out cv ))
                v = cv;
            else
                Log.Warning( property + " has error in settings configuration. Using default.", logCaller );

            Log.Trace( "method end", logCaller );
        }
        private void configLoadSettings(ConfigNode cn)
        {
            string logCaller = "Settings.configLoadSettings(ConfigNode)";
            Log.Trace( "method start", logCaller );

            string cSet = "Settings";
            ConfigNode configSettings;
            if (!config.HasNode( cSet ))
            {
                string message = "No Settings node found in config. This error is not fatal to the load process. Default settings will be used instead.";
                Log.Error( message, logCaller );
                return;
            }
            configSettings = config.GetNode( cSet );

            if (configSettings.HasValue( PropertyStrings.LoggingLevel ))
            {
                string ll = configSettings.GetValue( PropertyStrings.LoggingLevel );
                if (Enum.IsDefined( typeof( LogSeverity ), ll ))
                    LoggingLevel = (LogSeverity)Enum.Parse( typeof( LogSeverity ), ll );
                else
                    Log.Warning( "Logging Level of " + ll + " is not defined. Using default.", logCaller );
            }
            else
                Log.Warning( PropertyStrings.LoggingLevel + " has error in settings configuration. Using default.", logCaller );

            //INTERNAL
            assignFromConfigBool( configSettings, PropertyStrings.WindowsVisible, ref windowsVisible );
            assignFromConfigBool( configSettings, PropertyStrings.WindowMinimized, ref windowsMinimized );
            assignFromConfigInt( configSettings, PropertyStrings.WindowSelectedFlightMode, ref windowSelectedFlightMode );
            assignFromConfigInt( configSettings, PropertyStrings.FpsMinSlider, ref fpsMinSlider );
            assignFromConfigBool( configSettings, PropertyStrings.ShowFPS, ref showFPS );

            assignFromConfigBool( configSettings, PropertyStrings.UseStockToolbar, ref useStockToolbar );
            assignFromConfigBool( configSettings, PropertyStrings.UseBlizzyToolbar, ref useBlizzyToolbar );

            assignFromConfigInt( configSettings, PropertyStrings.FlightWindowX, ref flightWindowX );
            assignFromConfigInt( configSettings, PropertyStrings.FlightWindowY, ref flightWindowY );
            assignFromConfigInt( configSettings, PropertyStrings.SpaceCenterWindowX, ref spaceCenterWindowX );
            assignFromConfigInt( configSettings, PropertyStrings.SpaceCenterWindowY, ref spaceCenterWindowY );
            assignFromConfigInt( configSettings, PropertyStrings.SettingsWindowX, ref settingsWindowX );
            assignFromConfigInt( configSettings, PropertyStrings.SettingsWindowY, ref settingsWindowY );
            assignFromConfigInt( configSettings, PropertyStrings.FpsX, ref fpsX );
            assignFromConfigInt( configSettings, PropertyStrings.FpsY, ref fpsY );
            assignFromConfigFloat( configSettings, PropertyStrings.CustomKeySlider, ref customKeySlider );
            assignFromConfigBool( configSettings, PropertyStrings.ShowScreenMessages, ref showScreenMessages );
            assignFromConfigBool( configSettings, PropertyStrings.UseCustomDateTimeFormatter, ref useCustomDateTimeFormatter );
            assignFromConfigBool( configSettings, PropertyStrings.SettingsWindowOpen, ref settingsWindowOpen );
            assignFromConfigBool( configSettings, PropertyStrings.SupressFlightResultsDialog, ref supressFlightResultsDialog );
            assignFromConfigFloat( configSettings, PropertyStrings.SaveInterval, ref saveInterval );

            Log.Trace( "method end", logCaller );
        }
        #endregion
        #region Rails Data
        private void configCreateOrUpdateRailsData(ConfigNode cn)
        {
            string logCaller = "Settings.configCreateOrUpdateRailsData(ConfigNode)";
            Log.Trace( "method start", logCaller );

            string cRails = "RailsData";
            ConfigNode railsData;
            if (!config.HasNode( cRails ))
                config.AddNode( cRails );
            railsData = config.GetNode( cRails );

            configCreateOrUpdateCustomWarpRates( railsData );
            configCreateOrUpdateCustomAltitudeLimits( railsData );

            Log.Trace( "method end", logCaller );
        }
        private void configLoadRailsData(ConfigNode cn)
        {
            string logCaller = "Settings.configLoadRailsData(ConfigNode)";
            Log.Trace( "method start", logCaller );

            string cRails = "RailsData";
            ConfigNode railsData;
            if (!config.HasNode( cRails ))
            {
                string message = "No RailsData node found in config. This error is not fatal to the load process. Default warp rates and altitude limits will be used instead.";
                Log.Trace( message, logCaller );
                return;
            }
            railsData = config.GetNode( cRails );

            configLoadCustomWarpRates( railsData );
            configLoadCustomAltitudeLimits( railsData );

            Log.Trace( "method end", logCaller );
        }
        #region Warp Rates
        private void resetWarpRatesToDefault()
        {
            string logCaller = "Settings.resetWarpRatesToDefault()";
            Log.Trace( "method start", logCaller );

            TimeWarp tw = TimeWarp.fetch;
            IEnumerable<TCWarpRate> defaultWarpRates = tw.warpRates.Select( x => new TCWarpRate() { WarpRate = Convert.ToInt64( x ).ToString() } );

            standardWarpRates = standardWarpRates ?? new List<TCWarpRate>();
            standardWarpRates.Clear();
            standardWarpRates.AddRange( defaultWarpRates );

            Log.Trace( "standard warp rates reset", logCaller );

            if (customWarpRates != null)
                customWarpRates.Clear();

            setCustomWarpRates( defaultWarpRates );

            Log.Trace( "method end", logCaller );
        }
        private void setCustomWarpRates(IEnumerable<TCWarpRate> wr)
        {
            string logCaller = "Settings.setCustomWarpRates()";
            Log.Trace( "method start", logCaller );

            customWarpRates = customWarpRates ?? new List<TCWarpRate>();
            customWarpRates.Clear();
            customWarpRates.AddRange( wr );

            if (customWarpRates.Count == 0)
            {
                string message = "No values found in IEnumerable. Falling back to standard warp rates.";
                Log.Warning( message, logCaller );

                customWarpRates.AddRange( standardWarpRates );
            }

            Log.Trace( "method end", logCaller );
        }
        private void configCreateOrUpdateCustomWarpRates(ConfigNode cn)
        {
            string logCaller = "Settings.configCreateOrUpdateCustomWarpRates(ConfigNode)";
            Log.Trace( "method start", logCaller );

            // Rebuild the node
            if (cn.HasNode( "customWarpRates" ))
                cn.RemoveNode( "customWarpRates" );
            ConfigNode customWarpRatesNode = cn.AddNode( "customWarpRates" );

            Log.Trace( "creating custom warp rates node", logCaller );
            for (int i = 0; i < customWarpRates.Count; i++)
            {
                customWarpRatesNode.AddValue( "customWarpRate" + i, customWarpRates[i].WarpRate );
            }

            Log.Trace( "method end", logCaller );
        }
        private void configLoadCustomWarpRates(ConfigNode cn)
        {
            string logCaller = "Settings.configLoadCustomWarpRates(ConfigNode)";
            Log.Trace( "method start", logCaller );

            if (!cn.HasNode( "customWarpRates" ))
            {
                string message = "No customWarpRates node found in config. This error is not fatal to the load process. Default warp rates will be used instead.";
                Log.Warning( message, logCaller );
                setCustomWarpRates( standardWarpRates );
                return;
            }

            ConfigNode customWarpRatesNode = cn.GetNode( "customWarpRates" );
            List<TCWarpRate> ltcwr = new List<TCWarpRate>();

            foreach (string s in customWarpRatesNode.GetValuesStartsWith( "customWarpRate" ))
            {
                int num;
                if (!(int.TryParse( s, out num )))
                {
                    string message = "A custom warp rate is not defined as an integer. This error is not fatal to the load process. Default warp rates will be used instead.";
                    Log.Warning( message, logCaller );
                    setCustomWarpRates( standardWarpRates );
                    return;
                }
                ltcwr.Add( new TCWarpRate { WarpRate = s } );
            }
            setCustomWarpRates( ltcwr );

            Log.Trace( "method end", logCaller );
        }
        #endregion
        #region Altitude Limits        
        private void setCustomAltitudeLimits(Dictionary<CelestialBody, IEnumerable<TCAltitudeLimit>> al)
        {
            string logCaller = "Settings.setCustomAltitudeLimits";
            Log.Trace( "method start", logCaller );

            customAltitudeLimits = customAltitudeLimits ?? new Dictionary<CelestialBody, List<TCAltitudeLimit>>();

            foreach (var kp in al)
            {
                CelestialBody cb = kp.Key;
                IEnumerable<TCAltitudeLimit> tcwr = kp.Value;

                setCustomAltitudeLimitsForBody( cb, tcwr );

                Log.Trace( "custom altitude limits for " + cb.name + " set", logCaller );
            }

            SetNeedsSavedFlag();
            Log.Trace( "method end", logCaller );
        }

        private void setCustomAltitudeLimitsForBody(CelestialBody cb, IEnumerable<TCAltitudeLimit> tcwr)
        {
            string logCaller = "Settings.setCustomAltitudeLimitsForBody";
            Log.Trace( "method start", logCaller );

            if (!customAltitudeLimits.ContainsKey( cb ))
                customAltitudeLimits.Add( cb, null );

            customAltitudeLimits[cb] = customAltitudeLimits[cb] ?? new List<TCAltitudeLimit>();
            customAltitudeLimits[cb].Clear();
            customAltitudeLimits[cb].AddRange( tcwr );

            SetNeedsSavedFlag();
            Log.Trace( "method end", logCaller );
        }

        private void resetAltitudeLimitsToDefault()
        {
            string logCaller = "Settings.resetAltitudeLimitsToDefault";
            Log.Trace( "method start", logCaller );

            standardAltitudeLimits = standardAltitudeLimits ?? new Dictionary<CelestialBody, List<TCAltitudeLimit>>();
            standardAltitudeLimits.Clear();

            if (customAltitudeLimits != null)
                customAltitudeLimits.Clear();

            Dictionary<CelestialBody, IEnumerable<TCAltitudeLimit>> customLimits = new Dictionary<CelestialBody, IEnumerable<TCAltitudeLimit>>();

            foreach (CelestialBody cb in FlightGlobals.Bodies)
            {
                IEnumerable<TCAltitudeLimit> defaultCurrentBodyLimits = cb.timeWarpAltitudeLimits.Select( x => new TCAltitudeLimit { AltitudeLimit = Convert.ToInt64( x ).ToString() } );
                Log.Trace( "default altitude limits for " + cb.name + " found", logCaller );

                if (!standardAltitudeLimits.ContainsKey( cb ))
                    standardAltitudeLimits.Add( cb, null );

                standardAltitudeLimits[cb] = standardAltitudeLimits[cb] ?? new List<TCAltitudeLimit>();
                standardAltitudeLimits[cb].Clear();
                standardAltitudeLimits[cb].AddRange( defaultCurrentBodyLimits );

                Log.Trace( "standard altitude limits for " + cb.name + " set", logCaller );

                customLimits.Add( cb, defaultCurrentBodyLimits.ToList() );
            }

            setCustomAltitudeLimits( customLimits );

            Log.Trace( "method end", logCaller );
        }
        private void configCreateOrUpdateCustomAltitudeLimits(ConfigNode cn)
        {
            string logCaller = "Settings.configCreateOrUpdateCustomAltitudeLimits(ConfigNode)";
            Log.Trace( "method start", logCaller );


            string cal = "customAltitudeLimits";

            ConfigNode customAltitudeLimitsNode = (cn.HasNode( cal )) ? cn.GetNode( cal ) : cn.AddNode( cal );

            foreach (var b in customAltitudeLimits)
            {
                string cbName = b.Key.name;

                // Rebuild the celestial body node. 
                if (customAltitudeLimitsNode.HasNode( cbName ))
                    customAltitudeLimitsNode.RemoveNode( cbName );

                ConfigNode celestial = customAltitudeLimitsNode.AddNode( b.Key.name );

                for (int j = 0; j < customWarpRates.Count; j++)
                {
                    celestial.AddValue( "customAltitudeLimit" + j, b.Value[j].AltitudeLimit );
                }
                Log.Write( "custom altitude limits for " + b.Key.name + " built", "Settings.configCreateOrUpdateCustomAltitudeLimits(ConfigNode)", LogSeverity.Trace );
            }

            Log.Write( "method end", "Settings.configCreateCustomAltitudeLimits", LogSeverity.Trace );
        }
        private void configLoadCustomAltitudeLimits(ConfigNode cn)
        {
            string logCaller = "Settings.configLoadCustomAltitudeLimits(ConfigNode)";
            Log.Trace( "method start", logCaller );

            if (!cn.HasNode( "customAltitudeLimits" ))
            {
                string message = "No customAltitudeLimitsNode node found in config. This error is not fatal to the load process. Default altitude limits will be used.";
                Log.Warning( message, logCaller );
                return;
            }

            ConfigNode customAltitudeLimitsNode = cn.GetNode( "customAltitudeLimits" );

            Dictionary<CelestialBody, IEnumerable<TCAltitudeLimit>> customLimits = new Dictionary<CelestialBody, IEnumerable<TCAltitudeLimit>>();
            foreach (CelestialBody cb in FlightGlobals.Bodies)
            {
                // Ignore bodies that exist in this game but don't exist in the file.
                if (!customAltitudeLimitsNode.HasNode( cb.name ))
                {
                    Log.Warning( "Celestial Body " + cb.name + " not found in Config Node. Using Defaults", logCaller );

                    List<TCAltitudeLimit> standardList = new List<TCAltitudeLimit>( standardAltitudeLimits[cb] );
                    customLimits.Add( cb, standardList );

                    continue;
                }

                ConfigNode celestialLimitsNode = customAltitudeLimitsNode.GetNode( cb.name );
                List<TCAltitudeLimit> t = new List<TCAltitudeLimit>();
                foreach (string al in celestialLimitsNode.GetValuesStartsWith( "customAltitudeLimit" ))
                {
                    t.Add( new TCAltitudeLimit { AltitudeLimit = al } );
                    // Only load up to the warp limit
                    if (t.Count == customWarpRates.Count)
                        break;
                }

                customLimits.Add( cb, t );
            }
            setCustomAltitudeLimits( customLimits );

            Log.Trace( "method end", logCaller );
        }
        #endregion
        #endregion
        #region Save and Load
        private void buildAndSaveConfig(bool fullRebuild = false)
        {
            string logCaller = "Settings.buildAndSaveConfig(bool)";
            Log.Trace( "method start", logCaller );

            config = config ?? new ConfigNode( "TimeControl" );

            if (fullRebuild)
                config.ClearData();

            configCreateOrUpdateSettings( config );
            configCreateOrUpdateKeyBinds( config );
            configCreateOrUpdateRailsData( config );

            config.Save( PluginUtilities.settingsFilePath );
            needsSaved = false;

            Log.Info( "Settings Saved to File", logCaller );

            Log.Trace( "method end", logCaller );
        }
        private void loadConfig()
        {
            string logCaller = "Settings.loadConfig()";
            Log.Trace( "method start", logCaller );

            config = ConfigNode.Load( PluginUtilities.settingsFilePath );
            if (config == null)//file does not exist
            {
                buildAndSaveConfig( true );
                config = ConfigNode.Load( PluginUtilities.settingsFilePath );
                if (config == null)//file does not exist
                {
                    Log.Error( "Serious Error. Cannot load config file even after trying to build. Failing.", logCaller );
                    return;
                }
            }

            // Even if we just created the config, load it anyway, to be sure.            
            configLoadSettings( config );
            configLoadKeyBinds( config );
            configLoadRailsData( config );

            configLoadSuccessful = true;

            Log.Info( "Settings Loaded from File", logCaller );

            Log.Trace( "method end", logCaller );
        }
        #endregion
        public void SetNeedsSavedFlag()
        {
            needsSaved = true;
        }

        private void setNeedsSaved(object sender, SC.ListChangedEventArgs e)
        {
            SetNeedsSavedFlag();
        }

        public void ResetWarpRates()
        {
            for (int i = 0; i < standardWarpRates.Count; i++)
                customWarpRates[i].WarpRate = standardWarpRates[i].WarpRate;

            TimeController.Instance.UpdateInternalTimeWarpArrays();
            SetNeedsSavedFlag();
        }

        public void ResetCustomAltitudeLimitsForBody(CelestialBody cb)
        {
            for (int i = 0; i < standardAltitudeLimits[cb].Count; i++)
            {
                customAltitudeLimits[cb][i].AltitudeLimit = standardAltitudeLimits[cb][i].AltitudeLimit;
            }

            TimeController.Instance.UpdateInternalTimeWarpArrays();
            SetNeedsSavedFlag();
        }

        public void AddWarpLevel()
        {
            string logCaller = "TimeController.AddWarpLevel";
            Log.Trace( "method start", logCaller );

            if (!IsReady)
            {
                Log.Trace( "method end", logCaller );
                return;
            }

            if (WarpLevels >= 99)
            {
                Log.Warning( "cannot go above 99 warp levels", logCaller );
                Log.Trace( "method end", logCaller );
                return;
            }

            var cwr = CustomWarpRates;
            var calv = CustomAltitudeLimits.Values;

            cwr.Add( new TCWarpRate() { WarpRate = cwr[cwr.Count - 1].WarpRate } );
            foreach (IList<TCAltitudeLimit> s in Settings.Instance.CustomAltitudeLimits.Values)
                s.Add( new TCAltitudeLimit() { AltitudeLimit = s[s.Count - 1].AltitudeLimit } );
            WarpLevels++;
            SetNeedsSavedFlag();

            if (TimeController.Instance == null || !TimeController.IsReady)
                Log.Warning( "Cannot add warp level to TimeController, object not found or is not ready", logCaller );
            else
                TimeController.Instance.UpdateInternalTimeWarpArrays();

            Log.Info( "Warp Level Added", logCaller );

            Log.Trace( "method end", logCaller );
        }

        public void RemoveWarpLevel()
        {
            string logCaller = "TimeController.RemoveWarpLevel";
            Log.Trace( "method start", logCaller );

            if (!IsReady)
            {
                Log.Trace( "method end", logCaller );
                return;
            }

            if (WarpLevels <= 8)
            {
                Log.Warning( "cannot go below 8 warp levels", logCaller );
                Log.Trace( "method end", logCaller );
                return;
            }

            var cwr = CustomWarpRates;
            var calv = CustomAltitudeLimits.Values;

            cwr.RemoveAt( cwr.Count - 1 );
            foreach (IList<TCAltitudeLimit> s in calv)
                s.RemoveAt( s.Count - 1 );
            WarpLevels--;
            SetNeedsSavedFlag();

            if (TimeController.Instance == null || !TimeController.IsReady)
                Log.Warning( "Cannot add warp level to TimeController, object not found or is not ready", logCaller );
            else
                TimeController.Instance.UpdateInternalTimeWarpArrays();

            Log.Info( "Warp Level Removed", logCaller );

            Log.Trace( "method end", logCaller );
        }
        #region Properties
        #region Static Properties
        public static float SaveIntervalMin {
            get {
                return 1f;
            }
        }
        public static float SaveIntervalMax {
            get {
                return 60f;
            }
        }
        public static float SaveIntervalDefault {
            get {
                return 5f;
            }
        }
        #endregion
        public LogSeverity LoggingLevel {
            get {
                return loggingLevel;
            }

            set {
                if (loggingLevel != value)
                {
                    loggingLevel = value;
                    Log.LoggingLevel = loggingLevel;
                    OnPropertyChanged( PropertyStrings.LoggingLevel );
                    SetNeedsSavedFlag();
                }
            }
        }
        public bool UseStockToolbar {
            get {
                return useStockToolbar;
            }

            set {
                if (useStockToolbar != value)
                {
                    useStockToolbar = value;
                    OnPropertyChanged( PropertyStrings.UseStockToolbar );
                    SetNeedsSavedFlag();
                }
            }
        }
        public bool UseBlizzyToolbar {
            get {
                return useBlizzyToolbar;
            }

            set {
                if (useBlizzyToolbar != value)
                {
                    useBlizzyToolbar = value;
                    OnPropertyChanged( PropertyStrings.UseBlizzyToolbar );
                    SetNeedsSavedFlag();
                }
            }
        }
        public int WindowSelectedFlightMode {
            get {
                return windowSelectedFlightMode;
            }

            set {
                if (windowSelectedFlightMode != value)
                {
                    windowSelectedFlightMode = value;
                    OnPropertyChanged( PropertyStrings.WindowSelectedFlightMode );
                    SetNeedsSavedFlag();
                }
            }
        }
        public bool ShowFPS {
            get {
                return showFPS;
            }

            set {
                if (showFPS != value)
                {
                    showFPS = value;
                    OnPropertyChanged( PropertyStrings.ShowFPS );
                    SetNeedsSavedFlag();
                }
            }
        }
        public int FpsMinSlider {
            get {
                return fpsMinSlider;
            }

            set {
                if (fpsMinSlider != value)
                {
                    fpsMinSlider = value;
                    OnPropertyChanged( PropertyStrings.FpsMinSlider );
                    SetNeedsSavedFlag();
                }
            }
        }
        public float MaxDeltaTimeSlider {
            get {
                return maxDeltaTimeSlider;
            }

            set {
                if (maxDeltaTimeSlider != value)
                {
                    maxDeltaTimeSlider = value;
                    OnPropertyChanged( PropertyStrings.MaxDeltaTimeSlider );
                    SetNeedsSavedFlag();
                }
            }
        }
        public int WarpLevels {
            get {
                return warpLevels;
            }

            set {
                if (warpLevels != value)
                {
                    warpLevels = value;
                    OnPropertyChanged( PropertyStrings.WarpLevels );
                    SetNeedsSavedFlag();
                }
            }
        }
        public float CustomKeySlider {
            get {
                return customKeySlider;
            }

            set {
                if (customKeySlider != value)
                {
                    customKeySlider = value;
                    OnPropertyChanged( PropertyStrings.CustomKeySlider );
                    SetNeedsSavedFlag();
                }
            }
        }


        #region GUI Properties
        public int FlightWindowX {
            get {
                return flightWindowX;
            }

            set {
                if (flightWindowX != value)
                {
                    flightWindowX = value;
                    OnPropertyChanged( PropertyStrings.FlightWindowX );
                    SetNeedsSavedFlag();
                }
            }
        }
        public int FlightWindowY {
            get {
                return flightWindowY;
            }

            set {
                if (flightWindowY != value)
                {
                    flightWindowY = value;
                    OnPropertyChanged( PropertyStrings.FlightWindowY );
                    SetNeedsSavedFlag();
                }
            }
        }
        public int SpaceCenterWindowX {
            get {
                return spaceCenterWindowX;
            }

            set {
                if (spaceCenterWindowX != value)
                {
                    spaceCenterWindowX = value;
                    OnPropertyChanged( PropertyStrings.SpaceCenterWindowX );
                    SetNeedsSavedFlag();
                }
            }
        }
        public int SpaceCenterWindowY {
            get {
                return spaceCenterWindowY;
            }

            set {
                if (spaceCenterWindowY != value)
                {
                    spaceCenterWindowY = value;
                    OnPropertyChanged( PropertyStrings.SpaceCenterWindowY );
                    SetNeedsSavedFlag();
                }
            }
        }
        public int SettingsWindowX {
            get {
                return settingsWindowX;
            }

            set {
                if (settingsWindowX != value)
                {
                    settingsWindowX = value;
                    OnPropertyChanged( PropertyStrings.SettingsWindowX );
                    SetNeedsSavedFlag();
                }
            }
        }
        public int SettingsWindowY {
            get {
                return settingsWindowY;
            }

            set {
                if (settingsWindowY != value)
                {
                    settingsWindowY = value;
                    OnPropertyChanged( PropertyStrings.SettingsWindowY );
                    SetNeedsSavedFlag();
                }
            }
        }
        public int FpsX {
            get {
                return fpsX;
            }

            set {
                if (fpsX != value)
                {
                    fpsX = value;
                    fpsPosition.x = value;
                    fpsPosition = fpsPosition.ClampToScreen();
                    OnPropertyChanged( PropertyStrings.FpsX );
                    SetNeedsSavedFlag();
                }
            }
        }
        public int FpsY {
            get {
                return fpsY;
            }
            set {
                if (fpsY != value)
                {
                    fpsY = value;
                    fpsPosition.y = value;
                    fpsPosition = fpsPosition.ClampToScreen();
                    OnPropertyChanged( PropertyStrings.FpsY );
                    SetNeedsSavedFlag();
                }
            }
        }
        public bool WindowMinimized {
            get {
                return windowsMinimized;
            }

            set {
                if (windowsMinimized != value)
                {
                    windowsMinimized = value;
                    OnPropertyChanged( PropertyStrings.WindowMinimized );
                    SetNeedsSavedFlag();
                }
            }
        }
        public bool WindowsVisible {
            get {
                return windowsVisible;
            }

            set {
                if (windowsVisible != value)
                {
                    windowsVisible = value;
                    OnPropertyChanged( PropertyStrings.WindowsVisible );
                    SetNeedsSavedFlag();
                }
            }
        }
        public bool ShowScreenMessages {
            get {
                return showScreenMessages;
            }
            set {
                if (showScreenMessages != value)
                {
                    showScreenMessages = value;
                    OnPropertyChanged( PropertyStrings.ShowScreenMessages );
                    SetNeedsSavedFlag();
                }
            }
        }
        public bool UseCustomDateTimeFormatter {
            get {
                return useCustomDateTimeFormatter;
            }
            set {
                if (useCustomDateTimeFormatter != value)
                {
                    useCustomDateTimeFormatter = value;
                    OnPropertyChanged( PropertyStrings.UseCustomDateTimeFormatter );
                    SetNeedsSavedFlag();
                }
            }
        }

        /// <summary>
        /// Number of seconds to wait before saving config file after a change
        /// Defaults to 30 seconds. Clamps between SaveIntervalMin and SaveIntervalMax
        /// </summary>
        public float SaveInterval {
            get {
                return saveInterval;
            }

            set {
                if (saveInterval != value)
                {
                    saveInterval = Mathf.Clamp( Mathf.Round( value ), SaveIntervalMin, SaveIntervalMax );
                    OnPropertyChanged( PropertyStrings.SaveInterval );
                    SetNeedsSavedFlag();
                }
            }
        }
        public bool SettingsWindowOpen {
            get {
                return settingsWindowOpen;
            }
            set {
                if (settingsWindowOpen != value)
                {
                    settingsWindowOpen = value;
                    OnPropertyChanged( PropertyStrings.SettingsWindowOpen );
                    SetNeedsSavedFlag();
                }
            }
        }
        public bool SupressFlightResultsDialog {
            get {
                return supressFlightResultsDialog;
            }
            set {
                if (supressFlightResultsDialog != value)
                {
                    supressFlightResultsDialog = value;
                    OnPropertyChanged( PropertyStrings.SupressFlightResultsDialog );
                    SetNeedsSavedFlag();
                }
            }
        }
        #endregion

        public List<TCWarpRate> StandardWarpRates {
            get {
                return standardWarpRates;
            }
        }
        public List<TCWarpRate> CustomWarpRates {
            get {
                return customWarpRates;
            }
        }
        public Dictionary<CelestialBody, List<TCAltitudeLimit>> StandardAltitudeLimits {
            get {
                return standardAltitudeLimits;
            }
        }
        public Dictionary<CelestialBody, List<TCAltitudeLimit>> CustomAltitudeLimits {
            get {
                return customAltitudeLimits;
            }
        }
        public List<TCKeyBinding> KeyBinds {
            get {
                return keyBinds;
            }
        }
        #endregion
        #region  Fields
        //Plugin Configuration
        private ConfigNode config;

        //Window Positions
        private int flightWindowX = 100;
        private int flightWindowY = 100;
        private int spaceCenterWindowX = 100;
        private int spaceCenterWindowY = 100;
        private int settingsWindowX = 100;
        private int settingsWindowY = 100;

        //FPS Display
        private Rect fpsPosition = new Rect( 155, 0, 100, 100 );
        private int fpsX = 155;
        private int fpsY = 0;

        //Display
        private bool windowsMinimized = false; //small mode
        private bool windowsVisible = true; //toolbar and hiding
        private bool settingsWindowOpen = false;
        private bool supressFlightResultsDialog = false;
        private bool useStockToolbar = true;
        private bool useBlizzyToolbar = true;
        private bool useCustomDateTimeFormatter = false;
        private bool configLoadSuccessful = false;
        private bool showScreenMessages = true;

        //Options
        private int windowSelectedFlightMode = 2;
        private bool showFPS = false;
        private int fpsMinSlider = 5;
        private float maxDeltaTimeSlider = GameSettings.PHYSICS_FRAME_DT_LIMIT;
        private float customKeySlider = 0f;
        private bool needsSaved = false;
        private float lastSave = 0f;
        private float saveInterval = SaveIntervalDefault; // Save changes every 5 seconds, if there are changes to save

        //Rails warp
        private int warpLevels = 8;
        private List<TCWarpRate> standardWarpRates;
        private List<TCWarpRate> customWarpRates;
        private Dictionary<CelestialBody, List<TCAltitudeLimit>> standardAltitudeLimits;
        private Dictionary<CelestialBody, List<TCAltitudeLimit>> customAltitudeLimits;
        private List<TCKeyBinding> keyBinds;

        private LogSeverity loggingLevel = Log.LoggingLevel;



        #endregion
    }
}

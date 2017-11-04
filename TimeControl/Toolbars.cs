using System;
using System.Collections;
using UnityEngine;
using KSP.UI.Screens;

namespace TimeControl
{
    [KSPAddon( KSPAddon.Startup.MainMenu, true )]
    internal sealed class Toolbars : MonoBehaviour
    {
        #region Singleton
        private static Toolbars instance;
        internal static Toolbars Instance { get { return instance; } }
        #endregion

        private ApplicationLauncher.AppScenes AppScenes = ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.TRACKSTATION;

        internal Texture2D buttonTexture;

        private ApplicationLauncherButton appLauncherButton;

        internal bool StockToolbarEnabled { get { return HighLogic.CurrentGame?.Parameters?.CustomParams<TimeControlParameterNode>()?.UseStockToolbar ?? false ; } }
        internal static bool IsAvailable { get { return ApplicationLauncher.Ready && ApplicationLauncher.Instance != null; } }

        internal bool IsReady { get; private set; } = false;

        #region MonoBehavior
        private void Awake()
        {
            DontDestroyOnLoad( this );
            instance = this;
        }

        private void Start()
        {
            buttonTexture = GameDatabase.Instance.GetTexture( PluginAssemblyUtilities.GameDatabasePathStockToolbarIcons + "/enabled", false );
            
            global::GameEvents.onGUIApplicationLauncherReady.Add( this.AppLauncherReady );
            global::GameEvents.onGUIApplicationLauncherDestroyed.Add( this.AppLauncherDestroyed );
            global::GameEvents.onLevelWasLoadedGUIReady.Add( this.AppLauncherDestroyed );
            global::GameEvents.OnGameSettingsApplied.Add( this.OnGameSettingsApplied );

            StartCoroutine( StartAfterSettingsAndGUIReady() );
        }

        #endregion

        private BlizzyToolbar.IButton toolbarButton;


        /// <summary>
        /// Configures the Toolbars once the Settings are loaded
        /// </summary>
        public IEnumerator StartAfterSettingsAndGUIReady()
        {
            while (!TimeControlIMGUI.IsReady)
                yield return null;
            
            if (BlizzyToolbar.ToolbarManager.ToolbarAvailable)
            {
                toolbarButton = BlizzyToolbar.ToolbarManager.Instance.add( "TimeControl", "button" );
                toolbarButton.TexturePath = "TimeControl/ToolbarIcons/BlizzyToolbarIcons/enabled";
                toolbarButton.ToolTip = "Time Control";
                toolbarButton.Visibility = new BlizzyToolbar.GameScenesVisibility( GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER ); //Places where the button should show up
                toolbarButton.OnClick += BlizzyToolbarButtonClick;
            }
            
            IsReady = true;

            yield break;
        }

        private void BlizzyToolbarButtonClick(BlizzyToolbar.ClickEvent e)
        {
            TimeControlIMGUI.Instance.ToggleGUIVisibility();
            Set( TimeControlIMGUI.Instance.WindowVisible );
        }

        private void OnGameSettingsApplied()
        {
            Reset();
        }

        private void OnClick()
        {
            if (!IsReady)
                return;

            TimeControlIMGUI.Instance.ToggleGUIVisibility();
            Set( TimeControlIMGUI.Instance.WindowVisible );
        }

        private void AppLauncherShow()
        {
            if (!IsReady)
                return;

           TimeControlIMGUI.Instance.TempUnHideGUI( "StockAppLauncher" );
        }
        private void AppLancherHide()
        {
            if (!IsReady)
                return;

            TimeControlIMGUI.Instance.TempHideGUI( "StockAppLauncher" );
        }

        private void AppLauncherReady() { if (!StockToolbarEnabled) return; Init(); }
        private void AppLauncherDestroyed(GameScenes gameScene) { if (HighLogic.LoadedSceneIsGame) return; Destroy(); }
        private void AppLauncherDestroyed() { Destroy(); }

        private void OnDestroy()
        {
            global::GameEvents.onGUIApplicationLauncherReady.Remove( this.AppLauncherReady );
            global::GameEvents.onGUIApplicationLauncherDestroyed.Remove( this.AppLauncherDestroyed );
            global::GameEvents.onLevelWasLoadedGUIReady.Remove( this.AppLauncherDestroyed );
        }

        private void Init()
        {
            if (!IsAvailable || !HighLogic.LoadedSceneIsGame) return;

            if (appLauncherButton == null)
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication( OnClick, OnClick, null, null, null, null, AppScenes, buttonTexture );

            Set( TimeControlIMGUI.Instance.WindowVisible );

            ApplicationLauncher.Instance.RemoveOnHideCallback( AppLancherHide );
            ApplicationLauncher.Instance.RemoveOnShowCallback( AppLauncherShow );
            ApplicationLauncher.Instance.AddOnShowCallback( AppLauncherShow );
            ApplicationLauncher.Instance.AddOnHideCallback( AppLancherHide );
        }

        private void Destroy()
        {
            if (appLauncherButton != null)
            {
                ApplicationLauncher.Instance.RemoveOnHideCallback( AppLancherHide );
                ApplicationLauncher.Instance.RemoveOnShowCallback( AppLauncherShow );
                ApplicationLauncher.Instance.RemoveModApplication( appLauncherButton );
                ApplicationLauncher.Instance.RemoveApplication( appLauncherButton );
                appLauncherButton = null;
            }
        }

        internal void Set(bool SetTrue, bool force = false)
        {
            if (!IsAvailable || appLauncherButton == null)
                return;

            if (SetTrue)
            {
                if (appLauncherButton.enabled == false)
                {
                    appLauncherButton.SetTrue( force );
                }
            }
            else
            {
                if (appLauncherButton.enabled == true)
                {
                    appLauncherButton.SetFalse( force );
                }
            }
        }

        internal void Reset()
        {
            if (appLauncherButton != null)
            {
                Set( false );
                if (!StockToolbarEnabled)
                {
                    Destroy();
                }
            }
            if (StockToolbarEnabled)
            {
                Init();
            }
        }
    }
}
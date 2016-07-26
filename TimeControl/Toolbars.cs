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

        internal bool StockToolbarEnabled { get { return Settings.Instance.UseStockToolbar; } }
        internal static bool isAvailable { get { return ApplicationLauncher.Ready && ApplicationLauncher.Instance != null; } }

        internal bool IsReady { get; private set; } = false;

        #region MonoBehavior
        private void Awake()
        {
            DontDestroyOnLoad( this );
            instance = this;
        }

        private void Start()
        {
            buttonTexture = TCResources.stockIcon;

            GameEvents.onGUIApplicationLauncherReady.Add( AppLauncherReady );
            GameEvents.onGUIApplicationLauncherDestroyed.Add( AppLauncherDestroyed );
            GameEvents.onLevelWasLoadedGUIReady.Add( AppLauncherDestroyed );

            StartCoroutine( StartAfterSettingsAndGUIReady() );
        }

        #endregion

        private BlizzyToolbar.IButton toolbarButton;


        /// <summary>
        /// Configures the Toolbars once the Settings are loaded
        /// </summary>
        public IEnumerator StartAfterSettingsAndGUIReady()
        {
            while (!Settings.IsReady || !TCGUI.IsReady)
                yield return null;

            Settings.Instance.PropertyChanged += SettingsPropertyChanged;

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
            Settings.Instance.WindowsVisible = !Settings.Instance.WindowsVisible;
        }

        private void SettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Settings.PropertyStrings.WindowsVisible)
            {
                Set( Settings.Instance.WindowsVisible );
            }

            if (e.PropertyName == Settings.PropertyStrings.UseStockToolbar)
            {
                Reset();
            }
        }

        private void OnClick()
        {
            if (!IsReady)
                return;

            Settings.Instance.WindowsVisible = !Settings.Instance.WindowsVisible;
        }

        private void AppLauncherShow()
        {
            if (!IsReady)
                return;

           TCGUI.Instance.TempUnHideGUI( "StockAppLauncher" );
        }
        private void AppLancherHide()
        {
            if (!IsReady)
                return;

            TCGUI.Instance.TempHideGUI( "StockAppLauncher" );
        }

        private void AppLauncherReady() { if (!StockToolbarEnabled) return; Init(); }
        private void AppLauncherDestroyed(GameScenes gameScene) { if (HighLogic.LoadedSceneIsGame) return; Destroy(); }
        private void AppLauncherDestroyed() { Destroy(); }

        private void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove( AppLauncherReady );
            GameEvents.onGUIApplicationLauncherDestroyed.Remove( AppLauncherDestroyed );
            GameEvents.onLevelWasLoadedGUIReady.Remove( AppLauncherDestroyed );
        }

        private void Init()
        {
            if (!isAvailable || !HighLogic.LoadedSceneIsGame) return;

            if (appLauncherButton == null)
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication( OnClick, OnClick, null, null, null, null, AppScenes, buttonTexture );

            Set( Settings.Instance.WindowsVisible );

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
            if (!isAvailable || appLauncherButton == null)
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
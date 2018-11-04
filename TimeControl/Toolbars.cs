using System;
using System.Collections;
using UnityEngine;
using KSP.UI.Screens;

namespace TimeControl
{
    [KSPAddon( KSPAddon.Startup.Instantly, true )]
    internal sealed class Toolbars : MonoBehaviour
    {
        #region Singleton
        internal static Toolbars Instance { get; private set; }
        internal bool IsReady { get; private set; } = false;
        #endregion

        private ApplicationLauncher.AppScenes AppScenes = ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.TRACKSTATION;
        private Texture2D buttonTexture;
        private ApplicationLauncherButton appLauncherButton;
        private BlizzyToolbar.IButton toolbarButton;
        
        private bool StockToolbarEnabled
        {
            get => HighLogic.CurrentGame?.Parameters?.CustomParams<TimeControlParameterNode>()?.UseStockToolbar ?? false;
        }

        private static bool AppLauncherIsAvailable
        {
            get => ApplicationLauncher.Ready && ApplicationLauncher.Instance != null;
        }

        #region MonoBehavior
        private void Awake()
        {
            DontDestroyOnLoad( this );
            Instance = this;
        }

        private void Start()
        {
            StartCoroutine( Configure() );
        }

        #endregion


        /// <summary>
        /// Configures the Toolbars once the Settings are loaded
        /// </summary>
        public IEnumerator Configure()
        {            
            while (!GlobalSettings.IsReady || !TimeControlIMGUI.IsReady)
            {
                yield return new WaitForSeconds( 1f );
            }

            buttonTexture = GameDatabase.Instance.GetTexture( PluginAssemblyUtilities.GameDatabasePathStockToolbarIcons + "/enabled", false );

            global::GameEvents.onGUIApplicationLauncherReady.Add( this.AppLauncherReady );
            global::GameEvents.onGUIApplicationLauncherDestroyed.Add( this.AppLauncherDestroyed );
            global::GameEvents.onLevelWasLoadedGUIReady.Add( this.AppLauncherDestroyed );
            global::GameEvents.OnGameSettingsApplied.Add( this.OnGameSettingsApplied );

            if (BlizzyToolbar.ToolbarManager.ToolbarAvailable)
            {
                toolbarButton = BlizzyToolbar.ToolbarManager.Instance.add( "TimeControl", "button" );
                toolbarButton.TexturePath = "TimeControl/ToolbarIcons/BlizzyToolbarIcons/enabled";
                toolbarButton.ToolTip = "Time Control";
                toolbarButton.Visibility = new BlizzyToolbar.GameScenesVisibility( GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER ); //Places where the button should show up
                toolbarButton.OnClick += BlizzyToolbarButtonClick;
            }

            IsReady = true;

            Reset();

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
            {
                return;
            }

            TimeControlIMGUI.Instance.ToggleGUIVisibility();
            Set( TimeControlIMGUI.Instance.WindowVisible );
        }

        private void AppLauncherShow()
        {
            if (!IsReady)
            {
                return;
            }

            TimeControlIMGUI.Instance.TempUnHideGUI( "StockAppLauncher" );
        }
        private void AppLancherHide()
        {
            if (!IsReady)
            {
                return;
            }
            TimeControlIMGUI.Instance.TempHideGUI( "StockAppLauncher" );
        }

        private void AppLauncherReady()
        {
            if (!StockToolbarEnabled)
            {
                return;
            }

            Init();
        }
        private void AppLauncherDestroyed(GameScenes gameScene)
        {
            if (HighLogic.LoadedSceneIsGame)
            {
                return;
            }

            Destroy();
        }
        private void AppLauncherDestroyed()
        {
            Destroy();
        }

        private void OnDestroy()
        {
            global::GameEvents.onGUIApplicationLauncherReady.Remove( this.AppLauncherReady );
            global::GameEvents.onGUIApplicationLauncherDestroyed.Remove( this.AppLauncherDestroyed );
            global::GameEvents.onLevelWasLoadedGUIReady.Remove( this.AppLauncherDestroyed );
            global::GameEvents.OnGameSettingsApplied.Remove( this.OnGameSettingsApplied );
        }

        private void Init()
        {
            if (!AppLauncherIsAvailable || !HighLogic.LoadedSceneIsGame)
            {
                return;
            }

            if (appLauncherButton == null)
            {
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication( OnClick, OnClick, null, null, null, null, AppScenes, buttonTexture );
            }

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
            if (!AppLauncherIsAvailable || appLauncherButton == null)
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

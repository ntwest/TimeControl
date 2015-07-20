using System;
using System.Collections;
using UnityEngine;

namespace TimeControl
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    internal sealed class TCStockToolbar : MonoBehaviour
    {
        #region Singleton
        private static readonly TCStockToolbar instance = new TCStockToolbar();

        private TCStockToolbar() { 
            DontDestroyOnLoad(this);    
        }

        internal static TCStockToolbar Instance
        {
            get 
            {
                return instance;
            }
        }
        #endregion

        private ApplicationLauncher.AppScenes AppScenes = ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.TRACKSTATION;
        
        private string TexturePath = TC.MOD + "/active";
        internal Texture2D buttonTexture;

        private ApplicationLauncherButton appLauncherButton;

        internal bool Enabled { get { return Settings.useStockToolbar; } }
        internal static bool isAvailable { get { return ApplicationLauncher.Ready && ApplicationLauncher.Instance != null; } }

        #region MonoBehavior

        private void Awake()
        {

        }

        private void Start()
        {
            buttonTexture = GameDatabase.Instance.GetTexture(TexturePath, false);

            GameEvents.onGUIApplicationLauncherReady.Add(AppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(AppLauncherDestroyed);
            GameEvents.onLevelWasLoadedGUIReady.Add(AppLauncherDestroyed);
        }

        #endregion

        private void OnClick()
        {
            Settings.visible = !Settings.visible;

            if (Settings.visible)
            {
                Set(true);
            }
            else
            {
                Set(false);
            }
        }

        private void AppLauncherShow()
        {
            Settings.tempInvisible = false;
        }
        private void AppLancherHide() 
        {
            Settings.tempInvisible = true;
        }

        private void AppLauncherReady() { if (!Enabled) return; Init(); }
        private void AppLauncherDestroyed(GameScenes gameScene) { if (HighLogic.LoadedSceneIsGame) return; Destroy(); }
        private void AppLauncherDestroyed() { Destroy(); }

        private void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(AppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(AppLauncherDestroyed);
            GameEvents.onLevelWasLoadedGUIReady.Remove(AppLauncherDestroyed);
        }

        private void Init()
        {
            if (!isAvailable || !HighLogic.LoadedSceneIsGame) return;
            
            if (appLauncherButton == null)
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(OnClick, OnClick, null, null, null, null, AppScenes, buttonTexture);

            if (Settings.visible)
            {
                Set(true);
            }
            else
            {
                Set(false);
            }

            ApplicationLauncher.Instance.RemoveOnHideCallback(AppLancherHide);
            ApplicationLauncher.Instance.RemoveOnShowCallback(AppLauncherShow);

            ApplicationLauncher.Instance.AddOnShowCallback(AppLauncherShow);
            ApplicationLauncher.Instance.AddOnHideCallback(AppLancherHide);
        }

        private void Destroy()
        {
            if (appLauncherButton != null)
            {
                ApplicationLauncher.Instance.RemoveOnHideCallback(AppLancherHide);
                ApplicationLauncher.Instance.RemoveOnShowCallback(AppLauncherShow);
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
                ApplicationLauncher.Instance.RemoveApplication(appLauncherButton);
                appLauncherButton = null;
            }
        }

        internal void Set(bool SetTrue, bool force = false)
        {
            if (!isAvailable)
            {
                return;
            }
            if (appLauncherButton != null)
            {
                if (SetTrue)
                {
                    if (appLauncherButton.State == RUIToggleButton.ButtonState.FALSE)
                    {
                        appLauncherButton.SetTrue(force);
                    }
                }
                else
                {
                    if (appLauncherButton.State == RUIToggleButton.ButtonState.TRUE)
                    {
                        appLauncherButton.SetFalse(force);
                    }
                }
            }
        }

        internal void Reset()
        {
            if (appLauncherButton != null)
            {
                Set(false);
                if (!Enabled)
                {
                    Destroy();
                }
            }
            if (Enabled)
            {
                Init();
            }
        }
    }
}
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
using KSP.UI.Screens;
using KSP.UI.Dialogs;

using TimeControl.BlizzyToolbar;

namespace TimeControl
{
    /// <summary>
    /// Main Addon Class. Sets up the TimeController, TCWindow, and TCKeyboardControls and sends events along to them
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class TimeControl : MonoBehaviour
    {
        #region Monobehavior
        private void Awake()
        {
            UnityEngine.Object.DontDestroyOnLoad(this); //Don't go away on scene changes

            TCResources.loadGUIAssets();
        }

        private void Start()
        {
        }

        private void Update()
        {

        }
        #endregion
    }
}
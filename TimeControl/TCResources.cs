using System;
using UnityEngine;
using KSP;

namespace TimeControl
{
    internal static class TCResources
    {
        internal static Texture2D stockIcon;
        internal static Texture2D blizzyIcon;

        internal static Texture2D stockIconDisabled;
        internal static Texture2D blizzyIconDisabled;

        //TODO implement animation on toolbar icon to indicate current warp type
        internal static Texture2D stockIconHyperWarpEffect100;
        internal static Texture2D stockIconHyperWarpEffect080;
        internal static Texture2D stockIconHyperWarpEffect060;
        internal static Texture2D stockIconHyperWarpEffect040;
        internal static Texture2D stockIconHyperWarpEffect020;
        internal static Texture2D stockIconHyperWarpEffect000;
        internal static Texture2D stockIconRailsWarpEffect100;
        internal static Texture2D stockIconRailsWarpEffect080;
        internal static Texture2D stockIconRailsWarpEffect060;
        internal static Texture2D stockIconRailsWarpEffect040;
        internal static Texture2D stockIconRailsWarpEffect020;
        internal static Texture2D stockIconRailsWarpEffect000;
        internal static Texture2D stockIconSlowMoWarpEffect100;
        internal static Texture2D stockIconSlowMoWarpEffect080;
        internal static Texture2D stockIconSlowMoWarpEffect060;
        internal static Texture2D stockIconSlowMoWarpEffect040;
        internal static Texture2D stockIconSlowMoWarpEffect020;
        internal static Texture2D stockIconSlowMoWarpEffect000;


        internal static Texture2D blizzyIconHyperWarpEffect100;
        internal static Texture2D blizzyIconHyperWarpEffect080;
        internal static Texture2D blizzyIconHyperWarpEffect060;
        internal static Texture2D blizzyIconHyperWarpEffect040;
        internal static Texture2D blizzyIconHyperWarpEffect020;
        internal static Texture2D blizzyIconHyperWarpEffect000;
        internal static Texture2D blizzyIconRailsWarpEffect100;
        internal static Texture2D blizzyIconRailsWarpEffect080;
        internal static Texture2D blizzyIconRailsWarpEffect060;
        internal static Texture2D blizzyIconRailsWarpEffect040;
        internal static Texture2D blizzyIconRailsWarpEffect020;
        internal static Texture2D blizzyIconRailsWarpEffect000;
        internal static Texture2D blizzyIconSlowMoWarpEffect100;
        internal static Texture2D blizzyIconSlowMoWarpEffect080;
        internal static Texture2D blizzyIconSlowMoWarpEffect060;
        internal static Texture2D blizzyIconSlowMoWarpEffect040;
        internal static Texture2D blizzyIconSlowMoWarpEffect020;
        internal static Texture2D blizzyIconSlowMoWarpEffect000;

        internal static void loadGUIAssets()
        {
            Log.Write( "Loading GUI Assets Started", "TCResources.loadGUIAssets", LogSeverity.Info );

            stockIcon = GameDatabase.Instance.GetTexture( PluginUtilities.GameDatabasePathStockToolbarIcons + "/enabled" , false );
            blizzyIcon = GameDatabase.Instance.GetTexture( PluginUtilities.GameDatabasePathBlizzyToolbarIcons + "/enabled", false );

            Log.Write( "Loading GUI Assets Complete", "TCResources.loadGUIAssets", LogSeverity.Info );
        }

        /// <summary>
        /// Shamelessly borrowed this feature from Kerbal Alarm Clock
        /// There is probably a better way with a PackedSprite but as I don't know how, this will have to do.
        /// TODO actually use this
        /// </summary>
        internal static Texture2D GetHyperWarpIcon(Boolean AppLauncherVersion = false)
        {
            Texture2D textureReturn;

            Double intHundredth = Math.Truncate( DateTime.Now.Millisecond / 100d );
            switch (Convert.ToInt64( intHundredth ))
            {
                case 0:
                    textureReturn = AppLauncherVersion ? TCResources.stockIconHyperWarpEffect100 : TCResources.blizzyIconHyperWarpEffect100;
                    break;
                case 1:
                case 9:
                    textureReturn = AppLauncherVersion ? TCResources.stockIconHyperWarpEffect080 : TCResources.blizzyIconHyperWarpEffect080;
                    break;
                case 2:
                case 8:
                    textureReturn = AppLauncherVersion ? TCResources.stockIconHyperWarpEffect060 : TCResources.blizzyIconHyperWarpEffect060;
                    break;
                case 3:
                case 7:
                    textureReturn = AppLauncherVersion ? TCResources.stockIconHyperWarpEffect040 : TCResources.blizzyIconHyperWarpEffect040;
                    break;
                case 4:
                case 6:
                    textureReturn = AppLauncherVersion ? TCResources.stockIconHyperWarpEffect020 : TCResources.blizzyIconHyperWarpEffect020;
                    break;
                case 5:
                    textureReturn = AppLauncherVersion ? TCResources.stockIconHyperWarpEffect000 : TCResources.blizzyIconHyperWarpEffect000;
                    break;
                default:
                    textureReturn = AppLauncherVersion ? TCResources.stockIconHyperWarpEffect100 : TCResources.blizzyIconHyperWarpEffect100;
                    break;
            }
            return textureReturn;
        }



    }
}
using System;
using System.Reflection;
using UnityEngine;


namespace TimeControl
{
    static class PluginUtilities
    {
        internal readonly static string VERSION =
            System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Major + "."
            + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor + "."
            + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Build + "."
            + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Revision;
        internal readonly static string MOD = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

        internal static readonly string PathApp = KSPUtil.ApplicationRootPath.Replace( "\\", "/" );
        internal static readonly string PathPlugin = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location ).Replace( "\\", "/" );
        internal static readonly string PathPluginData = string.Format( "{0}/PluginData", PathPlugin );
        //internal static readonly string PathTextures = string.Format( "{0}/Textures", PathPlugin );

        //internal static readonly string GameDatabasePathTextures = string.Format( "{0}/Textures", MOD );
        internal static readonly string GameDatabasePathStockToolbarIcons = string.Format( "{0}/ToolbarIcons/StockToolbarIcons", MOD );
        internal static readonly string GameDatabasePathBlizzyToolbarIcons = string.Format( "{0}/ToolbarIcons/BlizzyToolbarIcons", MOD );
        
        internal static readonly string settingsFilePath = string.Format( "{0}/settings.cfg", PathPluginData );


        //internal static readonly string PathPluginSounds = string.Format( "{0}/Sounds", PathPlugin );

        static internal float convertToExponential(float a) //1-64 exponential curve
        {
            return Mathf.Clamp( Mathf.Floor( Mathf.Pow( 64, a ) ), 1, 64 );            
        }

        static int parseSTOI(string a) //Parses a string to an int with standard limitations
        {
            int num;
            if (!Int32.TryParse( a, out num ))
            {
                return 0;
            }
            else
            {
                return Int32.Parse( a,
                                      System.Globalization.NumberStyles.AllowExponent
                                    | System.Globalization.NumberStyles.AllowLeadingWhite
                                    | System.Globalization.NumberStyles.AllowTrailingWhite
                                    | System.Globalization.NumberStyles.AllowThousands
                                    );
            }
        }
    }
}

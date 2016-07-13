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
        internal static readonly string PathTextures = string.Format( "{0}/Textures", PathPlugin );
        internal static readonly string PathStockToolbarIcons = string.Format( "{0}/ToolbarIcons/StockToolbarIcons", MOD );
        internal static readonly string PathBlizzyToolbarIcons = string.Format( "{0}/ToolbarIcons/BlizzyToolbarIcons", MOD );
        
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

        /// <summary>
        /// Loads a texture from the file system directly. Borrowed from Kerbal Alarm Clock (MIT Licensed)
        /// </summary>
        /// <param name="tex">Unity Texture to Load</param>
        /// <param name="FileName">Image file name</param>
        /// <param name="FolderPath">Optional folder path of image</param>
        /// <returns></returns>
        public static Boolean LoadImageFromFile(ref Texture2D tex, String FileName, String FolderPath = "")
        {
            //DebugLogFormatted("{0},{1}",FileName, FolderPath);
            Boolean blnReturn = false;
            try
            {
                if (FolderPath == "") FolderPath = PathTextures;

                //File Exists check
                if (System.IO.File.Exists( String.Format( "{0}/{1}", FolderPath, FileName ) ))
                {
                    try
                    {
                        //MonoBehaviourExtended.LogFormatted_DebugOnly( "Loading: {0}", String.Format( "{0}/{1}", FolderPath, FileName ) );
                        tex.LoadImage( System.IO.File.ReadAllBytes( String.Format( "{0}/{1}", FolderPath, FileName ) ) );
                        blnReturn = true;
                    }
                    catch (Exception ex)
                    {
                        //MonoBehaviourExtended.LogFormatted( "Failed to load the texture:{0} ({1})", String.Format( "{0}/{1}", FolderPath, FileName ), ex.Message );
                    }
                }
                else
                {
                    //MonoBehaviourExtended.LogFormatted( "Cannot find texture to load:{0}", String.Format( "{0}/{1}", FolderPath, FileName ) );
                }


            }
            catch (Exception ex)
            {
                //MonoBehaviourExtended.LogFormatted( "Failed to load (are you missing a file):{0} ({1})", String.Format( "{0}/{1}", FolderPath, FileName ), ex.Message );
            }
            return blnReturn;
        }


        /*
        static internal float linearInterpolate(float current, float target, float amount)
        {
            if (current > target && Mathf.Abs(current - target) > amount)
            {
                return current - amount;
            }
            else if (current < target && Mathf.Abs(current - target) > amount)
            {
                return current + amount;
            }
            else
            {
                return target;
            }
        }
        */
    }
}

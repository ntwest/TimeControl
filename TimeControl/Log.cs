using System;
using System.Reflection;
using UnityEngine;

namespace TimeControl
{
    public enum LogSeverity { Trace = 1, Info = 2, Warning = 3, Error = 4 }

    static class Log
    {
        internal readonly static string VERSION = Assembly.GetAssembly( typeof( Log ) ).GetName().Version.Major + "." + Assembly.GetAssembly( typeof( Log ) ).GetName().Version.Minor + Assembly.GetAssembly( typeof( Log ) ).GetName().Version.Build;
        internal readonly static string MOD = Assembly.GetAssembly( typeof( Log ) ).GetName().Name;
        private static string logPrefix = MOD + "(" + VERSION + "): ";

        /// <summary>
        /// Show all messages of this level and below (e.g. Error only shows errors, while Info shows Errors, Warnings, and Info)
        /// </summary>
        static public LogSeverity LoggingLevel { get; set; }

        static Log()
        {
            // Start out writing only info, warnings or errors. Can be changed in config file.
            LoggingLevel = LogSeverity.Info;
        }

        static public void Trace(string message, string caller = "")
        {
            Log.Write( message, caller, LogSeverity.Trace );
        }
        static public void Info(string message, string caller = "")
        {
            Log.Write( message, caller, LogSeverity.Info );
        }
        static public void Warning(string message, string caller = "")
        {
            Log.Write( message, caller, LogSeverity.Warning );
        }
        static public void Error(string message, string caller = "")
        {
            Log.Write( message, caller, LogSeverity.Error );
        }

        static public void Write(string message, string caller = "", LogSeverity sev = LogSeverity.Warning)
        {
            // Return if we don't need to write messages for this severity
            if (LoggingLevel > sev)
                return;

            message = string.Format( "[{0}] [{1}]: <{2}> ({3}) - {4}", DateTime.Now, logPrefix, sev, caller, message );
            switch (sev)
            {
                case LogSeverity.Error:
                    UnityEngine.Debug.LogError( message );
                    break;
                case LogSeverity.Warning:
                    UnityEngine.Debug.LogWarning( message );
                    break;
                case LogSeverity.Info:
                case LogSeverity.Trace:
                    UnityEngine.Debug.Log( message );
                    break;
            }
        }
    }
}

using System;
using System.Reflection;
using UnityEngine;

namespace TimeControl
{
    public class TC
    {
        #region Logging

        internal readonly static string VERSION = Assembly.GetAssembly(typeof(TimeControl)).GetName().Version.Major + "." + Assembly.GetAssembly(typeof(TimeControl)).GetName().Version.Minor + Assembly.GetAssembly(typeof(TimeControl)).GetName().Version.Build;
        internal readonly static string MOD = Assembly.GetAssembly(typeof(TimeControl)).GetName().Name;

        private static string logPrefix = MOD + "(" + VERSION + "): ";

        internal static bool logDebugMessages = false;

        internal static void LogDebug(string _string)
        {
            if (logDebugMessages)
            {
                Debug.Log(logPrefix + _string);
            }
        }

        internal static void Log(string _string)
        {
            Debug.Log(logPrefix + _string);
        }

        internal static void Warning(string _string)
        {
            Debug.LogWarning(logPrefix + _string);
        }

        #endregion


        static internal int getPlanetaryID(string s) //ID from name
        {
            return PSystemManager.Instance.localBodies.FindIndex(p => p.name.Equals(s));
        }

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

        static internal float convertToExponential(float a) //1-64 exponential curve
        {
            return Mathf.Clamp(Mathf.Floor(Mathf.Pow(64, a)), 1, 64);
        }
    }
}

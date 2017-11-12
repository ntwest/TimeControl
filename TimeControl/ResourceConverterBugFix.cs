using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using UnityEngine;

/*
/* ntwest - Portions of this code file derived from BetterTimeWarp Continued by linuxgurugamer
https://github.com/linuxgurugamer/BetterTimeWarpContinued/blob/master/BetterTimeWarp/BetterTimeWarp.cs
Author: linuxgurugamer

linuxgurugamer has given me permission to use this code.
*/

namespace TimeControl
{
    [KSPAddon( KSPAddon.Startup.MainMenu, true )]
    internal class ResourceConverterWarpFix : MonoBehaviour
    {
        #region Singleton
        public static bool IsReady { get; private set; } = false;
        private static ResourceConverterWarpFix instance;
        public static ResourceConverterWarpFix Instance { get { return instance; } }
        #endregion

        #region Private Fields
        private List<Part> resourceConverterParts;
        private int lastWarpRateIdx;
        #endregion

        #region MonoBehavior
        private void Awake()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( Awake );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                DontDestroyOnLoad( this );
                instance = this;

                resourceConverterParts = new List<Part>();
                lastWarpRateIdx = 0;
            }
        }
        private void Start()
        {
            const string logBlockName = nameof( TimeController ) + "." + nameof( Start );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                GameEvents.onTimeWarpRateChanged.Add( onTimeWarpRateChanged );
                GameEvents.onPartUnpack.Add( onPartUnpack );

                Log.Info( nameof( ResourceConverterWarpFix ) + " is ready.", logBlockName );
                IsReady = true;
            }
        }

        private void OnDestroy()
        {
            GameEvents.onTimeWarpRateChanged.Remove( onTimeWarpRateChanged );
            GameEvents.onPartUnpack.Remove( onPartUnpack );
        }
        #endregion MonoBehavior

        #region GameEvents

        /// <summary>
        /// When unpacking parts after warp, fix issue with ResourceConverter
        /// </summary>
        /// <param name="p"></param>
        private void onPartUnpack(Part p)
        {
            const string logBlockName = nameof( ResourceConverterWarpFix ) + "." + nameof( onPartUnpack );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (resourceConverterParts.Contains( p ))
                {
                    resourceConverterParts.Remove( p );
                    foreach (PartModule pm in p.Modules)
                    {
                        if (pm is ModuleResourceConverter mrc)
                        {
                            CorrectLastUpdateTime( mrc );
                        }
                    }
                }
            }
        }


        /// <summary>
        /// When changing warp rate, fix issue with ResourceConverter
        /// Bug Fix for resource converter provided by linuxgurugamer
        /// </summary>
        private void onTimeWarpRateChanged()
        {
            const string logBlockName = nameof( ResourceConverterWarpFix ) + "." + nameof( onTimeWarpRateChanged );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                if (TimeWarp.fetch != null)
                {
                    if (Log.LoggingLevel == LogSeverity.Trace)
                    {
                        Log.Trace( "TimeWarp.fetch.current_rate_index: ".MemoizedConcat( TimeWarp.fetch.current_rate_index.MemoizedToString() ), logBlockName );
                        Log.Trace( "TimeWarp.CurrentRate: ".MemoizedConcat( TimeWarp.CurrentRate.MemoizedToString() ), logBlockName );
                        Log.Trace( "lastWarpRateIdx: ".MemoizedConcat( lastWarpRateIdx.MemoizedToString() ), logBlockName );
                    }

                    if (lastWarpRateIdx > 0 && TimeWarp.CurrentRate > 1)
                    {
                        foreach (var v in FlightGlobals.fetch.vesselsLoaded)
                        {
                            foreach (var p in v.Parts)
                            {
                                foreach (PartModule pm in p.Modules)
                                {
                                    if (pm is ModuleResourceConverter mrc)
                                    {
                                        if (Log.LoggingLevel == LogSeverity.Trace)
                                        {
                                            Log.Trace( "Found ModuleResourceConverter" );
                                            Log.Trace( "moduleName = ".MemoizedConcat( mrc.moduleName ), logBlockName );
                                            Log.Trace( "IsActivated= ".MemoizedConcat( mrc.IsActivated.MemoizedToString() ), logBlockName );
                                        }

                                        if (!mrc.IsActivated)
                                        {
                                            if (!resourceConverterParts.Contains( p ))
                                                resourceConverterParts.Add( p );
                                        }
                                        else
                                        {
                                            if (resourceConverterParts.Contains( p ))
                                            {
                                                resourceConverterParts.Remove( p );
                                                CorrectLastUpdateTime( mrc );
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    lastWarpRateIdx = TimeWarp.fetch.current_rate_index;
                }
            }
        }
        #endregion GameEvents

        #region Private Methods
        private void CorrectLastUpdateTime(ModuleResourceConverter mrc)
        {
            const string logBlockName = nameof( ResourceConverterWarpFix ) + "." + nameof( CorrectLastUpdateTime );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                FieldInfo fi = mrc.GetType().GetField( "lastUpdateTime", BindingFlags.NonPublic | BindingFlags.Instance );
                if (fi != null)
                {
                    Log.Info( "Updating lastUpdateTime" );
                    fi.SetValue( mrc, Planetarium.GetUniversalTime() );
                }
                else
                {
                    Log.Error( "Unable to get pointer to lastUpdateTime" );
                }
            }
        }
        #endregion Private Methods
    }
}
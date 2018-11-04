using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

namespace ResourceConverterWarpFix
{
    [KSPAddon( KSPAddon.Startup.Instantly, true )]
    internal class ResourceConverterWarpFix : MonoBehaviour
    {
        #region Singleton
        public static ResourceConverterWarpFix Instance { get; private set; }
        public static bool IsReady { get; private set; } = false;        
        #endregion

        #region Private Fields
        private List<Part> resourceConverterParts;
        private int lastWarpRateIdx;
        #endregion

        #region MonoBehavior
        private void Awake()
        {
            DontDestroyOnLoad( this );
            Instance = this;

            resourceConverterParts = new List<Part>();
            lastWarpRateIdx = 0;
        }
        private void Start()
        {
            GameEvents.onTimeWarpRateChanged.Add( onTimeWarpRateChanged );
            GameEvents.onPartUnpack.Add( onPartUnpack );
            IsReady = true;
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

        /// <summary>
        /// When changing warp rate, correct the last update time if the converter is active
        ///   (or add the part to the list of parts to be corrected when warp slows)
        /// </summary>
        private void onTimeWarpRateChanged()
        {
            if (TimeWarp.fetch != null)
            {
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
                                    if (!mrc.IsActivated)
                                    {
                                        if (!resourceConverterParts.Contains( p ))
                                        {
                                            resourceConverterParts.Add( p );
                                        }
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
        #endregion GameEvents

        #region Private Methods
        private void CorrectLastUpdateTime(ModuleResourceConverter mrc)
        {
            if (mrc is null)
            {
                throw new ArgumentNullException( nameof( mrc ) );
            }

            FieldInfo fi = mrc.GetType().GetField( "lastUpdateTime", BindingFlags.NonPublic | BindingFlags.Instance );
            if (fi != null)
            {
                fi.SetValue( mrc, Planetarium.GetUniversalTime() );
            }
            else
            {
                throw new InvalidOperationException( "Unable to get a lastUpdateTime field on module " + mrc.moduleName );
            }
        }
        #endregion Private Methods
    }
}

/*
All code in this file Copyright(c) 2016 Nate West

Portions of the logic (but not the code) in this file is derived from code in BetterTimeWarp Continued by linuxgurugamer
https://github.com/linuxgurugamer/BetterTimeWarpContinued/blob/master/BetterTimeWarp/BetterTimeWarp.cs
linuxgurugamer has provided permission for me to use this code.

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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TimeControl.KeyBindings
{
    public static class TimeControlKeyBindingFactory
    {
        public static TimeControlKeyBinding LoadFromConfigNode(ConfigNode cn)
        {
            const string logBlockName = nameof( TimeControlKeyBindingFactory ) + "." + nameof( LoadFromConfigNode );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                Log.Trace( "Loading Key Binding from config node " + cn.ToString(), logBlockName );

                string s = cn.GetValue( "Action" );
                string s_tcka = Enum.GetNames( typeof( TimeControlKeyAction ) ).ToList().Where( e => e == s ).FirstOrDefault();
                TimeControlKeyAction tcka;
                if (Enum.IsDefined( typeof( TimeControlKeyAction ), s_tcka ))
                {
                    tcka = (TimeControlKeyAction)Enum.Parse( typeof( TimeControlKeyAction ), s_tcka );
                }
                else
                {
                    Log.Error( "Action " + s_tcka + " not found. Failing.", logBlockName );
                    return null;
                }

                string keycombo = cn.GetValue( "KeyCombination" );
                List<KeyCode> iekc = GetKeyCombinationFromString( keycombo ) ?? new List<KeyCode>();
                if (iekc == null)
                {
                    Log.Error( "Key combination ( " + keycombo + " ) is not defined correctly for action ( " + s_tcka + " ). Will use [None].", logBlockName );
                }

                bool assignedID = cn.TryAssignFromConfigInt( "ID", out int id );
                if (!assignedID)
                {
                    Log.Error( "Key has a bad value ID assigned in config. Ignoring Key Definition.", logBlockName );
                    return null;
                }

                bool assignedIsUserDefined = cn.TryAssignFromConfigBool( "IsUserDefined", out bool isUserDefined );
                if (!assignedIsUserDefined)
                {
                    Log.Error( "Key has a bad value IsUserDefined assigned in config. Ignoring Key Definition.", logBlockName );
                    return null;
                }

                bool hasV = cn.HasValue( "V" );
                bool assignedV = cn.TryAssignFromConfigFloat( "V", out float v );
                if (hasV && !assignedV)
                {
                    Log.Error( "Key has a bad value 'V' assigned in config. Ignoring Key Definition.", logBlockName );
                    return null;
                }

                bool hasVesselOrbitLocation = cn.HasValue( "VesselOrbitLocation" );
                bool assignedVesselOrbitLocation = cn.TryAssignFromConfigEnum( "VesselOrbitLocation", out WarpToVesselOrbitLocation.VesselOrbitLocation vol );
                if (hasVesselOrbitLocation && !assignedVesselOrbitLocation)
                {
                    Log.Error( "Key has a bad value 'VesselOrbitLocation' assigned in config. Ignoring Key Definition.", logBlockName );
                    return null;
                }

                bool hasTimeIncrement = cn.HasValue( "TI" );
                bool assignedTimeIncrement = cn.TryAssignFromConfigEnum( "TI", out WarpForNTimeIncrements.TimeIncrement ti );
                if (hasTimeIncrement && !assignedTimeIncrement)
                {
                    Log.Error( "Key has a bad value 'TI' assigned in config. Ignoring Key Definition.", logBlockName );
                    return null;
                }

                TimeControlKeyBinding tckb = GetFromAction( tcka );
                if (tckb is null)
                {
                    return null;
                }

                tckb.KeyCombination = iekc;
                tckb.ID = id;
                tckb.IsUserDefined = isUserDefined;

                if (tckb is TimeControlKeyBindingValue tckbv)
                {
                    tckbv.V = v;
                }
                if (tckb is WarpToVesselOrbitLocation wtvol)
                {
                    wtvol.VesselLocation = vol;
                }
                if (tckb is WarpForNTimeIncrements wfnti)
                {
                    wfnti.TI = ti;
                }


                return tckb;
            }
        }

        private static TimeControlKeyBinding GetFromAction(TimeControlKeyAction tcka)
        {
            const string logBlockName = nameof( TimeControlKeyBindingFactory ) + "." + nameof( GetFromAction );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                switch (tcka)
                {
                    case TimeControlKeyAction.GUIToggle:
                        return new GUIToggle();
                    case TimeControlKeyAction.Realtime:
                        return new Realtime();
                    case TimeControlKeyAction.PauseToggle:
                        return new PauseToggle();
                    case TimeControlKeyAction.TimeStep:
                        return new TimeStep();
                    case TimeControlKeyAction.HyperToggle:
                        return new HyperToggle();
                    case TimeControlKeyAction.SlowMoToggle:
                        return new SlowMoToggle();
                    case TimeControlKeyAction.WarpToNextKACAlarm:
                        return new WarpToNextKACAlarm();
                    case TimeControlKeyAction.HyperActivate:
                        return new HyperActivate();
                    case TimeControlKeyAction.HyperDeactivate:
                        return new HyperDeactivate();
                    case TimeControlKeyAction.SlowMoActivate:
                        return new SlowMoActivate();
                    case TimeControlKeyAction.SlowMoDeactivate:
                        return new SlowMoDeactivate();
                    case TimeControlKeyAction.HyperRateSetRate:
                        return new HyperRateSetRate();
                    case TimeControlKeyAction.HyperRateSlowDown:
                        return new HyperRateSlowDown();
                    case TimeControlKeyAction.HyperRateSpeedUp:
                        return new HyperRateSpeedUp();
                    case TimeControlKeyAction.HyperPhysicsAccuracySet:
                        return new HyperPhysicsAccuracySet();
                    case TimeControlKeyAction.HyperPhysicsAccuracyDown:
                        return new HyperPhysicsAccuracyDown();
                    case TimeControlKeyAction.HyperPhysicsAccuracyUp:
                        return new HyperPhysicsAccuracyUp();
                    case TimeControlKeyAction.SlowMoSetRate:
                        return new SlowMoSetRate();
                    case TimeControlKeyAction.SlowMoSlowDown:
                        return new SlowMoSlowDown();
                    case TimeControlKeyAction.SlowMoSpeedUp:
                        return new SlowMoSpeedUp();
                    case TimeControlKeyAction.WarpForNOrbits:
                        return new WarpForNOrbits();
                    case TimeControlKeyAction.WarpForNTimeIncrements:
                        return new WarpForNTimeIncrements();
                    case TimeControlKeyAction.WarpToVesselOrbitLocation:
                        return new WarpToVesselOrbitLocation();
                }
                Log.Error( "Key action " + tcka.ToString() + " not mapped to internal action. Please report this error to the developer!", logBlockName );
                return null;
            }
        }

        private static List<KeyCode> GetKeyCombinationFromString(string s)
        {
            const string logBlockName = nameof( TimeControlKeyBindingFactory ) + "." + nameof( GetKeyCombinationFromString );
            using (EntryExitLogger.EntryExitLog( logBlockName, EntryExitLoggerOptions.All ))
            {
                string parse = s.Trim();

                Log.Trace( "Parsing string ".MemoizedConcat( parse ), logBlockName );

                // Must start with [ and end with ]
                if (parse[0] != '[' || parse[parse.Length - 1] != ']')
                {
                    Log.Error( "Key Code must be surrounded by [ ]. String Value = ".MemoizedConcat( s ), logBlockName );
                    return null;
                }

                // Strip start and end characters
                parse = parse.Substring( 1, parse.Length - 2 );

                IEnumerable<string> keys;
                if (parse.Contains( "][" ))
                {
                    // Split On ][
                    keys = parse.Split( new string[] { "][" }, StringSplitOptions.None ).Select( x => x.Trim() );
                }
                else
                {
                    keys = new List<string>() { parse };
                }

                List<KeyCode> lkc = new List<KeyCode>();

                foreach (string key in keys)
                {
                    Log.Trace( "Parsing key ".MemoizedConcat( key ), logBlockName );

                    if (key == "None")
                    {
                        Log.Trace( "'None' key found, not adding to key list", logBlockName );
                        continue;
                    }
                    if (key == "Ctrl")
                    {
                        lkc.Add( KeyCode.LeftControl );
                        Log.Trace( "Added LeftControl to key list", logBlockName );
                        continue;
                    }
                    if (key == "Alt")
                    {
                        lkc.Add( KeyCode.LeftAlt );
                        Log.Trace( "Added LeftAlt to key list", logBlockName );
                        continue;
                    }
                    if (key == "Cmd")
                    {
                        lkc.Add( KeyCode.LeftCommand );
                        Log.Trace( "Added LeftCommand to key list", logBlockName );
                        continue;
                    }
                    if (key == "Shift")
                    {
                        lkc.Add( KeyCode.LeftShift );
                        Log.Trace( "Added LeftShift to key list", logBlockName );
                        continue;
                    }

                    if (!Enum.IsDefined( typeof( KeyCode ), key ))
                    {
                        Log.Error( "Key ".MemoizedConcat( key ).MemoizedConcat( " not found! String Value = ".MemoizedConcat( s ) ), logBlockName );
                        return null;
                    }

                    KeyCode parsedKeyCode = (KeyCode)Enum.Parse( typeof( KeyCode ), key );
                    lkc.Add( parsedKeyCode );
                    Log.Trace( "Added ".MemoizedConcat( parsedKeyCode.ToString() ).MemoizedConcat( " to key list" ), logBlockName );
                }

                return lkc;
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

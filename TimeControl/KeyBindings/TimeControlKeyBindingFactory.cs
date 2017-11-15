using System;
using System.Collections;
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
                bool assignedTimeIncrement = cn.TryAssignFromConfigEnum( "TI", out WarpForNTimeIncrements.TimeIncrement ti);
                if (hasTimeIncrement && !assignedTimeIncrement)
                {
                    Log.Error( "Key has a bad value 'TI' assigned in config. Ignoring Key Definition.", logBlockName );
                    return null;
                }

                switch (tcka)
                {
                    case TimeControlKeyAction.GUIToggle:
                        return new GUIToggle() { KeyCombination = iekc };
                    case TimeControlKeyAction.Realtime:
                        return new Realtime() { KeyCombination = iekc };
                    case TimeControlKeyAction.PauseToggle:
                        return new PauseToggle() { KeyCombination = iekc };
                    case TimeControlKeyAction.TimeStep:
                        return new TimeStep() { KeyCombination = iekc };
                    case TimeControlKeyAction.HyperToggle:
                        return new HyperToggle() { KeyCombination = iekc };
                    case TimeControlKeyAction.SlowMoToggle:
                        return new SlowMoToggle() { KeyCombination = iekc };
                    case TimeControlKeyAction.WarpToNextKACAlarm:
                        return new WarpToNextKACAlarm() { KeyCombination = iekc };
                    case TimeControlKeyAction.HyperActivate:
                        return new HyperActivate() { KeyCombination = iekc };
                    case TimeControlKeyAction.HyperDeactivate:
                        return new HyperDeactivate() { KeyCombination = iekc };
                    case TimeControlKeyAction.SlowMoActivate:
                        return new SlowMoActivate() { KeyCombination = iekc };
                    case TimeControlKeyAction.SlowMoDeactivate:
                        return new SlowMoDeactivate() { KeyCombination = iekc };
                    case TimeControlKeyAction.HyperRateSetRate:
                        return new HyperRateSetRate() { KeyCombination = iekc, V = v };
                    case TimeControlKeyAction.HyperRateSlowDown:
                        return new HyperRateSlowDown() { KeyCombination = iekc, V = v };
                    case TimeControlKeyAction.HyperRateSpeedUp:
                        return new HyperRateSpeedUp() { KeyCombination = iekc, V = v };
                    case TimeControlKeyAction.HyperPhysicsAccuracySet:
                        return new HyperPhysicsAccuracySet() { KeyCombination = iekc, V = v };
                    case TimeControlKeyAction.HyperPhysicsAccuracyDown:
                        return new HyperPhysicsAccuracyDown() { KeyCombination = iekc, V = v };
                    case TimeControlKeyAction.HyperPhysicsAccuracyUp:
                        return new HyperPhysicsAccuracyUp() { KeyCombination = iekc, V = v };
                    case TimeControlKeyAction.SlowMoSetRate:
                        return new SlowMoSetRate() { KeyCombination = iekc, V = v };
                    case TimeControlKeyAction.SlowMoSlowDown:
                        return new SlowMoSlowDown() { KeyCombination = iekc, V = v };
                    case TimeControlKeyAction.SlowMoSpeedUp:
                        return new SlowMoSpeedUp() { KeyCombination = iekc, V = v };
                    case TimeControlKeyAction.WarpForNOrbits:
                        return new WarpForNOrbits() { KeyCombination = iekc, V = v };
                    case TimeControlKeyAction.WarpForNTimeIncrements:
                        return new WarpForNTimeIncrements( ti ) { KeyCombination = iekc, V = v };
                    case TimeControlKeyAction.WarpToVesselOrbitLocation:
                        return new WarpToVesselOrbitLocation( vol ) { KeyCombination = iekc, V = v };
                }

                Log.Error( "Key action " + tcka.ToString() + " not mapped to internal action. Please report this error to the developer!", logBlockName );
                return null;
            }
        }

        private static List<KeyCode> GetKeyCombinationFromString(string s)
        {
            const string logBlockName = nameof( TimeControlKeyBinding ) + "." + nameof( GetKeyCombinationFromString );
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
                if (s.Contains( "][" ))
                {
                    // Split On ][
                    keys = s.Split( new string[] { "][" }, StringSplitOptions.None ).Select( x => x.Trim() );
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
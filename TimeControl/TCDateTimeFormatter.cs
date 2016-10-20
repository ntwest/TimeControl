using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP;

namespace TimeControl
{
    public class TCDateTimeFormatter : IDateTimeFormatter
    {
        /// <summary>
        /// Orbital period of home body.
        /// </summary>
        public int KerbinYear {
            get {
                int yearLength = 9203400;
                int homeBodyOrbitalPeriod = Convert.ToInt32( FlightGlobals.GetHomeBody().orbit.period );
                if (homeBodyOrbitalPeriod != 0)
                    yearLength = homeBodyOrbitalPeriod;
                return yearLength;
            }
        }

        /// <summary>
        /// Orbital Rotation in Seconds (sidereal day)
        /// </summary>
        public int KerbinDay {
            get {
                int dayLength = 21600;
                int homeBodyRotationPeriod = Convert.ToInt32( FlightGlobals.GetHomeBody().rotationPeriod );
                if (homeBodyRotationPeriod != 0)
                    dayLength = homeBodyRotationPeriod;
                return dayLength;
            }
        }

        public int EarthDay {
            get {
                return (86400);
            }
        }
        public int EarthYear {
            get {
                return (31536000);
            }
        }

        public int Day {
            get {
                if (GameSettings.KERBIN_TIME)
                    return KerbinDay;
                else
                    return EarthDay;
            }
        }
        public int Year {
            get {
                if (GameSettings.KERBIN_TIME)
                    return KerbinYear;
                else
                    return EarthYear;
            }
        }

        public int Minute {
            get {
                return 60;
            }
        }

        public int Hour {
            get {
                return 3600;
            }
        }

        /// <summary>
        /// Returns an array that appears to be Second, Minute, Hour, Day, Year
        /// </summary>
        public int[] GetDateFromUT(double time)
        {
            if (GameSettings.KERBIN_TIME)
                return GetKerbinDateFromUT( time );
            else
                return GetEarthDateFromUT( time );
        }
        /// <summary>
        /// Returns an array that appears to be Second, Minute, Hour, Day, Year
        /// </summary>
        public int[] GetEarthDateFromUT(double time)
        {
            long t = Convert.ToInt64( time );

            try
            {
                // Current Year
                int year = (int)(t / EarthYear);
                t = t - (year * EarthYear);

                // Current Day
                int day = (int)(t / EarthDay);
                // Current Hour of the day = Total seconds * (1 minutes / 60 seconds) * (1 hours / 60 minutes) % ( Day Length in Seconds * (1 minutes / 60 seconds) * (1 hours / 60 minutes) )
                int hour = (int)((t / 3600) % (EarthDay / 3600));
                // Current Minute of the Hour
                int minute = (int)((t / 60) % 60);
                // Current Second
                int second = (int)(t % 60);

                return new int[]
                {
                second,
                minute,
                hour,
                day,
                year
                };
            }
            catch (DivideByZeroException dbze)
            {
                Log.Write( dbze.Message, "GetEarthDateFromUT", LogSeverity.Error );
                Log.Write( dbze.StackTrace, "GetEarthDateFromUT", LogSeverity.Error );
                return new int[] { 0, 0, 0, 0, 0 };
            }
        }

        /// <summary>
        /// Returns an array that appears to be Second, Minute, Hour, Day, Year
        /// </summary>
        public int[] GetKerbinDateFromUT(double time)
        {
            int t = Convert.ToInt32( time );
            try
            {
                // Current Year
                int year = (int)(t / KerbinYear);
                t = t - (year * KerbinYear);

                // Current Day
                int day = (int)(t / KerbinDay);
                // Current Hour of the day = Total seconds * (1 minutes / 60 seconds) * (1 hours / 60 minutes) % ( Day Length in Seconds * (1 minutes / 60 seconds) * (1 hours / 60 minutes) )
                int hour = (int)((t / 3600) % (KerbinDay / 3600));
                // Current Minute of the Hour
                int minute = (int)((t / 60) % 60);
                // Current Second
                int second = (int)(t % 60);

                return new int[]
                {
                second,
                minute,
                hour,
                day,
                year
                };
            }
            catch (DivideByZeroException dbze)
            {
                Log.Write( dbze.Message, "GetKerbinDateFromUT", LogSeverity.Error );
                Log.Write( dbze.StackTrace, "GetKerbinDateFromUT", LogSeverity.Error );
                return new int[] { 0,0,0,0,0 };
            }
        }
        public string PrintDate(double time, bool includeTime, bool includeSeconds = false)
        {
            //Log.Trace( "PrintDate for UT " + time );

            if (IsInvalidTime( time ))
                return InvalidTimeStr( time );

            int[] t = GetDateFromUT( time );

            // Add 1 to Year
            t[4] = t[4] + 1;

            // Add 1 to Day
            t[3] = t[3] + 1;

            string s;
            if (includeTime)
            {
                if (includeSeconds)
                    s = "Year {0}, Day {1} - {2}h, {3}m, {4}s";
                else
                    s = "Year {0}, Day {1} - {2}h, {3}m";
            }
            else
            {
                if (includeSeconds)
                    s = "Year {0}, Day {1}, {2}s";
                else
                    s = "Year {0}, Day {1}";
            }
            return string.Format( s, t[4], t[3], t[2], t[1], t[0] );
        }
        public string PrintDateCompact(double time, bool includeTime, bool includeSeconds = false)
        {
            //Log.Trace( "PrintDateCompact for UT " + time );

            if (IsInvalidTime( time ))
                return InvalidTimeStr( time );

            int[] t = GetDateFromUT( time );

            // Add 1 to Year
            t[4] = t[4] + 1;

            // Add 1 to Day
            t[3] = t[3] + 1;

            string s;
            if (includeTime)
            {
                if (includeSeconds)
                    s = "Y{0}, D{01}, {2}:{3:00}:{4:00}";
                else
                    s = "Y{0}, D{01}, {2}:{3:00}";
            }
            else
            {
                if (includeSeconds)
                    s = "Y{0}, D{01}:{4:00}";
                else
                    s = "Y{0}, D{01}";
            }
            return string.Format(s, t[4], t[3], t[2], t[1], t[0]) ;
        }
        public string PrintDateDelta(double time, bool includeTime, bool includeSeconds, bool useAbs)
        {
            //Log.Trace( "PrintDateDelta for UT " + time );

            if (IsInvalidTime( time ))
                return InvalidTimeStr( time );

            int[] t = GetDateFromUT( time );

            bool y = (t[4] != 0);
            bool d = (t[3] != 0);
            bool h = (t[2] != 0);
            bool m = (t[1] != 0);
            bool hass = (t[0] != 0);

            string s;
            if (includeTime)
            {
                if (includeSeconds)
                {
                    if (y)
                        s = "{0} years, {1} days, {2} hours, {3} minutes, {4} seconds";
                    else if (d)
                        s = "{1} days, {2} hours, {3} minutes, {4} seconds";
                    else if (h)
                        s = "{2} hours, {3} minutes, {4} seconds";
                    else if (m)
                        s = "{3} minutes, {4} seconds";
                    else 
                        s = "{4} seconds";
                }
                else
                {
                    if (y)
                        s = "{0} years, {1} days, {2} hours, {3} minutes";
                    else if (d)
                        s = "{1} days, {2} hours, {3} minutes";
                    else if (h)
                        s = "{2} hours, {3} minutes";
                    else if (m)
                        s = "{3} minutes";
                    else
                        s = "";
                }
            }
            else
            {
                if (y)
                    s = "{0} years, {1} days";
                else if (d)
                    s = "{1} days";
                else
                    s = "";
            }
            return string.Format( s, t[4], t[3], t[2], t[1], t[0] );
        }
        public string PrintDateDeltaCompact(double time, bool includeTime, bool includeSeconds, bool useAbs)
        {
            //Log.Trace( "PrintDateDeltaCompact for UT " + time );

            if (IsInvalidTime( time ))
                return InvalidTimeStr( time );

            int[] t = GetDateFromUT( time );

            bool y = (t[4] != 0);
            bool d = (t[3] != 0);
            bool h = (t[2] != 0);
            bool m = (t[1] != 0);
            bool hass = (t[0] != 0);

            string s;
            if (includeTime)
            {
                if (includeSeconds)
                {
                    if (y)
                        s = "{0}y, {1}d, {2}h, {3}m, {4}s";
                    else if (d)
                        s = "{1}d, {2}h, {3}m, {4}s";
                    else if (h)
                        s = "{2}h, {3}m, {4}s";
                    else if (m)
                        s = "{3}m, {4}s";
                    else
                        s = "{4}s";
                }
                else
                {
                    if (y)
                        s = "{0}y, {1}d, {2}h, {3}m";
                    else if (d)
                        s = "{1}d, {2}h, {3}m";
                    else if (h)
                        s = "{2}h, {3}m";
                    else if (m)
                        s = "{3}m";
                    else
                        s = "";
                }
            }
            else
            {
                if (y)
                    s = "{0}y, {1}d";
                else if (d)
                    s = "{1}d";
                else
                    s = "";
            }
            return string.Format( s, t[4], t[3], t[2], t[1], t[0] );
        }
        public string PrintDateNew(double time, bool includeTime)
        {
            //Log.Trace( "PrintDateNew for UT " + time );

            if (IsInvalidTime( time ))
                return InvalidTimeStr( time );

            int[] t = GetDateFromUT( time );

            // Add 1 to Year
            t[4] = t[4] + 1;

            // Add 1 to Day
            t[3] = t[3] + 1;

            string s;
            if (includeTime)
            {
                s = "Year {0}, Day {1} - {2:00}:{3:00}:{4:00}";
            }
            else
            {
                s = "Year {0}, Day {1}";
            }
            return string.Format( s, t[4], t[3], t[2], t[1], t[0] );
        }
        public string PrintTime(double time, int valuesOfInterest, bool explicitPositive)
        {
            //Log.Trace( "PrintTime for UT " + time );

            if (IsInvalidTime( time ))
                return InvalidTimeStr( time );

            int[] t = GetDateFromUT( time );

            string s = "";
            if (valuesOfInterest >= 5 && t[4] != 0)
                s += "{0}y, ";
            if (   (valuesOfInterest >= 4 && t[3] != 0 )
                || (valuesOfInterest >= 5 && t[4] != 0)
               )
                s += "{1}d, ";
            if ((valuesOfInterest >= 3 && t[2] != 0)
                || (valuesOfInterest >= 4 && t[3] != 0)
                || (valuesOfInterest >= 5 && t[4] != 0)
               )
                s += "{2}h, ";

            else if (valuesOfInterest == 3)
                s = "{0}y, {1}d, {2}h";
            else if (valuesOfInterest == 2)
                s = "{0}y, {1}d";
            else if (valuesOfInterest == 1)
                s = "{0}y";
            else
                s = "";
            if (explicitPositive)
            {
                s = "+ " + s;
            }
            return string.Format( s, t[4], t[3], t[2], t[1], t[0] );
        }
        public string PrintTimeCompact(double time, bool explicitPositive)
        {
            //Log.Trace( "PrintTimeCompact for UT " + time );

            if (IsInvalidTime( time ))
                return InvalidTimeStr( time );

            string s = "{1}:{2:00}:{3:00}:{4:00}";
            if (explicitPositive && time >= 0)
            {
                s = "T+ " + s; 
            }
            if (time < 0)
            {
                s = "T- " + s;
            }

            int[] t = GetDateFromUT( Math.Abs(time) );

            return string.Format( s, t[4], t[3], t[2], t[1], t[0] );
        }
        public string PrintTimeLong(double time)
        {
            //Log.Trace( "PrintTimeLong for UT " + time );

            if (IsInvalidTime( time ))
                return InvalidTimeStr( time );

            int[] t = GetDateFromUT( time );
            string s;
            s = "{0}Years, {1}Days, {2}Hours, {3}Mins, {4}Secs";            
            return string.Format( s, t[4], t[3], t[2], t[1], t[0] );
        }
        public string PrintTimeStamp(double time, bool days = false, bool years = false)
        {
            //Log.Trace( "PrintTimeStamp for UT " + time );

            if (IsInvalidTime( time ))
                return InvalidTimeStr( time );

            int[] t = GetDateFromUT( time );            
            string s;
            if (days)
            {
                if (years)
                {
                    s = "Year {0}, Day {1} - {2:00}:{3:00}:{4:00}";
                }
                else
                {
                    s = "Day {1} - {2:00}:{3:00}:{4:00}";
                }
            }
            else
            {
                if (years)
                {
                    s = "Year {0} - {2:00}:{3:00}:{4:00}";
                }
                else
                {
                    s = "{2:00}:{3:00}:{4:00}";
                }
            }
            return string.Format( s, t[4], t[3], t[2], t[1], t[0] );
        }
        public string PrintTimeStampCompact(double time, bool days = false, bool years = false)
        {
            //Log.Trace( "PrintTimeStampCompact for UT " + time );

            if (IsInvalidTime( time ))
                return InvalidTimeStr( time );

            int[] t = GetDateFromUT( time );
            string s;
            if (days)
            {
                if (years)
                {
                    s = "{0}y, {1}d, {2:00}:{3:00}:{4:00}";
                }
                else
                {
                    s = "{1}d, {2:00}:{3:00}:{4:00}";
                }
            }
            else
            {
                if (years)
                {
                    s = "{0}y, {2:00}:{3:00}:{4:00}";
                }
                else
                {
                    s = "{2:00}:{3:00}:{4:00}";
                }
            }
            return string.Format( s, t[4], t[3], t[2], t[1], t[0] );
        }

        protected bool IsInvalidTime(double time)
        {
            if (double.IsNaN( time ) || double.IsPositiveInfinity( time ) || double.IsNegativeInfinity( time ))
                return true;
            else
                return false;
        }
        protected string InvalidTimeStr(double time)
        {
            if (double.IsNaN( time ))
            {
                return "NaN";
            }
            if (double.IsPositiveInfinity( time ))
            {
                return "+Inf";
            }
            if (double.IsNegativeInfinity( time ))
            {
                return "-Inf";
            }
            return null;
        }        
    }
}

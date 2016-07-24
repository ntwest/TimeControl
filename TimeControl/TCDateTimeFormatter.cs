using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP;

namespace TimeControl
{
    public class TCDateTimeFormatter : KSPUtil.DefaultDateTimeFormatter
    {
        /// <summary>
        /// Length of a Kerbin Year in Seconds? I think. Orbital period of home body.
        /// </summary>
        new public int KerbinYear {
            get {
                return Convert.ToInt32( FlightGlobals.GetHomeBody().orbit.period );
            }
        }

        /// <summary>
        /// Length of a Kerbin Solar Day in seconds?
        /// </summary>
        new public int KerbinDay {
            get {
                return Convert.ToInt32( FlightGlobals.GetHomeBody().solarDayLength );
            }
        }

        new public int EarthDay {
            get {
                return (86400);
            }
        }
        new public int EarthYear {
            get {
                return (31536000);
            }
        }

        new public int Day {
            get {
                if (GameSettings.KERBIN_TIME)
                    return KerbinDay;
                else
                    return EarthDay;
            }
        }
        new public int Year {
            get {
                if (GameSettings.KERBIN_TIME)
                    return KerbinYear;
                else
                    return EarthYear;
            }
        }

        /// <summary>
        /// Returns an array that appears to be Second, Minute, Hour, Day, Year
        /// </summary>
        new public int[] GetDateFromUT(double time)
        {
            if (GameSettings.KERBIN_TIME)
                return GetKerbinDateFromUT( time );
            else
                return GetEarthDateFromUT( time );
        }
        /// <summary>
        /// Returns an array that appears to be Second, Minute, Hour, Day, Year
        /// </summary>
        new public int[] GetEarthDateFromUT(double time)
        {
            int t = Convert.ToInt32( time );

            // Current Year
            int year = t / EarthYear;
            t = t - (year * EarthYear);

            // Current Day
            int day = t / EarthDay;
            t = t - (day * EarthDay);

            // Current Hour of the day = Total seconds * (1 minutes / 60 seconds) * (1 hours / 60 minutes) % ( Day Length in Seconds * (1 minutes / 60 seconds) * (1 hours / 60 minutes) )
            int hour = (t * (1 / 60) * (1 / 60)) % (EarthDay * (1 / 60) * (1 / 60));
            t = t - (hour * 60 ^ 2);

            // Current Minute of the Day
            int minute = (t / 60) % 60;

            // Current Second
            int second = t % 60;

            return new int[]
            {
                second,
                minute,
                hour,
                day,
                year
            };
        }

        /// <summary>
        /// Returns an array that appears to be Second, Minute, Hour, Day, Year
        /// </summary>
        new public int[] GetKerbinDateFromUT(double time)
        {
            int t = Convert.ToInt32( time );

            // Current Year
            int year = t / KerbinYear;
            t = t - (year * KerbinYear);

            // Current Day
            int day = t / KerbinDay;
            t = t - (day * KerbinDay);
            
            // Current Hour of the day = Total seconds * (1 minutes / 60 seconds) * (1 hours / 60 minutes) % ( Day Length in Seconds * (1 minutes / 60 seconds) * (1 hours / 60 minutes) )
            int hour = (t * (1 / 60) * (1 / 60)) % (KerbinDay * (1 / 60) * (1 / 60));
            t = t - (hour * 60 ^ 2);

            // Current Minute of the Day
            int minute = (t / 60) % 60;

            // Current Second
            int second = t % 60;
            
            return new int[]
            {
                second,
                minute,
                hour,
                day,
                year
            };
        }
        new public string PrintDate(double time, bool includeTime, bool includeSeconds = false)
        {
            int[] t = GetDateFromUT( time );

            // Add 1 to Year
            t[4] = t[4] + 1;
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
        new public string PrintDateCompact(double time, bool includeTime, bool includeSeconds = false)
        {
            int[] t = GetDateFromUT( time );

            // Add 1 to Year
            t[4] = t[4] + 1;

            string s;
            if (includeTime)
            {
                if (includeSeconds)
                    s = "Y{0}, D{1}, {2}:{3:00}:{4:00}";
                else
                    s = "Y{0}, D{1}, {2}:{3:00}";
            }
            else
            {
                if (includeSeconds)
                    s = "Y{0}, D{1}:{4:00}";
                else
                    s = "Y{0}, D{1}";
            }
            return string.Format(s, t[4], t[3], t[2], t[1], t[0]) ;
        }
        new public string PrintDateDelta(double time, bool includeTime, bool includeSeconds, bool useAbs)
        {
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
        new public string PrintDateDeltaCompact(double time, bool includeTime, bool includeSeconds, bool useAbs)
        {
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
        new public string PrintDateNew(double time, bool includeTime)
        {
            int[] t = GetDateFromUT( time );

            // Add 1 to Year
            t[4] = t[4] + 1;
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
        new public string PrintTime(double time, int valuesOfInterest, bool explicitPositive)
        {
            int[] t = GetDateFromUT( time );

            string s = "";
            if (valuesOfInterest >= 4)
                s = "{1}d, {2}h, {3}m, {4}s";
            else if (valuesOfInterest == 3)
                s = "{2}h, {3}m, {4}s";
            else if (valuesOfInterest == 2)
                s = "{3}m, {4}s";
            else if (valuesOfInterest <= 1)
                s = "{4}s";            
            if (explicitPositive)
            {
                s = "+ " + s;
            }
            return string.Format( s, t[4], t[3], t[2], t[1], t[0] );
        }
        new public string PrintTimeCompact(double time, bool explicitPositive)
        {
            string s = "{1}{2:00}{3:00{4:00}s";
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
        new public string PrintTimeLong(double time)
        {
            int[] t = GetDateFromUT( time );
            string s;
            s = "{0}Years, {1}Days, {2}Hours, {3}Mins, {4}Secs";            
            return string.Format( s, t[4], t[3], t[2], t[1], t[0] );
        }
        new public string PrintTimeStamp(double time, bool days = false, bool years = false)
        {
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
        new public string PrintTimeStampCompact(double time, bool days = false, bool years = false)
        {
            int[] t = GetDateFromUT( time );
            string s;
            if (days)
            {
                if (years)
                {
                    s = "{0}y, {1}d - {2:00}:{3:00}:{4:00}";
                }
                else
                {
                    s = "{1}d - {2:00}:{3:00}:{4:00}";
                }
            }
            else
            {
                if (years)
                {
                    s = "{0}y - {2:00}:{3:00}:{4:00}";
                }
                else
                {
                    s = "{2:00}:{3:00}:{4:00}";
                }
            }
            return string.Format( s, t[4], t[3], t[2], t[1], t[0] );
        }
    }
}

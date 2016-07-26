using System;
using SC = System.ComponentModel;
using UnityEngine;

namespace TimeControl
{
    public class TCAltitudeLimit
    { 
        private string altitudeLimit;

        public int AltitudeLimitInt {
            get {
                return Int32.Parse( altitudeLimit );
            }
            set {
                altitudeLimit = value.ToString();
            }
        }

        public string AltitudeLimit {
            get {
                return altitudeLimit;
            }
            set {
                if (altitudeLimit != value)
                {
                    // Must be parseable as an integer, otherwise don't modify this value and ignores
                    int num;
                    if (Int32.TryParse( value, out num ))
                    {
                        altitudeLimit = value;
                    }
                }
            }
        }
    }
}

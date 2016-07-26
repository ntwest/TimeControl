using System;
using SC = System.ComponentModel;
using UnityEngine;

namespace TimeControl
{

    public class TCWarpRate
    { 
        private string warpRate;

        public int WarpRateInt {
            get {
                return Int32.Parse( warpRate );
            }
            set {
                warpRate = value.ToString();           
            }
        }

        public string WarpRate {
            get {
                return warpRate;
            }
            set {
                if (warpRate != value)
                {
                    // Must be parseable as an integer, otherwise don't modify this value
                    int num;
                    if (Int32.TryParse( value, out num ))
                    {
                        warpRate = value;
                    }
                }
            }
        }
    }
}

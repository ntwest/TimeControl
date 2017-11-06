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

using System;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TimeControl
{
    internal static class ExtensionMethods
    {
        internal static ManeuverNode FirstUpcomingManuverNode(this Vessel v, double fromUT)
        {
            if (!((v.patchedConicSolver?.maneuverNodes?.Count ?? 0) > 0))
            {
                return null;
            }
            
            var nodes = v.patchedConicSolver.maneuverNodes.Where( f => f.UT >= fromUT );

            if (nodes == null || nodes.Count() == 0)
            {
                return null;
            }

            return nodes.First();
        }

        internal static bool TryAssignFromConfigFloat(this ConfigNode cn, string property, ref float v)
        {
            if (cn.HasValue( property ) && float.TryParse( cn.GetValue( property ), out float cv ))
            {
                v = cv;
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool TryAssignFromConfigBool(this ConfigNode cn, string property, ref bool v)
        {
            if (cn.HasValue( property ) && bool.TryParse( cn.GetValue( property ), out bool cv ))
            {
                v = cv;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

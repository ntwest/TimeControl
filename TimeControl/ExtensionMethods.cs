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

        /// <summary>
        /// Get the Descending Node Vector
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        internal static Vector3d GetDNVector(this Orbit o)
        {
            double angle = (o.LAN + 180) % 360;
            angle = angle < 0 ? angle + 360 : angle;
            Vector3d result = QuaternionD.AngleAxis( angle, Planetarium.Zup.Z ) * Planetarium.Zup.X;
            return result;
        }

        /// <summary>
        /// Compute UT of Equitorial Ascending Node
        /// </summary>
        /// <returns>UT</returns>
        internal static double GetAscendingNodeUT(this Orbit o)
        {
            return o.GetUTforTrueAnomaly( o.GetTrueAnomalyOfZupVector( o.GetANVector() ), 2 );
        }

        /// <summary>
        /// Compute UT of Ascending Node based on a target orbit
        /// </summary>
        /// <param name="to">Target Orbit</param>
        /// <returns>UT</returns>
        internal static double GetAscendingNodeUT(this Orbit o, Orbit to)
        {
            return o.GetUTforTrueAnomaly( o.GetTrueAnomalyOfZupVector( Vector3d.Cross( to.h, o.GetOrbitNormal() ).normalized ), 2 );
        }

        /// <summary>
        /// Compute UT of Equitorial Descending Node
        /// </summary>
        /// <returns>UT</returns>
        internal static double GetDescendingNodeUT(this Orbit o)
        {
            return o.GetUTforTrueAnomaly( o.GetTrueAnomalyOfZupVector( o.GetDNVector() ), 2 );
        }

        /// <summary>
        /// Compute UT of Descending Node based on a target orbit
        /// </summary>
        /// <param name="to">Target Orbit</param>
        /// <returns>UT</returns>
        internal static double GetDescendingNodeUT(this Orbit o, Orbit to)
        {
            return o.GetUTforTrueAnomaly( o.GetTrueAnomalyOfZupVector( Vector3d.Cross( o.GetOrbitNormal(), to.h ).normalized ), 2 );
        }
    }
}

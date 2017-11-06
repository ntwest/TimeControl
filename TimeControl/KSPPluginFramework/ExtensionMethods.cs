/* ntwest - Pulled portions of code from KSPPluginFramework Version 1.2, 
Forum Thread:http://forum.kerbalspaceprogram.com/threads/66503-KSP-Plugin-Framework
Author: TriggerAu, 2014

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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP;
using UnityEngine;

namespace KSPPluginFramework
{
    /// <summary>
    /// CLass containing some useful extension methods for Unity Objects as well as Memoization for string representations of things.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Ensure that the Rect remains within the screen bounds
        /// </summary>
        public static Rect ClampToScreen(this Rect r)
        {
            return r.ClampToScreen( new RectOffset( 0, 0, 0, 0 ) );
        }

        /// <summary>
        /// Ensure that the Rect remains within the screen bounds
        /// </summary>
        /// <param name="ScreenBorder">A Border to the screen bounds that the Rect will be clamped inside (can be negative)</param>
        public static Rect ClampToScreen(this Rect r, RectOffset ScreenBorder)
        {
            r.x = Mathf.Clamp( r.x, ScreenBorder.left, Screen.width - r.width - ScreenBorder.right );
            r.y = Mathf.Clamp( r.y, ScreenBorder.top, Screen.height - r.height - ScreenBorder.bottom );
            return r;
        }

        public static GUIStyle PaddingChange(this GUIStyle g, Int32 PaddingValue)
        {
            GUIStyle gReturn = new GUIStyle( g );
            gReturn.padding = new RectOffset( PaddingValue, PaddingValue, PaddingValue, PaddingValue );
            return gReturn;
        }
        public static GUIStyle PaddingChangeBottom(this GUIStyle g, Int32 PaddingValue)
        {
            GUIStyle gReturn = new GUIStyle( g );
            gReturn.padding.bottom = PaddingValue;
            return gReturn;
        }
    }
}
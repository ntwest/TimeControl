using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TimeControl
{
    internal class FloatComparer : Comparer<float>
    {
        public override int Compare(float x, float y)
        {
            if (Mathf.Approximately( x, y ))
                return 0;
            else
                return x.CompareTo( y );
        }
    }

    internal static class ExtensionMethods
    {
        #region KSPPluginFramework
        /*
        The ClampToScreen Methods Come from the KSPPluginFramework 
        
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

        /// <summary>
        /// Ensure that the Rect remains within the screen bounds
        /// </summary>
        internal static Rect ClampToScreen(this Rect r)
        {
            return r.ClampToScreen( new RectOffset( 0, 0, 0, 0 ) );
        }

        /// <summary>
        /// Ensure that the Rect remains within the screen bounds
        /// </summary>
        /// <param name="ScreenBorder">A Border to the screen bounds that the Rect will be clamped inside (can be negative)</param>
        internal static Rect ClampToScreen(this Rect r, RectOffset ScreenBorder)
        {
            r.x = Mathf.Clamp( r.x, ScreenBorder.left, Screen.width - r.width - ScreenBorder.right );
            r.y = Mathf.Clamp( r.y, ScreenBorder.top, Screen.height - r.height - ScreenBorder.bottom );
            return r;
        }
        #endregion


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

        internal static bool TryAssignFromConfigInt(this ConfigNode cn, string property, out int v)
        {
            if (cn.HasValue( property ) && int.TryParse( cn.GetValue( property ), out int cv ))
            {
                v = cv;
                return true;
            }
            else
            {
                v = default( int );
                return false;
            }
        }

        internal static bool TryAssignFromConfigFloat(this ConfigNode cn, string property, out float v)
        {
            if (cn.HasValue( property ) && float.TryParse( cn.GetValue( property ), out float cv ))
            {
                v = cv;
                return true;
            }
            else
            {
                v = default( float );
                return false;
            }
        }

        internal static bool TryAssignFromConfigBool(this ConfigNode cn, string property, out bool v)
        {
            if (cn.HasValue( property ) && bool.TryParse( cn.GetValue( property ), out bool cv ))
            {
                v = cv;
                return true;
            }
            else
            {
                v = default( bool );
                return false;
            }
        }

        internal static bool TryAssignFromConfigEnum<T>(this ConfigNode cn, string property, out T e)
        {
            if (cn.HasValue( property ))
            {
                Type eT = typeof( T );
                string ll = cn.GetValue( property );
                if (Enum.IsDefined( eT, ll ))
                {
                    e = (T)Enum.Parse( eT, ll );
                    return true;
                }
                else
                {
                    e = default( T );
                    return false;
                }
            }
            else
            {
                e = default( T );
                return false;
            }
        }

        private static Func<object, string> memoizedToStringFunc1 = 
            ((Func<object, string>)( (o) => { return o.ToString(); } )).Memoize();

        private static Func<object, IFormatProvider, string> memoizedToStringFunc2 =
            ((Func<object, IFormatProvider, string>)((o, p) => {
                Type t = o.GetType();
                if (t == typeof ( float )) return ((float)o).ToString( p );
                else if (t == typeof( double )) return ((double)o).ToString( p );
                else if (t == typeof( decimal )) return ((decimal)o).ToString( p );
                else if (t == typeof( sbyte )) return ((sbyte)o).ToString( p );
                else if (t == typeof( byte )) return ((byte)o).ToString( p );                
                else if (t == typeof( short )) return ((short)o).ToString( p );
                else if (t == typeof( ushort )) return ((ushort)o).ToString( p );
                else if (t == typeof( int )) return ((int)o).ToString( p );
                else if (t == typeof( uint )) return ((uint)o).ToString( p );
                else if (t == typeof( long )) return ((long)o).ToString( p );
                else if (t == typeof( ulong)) return ((ulong)o).ToString( p );                
                else if (t == typeof( char )) return ((char)o).ToString( p );
                else if (t == typeof( bool )) return ((bool)o).ToString( p );
                else
                    return o.ToString();
            })).Memoize();

        private static Func<object, string, string> memoizedToStringFunc3 =
            ((Func<object, string, string>)((o, s) => {
                Type t = o.GetType();
                if (t == typeof( float )) return ((float)o).ToString( s );
                else if (t == typeof( double )) return ((double)o).ToString( s );
                else if (t == typeof( decimal )) return ((decimal)o).ToString( s );
                else if (t == typeof( sbyte )) return ((sbyte)o).ToString( s );
                else if (t == typeof( byte )) return ((byte)o).ToString( s );
                else if (t == typeof( short )) return ((short)o).ToString( s );
                else if (t == typeof( ushort )) return ((ushort)o).ToString( s );
                else if (t == typeof( int )) return ((int)o).ToString( s );
                else if (t == typeof( uint )) return ((uint)o).ToString( s );
                else if (t == typeof( long )) return ((long)o).ToString( s );
                else if (t == typeof( ulong )) return ((ulong)o).ToString( s );
                else
                    return o.ToString();
            })).Memoize();

        private static Func<object, string, IFormatProvider, string> memoizedToStringFunc4 =
            ((Func<object, string, IFormatProvider, string>)((o, s, p) => {
                Type t = o.GetType();
                if (t == typeof( float )) return ((float)o).ToString( s, p );
                else if (t == typeof( double )) return ((double)o).ToString( s, p );
                else if (t == typeof( decimal )) return ((decimal)o).ToString( s, p );
                else if (t == typeof( sbyte )) return ((sbyte)o).ToString( s, p );
                else if (t == typeof( byte )) return ((byte)o).ToString( s, p );
                else if (t == typeof( short )) return ((short)o).ToString( s, p );
                else if (t == typeof( ushort )) return ((ushort)o).ToString( s, p );
                else if (t == typeof( int )) return ((int)o).ToString( s, p );
                else if (t == typeof( uint )) return ((uint)o).ToString( s, p );
                else if (t == typeof( long )) return ((long)o).ToString( s, p );
                else if (t == typeof( ulong )) return ((ulong)o).ToString( s, p );
                else
                    return o.ToString();
            })).Memoize();

        private static Func<string, string, string> memoizedStringConcatFunc =
            ((Func<string, string, string>)((A, B) => { return A + B; })).Memoize();
        
        internal static string MemoizedConcat(this String str, string str2)
        {
            return memoizedStringConcatFunc( str, str2 );
        }

        internal static string MemoizedToString(this object o)
        {
            return memoizedToStringFunc1( o );
        }
        internal static string MemoizedToString(this object o, IFormatProvider provider)
        {
            return memoizedToStringFunc2( o, provider );
        }
        internal static string MemoizedToString(this object o, string format)
        {
            return memoizedToStringFunc3( o, format );
        }
        internal static string MemoizedToString(this object o, string format, IFormatProvider provider)
        {            
            return memoizedToStringFunc4( o, format, provider );
        }

        /// <summary>
        /// One parameter memoization
        /// </summary>
        internal static Func<A, R> Memoize<A, R>(this Func<A, R> f)
        {
            var d = new Dictionary<A, R>();
            return a =>
            {
                R r;
                if (!d.TryGetValue( a, out r ))
                {
                    r = f( a );
                    d.Add( a, r );
                }
                return r;
            };
        }
        internal static Func<T, R> CastByExample<T, R>(Func<T, R> f, T t) { return f; }
        /// <summary>
        /// Two parameter memoization using anonymous types
        /// </summary>
        internal static Func<A, B, R> Memoize<A, B, R>(this Func<A, B, R> f)
        {
            var example = new { A = default( A ), B = default( B ) };
            var tuplified = CastByExample( t => f( t.A, t.B ), example );
            var memoized = tuplified.Memoize();
            return (a, b) => memoized( new { A = a, B = b } );
        }
        /// <summary>
        /// Three parameter memoization using anonymous types
        /// </summary>
        internal static Func<A, B, C, R> Memoize<A, B, C, R>(this Func<A, B, C, R> f)
        {
            var example = new { A = default( A ), B = default( B ), C = default( C ) };            
            var tuplified = CastByExample( t => f( t.A, t.B, t.C ), example );
            var memoized = tuplified.Memoize();
            return (a, b, c) => memoized( new { A = a, B = b, C = c } );
        }

    }
}

/*
All code in this file (except as ohterwise noted) Copyright(c) 2016 Nate West

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

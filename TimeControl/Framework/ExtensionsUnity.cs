/* ntwest - Pulled portions of code from KSPPluginFramework Version 1.2, 
Forum Thread:http://forum.kerbalspaceprogram.com/threads/66503-KSP-Plugin-Framework
Author: TriggerAu, 2014
License: The MIT License (MIT)

Other code written by ntwest, MIT Licensed.
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

        // Yes, Virginia, there is a Santa Claus. String Concatenation of two strings is fast. And once we do it, we never have to do it again since we've cached the result.
        private static Func<string, string, string> memoizedStringConcatFunc =
            ((Func<string, string, string>)((A, B) => { return A + B; })).Memoize();
        
        public static string MemoizedConcat(this String str, string str2)
        {
            return memoizedStringConcatFunc( str, str2 );
        }

        public static string MemoizedToString(this object o)
        {
            return memoizedToStringFunc1( o );
        }
        public static string MemoizedToString(this object o, IFormatProvider provider)
        {
            return memoizedToStringFunc2( o, provider );
        }
        public static string MemoizedToString(this object o, string format)
        {
            return memoizedToStringFunc3( o, format );
        }
        public static string MemoizedToString(this object o, string format, IFormatProvider provider)
        {            
            return memoizedToStringFunc4( o, format, provider );
        }

        /// <summary>
        /// One parameter memoization
        /// </summary>
        public static Func<A, R> Memoize<A, R>(this Func<A, R> f)
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
        public static Func<T, R> CastByExample<T, R>(Func<T, R> f, T t) { return f; }
        /// <summary>
        /// Two parameter memoization using anonymous types
        /// </summary>
        public static Func<A, B, R> Memoize<A, B, R>(this Func<A, B, R> f)
        {
            var example = new { A = default( A ), B = default( B ) };
            var tuplified = CastByExample( t => f( t.A, t.B ), example );
            var memoized = tuplified.Memoize();
            return (a, b) => memoized( new { A = a, B = b } );
        }
        /// <summary>
        /// Three parameter memoization using anonymous types
        /// </summary>
        public static Func<A, B, C, R> Memoize<A, B, C, R>(this Func<A, B, C, R> f)
        {
            var example = new { A = default( A ), B = default( B ), C = default( C ) };
            var tuplified = CastByExample( t => f( t.A, t.B, t.C ), example );
            var memoized = tuplified.Memoize();
            return (a, b, c) => memoized( new { A = a, B = b, C = c } );
        }


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
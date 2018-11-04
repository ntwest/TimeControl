using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TimeControl
{
    [Flags]
    public enum EntryExitLoggerOptions
    {
        /// <summary>
        /// Don't log methods at all
        /// </summary>
        None = 0x00,
        /// <summary>
        /// Log entry into the method
        /// </summary>
        Entry = 0x01,
        /// <summary>
        /// Log exit from the method
        /// </summary>
        Exit = 0x02,
        /// <summary>
        /// Log the execution time of the method
        /// </summary>
        ExecutionTime = 0x04,
        /// <summary> 
        /// Log all data 
        /// </summary> 
        All = Entry | Exit | ExecutionTime,
        /// <summary>
        /// Always write to log even if logging level is set to greater than trace
        /// </summary>
        AlwaysLog = 0x08,
    }


    public class EntryExitLogger : IDisposable
    {
        private bool alwaysLog;
        private string blockName;
        private EntryExitLoggerOptions options;
        private Stopwatch sw;

        /// <summary>
        /// Log a block of code entry, exit, and timing
        /// </summary>
        /// <param name="blockName">The name of the code block being logged</param>
        /// <param name="options">The log options</param>
        /// <returns>A disposable object or none if logging is disabled</returns>
        public static IDisposable EntryExitLog(string blockName, EntryExitLoggerOptions options = EntryExitLoggerOptions.AlwaysLog)
        {
            IDisposable logger = null;

            if (Log.LoggingLevel == LogSeverity.Trace || ((options & EntryExitLoggerOptions.AlwaysLog) == EntryExitLoggerOptions.AlwaysLog))
            {
                // Check if ExecutionTime logging is requested, and if so log if Verbose logging (or greater) is chosen
                bool shouldCreate = ((options & EntryExitLoggerOptions.ExecutionTime) == EntryExitLoggerOptions.ExecutionTime);

                // If not logging ExecutionTime log only if Entry or Exit tracing is requested
                if (!shouldCreate)
                {
                    shouldCreate = (((options & EntryExitLoggerOptions.Entry) == EntryExitLoggerOptions.Entry) || ((options & EntryExitLoggerOptions.Exit) == EntryExitLoggerOptions.Exit));
                }

                // Check if we actually need to log anything
                if (shouldCreate)
                {
                    logger = new EntryExitLogger( blockName, options );
                }
            }

            // Will return null if no method logger was needed - which will effectively be ignored in a using statement.
            return logger;
        }

        /// <summary>
        /// Ctor private - just called from the static MethodLog method
        /// </summary>
        /// <param name="blockName">The name of the method being logged</param>
        /// <param name="options">The log options</param>
        private EntryExitLogger(string blockName, EntryExitLoggerOptions options)
        {
            this.blockName = blockName;
            this.options = options;
            this.alwaysLog = ((this.options & EntryExitLoggerOptions.AlwaysLog) == EntryExitLoggerOptions.AlwaysLog);

            if ((this.options & EntryExitLoggerOptions.ExecutionTime) == EntryExitLoggerOptions.ExecutionTime)
            {
                this.sw = new Stopwatch();
                this.sw.Start();
            }

            if ((this.options & EntryExitLoggerOptions.Entry) == EntryExitLoggerOptions.Entry)
            {
                Log.Trace( "block entry", this.blockName, this.alwaysLog );
            }
        }

        /// <summary>
        /// Tidy up
        /// </summary>
        public void Dispose()
        {
            try
            {
                if ((this.options & EntryExitLoggerOptions.ExecutionTime) == EntryExitLoggerOptions.ExecutionTime)
                {
                    this.sw.Stop();
                    Log.Trace( String.Format( "block execution time {0}ms", this.sw.ElapsedMilliseconds ), this.blockName, this.alwaysLog );
                }

                if ((this.options & EntryExitLoggerOptions.Exit) == EntryExitLoggerOptions.Exit)
                {
                    Log.Trace( "block exit", this.blockName, this.alwaysLog );
                }
            } catch (Exception e)
            {
                Log.Error( e.Message );
                Log.Error( e.StackTrace );
            }
        }
    }
}

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

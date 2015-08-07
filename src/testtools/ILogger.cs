// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Protocols.TestTools.Logging;
using System.Collections.ObjectModel;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// An interface of logging infrastructure.
    /// </summary>
    /// <remarks>
    /// This interface describes a set of methods for test logging. A fixed number of test log entry
    /// kinds is supported. The single point to add a log entry is the <see cref="ILogger.Add"/> method.
    /// <para> 
    /// Explicit logging in test code can usually be narrowed to a minimum because validation logic using the <see cref="IChecker"/>
    /// API automatically takes care of logging. Also, adapters as obtained by the test site (<see cref="ITestSite"/>) take 
    /// care of logging calls to the underlying SUT (log entry type <see cref="LogEntryKind.EnterAdapter"/>).
    /// </para>
    /// <para>
    /// Which kind of log messages are actually processed and which targets they are directed to depends on the
    /// logging profile which is defined in the test configuration of the test site. Test code can explicitly change 
    /// the logging profile using the <see cref="ILogger.ActiveLoggingProfile"/> property.
    /// </para>
    /// </remarks>
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// Gets the test site this logging object is hosted on.
        /// </summary>
        ITestSite Site { get; }

        /// <summary>
        /// Adds an entry to the log.
        /// </summary>
        /// <param name="kind">The log message kind.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing one or more objects to format.</param>        
        /// <remarks>
        /// The <paramref name="message"/> parameter uses an extended format string for logging. 
        /// In addition to the standard formats (see <see cref="System.String.Format(string, object[])"/>), 
        /// the following codes are provided:
        /// <list type="bullet">
        /// <item>
        ///     <c>{m}</c>: inserts the name of the executing method together with its parameters (the executing method
        ///     is the caller of <see cref="ILogger.Add"/>)
        /// </item>
        /// <item>
        ///     <c>{t}</c>: inserts the name of the executing test.
        /// </item>
        /// </list>
        /// </remarks>
        void Add(LogEntryKind kind, string message, params object[] parameters);

        /// <summary>
        /// Adds the test result statistics to the log.
        /// </summary>
        void AddTestStatistic();

        /// <summary>
        /// Indicates whether the given log entry kind is actually active. This method can be used to guard
        /// expensive code to be executed only in the case where logging is enabled for the given
        /// entry kind.
        /// </summary>
        /// <param name="kind">Logs entry kind.</param>
        /// <returns>true indicates the given log kind is active; otherwise, false</returns>
        bool IsActive(LogEntryKind kind);

        /// <summary>
        /// Gets or sets the active logging profile. A logging profile is defined in the test configuration and
        /// describes what entries are actually logged and in which way (e.g. send beacon packages, etc.).
        /// </summary>
        string ActiveLoggingProfile { get; set; }

        /// <summary>
        /// Gets the log profile
        /// </summary>
        LogProfile LogProfile { get; }
    }


    /// <summary>
    /// An enumeration type which represents the types of message log entries. 
    /// </summary>
    public enum LogEntryKind
    {
        /// <summary>
        /// Indicates the beginning of logical test group.
        /// </summary>
        BeginGroup,

        /// <summary>
        /// Indicates the end of logical test group.
        /// </summary>
        EndGroup,

        /// <summary>
        /// Indicates a check point has passed. Captured requirements should be logged as this kind.
        /// </summary>
        Checkpoint,

        /// <summary>
        /// Indicates an assertion verification has passed.
        /// </summary>
        CheckSucceeded,

        /// <summary>
        /// Indicates an assertion verification has failed.
        /// </summary>
        CheckFailed,

        /// <summary>
        /// Indicates the inability to determine a Pass or Fail. Typically it requires manual analysis.
        /// </summary>
        CheckInconclusive,

        /// <summary>
        /// Indicates something that could be verified, but currently is not. It is treated like CheckSucceeded.
        /// </summary>
        CheckUnverified,

        /// <summary>
        /// Indicates entering test adapter code. Generally, PTF automatically logs this kind, and user should 
        /// not explicitly log it. The only exception is PTF can not log for managed adapter whose interface definition
        /// contains generic type.
        /// </summary>
        EnterAdapter,

        /// <summary>
        /// Indicates exiting test adapter code. Generally, PTF automatically logs this kind, and user should 
        /// not explicitly log it. The only exception is PTF can not log for managed adapter whose interface definition
        /// contains generic type.
        /// </summary>
        ExitAdapter,

        /// <summary>
        /// Indicates entering test method code.
        /// </summary>
        EnterMethod,

        /// <summary>
        /// Indicates exiting test method code
        /// </summary>
        ExitMethod,

        /// <summary>
        /// A free-style log entry for settings information.
        /// </summary>
        Settings,

        /// <summary>
        /// A free-style log entry for comment information.
        /// </summary>
        Comment,

        /// <summary>
        /// A free-style log entry for warning information.
        /// </summary>
        Warning,

        /// <summary>
        /// A free-style log entry for debugging information. 
        /// </summary>
        Debug,

        /// <summary>
        /// PTF internal use only. Indicates the outcome of a test is failed.
        /// </summary>
        TestFailed,

        /// <summary>
        /// PTF internal use only. Indicates the outcome of a test is inconclusive.
        /// </summary>
        TestInconclusive,

        /// <summary>
        /// PTF internal use only. Indicates the outcome of a test is passed.
        /// </summary>
        TestPassed,

        /// <summary>
        /// PTF internal use only. Indicates the status of a test is in progress.
        /// </summary>
        TestInProgress,

        /// <summary>
        /// PTF internal use only. Indicates the outcome of a test is error.
        /// </summary>
        TestError,

        /// <summary>
        /// PTF internal use only. Indicates the outcome of a test is timeout.
        /// </summary>
        TestTimeout,

        /// <summary>
        /// PTF internal use only. Indicates the outcome of a test is aborted.
        /// </summary>
        TestAborted,

        /// <summary>
        /// PTF internal use only. Indicates the outcome of a test is unknown.
        /// </summary>
        TestUnknown,

        /// <summary>
        /// PTF internal use only. Indicates an assertion verification has failed but can be ignored
        /// </summary>
        ExceptionalRequirement,

        /// <summary>
        /// Indicates a test step.
        /// </summary>
        TestStep
    }

    /// <summary>
    /// A class to represent the available log message.
    /// </summary>
    public class AvailableLogMessage : IDisposable
    {
        /// <summary>
        /// Whether this class is disposed
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Log information
        /// </summary>
        private Dictionary<string, object> logInfo;

        /// <summary>
        /// Log sinks
        /// </summary>
        private IList<LogSink> logSinks;

        /// <summary>
        /// Whether this message is the end of log
        /// </summary>
        private bool endOfLog;

        /// <summary>
        /// Gets all the entries of log information.
        /// </summary>
        public Dictionary<string, object> LogInfo
        {
            get
            {
                return logInfo;
            }
        }

        /// <summary>
        /// Gets available types of log sinks.
        /// </summary>
        public IList<LogSink> LogSinks
        {
            get
            {
                return logSinks;
            }
        }

        /// <summary>
        /// Whether this message is the end of log.
        /// </summary>
        public bool EndOfLog
        {
            get
            {
                return endOfLog;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sinks">Available types of log sinks.</param>
        /// <param name="info">Information to be logged.</param>
        /// <param name="isEndOfLog">Whether this message is the end of log</param>
        public AvailableLogMessage(
            IList<LogSink> sinks,
            Dictionary<string, object> info,
            bool isEndOfLog)
        {
            this.logSinks = sinks;
            this.logInfo = info;
            this.endOfLog = isEndOfLog;
        }


        /// <summary>
        /// Destructor
        /// </summary>
        ~AvailableLogMessage()
        {
            Dispose(false);
        }


        /// <summary>
        /// Releases resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Releases resources
        /// </summary>
        /// <param name="disposing">
        /// If disposing equals true, managed and unmanaged resources are disposed;
        /// else only unmanaged resources can be disposed.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // release logInfo
                    this.logInfo.Clear();
                    this.logInfo = null;

                    // release logSinks
                    for (int i = 0; i < this.logSinks.Count; i++)
                    {
                        this.logSinks[i] = null;
                    }
                    this.logSinks.Clear();
                    this.logSinks = null;
                }
                this.disposed = true;
            }
        }
    }
}
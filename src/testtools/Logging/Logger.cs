// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Collections.ObjectModel;

namespace Microsoft.Protocols.TestTools.Logging
{
    /// <summary>
    /// A class which provides logging functionality.
    /// </summary>
    internal class Logger : ILogger
    {
        /// <summary>
        /// Allowed maximum size of LogMessageQueue
        /// </summary>
        private const int MaxCapability = 4096;

        /// <summary>
        /// Whether this class is disposed
        /// </summary>
        private bool disposed;

        /// <summary>
        /// It only allows one sink write to the log file each time
        /// (Log entries should be organized in proper order)
        /// </summary>
        private object sinkLocker = new object();

        /// <summary>
        /// By default the reader is disabled (until queue is not empty)
        /// </summary>
        private EventWaitHandle hRead = new ManualResetEvent(false);

        /// <summary>
        /// By default the writer is enabled (will be disabled when queue exceeds max size)
        /// </summary>
        private EventWaitHandle hWrite = new ManualResetEvent(true);

        private ITestSite testSite;

        private LogProfile logProfile;

        private string activeLogProfile;

        private List<LogProvider> registeredProviders = new List<LogProvider>();

        private static Dictionary<ITestSite, bool> outputStatistics = new Dictionary<ITestSite, bool>();

        private Thread logRunner;

        private List<Exception> errors = new List<Exception>();

        private Queue<AvailableLogMessage> logMessageQueue
            = new Queue<AvailableLogMessage>();

        /// <summary>
        /// The message queue to store log messages waiting for handling by logger thread.
        /// </summary>
        internal Queue<AvailableLogMessage> LogMessageQueue
        {
            get
            {
                return logMessageQueue;
            }
        }

        /// <summary>
        /// Disables the default constructor
        /// </summary>
        private Logger()
        {
        }

        /// <summary>
        /// Constructs a new Logger instance.
        /// </summary>
        /// <param name="testSite">The test site this logging object is hosted in.</param>
        internal Logger(ITestSite testSite)
        {
            this.testSite = testSite;
            this.logProfile = LogProfileParser.CreateLogProfileFromConfig(testSite.Config, testSite.TestAssemblyName);
            this.ActiveLoggingProfile = LogProfileParser.ActiveProfileNameInConfig;
            RegisterDefaultLogProviders();
            logRunner = new Thread(Run);
            logRunner.Start();
            if (!outputStatistics.ContainsKey(testSite))
            {
                outputStatistics.Add(testSite, true);
            }
        }

        
        /// <summary>
        /// Reads and processes log messages
        /// </summary>
        private void Run()
        {
            AvailableLogMessage message = null;
            while (true)
            {
                // wait until queue is not empty
                hRead.WaitOne();
                lock (logMessageQueue)
                {
                    message = logMessageQueue.Dequeue();

                    // disable reader when queue is empty
                    if (logMessageQueue.Count == 0)
                    {
                        hRead.Reset();
                    }

                    // inform writer that queue is able to write
                    if (logMessageQueue.Count < MaxCapability)
                    {
                        hWrite.Set();
                    }
                }

                // skip empty message
                if (null == message)
                {
                    continue;
                }

                // end thread if this is the final message
                if (message.EndOfLog)
                {
                    message.Dispose();
                    message = null;
                    return;
                }

                // process message
                WriteInfoToSinks(message.LogInfo, (List<LogSink>)message.LogSinks);
                message.Dispose();
                message = null;
            }
        }


        /// <summary>
        /// Writes log info to sinks
        /// </summary>
        /// <param name="info">Log info</param>
        /// <param name="sinks">Log sinks</param>
        private void WriteInfoToSinks(Dictionary<string, object> info, List<LogSink> sinks)
        {
            if (null == info || null == sinks)
            {
                return;
            }

            lock (sinkLocker)
            {
                try
                {
                    foreach (LogSink sink in sinks)
                    {
                        sink.WriteEntry(info);
                        sink.Flush();
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is ThreadAbortException))
                    {
                        lock (errors)
                        {
                            errors.Add(ex);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Process errors from log runner thread.
        /// </summary>
        internal void ProcessErrors()
        {
            lock (errors)
            {
                if (errors.Count > 0)
                {
                    Exception exception = errors[0];
                    errors.Clear();
                    throw exception;
                }
            }
        }


        /// <summary>
        /// Gets the log runner thread state.
        /// </summary>
        internal System.Threading.ThreadState RunnerState
        {
            get
            {
                return logRunner.ThreadState;
            }
        }

        /// <summary>
        /// Registers a log provider to the current logger. The registered providers' properties 
        /// is to be appended into the information property bag and be delivered to log sinks.
        /// </summary>
        /// <param name="provider">The log provider instance to be registered.</param>
        public void RegisterLogProvider(LogProvider provider)
        {
            if (testSite == null)
            {
                throw new InvalidOperationException("TestSite must not be initialized before registering log providers.");
            }

            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            if (!registeredProviders.Contains(provider))
            {
                provider.Initialize(testSite);
                registeredProviders.Add(provider);
            }
        }

        private void RegisterDefaultLogProviders()
        {
            RegisterLogProvider(new ConfigPropertyLogProvider());
            RegisterLogProvider(new ClientInfoLogProvider());
            RegisterLogProvider(new ServerInfoLogProvider());
            RegisterLogProvider(new TestInfoLogProvider());
            RegisterLogProvider(new ReqInfoLogProvider());
        }

        private Dictionary<string, object> CreateLogInformationBag(
                LogEntryKind kind,
                string message,
                params object[] parameters
            )
        {
            Dictionary<string, object> information = new Dictionary<string, object>();
            Dictionary<string, bool> allowOverride = new Dictionary<string, bool>();

            // Append PTF-reserved information: LogEntryKind
            information.Add(LogInformationName.LogEntryKind, kind);
            allowOverride.Add(LogInformationName.LogEntryKind, false);

            // Append PTF-reserved information: CurrentTestCaseName
            if (testSite.TestProperties.ContainsKey(TestPropertyNames.CurrentTestCaseName))
            {
                information.Add(TestPropertyNames.CurrentTestCaseName,
                    testSite.TestProperties[TestPropertyNames.CurrentTestCaseName]);
                allowOverride.Add(TestPropertyNames.CurrentTestCaseName, false);
            }

            // Append PTF-reserved information: Message
            string msg = parameters != null && parameters.Length > 0 ? String.Format(message, parameters) : message;
            information.Add(LogInformationName.Message, msg);
            allowOverride.Add(LogInformationName.Message, false);

            // Append PTF-reserved information: TimeStamp
            DateTime timeStamp = DateTime.Now;
            information.Add(LogInformationName.TimeStamp, timeStamp);
            allowOverride.Add(LogInformationName.TimeStamp, false);

            lock (registeredProviders)
            {
                // Append information provided by providers.
                foreach (LogProvider provider in registeredProviders)
                {
                    provider.PrepareLogInformation(kind, msg, timeStamp, Site.TestProperties);

                    Dictionary<string, object> providedInfo = provider.Information;

                    foreach (string name in providedInfo.Keys)
                    {
                        if (information.ContainsKey(name) && !allowOverride[name])
                        {
                            // The information isn't allowed to be overridden.
                            throw new InvalidOperationException(
                                String.Format("Log information '{0}' is not allowed to be overridden.",
                                name));
                        }
                        else
                        {
                            // Append log information.
                            information[name] = providedInfo[name];
                            allowOverride[name] = provider.AllowOverride;
                        }
                    }
                }
            }
            return information;
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method is called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// This method will close all log sinks.
        /// </summary>
        /// <param name="disposing">
        /// If disposing equals false, the method is called by the 
        /// runtime from inside the finalizer and you should not reference 
        /// other objects. Only unmanaged resources can be disposed.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (logRunner.IsAlive)
                    {
                        // send end log message to end the thread.
                        AvailableLogMessage endMessage = new AvailableLogMessage(
                            new List<LogSink>(), new Dictionary<string, object>(), true);
                        lock (logMessageQueue)
                        {
                            logMessageQueue.Enqueue(endMessage);
                            hRead.Set();
                        }

                        // wait for thread end
                        logRunner.Join();
                        ProcessErrors();
                    }

                    // close event handles
                    hRead.Close();
                    hWrite.Close();

                    // clear queue
                    lock (logMessageQueue)
                    {
                        logMessageQueue.Clear();
                        logMessageQueue = null;
                    }

                    // clear sinks
                    if (null != logProfile)
                    {
                        foreach (LogSink sink in logProfile.AllSinks)
                        {
                            sink.Close();
                        }
                        logProfile.ProfilesMap.Clear();
                    }

                    // clear other resources
                    activeKinds.Clear();
                    activeKinds = null;

                    registeredProviders.Clear();
                    registeredProviders = null;

                    outputStatistics.Clear();
                    outputStatistics = null;

                    errors.Clear();
                    errors = null;
                }
                this.disposed = true;
            }
        }

        #endregion

        #region ILogger Members

        /// <summary>
        /// Implements <see cref="ILogger.Site"/>
        /// </summary>
        public ITestSite Site
        {
            get { return testSite; }
        }

        public LogProfile LogProfile
        {
            get { return logProfile; }
        }

        /// <summary>
        /// Implements <see cref="ILogger.Add"/>
        /// </summary>
        /// <param name="kind">The log message kind.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing one or more objects to format.</param>        
        public void Add(LogEntryKind kind, string message, params object[] parameters)
        {
            AvoidInvalidCall();
            ProcessErrors();

            // prepare log info and sinks
            Dictionary<string, object> info = CreateLogInformationBag(kind, message, parameters);
            List<LogSink> sinks = logProfile.GetSinksOfProfile(activeLogProfile, kind);

            // identify sinks that need to notify immediately
            List<LogSink> immediateSinks = new List<LogSink>();
            List<LogSink> ordinarySinks = new List<LogSink>();
            foreach (LogSink sink in sinks)
            {
                if (sink.NotifyImmediately)
                {
                    immediateSinks.Add(sink);
                }
                else
                {
                    ordinarySinks.Add(sink);
                }
            }
            WriteInfoToSinks(info, immediateSinks);

            // wait until queue size drops below max
            hWrite.WaitOne();
            lock (logMessageQueue)
            {
                // write a message and inform reader that queue is able to read
                logMessageQueue.Enqueue(new AvailableLogMessage(ordinarySinks, info, false));
                hRead.Set();

                // disable writer if queue size exceeds its max capability
                if (logMessageQueue.Count >= MaxCapability)
                {
                    hWrite.Reset();
                }
            }
        }


        /// <summary>
        /// Avoid calling any method if logger is disposed
        /// </summary>
        private void AvoidInvalidCall()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("Logger");
            }
        }


        /// <summary>
        /// Implements <see cref="ILogger.AddTestStatistic"/>
        /// </summary>
        public void AddTestStatistic()
        {
            AvoidInvalidCall();
            if (outputStatistics[testSite])
            {
                Dictionary<string, object> information = CreateLogInformationBag(LogEntryKind.Comment, "", null);

                Add(LogEntryKind.Settings, "Test finished, begin output configuration properties.");
                //
                NameValueCollection configProp = information[LogInformationName.PTFConfiguration] as NameValueCollection;
                foreach (string key in configProp)
                {
                    Add(LogEntryKind.Settings, "PTFConfigProperties.{0}: {1}", key, configProp[key]);
                }
                Add(LogEntryKind.Settings, "PTFConfigProperties.{0}: {1}", LogInformationName.ClientName, information[LogInformationName.ClientName]);
                Add(LogEntryKind.Settings, "PTFConfigProperties.{0}: {1}", LogInformationName.ClientOSInfo, information[LogInformationName.ClientOSInfo]);
                if (information.ContainsKey(LogInformationName.ServerOSInfo))
                {
                    Add(LogEntryKind.Settings, "PTFConfigProperties.{0}: {1}", LogInformationName.ServerOSInfo, information[LogInformationName.ServerOSInfo]);
                }

                Add(LogEntryKind.Comment, "begin output test statistics.");
                
                int executedTests = 0;
                foreach (int count in Site.TestResultsStatistics.Values)
                {
                    executedTests += count;
                }
                
                Add(LogEntryKind.Comment, "PTFTestResult.{0}: {1}",
                    LogInformationName.TestsExecuted, executedTests);

                foreach (KeyValuePair<PtfTestOutcome, int> kvp in Site.TestResultsStatistics)
                {
                    Add(LogEntryKind.Comment, 
                        "PTFTestResult.{0}: {1}",
                        LogInformationName.TestStatusName[kvp.Key], 
                        kvp.Value);
                }
               
                outputStatistics[testSite] = false;
            }
        }

        /// <summary>
        /// Implements <see cref="ILogger.ActiveLoggingProfile"/>
        /// </summary>
        public string ActiveLoggingProfile
        {
            get
            {
                return activeLogProfile;
            }
            set
            {
                activeLogProfile = value;
                if (logProfile != null)
                {
                    activeKinds = logProfile.GetActiveKindsOfProfile(activeLogProfile);
                }
            }
        }

        private List<LogEntryKind> activeKinds;

        /// <summary>
        /// Implements <see cref="ILogger.IsActive"/>
        /// </summary>
        /// <param name="kind">Log entry kind.</param>
        /// <returns>True indicates the given log kind is active; otherwise, false.</returns>
        public bool IsActive(LogEntryKind kind)
        {
            if (activeKinds == null)
            {
                return false;
            }

            return activeKinds.Contains(kind);
        }

        #endregion

    }
}

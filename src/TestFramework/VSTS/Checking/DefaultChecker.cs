// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Protocols.TestTools.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Protocols.TestTools.Checking
{
    /// <summary>
    /// The result of checking
    /// </summary>
    internal enum CheckResult
    {
        /// <summary>
        /// Indicates a checking verification has passed.
        /// </summary>
        Succeeded,

        /// <summary>
        /// Indicates a checking verification has failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Indicates the inability to determine the result.
        /// </summary>
        Inconclusive
    }



    /// <summary>
    /// An abstract base class of checkers which implements IChecker.
    /// </summary>
    /// <typeparam name="TFailException">The type of exception to be raised if a check is failed.</typeparam>
    /// <typeparam name="TInconclusiveException">The type of exception to be raised if a check is inconculsive.</typeparam>
    public abstract class DefaultChecker<TFailException, TInconclusiveException> : IChecker
        where TFailException : Exception
        where TInconclusiveException : Exception
    {
        private ITestSite testSite;

        private LogEntryKind failedLogKind;
        private LogEntryKind succeededLogKind;
        private LogEntryKind inconclusiveLogKind;
        private string exceptionFilter;
        private string checkerName;

        private List<string> exceptionalRequirements = new List<string>();

        private readonly object checkerLock = new object();

        private AsynchronousErrorProcessor asyncErrorProcessor;

        /// <summary>
        /// Constructs an instance of DefaultChecker.
        /// </summary>
        /// <param name="testSite">The test site to be bound.</param>
        /// <param name="checkerName">The name of the current checker (Assert, Assume or Debug).</param>
        /// <param name="failedLogKind">The log entry kind for logging a failed check.</param>
        /// <param name="succeededLogKind">The log entry kind for logging a succeeded check.</param>
        /// <param name="inconclusiveLogKind">The log entry kind for logging an inconclusive check.</param>
        /// <param name="checkerConfig">The checker confuguration to crate async error processor.</param>
        protected DefaultChecker(
            ITestSite testSite,
            string checkerName,
            LogEntryKind failedLogKind,
            LogEntryKind succeededLogKind,
            LogEntryKind inconclusiveLogKind,
            ICheckerConfig checkerConfig)
        {
            this.testSite = testSite;
            this.checkerName = checkerName;
            this.failedLogKind = failedLogKind;
            this.succeededLogKind = succeededLogKind;
            this.inconclusiveLogKind = inconclusiveLogKind;
            this.exceptionFilter = testSite.Properties[ConfigurationPropertyName.ExceptionFilter];

            if (checkerConfig == null)
            {
                throw new ArgumentNullException("checkerConfig");
            }

            this.asyncErrorProcessor = new AsynchronousErrorProcessor(
                checkerConfig.AssertFailuresBeforeThrowException, checkerConfig.MaxFailuresToDisplayPerTestCase);

            testSite.TestStarted += new EventHandler<TestStartFinishEventArgs>(
                    delegate(object sender, TestStartFinishEventArgs e)
                    {
                        asyncErrorProcessor.Initialize();
                    }
                );

            testSite.TestFinished += new EventHandler<TestStartFinishEventArgs>(
                    delegate(object sender, TestStartFinishEventArgs e)
                    {
                        asyncErrorProcessor.Cleanup();
                    }
                );

            if (null != testSite.Properties.Get("ExceptionalRequirements"))
            {
                var reqList = testSite.Properties.Get("ExceptionalRequirements").Split(',');
                foreach (string req in reqList)
                {
                    this.exceptionalRequirements.Add(req.Trim());
                }
            }

        }

        /// <summary>
        /// Checks if any error occurs.
        /// </summary>
        public void CheckErrors()
        {
            asyncErrorProcessor.Process();
        }

        private void Log(CheckResult checkResult, string message)
        {
            lock (checkerLock)
            {
                // Logging
                LogEntryKind kind = succeededLogKind;

                switch (checkResult)
                {
                    //CheckResult.Succeeded is the default condition.

                    case CheckResult.Failed:
                        kind = failedLogKind;
                        break;
                    case CheckResult.Inconclusive:
                        kind = inconclusiveLogKind;
                        break;
                    //Do not need default.
                    default:
                        break;
                }

                testSite.Log.Add(
                    kind,
                    message);

                if (checkResult != CheckResult.Succeeded)
                {
                    LogFailingStacks();
                }
            }
        }

        /// <summary>
        /// Dumps the call stacks of the calling assembly to logs.
        /// </summary>
        private void LogFailingStacks()
        {
            // The call stack frame count of PTF if there is a call to here. Currently there are 3 frames of DefaultChecker`2
            const int skipFrames = 3;

            StackTrace st = new StackTrace(skipFrames, true);
            if (st.FrameCount <= 0)
            {
                return;
            }

            // Find the last frame of the calling assembly and build the stack information.
            StringBuilder sbStacks = new StringBuilder();
            Assembly callingAss = st.GetFrame(0).GetMethod().Module.Assembly;
            for (int i = 0; i < st.FrameCount; i++)
            {
                Assembly assembly = st.GetFrame(i).GetMethod().Module.Assembly;
                if (assembly != callingAss)
                {
                    break;
                }
                sbStacks.Append(new StackTrace(st.GetFrame(i)).ToString());
            }

            // Logging
            if (sbStacks.Length > 0)
            {
                testSite.Log.Add(LogEntryKind.Comment, sbStacks.ToString());
            }
        }

        private string GetInformationString(CheckResult checkResult, string checkMethodName, string message, params object[] parameters)
        {
            // Tries to get requirement ID from parameters
            string requirementId = GetRequirementId(parameters);

            // Default information string
            string text = message;

            // Customized information string (if specified by parameters)
            if (!string.IsNullOrEmpty(message) && null == requirementId)
            {
                text = parameters != null && parameters.Length > 0 ?
                    String.Format(CultureInfo.CurrentCulture, message, parameters) : message;
            }

            // Load the format string according to the check result
            switch (checkResult)
            {
                case CheckResult.Succeeded:
                    return LoggingHelper.GetString("CheckSucceeded", checkerName, checkMethodName, text);

                case CheckResult.Failed:
                    if (null == requirementId)
                    {
                        return LoggingHelper.GetString("CheckFailed", checkerName, checkMethodName, text);
                    }
                    else
                    {
                        return LoggingHelper.GetString("CheckFailedOnReqId",
                            checkerName, checkMethodName, requirementId, text);
                    }

                case CheckResult.Inconclusive:
                    return LoggingHelper.GetString("CheckInconclusive", checkerName, checkMethodName, text);
            }

            return String.Empty;
        }

        /// <summary>
        /// Gets the Requirement ID from parameters
        /// (if parameters contain requirement id, it will contain exactly two objects;
        /// the first one is a string flag, the second is a string which represents requirement id)
        /// </summary>
        /// <param name="parameters">Input parameters</param>
        /// <returns>The requirement id</returns>
        private string GetRequirementId(params object[] parameters)
        {
            if (null == parameters || parameters.Length != 2)
            {
                return null;
            }

            string flag = parameters[0].ToString();
            if (flag.Equals("ContainsReqId"))
            {
                return parameters[1].ToString();
            }
            return null;
        }

        private static string FormatValueTypeString(object var)
        {
            if (var == null) return "(null)";
            if (var is short)
            {
                return String.Format("{0} (0x{1})", var.ToString(), ((short)var).ToString("X4"));
            }
            else if (var is ushort)
            {
                return String.Format("{0} (0x{1})", var.ToString(), ((ushort)var).ToString("X4"));
            }
            else if (var is int)
            {
                return String.Format("{0} (0x{1})", var.ToString(), ((int)var).ToString("X8"));
            }
            else if (var is uint)
            {
                return String.Format("{0} (0x{1})", var.ToString(), ((uint)var).ToString("X8"));
            }
            else if (var is long)
            {
                return String.Format("{0} (0x{1})", var.ToString(), ((long)var).ToString("X16"));
            }
            else if (var is ulong)
            {
                return String.Format("{0} (0x{1})", var.ToString(), ((ulong)var).ToString("X16"));
            }
            else
                return String.Format("{0} ({1})", var.ToString(), var.GetType().FullName);
        }

        private void GenerateException(CheckResult checkResult, string message, params object[] parameters)
        {
            if (checkResult != CheckResult.Succeeded)
            {
                bool isExceptionRequired = true;
                if (!string.IsNullOrEmpty(exceptionFilter))
                {
                    Regex regex = new Regex(exceptionFilter);
                    if (regex.IsMatch(message))
                    {
                        isExceptionRequired = false;
                    }
                }

                /************************ Test Case Pass/Fail Configurations **************************
                 * If the current requirement is in the exceptionalRequirements list                  *
                 * do not generate exception but log it in the log file                               *
                 **************************************************************************************/
                if (null != parameters && parameters.Length == 2)
                {
                    //parameters[0] is reqIdFlag, arameters[1] is reqID
                    if (exceptionalRequirements.Contains(parameters[1].ToString()))
                    {
                        isExceptionRequired = false;
                        testSite.Log.Add(LogEntryKind.ExceptionalRequirement, message.TrimEnd('.'));
                    }
                }

                if (isExceptionRequired)
                {
                    if (checkResult == CheckResult.Failed)
                    {
                        asyncErrorProcessor.ReportAsyncException(CreateFailException(message));
                    }
                    else if (checkResult == CheckResult.Inconclusive)
                    {
                        asyncErrorProcessor.ReportAsyncException(CreateInconclusiveException(message));
                    }
                }
            }
        }

        /// <summary>
        /// Creates a failure exception.
        /// The method needs to be overridden to create an instance of TFailException
        /// </summary>
        /// <param name="message">A message that describes the exception.</param>
        /// <returns>The instance of TFailException type</returns>
        protected abstract TFailException CreateFailException(string message);

        /// <summary>
        /// Creates an inconclusive exception.
        /// The method needs to be overridden to create an instance of TInconclusiveException
        /// </summary>
        /// <param name="message">A message that describes the exception.</param>
        /// <returns>The instance of TInconclusiveException type</returns>
        protected abstract TInconclusiveException CreateInconclusiveException(string message);

        #region IChecker Members

        /// <summary>
        /// Implements <see cref="IChecker.Site"/>
        /// </summary>
        public ITestSite Site
        {
            get { return testSite; }
        }

        /// <summary>
        /// Implements <see cref="IChecker.Fail"/>.
        /// This method generates a log entry and throws a failure exception if failed.
        /// </summary>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array which contains one or more objects to format.</param>
        public virtual void Fail(string message, params object[] parameters)
        {
            string text = GetInformationString(CheckResult.Failed, "Fail", message, parameters);

            Log(CheckResult.Failed, text);
            GenerateException(CheckResult.Failed, text);
        }

        /// <summary>
        /// Implements <see cref="IChecker.Pass"/>.
        /// This method generates a log entry.
        /// </summary>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array which contains one or more objects to format.</param>
        public void Pass(string message, params object[] parameters)
        {
            string text = GetInformationString(CheckResult.Succeeded, "Pass", message, parameters);

            Log(CheckResult.Succeeded, text);
        }

        /// <summary>
        /// Implements <see cref="IChecker.Inconclusive"/>.
        /// This method generates a log entry and throws an inconclusive exception if failed.
        /// </summary>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array which contains one or more objects to format.</param>
        public void Inconclusive(string message, params object[] parameters)
        {
            string text = GetInformationString(CheckResult.Inconclusive, "Inconclusive", message, parameters);

            Log(CheckResult.Inconclusive, text);
            GenerateException(CheckResult.Inconclusive, text);
        }

        /// <summary>
        /// Implements <see cref="IChecker.AreEqual"/>.
        /// This method generates a log entry and throws a failure exception if failed.
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare.</typeparam>
        /// <param name="expected">The first object to compare. This is the object the test expects.</param>
        /// <param name="actual">The second object to compare. This is the object the test produced.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array which contains zero or more objects to format.</param>
        public void AreEqual<T>(T expected, T actual, string message, params object[] parameters)
        {
            // Set text and res to the default values of successful condition.
            string text = (message == null) ? string.Empty : message; // To store additional information
            CheckResult res = CheckResult.Succeeded; // The result of the check.

            if (!object.Equals(expected, actual))
            {
                // Not Equals

                // Type of the checking data is different.
                if (((actual != null) && (expected != null)) && !actual.GetType().Equals(expected.GetType()))
                {
                    text = LoggingHelper.GetString(
                        "AreEqualFailMsg",
                        FormatValueTypeString(expected),
                        FormatValueTypeString(actual),
                        ((message == null) ? string.Empty : message));
                }
                else
                {
                    // Values are not equal.
                    text = LoggingHelper.GetString(
                        "AreEqualFailMsg",
                        FormatValueTypeString(expected),
                        FormatValueTypeString(actual),
                        ((message == null) ? string.Empty : message));
                }

                res = CheckResult.Failed;
            }

            text = GetInformationString(res, "AreEqual", text, parameters);
            Log(res, text);
            GenerateException(res, text, parameters);
        }

        /// <summary>
        /// Implements <see cref="IChecker.AreNotEqual"/>.
        /// This method generates a log entry and throws a failure exception if failed.
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare.</typeparam>
        /// <param name="expected">The first object to compare. This is the object the test expects.</param>
        /// <param name="actual">The second object to compare. This is the object the test produced.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array which contains zero or more objects to format.</param>
        public void AreNotEqual<T>(T expected, T actual, string message, params object[] parameters)
        {
            // Set text and res to the default values of successful condition.
            string text = (message == null) ? string.Empty : message; // To store additional information
            CheckResult res = CheckResult.Succeeded; // The result of the check.

            if (object.Equals(expected, actual))
            {
                // Failed
                text = LoggingHelper.GetString(
                    "AreNotEqualFailMsg",
                        FormatValueTypeString(expected),
                        FormatValueTypeString(actual),
                    (message == null) ? string.Empty : message);

                res = CheckResult.Failed;
            }

            text = GetInformationString(res, "AreNotEqual", text, parameters);
            Log(res, text);
            GenerateException(res, text, parameters);
        }

        /// <summary>
        /// Implements <see cref="IChecker.AreSame"/>.
        /// This method generates a log entry and throws a failure exception if failed.
        /// </summary>
        /// <param name="expected">The first object to compare. This is the object the test expects.</param>
        /// <param name="actual">The second object to compare. This is the object the test produced.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array which contains zero or more objects to format.</param>
        public void AreSame(object expected, object actual, string message, params object[] parameters)
        {
            // Set text and res to the default values of successful condition.
            string text = (message == null) ? string.Empty : message; // To store additional information

            // We don't need additional information in this case.
            CheckResult res = object.ReferenceEquals(expected, actual) ?
                CheckResult.Succeeded : CheckResult.Failed; // The result of the check.

            text = GetInformationString(res, "AreSame", text, parameters);
            Log(res, text);
            GenerateException(res, text, parameters);
        }

        /// <summary>
        /// Implements <see cref="IChecker.AreSame"/>.
        /// This method generates a log entry and throws a failure exception if failed.
        /// </summary>
        /// <param name="expected">The first object to compare. This is the object the test expects.</param>
        /// <param name="actual">The second object to compare. This is the object the test produced.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array which contains zero or more objects to format.</param>
        public void AreNotSame(object expected, object actual, string message, params object[] parameters)
        {
            // Set text and res to the default values of successful condition.
            string text = (message == null) ? string.Empty : message; // To store additional information

            // We don't need additional information in this case.
            CheckResult res = !Object.ReferenceEquals(expected, actual) ?
                CheckResult.Succeeded : CheckResult.Failed; // The result of the check.

            text = GetInformationString(res, "AreNotSame", text, parameters);
            Log(res, text);
            GenerateException(res, text, parameters);
        }

        /// <summary>
        /// Implements <see cref="IChecker.IsTrue"/>.
        /// This method generates a log entry and throws a failure exception if failed.
        /// </summary>
        /// <param name="value">The bool value to check.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array which contains zero or more objects to format.</param>
        public void IsTrue(bool value, string message, params object[] parameters)
        {
            // Set text and res to the default values of successful condition.
            string text = (message == null) ? string.Empty : message; // To store additional information

            // We don't need additional information in this case.
            CheckResult res = value ?
                CheckResult.Succeeded : CheckResult.Failed; // The result of the check.

            text = GetInformationString(res, "IsTrue", text, parameters);
            Log(res, text);
            GenerateException(res, text, parameters);
        }

        /// <summary>
        /// Implements <see cref="IChecker.IsFalse"/>.
        /// This method generates a log entry and throws a failure exception if failed.
        /// </summary>
        /// <param name="value">The bool value to check.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array which contains zero or more objects to format.</param>
        public void IsFalse(bool value, string message, params object[] parameters)
        {
            // Set text and res to the default values of successful condition.
            string text = (message == null) ? string.Empty : message; // To store additional information

            // We don't need additional information in this case.
            CheckResult res = !value ?
                CheckResult.Succeeded : CheckResult.Failed; // The result of the check.

            text = GetInformationString(res, "IsFalse", text, parameters);
            Log(res, text);
            GenerateException(res, text, parameters);
        }

        /// <summary>
        /// Implements <see cref="IChecker.IsNotNull"/>.
        /// This method generates a log entry and throws a failure exception if failed.
        /// </summary>
        /// <param name="value">The object to check.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array which contains zero or more objects to format.</param>
        public void IsNotNull(object value, string message, params object[] parameters)
        {
            // Set text and res to the default values of successful condition.
            string text = (message == null) ? string.Empty : message; // To store additional information

            // We don't need additional information in this case.
            CheckResult res = (value != null) ?
                CheckResult.Succeeded : CheckResult.Failed; // The result of the check.

            text = GetInformationString(res, "IsNotNull", text, parameters);
            Log(res, text);
            GenerateException(res, text, parameters);
        }

        /// <summary>
        /// Implements <see cref="IChecker.IsNull"/>.
        /// This method generates a log entry and throws a failure exception if failed.
        /// </summary>
        /// <param name="value">The object to check.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array which contains zero or more objects to format.</param>
        public void IsNull(object value, string message, params object[] parameters)
        {
            // Set text and res to the default values of successful condition.
            string text = (message == null) ? string.Empty : message; // To store additional information

            // We don't need additional information in this case.
            CheckResult res = (value == null) ?
                CheckResult.Succeeded : CheckResult.Failed; // The result of the check.

            text = GetInformationString(res, "IsNull", text, parameters);
            Log(res, text);
            GenerateException(res, text, parameters);
        }

        /// <summary>
        /// Implements <see cref="IChecker.IsInstanceOfType"/>.
        /// This method generates a log entry and throws a failure exception if failed.
        /// </summary>
        /// <param name="value">The object value to check.</param>
        /// <param name="type">The object type to check.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array which contains zero or more objects to format.</param>
        public void IsInstanceOfType(object value, Type type, string message, params object[] parameters)
        {
            // Set text and res to the default values of successful condition.
            string text = (message == null) ? string.Empty : message; // To store additional information
            CheckResult res = CheckResult.Succeeded; // The result of the check.

            if (type == null)
            {
                // Expected type is not provided.
                res = CheckResult.Failed;
            }
            else if (!type.IsInstanceOfType(value))
            {
                // Failed
                text = LoggingHelper.GetString(
                    "IsInstanceOfFailMsg",
                    type.ToString(),
                    (value == null) ? "(null)" : value.GetType().ToString(),
                    (message == null) ? string.Empty : message);

                res = CheckResult.Failed;
            }

            text = GetInformationString(res, "IsInstanceOfType", text, parameters);
            Log(res, text);
            GenerateException(res, text, parameters);
        }

        /// <summary>
        /// Implements <see cref="IChecker.IsNotInstanceOfType"/>.
        /// This method generates a log entry and throws a failure exception if failed.
        /// </summary>
        /// <param name="value">The object value to check.</param>
        /// <param name="type">The object type to check.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array which contains zero or more objects to format.</param>
        public void IsNotInstanceOfType(object value, Type type, string message, params object[] parameters)
        {
            // Set text and res to the default values of successful condition.
            string text = (message == null) ? string.Empty : message; // To store additional information
            CheckResult res = CheckResult.Succeeded; // The result of the check.

            if (type == null)
            {
                // Expected type is not provided.
                res = CheckResult.Failed;
            }
            else if ((value != null) && type.IsInstanceOfType(value))
            {
                // Failed
                text = LoggingHelper.GetString(
                    "IsNotInstanceOfFailMsg",
                    type.ToString(),
                    value.GetType().ToString(),
                    (message == null) ? string.Empty : message);

                res = CheckResult.Failed;
            }

            text = GetInformationString(res, "IsNotInstanceOfType", text, parameters);
            Log(res, text);
            GenerateException(res, text, parameters);
        }

        /// <summary>
        /// Implements <see cref="IChecker.IsSuccess"/>.
        /// This method generates a log entry and throws a failure exception if failed.
        /// </summary>
        /// <param name="hresult">The HRESULT value to check.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array which contains zero or more objects to format.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void IsSuccess(int hresult, string message, params object[] parameters)
        {
            // Set text and res to the default values of successful condition.
            string text = (message == null) ? string.Empty : message; // To store additional information

            // We don't need additional information in this case.
            CheckResult res = (hresult >= 0) ?
                CheckResult.Succeeded : CheckResult.Failed; // The result of the check.

            text = GetInformationString(res, "IsSuccess", text, parameters);
            Log(res, text);
            GenerateException(res, text, parameters);
        }

        /// <summary>
        /// Implements <see cref="IChecker.Unverified"/>
        /// </summary>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array which contains zero or more objects to format.</param>
        public void Unverified(string message, params object[] parameters)
        {
            testSite.Log.Add(LogEntryKind.CheckUnverified, message, parameters);
        }

        #endregion
    }

    /// <summary>
    /// The Checker Factory
    /// <para/>
    /// (from which the checker can be retrieved by checker kind, test site, and checker configuration.)
    /// </summary>
    public static class VsCheckerFactory
    {
        /// <summary>
        /// Gets a checker by checker kind, test site, and checker config.
        /// </summary>
        /// <param name="kind">The checker kind</param>
        /// <param name="testSite">The test site</param>
        /// <param name="checkerConfig">The checker configuration</param>
        /// <returns>The checker</returns>
        public static IChecker GetChecker(CheckerKinds kind, ITestSite testSite, ICheckerConfig checkerConfig)
        {
            IChecker checker = null;
            switch (kind)
            {
                case CheckerKinds.AssertChecker:
                    checker = new DefaultAssertChecker(testSite, checkerConfig);
                    break;
                case CheckerKinds.AssumeChecker:
                    checker = new DefaultAssumeChecker(testSite, checkerConfig);
                    break;
                case CheckerKinds.DebugChecker:
                    checker = new DefaultDebugChecker(testSite, checkerConfig);
                    break;
                default:
                    throw new InvalidOperationException("Checker kind is not supported: " + kind.ToString());
            }
            if (null == checker)
            {
                throw new InvalidOperationException(
                    string.Format("Cannot create {0} checker instance.", kind.ToString()));
            }
            return checker;
        }
    }
}

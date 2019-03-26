// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Microsoft.Protocols.TestTools.Logging
{
    /// <summary>
    /// This class contains supporting functions for logging.
    /// </summary>
    public sealed class LoggingHelper
    {
        private const string timeStampFormat = "{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}.{6:D3}";

        /// <summary>
        /// Disabled default constructor. Only static methods are intent in this class.
        /// </summary>
        private LoggingHelper()
        {
        }

        /// <summary>
        /// Retrieve a specified string from the resource.
        /// </summary>
        /// <param name="name">Name of the string.</param>
        /// <param name="parameters">An object array containing zero or more objects to format. </param>
        /// <returns>The corresponding string in the resource.</returns>
        public static string GetString(string name, params object[] parameters)
        {
            //string str = Messages.ResourceManager.GetString(name);
            System.Reflection.FieldInfo field = typeof(Messages).GetField(name);
            string str = (string)field.GetValue(null);
            return String.Format(str, parameters);
        }

        /// <summary>
        /// Map a PtfTestOutcome value to its corresponding LogEntryKind value.
        /// </summary>
        /// <param name="unitTestOutcome">Unit test outcome</param>
        /// <returns>The log entry kind</returns>
        public static LogEntryKind PtfTestOutcomeToLogEntryKind(PtfTestOutcome unitTestOutcome)
        {
            LogEntryKind logEntryKind = LogEntryKind.TestUnknown;

            switch(unitTestOutcome)
            {
                case PtfTestOutcome.Failed:
                    logEntryKind = LogEntryKind.TestFailed;
                    break;
                case PtfTestOutcome.Inconclusive:
                    logEntryKind = LogEntryKind.TestInconclusive;
                    break;
                case PtfTestOutcome.Passed:
                    logEntryKind = LogEntryKind.TestPassed;
                    break;
                case PtfTestOutcome.InProgress:
                    logEntryKind = LogEntryKind.TestInProgress;
                    break;
                case PtfTestOutcome.Error:
                    logEntryKind = LogEntryKind.TestError;
                    break;
                case PtfTestOutcome.Timeout:
                    logEntryKind = LogEntryKind.TestTimeout;
                    break;
                case PtfTestOutcome.Aborted:
                    logEntryKind = LogEntryKind.TestAborted;
                    break;
                default:
                    logEntryKind = LogEntryKind.TestUnknown;
                    break;
            }

            return logEntryKind;
        }


    }
}

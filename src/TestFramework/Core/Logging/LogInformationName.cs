// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools.Logging
{
    internal static class LogInformationName
    {
        internal const string Message = "Message";
        internal const string LogEntryKind = "LogEntryKind";
        internal const string TimeStamp = "TimeStamp";

        internal const string ProtocolName = "ProtocolName";
        internal const string ProtocolVersion = "ProtocolVersion";

        internal const string ServerIPInfo = "ServerIPAddress";
        internal const string ServerOSInfo = "ServerOS";
        internal const string ServerOSVendor = "ServerOSVendor";

        internal const string ClientName = "ClientName";
        internal const string ClientOSInfo = "ClientOS";

        internal const string TestCaseName = "TestCaseName";
        internal const string TestName = "TestName";
        internal const string TestResult = "TestResult";
        internal const string TestStatus = "TestStatus";

        internal const string ProtocolID = "ProtocolID";
        internal const string ProtocolSection = "ProtocolSection";

        internal const string PTFConfiguration = "PTFConfiguration";

        internal const string TestsExecuted = "TestsExecuted";

        private static Dictionary<PtfTestOutcome, string> testStatusName;
        internal static Dictionary<PtfTestOutcome, string> TestStatusName
        {
            get
            {
                if (testStatusName == null)
                {
                    testStatusName =
                        new Dictionary<PtfTestOutcome, string>();

                    testStatusName.Add(PtfTestOutcome.Passed, "TestsPassed");
                    testStatusName.Add(PtfTestOutcome.Failed, "TestsFailed");
                    testStatusName.Add(PtfTestOutcome.Inconclusive, "TestsInconclusive");
                    testStatusName.Add(PtfTestOutcome.Error, "TestsError");
                    testStatusName.Add(PtfTestOutcome.Aborted, "TestsAborted");
                    testStatusName.Add(PtfTestOutcome.InProgress, "TestsInProgress");
                    testStatusName.Add(PtfTestOutcome.Timeout, "TestsTimeout");
                    testStatusName.Add(PtfTestOutcome.Unknown, "TestsUnknown");
                }

                return testStatusName;
            }
        }
    }
}

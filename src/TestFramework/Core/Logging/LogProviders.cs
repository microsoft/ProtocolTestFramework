// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using Microsoft.Protocols.TestTools;

namespace Microsoft.Protocols.TestTools.Logging
{
    internal class ConfigPropertyLogProvider : LogProvider
    {
        public override void Initialize(ITestSite testSite)
        {
            base.Initialize(testSite);
            foreach (string key in testSite.Properties.AllKeys)
            {
                info.Add(key, testSite.Properties[key]);
            }
        }
    }

    internal class ClientInfoLogProvider : LogProvider
    {
        public override void Initialize(ITestSite testSite)
        {
            base.Initialize(testSite);

            info[LogInformationName.ClientName] = Dns.GetHostName();

            info[LogInformationName.ClientOSInfo] = Environment.OSVersion;
        }
    }

    internal class TestInfoLogProvider : LogProvider
    {
        private enum TestStatus
        {
            Framework,
            Running,
            Start,
            Stop
        }

        private enum TestResult
        {
            Failed,
            Inconclusive,
            Passed,
            InProgress,
            Error,
            Timeout,
            Aborted,
            Unknown
        }

        private TestResult testResult = TestResult.Unknown;
        private TestStatus testStatus = TestStatus.Framework;
        private string testCaseName;


        public override void Initialize(ITestSite testSite)
        {
            base.Initialize(testSite);
            info[LogInformationName.TestName] = testSite.TestName;
        }

        public override void PrepareLogInformation(
            LogEntryKind kind,
            string message,
            DateTime timeStamp,
            Dictionary<string, Object> testProperties)
        {
            if (testProperties.ContainsKey(TestPropertyNames.CurrentTestCaseName)
                && testProperties[TestPropertyNames.CurrentTestCaseName] != null)
            {
                testCaseName = testProperties[TestPropertyNames.CurrentTestCaseName] as string;
            }
            else
            {
                testCaseName = null;
            }

            if (testProperties.ContainsKey(TestPropertyNames.CurrentTestOutcome)
                && testProperties[TestPropertyNames.CurrentTestOutcome] != null)
            {
                PtfTestOutcome currentTestOutcome =
                (PtfTestOutcome)testProperties[TestPropertyNames.CurrentTestOutcome];
                UpdateStatus(currentTestOutcome);
            }
        }

        public override Dictionary<string, object> Information
        {
            get
            {
                if (testCaseName == null)
                {
                    // If runs out of a test case, the information bag should not contain this property.
                    if (info.ContainsKey(LogInformationName.TestCaseName))
                    {
                        info.Remove(LogInformationName.TestCaseName);
                    }
                }
                else
                {
                    info[LogInformationName.TestCaseName] = testCaseName;
                }

                info[LogInformationName.TestResult] = testResult;
                info[LogInformationName.TestStatus] = testStatus;

                return info;
            }
        }

        public override bool AllowOverride
        {
            get
            {
                return false;
            }
        }

        private void UpdateStatus(PtfTestOutcome currentTestOutcome)
        {
            testResult = PtfTestOutcomeToTestResult(currentTestOutcome);
            testStatus = PtfTestOutcomeToTestStatus(currentTestOutcome);
        }

        private static TestResult PtfTestOutcomeToTestResult(PtfTestOutcome testOutcome)
        {
            //keep the previous test result if not changing
            TestResult result = TestResult.Unknown;
            switch (testOutcome)
            {
                case PtfTestOutcome.Passed:
                    result = TestResult.Passed;
                    break;

                case PtfTestOutcome.Aborted:
                    result = TestResult.Aborted;
                    break;

                case PtfTestOutcome.Error:
                    result = TestResult.Error;
                    break;

                case PtfTestOutcome.Failed:
                    result = TestResult.Failed;
                    break;

                case PtfTestOutcome.Timeout:
                    result = TestResult.Timeout;
                    break;

                case PtfTestOutcome.Inconclusive:
                    result = TestResult.Inconclusive;
                    break;

                case PtfTestOutcome.Unknown:
                    result = TestResult.Unknown;
                    break;

                case PtfTestOutcome.InProgress:
                    result = TestResult.InProgress;
                    break;

                default:
                    break;
            }

            return result;
        }

        private static TestStatus PtfTestOutcomeToTestStatus(PtfTestOutcome testOutcome)
        {
            //keep the previous test status if not changing
            TestStatus result = TestStatus.Framework;

            switch (testOutcome)
            {
                case PtfTestOutcome.Passed:
                case PtfTestOutcome.Aborted:
                case PtfTestOutcome.Error:
                case PtfTestOutcome.Failed:
                case PtfTestOutcome.Timeout:
                case PtfTestOutcome.Inconclusive:
                    result = TestStatus.Stop;
                    break;
                case PtfTestOutcome.InProgress:
                    result = TestStatus.Running;
                    break;
                case PtfTestOutcome.Unknown:
                    result = TestStatus.Framework;
                    break;
                default:
                    break;
            }

            return result;
        }
    }

    internal class ReqInfoLogProvider : LogProvider
    {
        public override void Initialize(ITestSite testSite)
        {
            base.Initialize(testSite);
            info.Add(LogInformationName.PTFConfiguration, testSite.Properties);
        }
    }
}

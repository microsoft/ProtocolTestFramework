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

    internal class SafeNativeMethods
    {
        private SafeNativeMethods()
        {
        }

        [DllImport("netapi32.dll", SetLastError = true)]
        public static extern int NetWkstaGetInfo(
            [MarshalAs(UnmanagedType.LPWStr)]string servername,
            int level, out IntPtr lpBuffer);

        [DllImport("Netapi32.dll", SetLastError = true)]
        public static extern int NetApiBufferFree(IntPtr Buffer);

    }

    internal class ServerInfoLogProvider : LogProvider
    {
        private string serverName;
        private IPAddress serverIP;
        private OperatingSystem serverOsInfo;

        [StructLayout(LayoutKind.Sequential)]
        private struct WKSTA_INFO_100
        {
            public int wki102_platform_id;

            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
            public string wki102_computername;

            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
            public string wki102_langroup;

            public int wki102_ver_major;

            public int wki102_ver_minor;
        }

        public override void Initialize(ITestSite testSite)
        {
            base.Initialize(testSite);

            // Gets the server name
            serverName = testSite.Properties[ConfigurationPropertyName.BeaconLogServerName];

            info.Add(LogInformationName.ServerOSVendor, "Microsoft");
        }

        public override Dictionary<string, object> Information
        {
            get
            {
                if (!info.ContainsKey(LogInformationName.ServerOSInfo))
                {
                    // Make sure the server information can be obtained.
                    if (GetServerInfo())
                    {
                        info.Add(LogInformationName.ServerOSInfo, serverOsInfo);
                    }
                }

                if (!info.ContainsKey(LogInformationName.ServerIPInfo))
                {
                    // Make sure the server information can be obtained.
                    if (GetServerIP())
                    {
                        info.Add(LogInformationName.ServerIPInfo, serverIP);
                    }
                }
                return info;
            }
        }

        private bool GetServerInfo()
        {
            if (serverName != null && serverOsInfo == null)
            {
                IntPtr buffer = new IntPtr(); //represents the struct pointer.

                WKSTA_INFO_100 wksInfo;

                // Call Win32 API NetWkstaGetInfo to get the server OS information.
                int result = SafeNativeMethods.NetWkstaGetInfo(serverName, 100, out buffer);

                // 0 indicates succeeded. 
                if (result == 0)
                {
                    Int32 pointer = buffer.ToInt32();
                    wksInfo = (WKSTA_INFO_100)Marshal.PtrToStructure(
                        new IntPtr(pointer), typeof(WKSTA_INFO_100));

                    // Free unmanaged buffer.
                    SafeNativeMethods.NetApiBufferFree(buffer);

                    serverOsInfo = new OperatingSystem(
                        PlatformIDToEnum(wksInfo.wki102_platform_id),
                        new Version(wksInfo.wki102_ver_major, wksInfo.wki102_ver_minor)
                    );
                }
                else
                {
                    // If NetWkstaGetInfo failed, set the this information to null silently.
                    serverOsInfo = null;
                }
            }
            return (serverOsInfo != null);
        }

        private static PlatformID PlatformIDToEnum(int platformValue)
        {
            //PLATFORM_ID_DOS 300
            //PLATFORM_ID_OS2 400
            //PLATFORM_ID_NT  500
            //PLATFORM_ID_OSF 600
            //PLATFORM_ID_VMS 700
            switch (platformValue)
            {
                    
                case 500:
                    return PlatformID.Win32NT;
                    //break;
                case 300:
                case 400:
                case 600:
                case 700:
                default:
                    throw new FormatException(String.Format("PlatformID ({0}) is not a valid platform id.", platformValue));
            }
        }

        private bool GetServerIP()
        {
            if (serverName != null && serverIP == null)
            {
                try
                {
                    serverIP = Dns.GetHostEntry(serverName).AddressList[0];
                }
                catch (SocketException)
                {
                    // An error is encountered when resolving the host name, then set serverIP to null silently,
                    // and try to resolve next time.
                    serverIP = null;
                }
            }
            return (serverIP != null);
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

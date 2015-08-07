// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net;

namespace Microsoft.Protocols.TestTools.Logging
{
    internal class BeaconLogSink : LogSink
    {
        public BeaconLogSink(string name)
            : base(name)
        {
        }

        private const int tsapPort = 58727;

        private static UdpClient udpClient = new UdpClient();

        private static void WriteAny(Dictionary<string, object> information)
        {
            string hostName;
            if (information.ContainsKey(ConfigurationPropertyName.BeaconLogServerName) &&
                information[ConfigurationPropertyName.BeaconLogServerName] != null &&
                !string.IsNullOrEmpty(((string)information[ConfigurationPropertyName.BeaconLogServerName]).Trim()))
            {
                hostName = (string)information[ConfigurationPropertyName.BeaconLogServerName];
            }
            else
            {
                //default is broadcast
                hostName = IPAddress.Broadcast.ToString();
            }

            // Build the binary buffer of the logging information by TsapDataBuilder.
            byte[] buffer = TsapDataBuilder.Build(information);

            // UDP packets on a fixed port, 58727
            try
            {
                udpClient.Send(buffer, buffer.Length, hostName, tsapPort);
            }
            catch(SocketException)
            {
                // Ignore SocketException exceptions.
            }
            
        }

        protected override void OnWriteEntry(Dictionary<string, object> information)
        {
            WriteAny(information);
        }

        protected override void OnWriteBeginGroup(Dictionary<string, object> information)
        {
            WriteAny(information);
            base.OnWriteBeginGroup(information);
        }

        protected override void OnWriteEndGroup(Dictionary<string, object> information)
        {
            base.OnWriteEndGroup(information);
            WriteAny(information);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (udpClient != null)
            {
                udpClient.Close();
                udpClient = null;
            }
        }

        public override void Flush()
        {
        }
    }

    /// <summary>
    /// TsapDataBuilder is used to collect information and encode them into a byte[] buffer.
    /// </summary>
    internal class TsapDataBuilder
    {
        private TsapDataBuilder()
        {
        }

        private TsapDataBuilder(IDictionary<string, object> information)
        {
            logInfo = information;
        }

        private IDictionary<string, object> logInfo;

        private const byte version = 1;
        private const byte command = 0;
        private StringBuilder payload = new StringBuilder();

        private byte[] ConvertMessage()
        {
            //BUGBUG: Netmon parses TSAP frame as UNICODE encoded. This conflicts
            // with TSAP spec.
            byte[] plBytes = UnicodeEncoding.Unicode.GetBytes(payload.ToString());
            byte[] res = null;

            // Check if the TSAP payload is longer than 1024 bytes.
            // If it is longer than 1024 bytes, we should restrict it to 1024 bytes,
            // and log a warning through System.Diagnostics.Trace.
            if (plBytes.Length + 2 > 1024)
            {
                res = new byte[1024];
                ApplicationLog.TraceLog("[Beacon Log Warning]: The log message will be cut down to 1024 bytes by Beacon Log " +
                                    "because it exceeds the max length of TSAP payload.");
            }
            else
            {
                res = new byte[plBytes.Length + 2]; 
            }

            res[0] = version;
            res[1] = command;

            Array.Copy(plBytes, 0, res, 2, res.Length - 2);
            return res;
        }

        private void AppendAttribute(char type, string logInfoName)
        {
            if (logInfo.ContainsKey(logInfoName))
            {
                object logInfoContent = logInfo[logInfoName];
                
                // For the content of comment may be stack trace, we should remove all the "\r\n" from the string,
                // and trim the start and end of the string
                if (type == 'c')
                {
                    string removeFilter = "\r\n";
                    string trimFilter = "\t ";
                    if (logInfoContent == null)
                        logInfoContent = string.Empty;

                    logInfoContent = ((string)logInfoContent).Replace(removeFilter, string.Empty);
                    logInfoContent = ((string)logInfoContent).Trim(trimFilter.ToCharArray());
                }

                payload.AppendFormat("{0}={1}\r\n", type, logInfoContent);
            }
        }

        private void BuildClientDescription()
        {
            //a=To specify the end host that is sending the TSAP frame; 
            //p=To specify the platform of the end host; 
            //v= To specify the version of the client/server;
            //c=To specify the component name(s) of the OS that is(are) being tested for the test scenario;
            //e=To specify the version of the component(s) of the OS that is(are) being tested; 

            AppendAttribute('a', LogInformationName.ClientName);

            if (logInfo.ContainsKey(LogInformationName.ClientOSInfo))
            {
                payload.AppendFormat(
                    "p={0}\r\n", 
                    ((OperatingSystem)logInfo[LogInformationName.ClientOSInfo]).Platform
                    );
                payload.AppendFormat(
                    "v={0}\r\n", 
                    ((OperatingSystem)logInfo[LogInformationName.ClientOSInfo]).Version
                    );
            }

            AppendAttribute('c', LogInformationName.ProtocolName);
            AppendAttribute('e', LogInformationName.ProtocolVersion);
        }

        private void BuildServerDescription()
        {
            //a=To specify the end host that is sending the TSAP frame; 
            //p=To specify the platform of the end host; 
            //v= To specify the version of the client/server;
            //c=To specify the component name(s) of the OS that is(are) being tested for the test scenario;
            //e=To specify the version of the component(s) of the OS that is(are) being tested; 
            //u=To specify the vendor that the server platform is running (say  SUN, SUSE, MSFT etc) 

            AppendAttribute('a', LogInformationName.ServerName);

            if (logInfo.ContainsKey(LogInformationName.ServerOSInfo))
            {
                payload.AppendFormat(
                    "p={0}\r\n",
                    ((OperatingSystem)logInfo[LogInformationName.ServerOSInfo]).Platform
                    );
                payload.AppendFormat(
                    "v={0}\r\n",
                    ((OperatingSystem)logInfo[LogInformationName.ServerOSInfo]).Version
                    );
            }

            AppendAttribute('c', LogInformationName.ProtocolName);
            AppendAttribute('e', LogInformationName.ProtocolVersion);
            AppendAttribute('u', LogInformationName.ServerOSVendor);
        }

        private static bool IsCheckPointType(LogEntryKind kind)
        {
            return (
                kind == LogEntryKind.CheckFailed ||
                kind == LogEntryKind.CheckInconclusive ||
                kind == LogEntryKind.Checkpoint ||
                kind == LogEntryKind.CheckSucceeded ||
                kind == LogEntryKind.CheckUnverified ||
                kind == LogEntryKind.Debug
                );
        }

        private void BuildTestDescription()
        {
            //i=To specify the ID of the test that is sending the TSAP frame; This field can also be used for versioning of the tests. 
            //d=To specify id of document; 
            //s= To specify the document section(s) under test;
            //t=To specify the start/stop of the test scenario;
            //T=To specify the timestamp of the start/stop of the specific test;
            //f=To specify the fail/pass of the test; 
            //m=any other message that can be sent by client/server for extensibility purposes;
            //c= any comment about the test scenario;

            // BUGBUG: type 'd' and 's' is missing since it is TBD.

            AppendAttribute('i', LogInformationName.TestCaseName);
            AppendAttribute('d', LogInformationName.ProtocolID);
            AppendAttribute('s', LogInformationName.ProtocolSection);
            AppendAttribute('t', LogInformationName.TestStatus);
            AppendAttribute('T', LogInformationName.TimeStamp);
            // In Netmon-parsed TSAP packets, the 'f' attribute represents for the "Status" field
            AppendAttribute('f', LogInformationName.TestResult);

            // Log as type 'm' if logging kind is check point, otherwise the type is 'c'.
            AppendAttribute(
                IsCheckPointType((LogEntryKind)logInfo[LogInformationName.LogEntryKind]) ? 'm' : 'c',
                LogInformationName.Message);
        }

        public static byte[] Build(IDictionary<string, object> information)
        {
            TsapDataBuilder builder = new TsapDataBuilder(information);

            builder.BuildClientDescription();
            builder.BuildServerDescription();
            builder.BuildTestDescription();

            return builder.ConvertMessage();
        }
    }
}

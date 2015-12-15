// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Protocols.TestTools
{
    class LogmanCapture : IAutoCapture
    {
        string ClassName = "Unknown";
        string CaseName = "Unknown";
        string CaptureFileFolder = null;
        bool StopOnError = false;
        public void Initialize(NameValueCollection properties, string className)
        {
            ClassName = className;
            StopOnError = Convert.ToBoolean(properties.Get("PTF.NetworkCapture.StopRunningOnError"));
            CaptureFileFolder = properties.Get("PTF.NetworkCapture.CaptureFileFolder");

            if (!string.IsNullOrEmpty(CaptureFileFolder))
            {
                if (!Directory.Exists(CaptureFileFolder))
                    Directory.CreateDirectory(CaptureFileFolder);
            }
            // Try to stop the previous capture session if possible, ignore exception.
            SyncExecute("logman.exe", "stop -n ptftrace", true);
            SyncExecute("logman.exe", "delete -n ptftrace", true);
            SyncExecute("netsh.exe", "trace stop", true);

            SyncExecute("netsh.exe",
                "trace start capture=yes globalLevel=1 persistent=no overwrite=yes correlation=disabled report=no provider=Microsoft-Windows-NDIS-PacketCapture tracefile=Traffic.etl");
        }

        public void Cleanup()
        {
            SyncExecute("netsh.exe", "trace stop");
        }

        public void StartCapture(string testName)
        {
            // Logman capture
            CaseName = testName;
            string filename = string.Format("{0}#{1}.etl", ClassName, testName);
            if (!string.IsNullOrEmpty(CaptureFileFolder))
            {
                filename = System.IO.Path.Combine(CaptureFileFolder, filename);
            }
            if (File.Exists(filename)) File.Delete(filename);
            SyncExecute("logman.exe", string.Format("create trace -n ptftrace -o \"{0}\" --v -bs 64 -nb 16 512", filename));
            SyncExecute("logman.exe", "update trace -n ptftrace -p Microsoft-Windows-NDIS-PacketCapture 0 4");
            SyncExecute("logman.exe", "update trace -n ptftrace -p Protocol-Test-Suite");
            SyncExecute("logman.exe", "start -n ptftrace");
        }

        public void StopCapture()
        {
            SyncExecute("logman.exe", "stop -n ptftrace");
            SyncExecute("logman.exe", "delete -n ptftrace");
        }

        private void SyncExecute(string file, string args, bool ignoreException = false)
        {
            Process ps = new Process();
            ps.StartInfo = new ProcessStartInfo()
            {
                FileName = file,
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            ps.Start();
            ps.WaitForExit();
            if (ps.ExitCode != 0 && !ignoreException)
            {
                string StdErr = ps.StandardError.ReadToEnd();
                string StdOut = ps.StandardOutput.ReadToEnd();
                string CommandLine = string.Format("{0} {1}", file, args);
                StringBuilder errorMsg = new StringBuilder();
                errorMsg.AppendLine()
                    .Append("Test Suite: ").AppendLine(ClassName)
                    .Append("Test Case: ").AppendLine(CaseName)
                    .Append("Working Directory: ").AppendLine(ps.StartInfo.WorkingDirectory)
                    .Append("Command: ").AppendLine(CommandLine)
                    .AppendLine("Standard Output:")
                    .AppendLine(StdOut)
                    .AppendLine("Error Output")
                    .AppendLine(StdErr);

                throw new AutoCaptureException(errorMsg.ToString(), StopOnError);
            }
        }
    }
}

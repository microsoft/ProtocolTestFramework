// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Pipes;

namespace Microsoft.Protocols.TestTools.Logging
{
    internal class PipeSink : LogSink
    {
        private const string PipeName = "PTFToolPipe";

        private NamedPipeClientStream client;

        private StreamWriter sw;

        private const string timeStampFormat = "{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}.{6:D3}"; //e.g. 2007-05-16 16:50:23.412

        public PipeSink(string name)
            : base(name)
        {
            client = new NamedPipeClientStream(PipeName);
            try
            {
                client.Connect(1000);
                sw = new StreamWriter(client);
            }
            catch { sw = null; }
        }

        public PipeSink(string name, string identity)
            : base(name)
        {
            client = new NamedPipeClientStream(string.IsNullOrEmpty(identity) ? PipeName : identity);
            try
            {
                client.Connect(1000);
                sw = new StreamWriter(client);
            }
            catch { sw = null; }
        }

        protected override void OnWriteEntry(Dictionary<string, object> information)
        {
            WriteAny(
                (LogEntryKind)information[LogInformationName.LogEntryKind],
                (string)information[LogInformationName.Message],
                (DateTime)information[LogInformationName.TimeStamp]);
        }

        protected override void OnWriteBeginGroup(Dictionary<string, object> information)
        {
            WriteAny(
                (LogEntryKind)information[LogInformationName.LogEntryKind],
                (string)information[LogInformationName.Message],
                (DateTime)information[LogInformationName.TimeStamp]);
            base.OnWriteBeginGroup(information);
        }

        protected override void OnWriteEndGroup(Dictionary<string, object> information)
        {
            base.OnWriteEndGroup(information);
            WriteAny(
                (LogEntryKind)information[LogInformationName.LogEntryKind],
                (string)information[LogInformationName.Message],
                (DateTime)information[LogInformationName.TimeStamp]);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (sw != null)
            {
                sw.Close();
                sw = null;
            }
            client.Close();
        }

        private void WriteAny(LogEntryKind kind, string message, DateTime timeStamp)
        {
            if (sw == null) return;
            if (!String.IsNullOrEmpty(message))
            {
                try
                {
                    sw.Write(
                        timeStampFormat,  // format sting
                        timeStamp.Year,      // paramters
                        timeStamp.Month,
                        timeStamp.Day,
                        timeStamp.Hour,
                        timeStamp.Minute,
                        timeStamp.Second,
                        timeStamp.Millisecond);
                    sw.Write(String.Format(" [{0}] {1}\n", kind.ToString(), message));
                }
                catch { sw = null; }
            }
        }

        public override void Flush()
        {
            if (sw == null) return;
            sw.Flush();
        }
    }
}

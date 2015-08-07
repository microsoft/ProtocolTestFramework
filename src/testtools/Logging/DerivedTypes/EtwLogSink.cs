// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Protocols.TestTools.ExtendedLogging;

namespace Microsoft.Protocols.TestTools.Logging
{
    internal class EtwLogSink : LogSink
    {
        public EtwLogSink(string name)
            : base(name)
        {
        }

        protected override void OnWriteEntry(Dictionary<string, object> information)
        {
            ExtendedLoggerConfig.TestSuiteEvents.EventWriteTestSuiteLog(
                ExtendedLogging.ExtendedLoggerConfig.TestSuiteName,
                ExtendedLogging.ExtendedLoggerConfig.CaseName,
                information[LogInformationName.LogEntryKind].ToString(),
                (string)information[LogInformationName.Message]
                );
        }
        public override bool NotifyImmediately
        {
            get { return true; }
        }
        public override void Flush()
        {
        }
    }
}

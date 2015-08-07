// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Protocols.TestTools.ExtendedLogging
{
    /// <summary>
    /// Provides APIs for logging in Protocol Library and dumping message to ETW provider.
    /// </summary>
    public class ExtendedLogger
    {
        /// <summary>
        /// Dumps network message to ETW provider.
        /// </summary>
        /// <param name="messageName">The Name of the Message. In format protocol:message</param>
        /// <param name="dumpLevel">An enum indicating whether this message is a complete message on the wire or only part of the encrypted structure.</param>
        /// <param name="comments">Some comments about the dumped message.</param>
        /// <param name="payload">The byte array to dump.</param>
        static public void DumpMessage(string messageName, DumpLevel dumpLevel, string comments, byte[] payload)
        {
            ExtendedLoggerConfig.TestSuiteEvents.EventWriteRawMessage(
                ExtendedLoggerConfig.TestSuiteName,
                ExtendedLoggerConfig.CaseName,
                messageName,
                (ushort)dumpLevel,
                comments,
                (ushort)payload.Length,
                payload
                );
        }

        /// <summary>
        /// Dumps network message to ETW provider.
        /// </summary>
        /// <param name="messageName">The Name of the Message. In format protocol:message</param>
        /// <param name="comments">Some comments about the dumped message.</param>
        /// <param name="payload">The byte array to dump.</param>
        static public void DumpMessage(string messageName, string comments, byte[] payload)
        {
            DumpMessage(messageName, DumpLevel.WholeMessage, comments, payload);
        }

        /// <summary>
        /// Dumps network message to ETW provider.
        /// </summary>
        /// <param name="messageName">The Name of the Message. In format protocol:message</param>
        /// <param name="payload">The byte array to dump.</param>
        static public void DumpMessage(string messageName, byte[] payload)
        {
            DumpMessage(messageName, "", payload);
        }

    }

    /// <summary>
    /// Dump level for the DumpMessage method
    /// </summary>
    public enum DumpLevel
    {
        /// <summary>
        /// The message is a message on the wire.
        /// </summary>
        WholeMessage = 0,
        /// <summary>
        /// The message is part of the message on the wire.
        /// </summary>
        PartialMessage = 1
    }

    /// <summary>
    /// This class is for internal use only.
    /// </summary>
    public class ExtendedLoggerConfig
    {
        /// <summary>
        /// Events of the test suite
        /// </summary>
        static public TEST_SUITE_EVENTS TestSuiteEvents = new TEST_SUITE_EVENTS();

        /// <summary>
        /// Name of the test case
        /// </summary>
        static public string CaseName = "N/A";

        /// <summary>
        /// Name of the test suite
        /// </summary>
        static public string TestSuiteName = "N/A";
    }

}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Protocols.TestTools;

namespace Microsoft.Protocols.TestTools.Logging
{
    /// <summary>
    /// Only for internal use. An abstract base class provides a specified group of log information.
    /// </summary>
    internal abstract class LogProvider
    {
        internal Dictionary<string, object> info = new Dictionary<string,object>();

        /// <summary>
        /// Gets the log information.
        /// </summary>
        public virtual Dictionary<string, object> Information
        {
            get { return info; }
        }

        /// <summary>
        /// Gets whether the information can be overridden. The default value is true.
        /// </summary>
        public virtual bool AllowOverride
        {
            get { return true; }
        }

        /// <summary>
        /// Initializes the LogProvider instances.
        /// </summary>
        /// <param name="testSite">The test site which this LogProvider is hosted in.</param>
        public virtual void Initialize(ITestSite testSite)
        {
        }

        /// <summary>
        /// Prepares the log information according to the log entry information. This method will be called
        /// before the log entry is added to all sinks.
        /// </summary>
        /// <param name="kind">Log information kind.</param>
        /// <param name="message">Log information string.</param>
        /// <param name="timeStamp">The timestamp when the log information is created.</param>
        /// <param name="testProperties">The current test runtime properties.</param>
        public virtual void PrepareLogInformation(
            LogEntryKind kind,
            string message,
            DateTime timeStamp,
            Dictionary<string, Object> testProperties)
        {
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// A static class which contains the XML node names in the PTF configuration files. 
    /// </summary>
    public static class ConfigurationPropertyName
    {
        /// <summary>
        /// Server name
        /// </summary>
        public const string ServerName = "ServerComputerName";

        /// <summary>
        /// Beacon log target server name
        /// </summary>
        public const string BeaconLogServerName = "BeaconLogTargetServer";

        /// <summary>
        /// Feature name
        /// </summary>
        public const string ProtocolName = "FeatureName";
        
        /// <summary>
        /// Version
        /// </summary>
        public const string ProtocolVersion = "Version";
        
        /// <summary>
        /// Test suite name
        /// </summary>
        public const string TestName = "TestName";

        /// <summary>
        /// Regex filter for preventing throwing exception from the failure of Assert
        /// </summary>
        public const string ExceptionFilter = "ExceptionFilter";

        /// <summary>
        /// Regex filter to judge the result together with condition 
        /// </summary>
        public const string BypassFilter = "BypassFilter";
    }
}
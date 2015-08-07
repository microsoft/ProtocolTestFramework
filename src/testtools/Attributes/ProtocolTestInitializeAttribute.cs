// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// An internal attribute to callback test initialize action through PTF.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    sealed public class ProtocolTestInitializeAttribute : Attribute
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ProtocolTestInitializeAttribute()
        { }
    }
}

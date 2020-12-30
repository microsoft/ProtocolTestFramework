// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Protocols.TestTools.AdapterConsole
{
    [Serializable]
    public class ProcessResult
    {
        public string ReturnValue { get; set; }

        public Dictionary<string, string> OutArgValues { get; set; }
    }
}

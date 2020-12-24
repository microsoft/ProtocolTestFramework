// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Protocols.TestTools.AdapterConsole
{
    public class ParaCommandInfo
    {
        public string Title { get; set; }

        public string ParameterName { get; set; }

        public int ParameterIndex { get; set; }

        public string Content { get; set; }

        public bool IsExecute { get; set; }

        public string Type { get; set; }

        public object Value { get; set; }
    }

    public class ArgDetail
    {
        public string HelpMsg { get; set; }

        public ParaCommandInfo ReturnParam { get; set; }

        public List<ParaCommandInfo> OutParams { get; set; }

        public string OutFilePath { get; set; }

    }
}

// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.Protocols.TestTools;

namespace Microsoft.Protocols.TestSuites.XXXX.Adapter
{
    /// <summary>
    /// Defines the SUT control adapter
    /// It's used to control the SUT
    /// </summary>
    public interface IXXXX_SUTControlAdapter: IAdapter
    {
        [MethodHelp("Reset SUT to initial state. Return true for success, false for failure")]
        bool ResetSUT();
    }
}
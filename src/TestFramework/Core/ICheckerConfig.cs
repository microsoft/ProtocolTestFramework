// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// The checker configuration
    /// </summary>
    public interface ICheckerConfig
    {
        /// <summary>
        /// The number of assert failures need to be bypassed.
        /// </summary>
        int AssertFailuresBeforeThrowException {get;}

        /// <summary>
        /// The maximum failure messages need to be displayed.
        /// </summary>
        int MaxFailuresToDisplayPerTestCase { get; }
    }
}

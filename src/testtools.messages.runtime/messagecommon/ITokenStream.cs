// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools.Messages.Marshaling
{
    /// <summary>
    /// Interface for the token streams
    /// </summary>
    public interface ITokenStream
    {
        /// <summary>
        /// Gets the next token in stream
        /// </summary>
        /// <returns>The next token</returns>
        IToken NextToken();
    }
}

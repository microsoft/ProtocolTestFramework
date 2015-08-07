// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;


namespace Microsoft.Protocols.TestTools.Messages
{
    /// <summary>
    /// An interface which must be implemented by RPC adapter.
    /// </summary>
    /// <remarks>Currently this interface is empty, but it will contain things in the future.</remarks>
    public interface IRpcAdapter : IAdapter
    {
    }

    /// <summary>
    /// An interface which may be implemented by RPC adapter to enable implicit handle passing.
    /// </summary>
    /// <remarks>
    /// All RPC stubs which are associated with this interface can obtain the first parameter implicitly as specified by the
    /// handle. The handle is retrieved from the PTF configuration file and can be set programmatically
    /// (see <see cref="Handle">Handle</see>).
    /// <para>
    /// Note that by the nature of RPC stub binding, no type checking against the actual RPC stubs is to be made.
    /// It is the responsibility of the user to ensure that all RPC stubs actually receive as the first parameter
    /// a string value which represents the handle. If you have RPC calls with different handles, you should put them
    /// into different interfaces. 
    /// </para>
    /// </remarks>
    public interface IRpcImplicitHandleAdapter : IRpcAdapter
    {
        /// <summary>
        /// Gets or sets the handle. 
        /// The default value of the handle is retrieved from the PTF configuration file 
        /// by using the property name "T.handle", where T is the name of the adapter which extends this interface.
        /// </summary>
        string Handle { get; set; }
    }
}

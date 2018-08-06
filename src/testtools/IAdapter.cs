// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools
{
   
    /// <summary>
    /// An interface that every adapter must implement.
    /// </summary>
    [Microsoft.SpecExplorer.Runtime.Testing.TestAdapter]
    public interface IAdapter : IDisposable
    {
        /// <summary>
        /// Gets the test site associated with this adapter.
        /// </summary>
        ITestSite Site { get; }

        /// <summary>
        /// Initializes the current adapter instance and associates with a test site.
        /// </summary>
        /// <remarks >
        /// This method is called automatically by <see cref="ITestSite.GetAdapter"/>. User needs not call it directly.
        /// </remarks>
        /// <param name="testSite">The test site instance associated with the current adapter.</param>
        void Initialize(ITestSite testSite);

        /// <summary>
        /// This method is called before each test case runs. User dose not need to call it directly.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// A class which contains static methods used to determine adapter types.
    /// </summary>
    public sealed class AdapterType
    {
        /// <summary>
        /// The adapter base type's full name. 
        /// </summary>
        private static string fullName = typeof(IAdapter).FullName;

        /// <summary>
        /// Disables the default constructor.
        /// </summary>
        private AdapterType()
        {
        }

        /// <summary>
        /// Gets the full name of adapter base type.
        /// </summary>
        public static string AdapterTypeFullName
        {
            get { return fullName; }
        }

        /// <summary>
        ///  Gets a bool value which indicates whether the specified string is the full name of the adapter base type . 
        /// </summary>
        /// <param name="typeFullName">The full name of the type.</param>
        /// <returns>true if it equals the adapter base type's full name; otherwise, false.</returns>
        public static bool IsAdapterTypeFullName(string typeFullName)
        {
            return (fullName == typeFullName);
        }
    }
}

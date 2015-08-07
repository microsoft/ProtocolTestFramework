// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Internal use only. Interface between PTF and TCM.
    /// TCM should use this interface to inform PTF the status
    /// of overall tests run.
    /// An instance of this interface could be retrieved by
    /// ProtocolTestsManagerFactory.GetTestsManager()
    /// </summary>
    public interface IProtocolTestsManager
    {
        /// <summary>
        /// Gets test notify
        /// </summary>
        /// <param name="testSuiteName">Test suite name</param>
        /// <returns>Test notify</returns>
        IProtocolTestNotify GetProtocolTestNotify(string testSuiteName);

        /// <summary>
        /// Initializes PTF test site before all tests run.
        /// </summary>
        /// <param name="config">PTF configuration data</param>
        /// <param name="context">PTF protocol testcontext</param>
        /// <param name="testSuiteName">Test suite name</param>
        /// <param name="testAssemblyName">Test assembly name</param>
        void Initialize(
            IConfigurationData config,
            IProtocolTestContext context,
            string testSuiteName,
            string testAssemblyName);

        /// <summary>
        /// Initializes PTF test site before all tests run.
        /// </summary>
        /// <param name="config">PTF configuration data</param>
        /// <param name="configPath">Ptfconfig deployment path</param>
        /// <param name="testSuiteName">Test suite name</param>
        /// <param name="testAssemblyName">Test assembly name</param>
        void Initialize(
            IConfigurationData config,
            string configPath,
            string testSuiteName,
            string testAssemblyName);

        /// <summary>
        /// Gets test site by test suite name.
        /// </summary>
        /// <param name="testSuiteName">Test suite name</param>
        /// <returns>Test site instance.</returns>
        ITestSite GetTestSite(string testSuiteName);

        /// <summary>
        /// Indicates all test methods have been executed,
        /// and all test classes have been torn down.
        /// </summary>
        void TestsRunCleanup();

        /// <summary>
        /// cleanup after a test suite have been executed.
        /// </summary>
        /// <param name="testSuiteName">The test suite name which is used to get test site instance, and dispose it.</param>
        void CleanupTestSite(string testSuiteName);
    }
}

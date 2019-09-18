// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools
{
    internal class ProtocolTestsManager : IProtocolTestsManager
    {
        public void Initialize(
            IConfigurationData config,
            IProtocolTestContext context,
            string testSuiteName,
            string testAssemblyName)
        {
            TestSiteProvider.Initialize(config, context, testSuiteName, testAssemblyName);
        }

        public void Initialize(
            IConfigurationData config,
            string testAssemblyPath,
            string ptfconfigPath,
            string testSuiteName,
            string testAssemblyName)
        {
            TestSiteProvider.Initialize(config, testAssemblyPath, ptfconfigPath, testSuiteName, testAssemblyName);
        }

        public ITestSite GetTestSite(string testSuiteName)
        {
            return TestSiteProvider.GetTestSite(testSuiteName);
        }

        public void TestsRunCleanup()
        {
            TestSiteProvider.Cleanup();
        }

        public void CleanupTestSite(string testSuiteName)
        {
            TestSiteProvider.DisposeTestSite(testSuiteName);
        }

        public IProtocolTestNotify GetProtocolTestNotify(string testSuiteName)
        {

            return TestSiteProvider.GetProtocolTestNotify(testSuiteName);
        }


    }

    /// <summary>
    /// IProtocolTestsManager factory.
    /// </summary>
    public static class ProtocolTestsManagerFactory
    {
        private static IProtocolTestsManager testManager;

        /// <summary>
        /// Gets the IProtocolTestsManager instances.
        /// </summary>
        public static IProtocolTestsManager TestsManager
        {
            get
            {
                if (testManager == null)
                {
                    testManager = new ProtocolTestsManager();
                }

                return testManager;
            }
        }
    }
}

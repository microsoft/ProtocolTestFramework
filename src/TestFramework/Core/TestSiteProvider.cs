// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// FOR INTERNAL USE ONLY.
    /// A static class which manages the test site used throughout a series of test suite executions.
    /// </summary>
    internal static class TestSiteProvider
    {
        static Dictionary<string, DefaultTestSite> testSites;
        static object asyncCleanupLock = new object();

        /// <summary>
        /// Initializes the current test site, based on test test context and test suite name.
        /// If a current test site exists which does have same test suite name,
        /// it will be reused, otherwise a new one will be created, otherwise the current one
        /// will be reused.
        /// </summary>
        /// <param name="config">Configuration data from ptfconfig</param>
        /// <param name="context"></param>
        /// <param name="testSuiteName"></param>
        /// <param name="testAssemblyName">Test assembly name</param>
        public static void Initialize(
            IConfigurationData config, IProtocolTestContext context, string testSuiteName, string testAssemblyName)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context", "Test context cannot be null.");
            }

            Initialize(config, context.TestAssemblyDir, context.PtfconfigDir, testSuiteName, testAssemblyName);
        }

        /// <summary>
        /// Initializes the current test site, using the given config search path and test suite name.
        /// If a current test site exists which does have same test suite name,
        /// it will be reused, otherwise a new one will be created, otherwise the current one
        /// will be reused.
        /// </summary>
        /// <param name="config">Configuration data from ptfconfig</param>
        /// <param name="testAssemblyPath"></param>
        /// <param name="ptfconfigPath"></param>
        /// <param name="testSuiteName"></param>
        /// <param name="testAssemblyName">Test assembly name</param>
        public static void Initialize(
            IConfigurationData config, string testAssemblyPath, string ptfconfigPath, string testSuiteName, string testAssemblyName)
        {
            if (string.IsNullOrEmpty(testAssemblyPath))
            {
                throw new ArgumentException("testAssemblyPath cannot be null or empty.", "testAssemblyPath");
            }

            if (string.IsNullOrEmpty(ptfconfigPath))
            {
                throw new ArgumentException("ptfconfigPath cannot be null or empty.", "ptfconfigPath");
            }


            InitializeTestSite(config, testAssemblyPath, ptfconfigPath, testSuiteName, PtfTestOutcome.Unknown, testAssemblyName);
        }

        private static void InitializeTestSite(
            IConfigurationData config,
            string testAssemblyPath,
            string ptfconfigPath,
            string testSuiteName,
            PtfTestOutcome currentTestOutCome,
            string testAssemblyName)
        {
            if (null == config)
            {
                throw new ArgumentException("config cannot be null.");
            }

            if (testSites == null)
            {
                testSites = new Dictionary<string, DefaultTestSite>();
            }

            if (!testSites.ContainsKey(testSuiteName))
            {
                DefaultTestSite testSite = new DefaultTestSite(config, testAssemblyPath, ptfconfigPath, testSuiteName, testAssemblyName);

                testSites.Add(testSuiteName, testSite);
            }
            else
            {
                testSites[testSuiteName].DisposeAdapters();
            }

            testSites[testSuiteName].TestProperties[TestPropertyNames.CurrentTestCaseName] = null;
            testSites[testSuiteName].TestProperties[TestPropertyNames.CurrentTestOutcome] = currentTestOutCome;
        }

        public static ITestSite GetTestSite(string testSuiteName)

        {
            if (testSites == null || (!testSites.ContainsKey(testSuiteName)))
            {
                return null;
            }
            return testSites[testSuiteName];
        }

        public static IProtocolTestNotify GetProtocolTestNotify(string testSuiteName)

        {
            if (testSites == null || (!testSites.ContainsKey(testSuiteName)))
            {
                return null;
            }
            return testSites[testSuiteName];
        }

        /// <summary>
        /// Cleans up the current test site. This method closes the log and disposes the test site.
        /// </summary>
        public static void Cleanup()
        {
            lock (asyncCleanupLock)
            {
                if (testSites != null)
                {
                    foreach (KeyValuePair<string, DefaultTestSite> kvp in testSites)
                    {
                        if (kvp.Value != null)
                        {
                            kvp.Value.Dispose();
                        }
                    }
                    testSites.Clear();
                }
            }
        }

        public static void DisposeTestSite(string testSuiteName)
        {
            lock (asyncCleanupLock)
            {
                if (testSites != null && testSites.ContainsKey(testSuiteName))
                {
                    testSites[testSuiteName].Dispose();
                    testSites.Remove(testSuiteName);
                }
            }
        }
    }
}

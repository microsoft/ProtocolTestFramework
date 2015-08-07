// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using Microsoft.Protocols.TestTools;
using Microsoft.Protocols.TestSuites.XXXX.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Protocols.TestSuites.XXXX.TestSuite.Scenario1
{
    /// <summary>
    /// Summary description for the test cases of this scenario
    /// </summary>
    [TestClass]
    public class XXXX_Scenario1 : TestClassBase
    {
        #region Variables
        // Put here fields representing adapters
        static IXXXX_SUTControlAdapter SUTAdapter = null;
        static IXXXX_Adapter protocolAdapter = null;

        // Other static and instance properties
        // ...

        #endregion

        #region Test Suite Initialization and Cleanup

        /// <summary>
        /// Use ClassInitialize to run code before running the first test in the class
        /// </summary>
        [ClassInitialize()]
        public static void ClassInitialize(TestContext testContext)
        {
            TestClassBase.Initialize(testContext);

            try
            {
                SUTAdapter = TestClassBase.BaseTestSite.GetAdapter<IXXXX_SUTControlAdapter>();
                protocolAdapter = TestClassBase.BaseTestSite.GetAdapter<IXXXX_Adapter>();
            }
            catch (Exception ex)
            {
                TestClassBase.BaseTestSite.Assume.Inconclusive("ClassInitialize: Unexpected Exception - " + ex.Message);
            }
        }

        /// <summary>
        /// Use ClassCleanup to run code after all tests in a class have run
        /// </summary>
        [ClassCleanup()]
        public static void ClassCleanup()
        {
            TestClassBase.Cleanup();
        }

        #endregion

        #region Test Case Initialization and Cleanup

        /// <summary>
        /// TestInitialize will be run before every case's execution
        /// </summary>
        protected override void TestInitialize()
        {
            try
            {
                // Do some common initialization for every case.
                Site.Assume.AreEqual(true, SUTAdapter.ResetSUT(), "Reset SUT to initial states");
            }
            catch (Exception ex)
            {
                Site.Assume.Inconclusive("TestInitialize: Unexpected Exception - " + ex.Message);
            }
        }

        /// <summary>
        /// TestCleanup will be run after every case's execution
        /// </summary>
        protected override void TestCleanup()
        {
            try
            {
                // Do some common cleanup for every case.
                protocolAdapter.Reset(); 
            }
            catch (Exception ex)
            {
                Site.Log.Add(LogEntryKind.Warning, "TestCleanup: Unexpected Exception:", ex);
            }
        }

        #endregion

        #region Test cases
        [TestMethod]  // Indicates it's a test case
        [TestCategory("BVT")]  // It's used to categorize the test cases
        [Description("The case is designed to test if the SUT could be connected")]  // Describe what the test case is testing
        public void BVT_ConnectToSUT()
        {
            #region Case specific setup

            try
            {
                // Any test case specific setup logics
            }
            catch (Exception ex)
            {
                Site.Assume.Inconclusive("Unexpected Exception raised in one of custom test case setup steps: {0}", ex);
            }
            #endregion

            #region STEP1 Send a request to SUT
            Site.Log.Add(LogEntryKind.TestStep, "Step 1: Send a request to SUT");

            bool ret = protocolAdapter.SendRequest(Site.Properties.Get("SUTIPAddress"));
            Site.Assert.IsTrue(ret, "Send request should succeed.");
            #endregion

            #region STEP2 Waiting for a response from SUT
            int status = 0;
            Site.Log.Add(LogEntryKind.TestStep, "Step 2: Wait for a response from SUT");

            int timeout = int.Parse(Site.Properties.Get("SUTResponseTimeout"));
            ret = protocolAdapter.WaitForResponse(out status, timeout);
            Site.Assert.IsTrue(ret, "SUT response should be received in {0} seconds", timeout);
            #endregion

            #region Verify the status of the response
            // Status or other fields in the response can be checked here
            Site.Assert.AreEqual(0, status, "SUT should return success status in the response");
            #endregion

            #region Case specific cleanup
            try
            {
                // Any test case specific clean up logics
            }
            catch (Exception ex)
            {
                Site.Log.Add(LogEntryKind.Warning, "Unexpected Exception raised in one of custom test case cleanup steps: {0}", ex);
            }
            #endregion
        }
        #endregion

    }

}
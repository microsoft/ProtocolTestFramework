// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using Microsoft.Protocols.TestTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Protocols.TestTools.Test.Utilities;

namespace Microsoft.Protocols.TestTools.Test.TestAdapter
{
    /// <summary>
    /// Interface definition of an interactive adapter.
    /// </summary> 
    public interface IInteractiveAdapter : IAdapter
    {
        [MethodHelp("Please enter the same value with \"Number\" and then press \"Continue\" button.")]
        int ReturnInt(int Number);

        [MethodHelp("Please enter the same string with \"String\" (case sensitive) and then press \"Continue\" button.")]
        string ReturnString(string String);

        [MethodHelp("Please press \"Abort\" button.")]
        void Abort();
    }

    /// <summary>
    /// Test cases to test PTF adapter: Interactive adapter
    /// </summary>
    [TestClass]
    public class TestInteractiveAdapter : TestClassBase
    {
        IInteractiveAdapter interactiveAdapter;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            TestClassBase.Initialize(testContext, "TestAdapter");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            TestClassBase.Cleanup();
        }

        protected override void TestInitialize()
        {
            interactiveAdapter = BaseTestSite.GetAdapter<IInteractiveAdapter>();
        }

        protected override void TestCleanup()
        {
            interactiveAdapter.Reset();
        }

        #region Test cases
        [TestMethod]
        [TestCategory("TestAdapter")]
        public void InteractiveAdapterReturnInt()
        {
            BaseTestSite.Assert.AreEqual(
                0,
                interactiveAdapter.ReturnInt(0), 
                "Returned value should be 0");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void InteractiveAdapterReturnString()
        {
            BaseTestSite.Assert.AreEqual(
                "PTF",
                interactiveAdapter.ReturnString("PTF"), 
                "Returned value should be PTF");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        [PTFExpectedException(typeof(AssertInconclusiveException))]
        public void InteractiveAdapterAbort()
        {
            interactiveAdapter.Abort();
        }
        #endregion
    }
}

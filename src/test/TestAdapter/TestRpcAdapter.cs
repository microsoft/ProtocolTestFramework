// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using Microsoft.Protocols.TestTools;
using Microsoft.Protocols.TestTools.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Protocols.TestTools.Test.Utilities;

namespace Microsoft.Protocols.TestTools.Test.TestAdapter
{
    public interface IMyRpcAdapter : IRpcAdapter
    {
    }

    /// <summary>
    /// Test cases to test PTF adapter: Rpc adapter
    /// </summary>
    [TestClass]
    public class TestRpcAdapter : TestClassBase
    {

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

        #region Test cases
        [TestMethod]
        [TestCategory("TestAdapter")]
        public void RpcAdapterGetAdapter()
        {
            IMyRpcAdapter myRpcAdapter = BaseTestSite.GetAdapter<IMyRpcAdapter>();
            BaseTestSite.Assert.IsNotNull(myRpcAdapter, "Get rpc adapter should succeed.");
            myRpcAdapter.Reset();
        }
        #endregion
    }
}

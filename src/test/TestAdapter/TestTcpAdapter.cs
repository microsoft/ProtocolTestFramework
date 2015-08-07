// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.Protocols.TestTools;
using Microsoft.Protocols.TestTools.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Protocols.TestTools.Test.Utilities;

namespace Microsoft.Protocols.TestTools.Test.TestAdapter
{
    public interface IMyTcpAdapter: IAdapter
    {

    }

    public class MyTcpAdapter : TcpAdapterBase, IMyTcpAdapter
    {
        public MyTcpAdapter()
            : base("MyTcpAdapter")
        {
        }
        public override void Initialize(ITestSite testSite)
        {
            base.Initialize(testSite);

            // Call TcpAdapterBase.Connect() to verify if it works.
            Connect();
        }
    }

    /// <summary>
    /// Test cases to test PTF adapter: Rpc adapter
    /// </summary>
    [TestClass]
    public class TestTcpAdapter : TestClassBase
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
        public void TcpAdapterTryConnect()
        {
            // Set up a tcp listener, and wait for the tcp connect
            TcpListener tcpListener = new 
                TcpListener(IPAddress.Parse(BaseTestSite.Properties["MyTcpAdapter.hostname"]), Int32.Parse(BaseTestSite.Properties["MyTcpAdapter.port"]));
            tcpListener.Start();

            IMyTcpAdapter myTcpAdapter = BaseTestSite.GetAdapter<IMyTcpAdapter>();
            BaseTestSite.Assert.IsNotNull(myTcpAdapter, "Get tcp adapter should succeed.");
            myTcpAdapter.Reset();
            tcpListener.Stop();
        }
        #endregion
    }
}

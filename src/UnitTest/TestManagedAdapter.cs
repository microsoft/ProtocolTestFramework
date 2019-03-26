// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Protocols.TestTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Protocols.TestTools.UnitTest.Utilities;

namespace Microsoft.Protocols.TestTools.UnitTest.TestAdapter
{
    /// <summary>
    /// Interface definition of a managed adapter
    /// </summary>
    public interface IManagedAdapter : IAdapter
    {
        /// <summary>
        /// Adds two addends together
        /// </summary>
        int Sum(int x, int y);
    }

    public class ManagedAdapter : ManagedAdapterBase, IManagedAdapter
    {
        public int Sum(int x, int y)
        {
            return x + y;
        }
    }

    /// <summary>
    /// Interface definition of a managed adapter, but no relevant implementation
    /// </summary>
    public interface IUnimplementedManagedAdapter : IAdapter
    {
        /// <summary>
        /// Adds two addends together
        /// </summary>
        int Sum(int x, int y);
    }

    /// <summary>
    /// Test cases to test PTF adapter: Managed adapter
    /// </summary>
    [TestClass]
    public class TestManagedAdapter : TestClassBase
    {
        IManagedAdapter managedAdapter;

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
            managedAdapter = BaseTestSite.GetAdapter<IManagedAdapter>();
        }

        protected override void TestCleanup()
        {
            managedAdapter.Reset();
        }

        #region Test cases
        [TestMethod]
        [TestCategory("TestAdapter")]
        public void ManagerAdapterCallSucceed()
        {
            BaseTestSite.Assert.AreEqual(
                3 + 4,
                managedAdapter.Sum(3, 4),
                "Managed adapter should return 7");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        [PTFExpectedException(typeof(InvalidOperationException))]
        public void ManagerAdapterUnimplemented()
        {
            IUnimplementedManagedAdapter undefinedManagedAdapter = BaseTestSite.GetAdapter<IUnimplementedManagedAdapter>();
        }
        #endregion
    }
}

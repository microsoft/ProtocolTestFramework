using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Protocols.TestTools.UnitTest.TestAdapter
{
    /// <summary>
    /// Interface definition of a interactive adapter
    /// </summary>
    public interface IInteractiveAdapter : IAdapter
    {
        [MethodHelp("Check interactive adapter return value, expected input value is [Y].")]
        int ReturnInt(int number);
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
            string outStr = string.Empty;
            int outValue = interactiveAdapter.ReturnInt(0, out outStr);
            BaseTestSite.Assert.AreEqual(
                1,
                outValue,
                "Interactive adapter should return 1");
        }

        #endregion
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Protocols.TestTools.UnitTest.TestAdapter
{
    /// <summary>
    /// Interface definition of a interactive adapter
    /// </summary>
    public interface IInteractiveAdapter : IAdapter
    {
        [MethodHelp("Check interactive adapter return value, expected input value is [Y].")]
        int CheckReturnValueAfterEnterY(int number);

        [MethodHelp("Check interactive adapter return value, expected input value is [N].")]
        int CheckReturnValueAfterEnterN(int number);
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
        public void InteractiveAdapterCheckReturnValueAfterEnterY()
        {
            int outValue = interactiveAdapter.CheckReturnValueAfterEnterY(0);
            BaseTestSite.Assert.AreEqual(
                1,
                outValue,
                "Interactive adapter should return 1");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        [ExpectedException(typeof(AssertFailedException))]
        public void InteractiveAdapterCheckReturnValueAfterEnterN()
        {
            interactiveAdapter.CheckReturnValueAfterEnterY(0);
        }

        #endregion
    }
}

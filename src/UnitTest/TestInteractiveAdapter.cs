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

        /// <summary>
        /// Checks if the current environment is a CI/CD environment where interactive tests should be skipped
        /// </summary>
        private static bool IsRunningInCICD()
        {
            // Check common CI/CD environment variables
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")) ||           // Azure DevOps
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||     // GitHub Actions
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||                 // Generic CI indicator
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILD_BUILDID")) ||      // Azure DevOps Build ID
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SYSTEM_TEAMFOUNDATIONCOLLECTIONURI")); // Azure DevOps TFS
        }

        /// <summary>
        /// Checks if the current environment supports interactive console operations
        /// </summary>
        private static bool CanRunInteractiveTests()
        {
            // Check if we're in a CI/CD environment
            if (IsRunningInCICD())
            {
                return false;
            }

            // Check if we can access console (interactive tests require user input)
            // In automated environments, Console.IsInputRedirected is often true
            try
            {
                return !Console.IsInputRedirected && !Console.IsOutputRedirected;
            }
            catch
            {
                return false;
            }
        }

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
            // Skip interactive tests in CI/CD environments or when console interaction is not available
            if (!CanRunInteractiveTests())
            {
                BaseTestSite.Assert.Inconclusive("Interactive adapter tests are skipped in CI/CD environments or when console interaction is not available. These tests require manual user interaction.");
                return;
            }

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
            // Skip interactive tests in CI/CD environments or when console interaction is not available
            if (!CanRunInteractiveTests())
            {
                BaseTestSite.Assert.Inconclusive("Interactive adapter tests are skipped in CI/CD environments or when console interaction is not available. These tests require manual user interaction.");
                return;
            }

            interactiveAdapter.CheckReturnValueAfterEnterN(0);
        }

        #endregion
    }
}

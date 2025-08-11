// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Protocols.TestTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Protocols.TestTools.UnitTest.Utilities;

namespace Microsoft.Protocols.TestTools.UnitTest.TestAdapter
{
    public interface IShellAdapter : IAdapter
    {
        [MethodHelp("The script does not exist.")]
        void ScriptNotExisted();

        [MethodHelp("Return the value of the property \"FeatureName\".")]
        string GetPtfProp();

        [MethodHelp("Shell script will throw an exception.")]
        void ThrowException(string exceptionMessage);

        [MethodHelp("Shell script will return an integer.")]
        int ReturnInt(int number, out string name);

        [MethodHelp("Shell script will return a string.")]
        string ReturnString(string str);

        [MethodHelp("Shell script will return a boolean.")]
        bool ReturnBool(bool value);
    }

    /// <summary>
    /// Test cases to test PTF adapter: Script adapter
    /// </summary>
    [TestClass]
    public class TestShellAdapter : TestClassBase
    {
        IShellAdapter shellAdapter;

        /// <summary>
        /// Checks if the current environment is a CI/CD environment where shell tests should be skipped
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
        /// Checks if WSL (Windows Subsystem for Linux) is available for shell script execution
        /// </summary>
        private static bool IsWSLAvailable()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return true; // On Linux/macOS, shell scripts can run natively
            }

            // Check for WSL on Windows
            string winDir = Environment.GetEnvironmentVariable("WINDIR");
            if (string.IsNullOrEmpty(winDir))
            {
                return false;
            }

            string wslPath;
            if (Environment.Is64BitProcess)
            {
                wslPath = Path.Combine(winDir, "System32", "bash.exe");
            }
            else
            {
                wslPath = Path.Combine(winDir, "Sysnative", "bash.exe");
            }

            return File.Exists(wslPath);
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
            shellAdapter = BaseTestSite.GetAdapter<IShellAdapter>();
        }

        protected override void TestCleanup()
        {
            shellAdapter.Reset();
        }

        #region Test cases
        [TestMethod]
        [TestCategory("TestAdapter")]
        [PTFExpectedException(typeof(AssertInconclusiveException), "Assume.Fail failed. Shell script file (ScriptNotExisted.sh) can not be found.")]
        public void ShellAdapterScriptNotExisted()
        {
            shellAdapter.ScriptNotExisted();
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void ShellAdapterGetPtfProp()
        {
            // Skip shell tests in CI/CD environments or when WSL is not available
            if (IsRunningInCICD() || !IsWSLAvailable())
            {
                BaseTestSite.Assert.Inconclusive("Shell adapter tests are skipped in CI/CD environments or when WSL is not available. Shell scripts require Windows Subsystem for Linux (WSL) to execute on Windows.");
                return;
            }

            string propName = "FeatureName";
            BaseTestSite.Assert.AreEqual(
                BaseTestSite.Properties[propName],
                shellAdapter.GetPtfProp(),
                "Script adapter should return property value of {0}", propName);
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        [PTFExpectedException(typeof(InvalidOperationException))]
        public void ShellAdapterThrowException()
        {
            shellAdapter.ThrowException("Exception message.");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void ShellAdapterReturnInt()
        {
            // Skip shell tests in CI/CD environments or when WSL is not available
            if (IsRunningInCICD() || !IsWSLAvailable())
            {
                BaseTestSite.Assert.Inconclusive("Shell adapter tests are skipped in CI/CD environments or when WSL is not available. Shell scripts require Windows Subsystem for Linux (WSL) to execute on Windows.");
                return;
            }

            int num = 42;
            string name = string.Empty;
            BaseTestSite.Assert.AreEqual(
                num,
                shellAdapter.ReturnInt(num, out name),
                "Shell adapter should return " + num);
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void ShellAdpaterReturnString()
        {
            // Skip shell tests in CI/CD environments or when WSL is not available
            if (IsRunningInCICD() || !IsWSLAvailable())
            {
                BaseTestSite.Assert.Inconclusive("Shell adapter tests are skipped in CI/CD environments or when WSL is not available. Shell scripts require Windows Subsystem for Linux (WSL) to execute on Windows.");
                return;
            }

            string str = "PTF";
            BaseTestSite.Assert.AreEqual(
                str,
                shellAdapter.ReturnString(str),
                "Shell adapter should return " + str);
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void ShellAdapterReturnTrue()
        {
            // Skip shell tests in CI/CD environments or when WSL is not available
            if (IsRunningInCICD() || !IsWSLAvailable())
            {
                BaseTestSite.Assert.Inconclusive("Shell adapter tests are skipped in CI/CD environments or when WSL is not available. Shell scripts require Windows Subsystem for Linux (WSL) to execute on Windows.");
                return;
            }

            BaseTestSite.Assert.IsTrue(
                shellAdapter.ReturnBool(true),
                "Shell adapter should return true");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void ShellAdapterReturnFalse()
        {
            // Skip shell tests in CI/CD environments or when WSL is not available
            if (IsRunningInCICD() || !IsWSLAvailable())
            {
                BaseTestSite.Assert.Inconclusive("Shell adapter tests are skipped in CI/CD environments or when WSL is not available. Shell scripts require Windows Subsystem for Linux (WSL) to execute on Windows.");
                return;
            }

            BaseTestSite.Assert.IsFalse(
                shellAdapter.ReturnBool(false),
                "Shell adapter should return false");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void ShellAdpaterReturnStringContainingSpecialCharaters()
        {
            // Skip shell tests in CI/CD environments or when WSL is not available
            if (IsRunningInCICD() || !IsWSLAvailable())
            {
                BaseTestSite.Assert.Inconclusive("Shell adapter tests are skipped in CI/CD environments or when WSL is not available. Shell scripts require Windows Subsystem for Linux (WSL) to execute on Windows.");
                return;
            }

            string str = "It's great!!";
            BaseTestSite.Assert.AreEqual(
                str,
                shellAdapter.ReturnString(str),
                "Shell adapter should return " + str);
        }
        #endregion
    }
}

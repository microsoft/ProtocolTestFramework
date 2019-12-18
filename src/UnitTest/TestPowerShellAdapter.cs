// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Protocols.TestTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Protocols.TestTools.UnitTest.Utilities;

namespace Microsoft.Protocols.TestTools.UnitTest.TestAdapter
{
    /// <summary>
    /// Interface definition of a powershell adapter
    /// </summary>
    public interface IPowershellAdapter : IAdapter
    {
        [MethodHelp("Powershell script will throw an exception.")]
        void ThrowException(string exceptionMessage);

        [MethodHelp("Powershell script will return an integer.")]
        int ReturnInt(int number);

        [MethodHelp("Powershell script will return a string.")]
        string ReturnString(string str);

        [MethodHelp("Powershell script will return a boolean.")]
        bool ReturnBool(bool value);

        [MethodHelp("The relevant Powershell script does not exist")]
        void ScriptNotExisted();

        [MethodHelp("Powershell script will call another script and return a string.")]
        string NestedCall(string str);
    }

    /// <summary>
    /// Test cases to test PTF adapter: Powershell adapter
    /// </summary>
    [TestClass]
    public class TestPowershellAdapter : TestClassBase
    {
        IPowershellAdapter powershellAdapter;

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
            powershellAdapter = BaseTestSite.GetAdapter<IPowershellAdapter>();
        }

        protected override void TestCleanup()
        {
            powershellAdapter.Reset();
        }

        #region Test cases
        [TestMethod]
        [TestCategory("TestAdapter")]
        [PTFExpectedException(typeof(InvalidOperationException))]
        public void PowershellAdapterThrowException()
        {
            powershellAdapter.ThrowException("exception");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void PowershellAdapterReturnInt()
        {
            BaseTestSite.Assert.AreEqual(
                0,
                powershellAdapter.ReturnInt(0),
                "Powershell adapter should return 0");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void PowershellAdpaterReturnString()
        {
            BaseTestSite.Assert.AreEqual(
                "PTF",
                powershellAdapter.ReturnString("PTF"),
                "Powershell adapter should return PTF");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void PowershellAdapterReturnBool()
        {
            BaseTestSite.Assert.IsTrue(
                powershellAdapter.ReturnBool(true),
                "Powershell adapter should return true");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        [PTFExpectedException(typeof(AssertInconclusiveException), "Assume.Fail failed. PowerShell script file (ScriptNotExisted.ps1) can not be found.")]
        public void PowershellAdapterScriptNotExisted()
        {
            powershellAdapter.ScriptNotExisted();
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void PowershellAdapterNestedCall()
        {
            BaseTestSite.Assert.AreEqual(
                "PTF",
                powershellAdapter.NestedCall("PTF"),
                "Powershell adapter should return PTF");
        }
        #endregion
    }
}

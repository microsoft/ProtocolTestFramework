// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using Microsoft.Protocols.TestTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Protocols.TestTools.Test.Utilities;

namespace Microsoft.Protocols.TestTools.Test.TestAdapter
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
        int ReturnInt(int number);

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
            int num = 42;
            BaseTestSite.Assert.AreEqual(
                num,
                shellAdapter.ReturnInt(num),
                "Shell adapter should return " + num);
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void ShellAdpaterReturnString()
        {
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
            BaseTestSite.Assert.IsTrue(
                shellAdapter.ReturnBool(true),
                "Shell adapter should return true");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void ShellAdapterReturnFalse()
        {
            BaseTestSite.Assert.IsFalse(
                shellAdapter.ReturnBool(false),
                "Shell adapter should return false");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void ShellAdpaterReturnStringContainingSpecialCharaters()
        {
            string str = "It's great!!";
            BaseTestSite.Assert.AreEqual(
                str,
                shellAdapter.ReturnString(str),
                "Shell adapter should return " + str);
        }
        #endregion
    }
}

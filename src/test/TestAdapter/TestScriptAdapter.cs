// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using Microsoft.Protocols.TestTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Protocols.TestTools.Test.Utilities;

namespace Microsoft.Protocols.TestTools.Test.TestAdapter
{
    public interface IScriptAdapter : IAdapter
    {
        [MethodHelp("The script does not exist.")]
        void ScriptNotExisted();

        [MethodHelp("Return Help text")]
        string ReturnHelpText();

        [MethodHelp("Display all properties in the PTFconfig.")]
        void SetPtfProp();

        [MethodHelp("Return the value of the property \"FeatureName\".")]
        string GetPtfProp();

        [MethodHelp("Set failure message.")]
        void SetFailureMessage();

        [MethodHelp("Return the type of the return value.")]
        string ReturnTypeOfReturnValue();

        [MethodHelp("Return the value of the first input parameter.")]
        string ReturnInputParameterValue(string inputParameter);

        [MethodHelp("Return the name and the type of the first input parameter.")]
        string ReturnInputParameterNameAndType(string inputParameter);
    }

    /// <summary>
    /// Test cases to test PTF adapter: Script adapter
    /// </summary>
    [TestClass]
    public class TestScriptAdapter : TestClassBase
    {
        IScriptAdapter scriptAdapter;

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
            scriptAdapter = BaseTestSite.GetAdapter<IScriptAdapter>();
        }

        protected override void TestCleanup()
        {
            scriptAdapter.Reset();
        }

        #region Test cases
        [TestMethod]
        [TestCategory("TestAdapter")]
        [PTFExpectedException(typeof(AssertInconclusiveException), "Assume.Fail failed. Script SetFailureMessage.cmd " +
            "exited with non-zero code. Error code: -1. Failure message: PtfAdFailureMessage")]
        public void ScriptAdapterCheckFailureMessage()
        {
            scriptAdapter.SetFailureMessage();
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void ScriptAdapterReturnTypeOfReturnValue()
        {
            BaseTestSite.Assert.AreEqual(
                "PtfAdReturn:System.String",
                scriptAdapter.ReturnTypeOfReturnValue(), 
                "Script adapter should return [PtfAdReturn:<type of returnValue>]");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        [PTFExpectedException(typeof(AssertInconclusiveException), "Assume.Fail failed. The invoking script file (ScriptNotExisted.cmd) can not be found.")]
        public void ScriptAdapterScriptNotExisted()
        {
            scriptAdapter.ScriptNotExisted();
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void ScriptAdapterReturnInputParameterValue()
        {
            BaseTestSite.Assert.AreEqual(
                "parameter value",
                scriptAdapter.ReturnInputParameterValue("parameter value"), 
                "Script adapter should return the value of the first input parameter");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void ScriptAdapterReturnInputParameterNameAndType()
        {
            BaseTestSite.Assert.AreEqual(
                "inputParameter:System.String",
                scriptAdapter.ReturnInputParameterNameAndType("parameter value"), 
                "Script adapter should return the name and type of the first input parameter");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void ScriptAdapterReturnHelpText()
        {
            BaseTestSite.Assert.AreEqual(
                "Return Help text", 
                scriptAdapter.ReturnHelpText(), 
                "Script adapter should return help text");
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        [Description("Command \"set ptfprop\" can be used in the script to display all properties in the PTFconfig. " + 
            "It could be verified by checking the test case log.")]
        public void ScriptAdapterSetPtfProp()
        {
            scriptAdapter.SetPtfProp();
        }

        [TestMethod]
        [TestCategory("TestAdapter")]
        public void ScriptAdapterGetPtfProp()
        {
            string propName = "FeatureName";
            BaseTestSite.Assert.AreEqual(
                BaseTestSite.Properties[propName], 
                scriptAdapter.GetPtfProp(), 
                "Script adapter should return property value of {0}", propName);
        }
        #endregion
    }
}

// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.Protocols.TestTools.Messages.Runtime;
using Microsoft.Protocols.TestTools.UnitTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Protocols.TestTools.UnitTest.TestPtfTestClassBase
{
    /// <summary>
    /// Test cases to test PTF checker: BaseTestSite.Assert
    /// </summary>
    [TestClass]
    public class TestPtfTestClassBase : PtfTestClassBase
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            PtfTestClassBase.Initialize(testContext, "TestPtfTestClassBase");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            PtfTestClassBase.Cleanup();
        }

        [TestMethod]
        [TestCategory("PtfTestClassBase")]
        public void CheckAssertPassed()
        {
            this.Assert(true, "The value should be true.");
        }

        [TestMethod]
        [TestCategory("PtfTestClassBase")]
        public void CheckAssumePassed()
        {
            this.Assume(true, "The value should be true.");
        }

        [TestMethod]
        [TestCategory("PtfTestClassBase")]
        public void TestCheckpoint()
        {
            this.Checkpoint("The value should be true.");
        }

        [TestMethod]
        [TestCategory("PtfTestClassBase")]
        public void TestCommnet()
        {
            this.Comment("Comment test.");
        }

        [TestMethod]
        [TestCategory("PtfTestClassBase")]
        public void TestIsTrue()
        {
            this.Assert(this.IsTrue(1 == 1, string.Empty), "Assert true should pass");
            this.Assert(this.IsTrue(1 == 2, "test filter."), "BypassFilter should work and IsTrue should return true");
            this.TestSite.Assert.AreEqual<bool>(false, this.IsTrue(1 == 2, " invalid bypass filter."), "IsTrue should return false");
        }

        [TestMethod]
        [TestCategory("PtfTestClassBase")]
        public void TestSetSwitch()
        {
            TimeSpan defaultProceedControlTimeout = this.ProceedControlTimeout;
            defaultProceedControlTimeout.Add(new TimeSpan(1, 0, 0));
            this.SetSwitch("proceedcontroltimeout", "" + defaultProceedControlTimeout.TotalMilliseconds);
            this.TestSite.Assert.AreEqual<TimeSpan>(defaultProceedControlTimeout, this.ProceedControlTimeout, "ProceedControlTimeout should be same as defaultProceedControlTimeout");
        }

        [TestMethod]
        [TestCategory("PtfTestClassBase")]
        public void TestMake()
        {
            string testStringValue = "Test Value1";
            int testIntValue = (new Random()).Next(1, 100);
            bool testBooleanValue = true;

            TestMakeStruct make = this.Make<TestMakeStruct>(new string[] { "TestValueString", "TestValueInt", "TestValueBoolean" }, new object[] { testStringValue, testIntValue, testBooleanValue });
            this.TestSite.Assert.AreEqual(testStringValue, make.TestValueString, "TestValueString should be same after Make method invoked");
            this.TestSite.Assert.AreEqual(testIntValue, make.TestValueInt, "TestValueInt should be same after Make method invoked");
            this.TestSite.Assert.AreEqual(testBooleanValue, make.TestValueBoolean, "TestValueBoolean should be same after Make method invoked");
        }

        static System.Reflection.EventInfo OnTestEventInfo = TestManagerHelpers.GetEventInfo(typeof(ITestEventAdapter), "OnTestEvent");
        static System.Reflection.MethodBase TestMethodInfo = TestManagerHelpers.GetMethodInfo(typeof(ITestEventAdapter), "TestMethod", typeof(int));

        [TestMethod]
        [TestCategory("PtfTestClassBase")]
        public void TestExpectEventInfo()
        {
            this.InitializeTestManager();
            this.Manager.BeginTest("Test expect eventInfo");

            ITestEventAdapter ITestEventInstance = (ITestEventAdapter)this.GetAdapter(typeof(ITestEventAdapter));
            ITestEventInstance.OnTestEvent += ITestEventInstance_OnTestEvent;
            ITestEventInstance.TriggerEvent(1);
            int temp1 = this.Manager.ExpectEvent(this.QuiescenceTimeout, true, new ExpectedEvent(OnTestEventInfo, null, new OnTestEventHandler(this.TestEventCheck1)), new ExpectedEvent(OnTestEventInfo, null, new OnTestEventHandler(this.TestEventCheck2)));
            this.TestSite.Assert.AreEqual(0, temp1, "Expect TestEventCheck1 execute sucessfully");

            ITestEventInstance.TriggerEvent(2);
            int temp2 = this.Manager.ExpectEvent(this.QuiescenceTimeout, true, new ExpectedEvent(OnTestEventInfo, null, new OnTestEventHandler(this.TestEventCheck1)), new ExpectedEvent(OnTestEventInfo, null, new OnTestEventHandler(this.TestEventCheck2)));
            this.TestSite.Assert.AreEqual(1, temp2, "Expect TestEventCheck1 execute sucessfully");

            this.Manager.EndTest();
        }

        [TestMethod]
        [TestCategory("PtfTestClassBase")]
        public void TestExpectReturnMethodInfo()
        {
            this.InitializeTestManager();
            this.Manager.BeginTest("Test expect return methodInfo");

            ITestEventAdapter ITestEventInstance = (ITestEventAdapter)this.GetAdapter(typeof(ITestEventAdapter));
            int testValue = 1;
            ITestEventInstance.TestMethod(testValue);
            this.Manager.AddReturn(TestMethodInfo, null, testValue);
            int temp3 = this.Manager.ExpectReturn(this.QuiescenceTimeout, true, new ExpectedReturn(TestMethodInfo, null, new TestMethodDelegate(this.TestMethodCheck1)), new ExpectedReturn(TestMethodInfo, null, new TestMethodDelegate(this.TestMethodCheck2)));
            this.TestSite.Assert.AreEqual(0, temp3, "Expect TestMethodCheck1 execute sucessfully");

            testValue = 2;
            ITestEventInstance.TestMethod(testValue);
            this.Manager.AddReturn(TestMethodInfo, null, testValue);
            int temp4 = this.Manager.ExpectReturn(this.QuiescenceTimeout, true, new ExpectedReturn(TestMethodInfo, null, new TestMethodDelegate(this.TestMethodCheck1)), new ExpectedReturn(TestMethodInfo, null, new TestMethodDelegate(this.TestMethodCheck2)));
            this.TestSite.Assert.AreEqual(1, temp4, "Expect TestMethodCheck2 execute sucessfully");

            this.Manager.EndTest();
        }

        private void ITestEventInstance_OnTestEvent(int index)
        {
            this.Manager.Comment($"checking step \'event triggered, Index: {index}");
            this.Manager.AddEvent(OnTestEventInfo, this, new object[] { index });
        }

        private void TestEventCheck1(int testValue)
        {
            this.Manager.Comment("checking step TestEventCheck1");
            if (testValue != 1)
            {
                throw new TransactionFailedException("TestValue does not equals 1");
            }
        }

        private void TestEventCheck2(int testValue)
        {
            this.Manager.Comment("checking step TestEventCheck2");
            if (testValue != 2)
            {
                throw new TransactionFailedException("TestValue does not equals 2");
            }
        }

        private void TestMethodCheck1(int testValue)
        {
            this.Manager.Comment("checking step TestMethodCheck1");
            if (testValue != 1)
            {
                throw new TransactionFailedException("TestValue does not equals 1");
            }
        }

        private void TestMethodCheck2(int testValue)
        {
            this.Manager.Comment("checking step TestMethodCheck2");
            if (testValue != 2)
            {
                throw new TransactionFailedException("TestValue does not equals 2");
            }
        }
    }

    public delegate void OnTestEventHandler(int index);

    public delegate void TestMethodDelegate(int index);

    public interface ITestEventAdapter : IAdapter
    {
        event OnTestEventHandler OnTestEvent;

        void TriggerEvent(int index);

        void TestMethod(int testValue);
    }

    public class TestEventAdapter : ManagedAdapterBase, ITestEventAdapter
    {
        public event OnTestEventHandler OnTestEvent;

        public void TestMethod(int testValue)
        {
            Site.Log.Add(LogEntryKind.Debug, $"TestMethod execute, testValue: {testValue}");
        }

        public void TriggerEvent(int index)
        {
            if (OnTestEvent != null)
            {
                OnTestEvent(index);
            }
        }
    }
}

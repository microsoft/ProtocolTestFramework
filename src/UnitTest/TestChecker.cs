// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using Microsoft.Protocols.TestTools;
using Microsoft.Protocols.TestTools.Checking;
using Microsoft.Protocols.TestTools.UnitTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Protocols.TestTools.UnitTest.TestChecker
{
    /// <summary>
    /// A class used for testing.
    /// </summary>
    public class ClassForTest
    {
        public ClassForTest()
        {
        }
    }

    /// <summary>
    /// Test cases to test PTF checker: BaseTestSite.Assert
    /// </summary>
    [TestClass]
    public class TestChecker : TestClassBase
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            TestClassBase.Initialize(testContext, "TestChecker");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            TestClassBase.Cleanup();
        }

        [TestMethod]
        [TestCategory("TestChecker")]
        public void CheckAreEqualPassed()
        {
            BaseTestSite.Assert.AreEqual<int>(2, 2, "The two values should be the same.");
        }
        [TestMethod]
        [TestCategory("TestChecker")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.AreEqual failed. Expected: <2 (0x00000002)>, Actual: <3 (0x00000003)>. The two values should be the same.")]
        public void CheckAreEqualFailed()
        {
            BaseTestSite.Assert.AreEqual<int>(2, 3, "The two values should be the same.");
        }

        [TestMethod]
        [TestCategory("TestChecker")]
        public void CheckAreNotEqualPassed()
        {
            BaseTestSite.Assert.AreNotEqual<int>(2, 3, "The two values should be different.");
        }
        [TestMethod]
        [TestCategory("TestChecker")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.AreNotEqual failed. Expected: <2 (0x00000002)>, Actual: <2 (0x00000002)>. The two values should be different.")]
        public void CheckAreNotEqualFailed()
        {
            BaseTestSite.Assert.AreNotEqual<int>(2, 2, "The two values should be different.");
        }

        [TestMethod]
        [TestCategory("TestChecker")]
        public void CheckAreSamePassed()
        {
            ClassForTest object1 = new ClassForTest();
            ClassForTest object2 = object1;
            BaseTestSite.Assert.AreSame(object1, object2, "The two object references should be the same.");
        }
        [TestMethod]
        [TestCategory("TestChecker")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.AreSame failed. The two object references should be the same.")]
        public void CheckAreSameFailed()
        {
            ClassForTest object1 = new ClassForTest();
            ClassForTest object2 = new ClassForTest();
            BaseTestSite.Assert.AreSame(object1, object2, "The two object references should be the same.");
        }

        [TestMethod]
        [TestCategory("TestChecker")]
        public void CheckAreNotSamePassed()
        {
            ClassForTest object1 = new ClassForTest();
            ClassForTest object2 = new ClassForTest();
            BaseTestSite.Assert.AreNotSame(object1, object2, "The two object references should be different.");
        }
        [TestMethod]
        [TestCategory("TestChecker")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.AreNotSame failed. The two object references should be different.")]
        public void CheckAreNotSameFailed()
        {
            ClassForTest object1 = new ClassForTest();
            ClassForTest object2 = object1;
            BaseTestSite.Assert.AreNotSame(object1, object2, "The two object references should be different.");
        }

        [TestMethod]
        [TestCategory("TestChecker")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.Fail failed. This case should raise a failure message.")]
        public void CheckAssertFailed()
        {
            BaseTestSite.Assert.Fail("This case should raise a failure message.");
        }
        [TestMethod]
        [TestCategory("TestChecker")]
        public void CheckAssertPassed()
        {
            BaseTestSite.Assert.Pass("This case should raise a success message.");
        }
        [TestMethod]
        [TestCategory("TestChecker")]
        [PTFExpectedException(typeof(AssertInconclusiveException), "Assert.Inconclusive is inconclusive. The case should raise an inconclusive message.")]
        public void CheckAssertInconclusive()
        {
            BaseTestSite.Assert.Inconclusive("The case should raise an inconclusive message.");
        }

        [TestMethod]
        [TestCategory("TestChecker")]
        public void CheckIsTruePassed()
        {
            BaseTestSite.Assert.IsTrue(true, "The value should be true.");
        }
        [TestMethod]
        [TestCategory("TestChecker")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.IsTrue failed. The value should be true.")]
        public void CheckIsTrueFailed()
        {
            BaseTestSite.Assert.IsTrue(false, "The value should be true.");
        }

        [TestMethod]
        [TestCategory("TestChecker")]
        public void CheckIsFalsePassed()
        {
            BaseTestSite.Assert.IsFalse(false, "The value should be false.");
        }
        [TestMethod]
        [TestCategory("TestChecker")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.IsFalse failed. The value should be false.")]
        public void CheckIsFalseFailed()
        {
            BaseTestSite.Assert.IsFalse(true, "The value should be false.");
        }

        [TestMethod]
        [TestCategory("TestChecker")]
        public void CheckIsInstanceOfTypePassed()
        {
            string a = "123";
            BaseTestSite.Assert.IsInstanceOfType(a, typeof(System.String), "The object should be an instance of type: System.String.");
        }
        [TestMethod]
        [TestCategory("TestChecker")]
        [PTFExpectedException(typeof(AssertFailedException),
            "Assert.IsInstanceOfType failed. Expected Type: <System.String>, Actual Type: <System.Int32>. The object should be an instance of type: System.String.")]
        public void CheckIsInstanceOfTypeFailed()
        {
            int a = 123;
            BaseTestSite.Assert.IsInstanceOfType(a, typeof(System.String), "The object should be an instance of type: System.String.");
        }

        [TestMethod]
        [TestCategory("TestChecker")]
        public void CheckIsNotInstanceOfTypePassed()
        {
            int a = 123;
            BaseTestSite.Assert.IsNotInstanceOfType(a, typeof(System.String), "The object should not be an instance of type: System.String.");
        }
        [TestMethod]
        [TestCategory("TestChecker")]
        [PTFExpectedException(typeof(AssertFailedException),
            "Assert.IsNotInstanceOfType failed. Wrong Type: <System.String>, Actual Type: <System.String>. The object should not be an instance of type: System.String.")]
        public void CheckIsNotInstanceOfTypeFailed()
        {
            string a = "123";
            BaseTestSite.Assert.IsNotInstanceOfType(a, typeof(System.String), "The object should not be an instance of type: System.String.");
        }

        [TestMethod]
        [TestCategory("TestChecker")]
        public void CheckIsNullPassed()
        {
            BaseTestSite.Assert.IsNull(null, "The object reference should be null.");
        }
        [TestMethod]
        [TestCategory("TestChecker")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.IsNull failed. The object reference should be null.")]
        public void CheckIsNullFailed()
        {
            BaseTestSite.Assert.IsNull("notNull", "The object reference should be null.");
        }

        [TestMethod]
        [TestCategory("TestChecker")]
        public void CheckIsNotNullPassed()
        {
            BaseTestSite.Assert.IsNotNull("notNull", "The object reference should not be null.");
        }
        [TestMethod]
        [TestCategory("TestChecker")]
        [PTFExpectedException(typeof(AssertFailedException),
            "Assert.IsNotNull failed. The object reference should not be null.")]
        public void CheckIsNotNullFailed()
        {
            BaseTestSite.Assert.IsNotNull(null, "The object reference should not be null.");
        }

        [TestMethod]
        [TestCategory("TestChecker")]
        public void CheckIsSuccessPassed()
        {
            BaseTestSite.Assert.IsSuccess(1, "The given error code should be a successful result.");
        }
        [TestMethod]
        [TestCategory("TestChecker")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.IsSuccess failed. The given error code should be a successful result.")]
        public void CheckIsSuccessFailed()
        {
            BaseTestSite.Assert.IsSuccess(-1, "The given error code should be a successful result.");
        }

        [TestMethod]
        [TestCategory("TestChecker")]
        public void CheckUnverified()
        {
            BaseTestSite.Assert.Unverified("This could not be verified currently.");
        }

        [TestMethod]
        [TestCategory("TestChecker")]
        public void CheckErrors()
        {
            BaseTestSite.Assert.CheckErrors();
        }
    }
}

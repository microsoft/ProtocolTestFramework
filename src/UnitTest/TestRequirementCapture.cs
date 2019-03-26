// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Protocols.TestTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Protocols.TestTools.UnitTest.Utilities;

namespace Microsoft.Protocols.TestTools.UnitTest.TestRequirementCapture
{
    /// <summary>
    /// Test cases to test PTF RequirementCapture
    /// </summary>
    [TestClass]
    public class TestRequirementCapture : TestClassBase
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            TestClassBase.Initialize(testContext, "TestRequirementCapture");
            BaseTestSite.DefaultProtocolDocShortName = "TestRequirementCapture";
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            TestClassBase.Cleanup();
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        public void CaptureRequirementAddRequirement()
        {
            BaseTestSite.Log.Add(
                LogEntryKind.Checkpoint,
                RequirementId.Make(BaseTestSite.DefaultProtocolDocShortName, 0, "Add a requirement."));
            BaseTestSite.CaptureRequirement(0, "Add a requirement.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        public void CaptureRequirementAreEqualPassed()
        {
            BaseTestSite.CaptureRequirementIfAreEqual(1, 1, 1, "The two values should be equal.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.AreEqual failed on requirement TestRequirementCapture_R2. " +
            "Expected: <1 (0x00000001)>, Actual: <2 (0x00000002)>. The two values should be equal.")]
        public void CaptureRequirementAreEqualFailed()
        {
            BaseTestSite.CaptureRequirementIfAreEqual(1, 2, 2, "The two values should be equal.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        public void CaptureRequirementAreSamePassed()
        {
            BaseTestSite.CaptureRequirementIfAreSame("a", "a", 3, "The two strings should be the same.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.AreSame failed on requirement TestRequirementCapture_R4. The two strings should be the same.")]
        public void CaptureRequirementAreSameFailed()
        {
            BaseTestSite.CaptureRequirementIfAreSame("a", "b", 4, "The two strings should be the same.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        public void CaptureRequirementAreNotSamePassed()
        {
            BaseTestSite.CaptureRequirementIfAreNotSame("a", "b", 5, "The two strings should not be the same.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.AreNotSame failed on requirement TestRequirementCapture_R6. The two strings should not be the same.")]
        public void CaptureRequirementAreNotSameFailed()
        {
            BaseTestSite.CaptureRequirementIfAreNotSame("a", "a", 6, "The two strings should not be the same.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        public void CaptureRequirementIfTruePassed()
        {
            BaseTestSite.CaptureRequirementIfIsTrue(true, 7, "The value should be true.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.IsTrue failed on requirement TestRequirementCapture_R8. The value should be true.")]
        public void CaptureRequirementIfTrueFailed()
        {
            BaseTestSite.CaptureRequirementIfIsTrue(false, 8, "The value should be true.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        public void CaptureRequirementIfFalsePassed()
        {
            BaseTestSite.CaptureRequirementIfIsFalse(false, 9, "The value should be false.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.IsFalse failed on requirement TestRequirementCapture_R10. The value should be false.")]
        public void CaptureRequirementIfFalseFailed()
        {
            BaseTestSite.CaptureRequirementIfIsFalse(true, 10, "The value should be false.");
        }


        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        public void CaptureRequirementIsInstanceOfPassed()
        {
            BaseTestSite.CaptureRequirementIfIsInstanceOfType(
                2,
                typeof(System.Int32),
                11,
                "The type of the instance should be System.Int32.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.IsInstanceOfType failed on requirement TestRequirementCapture_R12. " +
            "Expected Type: <System.String>, Actual Type: <System.Int32>. The type of the instance should be System.String.")]
        public void CaptureRequirementIsInstanceOfFailed()
        {
            BaseTestSite.CaptureRequirementIfIsInstanceOfType(
                2,
                typeof(System.String),
                12,
                "The type of the instance should be System.String.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        public void CaptureRequirementIsNotInstanceOfPassed()
        {
            BaseTestSite.CaptureRequirementIfIsNotInstanceOfType(
                2,
                typeof(System.String),
                13,
                "The type of the instance should not be System.String.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.IsNotInstanceOfType failed on requirement TestRequirementCapture_R14. " +
            "Wrong Type: <System.Int32>, Actual Type: <System.Int32>. The type of the instance should not be System.Int32.")]
        public void CaptureRequirementIsNotInstanceOfFailed()
        {
            BaseTestSite.CaptureRequirementIfIsNotInstanceOfType(
                2,
                typeof(System.Int32),
                14,
                "The type of the instance should not be System.Int32.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        public void CaptureRequirementIsNotNullPassed()
        {
            BaseTestSite.CaptureRequirementIfIsNotNull("123", 15, "The value should not be null.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.IsNotNull failed on requirement TestRequirementCapture_R16. The value should not be null.")]
        public void CaptureRequirementIsNotNullFailed()
        {
            BaseTestSite.CaptureRequirementIfIsNotNull(null, 16, "The value should not be null.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        public void CaptureRequirementIsNullPassed()
        {
            BaseTestSite.CaptureRequirementIfIsNull(null, 17, "The value should be null.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.IsNull failed on requirement TestRequirementCapture_R18. The value should be null.")]
        public void CaptureRequirementIsNullFailed()
        {
            BaseTestSite.CaptureRequirementIfIsNull("123", 18, "The value should be null.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        public void CaptureRequirementIsSuccessPassed()
        {
            BaseTestSite.CaptureRequirementIfIsSuccess(1, 19, "The HRESULT value should be success");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [PTFExpectedException(typeof(AssertFailedException), "Assert.IsSuccess failed on requirement TestRequirementCapture_R20. The HRESULT value should be success.")]
        public void CaptureRequirementIsSuccessFailed()
        {
            BaseTestSite.CaptureRequirementIfIsSuccess(-1, 20, "The HRESULT value should be success.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [Description("The test case will not fail because SkipMAYRequirements is set to true in PTFConfig")]
        public void CaptureRequirementSkipMayRequirement()
        {
            BaseTestSite.CaptureRequirementIfIsTrue(false, 21, "The value should be true.", RequirementType.May);
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [Description("The test case will not fail because SkipMUSTRequirements is set to true in PTFConfig")]
        public void CaptureRequirementSkipMustRequirement()
        {
            BaseTestSite.CaptureRequirementIfIsTrue(false, 22, "The value should be true.", RequirementType.Must);
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [Description("The test case will not fail because SkipSHOULDRequirements is set to true in PTFConfig")]
        public void CaptureRequirementSkipShouldRequirement()
        {
            BaseTestSite.CaptureRequirementIfIsTrue(false, 23, "The value should be true.", RequirementType.Should);
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [Description("The test case will not fail because SkipPRODUCTRequirements is set to true in PTFConfig")]
        public void CaptureRequirementSkipProductRequirement()
        {
            BaseTestSite.CaptureRequirementIfIsTrue(false, 24, "The value should be true.", RequirementType.Product);
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [Description("The expected value is reclaimed to 1 in PTFConfig")]
        public void CaptureRequirementExpectedValueReclaimed()
        {
            BaseTestSite.CaptureRequirementIfAreEqual(2, 1, 25, "The two values should be equal.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [Description("The case will not fail because the two requirements are set as ExceptionalRequirements in PTFConfig")]
        public void CaptureRequirementSkipExceptionalRequirements()
        {
            BaseTestSite.CaptureRequirementIfAreEqual(2, 1, 26, "The two values should be equal.");
            BaseTestSite.CaptureRequirementIfIsTrue(false, 27, "The value should be true.");
        }

        [TestMethod]
        [TestCategory("TestRequirementCapture")]
        [Description("An exception should be thrown if the same requirement id is used more than once but with different description.")]
        [PTFExpectedException(typeof(InvalidOperationException))]
        public void CaptureRequirementSameRequirementIdDifferentDescription()
        {
            BaseTestSite.CaptureRequirementIfAreEqual(1, 1, 28, "The two values should be equal.");
            BaseTestSite.CaptureRequirementIfIsTrue(true, 28, "The value should be true.");
        }
    }
}

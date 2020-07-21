// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// An interface which provides logging, assertions, and SUT adapters for test code onto its execution context.
    /// </summary>
    /// <remarks>
    /// This interface constitutes the anchor for the communication of test code with its execution context.
    /// It provides information about the currently executing test, the logger to use, the assertion verifier to use,
    /// and the adapters to the underlying SUT. It also provides generic access to properties defined in the test
    /// configuration.
    /// </remarks>
    public interface ITestSite
    {

        /// <summary>
        /// Gets the collection of properties taken from the test configuration. The content
        /// of the collection is not guranteed especially it can be used for passing configuration
        /// information to the test suite.
        /// </summary>
        NameValueCollection Properties { get; }

        /// <summary>
        /// Gets or sets the default document short name.
        /// The property is used to store the document(protocol) short name to make requirements ID.
        /// </summary>
        string DefaultProtocolDocShortName { get; set; }

        /// <summary>
        /// Gets the name of the feature (protocol) being tested.
        /// </summary>
        string FeatureName { get; }

        /// <summary>
        /// Gets the name of the executing test provided by configuration file.
        /// </summary>
        string TestName { get; }

        /// <summary>
        /// Gets the name of the test suite.
        /// </summary>
        string TestSuiteName { get; }

        /// <summary>
        /// Gets the logger associated with this test site.
        /// </summary>
        ILogger Log { get; }

        /// <summary>
        /// Gets the checker for verifying the test execution results associated with this test site.
        /// </summary>
        IChecker Assert { get; }

        /// <summary>
        /// Gets the checker for verifying the environment associated with this test site.
        /// </summary>
        IChecker Assume { get; }

        /// <summary>
        /// Gets the checker for verifying the test case code associated with this test site.
        /// </summary>
        IChecker Debug { get; }

        /// <summary>
        /// Returns an adapter implementation for the given adapter interface type.
        /// </summary>
        /// <typeparam name="T">The adapter interface type</typeparam>
        /// <returns>An adapter instance of the given type</returns>
        T GetAdapter<T>() where T : IAdapter;

        /// <summary>
        /// Returns an adapter implementation for the given adapter interface type.
        /// </summary>
        /// <typeparam name="adapterType">The adapter interface type.</typeparam>
        /// <returns>An adapter of the given type.</returns>
        IAdapter GetAdapter(Type adapterType);

        /// <summary>
        /// Reports the error message to Tcm.
        /// This method is obsolete.
        /// </summary>
        /// <param name="formatString">A composite format string.</param>
        /// <param name="parameters">An Object array containing one or more objects to format.</param>
        void ReportAsyncErrorToTcm(string formatString, params object[] parameters);

        /// <summary>
        /// TestStarted Event which is raised before a test is going to start. <see cref="TestStartFinishEventArgs"/>
        /// </summary>
        event EventHandler<TestStartFinishEventArgs> TestStarted;

        /// <summary>
        /// TestFinished Event which is raised after a test is finished. <see cref="TestStartFinishEventArgs"/>
        /// </summary>
        event EventHandler<TestStartFinishEventArgs> TestFinished;

        /// <summary>
        /// Checks errors in each checker.
        /// </summary>
        void CheckErrors();

        /// <summary>
        /// Initializes all checker instances
        /// </summary>
        /// <param name="checkers">All checkers need to register into test site</param>
        void RegisterCheckers(IDictionary<CheckerKinds, IChecker> checkers);

        #region Capture Requirement APIs

        /// <summary>
        /// Logs requirement as a checkpoint
        /// </summary>
        /// <param name="protocolDocShortName">User provide protocol short name</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirement(string protocolDocShortName, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the actual value is equal to the expected value. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <typeparam name="T">The type of values for check</typeparam>
        /// <param name="expected">Expect value</param>
        /// <param name="actual">Actual value</param>
        /// <param name="protocolDocShortName">User provide protocol short name</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product)</param>
        void CaptureRequirementIfAreEqual<T>(
            T expected, T actual,
            string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the actual value is not equal to the expected value. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <typeparam name="T">The type of values for check</typeparam>
        /// <param name="expected">Expect value</param>
        /// <param name="actual">Actual value</param>
        /// <param name="protocolDocShortName">User provide protocol short name</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfAreNotEqual<T>(
            T expected, T actual,
            string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the actual value is the same as the expected value. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="expected">The expected object</param>
        /// <param name="actual">The actual object</param>
        /// <param name="protocolDocShortName">User provide protocol short name</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfAreSame(
            object expected, object actual,
            string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the actual value is not the same as the expected value. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="expected">The expected object</param>
        /// <param name="actual">The actual object</param>
        /// <param name="protocolDocShortName">User provide protocol short name</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfAreNotSame(
            object expected, object actual,
            string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the boolean condition is true. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="condition">The boolean expression of the condition</param>
        /// <param name="protocolDocShortName">User provide protocol short name</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfIsTrue(
            bool condition, string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the boolean condition is false. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="condition">The boolean expression of the condition</param>
        /// <param name="protocolDocShortName">User provide protocol short name</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfIsFalse(
            bool condition, string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the value is null. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="value">The object value that needs to be checked.</param>
        /// <param name="protocolDocShortName">User provide protocol short name</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfIsNull(
            object value, string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the value is not null. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="value">The object value that needs to be checked.</param>
        /// <param name="protocolDocShortName">User provide protocol short name</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfIsNotNull(
            object value, string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the value is an instance of the expected type. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="value">Instance value</param>
        /// <param name="type">The expected instance type</param>
        /// <param name="protocolDocShortName">User provide protocol short name</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfIsInstanceOfType(
            object value, Type type,
            string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the value is not an instance of the expected type. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="value">Instance value</param>
        /// <param name="type">The expected instance type</param>
        /// <param name="protocolDocShortName">User provide protocol short name</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfIsNotInstanceOfType(
            object value, Type type,
            string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the HRESULT value is success. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="handle">The HRESULT value to check</param>
        /// <param name="protocolDocShortName">User provide protocol short name</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfIsSuccess(
            int handle, string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Logs requirement as unverified.
        /// </summary>
        /// <param name="protocolDocShortName">User provide protocol short name</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        void UnverifiedRequirement(string protocolDocShortName, int requirementId, string description);

        /// <summary>
        /// Logs requirement as a checkpoint.
        /// </summary>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirement(int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the actual value is equal to the expected value. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <typeparam name="T">The type of values for check</typeparam>
        /// <param name="expected">Expect value</param>
        /// <param name="actual">Actual value</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfAreEqual<T>(
            T expected, T actual,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the actual value is not equal to the expected value. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <typeparam name="T">The type of values for check</typeparam>
        /// <param name="expected">Expect value</param>
        /// <param name="actual">Actual value</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfAreNotEqual<T>(
            T expected, T actual,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the actual value is the same as the expected value. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="expected">The expected object</param>
        /// <param name="actual">The actual object</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfAreSame(
            object expected, object actual,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the actual value is not the same as the expected value. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="expected">The expected object</param>
        /// <param name="actual">The actual object</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfAreNotSame(
            object expected, object actual,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the boolean condition is true. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="condition">The boolean expression of the condition</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfIsTrue(bool condition, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the boolean condition is false. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="condition">The boolean expression of the condition</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfIsFalse(bool condition, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the value is null. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="value">The object value that needs to be checked.</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfIsNull(object value, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the value is not null. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="value">The object value that needs to be checked.</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfIsNotNull(object value, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the value is an instance of the expected type. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="value">Instance value</param>
        /// <param name="type">The expected instance type</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfIsInstanceOfType(
            object value, Type type,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the value is not an instance of the expected type. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="value">Instance value</param>
        /// <param name="type">The expected instance type</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfIsNotInstanceOfType(
            object value, Type type,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Verifies that the HRESULT value is success. Capture a requirement if the verification succeeds.
        /// </summary>
        /// <param name="handle">The HRESULT value to check</param>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        /// <param name="requirementType">Type of the requirement(May, Should, Must or Product).</param>
        void CaptureRequirementIfIsSuccess(int handle, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined);

        /// <summary>
        /// Logs requirement as unverified.
        /// </summary>
        /// <param name="requirementId">The unique requirement number</param>
        /// <param name="description">The description of requirement</param>
        void UnverifiedRequirement(int requirementId, string description);

        #endregion

        /// <summary>
        /// Indicates the statistics of the results of the executed test cases
        /// </summary>
        Dictionary<PtfTestOutcome, int> TestResultsStatistics { get; }

        /// <summary>
        /// Gets the runtime test related properties, such as current test case name.
        /// </summary>
        Dictionary<string, Object> TestProperties { get; }

        /// <summary>
        /// Gets the interface to query test configuration data
        /// </summary>
        IConfigurationData Config { get; }

        /// <summary>
        /// Gets the current test assembly name.
        /// </summary>
        string TestAssemblyName { get; }
    }

    /// <summary>
    /// A class which represents the event args used by <see cref="ITestSite.TestStarted"/> and <see cref="ITestSite.TestFinished"/>.
    /// </summary>
    public class TestStartFinishEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        protected TestStartFinishEventArgs() { }

        /// <summary>
        /// Constructs a TestStartFinishEventArgs instance which contains the test case name.
        /// </summary>
        /// <param name="testName">The current test case name.</param>
        public TestStartFinishEventArgs(string testName)
        {
            this.testCaseName = testName;
            this.testOutcome = PtfTestOutcome.Unknown;
        }

        /// <summary>
        /// Constructs a TestStartFinishEventArgs instance which contains the test case name.
        /// </summary>
        /// <param name="testName">The current test case name.</param>
        /// <param name="testOutcome">The current test outcome.</param>
        public TestStartFinishEventArgs(string testName, PtfTestOutcome testOutcome)
        {
            this.testCaseName = testName;
            this.testOutcome = testOutcome;
        }

        private string testCaseName;

        /// <summary>
        /// Gets the current test case name.
        /// </summary>
        public string TestName
        {
            get
            {
                return testCaseName;
            }
        }

        private PtfTestOutcome testOutcome;

        /// <summary>
        /// Gets the current test case outcome.
        /// </summary>
        public PtfTestOutcome TestOutcome
        {
            get
            {
                return testOutcome;
            }
        }
    }

    /// <summary>
    /// A class which defines the test property names
    /// </summary>
    public static class TestPropertyNames
    {
        /// <summary>
        /// Const string represents the current test case name
        /// </summary>
        public const string CurrentTestCaseName = "CurrentTestCaseName";

        /// <summary>
        /// Const string represents the current test outcome
        /// </summary>
        public const string CurrentTestOutcome = "CurrentTestOutcome";
    }

    /// <summary>
    /// Requirement type
    /// </summary>
    public enum RequirementType {
        /// <summary>
        /// Must requirement
        /// </summary>
        Must,
        /// <summary>
        /// Should requirement
        /// </summary>
        Should,
        /// <summary>
        /// May requirement
        /// </summary>
        May,
        /// <summary>
        /// Product behavior
        /// </summary>
        Product,
        /// <summary>
        /// Default requirement type
        /// </summary>
        Undefined,
    }
}

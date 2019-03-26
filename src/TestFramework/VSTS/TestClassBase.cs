// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Protocols.TestTools.Checking;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// A base test class for VSTS
    /// </summary>
    [TestClass]
    public abstract class TestClassBase
    {
        private TestContext context;
        private ITestSite testSite;
        private IProtocolTestContext ptfTestContext;
        private IProtocolTestNotify ptfTestNotify;
        //private Type testClass;
        private string testSuiteName;
        // Holds list of test cases for which the netmon trace should be captured
        private static List<string> selectedTestCases = new List<string>();
        // Holds the start time of the testsuite execution
        private static DateTime executionStartTime;
        // Holds the end time of the testsuite execution
        private static DateTime executionEndTime;
        private static Dictionary<Type, string> suiteNameCache = new Dictionary<Type, string>();

        private static int classCount;
        private static bool isUseDefaultSuiteName;
        private static string staticTestSuiteName;
        private static ITestSite baseTestSite;

        /// <summary>
        /// Constructor uses the default test suit name
        /// </summary>
        protected TestClassBase()
        {

            IProtocolTestsManager manager = ProtocolTestsManagerFactory.TestsManager;

            if (!suiteNameCache.ContainsKey(this.GetType()))
            {
                suiteNameCache[this.GetType()] = staticTestSuiteName;
            }

            //switch test site while test running
            testSuiteName = suiteNameCache[this.GetType()];
            testSite = manager.GetTestSite(testSuiteName);
            ptfTestNotify = manager.GetProtocolTestNotify(testSuiteName);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configPath">Configuration path</param>
        /// <param name="testSuiteName">Test suite name</param>
        protected TestClassBase(string configPath, string testSuiteName)
        {
        }

        #region VSTS properties and methods

        /// <summary>
        /// Only for internal use. This property is to support the VSTS infrastructure.
        /// Don't use this property in your test code.
        /// </summary>
        public TestContext TestContext
        {
            get
            {
                return context;
            }
            set
            {
                context = value;

                UpdaProtocolTestContext(context);
            }
        }

        /// <summary>
        /// Only for internal use. This property is to support the SE infrastructure.
        /// Don't use this property in your test code.
        /// </summary>
        protected virtual ITestSite Site
        {
            get
            {
                return testSite;
            }
            set
            {
                testSite = value;
                RegisterChecker(testSite);
            }
        }

        /// <summary>
        /// The base test site
        /// </summary>
        public static ITestSite BaseTestSite
        {
            get
            {
                if (baseTestSite == null)
                {
                    throw new InvalidOperationException(
                        "Test site is not initialized, please initialize it before test run.");
                }
                return baseTestSite;
            }
        }

        #endregion

        #region TSD methods
        /// <summary>
        /// Test initialize
        /// </summary>
        [ProtocolTestInitialize]
        protected virtual void TestInitialize()
        {
        }

        /// <summary>
        /// Test clean up
        /// </summary>
        [ProtocolTestCleanup]
        protected virtual void TestCleanup()
        {
        }

        #endregion

        #region TCM methods

        /// <summary>
        /// Only for internal use. TCM must set this property
        /// before setup method (with the [Setup] attribute)
        /// and update it during tests run.
        /// </summary>
        public IProtocolTestContext ProtocolTestContext
        {
            get
            {
                return ptfTestContext;
            }
            set
            {
                ptfTestContext = value;
            }
        }

        /// <summary>
        /// Only for internal use. Test method level initialization.
        /// TCM must call this method before each
        /// test method is executed.
        /// </summary>
        [TestInitialize]
        public void BaseTestInitialize()
        {
            //on test start
            string testName = this.GetType().FullName + "." + ProtocolTestContext.TestMethodName;
            this.ptfTestNotify.OnTestStarted(this, testName, ProtocolTestContext.TestOutcome, AssertExceptionHandler);
        }

        /// <summary>
        /// Only for internal use. Test method level cleanup action.
        /// TCM must call this method after each
        /// test method is executed.
        /// </summary>
        [TestCleanup]
        public void BaseTestCleanup()
        {
            //on test finished
            string testName = this.GetType().FullName + "." + ProtocolTestContext.TestMethodName;
            this.ptfTestNotify.OnTestFinished(this, testName, ProtocolTestContext.TestOutcome, AssertExceptionHandler);
        }

        /// <summary>
        /// Initializes the test suite base class.
        /// This method must be called by class initialize method in your test class.
        /// </summary>
        /// <remarks>
        /// The calling assembly name is used as test suite name.
        /// </remarks>
        /// <param name="testContext">VSTS test context.</param>
        public static void Initialize(TestContext testContext)
        {
            Assembly CallingAssembly = Assembly.GetCallingAssembly();
            if (CallingAssembly.GetType(testContext.FullyQualifiedTestClassName) != null)
            {
                staticTestSuiteName = Assembly.GetCallingAssembly().GetName().Name;
                isUseDefaultSuiteName = true;
            }
            else
            {
                // If PTF can not get the correct calling assembly name, find the assembly using class name.
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly.GetType(testContext.FullyQualifiedTestClassName) != null)
                    {
                        staticTestSuiteName = assembly.GetName().Name;
                        isUseDefaultSuiteName = true;
                        break;
                    }
                }
            }
            Initialize(testContext, staticTestSuiteName);
        }

        /// <summary>
        /// Initializes the test suite base class with explicitly given test suite name.
        /// This method must be called by class initialize method in your test class.
        /// </summary>
        /// <param name="testContext">VSTS test context.</param>
        /// <param name="testSuiteName">The name of the test suite. The test site uses this name to find configuration files.</param>
        public static void Initialize(TestContext testContext, string testSuiteName)
        {
            executionStartTime = DateTime.Now;
            if (testContext == null)
            {
                throw new InvalidOperationException("TestContext should not be null in UnitTestClassBase.");
            }
            classCount++;
            staticTestSuiteName = testSuiteName;
            IProtocolTestsManager manager = ProtocolTestsManagerFactory.TestsManager;

            if (null == manager.GetTestSite(staticTestSuiteName))
            {
                string testAssemblyName;
                IConfigurationData config = null;

                string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
                string testDeploymentDir = Path.GetDirectoryName(assemblyPath);

                config = ConfigurationDataProvider.GetConfigurationData(
                    testDeploymentDir, testSuiteName);

                if (isUseDefaultSuiteName)
                {
                    testAssemblyName = testSuiteName;
                    isUseDefaultSuiteName = false;
                }
                else
                {
                    testAssemblyName = Assembly.GetCallingAssembly().GetName().Name;
                }

                manager.Initialize(config, new VstsTestContext(testContext), testSuiteName, testAssemblyName);

                baseTestSite = manager.GetTestSite(testSuiteName);

                ITestSite site = manager.GetTestSite(testSuiteName);

                //registry all checkers
                RegisterChecker(site);
            }
            else
            {
                baseTestSite = manager.GetTestSite(testSuiteName);
            }


            /********************* Display expected runtime of the testsuite **********************
             * Log expected execution time of the test suite in the log file                      *
             **************************************************************************************/
            baseTestSite.Log.Add(LogEntryKind.Comment, "Expected execution time of the test suite (in seconds) is: " + baseTestSite.Properties.Get("ExpectedExecutionTime"));
        }

        /// <summary>
        /// Cleans up the test suite.
        /// User must call this method in ClassCleanup method.
        /// </summary>
        public static void Cleanup()
        {
            classCount--;
            if (classCount == 0)
            {
                /********************* Display expected runtime of the testsuite **************************
                 * Calculates the actual time taken for the test suite execution and logs it in log file  *
                 ******************************************************************************************/
                executionEndTime = DateTime.Now;
                double actualExecutionTime;
                actualExecutionTime = executionEndTime.Subtract(executionStartTime).TotalSeconds;

                baseTestSite.Log.Add(LogEntryKind.Comment, "Actual time taken for the test suite execution (in seconds) is: " + actualExecutionTime);

                ProtocolTestsManagerFactory.TestsManager.TestsRunCleanup();
            }
        }

        #endregion

        #region private methods

        private PtfTestOutcome AssertExceptionHandler(Exception exception)
        {
            Exception checkerException = exception;
            if (exception.InnerException != null)
            {
                checkerException = exception.InnerException;
            }
            //catch exception and updates the test result statistics
            if (checkerException.GetType() == typeof(AssertFailedException))
            {
                return PtfTestOutcome.Failed;
            }
            else if (checkerException.GetType() == typeof(AssertInconclusiveException))
            {
                return PtfTestOutcome.Inconclusive;
            }
            return PtfTestOutcome.Unknown;
        }

        private void UpdaProtocolTestContext(TestContext context)
        {
            if (this.ProtocolTestContext == null)
            {
                this.ProtocolTestContext = new VstsTestContext(context);
            }
            else
            {
                //update protocol test context
                VstsTestContext vsContext = this.ProtocolTestContext as VstsTestContext;
                vsContext.Update(context);
                this.ProtocolTestContext = vsContext;
            }
        }

        private static void EnsureOriginalAttributesNotUsed(Type derivedTestClass)
        {
            if (derivedTestClass == null)
            {
                throw new InvalidOperationException("Cannot get the test classes.");
            }

            Type currentType = derivedTestClass;
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            while (currentType != null)
            {
                //skip the base class
                if (currentType == typeof(TestClassBase))
                {
                    break;
                }

                foreach (MemberInfo mi in currentType.GetMethods(flags))
                {
                    if (Attribute.GetCustomAttribute(mi, typeof(TestInitializeAttribute), false) != null)
                    {
                        throw new InvalidOperationException("The test initialize attribute in VSTS unit test framework is not required by PTF. " +
                            "Please use ProtocolTestInitializeAttribute under Microsoft.Protocols.TestTools namespace instead.");
                    }
                    else if (Attribute.GetCustomAttribute(mi, typeof(TestCleanupAttribute), false) != null)
                    {
                        throw new InvalidOperationException("The test cleanup attribute in VSTS unit test framework is not required by PTF. " +
                            "Please use ProtocolTestCleanupAttribute under Microsoft.Protocols.TestTools namespace instead.");
                    }
                }

                currentType = currentType.BaseType;
            }
        }

        private static void RegisterChecker(ITestSite testSite)
        {
            IDictionary<CheckerKinds, IChecker> checkers = new Dictionary<CheckerKinds, IChecker>();
            ICheckerConfig checkerConfig;
            if (ConfigurationDataProvider.TryGetCheckerConfig<ICheckerConfig>(out checkerConfig))
            {
                IChecker assertChecker = VsCheckerFactory.GetChecker(CheckerKinds.AssertChecker, testSite, checkerConfig);
                IChecker assumeChecker = VsCheckerFactory.GetChecker(CheckerKinds.AssumeChecker, testSite, checkerConfig);
                IChecker debugChecker = VsCheckerFactory.GetChecker(CheckerKinds.DebugChecker, testSite, checkerConfig);
                checkers.Add(CheckerKinds.AssertChecker, assertChecker);
                checkers.Add(CheckerKinds.AssumeChecker, assumeChecker);
                checkers.Add(CheckerKinds.DebugChecker, debugChecker);
                testSite.RegisterCheckers(checkers);
            }
            else
            {
                throw new InvalidOperationException("Cannot retrieve the checker configuration from configuration data.");
            }
        }

        #endregion
    }
}

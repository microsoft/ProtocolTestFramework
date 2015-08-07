// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Protocols.TestTools;
using Microsoft.Protocols.TestTools.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Protocols.TestTools.Checking;
using System.Reflection;
using System.Runtime.InteropServices;

using System.Collections.ObjectModel;
using System.Globalization;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// A base test class for VSTS
    /// </summary>
    [TestClass]
    public abstract class TestClassBase
    {
        [DllImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true,
                CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int AllocConsole();

        [DllImport("kernel32.dll", EntryPoint = "GetConsoleWindow", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", EntryPoint = "IsWindowVisible", CharSet = CharSet.Auto)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "ShowWindow", CharSet = CharSet.Auto)]
        private static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

        private TestContext context;        
        private ITestSite testSite;
        private IProtocolTestContext ptfTestContext;
        private IProtocolTestNotify ptfTestNotify;
        //private Type testClass;
        private string testSuiteName;
        // Instance of AutoCapture class
        private static IAutoCapture autoCapture;
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
            if (testSite == null)
            {
                StringBuilder errorMsg = new StringBuilder();
                errorMsg.AppendFormat("Cannot get the test site {0}.", testSuiteName)
                    .AppendLine()
                    .Append("If you are running test suite from Visual Studio, please make sure that the TestSettings file is selected in TEST\\Test Settings menu.");
                throw new InvalidOperationException(errorMsg.ToString());
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configPath">Configuration path</param>
        /// <param name="testSuiteName">Test suite name</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
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
            Microsoft.Protocols.TestTools.ExtendedLogging.ExtendedLoggerConfig.CaseName = ProtocolTestContext.TestMethodName;
            this.ptfTestNotify.OnTestStarted(this, testName, ProtocolTestContext.TestOutcome, AssertExceptionHandler);
            try
            {
                if (autoCapture != null) autoCapture.StartCapture(ProtocolTestContext.TestMethodName);
            }
            catch (AutoCaptureException e)
            {
                baseTestSite.Log.Add(LogEntryKind.Warning, "Auto capture start capture Error: " + e.Message);
                if (e.StopRunning) throw;
            }
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
            try
            {
                if (autoCapture != null) autoCapture.StopCapture();
            }
            catch (AutoCaptureException e)
            {
                baseTestSite.Log.Add(LogEntryKind.Warning, "Auto capture cleanup Error: " + e.Message);
                if (e.StopRunning) throw;
            }

            Microsoft.Protocols.TestTools.ExtendedLogging.ExtendedLoggerConfig.CaseName = "N/A";
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

            Microsoft.Protocols.TestTools.ExtendedLogging.ExtendedLoggerConfig.TestSuiteName = testSuiteName;
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
                IConfigurationData config = ConfigurationDataProvider.GetConfigurationData(
                    testContext.TestDeploymentDir, testSuiteName);
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

                if (IsCommandlineConsoleRulePresent(config.Profiles))
                {
                    AllocConsole();
                    IntPtr hWnd = GetConsoleWindow();
                    bool visible = IsWindowVisible(hWnd);
                    if (!visible) ShowWindow(hWnd, 9); // 9 SW_RESTORE. Make the console visible.
                    Console.WriteLine("Test Results:");
                    Console.WriteLine("==============");
                    string consoleWidth = baseTestSite.Properties.Get("ConsoleWidth");
                    if (consoleWidth != null) Console.WindowWidth = Convert.ToInt32(consoleWidth);
                    string consoleHeight = baseTestSite.Properties.Get("ConsoleHeight");
                    if (consoleHeight != null) Console.WindowHeight = Convert.ToInt32(consoleHeight);
                    string consoleBufferHeight = baseTestSite.Properties.Get("ConsoleBufferHeight");
                    if (consoleBufferHeight != null) Console.BufferHeight = Convert.ToInt32(consoleBufferHeight);
                }
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

            //************* Automatic network message capture.*************
            if (Convert.ToBoolean(baseTestSite.Properties.Get("PTF.NetworkCapture.Enabled")))
            {
                string assemblyFile = baseTestSite.Properties.Get("PTF.NetworkCapture.Assembly");
                if (assemblyFile == null)
                {
                    // Use logman to capture by default.
                    autoCapture = new LogmanCapture();
                }
                else
                {
                    Assembly assembly = Assembly.LoadFrom(assemblyFile);
                    string className = baseTestSite.Properties.Get("PTF.NetworkCapture.Class");
                    if (className != null)
                    {
                        autoCapture = (IAutoCapture)assembly.CreateInstance(className);
                    }
                    else
                    {
                        foreach (Type type in assembly.GetTypes())
                        {
                            if (type.IsClass && typeof(IAutoCapture).IsAssignableFrom(type))
                            {
                                autoCapture = (IAutoCapture)Activator.CreateInstance(type);
                            }
                        }
                    }
                }
                try
                {
                    if (autoCapture != null) autoCapture.Initialize(baseTestSite.Properties, testSuiteName);
                }
                catch (AutoCaptureException e)
                {
                    baseTestSite.Log.Add(LogEntryKind.Warning, "Auto capture initialize Error: " + e.Message);
                    if (e.StopRunning) throw;
                }
            }
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
                //************* Automatic network message capture.*************
                if (autoCapture != null)
                {
                    try
                    {
                        autoCapture.Cleanup();
                    }
                    catch (AutoCaptureException e)
                    {
                        baseTestSite.Log.Add(LogEntryKind.Warning, "Auto capture cleanup Error: " + e.Message);
                        if (e.StopRunning) throw;
                    }
                    autoCapture = null;
                }

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

        private static bool IsCommandlineConsoleRulePresent(Collection<ProfileConfig> profiles)
        {
            foreach (ProfileConfig profile in profiles)
            {
                Collection<ProfileRuleConfig> rules = profile.Rules;
                foreach (ProfileRuleConfig rule in rules)
                {
                    // if there is at least 1 rule that uses "CommandlineConsole" sink which is not a deletion entry
                    // delete=true implies the rule is deleted. So a rule with delete=true need not be counted
                    switch (rule.Sink.ToLower())
                    {
                        case "redconsole":
                        case "greenconsole": 
                        case "whiteconsole":
                        case "yellowconsole":
                        case "commandlineconsole": 
                        if(rule.Delete == false) return true;
                        break;
                    }
                }
            }
            return false;
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using System.Xml.XPath;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using Microsoft.Protocols.TestTools.Logging;
using Microsoft.Win32;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// A class which implements <see cref="ITestSite"/>, based on an XML test configuration file.
    /// </summary>
    /// <remarks>
    /// For the detailed information about configuration file, please check the schema specification.
    /// </remarks>
    internal class DefaultTestSite : ITestSite, IDisposable, IProtocolTestNotify
    {
        // Private member that is returned by property Properties.
        private NameValueCollection properties;
        // Private member that is returned by property Log.
        private ILogger logger;
        // Private member that is returned by property Assert.
        private IChecker assertChecker;
        // Private member that is returned by property Assume.
        private IChecker assumeChecker;
        // Private member that is returned by property Debug.
        private IChecker debugChecker;
        // Adapter Dictionary to contain created adapters.
        private Dictionary<Type, IAdapter> adapters = new Dictionary<Type, IAdapter>();

        // Test Suite Name
        private string testSuiteName;

        //Default protocol short name
        private string defaultProtocolDocShortName;

        private string deploymentDirectory;

        private string testAssemblyName;

        // Event table
        private Dictionary<string, EventHandler<TestStartFinishEventArgs>> eventTable;

        // Configuration Data
        private IConfigurationData configData;


        private Dictionary<PtfTestOutcome, int> testResultsStatistics
             = new Dictionary<PtfTestOutcome, int>();

        //all initialize actions cache
        private Dictionary<Type, IList<MethodInfo>> initializeActions
            = new Dictionary<Type, IList<MethodInfo>>();

        //all cleanup actions cache
        private Dictionary<Type, IList<MethodInfo>> cleanupActions
            = new Dictionary<Type, IList<MethodInfo>>();

        private List<RequirementType> skipRequirement;

        /// <summary>
        /// A flag parameter used in checker methods
        /// (which indicates the presence of requirement id information)
        /// </summary>
        private const string reqIdFlag = "ContainsReqId";

        /// <summary>
        /// Implements <see cref="ITestSite.TestResultsStatistics"/>.
        /// </summary>
        public Dictionary<PtfTestOutcome, int> TestResultsStatistics
        {
            get
            {
                return testResultsStatistics;
            }
        }

        /// <summary>
        /// Constructs a new instance of DefaultTestSite.
        /// All checkers, loggers and adapter types are initialized in this constructor.
        /// </summary>
        /// <param name="config">Configuration data from ptfconfig</param>
        /// <param name="testDeploymentDirectory">The path of test suites deployment directory.</param>
        /// <param name="testSuiteName">The name of the test suite. The test site uses this name to find configuration files.</param>
        /// <param name="testAssemblyName">The test assembly name</param>
        public DefaultTestSite(IConfigurationData config, string testDeploymentDirectory, string testSuiteName, string testAssemblyName)
        {
            this.testSuiteName = testSuiteName;
            this.testAssemblyName = testAssemblyName;

            deploymentDirectory = testDeploymentDirectory;

            // initialize event table
            eventTable = new Dictionary<string, EventHandler<TestStartFinishEventArgs>>();
            eventTable.Add("TestStarted", null);
            eventTable.Add("TestFinished", null);

            // clear default protocol short name
            defaultProtocolDocShortName = string.Empty;

            this.properties = config.Properties;
            this.configData = config;
            logger = new Logger(this);

            skipRequirement = new List<RequirementType>();
            if (Convert.ToBoolean(properties.Get("SkipMUSTRequirements")))
                skipRequirement.Add(RequirementType.Must);
            if (Convert.ToBoolean(properties.Get("SkipSHOULDRequirements")))
                skipRequirement.Add(RequirementType.Should);
            if (Convert.ToBoolean(properties.Get("SkipMAYRequirements")))
                skipRequirement.Add(RequirementType.May);
            if (Convert.ToBoolean(properties.Get("SkipPRODUCTRequirements")))
                skipRequirement.Add(RequirementType.Product);
            if (Convert.ToBoolean(properties.Get("SkipUNDEFINEDRequirements")))
                skipRequirement.Add(RequirementType.Undefined);
        }

        /// <summary>
        /// Implement <see cref="ITestSite.RegisterCheckers"/>
        /// </summary>
        /// <param name="checkers">All checkers need to register into test site</param>
        public void RegisterCheckers(IDictionary<CheckerKinds, IChecker> checkers)
        {
            foreach (KeyValuePair<CheckerKinds, IChecker> kvp in checkers)
            {
                if (kvp.Key == CheckerKinds.AssertChecker)
                {
                    this.assertChecker = kvp.Value;
                    continue;
                }
                else if (kvp.Key == CheckerKinds.AssumeChecker)
                {
                    this.assumeChecker = kvp.Value;
                    continue;
                }
                else if (kvp.Key == CheckerKinds.DebugChecker)
                {
                    this.debugChecker = kvp.Value;
                    continue;
                }
                else
                {
                    throw new InvalidOperationException("The checker kind is not supported: " + kvp.Key.ToString());
                }
            }
        }

        /// <summary>
        /// Resets all adapters used in current test suite
        /// </summary>
        internal void ResetAdapters()
        {
            if (testResultsStatistics.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<Type, IAdapter> kvp in adapters)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Reset();
                }
            }
        }

        /// <summary>
        /// Disposes all adapters used in current test suite
        /// </summary>
        internal void DisposeAdapters()
        {
            foreach (KeyValuePair<Type, IAdapter> kvp in adapters)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Dispose();
                }
            }
            adapters.Clear();
        }

        #region ITestSite Members

        /// <summary>
        /// Implements <see cref="ITestSite.Properties"/>.
        /// </summary>
        public virtual NameValueCollection Properties
        {
            get
            {
                return properties;
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.Config"/>
        /// </summary>
        public IConfigurationData Config
        {
            get
            {
                return this.configData;
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.TestAssemblyName" />
        /// </summary>
        public string TestAssemblyName
        {
            get
            {
                return this.testAssemblyName;
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.DefaultProtocolDocShortName"/>
        /// </summary>
        public virtual string DefaultProtocolDocShortName
        {
            get
            {
                return defaultProtocolDocShortName;
            }
            set
            {
                defaultProtocolDocShortName = value;
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.FeatureName"/>.
        /// </summary>
        public virtual string FeatureName
        {
            get
            {
                if (properties[ConfigurationPropertyName.ProtocolName] == null)
                {
                    throw new InvalidOperationException("FeatureName is not available.");
                }
                return properties[ConfigurationPropertyName.ProtocolName];
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.TestName"/>.
        /// </summary>
        public virtual string TestName
        {
            get
            {
                if (properties == null || properties[ConfigurationPropertyName.TestName] == null)
                {
                    return String.Empty;
                }
                return properties[ConfigurationPropertyName.TestName];
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.TestSuiteName"/>.
        /// </summary>
        public virtual string TestSuiteName
        {
            get
            {
                return testSuiteName;
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.Log"/>.
        /// </summary>
        public virtual ILogger Log
        {
            get
            {
                return logger;
            }
        }


        /// <summary>
        /// Implements <see cref="ITestSite.Assert"/>.
        /// </summary>
        public virtual IChecker Assert
        {
            get
            {
                return assertChecker;
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.Assume"/>.
        /// </summary>
        public virtual IChecker Assume
        {
            get
            {
                return assumeChecker;
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.Debug"/>.
        /// </summary>
        public virtual IChecker Debug
        {
            get
            {
                return debugChecker;
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.GetAdapter"/>.
        /// </summary>
        /// <remarks>
        /// For script and interactive adapter, test site provides the default implementations in PTF.
        /// For managed adapter, test site provides the class instances according to the configuration, and if no class type is defined, it returns null.
        /// The <see cref="IAdapter.Initialize"/> method is automatically called before the instances is returned.
        /// </remarks>
        /// <param name="adapterType">The adapter interface type.</param>
        /// <returns>An adapter instance of the given type.</returns>
        public virtual IAdapter GetAdapter(Type adapterType)
        {
            IAdapter adapter = null;

            if (this.configData == null)
            {
                // Configuration files do not present.
                return adapter;
            }

            // Check if target type adapter is already created.
            if (adapters.ContainsKey(adapterType))
            {
                return adapters[adapterType];
            }

            // Get target adapter type.
            AdapterConfig adapterConfig = this.configData.GetAdapterConfig(adapterType.Name);

            // Create proxy for default type adapter.
            if (adapterConfig is InteractiveAdapterConfig)
            {
                adapter = (IAdapter)new InteractiveAdapterProxy(adapterType).GetTransparentProxy();
            }

            // Create proxy for PowerShell script type adapter
            else if (adapterConfig is PowerShellAdapterConfig)
            {
                adapter = (IAdapter)new PowerShellAdapterProxy(
                    ((PowerShellAdapterConfig)adapterConfig).ScriptDir,
                    adapterType).GetTransparentProxy();
            }

            // Create proxy for Shell script type adapter
            else if (adapterConfig is ShellAdapterConfig)
            {
                adapter = (IAdapter)new ShellAdapterProxy(
                    ((ShellAdapterConfig)adapterConfig).ScriptDir,
                    adapterType).GetTransparentProxy();
            }

            // Create instance for dot net type adapter.
            else if (adapterConfig is ManagedAdapterConfig)
            {
                try
                {
                    string adapterTypeName = ((ManagedAdapterConfig)adapterConfig).AdapterType;
                    if (adapterType.IsGenericType)
                    {
                        adapter = TestToolHelpers.CreateInstanceFromTypeName(adapterTypeName) as IAdapter;
                    }
                    else
                    {
                        Type adapterImplType = TestToolHelpers.ResolveTypeFromAssemblies(adapterTypeName, deploymentDirectory);
                        if (adapterImplType == null)
                            throw new InvalidOperationException(
                                String.Format("Can't find assembly \"{0}\"", adapterTypeName));
                        adapter = (new ManagedAdapterProxy(adapterImplType, adapterType).GetTransparentProxy()) as IAdapter;
                    }
                    // adapter is null if as operator fails due to an object can't be converted to IAdapter type
                    if (adapter == null)
                    {
                        throw new InvalidOperationException(
                            String.Format("Adapter {0} does not implement {1}",
                                          adapterTypeName, adapterType.FullName));
                    }
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException(
                               String.Format("Adapter {0} instance creation failed. Reason:{1}",
                                             adapterType.FullName, ex.ToString()));
                }
                catch (FileLoadException e)
                {
                    throw new InvalidOperationException(
                        String.Format("The assembly of the adapter ({0}) could not be loaded.", adapterType.Name), e);
                }
                catch (FileNotFoundException e)
                {
                    throw new InvalidOperationException(
                        String.Format("The assembly of the adapter ({0}) could not be found.", adapterType.Name), e);
                }
                catch (ArgumentException e)
                {
                    throw new InvalidOperationException(
                        String.Format("The type of the adapter ({0}) could not be found.", adapterType.Name), e);
                }
            }

            if (adapter == null)
            {
                throw new InvalidOperationException(String.Format("Failed while creating the adapter: {0}", adapterType.Name));
            }

            adapters.Add(adapterType, adapter);
            adapter.Initialize(this);

            return adapter;
        }

        /// <summary>
        /// Implements <see cref="ITestSite.GetAdapter"/>
        /// </summary>
        /// <remarks>
        /// This method calls the <see cref="ITestSite.GetAdapter"/> method.
        /// </remarks>
        /// <typeparam name="T">The type of the adapter.</typeparam>
        /// <returns>An adapter instance of the given type.</returns>
        public virtual T GetAdapter<T>() where T : IAdapter
        {
            // Set default value for compiling.
            T adapter = default(T);

            IAdapter result = GetAdapter(typeof(T));
            if (result != null)
                adapter = (T)result;

            return adapter;
        }

        /// <summary>
        /// Implements <see cref="ITestSite.ReportAsyncErrorToTcm"/>.
        /// </summary>
        /// <param name="formatString">A composite format string.</param>
        /// <param name="parameters">An Object array containing one or more objects to format.</param>
        public void ReportAsyncErrorToTcm(string formatString, params object[] parameters)
        {
            assertChecker.Fail(formatString, parameters);
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CheckErrors"/>.
        /// </summary>
        public void CheckErrors()
        {
            assertChecker.CheckErrors();
            assumeChecker.CheckErrors();
            debugChecker.CheckErrors();
        }

        /// <summary>
        /// Implements <see cref="ITestSite.TestStarted"/>.
        /// </summary>
        public event EventHandler<TestStartFinishEventArgs> TestStarted
        {
            add
            {
                eventTable["TestStarted"] += value;
            }
            remove
            {
                eventTable["TestStarted"] -= value;
            }
        }
        /// <summary>
        /// Implements <see cref="ITestSite.TestFinished"/>.
        /// </summary>
        public event EventHandler<TestStartFinishEventArgs> TestFinished
        {
            add
            {
                eventTable["TestFinished"] += value;
            }
            remove
            {
                eventTable["TestFinished"] -= value;
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirement(string, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirement(string protocolDocShortName, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            if (!skipRequirement.Contains(requirementType))
            {
                Log.Add(LogEntryKind.Checkpoint, RequirementId.Make(protocolDocShortName, requirementId, description));
            }
        }


        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfAreEqual{T}(T, T, string, int, string, RequirementType)"/>
        /// </summary>
        /// /// <typeparam name="T">The type of the compared values.</typeparam>
        public virtual void CaptureRequirementIfAreEqual<T>(
            T expected, T actual,
            string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = RequirementId.Make(protocolDocShortName, requirementId, description);
            string requirementString = "Requirement" + reqId;
            if (!skipRequirement.Contains(requirementType))
            {
                //Inorder to reduce the complexity of Method ValidateAllKeywordsRequirements, the below check has been included in this Capture Method.
                //Also, this check is applicable to only capture methods that have expectedValues.
                if (requirementType != RequirementType.Must && (null != properties[requirementString]))
                {
                    expected = (T)Convert.ChangeType(properties[requirementString].ToString(), typeof(T));
                }

                Assert.AreEqual<T>(expected, actual, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfAreNotEqual{T}(T, T, string, int, string, RequirementType)"/>
        /// </summary>
        /// /// <typeparam name="T">The type of the compared values.</typeparam>
        public virtual void CaptureRequirementIfAreNotEqual<T>(
            T expected, T actual,
            string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = RequirementId.Make(protocolDocShortName, requirementId, description);

            string requirementString = "Requirement" + reqId;
            if (!skipRequirement.Contains(requirementType))
            {
                if (requirementType != RequirementType.Must && (null != properties[requirementString]))
                {
                    expected = (T)Convert.ChangeType(properties[requirementString].ToString(), typeof(T));
                }

                Assert.AreNotEqual<T>(expected, actual, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfAreSame(object, object, string, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfAreSame(
            object expected, object actual,
            string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = RequirementId.Make(protocolDocShortName, requirementId, description);

            if (!skipRequirement.Contains(requirementType))
            {
                Assert.AreSame(expected, actual, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfAreNotSame(object, object, string, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfAreNotSame(
            object expected, object actual,
            string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = RequirementId.Make(protocolDocShortName, requirementId, description);

            if (!skipRequirement.Contains(requirementType))
            {
                Assert.AreNotSame(expected, actual, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfIsTrue(bool, string, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfIsTrue(
            bool condition, string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = RequirementId.Make(protocolDocShortName, requirementId, description);
            if (!skipRequirement.Contains(requirementType))
            {
                Assert.IsTrue(condition, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfIsFalse(bool, string, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfIsFalse(
            bool condition, string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = RequirementId.Make(protocolDocShortName, requirementId, description);
            if (!skipRequirement.Contains(requirementType))
            {
                Assert.IsFalse(condition, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfIsNull(object, string, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfIsNull(
            object value, string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = RequirementId.Make(protocolDocShortName, requirementId, description);
            if (!skipRequirement.Contains(requirementType))
            {
                Assert.IsNull(value, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfIsNotNull(object, string, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfIsNotNull(
            object value, string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = RequirementId.Make(protocolDocShortName, requirementId, description);
            if (!skipRequirement.Contains(requirementType))
            {
                Assert.IsNotNull(value, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfIsInstanceOfType(object, Type, string, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfIsInstanceOfType(
            object value, Type type,
            string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = RequirementId.Make(protocolDocShortName, requirementId, description);
            if (!skipRequirement.Contains(requirementType))
            {
                Assert.IsInstanceOfType(value, type, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfIsNotInstanceOfType(object, Type, string, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfIsNotInstanceOfType(
            object value, Type type,
            string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = RequirementId.Make(protocolDocShortName, requirementId, description);
            if (!skipRequirement.Contains(requirementType))
            {
                Assert.IsNotInstanceOfType(value, type, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfIsSuccess(int, string, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfIsSuccess(
            int handle, string protocolDocShortName,
            int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = RequirementId.Make(protocolDocShortName, requirementId, description);
            if (!skipRequirement.Contains(requirementType))
            {
                Assert.IsSuccess(handle, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.UnverifiedRequirement(string, int, string)"/>
        /// </summary>
        public virtual void UnverifiedRequirement(string protocolDocShortName, int requirementId, string description)
        {
            Assert.Unverified(RequirementId.Make(protocolDocShortName, requirementId, description));
        }



        //The overloaded version of CaptureRequirementIf* methods

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirement(int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirement(int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            if (!string.IsNullOrEmpty(DefaultProtocolDocShortName))
            {
                CaptureRequirement(DefaultProtocolDocShortName, requirementId, description, requirementType);
            }
            else
            {
                throw new InvalidOperationException("A non-empty value must be provided for property DefaultProtocolShortName to create a default requirement ID");
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfAreEqual{T}(T, T, int, string, RequirementType)"/>
        /// </summary>
        /// /// <typeparam name="T">The type of the compared values.</typeparam>
        public virtual void CaptureRequirementIfAreEqual<T>(T expected, T actual, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = GenerateRequirementId(requirementId, description);
            string requirementString = "Requirement" + reqId;
            if (!skipRequirement.Contains(requirementType))
            {
                if (requirementType != RequirementType.Must && (null != properties[requirementString]))
                {
                    expected = (T)Convert.ChangeType(properties[requirementString].ToString(), typeof(T));
                }
                Assert.AreEqual<T>(expected, actual, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfAreNotEqual{T}(T, T, int, string, RequirementType)"/>
        /// </summary>
        /// /// <typeparam name="T">The type of the compared values.</typeparam>
        public virtual void CaptureRequirementIfAreNotEqual<T>(T expected, T actual, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = GenerateRequirementId(requirementId, description);

            string requirementString = "Requirement" + reqId;
            if (!skipRequirement.Contains(requirementType))
            {
                if (requirementType != RequirementType.Must && (null != properties[requirementString]))
                {
                    expected = (T)Convert.ChangeType(properties[requirementString].ToString(), typeof(T));
                }

                Assert.AreNotEqual<T>(expected, actual, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }

        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfAreSame(object, object, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfAreSame(object expected, object actual, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = GenerateRequirementId(requirementId, description);

            if (!skipRequirement.Contains(requirementType))
            {
                Assert.AreSame(expected, actual, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfAreNotSame(object, object, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfAreNotSame(object expected, object actual, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = GenerateRequirementId(requirementId, description);
            if (!skipRequirement.Contains(requirementType))
            {
                Assert.AreNotSame(expected, actual, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfIsTrue(bool, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfIsTrue(bool condition, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = GenerateRequirementId(requirementId, description);
            if (!skipRequirement.Contains(requirementType))
            {
                Assert.IsTrue(condition, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfIsFalse(bool, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfIsFalse(bool condition, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = GenerateRequirementId(requirementId, description);
            if (!skipRequirement.Contains(requirementType))
            {
                Assert.IsFalse(condition, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfIsNull(object, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfIsNull(object value, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = GenerateRequirementId(requirementId, description);
            if (!skipRequirement.Contains(requirementType))
            {
                Assert.IsNull(value, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfIsNotNull(object, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfIsNotNull(object value, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = GenerateRequirementId(requirementId, description);
            if (!skipRequirement.Contains(requirementType))
            {
                Assert.IsNotNull(value, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfIsInstanceOfType(object, Type, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfIsInstanceOfType(object value, Type type, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = GenerateRequirementId(requirementId, description);
            if (!skipRequirement.Contains(requirementType))
            {
                Assert.IsInstanceOfType(value, type, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfIsNotInstanceOfType(object, Type, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfIsNotInstanceOfType(object value, Type type, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = GenerateRequirementId(requirementId, description);
            if (!skipRequirement.Contains(requirementType))
            {
                Assert.IsNotInstanceOfType(value, type, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.CaptureRequirementIfIsSuccess(int, int, string, RequirementType)"/>
        /// </summary>
        public virtual void CaptureRequirementIfIsSuccess(int handle, int requirementId, string description, RequirementType requirementType = RequirementType.Undefined)
        {
            string reqId = GenerateRequirementId(requirementId, description);
            if (!skipRequirement.Contains(requirementType))
            {
                Assert.IsSuccess(handle, description, reqIdFlag, reqId);
                CaptureRequirement(reqId);
            }
        }

        /// <summary>
        /// Implements <see cref="ITestSite.UnverifiedRequirement(int, string)"/>
        /// </summary>
        public virtual void UnverifiedRequirement(int requirementId, string description)
        {
            if (!string.IsNullOrEmpty(DefaultProtocolDocShortName))
            {
                UnverifiedRequirement(DefaultProtocolDocShortName, requirementId, description);
            }
            else
            {
                throw new InvalidOperationException("A non-empty value must be provided for property DefaultProtocolShortName to create a default requirement ID");
            }
        }
        #endregion

        #region Generate test report helper

        private string[] GetXmlLogFileNames(string[] logSinks)
        {
            string[] logFileNames = new string[logSinks.Length];
            for (int i = 0; i < logSinks.Length; i++)
            {
                string logSinkName = logSinks[i];
                foreach (LogSink logSink in this.Log.LogProfile.AllSinks)
                {
                    if (logSink.Name == logSinkName)
                    {
                        if (logSink is XmlTextSink)
                        {
                            XmlTextSink xmlTestSink = logSink as XmlTextSink;
                            logFileNames[i] = xmlTestSink.LogFileName;
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                string.Format("invalid log sink name {0}, the sink should be set to either xml sink or text sink.", logSinkName)
                                );
                        }
                        break;
                    }
                    continue;
                }
            }

            return logFileNames;
        }

        /// <summary>
        /// Gets the reporting tool inputs
        /// </summary>
        private string GetReportToolParams(IConfigurationData config)
        {
            const string tagReq = "/table:";
            const string tagLog = "/log:";
            const string tagOutupt = "/out:";
            const string tagReplace = "/replace";
            const string tagInScope = "/inscope:";
            const string tagOutOfScope = "/outofscope:";
            const string tagPrefix = "/prefix:";
            const string tagVerbose = "/verbose";
            StringBuilder parameterBuilder = new StringBuilder();
            if (config == null)
            {
                throw new InvalidOperationException("The configuration reader is not initialized.");
            }

            //Get requirement tables "/table:<tableNames>"
            string[] requirementTables = config.RequirementFiles;
            if (requirementTables.Length < 1)
            {
                throw new InvalidOperationException("No RequirementFile found, at least RequirementFile needed.");
            }
            else
            {
                parameterBuilder.Append(tagReq);
                for (int i = 0; i < requirementTables.Length; i++)
                {
                    string requirementTable = requirementTables[i];
                    parameterBuilder.Append("\"" + requirementTable + "\" ");
                }
            }

            //Get log xml "/log:<logXmlNames>"
            string[] logXmls = GetXmlLogFileNames(config.LogFiles);
            if (logXmls.Length < 1)
            {
                throw new InvalidOperationException("No log file found, at least one log file needed.");
            }
            else
            {
                parameterBuilder.Append(tagLog);
                for (int k = 0; k < logXmls.Length; k++)
                {
                    string logXml = logXmls[k];
                    if (!File.Exists(logXml))
                    {
                        throw new InvalidOperationException(string.Format("Invalid log xml file at: {0}", logXml));
                    }
                    parameterBuilder.Append("\"" + logXml + "\" ");
                }
            }

            //Get output report name "/out:<reportName>"
            parameterBuilder.Append(tagOutupt);
            string fullReportName = config.TestReportOutputFile;
            if (config.UseDefaultOutputDir == true)
            {
                fullReportName = Path.Combine(deploymentDirectory, fullReportName);
            }
            parameterBuilder.Append("\"" + fullReportName + "\" ");

            //ptf will aways force replace the old report file
            parameterBuilder.Append(tagReplace);

            if (!string.IsNullOrEmpty(config.InScope))
            {
                parameterBuilder.Append(" " + tagInScope + "\"" + config.InScope + "\" ");
            }

            if (!string.IsNullOrEmpty(config.OutScope))
            {
                parameterBuilder.Append(" " + tagOutOfScope + "\"" + config.OutScope + "\"");
            }

            if (!string.IsNullOrEmpty(config.Prefix))
            {
                parameterBuilder.Append(" " + tagPrefix + "\"" + config.Prefix + "\"");
            }

            if (config.VerboseMode)
            {
                parameterBuilder.Append(" " + tagVerbose);
            }
            return parameterBuilder.ToString();
        }

        /// <summary>
        /// Gets the reporting tool install directory
        /// </summary>
        /// <returns>Returns the full path name of the reporting tool</returns>
        private static string GetReportToolInstallDir()
        {
            string InstallRegistryKey = ConfigurationDataProvider.InstallRegistryKey;
            RegistryKey hklm = null;
            string installDir;

            if (Environment.Is64BitOperatingSystem)
            {
                InstallRegistryKey = ConfigurationDataProvider.Wow64InstallRegistryKey;
            }

            try
            {
                hklm = Registry.LocalMachine.OpenSubKey(InstallRegistryKey);
            }
            catch (System.Security.SecurityException e)
            {
                throw new InvalidOperationException("Can not open the Registry Key. Please make sure you have the authority to access Registry.", e);
            }
            catch (System.ArgumentException e)
            {
                throw new InvalidOperationException("Can not open the Registry Key. Please make sure your PTF installation is valid. " +
                                                    "Or you need to reinstall PTF.", e);
            }

            using (hklm)
            {
                if (hklm != null)
                {

                    installDir = hklm.GetValue("InstallDir") as string;
                    if (string.IsNullOrEmpty(installDir))
                    {
                        throw new InvalidOperationException("Can not find PTF installation directory from Registry. " +
                                                "Please make sure your PTF installation is valid. " +
                                                "Or you need to reinstall PTF.");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Can not find PTF installation directory from Registry. " +
                                                    "Please make sure your PTF installation is valid. " +
                                                    "Or you need to reinstall PTF.");
                }
            }

            return installDir;
        }

        /// <summary>
        /// Generates test report and auto display it.
        /// </summary>
        public void GenerateAndDisplayTestReport(IConfigurationData config)
        {
            if (config == null)
            {
                throw new InvalidOperationException("The configuration reader is not initialized.");
            }
            if (config.NeedGenerateReport)
            {
                int exitCode = 0;
                string reportToolInstallDir = GetReportToolInstallDir();
                using (Process proc = new Process())
                {
                    try
                    {
                        proc.StartInfo.FileName = Path.Combine(reportToolInstallDir, "ReportingTool.exe");
                        proc.StartInfo.Arguments = this.GetReportToolParams(config);
                        proc.StartInfo.UseShellExecute = false;
                        proc.StartInfo.CreateNoWindow = true;
                        proc.Start();
                        proc.WaitForExit();
                        exitCode = proc.ExitCode;
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException(e.Message);
                    }
                    finally
                    {
                        if (proc != null)
                        {
                            proc.Close();
                        }
                    }
                }
                if (exitCode != 0)
                {
                    string logPath = Path.GetFullPath("ReportingToolLog.log");
                    if (File.Exists(logPath))
                    {
                        throw new InvalidOperationException(
                                         string.Format("Fail to generate test report,"
                                         + " please find error message in reporting tool log: \r\n \"{0}\"", logPath)
                                                      );

                    }
                    else
                    {
                        //Generate log file fail
                        if (exitCode == 2)
                        {
                            throw new InvalidOperationException("Reporting tool cannot create log file");
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                string.Format("Reporting tool process exited with error code: {0}", exitCode)
                                );
                        }
                    }
                }
                else
                {
                    if (config.NeedAutoDisplay)
                    {
                        string arguments = string.Empty;
                        //get the full output path of the report file
                        if (config.UseDefaultOutputDir)
                        {
                            arguments = Path.Combine(deploymentDirectory, config.TestReportOutputFile);
                        }
                        else
                        {
                            arguments = config.TestReportOutputFile;
                        }
                        Process.Start("IExplore.exe", arguments);

                        Console.WriteLine("Test report {0} is generated successfully", arguments);
                    }
                }
            }
        }

        /// <summary>
        /// Logs requirement capture info
        /// </summary>
        /// <param name="message">Message which contains requirement info</param>
        private void CaptureRequirement(string message)
        {
            Log.Add(LogEntryKind.Checkpoint, message);
        }

        /// <summary>
        /// Generates requirement id in string format
        /// (meanwhile update the requirement dictionary)
        /// </summary>
        /// <param name="requirementId">Requirement id</param>
        /// <param name="description">Description</param>
        /// <returns>The requirement id in string format</returns>
        private string GenerateRequirementId(int requirementId, string description)
        {
            if (!String.IsNullOrEmpty(defaultProtocolDocShortName))
            {
                return RequirementId.Make(defaultProtocolDocShortName, requirementId, description);
            }
            else
            {
                throw new InvalidOperationException(
                    "A non-empty value must be provided for property DefaultProtocolShortName to create a default requirement ID");
            }
        }
        #endregion

        #region IDisposable Members

        /// <summary>
        /// Indicates if the DefaultTestSite has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Gets or sets the disposing status.
        /// </summary>
        protected bool IsDisposed
        {
            get
            {
                return disposed;
            }
            set
            {
                disposed = value;
            }
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// </summary>
        /// <param name="disposing">
        /// If disposing equals false, the method is called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                if (logger != null)
                {
                    logger.AddTestStatistic();
                    logger.Dispose();
                }
                GenerateAndDisplayTestReport(this.configData);
                DisposeAdapters();
                initializeActions.Clear();
                cleanupActions.Clear();
            }
            disposed = true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This destructor will run only if the Dispose method
        /// does not get called.
        /// It gives your base class the opportunity to finalize.
        /// Do not provide destructors in types derived from this class.
        /// </summary>
        ~DefaultTestSite()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        #endregion

        private Dictionary<string, Object> testProperties =
            new Dictionary<string, object>();

        /// <summary>
        /// Implements <see cref="ITestSite.TestProperties"/>
        /// </summary>
        public Dictionary<string, Object> TestProperties
        {
            get
            {
                return testProperties;
            }
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void OnTestStarted(object testClass, string testName, PtfTestOutcome testOutcome, AssertExceptionHandler exceptionHandler)
        {
            testProperties[TestPropertyNames.CurrentTestCaseName] = testName;
            testProperties[TestPropertyNames.CurrentTestOutcome] = testOutcome;
            this.WaitForProcessMessage();

            this.Log.Add(LoggingHelper.PtfTestOutcomeToLogEntryKind(testOutcome), testName);

            this.Log.Add(LogEntryKind.EnterMethod, testName);

            try
            {
                EventHandler<TestStartFinishEventArgs> handler = eventTable["TestStarted"];
                if (handler != null)
                {
                    //may throw exception and make test finish
                    handler(this, new TestStartFinishEventArgs(testName, testOutcome));
                }
                this.InvokeActions(typeof(ProtocolTestInitializeAttribute), testClass);

            }
            catch (Exception e)
            {
                UpdateTestResultsStatistics(exceptionHandler(e));
                throw;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void OnTestFinished(object testClass, string testName, PtfTestOutcome testOutcome, AssertExceptionHandler exceptionHandler)
        {
            testProperties[TestPropertyNames.CurrentTestCaseName] = testName;
            testProperties[TestPropertyNames.CurrentTestOutcome] = testOutcome;

            EventHandler<TestStartFinishEventArgs> handler = eventTable["TestFinished"];

            try
            {
                if (handler != null)
                {
                    //may throw exception and make test finish
                    handler(this, new TestStartFinishEventArgs(testName, testOutcome));
                }

                this.InvokeActions(typeof(ProtocolTestCleanupAttribute), testClass);
                UpdateTestResultsStatistics(testOutcome);
            }
            catch (Exception e)
            {
                UpdateTestResultsStatistics(exceptionHandler(e));
                throw;
            }
            finally
            {
                this.Log.Add(LoggingHelper.PtfTestOutcomeToLogEntryKind(testOutcome), testName);
                this.Log.Add(LogEntryKind.ExitMethod, testName);

                this.WaitForProcessMessage();

                testProperties[TestPropertyNames.CurrentTestCaseName] = null;
                testProperties[TestPropertyNames.CurrentTestOutcome] = PtfTestOutcome.Unknown;
            }
        }

        private void UpdateTestResultsStatistics(PtfTestOutcome testOutcome)
        {
            if (testResultsStatistics.ContainsKey(testOutcome))
            {
                testResultsStatistics[testOutcome]++;
            }
            else
            {
                testResultsStatistics.Add(testOutcome, 1);
            }
        }

        //ensure all messages in the log message queue were processed.
        private void WaitForProcessMessage()
        {
            Logger logger = this.Log as Logger;
            while (logger.LogMessageQueue.Count > 0)
            {
                System.Threading.Thread.Sleep(50);
            }

            logger.ProcessErrors();
        }

        private void InvokeActions(Type attribute, object testClass)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException("attribute");
            }
            if (testClass == null)
            {
                throw new ArgumentNullException("testClass");
            }
            IList<MethodInfo> actions = new List<MethodInfo>();
            Type testClassType = testClass.GetType();
            if (attribute == typeof(ProtocolTestCleanupAttribute))
            {
                if (!this.cleanupActions.ContainsKey(testClassType))
                {
                    this.cleanupActions[testClassType] =
                        TestToolHelpers.GetMethodsByAttribute(attribute, testClassType, true);
                }
                actions = this.cleanupActions[testClassType];
            }
            else if (attribute == typeof(ProtocolTestInitializeAttribute))
            {
                if (!this.initializeActions.ContainsKey(testClassType))
                {
                    this.initializeActions[testClassType] =
                        TestToolHelpers.GetMethodsByAttribute(attribute, testClassType, true);
                }
                actions = this.initializeActions[testClassType];
            }
            else
            {
                throw new InvalidOperationException(
                    "Unexpected attribute found while invoking protocol testing actions.");
            }
            //invoke all actions
            foreach (MethodInfo mi in actions)
            {
                // we don't use MethodInfo.Invoke() because
                // that will wrap any exceptions thrown from the method
                // being invoked into TargetInvocationException.
                Action action = (Action)Delegate.CreateDelegate(
                    typeof(Action),
                    testClass,
                    mi);

                action();
            }
        }
    }
}

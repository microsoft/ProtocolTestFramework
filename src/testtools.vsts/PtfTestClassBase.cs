// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#if FORMODEL

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

using Microsoft.SpecExplorer.Runtime.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Internal use only. An implementation of a Spec Explorer test class base which is 
    /// integrating into PTF.
    /// </summary>
    //there's a bug in SE, disable IByPassingTestSuite before they fix it.
    public class PtfTestClassBase : TestClassBase, IGeneratedTestClass, IBasicTestSite //IBypassingTestSite 
    {
        private Dictionary<IAdapter, bool> adapters = new Dictionary<IAdapter, bool>();
        ITestManager manager;
        int observationBound = 32;
        TimeSpan proceedControlTimeout = TimeSpan.FromMilliseconds(0);
        TimeSpan quiescenceTimeout = TimeSpan.FromMilliseconds(2000);
        private string configPath;
        private string testSuiteName;
        private string testAssemblyName;
        private Regex exceptionFilter;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PtfTestClassBase()
        {
            this.testAssemblyName = Assembly.GetCallingAssembly().GetName().Name;
            string regexFilter = TestSite.Properties[ConfigurationPropertyName.BypassFilter];

            if (regexFilter != null)
            {
                this.exceptionFilter = new Regex(regexFilter);
            }
        }

        /// <summary>
        /// Constructs test class, only used by SE online testing. This constructor
        /// ensures that a new test site will be created.
        /// </summary>
        /// <param name="configPath">Configuration path</param>
        /// <param name="testSuiteName">Test suite name</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1805:DoNotInitializeUnnecessarily")]
        public PtfTestClassBase(string configPath, string testSuiteName)
            : base(configPath, testSuiteName)
        {
            this.configPath = configPath;
            this.testSuiteName = testSuiteName;
            this.testAssemblyName = Assembly.GetCallingAssembly().GetName().Name;
            string regexFilter = TestSite.Properties[ConfigurationPropertyName.BypassFilter];

            if (regexFilter != null)
            {
                this.exceptionFilter = new Regex(regexFilter);
            }
        }

        #region PtfTestClassBase Properties

        /// <summary>
        /// Test site
        /// </summary>
        protected ITestSite TestSite
        {
            get
            {
                if (base.Site == null)
                {
                    IProtocolTestsManager testsManager =
                        ProtocolTestsManagerFactory.TestsManager;

                    // triggers initialization
                    if (configPath != null)
                    {
                        IConfigurationData config = ConfigurationDataProvider.GetConfigurationData(configPath, testSuiteName);
                        testsManager.Initialize(config, configPath, testSuiteName, testAssemblyName);
                    }
                    else
                    {
                        IConfigurationData config = ConfigurationDataProvider.GetConfigurationData(TestContext.TestDeploymentDir, testSuiteName);
                        testsManager.Initialize(config, base.ProtocolTestContext, testSuiteName, testAssemblyName);
                    }
                    base.Site = testsManager.GetTestSite(testSuiteName);
                    if (base.Site == null)
                    {
                        throw new InvalidOperationException(
                            string.Format("Cannot get the test site {0}", testSuiteName));
                    }
                }
                return base.Site;
            }
            set
            {
                base.Site = value;
            }
        }

        /// <summary>
        /// Test clean up
        /// </summary>
        protected override void TestCleanup()
        {
            foreach (IAdapter adapter in adapters.Keys)
                adapter.Reset();
        }
        #endregion

        #region IBasicTestSite implementation

        /// <summary>
        /// Returns PTF adapter.
        /// </summary>
        /// <param name="adapterType">The adapter type</param>
        /// <returns>The adapter</returns>
        public virtual object GetAdapter(Type adapterType)
        {
            IAdapter adapter = TestSite.GetAdapter(adapterType);
            adapters[adapter] = true;
            
            return adapter;
        }

        /// <summary>
        /// See <see cref="Microsoft.SpecExplorer.Runtime.Testing.GeneratedTestClassBase.BeginTest"/>
        /// </summary>
        /// <param name="name">The test name</param>
        public virtual void BeginTest(string name)
        {
            // do nothing ... we are already logging this via PTF
        }

        /// <summary>
        /// See <see cref="Microsoft.SpecExplorer.Runtime.Testing.GeneratedTestClassBase.EndTest"/>
        /// </summary>
        public virtual void EndTest()
        {
            // do nothing ... we are already logging this via PTF
        }

        /// <summary>
        /// See <see cref="Microsoft.SpecExplorer.Runtime.Testing.GeneratedTestClassBase.Assert"/>
        /// </summary>
        /// <param name="condition">A bool condition</param>
        /// <param name="description">Description message for Assert</param>
        public virtual void Assert(bool condition, string description)
        {
            Site.Assert.IsTrue(condition, description);
        }

        /// <summary>
        /// See <see cref="Microsoft.SpecExplorer.Runtime.Testing.GeneratedTestClassBase.Assume"/>
        /// </summary>
        /// <param name="condition">A bool condition</param>
        /// <param name="description">Description message for Assume</param>
        public virtual void Assume(bool condition, string description)
        {
            Site.Assume.IsTrue(condition, description);
        }

        /// <summary>
        /// See <see cref="Microsoft.SpecExplorer.Runtime.Testing.GeneratedTestClassBase.Checkpoint"/>
        /// </summary>
        /// <param name="description">Description message for a check point in log</param>
        public virtual void Checkpoint(string description)
        {
            Site.Log.Add(LogEntryKind.Checkpoint, description);
        }

        /// <summary>
        /// See <see cref="Microsoft.SpecExplorer.Runtime.Testing.GeneratedTestClassBase.Comment"/>
        /// </summary>
        /// <param name="description">Description message for a comment in log</param>
        public virtual void Comment(string description)
        {
            Site.Log.Add(LogEntryKind.Comment, description);
        }

        /// <summary>
        /// Checks condition together with description to by-pass assertion failure.
        /// </summary>
        /// <param name="condition">A bool condition</param>
        /// <param name="description">Description message for Assert</param>
        /// <returns>false if and only if condition is false and description is not by-passed.</returns>
        public virtual bool IsTrue(bool condition, string description)
        {
            bool result = condition;
            if (!result)
            {
                if (this.exceptionFilter != null)
                {
                    if (this.exceptionFilter.IsMatch(description))
                    {
                        result = true;
                    }
                }
            }
            return result;
        }

        #endregion

        #region IGeneratedTestClass Members

       
        /// <summary>
        /// Sets the field value of "proceedcontroltimeout", "quiescencetimeout", or "observationbound"
        /// </summary>
        /// <param name="name">
        /// The field name (in string format).
        /// <para/>
        /// Valid names include: "proceedcontroltimeout", "quiescencetimeout", and "observationbound".
        /// </param>
        /// <param name="value">Value (in string format) for the given field</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation")]
        public void SetSwitch(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            switch (name.ToLower())
            {
                case "proceedcontroltimeout":
                    try
                    {
                        proceedControlTimeout = TimeSpan.FromMilliseconds(Double.Parse(value));
                    }
                    catch (ArgumentException)
                    {
                        Assume(false, "proceedcontroltimeout switch has invalid value");
                    }
                    catch (OverflowException)
                    {
                        Assume(false, "proceedcontroltimeout switch has invalid value");
                    }
                    break;
                case "quiescencetimeout":
                    try
                    {
                        quiescenceTimeout = TimeSpan.FromMilliseconds(Double.Parse(value));
                    }
                    catch (ArgumentException)
                    {
                        Assume(false, "quiescencetimeout switch has invalid value");
                    }
                    catch (OverflowException)
                    {
                        Assume(false, "quiescencetimeout switch has invalid value");
                    }
                    break;
                case "observationbound":
                    try
                    {
                        observationBound = Int32.Parse(value);
                    }
                    catch (FormatException)
                    {
                        Assume(false, "observationbound switch has invalid value");
                    }
                    catch (OverflowException)
                    {
                        Assume(false, "observationbound switch has invalid value");
                    }
                    break;
            }
        }

        /// <summary>
        /// Time out for the quiescence status
        /// </summary>
        public TimeSpan QuiescenceTimeout
        {
            get { return quiescenceTimeout; }
            set { quiescenceTimeout = value; }
        }

        /// <summary>
        /// Time out for the proceed status
        /// </summary>
        public TimeSpan ProceedControlTimeout
        {
            get { return proceedControlTimeout; }
            set { proceedControlTimeout = value; }
        }

        /// <summary>
        /// To initialize the test manager
        /// </summary>
        public virtual void InitializeTestManager()
        {
            manager = new DefaultTestManager(this, observationBound, observationBound);
        }

        /// <summary>
        /// To clean up the test manager
        /// </summary>
        public virtual void CleanupTestManager()
        {
            // FIXME: check for ERROR entries in queues
            manager = null;
        }

        /// <summary>
        /// Returns the test manager. Only valid after initialization and before cleanup.
        /// </summary>
        public ITestManager Manager
        {
            get
            {
                if (manager == null)
                    throw new InvalidOperationException("test manager is not initialized");
                return manager;
            }
            set
            {
                this.manager = value;
            }
        }
 
        /// <summary>
        /// Creates a struct of type T with given field initialization.
        /// </summary>
        /// <typeparam name="T">Type T</typeparam>
        /// <param name="fieldNames">The field names</param>
        /// <param name="fieldValues">The field values</param>
        /// <returns>The created struct</returns>
        public T Make<T>(string[] fieldNames, object[] fieldValues)
        {
            object x = Activator.CreateInstance(typeof(T));
            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assume(fields.Length == fieldValues.Length,
                String.Format("number of field values provided for '{0}' (expected '{1}', actual '{2}')",
                typeof(T).FullName, fields.Length, fieldValues.Length));

            int j = 0;
            Dictionary<string, object> valueDict = new Dictionary<string,object>();
            foreach (string name in fieldNames)
                valueDict[name] = fieldValues[j++];

            foreach (FieldInfo field in fields)
            {
                field.SetValue(x, Convert.ChangeType(valueDict[field.Name], field.FieldType));
            }
            return (T)x;
        }

        #endregion
    }
}
#endif

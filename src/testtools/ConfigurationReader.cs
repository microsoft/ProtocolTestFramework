// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml.Schema;
using System.Text;
using System.Collections;
using Microsoft.Protocols.TestTools.Config;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// This class is used to read from test configuration files, get general 
    /// properties and information about adapters, sinks, etc.
    /// </summary>
    class ConfigurationReader : IConfigurationData, ICheckerConfig
    {
        // XPathNavigator of test configuration file.
        private XPathNavigator navigator;

        // XmlNamespaceManager of test configuration file.
        private XmlNamespaceManager manager;

        // XmlDocument of test configuration file.
        private XmlDocument document;

        // Containing general properties.
        private NameValueCollection properties;

        // XmlNode represents the log root node of the configuration.
        private XmlNode logNode;

        //XmlNode represents the report root node of the configuration
        private XmlNode reportNode;

        //A switch to enable or disable auto generated report.
        private bool needGenerateReport;

        //A switch to enable or disable auto display generated report file.
        private bool needAutoDisplay = true;

        //Store all requirement files
        private string[] requirementFiles;

        //Store all sinks in test report config
        private string[] logFiles;

        //Store report file config information
        private string testReportOutputFile;

        private bool useDefaultOutputDir = true;

        private static bool isParsed;

        private string inScope;

        private string outScope;

        private string prefix;

        private bool verboseMode;

        // Gets the default configuration namespace.
        public static string DefaultNamespace
        {
            get
            {
                return "http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/TestConfig";
            }
        }

        public static string DefaultSchemaLocation
        {
            get
            {
                return "http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/TestConfig http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/TestConfig.xsd";
            }
        }

        public static string DefaultSchemaInstance
        {
            get
            {
                return "http://www.w3.org/2001/XMLSchema-instance";
            }
        }

        public static string DefaultTestSiteTag
        {
            get
            {
                return "<TestSite xmlns=\"" + DefaultNamespace + "\" " +
                    "xmlns:xsi=\"" + DefaultSchemaInstance + "\" " +
                    "xsi:schemaLocation=\"" + DefaultSchemaLocation + "\">";
            }
        }

        /// <summary>
        /// Disables the default constructor.
        /// </summary>
        private ConfigurationReader()
        { }
        //The static field need to reset when the reader is created.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1805:DoNotInitializeUnnecessarily")]
        internal ConfigurationReader(string[] configFileNames, string schemaFileName)
        {
            if (configFileNames == null)
            {
                throw new ArgumentNullException("config file names");
            }
            else if (configFileNames.Length == 0)
            {
                throw new ArgumentException("config file names");
            }

            //reset
            if (isParsed)
            {
                isParsed = false;
            }
            SetProperXmlSchemaSet(schemaFileName);

            if (!ValidateConfigFiles(new string[] { configFileNames[0] }, true) ||
                !ValidateConfigFiles(configFileNames, false))
            {
                throw new XmlException(
                    String.Format("Validating configuration {0} failed: {1}",
                        invalidFilename,
                        validateErrorMessages));
            }

            XmlDocument xd = MergeConfigFiles(configFileNames);

            if (!ValidateXmlDocument(xd))
            {
                throw new XmlException(validateErrorMessages);
            }

            navigator = xd.CreateNavigator();

            manager = new XmlNamespaceManager(navigator.NameTable);
            manager.AddNamespace("tc", DefaultNamespace);
        }

        #region properties
        /// <summary>
        /// This property returns the XPathNavigator of merged test configuration file.
        /// </summary>
        private XPathNavigator Navigator
        {
            get
            {
                if (navigator == null)
                {
                    throw new InvalidOperationException("XML Navigator is not initialized.");
                }
                return navigator;
            }
        }

        /// <summary>
        /// This property returns the XmlNamespaceManager of merged test configuration file.
        /// </summary>
        private XmlNamespaceManager Manager
        {
            get
            {
                if (manager == null)
                {
                    throw new InvalidOperationException("XML Namespace manager is not initialized.");
                }
                return manager;
            }
        }

        /// <summary>
        /// The XmlDocument represents all configuration files.
        /// </summary>
        private XmlDocument Document
        {
            get { return document; }
            set { document = value; }
        }

        /// <summary>
        /// Gets the root node of the log configuration.
        /// </summary>
        private XmlNode LogNode
        {
            get
            {
                if (this.logNode == null)
                {
                    // Search for the test log node.
                    this.logNode = this.Document.DocumentElement.SelectSingleNode("/tc:TestSite/tc:TestLog", this.Manager);
                }

                if (this.logNode == null)
                {
                    throw new InvalidOperationException("TestLog is not found in the configuration. " +
                        "Please make sure TestLog is configurated properly in congfiguration file(s)." +
                        "Otherwise, please validate the Xml namespace and schema location in the configuration file(s). " +
                        "To specify the correct Xml namespace and schema location, " +
                        "the TestSite tag in configuration file(s) should be " + DefaultTestSiteTag);
                }

                return this.logNode;
            }
        }

        /// <summary>
        /// Gets the root node of the report configuration
        /// </summary>
        private XmlNode ReportNode
        {
            get
            {
                if (this.reportNode == null)
                {
                    // Search for the test report node
                    this.reportNode = this.Document.DocumentElement.SelectSingleNode("/tc:TestSite/tc:TestReport", this.Manager);
                }

                if (this.reportNode == null)
                {
                    this.needGenerateReport = false;
                    this.reportNode = null;
                }

                return this.reportNode;
            }
        }

        #endregion

        #region IConfigurationData Members

        /// <summary>
        /// Implement <see cref="IConfigurationData.GetAdapterConfig"/>
        /// </summary>
        /// <param name="adapterName">The adapter name</param>
        /// <returns>Returns the abstract adapter config data</returns>
        public AdapterConfig GetAdapterConfig(string adapterName)
        {
            AdapterConfig adapter;

            // Gets target adapter type.
            string type = this.GetAdapterAttribute(adapterName,
                                                    "type",
                                                    DefaultSchemaInstance
                                                    );

            // Create proxy for default type adapter.
            if (type.Equals("interactive", StringComparison.CurrentCultureIgnoreCase))
            {
                adapter = new InteractiveAdapterConfig(adapterName);
            }

            // Create proxy for command script type adapter.
            else if (type.Equals("script", StringComparison.CurrentCultureIgnoreCase))
            {
                string scriptdir = this.GetAdapterAttribute(adapterName, "scriptdir", "");
                adapter = new ScriptAdapterConfig(adapterName, scriptdir);
            }

            // Create proxy for PowerShell script type adapter
            else if (type.Equals("powershell", StringComparison.CurrentCultureIgnoreCase))
            {
                string psdir = this.GetAdapterAttribute(adapterName, "scriptdir", "");
                adapter = new PowerShellAdapterConfig(adapterName, psdir);
            }

            // Create proxy for PowerShell wrapper adapter
            else if (type.Equals("pswrapper", StringComparison.CurrentCultureIgnoreCase))
            {
                string psfile = this.GetAdapterAttribute(adapterName, "scriptfile", "");
                adapter = new PsWrapperAdapterConfig(adapterName, psfile);
            }

            // Create instance for dot net type adapter.
            else if (type.Equals("managed", StringComparison.CurrentCultureIgnoreCase))
            {
                string adapterTypeName = this.GetAdapterAttribute(adapterName, "adaptertype", "");
                string disablevalidationstring = this.TryGetAdapterAttribute(adapterName, "disablevalidation", "");
                bool disablevalidation = false;
                if (!String.IsNullOrEmpty(disablevalidationstring))
                {
                    Boolean.TryParse(disablevalidationstring, out disablevalidation); 
                }
                adapter = new ManagedAdapterConfig(adapterName, adapterTypeName, disablevalidation);
            }
            // Create instance for rpc type adapter.
            else if (type.Equals("rpc", StringComparison.CurrentCultureIgnoreCase))
            {
                string validationSwitch = this.TryGetAdapterAttribute(adapterName, "autovalidate", "");
                string callingConventionString = this.TryGetAdapterAttribute(adapterName, "callingconvention", "");
                string charsetString = this.TryGetAdapterAttribute(adapterName, "charset", "");

                bool autoValidate = true;
                if (!String.IsNullOrEmpty(validationSwitch))
                {
                    autoValidate = Boolean.Parse(validationSwitch);
                }

                //since the specified value has been validated by schema already, no need to re-check if the value is defined in Enum.
                CallingConvention callingConvention = CallingConvention.Winapi; //by default
                if (!String.IsNullOrEmpty(callingConventionString))
                {
                    callingConvention = (CallingConvention)Enum.Parse(typeof(CallingConvention), callingConventionString, true);
                }


                CharSet charset = CharSet.Auto; //by default
                if (!String.IsNullOrEmpty(charsetString))
                {
                    charset = (CharSet)Enum.Parse(typeof(CharSet), charsetString, true);
                }

                adapter = new RpcAdapterConfig(adapterName, type, autoValidate, callingConvention, charset);
            }
            else
            {
                adapter = new CustomAdapterConfig(adapterName, type);
            }

            return adapter;
        }

        public Collection<LogSinkConfig> LogSinks
        {
            get
            {
                XmlNode sinkNode = this.LogNode.SelectSingleNode("tc:Sinks", this.Manager);

                if (null == sinkNode)
                {
                    throw new XmlException("The Sink node is not found.");
                }

                //$REVIEW (nsun) we could cache the Log Sink config info for further perf improvment
                Collection<LogSinkConfig> logSinks = new Collection<LogSinkConfig>();

                // Build sinks and add them into the sinks collection.
                foreach (XmlNode sink in sinkNode.ChildNodes)
                {
                    if (sink.NodeType != XmlNodeType.Element)
                        continue;

                    string name = sink.Attributes["id"].Value;

                    switch (sink.Name)
                    {
                        case "Sink":
                            logSinks.Add(new CustomLogSinkConfig(
                                name,
                                sink.Attributes["type"].Value));
                            break;
                        case "File":
                            logSinks.Add(new FileLogSinkConfig(
                                name,
                                sink.Attributes["directory"].Value,
                                sink.Attributes["file"].Value,
                                sink.Attributes["format"].Value));
                            break;
                        case "Console":
                            logSinks.Add(new ConsoleLogSinkConfig(
                                name,
                                sink.Attributes["id"].Value));
                            break;
                        default:
                            throw new InvalidOperationException("The specified sink type is not supported.");
                    }

                }

                return logSinks;
            }
        }

        public Collection<ProfileConfig> Profiles
        {
            get
            {
                //$REVIEW (nsun) we could cache the Log Sink config info for further perf improvment
                XmlNode profileNode = this.LogNode.SelectSingleNode("tc:Profiles", this.Manager);

                if (null == profileNode)
                {
                    throw new XmlException("The Profile node is not found.");
                }

                Collection<ProfileConfig> profiles = new Collection<ProfileConfig>();

                // Parse profile entries.
                foreach (XmlNode profile in profileNode.ChildNodes)
                {
                    if (profile.NodeType != XmlNodeType.Element)
                        continue;

                    // Gets current profile name.
                    string name = profile.Attributes["name"].Value;

                    if (name == null)
                    {
                        throw new InvalidOperationException("The profile name attribute is not found.");
                    }

                    string baseProfile = (profile.Attributes["extends"] == null) ? null : profile.Attributes["extends"].Value;

                    Collection<ProfileRuleConfig> rules = new Collection<ProfileRuleConfig>();

                    // Parse all entries of a profile.
                    foreach (XmlNode entry in profile.ChildNodes)
                    {
                        if (entry.NodeType == XmlNodeType.Element && entry.Attributes != null)
                        {
                            bool isDelete = (entry.Attributes["delete"] == null) ?
                                false : Convert.ToBoolean(entry.Attributes["delete"].Value);

                            rules.Add(
                                new ProfileRuleConfig(entry.Attributes["kind"].Value,
                                    entry.Attributes["sink"].Value,
                                    isDelete));

                        }
                    }

                    profiles.Add(new ProfileConfig(name, baseProfile, rules));
                }

                return profiles;
            }
        }

        public string DefaultProfile
        {
            get
            {
                return this.LogNode.Attributes["defaultprofile"].Value;
            }
        }

        public string TestReportOutputFile
        {
            get
            {
                if (!isParsed)
                {
                    this.testReportOutputFile = null;
                    this.ConfigureNodesParser();
                }

                return testReportOutputFile;
            }
        }

        /// <summary>
        /// Gets the switch need generate report
        /// </summary>
        public bool NeedGenerateReport
        {
            get
            {
                if (this.ReportNode == null)
                {
                    return false;
                }

                XmlAttribute attrAutoGenerate = this.ReportNode.Attributes["autoGenerate"];
                string value = string.Empty;

                if (attrAutoGenerate != null)
                {
                    value = attrAutoGenerate.Value.Trim();
                }
                else
                {
                    //no attribute specified should set default value "true"
                    this.needGenerateReport = true;
                }

                this.needGenerateReport = TestToolHelpers.XmlBoolToBool(value);

                return this.needGenerateReport;
            }
        }

        /// <summary>
        /// Gets the switch need auto display test report
        /// </summary>
        public bool NeedAutoDisplay
        {
            get
            {
                if (!isParsed)
                {
                    this.needAutoDisplay = false;
                    this.ConfigureNodesParser();
                }
                return this.needAutoDisplay;
            }
        }

        public bool UseDefaultOutputDir
        {
            get
            {
                if (!isParsed)
                {
                    this.useDefaultOutputDir = false;
                    this.ConfigureNodesParser();
                }
                return this.useDefaultOutputDir;
            }
        }

        /// <summary>
        /// Gets the requirement table locations
        /// </summary>
        public string[] RequirementFiles
        {
            get
            {
                if (!isParsed)
                {
                    this.requirementFiles = null;
                    this.ConfigureNodesParser();
                }
                return this.requirementFiles;
            }
        }

        /// <summary>
        /// Gets the test log locations
        /// </summary>
        public string[] LogFiles
        {
            get
            {
                if (!isParsed)
                {
                    this.logFiles = null;
                    this.ConfigureNodesParser();
                }
                return this.logFiles;
            }
        }

        /// <summary>
        /// Implements <see cref="IConfigurationData.InScope"/>
        /// </summary>
        public string InScope
        {
            get
            {
                if (!isParsed)
                {
                    this.inScope = null;
                    this.ConfigureNodesParser();
                }
                return this.inScope;
            }
        }

        public string OutScope
        {
            get
            {
                if (!isParsed)
                {
                    this.outScope = null;
                    this.ConfigureNodesParser();
                }
                return this.outScope;
            }
        }

        public string Prefix
        {
            get
            {
                if (!isParsed)
                {
                    this.prefix = null;
                    this.ConfigureNodesParser();
                }
                return this.prefix;
            }
        }

        public bool VerboseMode
        {
            get
            {
                return this.verboseMode;
            }
        }

        /// <summary>
        /// Gets the properties from test configuration file.
        /// </summary>
        /// <returns>The NameValueCollection containing general properties.</returns>
        public NameValueCollection Properties
        {
            get
            {
                if (properties == null)
                {
                    properties = new NameValueCollection();
                    XPathNodeIterator iterator = this.Navigator.Select("//tc:Properties", this.Manager);
                    foreach (XPathNavigator selectedNode in iterator)
                    {
                        XPathNodeIterator children = selectedNode.SelectChildren(XPathNodeType.All);
                        foreach (XPathNavigator child in children)
                        {
                            AddProperties(child, "");
                        }
                    }
                }
                return properties;
            }
        }

        private bool AddProperties(XPathNavigator selectedNode, string prefix)
        {
            string name, value;
            if (selectedNode.Name == "Property" && selectedNode.HasAttributes)
            {
                name = prefix + selectedNode.GetAttribute("name", "");
                value = selectedNode.GetAttribute("value", "");
                if (properties[name] != null)
                {
                    string msg = String.Format(
                        "Duplicate property ({0}) is found in the configuration.",
                        name);
                    throw new XmlException(msg);
                }
                properties.Add(name, value);
            }
            else
            {
                if (selectedNode.Name == "Group" && selectedNode.HasAttributes)
                {
                    XPathNodeIterator children = selectedNode.SelectChildren(XPathNodeType.All);
                    foreach (XPathNavigator child in children)
                    {
                        AddProperties(child, prefix + selectedNode.GetAttribute("name", "") + ".");
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the configuration that specifies whether it should throw an exception for assert/assume failure.
        /// </summary>
        public int AssertFailuresBeforeThrowException
        {
            get
            {
                if (!isParsed)
                {
                    ConfigureNodesParser();
                }
                return this.assertFailuresBeforeThrowException;
            }
        }
        private int assertFailuresBeforeThrowException;

        /// <summary>
        /// Gets the configuration which specifies the max bypassed failure messages 
        /// will be displayed in error message.
        /// </summary>
        public int MaxFailuresToDisplayPerTestCase
        {
            get
            {
                if (!isParsed)
                {
                    ConfigureNodesParser();
                }
                return this.maxFailuresToDisplayPerTestCase;
            }
        }
        private int maxFailuresToDisplayPerTestCase;

        #endregion

        #region private helper methods

        /// <summary>
        /// Merges doc into docBase
        /// </summary>
        /// <param name="docBase"></param>
        /// <param name="doc"></param>
        private void MergeXmlDocument(XmlDocument docBase, XmlDocument doc)
        {
            XmlNode root = docBase.DocumentElement;

            //Create an XmlNamespaceManager for resolving namespaces.
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(docBase.NameTable);
            nsmgr.AddNamespace("tc", DefaultNamespace);

            XmlNode baseNode = root.SelectSingleNode("tc:TestRun", nsmgr);
            XmlNode node = doc.DocumentElement.SelectSingleNode("tc:TestRun", nsmgr);
            if (node != null)
            {
                if (baseNode == null)
                {
                    root.InsertBefore(
                        docBase.ImportNode(node, true).CloneNode(true),
                        root.SelectSingleNode("tc:Properties", nsmgr));
                }
                else
                {
                    root.ReplaceChild(docBase.ImportNode(node, true).CloneNode(true), baseNode);
                }
            }

            baseNode = root.SelectSingleNode("tc:Properties", nsmgr);
            node = doc.DocumentElement.SelectSingleNode("tc:Properties", nsmgr);
            MergePropertyAndGroup(baseNode, docBase.ImportNode(node, true), "name");

            node = doc.DocumentElement.SelectSingleNode("tc:Adapters", nsmgr);
            if (node != null)
            {
                baseNode = root.SelectSingleNode("tc:Adapters", nsmgr);
                AppendOrOverrideChild(baseNode, docBase.ImportNode(node, true), "name");
            }

            node = doc.DocumentElement.SelectSingleNode("tc:TestLog", nsmgr);
            if (node != null)
            {
                baseNode = root.SelectSingleNode("tc:TestLog", nsmgr);
                baseNode.Attributes["defaultprofile"].Value =
                    node.Attributes["defaultprofile"].Value;
            }

            node = doc.DocumentElement.SelectSingleNode("tc:TestLog/tc:Sinks", nsmgr);
            if (node != null)
            {
                baseNode = root.SelectSingleNode("tc:TestLog/tc:Sinks", nsmgr);
                AppendOrOverrideChild(baseNode, docBase.ImportNode(node, true), "id");
            }

            node = doc.DocumentElement.SelectSingleNode("tc:TestLog/tc:Profiles", nsmgr);
            if (node != null)
            {
                baseNode = root.SelectSingleNode("tc:TestLog/tc:Profiles", nsmgr);
                AppendOrOverrideChild(baseNode, docBase.ImportNode(node, true), "name");
            }

            node = doc.DocumentElement.SelectSingleNode("tc:TestReport", nsmgr);
            if (node != null)
            {
                baseNode = root.SelectSingleNode("tc:TestReport", nsmgr);
                if (baseNode == null)
                {
                    root.AppendChild(docBase.ImportNode(node, true).CloneNode(true));
                }
                else
                {
                    if (baseNode.Name == "TestReport")
                    {
                        baseNode.RemoveAll();
                        baseNode.Attributes.Append((XmlAttribute)docBase.ImportNode(node.Attributes["autoGenerate"], true));
                        foreach (XmlNode childNode in node.ChildNodes)
                        {
                            baseNode.AppendChild(docBase.ImportNode(childNode, true));
                        }
                    }
                }
            }


        }
        /// <summary>
        /// Merges multiple test configuration files including default and user defined ones into one XmlDocument.
        /// </summary>
        /// <param name="configFileNames">The names of test configuration files. It contains three elements.</param>
        /// <returns>The XmlDocument which contains data of all test configuration files.</returns>
        private XmlDocument MergeConfigFiles(string[] configFileNames)
        {
            try
            {
                if (configFileNames == null || configFileNames.Length < 2)
                {
                    throw new ArgumentException("At least two PTF config files should be passed in.");
                }
                
                Logging.ApplicationLog.TraceLog("Try to load " + configFileNames.Length + " config files.");

                XmlDocument docBase = new XmlDocument();
                docBase.XmlResolver = null;
                Logging.ApplicationLog.TraceLog("Loading configFileNames[0] :" + configFileNames[0]);
                docBase.Load(XmlReader.Create(configFileNames[0], new XmlReaderSettings() { XmlResolver = null }));

                Stack<XmlDocument> xmlDocs = new Stack<XmlDocument>();
                Stack<string> xmlDocsName = new Stack<string>();
                Stack<string> configFiles = new Stack<string>();
                for (int n = 1; n < configFileNames.Length; n++)
                {
                    if (configFileNames[n] != null) configFiles.Push(configFileNames[n]);
                }
                while (configFiles.Count > 0)
                {
                    string fileName = configFiles.Pop();

                    // Ignore multiple reference.
                    if (xmlDocsName.Contains(fileName))
                    {
                        Logging.ApplicationLog.TraceLog("Ignore multiple references: " + fileName);
                        continue;
                    }

                    XmlDocument doc = new XmlDocument();
                    doc.XmlResolver = null;
                    Logging.ApplicationLog.TraceLog("Loading config file:" + fileName);
                    doc.Load(XmlReader.Create(fileName, new XmlReaderSettings() { XmlResolver = null }));
                    xmlDocs.Push(doc);
                    xmlDocsName.Push(fileName);
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                    nsmgr.AddNamespace("tc", DefaultNamespace);
                    XmlNode root = doc.DocumentElement;
                    XmlNode incNode = root.SelectSingleNode("tc:Include", nsmgr);
                    if (incNode != null)
                    {
                        foreach (XmlNode nod in incNode.ChildNodes)
                        {
                            FileInfo fi = new FileInfo(fileName);
                            string path = Path.Combine(fi.DirectoryName, nod.Attributes["name"].Value);
                            if (!ValidateConfigFiles(new string[] { path }, false))
                            {
                                throw new XmlException(
                                    String.Format("Validating configuration {0} failed: {1}",
                                        invalidFilename,
                                        validateErrorMessages));
                            }
                            configFiles.Push(path);
                        }
                    }
                }
                while (xmlDocs.Count > 0)
                {
                    XmlDocument doc = xmlDocs.Pop();
                    string configFileName = xmlDocsName.Pop();
                    try
                    {
                        MergeXmlDocument(docBase, doc);
                    }
                    catch (XmlException e)
                    {
                        throw new InvalidOperationException(
                            String.Format(
                                "Merging the configuration file ({0}) failed. " +
                                "Please make sure it is valid. Otherwise, please validate the Xml namespace and schema location in this file. " +
                                "To specify the correct Xml namespace and schema location, " +
                                "the TestSite tag in configuration file(s) should be " + DefaultTestSiteTag,
                                configFileName),
                            e);

                    }
                    catch (InvalidOperationException e)
                    {
                        throw new InvalidOperationException(
                            String.Format(
                                "Merging the configuration file ({0}) failed. " +
                                "Please make sure it is valid. Otherwise, please validate the Xml namespace and schema location in this file. " +
                                "To specify the correct Xml namespace and schema location, " +
                                "the TestSite tag in configuration file(s) should be " + DefaultTestSiteTag,
                                configFileName),
                            e);

                    }
                }

                this.Document = docBase;

                Logging.ApplicationLog.TraceLog("Merged config file content: " + docBase.OuterXml);

                return docBase;
            }
            catch (XmlException e)
            {
                throw new InvalidOperationException("Failed to read test configuration file.", e);
            }
        }

        //Append or override report child node
        private static void AppendOrOverrideChildNode(XmlNode newFather, XmlNode importChild, string name)
        {
            string value = importChild.Attributes[name].Value;
            bool duplicate = false;

            XmlNode old = null;
            foreach (XmlNode xn in newFather.ChildNodes)
            {
                if (xn.NodeType != XmlNodeType.Element)
                    continue;
                if (xn.Name != importChild.Name)
                    continue;
                if (xn.Attributes[name].Value == value)
                {
                    duplicate = true;
                    old = xn;
                    break;
                }
            }
            if (duplicate)
            {
                newFather.ReplaceChild(importChild.CloneNode(true), old);
            }
            else
            {
                newFather.AppendChild(importChild.CloneNode(true));
            }
        }

        private static void MergePropertyAndGroup(XmlNode newParent, XmlNode mergingParent, string name)
        {
            Dictionary<string, XmlNode> propertyDict = new Dictionary<string, XmlNode>();
            Dictionary<string, XmlNode> groupDict = new Dictionary<string, XmlNode>();
            string value = "";
            //record the first property node where the Group node to insert before
            XmlNode firstProperty = null;
            foreach (XmlNode child in mergingParent.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    value = child.Attributes[name].Value;
                    try
                    {
                        if (child.Name == "Property")
                        {
                            propertyDict.Add(value, child);
                        }
                        else
                            groupDict.Add(value, child);
                    }
                    catch
                    {
                        throw new InvalidOperationException("Duplicate node with type\"" + child.Name + "name \"" + name + "\"=\"" + value + "\" found.");
                    }
                }
            }
            foreach (XmlNode child in groupDict.Values)
            {
                bool duplicate = false;
                XmlNode old = null;
                foreach (XmlNode xn in newParent.ChildNodes)
                {
                    if (xn.NodeType != XmlNodeType.Element)
                        continue;
                    if (xn.Name == "Property" && firstProperty == null)
                    {
                        firstProperty = xn;
                        continue;
                    }
                    if (xn.Name == "Group" && xn.Attributes[name].Value == child.Attributes[name].Value)
                    {
                        duplicate = true;
                        old = xn;
                        break;
                    }
                }
                if (duplicate)
                {
                    MergePropertyAndGroup(old, child.CloneNode(true), name);
                }
                else
                {
                    if(firstProperty == null)
                        newParent.AppendChild(child.CloneNode(true));
                    else
                        newParent.InsertBefore(child.CloneNode(true), firstProperty);
                }
            }
            foreach (XmlNode child in propertyDict.Values)
            {
                bool duplicate = false;
                XmlNode old = null;
                foreach (XmlNode xn in newParent.ChildNodes)
                {
                    if (xn.NodeType != XmlNodeType.Element || xn.Name != "Property")
                        continue;
                    if (xn.Attributes[name].Value == child.Attributes[name].Value)
                    {
                        duplicate = true;
                        old = xn;
                        break;
                    }
                }
                if (duplicate)
                {
                    newParent.ReplaceChild(child.CloneNode(true), old);
                }
                else
                {
                    newParent.AppendChild(child.CloneNode(true));
                }
            }
            groupDict.Clear();
            propertyDict.Clear();
        }

        private static void AppendOrOverrideChild(XmlNode newFather, XmlNode fatherOfChildren, string name)
        {
            Dictionary<string, XmlNode> dict = new Dictionary<string, XmlNode>();
            string value = "";
            foreach (XmlNode child in fatherOfChildren.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    value = child.Attributes[name].Value;
                    try
                    {
                        dict.Add(value, child);
                    }
                    catch
                    {
                        throw new InvalidOperationException("Duplicate node with name \"" + name + "\"=\"" + value + "\" found.");
                    }
                }
            }

            foreach (XmlNode child in dict.Values)
            {
                bool duplicate = false;

                XmlNode old = null;
                foreach (XmlNode xn in newFather.ChildNodes)
                {
                    if (xn.NodeType != XmlNodeType.Element)
                        continue;

                    if (xn.Attributes[name].Value == child.Attributes[name].Value)
                    {
                        duplicate = true;
                        old = xn;
                        break;
                    }
                }
                if (duplicate)
                {
                    newFather.ReplaceChild(child.CloneNode(true), old);
                }
                else
                {
                    newFather.AppendChild(child.CloneNode(true));
                }
            }

            dict.Clear();
        }


        /// <summary>
        /// Gets the adapter attribute from test configuration file. This method is used for getting necessary 
        /// parameters in construction of adapters.
        /// </summary>
        /// <param name="adapterName">The name of adapter requested.</param>
        /// <param name="attributeName">The name of attribute requested.</param>
        /// <param name="attributeNamespaceURI">The namespaceURI of attribute requested.</param>
        /// <returns>The attribute in string as requested in test configuration file.</returns>
        private string GetAdapterAttribute(string adapterName, string attributeName, string attributeNamespaceURI)
        {
            string targetAttribute = this.TryGetAdapterAttribute(adapterName, attributeName, attributeNamespaceURI);

            if (targetAttribute == null)
            {
                throw new InvalidOperationException(
                    String.Format(
                    "Adapter attribute {0} is not available for adapter {1}.",
                    attributeName, adapterName));
            }
            return targetAttribute;
        }

        /// <summary>
        /// Tries to get adapter attribute from test configuration file. This method is used for getting optional 
        /// parameters in the construction of adapters.
        /// </summary>
        /// <param name="adapterName">The name of adapter requested.</param>
        /// <param name="attributeName">The name of attribute requested.</param>
        /// <param name="attributeNamespaceURI">The namespaceURI of attribute requested.</param>
        /// <returns>The attribute in string as requested in test configuration file, or null, if not available.</returns>
        private string TryGetAdapterAttribute(string adapterName, string attributeName, string attributeNamespaceURI)
        {
            XPathNavigator currentNode;
            string nodePath;

            // Search for the target adapter with specified name.
            nodePath = "//tc:Adapter[@name='" + adapterName + "']";
            currentNode = this.Navigator.SelectSingleNode(nodePath, this.Manager);
            if (currentNode == null)
            {
                throw new InvalidOperationException
                    (String.Format("Cannot get adapter {0}, please check whether the 'name' attribute is configured correctly.",
                                    adapterName));
            }

            // Gets target attribute.
            string attributeString = currentNode.GetAttribute(attributeName, attributeNamespaceURI);
            if (attributeString.Trim() != null && attributeString.Trim().Length == 0)
                return null;
            else
                return attributeString;
        }

        private static string GetTimeStampString()
        {
            DateTime dt = DateTime.Now;
            string timeStampFormat = "{0:D4}-{1:D2}-{2:D2} {3:D2}-{4:D2}-{5:D2}-{6:D3}";
            string timeStamp = string.Format(timeStampFormat,
                                            dt.Year,
                                            dt.Month,
                                            dt.Day,
                                            dt.Hour,
                                            dt.Minute,
                                            dt.Second,
                                            dt.Millisecond);
            return timeStamp;
        }

        #endregion

        #region Configure Node Parse

        private void ConfigureNodesParser()
        {
            if (this.NeedGenerateReport)
            {
                TestReportNodeParser(this.ReportNode);
            }
            ParseTestRunNode();
            isParsed = true;
        }

        private void TestReportNodeParser(XmlNode reportXmlNode)
        {
            if (reportXmlNode == null)
            {
                throw new InvalidOperationException("The report xml node should not be null");
            }

            //get requirement file information from test report config
            XmlNodeList requirementTables = reportXmlNode.SelectNodes("tc:RequirementFile", this.Manager);
            this.GetRequirementFiles(requirementTables);


            //get the log file information from test report config
            XmlNodeList logSinks = reportXmlNode.SelectNodes("tc:LogFile", this.Manager);
            this.GetLogFileSinks(logSinks);

            //get report file information from test report config
            XmlNode reportFile = reportXmlNode.SelectSingleNode("tc:Report", this.Manager);
            if (reportFile == null)
            {
                string timeStamp = GetTimeStampString();
                StringBuilder sb = new StringBuilder();
                sb.Append("TestReport " + timeStamp + ".html");
                testReportOutputFile = sb.ToString();
                needAutoDisplay = true;
            }
            else
            {
                GetReportFileConfig(reportFile);
            }

            XmlNode inScopeNode = reportXmlNode.SelectSingleNode("tc:InScope", this.manager);
            if (inScopeNode != null)
            {
                GetScopeParameter(inScopeNode, ScopeKind.In);
            }

            XmlNode outScopeNode = reportXmlNode.SelectSingleNode("tc:OutOfScope", this.manager);
            if (outScopeNode != null)
            {
                GetScopeParameter(outScopeNode, ScopeKind.Out);
            }

            XmlNode prefixNode = reportXmlNode.SelectSingleNode("tc:Prefix", this.manager);
            if (prefixNode != null)
            {
                XmlAttribute value = prefixNode.Attributes["value"];
                if (value != null)
                {
                    prefix = value.Value.Trim();
                    if (string.IsNullOrEmpty(prefix))
                    {
                        throw new XmlException("The configured value in Prefix node cannot be null or empty.");
                    }
                }
            }

            XmlAttribute attrVerbose = reportXmlNode.Attributes["verbose"];
            if (attrVerbose != null)
            {
                string verboseValue = attrVerbose.Value.Trim();
                this.verboseMode = TestToolHelpers.XmlBoolToBool(verboseValue);
            }
        }

        private enum ScopeKind
        {
            In,
            Out
        }

        private void GetScopeParameter(XmlNode scopeNode, ScopeKind kind)
        {
            XmlNodeList scopes = scopeNode.SelectNodes("tc:Scope", this.manager);
            foreach (XmlNode scope in scopes)
            {
                XmlAttribute value = scope.Attributes["value"];
                if (kind == ScopeKind.In)
                {
                    if (string.IsNullOrEmpty(inScope))
                    {
                        inScope = value.Value;
                    }
                    else
                    {
                        inScope += "+" + value.Value;
                    }
                }
                else if (kind == ScopeKind.Out)
                {
                    if (string.IsNullOrEmpty(outScope))
                    {
                        outScope = value.Value;
                    }
                    else
                    {
                        outScope += "+" + value.Value;
                    }
                }
            }
        }

        private void GetRequirementFiles(XmlNodeList requirementTables)
        {
            if (requirementTables == null)
            {
                throw new XmlException("RequirementFile node is not found, at least one RequirementFile node is needed.");
            }
            this.requirementFiles = new string[requirementTables.Count];
            for (int i = 0; i < requirementTables.Count; i++)
            {
                XmlNode requirementTable = requirementTables[i];
                //Gets the location attribute in ReruirementFile node 
                //which represent the location of requirement file.
                XmlAttribute attrLocation = requirementTable.Attributes["location"];
                if (attrLocation == null)
                {
                    throw new XmlException("Attribute 'location' is needed for RequirementFile node");
                }
                string location = attrLocation.Value.Trim();
                if (string.IsNullOrEmpty(location))
                {
                    throw new InvalidOperationException(
                        "File location of requirement table should not be null or empty");
                }

                //Gets the full path of the location
                string fullPath = string.Empty;

                try
                {
                    //Translate relative path to absolute path
                    fullPath = Path.GetFullPath(location);

                    //Check if only contain directory without file name
                    string filename = Path.GetFileName(fullPath);
                    {
                        if (string.IsNullOrEmpty(filename))
                        {
                            throw new InvalidOperationException(
                                "Attribute 'location' cannot contain directory without file name.");
                        }
                        else
                        {
                            if (!fullPath.EndsWith(".xml", StringComparison.CurrentCultureIgnoreCase))
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.Append(fullPath);
                                sb.Append(".xml");
                                fullPath = sb.ToString();
                            }
                        }
                    }
                    requirementFiles[i] = fullPath;
                }
                catch
                {
                    throw new InvalidOperationException("invalid requirement file location: " + location);
                }
            }
        }

        private void GetLogFileSinks(XmlNodeList logSinks)
        {
            if (logSinks == null)
            {
                throw new XmlException("no log file provided, at least one log file needed.");
            }

            this.logFiles = new string[logSinks.Count];
            for (int j = 0; j < logSinks.Count; j++)
            {
                XmlNode logSink = logSinks[j];
                XmlAttribute attrSink = logSink.Attributes["sink"];
                if (attrSink == null)
                {
                    throw new XmlException("Attribute 'sink' is needed for LogFile node");
                }
                string sinkName = attrSink.Value.Trim();
                if (string.IsNullOrEmpty(sinkName))
                {
                    throw new XmlException("invalid sink name, the log sink name cannot be null or empty");
                }
                else
                {
                    logFiles[j] = sinkName;
                }

            }
        }

        private void GetReportFileConfig(XmlNode reportFile)
        {
            XmlAttribute attrReportName = reportFile.Attributes["name"];
            string fileName = string.Empty;
            if (attrReportName != null)
            {
                fileName = attrReportName.Value.Trim();
            }
            StringBuilder nameBuilder = new StringBuilder();
            if (string.IsNullOrEmpty(fileName))
            {
                //Auto generate a default name.
                string timeStamp = GetTimeStampString();
                nameBuilder.Append("TestReport " + timeStamp + ".html");
            }
            else
            {
                if (fileName.Contains("\\"))
                {
                    throw new InvalidOperationException(
                        "Invalid name, report file name cannot contain directory info."
                        + " please set the directory info in directory attribute"
                        );
                }
                nameBuilder.Append(fileName);
                if (!fileName.EndsWith(".html", StringComparison.CurrentCultureIgnoreCase))
                {
                    nameBuilder.Append(".html");
                }
            }
            fileName = nameBuilder.ToString();

            XmlAttribute attrDir = reportFile.Attributes["directory"];
            string directory = string.Empty;
            string path = string.Empty;
            if (attrDir != null)
            {
                this.useDefaultOutputDir = false;
                directory = attrDir.Value.Trim();
            }
            if (string.IsNullOrEmpty(directory))
            {
                path = fileName;
            }
            else
            {
                try
                {
                    path = Path.Combine(directory, fileName);
                }
                catch (Exception)
                {
                    throw new InvalidOperationException(
                        string.Format("invalid report directory {0}.", directory)
                        );
                }
            }

            try
            {
                this.testReportOutputFile = Path.GetFullPath(path);
            }
            catch (Exception)
            {
                throw new InvalidOperationException(
                        string.Format("Invalid report name or directory {0}.", path)
                        );
            }

            XmlAttribute attrAutoDisplay = reportFile.Attributes["autoDisplay"];
            this.GetAutoDisplaySwitch(attrAutoDisplay);
        }

        private void GetAutoDisplaySwitch(XmlAttribute attrAutoDisplay)
        {
            if (attrAutoDisplay != null)
            {
                string autoDisplay = string.Empty;
                autoDisplay = attrAutoDisplay.Value.Trim();
                this.needAutoDisplay = TestToolHelpers.XmlBoolToBool(autoDisplay);
            }
            else
            {
                //no attribute specified, should set default value true.
                this.needAutoDisplay = true;
            }
        }

        private void ParseTestRunNode()
        {
            const int defaultAssertsToBypass = 0;
            const int defaultMessagesToDisplay = 10;
            this.assertFailuresBeforeThrowException = defaultAssertsToBypass;
            this.maxFailuresToDisplayPerTestCase = defaultMessagesToDisplay;
            XmlNode testRun = this.Document.DocumentElement.SelectSingleNode(
                "/tc:TestSite/tc:TestRun", this.Manager);
            if (testRun != null)
            {
                this.assertFailuresBeforeThrowException = AttributeToInt(testRun, "AssertFailuresBeforeThrowException", defaultAssertsToBypass);
                this.maxFailuresToDisplayPerTestCase = AttributeToInt(testRun, "MaxFailuresToDisplayPerTestCase", defaultMessagesToDisplay);
            }
        }

        /// <summary>
        /// Gets the int value from attribute.
        /// </summary>
        private static int AttributeToInt(XmlNode node, string attrName, int dftValue)
        {
            const string attrValueCannotNull = "The value of attribute '{0}' in 'TestRun' element cannot be null or empty.";
            const string attrValueInvalid = "The value of attribute '{0}' in 'TestRun' element cannot less than {1}.";
            const string attrValueBadFormat = "The value of attribute '{0}' in 'TestRun' element is not in correct format. ";
            const string attrValueOverflow = "The value of attribute '{0}' in 'TestRun' element is too large. ";
            int retValue = dftValue;
            XmlAttribute attribute = node.Attributes[attrName];
            if (attribute != null)
            {
                string strValue = attribute.Value.Trim();
                if (string.IsNullOrEmpty(strValue))
                {
                    throw new InvalidOperationException(
                        string.Format(attrValueCannotNull, attrName));
                }
                try
                {
                    retValue = int.Parse(strValue);
                    if (retValue < 0)
                    {
                        throw new InvalidOperationException(
                            string.Format(attrValueInvalid, attrName, 0));
                    }
                }
                catch (FormatException e)
                {
                    throw new InvalidOperationException(
                        string.Format(attrValueBadFormat, attrName), e);
                }
                catch (OverflowException oe)
                {
                    throw new InvalidOperationException(
                        string.Format(attrValueOverflow, attrName), oe);
                }
            }
            return retValue;
        }

        #endregion

        #region Schema validate

        private bool validateResult;
        private string validateErrorMessages;
        private string invalidFilename;
        private XmlSchemaSet schemaSet;

        private void SetProperXmlSchemaSet(string schemaFileName)
        {
            // Create a schema cache.
            schemaSet = new XmlSchemaSet();
            if (schemaFileName == null)
            {
                TextReader tr = new StringReader(PTFConfig.TestConfig);
                schemaSet.Add(null, XmlReader.Create(tr, new XmlReaderSettings() { XmlResolver = null }));
            }
            else
            {
                schemaSet.Add(null, schemaFileName);
            }
        }

        private bool ValidateXmlDocument(XmlDocument xmldoc)
        {
            validateResult = true;

            xmldoc.Schemas = schemaSet;
            xmldoc.Validate(ValidationCallBack);

            if (!validateResult)
            {
                return false;
            }

            return true;
        }
        private bool ValidateConfigFiles(string[] configFilenames, bool processIdentityConstraints)
        {
            validateResult = true;

            foreach (string filename in configFilenames)
            {
                if (filename == null)
                    continue;

                using (XmlTextReader xmlReader = new XmlTextReader(filename) { DtdProcessing = DtdProcessing.Prohibit })
                {
                    // Create XML settings.
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.DtdProcessing = DtdProcessing.Prohibit;
                    settings.XmlResolver = null;
                    if (!processIdentityConstraints)
                        settings.ValidationFlags &= ~XmlSchemaValidationFlags.ProcessIdentityConstraints;
                    settings.ValidationType = ValidationType.Schema;
                    settings.Schemas = schemaSet;
                    settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);

                    using (XmlReader reader = XmlReader.Create(xmlReader, settings))
                    {
                        //Read and validate the XML data.
                        while (reader.Read() && validateResult) ;
                    }
                }
                if (!validateResult)
                {
                    // Validation failed return false;
                    invalidFilename = filename;
                    return false;
                }
            }
            // All files are valid.
            return true;
        }

        private void ValidationCallBack(object sender, ValidationEventArgs args)
        {
            // Validation failed.
            validateResult = false;
            validateErrorMessages = args.Message;
        }

        #endregion
    }
}

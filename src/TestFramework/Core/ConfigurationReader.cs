// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;

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

        private static bool isParsed;

        /// <summary>
        /// Gets the configuration XML file schema short name.
        /// </summary>
        private static string SchemaFile
        {
            get
            {
                return "Resources.Schema.TestConfig.xsd";
            }
        }

        /// <summary>
        /// Gets the default ptfconfig file short name.
        /// </summary>
        private static string DefaultPtfConfig
        {
            get
            {
                return "Resources.site.ptfconfig";
            }
        }

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
        internal ConfigurationReader(string[] configFileNames)
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
            SetXmlSchemaSet();

            if (!ValidateDefaultConfig() ||
                !ValidateConfigFiles(configFileNames))
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
                string adapterTypeName = this.TryGetAdapterAttribute(adapterName, "adaptertype", "");
                adapter = new InteractiveAdapterConfig(adapterName, adapterTypeName);
            }
            // Create proxy for PowerShell script type adapter
            else if (type.Equals("powershell", StringComparison.CurrentCultureIgnoreCase))
            {
                string psdir = this.GetAdapterAttribute(adapterName, "scriptdir", "");
                adapter = new PowerShellAdapterConfig(adapterName, psdir);
            }
            // Create proxy for Shell script type adapter
            else if (type.Equals("shell", StringComparison.CurrentCultureIgnoreCase))
            {
                string scriptdir = this.GetAdapterAttribute(adapterName, "scriptdir", "");
                adapter = new ShellAdapterConfig(adapterName, scriptdir);
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
            else
            {
                throw new ArgumentException(String.Format("Unsupported adapter type: {0}", type));
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
                            string identity = string.Empty;
                            if (sink.Attributes["identity"] != null)
                            {
                                identity = sink.Attributes["identity"].Value;
                            }
                            
                            logSinks.Add(new CustomLogSinkConfig(
                                name,
                                sink.Attributes["type"].Value,
                                identity));
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
        /// Get stream from a bundled resource
        /// </summary>
        /// <param name="path">dot separated path of the resource file</param>
        private Stream GetBundledResource(string path)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string project = assembly.GetName().Name;
            Stream stream = assembly.GetManifestResourceStream($"{project}.{path}");
            return stream;
        }

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
                if (configFileNames == null || configFileNames.Length == 0)
                {
                    throw new ArgumentException("At least one PTF config file should be passed in.");
                }

                Logging.ApplicationLog.TraceLog("Try to load " + configFileNames.Length + " config files.");

                XmlDocument docBase = new XmlDocument();
                docBase.XmlResolver = null;

                // Load site.ptfconfig
                Logging.ApplicationLog.TraceLog("Loading " + DefaultPtfConfig);
                Stream sitePtfconfigStream = GetBundledResource(DefaultPtfConfig);
                docBase.Load(XmlReader.Create(sitePtfconfigStream, new XmlReaderSettings() { XmlResolver = null }));

                Stack<XmlDocument> xmlDocs = new Stack<XmlDocument>();
                Stack<string> xmlDocsName = new Stack<string>();
                Stack<string> configFiles = new Stack<string>();
                for (int n = 0; n < configFileNames.Length; n++)
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
                            if (!ValidateConfigFiles(new string[] { path }))
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
            ParseTestRunNode();
            isParsed = true;
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

        private void SetXmlSchemaSet()
        {
            // Create a schema cache.
            schemaSet = new XmlSchemaSet();

            Stream schemaStream = GetBundledResource(SchemaFile);
            XmlSchema schema = XmlSchema.Read(schemaStream, null);
            schemaSet.Add(schema);
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

        private bool ValidateDefaultConfig()
        {
            validateResult = true;

            Stream sitePtfconfigStream = GetBundledResource(DefaultPtfConfig);

            if (!ValidateConfigFile(sitePtfconfigStream, true))
            {
                invalidFilename = DefaultPtfConfig;
                return false;
            }
            return true;
        }

        private bool ValidateConfigFiles(string[] configFilenames)
        {
            validateResult = true;

            foreach (string filename in configFilenames)
            {
                if (filename == null)
                    continue;

                using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (!ValidateConfigFile(fs, false))
                    {
                        invalidFilename = filename;
                        return false;
                    }
                }
            }
            // All files are valid.
            return true;
        }

        private bool ValidateConfigFile(Stream input, bool processIdentityConstraints)
        {
            validateResult = true;

            using (XmlTextReader xmlReader = new XmlTextReader(input) { DtdProcessing = DtdProcessing.Prohibit })
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

            return validateResult;
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

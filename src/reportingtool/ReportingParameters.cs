// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Text.RegularExpressions;

namespace Microsoft.Protocols.ReportingTool
{
    internal class ReportingParameters
    {
        private StringCollection xmlLogs;
        private StringCollection requirementTables;
        private string rsPrefix;
        private bool inScopeOverrided;
        private bool outScopeOverrided;
        public ReportingParameters()
        {
        }
        public StringCollection XmlLogs
        {
            get
            {
                if (xmlLogs == null)
                {
                    xmlLogs = new StringCollection();
                }
                return xmlLogs;
            }
        }
        public StringCollection RequirementTables
        {
            get
            {
                if (requirementTables == null)
                {
                    requirementTables = new StringCollection();
                }
                return requirementTables;
            }
        }

        private string outputFile;

        public String OutputFile
        {
            set
            {
                if (String.IsNullOrEmpty(outputFile))
                {
                    outputFile = value;
                    if (!value.EndsWith(".html") && !value.EndsWith(".htm"))
                    {
                        outputFile += ".html";
                    }
                }
                else
                {
                    throw new InvalidOperationException("[ERROR] Duplicate output filename parameter specified.");
                }
            }
            get
            {
                if (String.IsNullOrEmpty(outputFile))
                {
                    // default reporting filename
                    return String.Format("report{0}.html",
                        DateTime.Now.ToString("yyyyMMddhhmmss"));
                }
                else
                {
                    return outputFile;
                }
            }
        }

        /// <summary>
        /// A string prefix from the TD name, the format is "prefix_R".
        /// </summary>
        public string RSPrefix
        {
            get
            {
                return rsPrefix;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    rsPrefix = value;
                    if (rsPrefix.Contains(" "))
                    {
                        throw new InvalidOperationException(
                            "Prefix cannot contain spaces.");
                    }

                    //remove end "_"
                    Regex regex = new Regex(@"_+\b");
                    rsPrefix = regex.Replace(rsPrefix, "");

                    if (!rsPrefix.EndsWith("_R"))
                    {
                        rsPrefix += "_R";
                    }
                }
                else
                {
                    rsPrefix = null;
                }
            }
        }

        /// <summary>
        /// Check if all required arguments are meet.
        /// There must at least one reabable test log file and at least one reabable requirement table file.
        /// Check if files specified by parameters are available to read.
        /// Check if output file has already exists, if it is writable.
        /// </summary>
        public void Validate(bool scopeMode)
        {
            bool logflag = XmlLogs.Count > 0;
            bool tableflag = RequirementTables.Count > 0;
            if (!logflag)
            {
                throw new InvalidOperationException("[ERROR] Test log file name is not specified.");
            }
            if (!tableflag)
            {
                throw new InvalidOperationException("[ERROR] Requirement Specification file name is not specified.");
            }
            StringCollection invalidfiles = ValidateExistence();

            // warning user the invalid files.
            if (invalidfiles.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (string filename in invalidfiles)
                {
                    sb.AppendLine(filename);
                }
                throw new InvalidOperationException(
                    String.Format("[ERROR] Unable to find the files: {0}", sb.ToString()));
            }

            ParseScopeRules(scopeMode);

            // check req table files against internal schema
            using (TextReader tr = new StringReader(Resource.requirementTable))
            {
                XmlSchemaSet schemaSet = new XmlSchemaSet();
                schemaSet.Add(null, XmlReader.Create(tr, new XmlReaderSettings() { XmlResolver = null }));
                foreach (string filename in RequirementTables)
                {
                    // Create XML settings.
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.ValidationType = ValidationType.Schema;
                    settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
                    settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessIdentityConstraints;
                    settings.Schemas = schemaSet;
                    settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
                    settings.XmlResolver = null;
                    validateResult = true;
                    using (XmlReader reader = XmlReader.Create(filename, settings))
                    {
                        //Read and validate the XML data.
                        while (reader.Read()) { }
                    }
                    if (!validateResult)
                    {
                        // Validation failed return false;
                        throw new InvalidOperationException(
                            String.Format("[ERROR] Error occured while validating requirement table file {0}.\r\nTechnical details:\r\n{1}", filename, validateErrorMessages));
                    }
                }
            }

            // check output file
            if (File.Exists(OutputFile))
            {
                FileStream fs = null;
                try
                {
                    fs = File.OpenWrite(OutputFile);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        String.Format("[ERROR] Unable to open file {0} for reporting result output. Details:\r\n{1}",
                        OutputFile, e.Message));
                }
                finally
                {
                    if (fs != null)
                    {
                        fs.Close();
                    }
                }
            }
        }

        private StringCollection ValidateExistence()
        {
            StringCollection validfiles = new StringCollection();
            StringCollection invalidfiles = new StringCollection();

            // remove all invalid log files
            foreach (string filename in XmlLogs)
            {
                if (File.Exists(filename))
                {
                    FileInfo fi = new FileInfo(filename);
                    validfiles.Add(fi.FullName.ToLower());
                }
                else
                {
                    invalidfiles.Add(filename);
                }
            }
            xmlLogs.Clear();
            foreach (string filename in validfiles)
            {
                if (!xmlLogs.Contains(filename))
                {
                    xmlLogs.Add(filename);
                }
            }
            validfiles.Clear();

            // remove all invalid req table files
            foreach (string filename in RequirementTables)
            {
                if (File.Exists(filename))
                {
                    FileInfo fi = new FileInfo(filename);
                    validfiles.Add(fi.FullName.ToLower());
                }
                else
                {
                    invalidfiles.Add(filename);
                }
            }
            requirementTables.Clear();
            foreach (string filename in validfiles)
            {
                if (!requirementTables.Contains(filename))
                {
                    requirementTables.Add(filename);
                }
            }
            return invalidfiles;
        }

        public static string inScopeRule = "inScopeRule";
        public static string outScopeRule = "outScopeRule";

        Dictionary<string, List<string>> scopeRules = new Dictionary<string, List<string>>();
        /// <summary>
        /// Gets the in/out scope rules for the scope value.
        /// </summary>
        public Dictionary<string, List<string>> ScopeRules
        {
            get
            {
                return scopeRules;
            }
        }

        private void ParseScopeRules(bool isScopeMode)
        {
            if (isScopeMode)
            {
                // error handling for in/out scope parameters.
                if (!inScopeOverrided && !outScopeOverrided)
                {
                    throw new InvalidOperationException("Please explicitly specify in/out scope parameters.");
                }
                else if (inScopeOverrided && !outScopeOverrided)
                {
                    throw new InvalidOperationException("Please explicitly specify out-of-scope parameter.");
                }
                else if (!inScopeOverrided && outScopeOverrided)
                {
                    throw new InvalidOperationException("Please explicitly specify in-scope parameter.");
                }
            }

            string[] inscopes = this.inScopeString.Split('+');
            foreach (string inscope in inscopes)
            {
                if (!scopeRules.ContainsKey(inScopeRule))
                {
                    scopeRules[inScopeRule] = new List<string>();
                    if (string.Compare(inscope, noneValue, true) != 0)
                    {
                        scopeRules[inScopeRule].Add(inscope.ToLower());
                    }
                    else
                    {
                        if (inscopes.Length > 1)
                        {
                            throw new InvalidOperationException(
                                "None keyword is not allowed as the in scope value. If no value specified to in-scope, please use 'None' only.");
                        }
                        break;
                    }
                }
                else
                {
                    if (!scopeRules[inScopeRule].Contains(inscope.ToLower()))
                    {
                        if (string.Compare(inscope, noneValue, true) != 0)
                        {
                            scopeRules[inScopeRule].Add(inscope.ToLower());
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                "None keyword is not allowed as the in scope value. If no value specified to in-scope, please use 'None' only.");
                        }
                    }
                }
            }

            string[] outscopes = this.outScopeString.Split('+');
            foreach (string outscope in outscopes)
            {
                if (!scopeRules.ContainsKey(outScopeRule))
                {
                    scopeRules[outScopeRule] = new List<string>();
                    if (string.Compare(outscope, noneValue, true) != 0)
                    {
                        scopeRules[outScopeRule].Add(outscope.ToLower());
                    }
                    else
                    {
                        if (outscopes.Length > 1)
                        {
                            throw new InvalidOperationException(
                                "None keyword is not allowed as the in-scope value. If no value specified to in scope, please use 'None' only.");
                        }
                        break;
                    }
                }
                else
                {
                    if (!scopeRules[outScopeRule].Contains(outscope.ToLower()))
                    {
                        if (string.Compare(outscope, noneValue, true) != 0)
                        {
                            scopeRules[outScopeRule].Add(outscope.ToLower());
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                "None keyword is not allowed as the out-of-scope value. If no value specified to out scope, please use 'None' only.");
                        }
                    }
                }
            }
        }

        private static bool validateResult;
        private static string validateErrorMessages;
        static private void ValidationCallBack(object sender, ValidationEventArgs args)
        {
            // Validation failed. Treat warning as failure
            validateResult = false;
            validateErrorMessages = args.Message;
        }

        private bool prefix;

        public bool Prefix
        {
            get { return prefix; }
            set
            {
                prefix = value;
                if (prefix)
                {
                    log = false;
                    table = false;
                    output = false;
                    inScope = false;
                    outScope = false;
                    deltaScope = false;
                }
            }
        }

        private bool log;

        public bool Log
        {
            get { return log; }
            set
            {
                log = value;
                if (log)
                {
                    prefix = false;
                    table = false;
                    output = false;
                    inScope = false;
                    outScope = false;
                    deltaScope = false;
                }
            }
        }

        private bool table;

        public bool Table
        {
            get { return table; }
            set
            {
                table = value;
                if (table)
                {
                    prefix = false;
                    log = false;
                    output = false;
                    inScope = false;
                    outScope = false;
                    deltaScope = false;
                }
            }
        }

        private bool output;

        public bool Output
        {
            get { return output; }
            set
            {
                output = value;
                if (output)
                {
                    prefix = false;
                    table = false;
                    log = false;
                    inScope = false;
                    outScope = false;
                    deltaScope = false;
                }
            }
        }

        //reporting tool will not replace the old report file by default
        //user can set /r or /replace switch to replace the old report file
        private bool replace;

        public bool Replace
        {
            get { return replace; }
            set
            {
                replace = value;
                if (replace)
                {
                    prefix = false;
                    table = false;
                    log = false;
                    output = false;
                    inScope = false;
                    outScope = false;
                    deltaScope = false;
                }
            }
        }

        private bool inScope;
        public bool InScope
        {
            get { return inScope; }
            set
            {
                inScope = value;
                if (inScope)
                {
                    prefix = false;
                    table = false;
                    log = false;
                    output = false;
                    outScope = false;
                    deltaScope = false;
                }
            }
        }

        private bool outScope;
        public bool OutScope
        {
            get { return outScope; }
            set
            {
                outScope = value;
                if (outScope)
                {
                    prefix = false;
                    table = false;
                    log = false;
                    output = false;
                    inScope = false;
                    deltaScope = false;
                }
            }
        }

        //the scope parameter should match the format like XXX[+XXX]*, spaces are not allowed.
        Regex regex = new Regex("^\\w+(\\+\\w+)*$", RegexOptions.Compiled);
        readonly string noneValue = "none";

        //default value of inscope is Server+Both
        private string inScopeString = "Server+Both";
        public string InScopeString
        {
            get
            {
                return inScopeString;
            }
            set
            {
                if (regex.IsMatch(value) || string.Compare(noneValue, value, true) == 0)
                {
                    inScopeString = value;
                    inScopeOverrided = true;
                }
                else
                {
                    throw new InvalidOperationException(
                        String.Format("Invalid inscope string fomat \"{0}\", please use the correct format e.g. Server+Both. "
                        + "Don't allow spaces in the inscope argument string.", value));
                }
            }
        }

        //default value of outscope is client
        private string outScopeString = "client";
        public string OutScopeString
        {
            get
            {
                return outScopeString;
            }
            set
            {
                if (regex.IsMatch(value) || string.Compare(noneValue, value, true) == 0)
                {
                    outScopeString = value;
                    outScopeOverrided = true;
                }
                else
                {
                    throw new InvalidOperationException(
                        String.Format("Invalid outscope string fomat \"{0}\", please use the correct format e.g. Server+Both. "
                        + "Don't allow spaces in the out-of-scope argument string.", value));
                }
            }
        }

        /// <summary>
        /// Gets whether inscope parameter is explicitly specified. 
        /// </summary>
        public bool InScopeOverrided
        {
            get { return inScopeOverrided; }
        }

        /// <summary>
        /// Gets whether outscope parameter is explicitly specified.
        /// </summary>
        public bool OutScopeOverided
        {
            get { return outScopeOverrided; }
        }

        //specify the current parameter is delta scope value.
        private bool deltaScope;
        public bool DeltaScope
        {
            get { return deltaScope; }
            set
            {
                deltaScope = value;
                if (deltaScope)
                {
                    prefix = false;
                    table = false;
                    log = false;
                    output = false;
                    inScope = false;
                    outScope = false;
                }
            }
        }

        private string deltaScopeString;
        public string DeltaScopeString
        {
            get { return deltaScopeString; }
            set
            {
                if (regex.IsMatch(value))
                {
                    deltaScopeString = value;
                }
                else
                {
                    throw new InvalidOperationException(
                        String.Format("Invalid delta string fomat \"{0}\", please use the correct format e.g. Changed+New. "
                        + "Don't allow spaces in the delta scope argument string.", value));
                }
            }
        }

        private List<string> deltaScopeValues = new List<string>();
        public List<string> DeltaScopeValues
        {
            get 
            {
                if (deltaScopeValues.Count == 0)
                {
                    GetDeltaScopeValues();
                }
                return deltaScopeValues; 
            }
        }

        private void GetDeltaScopeValues()
        {
            if (string.IsNullOrEmpty(deltaScopeString))
            {
                return;
            }

            string[] deltaScopes = deltaScopeString.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string delta in deltaScopes)
            {
                if (!string.IsNullOrEmpty(delta.Trim()))
                {
                    switch(delta.Trim().ToLower())
                    {
                        case "new":
                            deltaScopeValues.Add("new");
                            break;
                        case "changed":
                            deltaScopeValues.Add("changed");
                            break;
                        case "unchanged":
                            deltaScopeValues.Add("unchanged");
                            deltaScopeValues.Add("editorial");
                            deltaScopeValues.Add("sectionmoved");
                            break;
                        default:
                            throw new InvalidOperationException(
                                string.Format("Unsupported delta scope value {0}", delta));
                    }
                }
            }
        }
    }
}

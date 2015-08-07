// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace Microsoft.Protocols.ReportingTool
{
    internal class XmlLogAnalyzer
    {
        const string CHECKPOINT = "Checkpoint";
        const string COMMENT = "Comment";
        const string TESTPASSED = "TestPassed";
        const string SETTINGS = "Settings";
        const string CHECKPOINTSTATEMENT = "kind <> '" + CHECKPOINT + "'";
        const string CONFPROP = "PTFConfigProperties.";
        const string TESTRESULT = "PTFTestResult.";
        const string COMMENTSTATEMENT = "kind = '" + COMMENT + "'";
        const string SETTINGSSTATEMENT = "kind = '" + SETTINGS + "'";
        //Test result
        private static string[][] testStatusName = new string[7][] { 
                new string[2] {"TestPassed", "TestsPassed"}, 
                new string[2] {"TestFailed", "TestsFailed"}, 
                new string[2]{"TestInconclusive", "TestsInconclusive" }, 
                new string[2]{"TestError", "TestsError" }, 
                new string[2]{ "TestAborted", "TestsAborted"}, 
                new string[2]{"TestTimeout", "TestsTimeout" }, 
                new string[2] { "TestUnknown", "TestsUnknown"} };

        const string TESTOUTCOMESTATEMENT = "kind = '{0}'";

        // NOTICE: these two patterns are only used in TSD internally.
        static readonly Regex MSPATTERN = new Regex(@"^MS-[A-Za-z0-9]{2,8}_R\d{1,4}$", RegexOptions.Compiled);
        static readonly Regex RFCPATTERN = new Regex(@"^RFC\d{2,5}_R\d{1,4}$", RegexOptions.Compiled);

        // Use Dictionay as a Set
        private Dictionary<string, string> checkpointEntries;
        // All key:value pair of PTF configurations.
        private Dictionary<string, string> ptfConfigurations;
        // All TestResultType:Counter pair of Test Results stored in test log.
        private Dictionary<string, string> testResult;
        // Protocols covered by the test.
        private List<string> protocols;
        // All test cases with test outcome, key: test case name, value: test outcome
        private Dictionary<string, string> testCases;
        // All excluded requirements, key: req_id, value: test case name
        private Dictionary<string, string> excludedRequirements;
        // Timestamps for all excluded requirements, key: req_id, value: timestamp
        private Dictionary<string, string> excludedRequirementsTimestamp;

        protected XmlLogAnalyzer()
        {
            protocols = new List<string>();
        }
        public XmlLogAnalyzer(string logFilename)
            : this()
        {
            IList<string> filenames = new List<string>();
            filenames.Add(logFilename);
            Load(filenames);
        }
        public XmlLogAnalyzer(StringCollection logFilenames)
            : this()
        {
            IList<string> filenames = new List<string>();
            foreach (string filename in logFilenames)
            {
                filenames.Add(filename);
            }
            Load(filenames);
        }
        public XmlLogAnalyzer(IList<string> logFilenames)
            : this()
        {
            Load(logFilenames);
        }
        private void Load(IList<string> logFilenames)
        {
            string currentFile = string.Empty;
            try
            {
                TestLog logEntry = null;
                TestLog tempLogEntry = new TestLog();
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.XmlResolver = null;
                foreach (string logfile in logFilenames)
                {
                    currentFile = logfile;
                    //allow log file contain zero entry.
                    if (logEntry == null)
                    {
                        logEntry = new TestLog();
                        logEntry.ReadXml(XmlReader.Create(logfile, settings));
                    }
                    else
                    {
                        tempLogEntry.ReadXml(XmlReader.Create(logfile, settings));
                        logEntry.Merge(tempLogEntry);
                        tempLogEntry.Clear();
                    }
                }
                tempLogEntry.Dispose();

                currentFile = string.Empty;

                // get all ptf configurations
                GetStatsFromLogEntry(logEntry, ref ptfConfigurations, CONFPROP, SETTINGSSTATEMENT);

                // get all test results
                GetStatsFromLogEntry(logEntry, ref testResult, TESTRESULT, COMMENTSTATEMENT);

                //group all test cases by test outcome
                GetTestCasesWithOutcome(logEntry);

                // remove all entries except Checkpoint kind log entries
                DataRow[] rows = logEntry.LogEntry.Select(CHECKPOINTSTATEMENT);
                foreach (DataRow row in rows)
                {
                    row.Delete();
                }
                logEntry.LogEntry.AcceptChanges();

                // fill data for checkpoints
                FillCheckpointEntries(logEntry.LogEntry);

                logEntry.Dispose();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    String.Format("[ERROR] Unable to get test log data from specified xml log file(s) {0}. Details:\r\n{1}", currentFile, e.Message + e.StackTrace));
            }
        }

        private void GetTestCasesWithOutcome(TestLog logEntry)
        {
            if (testCases == null)
            {
                testCases = new Dictionary<string, string>();
            }
            foreach (string[] valuePair in testStatusName)
            {
                DataRow[] rows = logEntry.LogEntry.Select(
                        string.Format(TESTOUTCOMESTATEMENT, valuePair[0]));
                if (rows.Length == 0 && testResult.ContainsKey(valuePair[1]))
                {
                    Warning("[Warning] The log entry kind '{0}' should be enabled.", valuePair[0]);
                }
                else
                {
                    foreach (DataRow row in rows)
                    {
                        //get the test case name from the testfailed, testinconclusive, testpassed,
                        //testerror, testtimeout, testaborted or testunknown log entries.
                        string testCaseName = (row as TestLog.LogEntryRow).Message.Trim();
                        testCases[testCaseName] = valuePair[0];
                    }
                }
            }
        }

        /// <summary>
        /// Parses key:value string pairs by specified match string from log entries.
        /// </summary>
        /// <param name="logEntry">The log entry dataset which contains all log entries.</param>
        /// <param name="dict">The dictionary which contains the matching key:value pairs.</param>
        /// <param name="matchString">The string to match. "matchString : value" is a valid log entry message.</param>
        /// <param name="filter">The filter expression to select log messages</param>
        private static void GetStatsFromLogEntry(TestLog logEntry, ref Dictionary<string, string> dict, string matchString, string filter)
        {
            if (dict == null)
            {
                dict = new Dictionary<string, string>();
            }
            DataRow[] rows = logEntry.LogEntry.Select(filter);
            if (rows.Length == 0)
            {
                Warning("[Warning] The log entry kind 'Comment' and 'Settings' should be enabled.");
            }
            foreach (DataRow row in rows)
            {
                string tmp = (row as TestLog.LogEntryRow).Message;
                if (tmp.StartsWith(matchString))
                {
                    tmp = tmp.Remove(0, matchString.Length);
                    int index = tmp.IndexOf(":");
                    
                    if (index != -1)
                    {
                        string key = tmp.Substring(0, index);
                        string value = tmp.Substring(index + 1);
                        if (!dict.ContainsKey(key))
                        {
                            dict.Add(key, value);
                        }
                        else
                        {
                            dict[key] = value;
                        }
                        if (key == "TestsExecuted")
                        {
                            DateTime stamp = ((TestLog.LogEntryRow)row).timeStamp;
                            dict.Add("TimeStamp", stamp.ToString());
                        }
                    }
                    
                }
            }
        }

        /// <summary>
        /// Stores all checkpoint log entry messages to <c ref="checkpointEntries"></c>
        /// </summary>
        /// <param name="dt">The datatable contias all checkpoint log entries</param>
        private void FillCheckpointEntries(TestLog.LogEntryDataTable dt)
        {
            if (checkpointEntries == null)
            {
                checkpointEntries = new Dictionary<string, string>();
            }
            if (excludedRequirements == null)
            {
                excludedRequirements = new Dictionary<string, string>();
            }
            if (excludedRequirementsTimestamp == null)
            {
                excludedRequirementsTimestamp = new Dictionary<string, string>();
            }
            foreach (TestLog.LogEntryRow entry in dt)
            {
                //excluding captured requirements from failed test cases.
                if (!entry.IstestCaseNull() && !string.IsNullOrEmpty(entry.testCase))
                {
                    //requirements covered in failed test case.
                    if (testCases.ContainsKey(entry.testCase) && 
                        string.Compare(testCases[entry.testCase], TESTPASSED, true) != 0)
                    {
                        if (!excludedRequirements.ContainsKey(entry.Message))
                        {
                            excludedRequirements.Add(entry.Message, entry.testCase);
                            excludedRequirementsTimestamp.Add(
                                entry.Message, entry.timeStamp.ToString());
                        }
                        continue;
                    }
                }
                if (!checkpointEntries.ContainsKey(entry.Message))
                {
                    checkpointEntries.Add(entry.Message, entry.timeStamp.ToString());
                    // parse protocol name from message string
                    Match ms = MSPATTERN.Match(entry.Message);
                    Match rfc = RFCPATTERN.Match(entry.Message);
                    if (ms.Success || rfc.Success)
                    {
                        string protocol = entry.Message.Remove(entry.Message.IndexOf('_'));
                        if (!protocols.Contains(protocol))
                        {
                            protocols.Add(protocol);
                        }
                    }
                    else
                    {
                        //Remove the schema restriction to the reqirement id
                        //The protocols will not be parse from message string.
                        continue;
                    }
                }
            }
        }

        private static void Warning(string format, params object[] args)
        {
            string message = string.Format(format, args);
            if (ReportingLog.Log != null)
            {
                ReportingLog.Log.TraceWarning(message);
            }
            else
            {
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// The dictionary contains test case names and test outcomes.
        /// </summary>
        public Dictionary<string, string> AllTestCases
        {
            get
            {
                return testCases;
            }
        }

        /// <summary>
        /// The dictionary contains excluded requirements id and test case the req belong to.
        /// </summary>
        public Dictionary<string, string> ExcludedRequirements
        {
            get
            {
                return excludedRequirements;
            }
        }

        /// <summary>
        /// The dictionary contains excluded requirments id and timestamps.
        /// </summary>
        public Dictionary<string, string> ExcludedRequirementsTimestamp
        {
            get
            {
                return excludedRequirementsTimestamp;
            }
        }

        /// <summary>
        /// The dictionary contains the covered requirement ids and time stamp of checkpoint.
        /// </summary>
        public Dictionary<string, string> CoveredRequirements
        {
            get
            {
                if (checkpointEntries == null)
                {
                    checkpointEntries = new Dictionary<string, string>();
                }
                return checkpointEntries;
            }
        }

        /// <summary>
        /// The dictionary contains all PTF configuration values.
        /// </summary>
        public Dictionary<string, string> PTFConfigurations
        {
            get
            {
                if (ptfConfigurations == null)
                {
                    ptfConfigurations = new Dictionary<string, string>();
                }
                return ptfConfigurations;
            }
        }

        /// <summary>
        /// The dictionary contains all PTF test result values.
        /// </summary>
        public Dictionary<string, string> TestResult
        {
            get
            {
                if (testResult == null)
                {
                    testResult = new Dictionary<string, string>();
                }
                return testResult;
            }
        }

        /// <summary>
        /// A list of covered protocols
        /// </summary>
        public List<string> Protocols
        {
            get
            {
                return protocols;
            }
        }
    }
}

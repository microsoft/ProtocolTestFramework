// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Web;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.Protocols.ReportingTool
{
    /// <summary>
    /// Reporting tool error status
    /// </summary>
    public enum ReportingToolError : int
    {
        /// <summary>
        /// Status: Success
        /// </summary>
        Success = 0,

        /// <summary>
        /// Status: Failed to generate report
        /// </summary>
        GenerateReportFailed = 1,

        /// <summary>
        /// Status: Failed to create app log
        /// </summary>
        CreateAppLogFailed = 2
    }

    /// <summary>
    /// Reporting tool class
    /// </summary>
    public class Reporting
    {
        struct ExclRequirementInfo
        {
            string testCase;
            string testResult;
            string timeStamp;
            string logFile;

            public ExclRequirementInfo(
                string testCase, string testResult, string timeStamp, string logFile)
            {
                this.testCase = testCase;
                this.testResult = testResult;
                this.timeStamp = timeStamp;
                this.logFile = logFile;
            }
            public string TestCase { get { return testCase; } }
            public string TestResult { get { return testResult; } }
            public string TimeStamp { get { return timeStamp; } }
            public string LogFile { get { return logFile; } }
        }
        TableAnalyzer tableAnalyzer;
        ReportingParameters param;
        int logState;
        bool scopeMode;
        bool verboseMode;
        bool deltaMode;

        /// <summary>
        /// Constructor
        /// </summary>
        public Reporting()
        {
        }

        private static bool ArgumentMatch(string arg, string formal)
        {
            return ArgumentMatch(arg, formal, true);
        }

        /// <summary>
        /// Provides two match kinds:
        /// extra match: argu == formal.
        /// short-form match: argu is a char and equals to formal's first char.
        /// argu is striped from command line arg, without '/' or '-'
        /// </summary>
        /// <param name="arg">The command line argument which starts with '/' or '-'</param>
        /// <param name="formal">The expected argument string</param>
        /// <param name="exactMatch">true means exact match mode, false means short-form match</param>
        /// <returns>true if argument matchs, else false.</returns>
        private static bool ArgumentMatch(string arg, string formal, bool exactMatch)
        {
            if ((arg[0] == '/') || (arg[0] == '-'))
            {
                arg = arg.Substring(1);
                if (arg == formal)
                {
                    return true;
                }
                else if (!exactMatch && arg.Length == 1)
                {
                    return (arg[0] == formal[0]);
                }
            }
            return false;
        }

        /// <summary>
        /// Parse command line arguments.
        /// Required arguments:
        /// /l:&lt;logfilename&gt;
        /// /t:&lt;tablefilename&gt;
        /// 
        /// multiple filenames can be provided together, separated by space char.
        /// /log:&lt;logfilename1&gt; &lt;logfilename2&gt;
        /// Optional arguments:
        /// /o:&lt;outputfilename&gt;
        /// /?
        /// </summary>
        /// <param name="args">command line arguments</param>
        /// <returns>True if parse ok, false means print help message only.</returns>
        private bool ParseParameters(string[] args)
        {
            param = new ReportingParameters();
            bool ret = true;
           
            #region parse command line parameters
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                string candidateArg = string.Empty;
                bool isSwitch = false;
                // normalize argument
                if (arg.StartsWith("/") || arg.StartsWith("-"))
                {
                    isSwitch = true;
                    int index = arg.IndexOf(":");
                    if (index != -1)
                    {
                        candidateArg = arg.Substring(index + 1);
                        arg = arg.Substring(0, index);
                    }
                }
                arg = arg.ToLower();

                // if it is a switch argument
                if (!isSwitch)
                {
                    AddPatameter(arg);
                }
                else
                {
                    // parse argument by argumentmatch
                    ret = ParseArguments(arg, candidateArg);
                    if (!ret)
                    {
                        return false;
                    }
                }

            }
            #endregion
            param.Validate(scopeMode);
            return ret;
        }

        //really need multiple if...else logic to deal with all arguments
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private bool ParseArguments(string arg, string candidateArg)
        {
            if (ArgumentMatch(arg, "?") || ArgumentMatch(arg, "help"))
            {
                WriteHeader();
                WriteHelpMessage();
                return false;
            }
            else if (ArgumentMatch(arg, "p") || ArgumentMatch(arg, "prefix"))
            {
                if (!String.IsNullOrEmpty(candidateArg))
                    param.RSPrefix = candidateArg;
                param.Prefix = true;
            }
            else if (ArgumentMatch(arg, "l") || ArgumentMatch(arg, "log"))
            {
                if (!String.IsNullOrEmpty(candidateArg))
                {
                    //Support wild card input like "log*.xml" or "*.xml"
                    if (File.Exists(candidateArg))
                    {
                        //A complete file name
                        param.XmlLogs.Add(candidateArg);
                    }
                    else
                    {
                        //treat as a pattern
                        string[] logFileNames = GetFilesFromPattern(candidateArg);
                        if (logFileNames == null)
                        {
                            throw new InvalidOperationException("Invalid log file name pattern:" + candidateArg);
                        }
                        param.XmlLogs.AddRange(logFileNames);
                    }
                }
                param.Log = true;
            }
            else if (ArgumentMatch(arg, "t") || ArgumentMatch(arg, "table"))
            {
                if (!String.IsNullOrEmpty(candidateArg))
                {
                    param.RequirementTables.Add(candidateArg);
                }
                param.Table = true;
            }
            else if (ArgumentMatch(arg, "o") || ArgumentMatch(arg, "out"))
            {
                if (!String.IsNullOrEmpty(candidateArg))
                {
                    param.OutputFile = candidateArg;
                }
                param.Output = true;
            }
            else if (ArgumentMatch(arg, "ins") || ArgumentMatch(arg, "inscope"))
            {
                if (!string.IsNullOrEmpty(candidateArg.Trim()))
                {
                    param.InScopeString = candidateArg.Trim();
                }
                scopeMode = true;
                param.InScope = true;
            }
            else if (ArgumentMatch(arg, "oos") || ArgumentMatch(arg, "outofscope"))
            {
                if (!string.IsNullOrEmpty(candidateArg.Trim()))
                {
                    param.OutScopeString = candidateArg.Trim();
                }
                scopeMode = true;
                param.OutScope = true;
            }
            else if (ArgumentMatch(arg, "d") || ArgumentMatch(arg, "delta"))
            {
                if (!string.IsNullOrEmpty(candidateArg.Trim()))
                {
                    param.DeltaScopeString = candidateArg.Trim();
                }
                deltaMode = true;
                param.DeltaScope = true;
            }
            else if (ArgumentMatch(arg, "r") || ArgumentMatch(arg, "replace"))
            {
                if (!string.IsNullOrEmpty(candidateArg.Trim()))
                {
                    throw new InvalidOperationException("[ERROR] No argument is needed for replace switch.");
                }
                param.Replace = true;
            }
            else if (ArgumentMatch(arg, "v") || ArgumentMatch(arg, "verbose"))
            {
                if (!string.IsNullOrEmpty(candidateArg.Trim()))
                {
                    throw new InvalidOperationException("[ERROR] No argument is needed for verbose switch.");
                }
                verboseMode = true;
            }
            else
            {
                throw new InvalidOperationException("[ERROR] Invalid switch: " + arg);
            }
            return true;
        }

        private void AddPatameter(string arg)
        {
            if (param.Log)
            {
                param.XmlLogs.Add(arg);
            }
            else if (param.Prefix)
            {
                if (String.IsNullOrEmpty(param.RSPrefix))
                    param.RSPrefix = arg;
                else
                    throw new InvalidOperationException("Unexpected multiple prefix is found.");
            }
            else if (param.Table)
            {
                param.RequirementTables.Add(arg);
            }
            else if (param.Output)
            {
                param.OutputFile = arg;
            }
            else if (param.InScope)
            {
                if (param.InScopeOverrided)
                {
                    throw new InvalidOperationException("Spaces are not allowed in the scope parameter.");
                }
                else
                {
                    if (!string.IsNullOrEmpty(arg.Trim()))
                    {
                        param.InScopeString = arg.Trim();
                    }
                }
            }
            else if (param.OutScope)
            {
                if (param.OutScopeOverided)
                {
                    throw new InvalidOperationException("Spaces are not allowed in the scope parameter.");
                }
                else
                {
                    if (!string.IsNullOrEmpty(arg.Trim()))
                    {
                        param.OutScopeString = arg.Trim();
                    }
                }
            }
            else if (param.DeltaScope)
            {
                if (string.IsNullOrEmpty(param.DeltaScopeString))
                {
                    if (!string.IsNullOrEmpty(arg.Trim()))
                    {
                        param.DeltaScopeString = arg.Trim();
                    }
                }
                else
                {
                    throw new InvalidOperationException("Space is not allowed in the delta scope parameter.");
                }
            }
            else
            {
                throw new InvalidOperationException(
                    String.Format("[ERROR] Invalid argument: {0}", arg));
            }
        }

        /// <summary>
        /// Get all file names match the specific pattern
        /// </summary>
        /// <param name="pattern">file name pattern</param>
        /// <returns>Returns all file names match the pattern</returns>
        private string[] GetFilesFromPattern(string pattern)
        {
            string[] logFileNames = null;
            string fullPath = null;
            string namePattern = null;

            //filter start spaces
            string filePattern = pattern.TrimStart(new char[] { ' ' });

            //get the file log directory from pattern string
            string logFileDirectory = Path.GetDirectoryName(filePattern);
            if (string.IsNullOrEmpty(logFileDirectory))
            {
                LogTraceError(
                    string.Format(
                    "The directory of log file: {0} is not found, please provide the correct path.", pattern)
                    );
                return null;
            }

            //get the log file name pattern
            if (logFileDirectory.EndsWith("\\"))
            {
                namePattern = filePattern.Substring(logFileDirectory.Length);
            }
            else
            {
                namePattern = filePattern.Substring(logFileDirectory.Length + 1);
            }

            try
            {
                //get thr full path for file log firectory
                fullPath = Path.GetFullPath(logFileDirectory);

                //filter end spaces
                namePattern = namePattern.TrimEnd(new char[] { ' ' });

                //get all file names in the path which match the name pattern
                logFileNames = Directory.GetFiles(fullPath, namePattern, SearchOption.TopDirectoryOnly);
                if (logFileNames.Length == 0)
                {
                    return null;
                }
            }
            catch (ArgumentNullException)
            {
                LogTraceError("Log file directory does not exist.");
            }
            catch (System.Security.SecurityException)
            {
                LogTraceError("The caller does not have the " +
                    "required permission.");
            }
            catch (ArgumentException)
            {
                LogTraceError("Path is an empty string, " +
                    "contains only white spaces, " +
                    "or contains invalid characters.");
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                LogTraceError(string.Format("The path does not exist: {0}", fullPath));
            }

            return logFileNames;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031")]
        /// <summary>
        /// The entry method of Reporting class.
        /// </summary>
        /// <param name="args">Command line arguments passed to the program</param>
        /// <returns>0 if report is successfully generated, else means error occurs.</returns>
        private int Run(string[] args)
        {
            int exitCode = (int)ReportingToolError.Success;
            try
            {
                if (args.Length == 0)
                {
                    WriteHeader();
                    WriteHelpMessage();
                }
                else if (ParseParameters(args))
                {
                    // check if output file is already exists
                    if (File.Exists(param.OutputFile))
                    {
                        if (param.Replace == false)
                        {
                            LogTraceInfo(
                                string.Format("The output file '{0}' has already exists, but do not be overwrited."
                                + "Please use /r or /replace switch to overwrite the old file.",
                                param.OutputFile)
                                );
                            return (int)ReportingToolError.GenerateReportFailed;
                        }
                    }

                    // get requirement table analyzer
                    tableAnalyzer = new TableAnalyzer(
                        param.RequirementTables, param.ScopeRules, param.DeltaScopeValues, scopeMode, param.RSPrefix);

                    Generate();
                }
            }
            catch (Exception ex)
            {

                LogTraceError(ex.Message);
                exitCode = (int)ReportingToolError.GenerateReportFailed;
            }
            return exitCode;
        }

        /// <summary>
        /// Generate a html result page from given information in <c ref="param"></c>
        /// </summary>
        private void Generate()
        {
            string tempFile = string.Empty;

            try
            {
                Dictionary<string, List<string>> globalVerifiedRequirements =
                    new Dictionary<string, List<string>>();
                Dictionary<string, string> globleVerifiedRequirementsTimestamp =
                    new Dictionary<string, string>();
                Dictionary<string, string> inconsistencyErrors =
                    new Dictionary<string, string>();
                StringBuilder sb = new StringBuilder();

                Dictionary<string, ExclRequirementInfo> globalExcludedRequirements
                    = new Dictionary<string, ExclRequirementInfo>();

                // write html header
                sb.Append(Resource.HtmlTemplateHeader);

                foreach (string filename in param.XmlLogs)
                {
                    // get xml log analyzer
                    XmlLogAnalyzer logAnalyzer = new XmlLogAnalyzer(filename);
                    // get failed test cases and exclued requirements
                    foreach (KeyValuePair<string, string> kvp in logAnalyzer.ExcludedRequirements)
                    {
                        //the requirement covered in one passed case, it will never be excluded.
                        if (!logAnalyzer.CoveredRequirements.ContainsKey(kvp.Key) &&
                            !globalExcludedRequirements.ContainsKey(kvp.Key))
                        {
                            //add test case which the req belong to
                            //add the test case result
                            //add the timestamp
                            //add the log file name which contain the req
                            globalExcludedRequirements[kvp.Key] =
                                new ExclRequirementInfo(kvp.Value,
                                    logAnalyzer.AllTestCases[kvp.Value],
                                    logAnalyzer.ExcludedRequirementsTimestamp[kvp.Key],
                                    filename);
                        }
                    }

                    foreach (KeyValuePair<string, string> kvp in logAnalyzer.CoveredRequirements)
                    {
                        string rsId = tableAnalyzer.GetRequirementId(kvp.Key, false);
                        if (!string.IsNullOrEmpty(rsId))
                        {
                            //the logged ID should be 1-1 mapping to the Req_ID
                             if (tableAnalyzer.RequirementsToVerify.ContainsKey(rsId))
                            {

                                //unverified requirements were captured.
                                if (string.Compare(tableAnalyzer.RequirementVerifications[rsId], VerificationValues.UNVERIFIED, true) == 0)
                                {
                                    if (!inconsistencyErrors.ContainsKey(rsId))
                                        inconsistencyErrors.Add(rsId, tableAnalyzer.RequirementVerifications[rsId]);
                                    continue;
                                }

                                if (!globalVerifiedRequirements.ContainsKey(rsId))
                                {
                                    globalVerifiedRequirements[rsId] = new List<string>();
                                    globalVerifiedRequirements[rsId].Add(filename);
                                    if (!globleVerifiedRequirementsTimestamp.ContainsKey(rsId))
                                    {
                                        globleVerifiedRequirementsTimestamp[rsId] = kvp.Value;
                                    }
                                }
                                else
                                {
                                    if (!globalVerifiedRequirements[rsId].Contains(filename))
                                        globalVerifiedRequirements[rsId].Add(filename);
                                    else
                                        continue;
                                }
                            }
                            else
                            {
                                //non-testable, deleted, non-exist requirements were captured.
                                if (string.Compare(tableAnalyzer.RequirementVerifications[rsId], VerificationValues.ADAPTER, true) != 0 &&
                                    string.Compare(tableAnalyzer.RequirementVerifications[rsId], VerificationValues.TESTCASE, true) != 0)
                                {
                                    if (!inconsistencyErrors.ContainsKey(rsId))
                                        inconsistencyErrors.Add(rsId, tableAnalyzer.RequirementVerifications[rsId]);
                                }
                            }
                        }
                        else
                        {
                            if (!inconsistencyErrors.ContainsKey(kvp.Key))
                            {
                                inconsistencyErrors.Add(kvp.Key, VerificationValues.NONEXIST);
                            }
                        }
                    }

                    // write whole table of ptfconf/test result from log to html
                    string logOutput = GetLogStatsHTML(filename, logAnalyzer);
                    sb.Append(logOutput);

                    // output a line break to make source clear
                    sb.Append("\r\n");

                    // write a horizontal line to separate results from different logs
                    sb.Append("<hr />");
                }

                // write html footer
                sb.Append(Resource.HtmlTemplateFooter);

                sb.Replace("$GLOBALSTAT$",
                    GetGlobalStat(globalVerifiedRequirements, globleVerifiedRequirementsTimestamp));

                if (verboseMode && globalExcludedRequirements.Count > 0)
                {
                    sb.Replace("$EXCLUDEDREQUIREMENTS$",
                        GetExcludedRequirements(globalExcludedRequirements, inconsistencyErrors));
                }
                else
                {
                    sb.Replace("$EXCLUDEDREQUIREMENTS$", string.Empty);
                }

                sb.Replace("$GLOBALRSINCONSISTENCY$",
                    GetGlobalRSInconsistenies(
                    tableAnalyzer.RSValicationErrors,
                    tableAnalyzer.RSValidationWarnings));

                sb.Replace("$GLOBALINCONSISTENCY$",
                    GetGlobalInconsistencise(globalVerifiedRequirements, inconsistencyErrors));


                tempFile = Path.GetTempFileName();
                using (StreamWriter sw = new StreamWriter(tempFile, false, Encoding.UTF8))
                {
                    sw.Write(sb.ToString());
                }

                string outputDirectory = Path.GetDirectoryName(Path.GetFullPath(param.OutputFile));
                if (!Directory.Exists(outputDirectory))
                {
                    throw new InvalidOperationException(
                        string.Format("Test report output directory does not exist: {0}",
                        outputDirectory)
                        );
                }
                // move temp file to output file.
                File.Delete(param.OutputFile);
                File.Move(tempFile, param.OutputFile);
                Console.WriteLine("[NOTICE] Report file generated: \r\n" +
                    "  " + param.OutputFile);

                if (verboseMode)
                {
                    string fileName = "ReqCoveredStatus.xml";
                    string outputDir = Path.GetDirectoryName(param.OutputFile);
                    string outputPath = Path.Combine(outputDir, fileName);
                    DumpReqsCaptureStatus(globalVerifiedRequirements, inconsistencyErrors, outputPath);
                    Console.WriteLine("[NOTICE] Requirement coverage report generated: \r\n" +
                    "  " + outputPath);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                // delete temporary file
                if (!String.IsNullOrEmpty(tempFile) && File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

         private void ProcessDerivedRequirements(
            string coveredReqID,
            Dictionary<string, DerivedRequirement> derivedRequirements)
        {
            if (derivedRequirements.ContainsKey(coveredReqID))
            {
                foreach (string originalReqID in derivedRequirements[coveredReqID].OriginalReqs)
                {
                    if (derivedRequirements[originalReqID].DerivedReqs.ContainsKey(coveredReqID))
                    {
                        DerivedRequirement originalReq = derivedRequirements[originalReqID];
                        if (derivedRequirements[originalReqID].CoveredStatus != CoveredStatus.Verified)
                        {
                            if (derivedRequirements[coveredReqID].CoveredStatus == CoveredStatus.Unverified)
                            {
                                throw new InvalidOperationException("The derived requirement is not verified");
                            }
                            bool isPartialVerified =
                                derivedRequirements[coveredReqID].CoveredStatus == CoveredStatus.Partial ? true : false;
                            switch (originalReq.DerivedReqs[coveredReqID])
                            {
                                case DerivedType.Inferred:
                                    //for inferred type if derived requirement is verified, then original requirement is verified.
                                    //if derived requirement is partial verified, then original requirement is partial verified.
                                    originalReq.CoveredStatus =
                                        isPartialVerified ? CoveredStatus.Partial : CoveredStatus.Verified;
                                    break;
                                case DerivedType.Partial:
                                    //for partial type both derived requirement is verified and partial verified,
                                    //the original requirement is partial verified.
                                    originalReq.CoveredStatus = CoveredStatus.Partial;
                                    break;
                                case DerivedType.Cases:
                                    //for case type, if the last derived requirement verified, then the original verified.
                                    //else the original requirement is partial verified.
                                    if (originalReq.DerivedReqs.ContainsKey(coveredReqID))
                                    {
                                        if (originalReq.CaseCount > 1)
                                        {
                                            originalReq.CoveredStatus = CoveredStatus.Partial;
                                        }
                                        else if (originalReq.CaseCount == 1)
                                        {
                                            originalReq.CoveredStatus =
                                                isPartialVerified ? CoveredStatus.Partial : CoveredStatus.Verified;
                                        }
                                        if (!isPartialVerified)
                                        {
                                            originalReq.DerivedReqs.Remove(coveredReqID);
                                        }
                                    }
                                    break;
                            }
                        }

                        originalReq.TimeStamp = derivedRequirements[coveredReqID].TimeStamp;
                        derivedRequirements[originalReqID] = originalReq;
                        if (derivedRequirements[originalReqID].OriginalReqs.Count != 0)
                        {
                            //To deal with the nested derived cases.
                            ProcessDerivedRequirements(originalReqID, derivedRequirements);
                        }
                    }
                }
            }
        }

        private int ComputeDuplicatedVerifiedRequirements(
            IDictionary<string, DerivedRequirement> derivedRequirements,
            Dictionary<string, List<string>> globalVerifiedRequirements)
        {
            int count = 0;
            foreach (KeyValuePair<string, DerivedRequirement> kvp in derivedRequirements)
            {
                if (kvp.Value.CoveredStatus == CoveredStatus.Verified)
                {
                    if (globalVerifiedRequirements.ContainsKey(kvp.Key))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private int ComputeDuplicatedRequirements(
            IDictionary<string, DerivedRequirement> derivedRequirements,
            IDictionary<string, List<string>> requirementsToVerify)
        {
            int count = 0;
            foreach (KeyValuePair<string, DerivedRequirement> kvp in derivedRequirements)
            {
                if (requirementsToVerify.ContainsKey(kvp.Key))
                {
                    count++;
                }
            }

            return count;
        }

        private int ComputeCoveredOriginal(
            Dictionary<string, DerivedRequirement> derivedRequiremens,
            CoveredStatus coveredType)
        {
            int count = 0;
            if (derivedRequiremens != null && derivedRequiremens.Count > 0)
            {
                foreach (DerivedRequirement original in derivedRequiremens.Values)
                {
                    if (original.OriginalReqs.Count == 0 &&
                        original.CoveredStatus == coveredType &&
                        !tableAnalyzer.InformativeRequirements.Contains(original.ReqID))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private string GetExcludedRequirements(
            Dictionary<string, ExclRequirementInfo> globalExcludedRequirements,
            Dictionary<string, string> inconsistencyErrors)
        {
            StringBuilder sb = new StringBuilder(Resource.ExcludReqTable);
            StringBuilder excludedReqs = new StringBuilder();
            List<string> ltStrings = new List<string>();

            foreach (KeyValuePair<string, ExclRequirementInfo> kvp in globalExcludedRequirements)
            {
                string discription = String.Empty;
                string doc_sect = String.Empty;
                string scope = String.Empty;
                if (tableAnalyzer.RequirementsToVerify.ContainsKey(kvp.Key))
                {
                    ltStrings = tableAnalyzer.RequirementsToVerify[kvp.Key];
                    discription = ltStrings[0];
                    doc_sect = ltStrings[1];
                    scope = ltStrings[2];
                }
                else if (tableAnalyzer.RequirementsNotToVerify.ContainsKey(kvp.Key))
                {
                    ltStrings = tableAnalyzer.RequirementsNotToVerify[kvp.Key];
                    discription = ltStrings[0];
                    doc_sect = ltStrings[1];
                    scope = ltStrings[2];
                }
                else if (tableAnalyzer.RequirementsDeleted.ContainsKey(kvp.Key))
                {
                    ltStrings = tableAnalyzer.RequirementsDeleted[kvp.Key];
                    discription = ltStrings[0];
                    doc_sect = ltStrings[1];
                    scope = ltStrings[2];
                }
                else
                {
                    //the excluded requirement is not exist, should add to inconsistency table.
                    inconsistencyErrors.Add(kvp.Key, VerificationValues.NONEXIST);
                    continue;
                }

                excludedReqs.Append(@"
                        <tr class='RSExcluded'>
                            <td>
                                " + HttpUtility.HtmlEncode(kvp.Key) + @"</td>
                            <td>
                                " + HttpUtility.HtmlEncode(discription) + @"</td>
                            <td>
                                " + HttpUtility.HtmlEncode(doc_sect) + @"</td>
                            <td>
                                " + HttpUtility.HtmlEncode(scope) + @"</td>
                            <td>
                                " + HttpUtility.HtmlEncode(kvp.Value.TestCase) + @"</td>
                            <td>
                                " + HttpUtility.HtmlEncode(kvp.Value.TestResult) + @"</td>
                            <td>
                                " + HttpUtility.HtmlEncode(kvp.Value.TimeStamp) + @"</td>
                            <td>
                                " + HttpUtility.HtmlEncode(kvp.Value.LogFile) + @"</td>
                        </tr>");
            }
            sb.Replace("$ExcReqs$", excludedReqs.ToString());
            return sb.ToString();
        }

        
        private string GetGlobalStat(Dictionary<string, List<string>> globalVerifiedRequirements,
            Dictionary<string, string> globalVerifiedRequirementsTimestamp)
        {
            uint totalRequirements = tableAnalyzer.TotalCount;
            uint toVerifyRequirements = tableAnalyzer.ToVerifyCount;
            uint verifiedRequirements = (uint)globalVerifiedRequirements.Count;

            //comput derived requirement coverage
            Dictionary<string, DerivedRequirement> derivedReqs =
                new Dictionary<string, DerivedRequirement>(tableAnalyzer.DerivedRequirements);
            foreach (KeyValuePair<string, List<string>> kvp in globalVerifiedRequirements)
            {
                if (derivedReqs.ContainsKey(kvp.Key))
                {
                    //compute original requirement coverage which is covered by derived requirement.
                    DerivedRequirement derivedReq = derivedReqs[kvp.Key];
                    derivedReq.CoveredStatus = CoveredStatus.Verified;
                    derivedReq.TimeStamp = globalVerifiedRequirementsTimestamp[kvp.Key];
                    derivedReqs[kvp.Key] = derivedReq;
                    ProcessDerivedRequirements(kvp.Key, derivedReqs);
                }
            }
            uint verifiedOriginalCount =
                (uint)ComputeCoveredOriginal(derivedReqs, CoveredStatus.Verified);
            uint partialVerifiedOriginalCount =
                (uint)ComputeCoveredOriginal(derivedReqs, CoveredStatus.Partial);
            uint totalOriginalCount = tableAnalyzer.TotalOriginalCount;
            uint duplicateRequirementsCount =
                (uint)ComputeDuplicatedRequirements(derivedReqs, tableAnalyzer.RequirementsToVerify);
            uint duplicateVerifiedCount =
                (uint)ComputeDuplicatedVerifiedRequirements(derivedReqs, globalVerifiedRequirements);
            StringBuilder sb = new StringBuilder(Resource.GlobalTable);

            string tableDescriptionNonDelta = "<br/>This report is only for requirement with scope value = '{0}'";
            string tableDescriptionForDelta = tableDescriptionNonDelta + ", and delta value = '{1}'";

            //replace the table title
            if (deltaMode)
            {
                sb.Replace("$GLOBALTABLETITLE$", "Delta Pass Requirement Coverage Statistics");
                sb.Replace("$GLOBALTABLEDISCRIPTION$",
                    string.Format(tableDescriptionForDelta, param.InScopeString, param.DeltaScopeString));
            }
            else
            {
                sb.Replace("$GLOBALTABLETITLE$", "Global Requirement Coverage Statistics");
                sb.Replace("$GLOBALTABLEDISCRIPTION$",
                    string.Format(tableDescriptionNonDelta, param.InScopeString));
            }

            //display a error message due to the inconsistencies in RS.
            if (tableAnalyzer.RSValicationErrors.Count > 0)
            {
                sb.Replace("$ErrorMessage$", Resource.InconsistencyErrorMessage);
            }
            else
            {
                sb.Replace("$ErrorMessage$", string.Empty);
            }

            //calculate the aggregate coverage percents
            uint finalToVerifyCount =
                (totalOriginalCount + toVerifyRequirements - duplicateRequirementsCount);
            uint finalVerifyCount =
                verifiedOriginalCount + verifiedRequirements - duplicateVerifiedCount;
            float finalPercent =
                finalToVerifyCount == 0 ? 0 : (float)finalVerifyCount / finalToVerifyCount;
            sb.Replace("$finalToVerified$", finalToVerifyCount.ToString("D"));
            sb.Replace("$finalVerified$", finalVerifyCount.ToString("D") + " (" +
                 finalPercent.ToString("P") + ")");
            float finalPartialPercent =
                finalToVerifyCount == 0 ? 0 : (float)partialVerifiedOriginalCount / finalToVerifyCount;
            sb.Replace("$finalPVerified$", partialVerifiedOriginalCount.ToString("D") + " (" +
                 finalPartialPercent.ToString("P") + ")");
            uint finalUnverifyCount =
                finalToVerifyCount - finalVerifyCount - partialVerifiedOriginalCount;
            float finalUnverifyPercent =
                finalToVerifyCount == 0 ? 0 : (1 - finalPercent - finalPartialPercent);
            sb.Replace("$finalUVerified$", finalUnverifyCount.ToString("D") + " (" +
                 finalUnverifyPercent.ToString("P") + ")");
            uint totalVerified = finalVerifyCount + partialVerifiedOriginalCount;
            float totalVerifyPercent =
                finalToVerifyCount == 0 ? 0 : (float)totalVerified / finalToVerifyCount;
            sb.Replace("%totalChecked%", totalVerified.ToString("D") + " (" +
                 totalVerifyPercent.ToString("P") + ")");
            //calculate the derived coverage percents
            uint derivedTotal = 0;
            uint derivedVerified = 0;
            uint derivedUnverified = 0;
            foreach (KeyValuePair<string, DerivedRequirement> kvp in derivedReqs)
            {
                if (kvp.Value.OriginalReqs.Count > 0)
                {
                    derivedTotal++;
                    if (kvp.Value.CoveredStatus != CoveredStatus.Unverified)
                    {
                        derivedVerified++;
                    }
                    else
                    {
                        derivedUnverified++;
                    }
                }
            }
            sb.Replace("$derivedTotal$", derivedTotal.ToString("D"));
            float totalDerivedPercent = (derivedTotal == 0 ? 0 : (float)derivedVerified / derivedTotal);
            sb.Replace("$derivedVerified$", derivedVerified.ToString("D") + " (" +
                 totalDerivedPercent.ToString("P") + ")");
            float unverifiedDerivedPercent = (derivedTotal == 0 ? 0 : 1 - totalDerivedPercent);
            sb.Replace("$derivedUnverified$", derivedUnverified.ToString("D") + " (" +
                 unverifiedDerivedPercent.ToString("P") + ")");

            sb.Replace(@"$DETAIL$", WriteDetails(
                globalVerifiedRequirements, derivedReqs, globalVerifiedRequirementsTimestamp));

            foreach (KeyValuePair<string, DerivedRequirement> kvp in derivedReqs)
            {
                tableAnalyzer.DerivedRequirements[kvp.Key] = kvp.Value;
            }

            return sb.ToString();
        }

        private string GetGlobalRSInconsistenies(
            Dictionary<string, string> rsValidationErrors,
            Dictionary<string, string> rsValidationWarnings)
        {
            //if verbose mode, and there is no errors and warnings, will not display the table
            //if not verbose mode, and there is no errors, will not diaplay the table
            if (rsValidationErrors.Count == 0)
            {
                if (rsValidationWarnings.Count == 0 || !verboseMode)
                {
                    return string.Empty;
                }
            }
            StringBuilder rsValidationDetail = new StringBuilder(Resource.RSInconsistencyTable);
            rsValidationDetail.Replace("$RSERRORS$",
                rsValidationErrors.Count == 0 ? string.Empty : "Inconsistency Errors");
            rsValidationDetail.Replace("$RSInconsistentErrorsCount$",
                rsValidationErrors.Count == 0 ? string.Empty : rsValidationErrors.Count.ToString("D"));
            rsValidationDetail.Replace("$RSINCONSISTENCYERRORS$",
                WriteRSValidationErrorDetails(rsValidationErrors));

            //only verbose mode show the warnings
            if (verboseMode)
            {
                rsValidationDetail.Replace("$RSWARNINGS$",
                    rsValidationWarnings.Count == 0 ? string.Empty : "Inconsistency Warnings");
                rsValidationDetail.Replace("$RSInconsistentWarningsCount$",
                    rsValidationWarnings.Count == 0 ? string.Empty : rsValidationWarnings.Count.ToString("D"));
                rsValidationDetail.Replace("$RSINCONSISTENCYWARNINGS$",
                    WriteRSValidationWarningDetails(rsValidationWarnings));
            }
            else
            {
                rsValidationDetail.Replace("$RSWARNINGS$", string.Empty);
                rsValidationDetail.Replace("$RSInconsistentWarningsCount$", string.Empty);
                rsValidationDetail.Replace("$RSINCONSISTENCYWARNINGS$", string.Empty);
            }

            return rsValidationDetail.ToString();
        }

        private string GetGlobalInconsistencise(
            Dictionary<string, List<string>> globalVerifiedRequirements,
            Dictionary<string, string> inconsistencyErrors)
        {
            //requirement should be verified, but not verified in log file.
            Dictionary<string, string> inconsistencyWarnings =
                new Dictionary<string, string>();
            foreach (KeyValuePair<string, List<string>> kvp in tableAnalyzer.RequirementsToVerify)
            {
                if (!inconsistencyWarnings.ContainsKey(kvp.Key))
                {
                    if (tableAnalyzer.DerivedRequirements.ContainsKey(kvp.Key) &&
                        tableAnalyzer.DerivedRequirements[kvp.Key].OriginalReqs.Count == 0)
                    {
                        //ignore original with derivation
                        continue;
                    }
                    else
                    {
                        if (!globalVerifiedRequirements.ContainsKey(kvp.Key))
                        {
                            if (tableAnalyzer.DerivedRequirements.ContainsKey(kvp.Key) &&
                            tableAnalyzer.DerivedRequirements[kvp.Key].CoveredStatus != CoveredStatus.Unverified)
                            {
                                continue;
                            }
                            //requirement should cover, but not covered
                            if (string.Compare(tableAnalyzer.RequirementVerifications[kvp.Key], VerificationValues.UNVERIFIED, true) != 0)
                            {
                                inconsistencyWarnings.Add(
                                    kvp.Key, tableAnalyzer.RequirementVerifications[kvp.Key]);
                            }
                        }
                    }
                }
            }

            //all requirements' verification is adapter or test case will consider as need verify.
            foreach (KeyValuePair<string, List<string>> kvp in tableAnalyzer.RequirementsNotToVerify)
            {
                if (!inconsistencyWarnings.ContainsKey(kvp.Key))
                {
                    if (tableAnalyzer.DerivedRequirements.ContainsKey(kvp.Key) &&
                        tableAnalyzer.DerivedRequirements[kvp.Key].OriginalReqs.Count == 0)
                    {
                        //ignore original with derivation
                        continue;
                    }
                    else
                    {
                        if (string.Compare(kvp.Key, VerificationValues.ADAPTER, true) == 0 &&
                            string.Compare(kvp.Key, VerificationValues.TESTCASE, true) == 0 &&
                            !globalVerifiedRequirements.ContainsKey(kvp.Key))
                        {
                            //requirement should cover, but not covered
                            inconsistencyWarnings.Add(
                                kvp.Key, tableAnalyzer.RequirementVerifications[kvp.Key]);
                        }
                    }
                }
            }

            //if verbose mode, and there is no errors and warnings, will not display the table
            //if not verbose mode, and there is no errors, will not diaplay the table
            if (inconsistencyErrors.Count == 0)
            {
                if (inconsistencyWarnings.Count == 0 || !verboseMode)
                {
                    return string.Empty;
                }
            }

            StringBuilder sb = new StringBuilder(Resource.InconsistencyTable);

            sb.Replace("$InconsistentErrors$",
                        inconsistencyErrors.Count == 0 ? string.Empty : "Inconsistency Errors");
            sb.Replace("$InconsistentErrorsCount$",
                inconsistencyErrors.Count == 0 ? string.Empty : inconsistencyErrors.Count.ToString("D"));
            sb.Replace("$InconsistentErrorsDETAIL$",
                WriteInconsistencyErrorDetails(inconsistencyErrors));

            //only verbose mode show the warnings
            if (verboseMode)
            {
                sb.Replace("$InconsistentWarnings$",
                    inconsistencyWarnings.Count == 0 ? string.Empty : "Inconsistency Warnings");
                sb.Replace("$InconsistentWarningsCount$",
                    inconsistencyWarnings.Count == 0 ? string.Empty : inconsistencyWarnings.Count.ToString("D"));
                sb.Replace("$InconsistentWarningsDETAIL$",
                    WriteInconsistencyWarningDetails(inconsistencyWarnings));
            }
            else
            {
                sb.Replace("$InconsistentWarnings$", string.Empty);
                sb.Replace("$InconsistentWarningsCount$", string.Empty);
                sb.Replace("$InconsistentWarningsDETAIL$", string.Empty);
            }

            return sb.ToString();
        }

        private static string WriteRSValidationErrorDetails(
            Dictionary<string, string> rsValidationErrors)
        {
            if (rsValidationErrors.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder validationDetail = new StringBuilder();

            validationDetail.Append(@"
         <tr>
              <td class='TableCellHighlighted' colspan='3'>
                  <a name='jump-rsinconsistencyerrors'>Errors:</a>
              </td>
          </tr>
          <tr>
              <th style='width: 12%;' colspan='1'>
                  Req ID
              </th>
              <th style='width: 88%;' colspan='2'>
                  Inconsistency type 
              </th>
          </tr>");

            foreach (KeyValuePair<string, string> kvp in rsValidationErrors)
            {
                validationDetail.Append(@"
       <tr class='RSInconsistencyError'>
            <td colspan='1'>
            " + HttpUtility.HtmlEncode(kvp.Key) + @"</td>
            <td colspan='2'>
            " + HttpUtility.HtmlEncode(kvp.Value) + @"</td>
        </tr>");
            }

            return validationDetail.ToString();
        }

        private static string WriteRSValidationWarningDetails(
            Dictionary<string, string> rsValidationWarnings)
        {
            if (rsValidationWarnings.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder validationDetail = new StringBuilder();

            validationDetail.Append(@"
         <tr>
              <td class='TableCellHighlighted' colspan='3'>
                  <a name='jump-rsinconsistencywarnings'>Warnings:</a>
              </td>
          </tr>
          <tr>
              <th style='width: 12%;' colspan='1'>
                  Req ID
              </th>
              <th style='width: 88%;' colspan='2'>
                  Inconsistency type 
              </th>
          </tr>");

            foreach (KeyValuePair<string, string> kvp in rsValidationWarnings)
            {
                validationDetail.Append(@"
       <tr class='RSInconsistencyWarning'>
            <td colspan='1'>
            " + HttpUtility.HtmlEncode(kvp.Key) + @"</td>
            <td colspan='2'>
            " + HttpUtility.HtmlEncode(kvp.Value) + @"</td>
        </tr>");
            }

            return validationDetail.ToString();
        }

        private string WriteInconsistencyErrorDetails(
            Dictionary<string, string> inconsistencyErrorDetails)
        {
            List<string> ltStrings = new List<string>();
            if (inconsistencyErrorDetails.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder errorDetails = new StringBuilder();
            
            //add error table head
            errorDetails.Append(@"
        <tr>
            <td class='TableCellHighlighted' align='center' colspan='7'>
            <a name='jump-inconsistencyerrors'>Errors:</a>
            </td>
        </tr>
        <tr>
            <td style='width: 12%;'>
                Req ID
            </td>
            <td>
                Description
            </td>
            <td>
                Doc Sect
            </td>
            <td>
                Scope
            </td>
            <td style='width: 15%;'>
                Verification in RS
            </td>
            <td style='width: 20%;'>
                Direct Verification Result
            </td>
        </tr>");
            
            //write verified Non-testable requirements
            foreach (string req in GetInconsistencyByGroup(
                inconsistencyErrorDetails,
                VerificationValues.NONTESTABLE))
            {
                ltStrings = tableAnalyzer.RequirementsNotToVerify[req];
                errorDetails.Append(@"
        <tr class='InconsistencyError'>
            <td>
                " + HttpUtility.HtmlEncode(req) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(ltStrings[0]) + @"</td>
<td>
                " + HttpUtility.HtmlEncode(ltStrings[1]) + @"</td>
<td>
                " + HttpUtility.HtmlEncode(ltStrings[2]) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(VerificationValues.NONTESTABLE) + @"</td>
            <td>
                Verified </td>
        </tr>");
            }

            //write verified Unverified requirements
            foreach (string req in GetInconsistencyByGroup(
                inconsistencyErrorDetails,
                VerificationValues.UNVERIFIED))
            {
                
                //ignore informative/out-of-scope, unverified requirement.
                if (tableAnalyzer.RequirementsToVerify.ContainsKey(req))
                {
                    ltStrings = tableAnalyzer.RequirementsToVerify[req];
                    errorDetails.Append(@"
            <tr class='InconsistencyError'>
                <td>
                    " + HttpUtility.HtmlEncode(req) + @"</td>
                <td>
                    " + HttpUtility.HtmlEncode(ltStrings[0]) + @"</td>
<td>
                    " + HttpUtility.HtmlEncode(ltStrings[1]) + @"</td>
<td>
                    " + HttpUtility.HtmlEncode(ltStrings[2]) + @"</td>
                <td>
                    " + HttpUtility.HtmlEncode(VerificationValues.UNVERIFIED) + @"</td>
                <td>
                    Verified </td>
            </tr>");
                }
                else if (tableAnalyzer.RequirementsNotToVerify.ContainsKey(req))
                {
                    ltStrings = tableAnalyzer.RequirementsNotToVerify[req];
                    errorDetails.Append(@"
            <tr class='InconsistencyError'>
                <td>
                    " + HttpUtility.HtmlEncode(req) + @"</td>
                <td>
                    " + HttpUtility.HtmlEncode(ltStrings[0]) + @"</td>
<td>
                    " + HttpUtility.HtmlEncode(ltStrings[1]) + @"</td>
<td>
                    " + HttpUtility.HtmlEncode(ltStrings[2]) + @"</td>
                <td>
                    " + HttpUtility.HtmlEncode(VerificationValues.UNVERIFIED) + @"</td>
                <td>
                    Verified </td>
            </tr>");
                }
            }

            //write verified Deleted requirements
            foreach (string req in GetInconsistencyByGroup(
                inconsistencyErrorDetails,
                VerificationValues.DELETED))
            {
                ltStrings = tableAnalyzer.RequirementsDeleted[req];

                errorDetails.Append(@"
        <tr class='InconsistencyError'>
            <td>
                " + HttpUtility.HtmlEncode(req) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(ltStrings[0]) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(ltStrings[1]) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(ltStrings[2]) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(VerificationValues.DELETED) + @"</td>
            <td>
                Verified </td>
        </tr>");
            }

            //write verified Non-exist requirements
            foreach (string req in GetInconsistencyByGroup(
                inconsistencyErrorDetails, VerificationValues.NONEXIST))
            {
                errorDetails.Append(@"
        <tr class='InconsistencyError'>
            <td>
                " + HttpUtility.HtmlEncode(req) + @"</td>
            <td> -- </td>
            <td>
                " + HttpUtility.HtmlEncode(VerificationValues.NONEXIST) + @"</td>
            <td>
                Verified </td>
        </tr>");
            }

            return errorDetails.ToString();
        }

        private string WriteInconsistencyWarningDetails(
            Dictionary<string, string> inconsistencyWarningDetails)
        {
            List<string> ltStrings = new List<string>();
            List<string> ltStringsNotoVerify = new List<string>();
            if (inconsistencyWarningDetails.Count == 0)
            {
                return string.Empty;
            }
            StringBuilder warningDetails = new StringBuilder();
            //add warning table head
            warningDetails.Append(@"
        <tr>
            <td class='TableCellHighlighted' colspan='5'>
            <a name='jump-inconsistencywarnings'>Warnings:</a>
            </td>
        </tr>
        <tr>
            <td style='width: 12%;'>
                Req ID
            </td>
            <td>
                Description
            </td>
            <td style='width: 10%;'>
                Verification in RS
            </td>
            <td style='width: 13%;'>
                Direct Verification Result
            </td>
        </tr>");
            //write unverified Adapter requirements
            foreach (string req in GetInconsistencyByGroup(
                inconsistencyWarningDetails,
                VerificationValues.ADAPTER))
            {
                ltStrings = tableAnalyzer.RequirementsToVerify[req];
                ltStringsNotoVerify = tableAnalyzer.RequirementsNotToVerify[req];
                warningDetails.Append(@"
        <tr class='InconsistencyWarning'>
            <td>
                " + HttpUtility.HtmlEncode(req) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(tableAnalyzer.RequirementsToVerify.ContainsKey(req) ?
                  ltStrings[0] :
                  ltStringsNotoVerify[0]) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(VerificationValues.ADAPTER) + @"</td>
            <td>
                Unverified </td>
        </tr>");
            }

            //write unverified Test Case requirements
            foreach (string req in GetInconsistencyByGroup(
                inconsistencyWarningDetails,
                VerificationValues.TESTCASE))
            {
                ltStrings = tableAnalyzer.RequirementsToVerify[req];
                ltStringsNotoVerify = tableAnalyzer.RequirementsNotToVerify[req];
                warningDetails.Append(@"
        <tr class='InconsistencyWarning'>
            <td>
                " + HttpUtility.HtmlEncode(req) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(tableAnalyzer.RequirementsToVerify.ContainsKey(req) ?
                  ltStrings[0] :
                  ltStringsNotoVerify[0]) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(VerificationValues.TESTCASE) + @"</td>
            <td>
                Unverified </td>
        </tr>");
            }

            return warningDetails.ToString();
        }

        private static IList<string> GetInconsistencyByGroup(
            Dictionary<string, string> inconsistencyRequirements, string groupName)
        {
            List<string> groupedRequirements = new List<string>();
            foreach (KeyValuePair<string, string> kvp in inconsistencyRequirements)
            {
                if (string.Compare(kvp.Value, groupName, true) == 0)
                {
                    groupedRequirements.Add(kvp.Key);
                }
            }
            return groupedRequirements;
        }

        private string WriteDetails(
            Dictionary<string, List<string>> globalVerifiedRequirements,
            Dictionary<string, DerivedRequirement> derviedReqs,
            Dictionary<string, string> globalVerifiedRequirementsTimestamp)
        {
            StringBuilder details = new StringBuilder();
            details.Append(WriteFinalVerifiedDetails(
                globalVerifiedRequirements, derviedReqs, globalVerifiedRequirementsTimestamp));
            details.Append(WriteDerivedVerifiedDetails(globalVerifiedRequirements, derviedReqs));
            return details.ToString();
        }

        private string WriteDerivedVerifiedDetails(
           Dictionary<string, List<string>> globalVerifiedRequirements,
           Dictionary<string, DerivedRequirement> derviedReqs)
        {
            List<string> ltStrings = new List<string>();
            StringBuilder details = new StringBuilder();
            // add derived requirements
            foreach (KeyValuePair<string, DerivedRequirement> kvp in derviedReqs)
            {
                if (kvp.Value.OriginalReqs.Count > 0)
                {
                    string discription = String.Empty;
                    string doc_Sect = String.Empty;
                    string scope = String.Empty;
                    if (tableAnalyzer.RequirementsToVerify.ContainsKey(kvp.Key))
                    {
                        ltStrings = tableAnalyzer.RequirementsToVerify[kvp.Key];
                        discription = ltStrings[0];
                        doc_Sect = ltStrings[1];
                        scope = ltStrings[2];
                    }
                    else
                    {
                        if (tableAnalyzer.RequirementsNotToVerify.ContainsKey(kvp.Key))
                        {
                            ltStrings = tableAnalyzer.RequirementsNotToVerify[kvp.Key];
                            discription = ltStrings[0];
                            doc_Sect = ltStrings[1];
                            scope = ltStrings[2];
                        }

                    }

                    StringBuilder logFiles = null;
                    if (globalVerifiedRequirements.ContainsKey(kvp.Key))
                    {
                        logFiles = new StringBuilder();
                        foreach (string log in globalVerifiedRequirements[kvp.Key])
                        {
                            if (logFiles.Length == 0)
                            {
                                logFiles.Append(Path.GetFileName(log));
                            }
                            else
                            {
                                logFiles.Append(";" + Path.GetFileName(log));
                            }
                        }
                    }
                    string displayType = string.Empty;
                    string timeStamp = string.Empty;
                    string status = string.Empty;
                    switch (kvp.Value.CoveredStatus)
                    {
                        case CoveredStatus.Verified:
                            displayType = "DerivedVerified";
                            timeStamp = kvp.Value.TimeStamp;
                            status = "Complete Verified";
                            break;
                        case CoveredStatus.Partial:
                            displayType = "DerivedPartialVerify";
                            timeStamp = kvp.Value.TimeStamp;
                            status = "Partial Verified";
                            break;
                        case CoveredStatus.Unverified:
                            displayType = "DerivedUnverified";
                            timeStamp = "--";
                            status = "Unverified";
                            break;
                    }
                    details.Append(@"
        <tr class='" + displayType + @"'>
            <td>
                " + HttpUtility.HtmlEncode(kvp.Key) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(discription) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(doc_Sect) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(scope) + @"</td>
            <td>
                " + status + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(timeStamp) + @"</td>
            <td>
                " + (logFiles != null ? logFiles.ToString() : "--") + @"</td>
        </tr>");
                }
            }
            return details.ToString();
        }


       
        private string WriteFinalVerifiedDetails(
            Dictionary<string, List<string>> globalVerifiedRequirements,
            Dictionary<string, DerivedRequirement> derviedReqs,
            Dictionary<string, string> globalVerifiedRequirementsTimestamp)
        {
            List<string> ltStrings = new List<string>();
            StringBuilder details = new StringBuilder();
            //add final requirement details
            foreach (KeyValuePair<string, DerivedRequirement> kvp in derviedReqs)
            {
                if (tableAnalyzer.InformativeRequirements.Contains(kvp.Key))
                {
                    continue;
                }
                if (kvp.Value.OriginalReqs.Count == 0)
                {
                    string discription;
                    string doc_Sect;
                    string scope;
                    if (tableAnalyzer.RequirementsToVerify.ContainsKey(kvp.Key))
                    {

                        ltStrings = tableAnalyzer.RequirementsToVerify[kvp.Key];
                        discription = ltStrings[0];
                        doc_Sect = ltStrings[1];
                        scope = ltStrings[2];
                    }
                    else
                    {
                        ltStrings = tableAnalyzer.RequirementsNotToVerify[kvp.Key];
                        discription = ltStrings[0];
                        doc_Sect = ltStrings[1];
                        scope = ltStrings[2];
                    }

                    string displayType = string.Empty;
                    string status = string.Empty;
                    switch (kvp.Value.CoveredStatus)
                    {
                        case CoveredStatus.Verified:
                            displayType = "FinalVerified";
                            status = "Verified";
                            break;
                        case CoveredStatus.Partial:
                            displayType = "FinalPartialVerify";
                            status = "Partial Verified";
                            break;
                        case CoveredStatus.Unverified:
                            displayType = "FinalUnverified";
                            status = "Unverified";
                            break;
                    }
                    details.Append(@"
        <tr class='" + displayType + @"'>
            <td>
                " + HttpUtility.HtmlEncode(kvp.Key) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(discription) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(doc_Sect) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(scope) + @"</td>
            <td>
                " + status + @"</td>
            <td>
                --</td>
            <td>
                --</td>
        </tr>");
                }
            }

            foreach (KeyValuePair<string, List<string>> kvp in tableAnalyzer.RequirementsToVerify)
            {
                if (!derviedReqs.ContainsKey(kvp.Key))
                {
                    if (globalVerifiedRequirements.ContainsKey(kvp.Key))
                    {
                        StringBuilder logFiles = new StringBuilder();
                        foreach (string log in globalVerifiedRequirements[kvp.Key])
                        {
                            if (logFiles.Length == 0)
                            {
                                logFiles.Append(Path.GetFileName(log));
                            }
                            else
                            {
                                logFiles.Append(";" + Path.GetFileName(log));
                            }
                        }
                        ltStrings = tableAnalyzer.RequirementsToVerify[kvp.Key];

                        details.Append(@"
        <tr class='FinalVerified'>
            <td>
                " + HttpUtility.HtmlEncode(kvp.Key) + @"</td>
           
<td>
                " + HttpUtility.HtmlEncode(ltStrings[0]) + @"
            </td>
<td>
                " + HttpUtility.HtmlEncode(ltStrings[1]) + @"
            </td>
<td>
                " + HttpUtility.HtmlEncode(ltStrings[2]) + @"
            </td>
            <td>Verified</td>
            <td>
                " + HttpUtility.HtmlEncode(globalVerifiedRequirementsTimestamp[kvp.Key]) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(logFiles.ToString()) + @"</td>
        </tr>");
                    }
                    else
                    {
                        ltStrings = tableAnalyzer.RequirementsToVerify[kvp.Key];
                        details.Append(@"
        <tr class='FinalUnverified'>
            <td>
                " + HttpUtility.HtmlEncode(kvp.Key) + @"</td>
            
<td>
                " + HttpUtility.HtmlEncode(ltStrings[0]) + @"
            </td>
<td>
                " + HttpUtility.HtmlEncode(ltStrings[1]) + @"
            </td>
<td>
                " + HttpUtility.HtmlEncode(ltStrings[2]) + @"
            </td>
            <td>Unverified</td>
            <td>
                --</td>
            <td>
                -- </td>
        </tr>");
                    }
                }
            }

            return details.ToString();
        }

        /// <summary>
        /// Get a pre-formatted html string which contains PTF conf and test result stats from specified log file.
        /// </summary>
        /// <param name="filename">file name of log file</param>
        /// <param name="logAnalyzer">Test log file analyzer</param>
        /// <returns>a pre-formatted html contains PTF conf and test result stats.</returns>
        private string GetLogStatsHTML(string filename, XmlLogAnalyzer logAnalyzer)
        {
            StringBuilder logOutput = new StringBuilder(Resource.LogTable);
            StringBuilder protocols = new StringBuilder();
            uint verifiedRequirements = 0;
            foreach (string protocol in logAnalyzer.Protocols)
            {
                protocols.Append(protocol + ';');
            }
            logOutput.Replace(@"$PROTOCOL$", HttpUtility.HtmlEncode(protocols.ToString()));
            logOutput.Replace(@"$FILENAME$", HttpUtility.HtmlEncode(Path.GetFileName(filename)));

            // get all PTF confs from log file
            StringBuilder config = new StringBuilder();
            foreach (KeyValuePair<string, string> ptfconf in logAnalyzer.PTFConfigurations)
            {
                config.Append(@"
        <tr>
            <td>
                " + HttpUtility.HtmlEncode(ptfconf.Key) + @"</td>
            <td>
                " + HttpUtility.HtmlEncode(ptfconf.Value) + @"</td>
        </tr>");
            }
            logOutput.Replace(@"$CONFIG$", config.ToString());


            // start output test result from log file
            StringBuilder testresult = new StringBuilder();
            uint total = 0;
            if (logAnalyzer.TestResult.ContainsKey("TestsExecuted"))
            {
                total = uint.Parse(logAnalyzer.TestResult["TestsExecuted"]);
            }
            foreach (KeyValuePair<string, string> result in logAnalyzer.TestResult)
            {
                if (result.Key == "TimeStamp")
                {
                    testresult.Append(@"
            <tr>
                <td>
                    " + HttpUtility.HtmlEncode(result.Key) + @"</td>
                <td>
                    " + HttpUtility.HtmlEncode(result.Value) + @"</td>
            </tr>");
                    continue;
                }
                uint actual = uint.Parse(result.Value);
                float percent = (total == 0) ? 0 : actual / (float)total;
                testresult.Append(@"
        <tr>
            <td>
                " + HttpUtility.HtmlEncode(result.Key) + @"</td>
            <td>
                " + actual + " (" + percent.ToString("P") + @")</td>
        </tr>");
            }
            logOutput.Replace(@"$TESTRESULT$", testresult.ToString());

            foreach (KeyValuePair<string, string> kvp in logAnalyzer.CoveredRequirements)
            {
                string req_id = tableAnalyzer.GetRequirementId(kvp.Key, false);
                //the requirement id in log file should always the full req id.
                if (!string.IsNullOrEmpty(req_id))
                {

                    if (tableAnalyzer.RequirementsToVerify.ContainsKey(req_id))
                    {
                        verifiedRequirements++;
                    }
                    else if (tableAnalyzer.RequirementsNotToVerify.ContainsKey(req_id))
                    {
                        LogTraceWarning("The test log file covers an unverifiable requirement : " + kvp.Key);
                    }
                    else
                    {
                        LogTraceWarning("The test log file covers a deleted requirement : " + kvp.Key);
                    }
                }
                else
                {
                    LogTraceWarning("The test log file covers a non-existed requirement :" + kvp.Key);
                }
            }
            logOutput.Replace(@"$Verified$", verifiedRequirements.ToString("D"));
            return logOutput.ToString();
        }
        private static void WriteHeader()
        {
            Console.WriteLine("Microsoft (R) Protocol Test Framework reporting utility");
            Console.WriteLine("[Microsoft (R) .NET Framework, Version 2.0.50727.42]");
        }
        private static void WriteHelpMessage()
        {
            Console.WriteLine(String.Format(Resource.HelpText,
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Name));
        }

        private void DumpReqsCaptureStatus(
            Dictionary<string, List<string>> globalVerifiedRequirements,
            Dictionary<string, string> inconsistencyErrors,
            string outputPath)
        {
            RequirementCollection reqCollection = null;
            XmlSerializer serializer = new XmlSerializer(typeof(RequirementCollection));
            foreach (string requirementTable in param.RequirementTables)
            {
                using (XmlTextReader xReader = new XmlTextReader(requirementTable) { DtdProcessing = DtdProcessing.Prohibit })
                {
                    if (reqCollection == null)
                    {
                        reqCollection = (RequirementCollection)serializer.Deserialize(xReader);
                    }
                    else
                    {
                        RequirementCollection tempTable = (RequirementCollection)serializer.Deserialize(xReader);
                        reqCollection.AddRequirements(tempTable.Requirements);
                    }
                    xReader.Close();
                }
            }

            //update requirement capture status.
            foreach (SerializableRequirement req in reqCollection.Requirements)
            {
                if (globalVerifiedRequirements.ContainsKey(req.REQ_ID) ||
                    inconsistencyErrors.ContainsKey(req.REQ_ID))
                {
                    req.CoveredStatus = IsCoveredType.Covered;
                }
            }

            //sort all requirements
            reqCollection.Sort();

            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            //write result to log file.
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.ConformanceLevel = ConformanceLevel.Auto;
            using (XmlTextWriter xWriter = new XmlTextWriter(outputPath, Encoding.UTF8))
            {
                xWriter.Formatting = Formatting.Indented;
                serializer.Serialize(xWriter, reqCollection);
                xWriter.Flush();
                xWriter.Close();
            }
        }

        #region Log Helper
        private bool MakeSureLogCreated()
        {
            if (ReportingLog.Log == null)
            {
                this.logState = (int)ReportingToolError.GenerateReportFailed;
                return false;
            }
            else
            {
                this.logState = (int)ReportingToolError.Success;
            }
            return true;
        }

        private void LogDebugInfo(string message)
        {
            if (MakeSureLogCreated())
            {
                ReportingLog.Log.DebugLog(message);
            }
        }

        private void LogTraceInfo(string message)
        {
            if (MakeSureLogCreated())
            {
                ReportingLog.Log.TraceInformation(message);
            }
        }

        private void LogTraceWarning(string message)
        {
            if (MakeSureLogCreated())
            {
                ReportingLog.Log.TraceWarning(message);
            }
        }

        private void LogTraceError(string message)
        {
            if (MakeSureLogCreated())
            {
                ReportingLog.Log.TraceError(message);
            }
        }
        #endregion
        static int Main(string[] args)
        {
            int exitCode = (int)ReportingToolError.Success;
            Reporting report = new Reporting();
            exitCode = report.Run(args);

            if (exitCode == 0)
            {
                exitCode = report.logState;
            }

            return exitCode;
        }
    }

    internal class ReportingLog
    {
        //Output file stream name
        private const string logFileName = "ReportingToolLog.log";

        //Output file stream
        private TextWriterTraceListener textListener;

        //Output console stream
        private ConsoleTraceListener consoleListener;

        //Log message format
        private const string timeStampFormat = "{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}.{6:D3}";

        static private ReportingLog log;

        public static ReportingLog Log
        {
            get
            {
                if (log == null)
                {
                    log = new ReportingLog();
                }
                return log;
            }
        }

        /// <summary>
        /// Consitructor for ReportingLog
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private ReportingLog()
        {
            try
            {
                if (File.Exists(logFileName))
                {
                    File.Delete(logFileName);
                }
                //Initialize trace listener
                FileStream logFileStream = new FileStream(logFileName,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite);

                if (logFileStream != null)
                {
                    textListener = new TextWriterTraceListener(logFileStream);
                    consoleListener = new ConsoleTraceListener();
                    Trace.Listeners.Clear();
                    Trace.Listeners.Add(textListener);
                    Trace.Listeners.Add(consoleListener);
                    Trace.AutoFlush = true;
                    Debug.Listeners.Clear();
                    Debug.Listeners.Add(textListener);
                    Debug.Listeners.Add(consoleListener);
                    Debug.AutoFlush = true;
                }
            }
            catch (Exception)
            {
                //We shouldn't catch general exception, but failure on reporting 
                //log should not prevent the tool from executing.
            }
        }

        private static string GetLogMessage(string message)
        {
            //format the message
            DateTime timeStamp = DateTime.Now;
            string timeStampInfo = string.Format(timeStampFormat,
                                                timeStamp.Year,
                                                timeStamp.Month,
                                                timeStamp.Day,
                                                timeStamp.Hour,
                                                timeStamp.Minute,
                                                timeStamp.Second,
                                                timeStamp.Millisecond);
            string logMessage = string.Format("[Reporting Tool Internal Trace Log][{0}] {1}",
                timeStampInfo,
                message);
            return logMessage;
        }

        /// <summary>
        /// Write a message to debug listeners
        /// </summary>
        /// <param name="message">The message to write to the log file</param>
        /// Failure on reporting log should not prevent the reporting tool from executing
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal void DebugLog(string message)
        {
            //Write the message into debug listeners
            try
            {
                if (this.textListener != null && this.consoleListener != null)
                    Debug.WriteLine(message);
            }
            catch (Exception)
            {
                //We shouldn't catch general exception, but application on internal 
                //log should not prevent the reporting tool from executing.
            }
        }

        /// <summary>
        /// Write a message to Trace listeners
        /// </summary>
        /// <param name="message">The message to write to the log file</param>
        /// Failure on reporting log should not prevent the reporting tool from executing
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal void TraceInformation(string message)
        {
            string logMessage = GetLogMessage(message);
            //Write the message into trace listeners
            try
            {
                if (this.textListener != null && this.consoleListener != null)
                    Trace.TraceInformation(logMessage);
            }
            catch (Exception)
            {
                //We shouldn't catch general exception, but application on internal 
                //log should not prevent the reporting tool from executing.
            }
        }

        /// <summary>
        /// Write a warning message to Trace listeners
        /// </summary>
        /// <param name="message">The message to write to the log file</param>
        /// Failure on reporting log should not prevent the reporting tool from executing
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal void TraceWarning(string message)
        {
            string logMessage = GetLogMessage(message);
            //Write the message into trace listeners
            try
            {
                if (this.textListener != null && this.consoleListener != null)
                    Trace.TraceWarning("[Warning]" + logMessage);
            }
            catch (Exception)
            {
                //We shouldn't catch general exception, but application on internal 
                //log should not prevent the reporting tool from executing.
            }
        }

        /// <summary>
        /// Write a warning message to Trace listeners
        /// </summary>
        /// <param name="message">The message to write to the log file</param>
        /// Failure on reporting log should not prevent the reporting tool from executing
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal void TraceError(string message)
        {
            string logMessage = GetLogMessage(message);
            //Write the message into trace listeners
            try
            {
                if (this.textListener != null && this.consoleListener != null)
                    Trace.TraceError("[Error]" + logMessage);
            }
            catch (Exception)
            {
                //We shouldn't catch general exception, but application on internal 
                //log should not prevent the reporting tool from executing.
            }
        }
    }
}

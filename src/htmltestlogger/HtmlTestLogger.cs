// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Microsoft.Protocols.TestTools
{
    [ExtensionUri("logger://HtmlTestLogger")]
    [FriendlyName("Html")]
    public class HtmlTestLogger : ITestLoggerWithParameters
    {
        private string reportFolderPath; //The path to the report folder
        private string txtResultFolderPath; //The path to the result folder which stores all the txt files
        private string htmlResultFolderPath; //The path to the result folder which stores all the html files
        private string jsFolderPath; //The path to the result folder which stores all the js files
        private string captureFolderPath; //The path to the result folder which stores all the capture files

        private DateTimeOffset testRunStartTime = DateTimeOffset.MaxValue.ToLocalTime(); //The start time of the test run
        private DateTimeOffset testRunEndTime = DateTimeOffset.MinValue.ToLocalTime(); //The end time of the test run
        private TxtToJSON txtToJSON = new TxtToJSON();

        private const string scriptNode = "<script language=\"javascript\" type=\"text/javascript\">";
        private const string jsFileName_Functions = "functions.js";
        private const string jsFileName_Jquery = "jquery-1.11.0.min.js";
        private const string indexHtmlName = "index.html";
        private const string txtResultFolderName = "Txt";
        private const string htmlResultFolderName = "Html";
        private const string jsFolderName = "js";
        private const string captureFolderName = "Captures";

        private const string outputFolderKey = "OutputFolder";

        private Dictionary<string, string> parametersDictionary;
        private string testResultsDirPath;

        /// <summary>
        /// Initializes the Test Logger.
        /// </summary>
        /// <param name="events">Events which can be registered for.</param>
        /// <param name="testResultsDirPath">Test Results Directory</param>
        public void Initialize(TestLoggerEvents events, string testResultsDirPath)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (string.IsNullOrEmpty(testResultsDirPath))
            {
                throw new ArgumentNullException(nameof(testResultsDirPath));
            }

            // Register for the events.
            events.TestRunMessage += TestMessageHandler;
            events.TestResult += TestResultHandler;
            events.TestRunComplete += TestRunCompleteHandler;

            this.testResultsDirPath = testResultsDirPath;
            CreateReportFolder();
        }

        /// <summary>
        /// Initializes the Test Logger.
        /// </summary>
        /// <param name="events">Events which can be registered for.</param>
        /// <param name="parameters">Collection of parameters</param>
        public void Initialize(TestLoggerEvents events, Dictionary<string, string> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (parameters.Count == 0)
            {
                throw new ArgumentException("No default parameters added", nameof(parameters));
            }

            this.parametersDictionary = parameters;
            this.Initialize(events, this.parametersDictionary[DefaultLoggerParameterNames.TestRunDirectory]);
        }

        #region Implement three events
        /// <summary>
        /// Called when a test message is received.
        /// </summary>
        private void TestMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            switch (e.Level)
            {
                case TestMessageLevel.Informational:
                    break;

                case TestMessageLevel.Warning:
                    break;

                case TestMessageLevel.Error:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Called when a test result is received.
        /// </summary>
        private void TestResultHandler(object sender, TestResultEventArgs e)
        {
            if (e.Result.Outcome.ToString() == "NotFound")
            {
                return;
            }
            string caseName = !string.IsNullOrEmpty(e.Result.DisplayName) ? e.Result.DisplayName : e.Result.TestCase.FullyQualifiedName;
            int dot = caseName.LastIndexOf('.');

            if (-1 != dot)
                caseName = caseName.Substring(dot + 1);

            string txtFileName = Path.Combine(txtResultFolderPath, e.Result.StartTime.ToLocalTime().ToString("yyyy-MM-dd-HH-mm-ss") + "_"
                + (e.Result.Outcome == TestOutcome.Skipped ? "Inconclusive" : e.Result.Outcome.ToString()) + "_" + caseName + ".txt");
            StringBuilder sb = new StringBuilder();

            if (DateTimeOffset.Compare(testRunStartTime, e.Result.StartTime.ToLocalTime()) > 0)
                testRunStartTime = e.Result.StartTime.ToLocalTime();
            if (DateTimeOffset.Compare(testRunEndTime, e.Result.EndTime.ToLocalTime()) < 0)
                testRunEndTime = e.Result.EndTime.ToLocalTime();
            try
            {
                sb.AppendLine(caseName);
                sb.AppendLine("Start Time: " + e.Result.StartTime.ToLocalTime().ToString("MM/dd/yyyy HH:mm:ss"));
                sb.AppendLine("End Time: " + e.Result.EndTime.ToLocalTime().ToString("MM/dd/yyyy HH:mm:ss"));
                sb.AppendLine("Result: " + (e.Result.Outcome == TestOutcome.Skipped ? "Inconclusive" : e.Result.Outcome.ToString()));
                sb.AppendLine(e.Result.TestCase.Source);
                if (!String.IsNullOrEmpty(e.Result.ErrorStackTrace))
                {
                    sb.AppendLine("===========ErrorStackTrace===========");
                    sb.AppendLine(e.Result.ErrorStackTrace);
                }
                if (!String.IsNullOrEmpty(e.Result.ErrorMessage))
                {
                    sb.AppendLine("===========ErrorMessage==============");
                    sb.AppendLine(e.Result.ErrorMessage);
                }

                foreach (TestResultMessage m in e.Result.Messages)
                {
                    if (m.Category == TestResultMessage.StandardOutCategory && !String.IsNullOrEmpty(m.Text))
                    {
                        sb.AppendLine("===========StandardOut===============");
                        sb.AppendLine(m.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Exception: " + ex.Message);
            }
            finally
            {
                // Generate txt log file
                File.WriteAllText(txtFileName, sb.ToString());

                // Generate html log file
                string htmlFileName = Path.Combine(htmlResultFolderPath, caseName + ".html");
                File.WriteAllText(htmlFileName, ConstructCaseHtml(txtFileName, caseName));
            }
        }

        /// <summary>
        /// Called when a test run is completed.
        /// </summary>
        private void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            // Insert the necessary info used in index.html and copy it to report folder.
            File.WriteAllText(Path.Combine(reportFolderPath, indexHtmlName), ConstructIndexHtml(e));
        }

        #endregion

        /// <summary>
        /// Creates the report folders
        /// </summary>
        private void CreateReportFolder()
        {
            if (this.parametersDictionary != null)
            {
                var isoutputFolderParameterExists = this.parametersDictionary.TryGetValue(outputFolderKey, out string outputFolderValue);
                if (isoutputFolderParameterExists && !string.IsNullOrWhiteSpace(outputFolderValue))
                {
                    reportFolderPath = Path.Combine(testResultsDirPath, outputFolderValue);
                }
                else
                {
                    reportFolderPath = Path.Combine(testResultsDirPath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
                }
            }
            else
            {
                reportFolderPath = Path.Combine(testResultsDirPath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
            }

            Directory.CreateDirectory(reportFolderPath);

            txtResultFolderPath = Path.Combine(reportFolderPath, txtResultFolderName);
            Directory.CreateDirectory(txtResultFolderPath);

            htmlResultFolderPath = Path.Combine(reportFolderPath, htmlResultFolderName);
            Directory.CreateDirectory(htmlResultFolderPath);

            jsFolderPath = Path.Combine(reportFolderPath, jsFolderName);
            Directory.CreateDirectory(jsFolderPath);

            captureFolderPath = Path.Combine(reportFolderPath, captureFolderName);
            Directory.CreateDirectory(captureFolderPath);

            // Copy the two .js files to report folder, the two files don't need to be changed.
            File.WriteAllText(Path.Combine(jsFolderPath, jsFileName_Functions), Properties.Resources.functions);
        }

        /// <summary>
        /// Constructs detailObj used in each [caseName].html
        /// </summary>
        private string ConstructDetailObj(string txtFileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.Append("var detailObj=");
            sb.Append(txtToJSON.ConstructCaseDetail(txtFileName, captureFolderPath));
            sb.AppendLine(";");

            return sb.ToString();
        }

        /// <summary>
        /// Constructs listObj used in functions.js
        /// </summary>
        private string ConstructListAndSummaryObj(TestRunCompleteEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("var listObj = " + txtToJSON.TestCasesString(txtResultFolderPath, captureFolderPath) + ";");

            // Clean the temp file
            File.Delete(txtToJSON.CaseCategoryFile);
            return sb.ToString();
        }

        /// <summary>
        /// Inserts the corresponding script to the template html and generates the [testcase].html 
        /// </summary>
        private string ConstructCaseHtml(string txtFileName, string caseName)
        {
            // Insert script to the template html (testcase.html)
            StringBuilder sb = new StringBuilder();
            sb.Append(ConstructDetailObj(txtFileName));
            sb.AppendLine("var titleObj = document.getElementById(\"right_sidebar_case_title\");");
            sb.Append(string.Format("CreateText(titleObj, \"{0}\");", caseName));

            return InsertScriptToTemplate(Properties.Resources.testcase, sb.ToString());
        }

        /// <summary>
        /// Inserts the corresponding script to the template html and generates the index.html 
        /// </summary>
        private string ConstructIndexHtml(TestRunCompleteEventArgs e)
        {
            return InsertScriptToTemplate(Properties.Resources.index, ConstructListAndSummaryObj(e));
        }

        /// <summary>
        /// Inserts scripts to the template html files and returns the updated content
        /// </summary>
        private string InsertScriptToTemplate(string templateHtml, string scriptToInsert)
        {
            int posInsert = templateHtml.IndexOf(scriptNode);
            return templateHtml.Insert(posInsert + scriptNode.Length, scriptToInsert);       
        }
    }
}

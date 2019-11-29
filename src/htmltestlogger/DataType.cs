// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Represents all data type used in TxtToJSON.cs
    /// </summary>
    public class DataType
    {
        /// <summary>
        /// Represents summary for all test cases
        /// </summary>
        public class TestCasesSummary
        {
            /// <summary>
            /// Test cases information
            /// </summary>
            public List<TestCase> TestCases;

            /// <summary>
            /// Test cases categories
            /// </summary>
            public List<string> TestCasesCategories;

            /// <summary>
            /// Test cases classes
            /// </summary>
            public List<string> TestCasesClasses;
        }

        /// <summary>
        /// Represents a test case
        /// </summary>
        public class TestCase
        {
            /// <summary>
            /// The name of the test case
            /// </summary>
            public string Name;

            /// <summary>
            /// The result of test case
            /// </summary>
            public string Result;

            /// <summary>
            /// The test class of the test case
            /// </summary>
            public string ClassType;

            /// <summary>
            /// The categories of the test case
            /// </summary>
            public List<string> Category;
        }

        /// <summary>
        /// Represents a detailed StandardOut log
        /// </summary>
        public class StandardOutDetail
        {
            /// <summary>
            /// The type of the StandardOut log
            /// </summary>
            public string Type;

            /// <summary>
            /// The content of the StandardOut log
            /// </summary>
            public string Content;
        }

        /// <summary>
        /// Represents detailed test case information
        /// </summary>
        public class TestCaseDetail
        {
            /// <summary>
            /// The name of the test case
            /// </summary>
            public string Name;

            /// <summary>
            /// The start time of the test case
            /// </summary>
            public DateTimeOffset StartTime;

            /// <summary>
            /// The end time of the test case
            /// </summary>
            public DateTimeOffset EndTime;

            /// <summary>
            /// The result of the test case
            /// </summary>
            public string Result;

            /// <summary>
            /// The source assembly of the test case
            /// </summary>
            public string Source;

            /// <summary>
            /// The test class of the test case
            /// </summary>
            public string ClassType;

            /// <summary>
            /// The Categories of the test case
            /// </summary>
            public List<string> Categories;

            /// <summary>
            /// The ErrorStackTrace log of the test case
            /// </summary>
            public List<string> ErrorStackTrace;

            /// <summary>
            /// The ErrorMessage log of the test case
            /// </summary>
            public List<string> ErrorMessage;

            /// <summary>
            /// The StandardOut log of the test case
            /// </summary>
            public List<StandardOutDetail> StandardOut;

            /// <summary>
            /// The Types in StandardOut log 
            /// </summary>
            public List<string> StandardOutTypes;

            /// <summary>
            /// The path of the capture file if any
            /// </summary>
            public string CapturePath;

            /// <summary>
            /// Set default value
            /// </summary>
            /// <param name="name">Test case name</param>
            /// <param name="startTime">Start time of the test case</param>
            /// <param name="endTime">End time of the test case</param>
            /// <param name="result">Result of the test case</param>
            /// <param name="source">Assembly of the test case</param>
            /// <param name="classType">Class of the test case</param>
            /// <param name="categories">Categories of the test case</param>
            public TestCaseDetail(string name, DateTimeOffset startTime, DateTimeOffset endTime, string result, string source, string classType, List<string> categories)
            {
                this.Name = name;
                this.StartTime = startTime;
                this.EndTime = endTime;
                this.Result = result;
                this.Source = source;
                this.ClassType = classType;
                this.Categories = categories;
                this.ErrorStackTrace = new List<string>();
                this.ErrorMessage = new List<string>();
                this.StandardOut = new List<StandardOutDetail>();
                this.StandardOutTypes = new List<string>();
                this.CapturePath = null;
            }
        }

        /// <summary>
        /// Represents the summary for all test cases
        /// </summary>
        public class RunSummary
        {
            /// <summary>
            /// The number of total test cases
            /// </summary>
            public long TotalCount;

            /// <summary>
            /// The number of passed test cases
            /// </summary>
            public long PassedCount;

            /// <summary>
            /// The number of failed test cases
            /// </summary>
            public long FailedCount;

            /// <summary>
            /// The number of inconclusive test cases
            /// </summary>
            public long InconclusiveCount;

            /// <summary>
            /// The pass rate of this run
            /// </summary>
            public float PassRate;

            /// <summary>
            /// The start time of this run
            /// </summary>
            public string StartTime;

            /// <summary>
            /// The end time to of this run
            /// </summary>
            public string EndTime;

            /// <summary>
            /// The duration of this run
            /// </summary>
            public string Duration;
        }
    }
}

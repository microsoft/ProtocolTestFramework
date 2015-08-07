// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.IO;
using Microsoft.Protocols.TestTools;
using Microsoft.Protocols.TestTools.Logging;
using Microsoft.Protocols.TestTools.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Protocols.TestTools.Test.TestLogging
{
    /// <summary>
    /// Defines a custom log sink.
    /// </summary>
    public class MySink : TextSink
    {
        StreamWriter sw;
        public MySink(string name)
            : base(name)
        {
            sw = new StreamWriter(String.Format("..\\..\\[TestLogging_MySinkLog]{0} MySinkLog.txt", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff")));
        }

        protected override TextWriter Writer
        {
            get { return sw; }
        }
    }


    /// <summary>
    /// Test cases to test PTF logging
    /// The cases do not verify if the generated log is expected. It should be done manually.
    /// </summary>
    [TestClass]
    public class TestLogging : TestClassBase
    {

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            TestClassBase.Initialize(testContext, "TestLogging");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            TestClassBase.Cleanup();
        }

        [TestCategory("TestLogging")]
        [TestMethod]
        public void AddGroupLog()
        {
            BaseTestSite.Log.Add(LogEntryKind.BeginGroup, "BeginGroup message");
            BaseTestSite.Log.Add(LogEntryKind.EndGroup, "EndGroup message");
        }

        [TestCategory("TestLogging")]
        [TestMethod]
        public void AddCheckpointLog()
        {
            BaseTestSite.Log.Add(LogEntryKind.Checkpoint, "Checkpoint message");
        }

        [TestMethod]
        [TestCategory("TestLogging")]
        public void AddTestStepLog()
        {
            BaseTestSite.Log.Add(LogEntryKind.TestStep, "TestStep message");
        }

        [TestMethod]
        [TestCategory("TestLogging")]
        public void AddCommentLog()
        {
            BaseTestSite.Log.Add(LogEntryKind.Comment, "Comment message");
        }

        [TestMethod]
        [TestCategory("TestLogging")]
        public void AddWarningLog()
        {
            BaseTestSite.Log.Add(LogEntryKind.Warning, "Warning message");
        }

        [TestMethod]
        [TestCategory("TestLogging")]
        public void AddDebugLog()
        {
            BaseTestSite.Log.Add(LogEntryKind.Debug, "Debug message");
        }

        [TestMethod]
        [TestCategory("TestLogging")]
        public void AddMethodLog()
        {
            BaseTestSite.Log.Add(LogEntryKind.EnterMethod, "EnterMethod message");
            BaseTestSite.Log.Add(LogEntryKind.ExitMethod, "ExitMethod message");
        }

        [TestMethod]
        [TestCategory("TestLogging")]
        public void AddAdapterLog()
        {
            BaseTestSite.Log.Add(LogEntryKind.EnterAdapter, "EnterAdapter message");
            BaseTestSite.Log.Add(LogEntryKind.ExitAdapter, "ExitAdapter message");
        }
    }
}

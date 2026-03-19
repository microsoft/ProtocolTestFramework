// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Protocols.TestTools.Logging;
using Microsoft.Protocols.TestTools.UnitTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Protocols.TestTools.UnitTest.TestLogging
{
    /// <summary>
    /// Tests that concurrent calls to CreateLogProfileFromConfig produce unique log filenames.
    /// RED: These tests fail before the threading fixes are applied.
    /// GREEN: Pass once LogProfileParser, TestSiteProvider, and TestClassBase are fixed.
    /// </summary>
    [TestClass]
    public class TestLogFilenameCollision
    {
        private TempLogDirectory _tempDir;

        [TestInitialize]
        public void Setup()
        {
            _tempDir = new TempLogDirectory();
            // Reset GuidFactory to default before each test
            LogProfileParser.GuidFactory = () => Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        [TestCleanup]
        public void Cleanup()
        {
            LogProfileParser.GuidFactory = () => Guid.NewGuid().ToString("N").Substring(0, 8);
            _tempDir?.Dispose();
        }

        // Extracts the full file path from the first file sink in a LogProfile
        private static string GetSinkFilePath(LogProfile profile)
        {
            foreach (var sink in profile.AllSinks)
            {
                if (sink is XmlTextSink xml) return xml.FilePath;
                if (sink is PlainTextSink txt) return txt.FilePath;
            }
            throw new InvalidOperationException("No file sink found in profile.");
        }

        // Disposes all file sinks in a profile (closes the file handles)
        private static void DisposeSinks(LogProfile profile)
        {
            if (profile == null) return;
            foreach (var sink in profile.AllSinks)
                sink.Dispose();
        }

        [TestMethod]
        [TestCategory("ThreadSafety")]
        public void PatchEnabled_ConcurrentCalls_ProduceDifferentFilenames()
        {
            // Arrange: two configs pointing at the same directory and same base filename
            var config1 = FakeConfigurationData.WithPatch(_tempDir.Path, "Results.xml");
            var config2 = FakeConfigurationData.WithPatch(_tempDir.Path, "Results.xml");

            LogProfile profileA = null, profileB = null;
            Exception exA = null, exB = null;
            var barrier = new Barrier(2);

            var t1 = new Thread(() =>
            {
                try { barrier.SignalAndWait(); profileA = LogProfileParser.CreateLogProfileFromConfig(config1, "AssemblyA", out _); }
                catch (Exception ex) { exA = ex; }
            });
            var t2 = new Thread(() =>
            {
                try { barrier.SignalAndWait(); profileB = LogProfileParser.CreateLogProfileFromConfig(config2, "AssemblyB", out _); }
                catch (Exception ex) { exB = ex; }
            });

            t1.Start(); t2.Start(); t1.Join(); t2.Join();

            // Assert: no exceptions and filenames are distinct
            Assert.IsNull(exA, $"Thread A threw: {exA}");
            Assert.IsNull(exB, $"Thread B threw: {exB}");

            string fileA = Path.GetFileName(GetSinkFilePath(profileA));
            string fileB = Path.GetFileName(GetSinkFilePath(profileB));
            Assert.AreNotEqual(fileA, fileB, "Concurrent calls with patch enabled must produce different filenames.");

            DisposeSinks(profileA);
            DisposeSinks(profileB);
        }

        [TestMethod]
        [TestCategory("ThreadSafety")]
        public void PatchEnabled_NThreads_AllFilenamesUnique()
        {
            // Arrange
            const int n = 8;
            var configs = Enumerable.Range(0, n)
                .Select(_ => FakeConfigurationData.WithPatch(_tempDir.Path, "Results.xml"))
                .ToArray();

            var profiles = new LogProfile[n];
            var exceptions = new Exception[n];
            var barrier = new Barrier(n);

            var threads = Enumerable.Range(0, n).Select(i => new Thread(() =>
            {
                try { barrier.SignalAndWait(); profiles[i] = LogProfileParser.CreateLogProfileFromConfig(configs[i], $"Assembly{i}", out _); }
                catch (Exception ex) { exceptions[i] = ex; }
            })).ToArray();

            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();

            // Assert: no exceptions
            for (int i = 0; i < n; i++)
                Assert.IsNull(exceptions[i], $"Thread {i} threw: {exceptions[i]}");

            // Assert: all filenames are distinct
            var filenames = profiles.Select(p => Path.GetFileName(GetSinkFilePath(p))).ToList();
            Assert.AreEqual(n, filenames.Distinct().Count(),
                $"Expected {n} unique filenames but got duplicates: {string.Join(", ", filenames)}");

            foreach (var p in profiles) DisposeSinks(p);
        }

        [TestMethod]
        [TestCategory("ThreadSafety")]
        public void PatchDisabled_SingleCall_UsesRawFilename()
        {
            // Verify backward compatibility: patch off → filename is exactly the configured value
            var config = FakeConfigurationData.WithoutPatch(_tempDir.Path, "Results.xml");

            var profile = LogProfileParser.CreateLogProfileFromConfig(config, "AssemblyA", out _);

            string filename = Path.GetFileName(GetSinkFilePath(profile));
            Assert.AreEqual("Results.xml", filename,
                "With patch disabled the filename must exactly match the configured sink file.");

            DisposeSinks(profile);
        }

        [TestMethod]
        [TestCategory("ThreadSafety")]
        public void PatchDisabled_ConcurrentCalls_SameFilename_ThrowsIOException()
        {
            // This test documents the pre-fix behavior: patch disabled + concurrent calls = IOException.
            // After the TestSiteProvider lock fix this scenario is prevented at a higher level,
            // but at the LogProfileParser level patch-disabled concurrent calls to the same file still throw.
            var config1 = FakeConfigurationData.WithoutPatch(_tempDir.Path, "Results.xml");
            var config2 = FakeConfigurationData.WithoutPatch(_tempDir.Path, "Results.xml");

            LogProfile profileA = null, profileB = null;
            Exception exA = null, exB = null;
            var barrier = new Barrier(2);

            var t1 = new Thread(() =>
            {
                try { barrier.SignalAndWait(); profileA = LogProfileParser.CreateLogProfileFromConfig(config1, "AssemblyA", out _); }
                catch (Exception ex) { exA = ex; }
            });
            var t2 = new Thread(() =>
            {
                try { barrier.SignalAndWait(); profileB = LogProfileParser.CreateLogProfileFromConfig(config2, "AssemblyB", out _); }
                catch (Exception ex) { exB = ex; }
            });

            t1.Start(); t2.Start(); t1.Join(); t2.Join();

            // Exactly one of the two threads must fail with IOException (file already open)
            bool ioExceptionOccurred = (exA is IOException) || (exB is IOException);
            Assert.IsTrue(ioExceptionOccurred,
                "Two concurrent patch-disabled calls to the same filename must cause an IOException on the second opener.");

            DisposeSinks(profileA);
            DisposeSinks(profileB);
        }
    }
}

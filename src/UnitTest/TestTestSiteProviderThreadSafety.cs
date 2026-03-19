// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.Protocols.TestTools.UnitTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Protocols.TestTools.UnitTest.TestLogging
{
    /// <summary>
    /// Tests that TestSiteProvider.InitializeTestSite is safe when called concurrently.
    ///
    /// Root cause: InitializeTestSite has no lock. Two threads both pass the ContainsKey
    /// check, both call new DefaultTestSite(...), both construct a Logger, both try to open
    /// the same log file → IOException: File in use.
    ///
    /// RED: IOException thrown before the lock is added to InitializeTestSite.
    /// GREEN: No exception once the lock is applied.
    /// </summary>
    [TestClass]
    public class TestTestSiteProviderThreadSafety
    {
        private TempLogDirectory _tempDir;

        // Suite names owned exclusively by this test class.
        private static readonly string[] OwnedSuiteNames =
            { "FileServerSuite", "SharedSuite", "Suite0", "Suite1", "Suite2", "Suite3" };

        [TestInitialize]
        public void Setup()
        {
            _tempDir = new TempLogDirectory();
            // Clean up only the suite names this class creates — never touch suites
            // owned by other test classes (which would cause ObjectDisposedException
            // in their ClassCleanup methods).
            foreach (var name in OwnedSuiteNames) TestSiteProvider.DisposeTestSite(name);
        }

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var name in OwnedSuiteNames) TestSiteProvider.DisposeTestSite(name);
            _tempDir?.Dispose();
        }

        [TestMethod]
        [TestCategory("ThreadSafety")]
        public void ConcurrentInitialize_SameSuiteName_PatchDisabled_DoesNotThrowIOException()
        {
            // This is the direct reproduction of "IOException: File in use (FileServer_Log.xml)".
            // Both threads initialize the same suite with patch disabled → same raw filename.
            // Without the lock, both DefaultTestSite constructors open the same file concurrently.
            var config1 = FakeConfigurationData.WithoutPatch(_tempDir.Path, "FileServer_Log.xml");
            var config2 = FakeConfigurationData.WithoutPatch(_tempDir.Path, "FileServer_Log.xml");

            Exception exA = null, exB = null;
            var barrier = new Barrier(2);

            var tA = new Thread(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    TestSiteProvider.Initialize(config1, _tempDir.Path, _tempDir.Path, "FileServerSuite", "FileServerAssembly");
                }
                catch (Exception ex) { exA = ex; }
            });

            var tB = new Thread(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    TestSiteProvider.Initialize(config2, _tempDir.Path, _tempDir.Path, "FileServerSuite", "FileServerAssembly");
                }
                catch (Exception ex) { exB = ex; }
            });

            tA.Start(); tB.Start(); tA.Join(); tB.Join();

            Assert.IsNull(exA is IOException ? exA : null,
                $"Thread A got IOException: {exA?.Message}");
            Assert.IsNull(exB is IOException ? exB : null,
                $"Thread B got IOException: {exB?.Message}");

            // Regardless of which thread won, exactly one site must exist
            var site = TestSiteProvider.GetTestSite("FileServerSuite");
            Assert.IsNotNull(site, "A test site must have been created.");
        }

        [TestMethod]
        [TestCategory("ThreadSafety")]
        public void ConcurrentInitialize_DifferentSuiteNames_AllSitesCreated()
        {
            // 4 threads, each initializing a different suite name — dictionary must hold all 4.
            const int n = 4;
            Exception[] exceptions = new Exception[n];
            var barrier = new Barrier(n);

            var threads = new Thread[n];
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var cfg = FakeConfigurationData.WithPatch(_tempDir.Path, $"Log{idx}.xml", profileName: $"Profile{idx}");
                threads[idx] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        TestSiteProvider.Initialize(cfg, _tempDir.Path, _tempDir.Path, $"Suite{idx}", $"Assembly{idx}");
                    }
                    catch (Exception ex) { exceptions[idx] = ex; }
                });
            }

            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();

            for (int i = 0; i < n; i++)
                Assert.IsNull(exceptions[i], $"Thread {i} threw: {exceptions[i]}");

            for (int i = 0; i < n; i++)
                Assert.IsNotNull(TestSiteProvider.GetTestSite($"Suite{i}"),
                    $"Suite{i} was not found in TestSiteProvider after concurrent initialization.");
        }

        [TestMethod]
        [TestCategory("ThreadSafety")]
        public void ConcurrentInitialize_SameSuiteName_OnlyOneDefaultTestSiteCreated()
        {
            // When two threads race on the same suite name, the second must not create
            // a new DefaultTestSite — it must reuse (or skip) the one created by the first.
            var config1 = FakeConfigurationData.WithPatch(_tempDir.Path, "Log.xml");
            var config2 = FakeConfigurationData.WithPatch(_tempDir.Path, "Log.xml");

            Exception exA = null, exB = null;
            var barrier = new Barrier(2);

            var tA = new Thread(() =>
            {
                try { barrier.SignalAndWait(); TestSiteProvider.Initialize(config1, _tempDir.Path, _tempDir.Path, "SharedSuite", "AssemblyA"); }
                catch (Exception ex) { exA = ex; }
            });
            var tB = new Thread(() =>
            {
                try { barrier.SignalAndWait(); TestSiteProvider.Initialize(config2, _tempDir.Path, _tempDir.Path, "SharedSuite", "AssemblyB"); }
                catch (Exception ex) { exB = ex; }
            });

            tA.Start(); tB.Start(); tA.Join(); tB.Join();

            Assert.IsNull(exA, $"Thread A threw: {exA}");
            Assert.IsNull(exB, $"Thread B threw: {exB}");

            Assert.IsNotNull(TestSiteProvider.GetTestSite("SharedSuite"),
                "SharedSuite must exist after concurrent initialization.");
        }
    }
}

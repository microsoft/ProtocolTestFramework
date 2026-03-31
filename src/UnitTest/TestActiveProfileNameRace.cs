// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Microsoft.Protocols.TestTools.Logging;
using Microsoft.Protocols.TestTools.UnitTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Protocols.TestTools.UnitTest.TestLogging
{
    /// <summary>
    /// Tests that concurrent calls to CreateLogProfileFromConfig each return the correct
    /// profile name for their own config, without being overwritten by a racing thread.
    ///
    /// Root cause: activeProfileName is a private static string. The Logger constructor
    /// calls CreateLogProfileFromConfig (which sets the static) and then immediately reads
    /// ActiveProfileNameInConfig. A racing thread can overwrite the static between these
    /// two lines, causing the wrong profile to be assigned.
    ///
    /// RED: Flaky/failing before the static field is replaced with an out parameter.
    /// GREEN: Always passes once CreateLogProfileFromConfig returns the name directly.
    /// </summary>
    [TestClass]
    public class TestActiveProfileNameRace
    {
        private TempLogDirectory _tempDirA;
        private TempLogDirectory _tempDirB;

        [TestInitialize]
        public void Setup()
        {
            _tempDirA = new TempLogDirectory();
            _tempDirB = new TempLogDirectory();
            // Reset GuidFactory default
            LogProfileParser.GuidFactory = () => Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        [TestCleanup]
        public void Cleanup()
        {
            LogProfileParser.GuidFactory = () => Guid.NewGuid().ToString("N").Substring(0, 8);
            _tempDirA?.Dispose();
            _tempDirB?.Dispose();
        }

        [TestMethod]
        [TestCategory("ThreadSafety")]
        public void ConcurrentCalls_EachCallGetsItsOwnProfileName()
        {
            // Arrange: two configs with different DefaultProfile names and different directories
            var configA = FakeConfigurationData.WithPatch(_tempDirA.Path, "LogA.xml", profileName: "ProfileA");
            var configB = FakeConfigurationData.WithPatch(_tempDirB.Path, "LogB.xml", profileName: "ProfileB");

            string capturedA = null;
            string capturedB = null;
            Exception exA = null, exB = null;
            LogProfile profileA = null, profileB = null;

            // Use a second barrier INSIDE GuidFactory to force the race window open:
            // Both threads will be inside CreateLogProfileFromConfig simultaneously.
            // Thread A sets activeProfileName="ProfileA", then waits here.
            // Thread B sets activeProfileName="ProfileB", then waits here.
            // Both proceed. Thread A then reads ActiveProfileNameInConfig and gets "ProfileB" (wrong).
            var interleaveBarrier = new Barrier(2);
            LogProfileParser.GuidFactory = () =>
            {
                interleaveBarrier.SignalAndWait(TimeSpan.FromSeconds(5));
                return Guid.NewGuid().ToString("N").Substring(0, 8);
            };

            var startBarrier = new Barrier(2);

            var tA = new Thread(() =>
            {
                try
                {
                    startBarrier.SignalAndWait();
                    profileA = LogProfileParser.CreateLogProfileFromConfig(configA, "AssemblyA", out string nameA);
                    capturedA = nameA;
                }
                catch (Exception ex) { exA = ex; }
            });

            var tB = new Thread(() =>
            {
                try
                {
                    startBarrier.SignalAndWait();
                    profileB = LogProfileParser.CreateLogProfileFromConfig(configB, "AssemblyB", out string nameB);
                    capturedB = nameB;
                }
                catch (Exception ex) { exB = ex; }
            });

            tA.Start(); tB.Start(); tA.Join(); tB.Join();

            Assert.IsNull(exA, $"Thread A threw: {exA}");
            Assert.IsNull(exB, $"Thread B threw: {exB}");

            // Each thread must read back its own profile name, not the other thread's
            Assert.AreEqual("ProfileA", capturedA,
                "Thread A read the wrong profile name — activeProfileName static field race condition.");
            Assert.AreEqual("ProfileB", capturedB,
                "Thread B read the wrong profile name — activeProfileName static field race condition.");

            // Close file handles so TempLogDirectory.Dispose() can delete the directories.
            foreach (var sink in profileA?.AllSinks ?? System.Linq.Enumerable.Empty<LogSink>()) sink.Dispose();
            foreach (var sink in profileB?.AllSinks ?? System.Linq.Enumerable.Empty<LogSink>()) sink.Dispose();
        }
    }
}

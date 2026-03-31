// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Protocols.TestTools.Logging;
using Microsoft.Protocols.TestTools.UnitTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Protocols.TestTools.UnitTest.TestLogging
{
    /// <summary>
    /// Tests the fallback filename path in CreateUniqueFileName.
    ///
    /// When the GUID-prefixed filename already exists on disk, CreateUniqueFileName falls back to:
    ///   "[assembly_sink]timestamp file.ext"
    /// This fallback currently DROPS the GUID, so two threads hitting the fallback at the same
    /// millisecond produce identical names.
    ///
    /// RED: Fallback omits GUID — concurrent fallback calls can still collide.
    /// GREEN: Fallback includes a secondary GUID after the fix.
    /// </summary>
    [TestClass]
    public class TestFallbackFilenameUniqueness
    {
        private TempLogDirectory _tempDir;

        [TestInitialize]
        public void Setup()
        {
            _tempDir = new TempLogDirectory();
            LogProfileParser.GuidFactory = () => Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        [TestCleanup]
        public void Cleanup()
        {
            LogProfileParser.GuidFactory = () => Guid.NewGuid().ToString("N").Substring(0, 8);
            _tempDir?.Dispose();
        }

        private static string GetSinkFilePath(LogProfile profile)
        {
            foreach (var sink in profile.AllSinks)
            {
                if (sink is XmlTextSink xml) return xml.FilePath;
                if (sink is PlainTextSink txt) return txt.FilePath;
            }
            throw new InvalidOperationException("No file sink in profile.");
        }

        private static void DisposeSinks(LogProfile profile)
        {
            if (profile == null) return;
            foreach (var sink in profile.AllSinks) sink.Dispose();
        }

        [TestMethod]
        [TestCategory("ThreadSafety")]
        public void PatchEnabled_GuidFilePreExists_FallbackNameIsUnique()
        {
            // Arrange: inject a fixed GUID so we can predict and pre-create the primary filename
            const string fixedGuid = "aabbccdd";
            LogProfileParser.GuidFactory = () => fixedGuid;

            var config = FakeConfigurationData.WithPatch(_tempDir.Path, "Results.xml");

            // Compute what the primary name will be (timestamp will match since we call right away)
            // Instead of computing the exact timestamp, just call once to get the primary name,
            // dispose its handle, then call again expecting the fallback.
            var firstProfile = LogProfileParser.CreateLogProfileFromConfig(config, "TestAsm", out _);
            string primaryPath = GetSinkFilePath(firstProfile);
            DisposeSinks(firstProfile);
            // File now exists on disk (created by the first call). Next call must use fallback.

            var secondProfile = LogProfileParser.CreateLogProfileFromConfig(config, "TestAsm", out _);
            string fallbackPath = GetSinkFilePath(secondProfile);
            DisposeSinks(secondProfile);

            Assert.AreNotEqual(primaryPath, fallbackPath,
                "Fallback filename must differ from the primary filename.");

            // The fallback must still contain a GUID-like segment so concurrent fallbacks don't collide
            string fallbackFilename = Path.GetFileName(fallbackPath);
            bool containsHexSegment = Regex.IsMatch(fallbackFilename, @"[0-9a-f]{8}");
            Assert.IsTrue(containsHexSegment,
                $"Fallback filename '{fallbackFilename}' must contain a GUID segment to prevent concurrent collisions.");
        }

        [TestMethod]
        [TestCategory("ThreadSafety")]
        public void PatchEnabled_TwoConcurrentCallsBothInFallbackPath_ProduceDifferentFilenames()
        {
            // Arrange: pre-create the primary file so both threads immediately hit the fallback
            const string fixedGuid = "11223344";
            LogProfileParser.GuidFactory = () => fixedGuid;

            var config = FakeConfigurationData.WithPatch(_tempDir.Path, "Results.xml");

            // Create the primary file so both threads fall into the fallback branch
            var primeProfile = LogProfileParser.CreateLogProfileFromConfig(config, "TestAsm", out _);
            DisposeSinks(primeProfile);

            // Now use a real GUID for the two concurrent fallback calls so they differ
            LogProfileParser.GuidFactory = () => Guid.NewGuid().ToString("N").Substring(0, 8);

            LogProfile profileA = null, profileB = null;
            Exception exA = null, exB = null;
            var barrier = new Barrier(2);

            var tA = new Thread(() =>
            {
                try { barrier.SignalAndWait(); profileA = LogProfileParser.CreateLogProfileFromConfig(config, "TestAsm", out _); }
                catch (Exception ex) { exA = ex; }
            });
            var tB = new Thread(() =>
            {
                try { barrier.SignalAndWait(); profileB = LogProfileParser.CreateLogProfileFromConfig(config, "TestAsm", out _); }
                catch (Exception ex) { exB = ex; }
            });

            tA.Start(); tB.Start(); tA.Join(); tB.Join();

            Assert.IsNull(exA, $"Thread A threw: {exA}");
            Assert.IsNull(exB, $"Thread B threw: {exB}");

            string fileA = Path.GetFileName(GetSinkFilePath(profileA));
            string fileB = Path.GetFileName(GetSinkFilePath(profileB));

            Assert.AreNotEqual(fileA, fileB,
                $"Two concurrent fallback calls produced the same filename: '{fileA}'");

            DisposeSinks(profileA);
            DisposeSinks(profileB);
        }

        [TestMethod]
        [TestCategory("ThreadSafety")]
        public void PatchEnabled_FilenameMatchesExpectedPattern()
        {
            // Characterization: verify the patch-enabled primary name format
            //   {timestamp}_{assembly}_{guid8}_{configuredFile}
            var config = FakeConfigurationData.WithPatch(_tempDir.Path, "Results.xml");
            var profile = LogProfileParser.CreateLogProfileFromConfig(config, "MyAssembly", out _);

            string filename = Path.GetFileName(GetSinkFilePath(profile));
            // Pattern: yyyy-MM-dd HH_mm_ss_fff_Assembly_xxxxxxxx_Results.xml
            var pattern = new Regex(@"^\d{4}-\d{2}-\d{2} \d{2}_\d{2}_\d{2}_\d{3}_MyAssembly_[0-9a-f]{8}_Results\.xml$");
            Assert.IsTrue(pattern.IsMatch(filename),
                $"Filename '{filename}' does not match expected pattern.");

            DisposeSinks(profile);
        }
    }
}

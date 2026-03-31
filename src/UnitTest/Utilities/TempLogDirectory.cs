// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Protocols.TestTools.UnitTest.Utilities
{
    /// <summary>
    /// Creates a unique temporary directory for log files and deletes it on dispose.
    /// </summary>
    internal sealed class TempLogDirectory : IDisposable
    {
        public string Path { get; }

        public TempLogDirectory()
        {
            Path = Directory.CreateTempSubdirectory("PTFLogTest_").FullName;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Protocols.TestTools.Checking
{
    /// <summary>
    /// The default assertion checker which is compatible with VSTS.
    /// </summary>
    public class DefaultAssertChecker : DefaultChecker<AssertFailedException, AssertInconclusiveException>
    {
        /// <summary>
        /// Constructs a new instance of DefaultAssertChecker.
        /// </summary>
        /// <param name="testSite">The test site which the checker is hosted on.</param>
        /// <param name="checkerConfig">The configuration to checker.</param>
        public DefaultAssertChecker(ITestSite testSite, ICheckerConfig checkerConfig)
            : base(testSite, "Assert", LogEntryKind.CheckFailed, LogEntryKind.CheckSucceeded, LogEntryKind.CheckInconclusive, checkerConfig)
        {
        }

        /// <summary>
        /// Creates a failure exception for <see cref="DefaultAssertChecker"/>.
        /// </summary>
        /// <param name="message">A message that describes the exception.</param>
        /// <returns>
        /// An instance of AssertFailedException.
        /// </returns>
        protected override AssertFailedException CreateFailException(string message)
        {
            return new AssertFailedException(message);
        }

        /// <summary>
        /// Creates an inconclusive exception for <see cref="DefaultAssertChecker"/>.
        /// </summary>
        /// <param name="message">A message that describes the exception.</param>
        /// <returns>
        /// An instance of AssertInconclusiveException.
        /// </returns>
        protected override AssertInconclusiveException CreateInconclusiveException(string message)
        {
            return new AssertInconclusiveException(message);
        }
    }

    /// <summary>
    /// The default assumption checker which is compatible with VSTS.
    /// </summary>
    public class DefaultAssumeChecker : DefaultChecker<AssertInconclusiveException, AssertInconclusiveException>
    {
        /// <summary>
        /// Constructs a new instance of DefaultAssumeChecker.
        /// </summary>
        /// <param name="testSite">The test site which the checker is hosted on.</param>
        /// <param name="checkerConfig">The configuration to checker.</param>
        public DefaultAssumeChecker(ITestSite testSite, ICheckerConfig checkerConfig)
            : base(testSite, "Assume", LogEntryKind.CheckFailed, LogEntryKind.CheckSucceeded, LogEntryKind.CheckInconclusive, checkerConfig)
        {
        }

        /// <summary>
        /// Creates a failure exception for <see cref="DefaultAssumeChecker"/>.
        /// </summary>
        /// <param name="message">A message that describes the exception.</param>
        /// <returns>
        /// An instance of AssertInconclusiveException.
        /// </returns>
        protected override AssertInconclusiveException CreateFailException(string message)
        {
            return new AssertInconclusiveException(message);
        }

        /// <summary>
        /// Creates an inconclusive exception for <see cref="DefaultAssumeChecker"/>.
        /// </summary>
        /// <param name="message">A message that describes the exception.</param>
        /// <returns>
        /// An instance of AssertInconclusiveException.
        /// </returns>
        protected override AssertInconclusiveException CreateInconclusiveException(string message)
        {
            return new AssertInconclusiveException(message);
        }
    }

    /// <summary>
    /// The default debug checker which is compatible with VSTS.
    /// </summary>
    public class DefaultDebugChecker : DefaultChecker<TestDebugException, TestDebugException>
    {
        /// <summary>
        /// Constructs a new instance of DefaultDebugChecker.
        /// </summary>
        /// <param name="testSite">The test site which the checker is hosted on.</param>
        /// <param name="checkerConfig">The configuration to checker.</param>
        public DefaultDebugChecker(ITestSite testSite, ICheckerConfig checkerConfig)
            : base(testSite, "Debug", LogEntryKind.Debug, LogEntryKind.Debug, LogEntryKind.Debug, checkerConfig)
        {
        }

        /// <summary>
        /// Creates a failure exception for <see cref="DefaultDebugChecker"/>.
        /// </summary>
        /// <param name="message">A message that describes the exception.</param>
        /// <returns>
        /// An instance of <see cref="TestDebugException"/>.
        /// </returns>
        protected override TestDebugException CreateFailException(string message)
        {
            return new TestDebugException(message);
        }

        /// <summary>
        /// Creates an inconclusive exception for <see cref="DefaultDebugChecker"/>.
        /// </summary>
        /// <param name="message">A message that describes the exception.</param>
        /// <returns>
        /// An instance of <see cref="TestDebugException"/>.
        /// </returns>
        protected override TestDebugException CreateInconclusiveException(string message)
        {
            return new TestDebugException(message);
        }
    }
}

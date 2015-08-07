// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// An interface which provides methods for protocol tests' initialization and cleaning up. 
    /// </summary>
    public interface IProtocolTestNotify
    {
        /// <summary>
        /// Should be called before each test method runs.
        /// </summary>
        /// <param name="testClass">Instance of the test class</param>
        /// <param name="testName">Test case name</param>
        /// <param name="testOutcome">Test outcome</param>
        /// <param name="exceptionHandler">Handler provided to process the assert exception</param>
        void OnTestStarted(object testClass, string testName, PtfTestOutcome testOutcome, AssertExceptionHandler exceptionHandler);

        /// <summary>
        /// Should be called after each test method runs.
        /// </summary>
        /// <param name="testClass">Instance of the test class</param>
        /// <param name="testName">Test case name</param>
        /// <param name="testOutcome">Test outcome</param>
        /// <param name="exceptionHandler">Handler provided to process the assert exception</param>
        void OnTestFinished(object testClass, string testName, PtfTestOutcome testOutcome, AssertExceptionHandler exceptionHandler);
    }

    /// <summary>
    /// A delegate which is used by <see xref="IProtocolTestNotify.OnTestStarted"/> and <see xref="IProtocolTestNotify.OnTestFinished"/>
    /// </summary>
    /// <param name="exception">Exception need to be handled</param>
    /// <returns>PtfTestOutcome corresponding to the assert exception</returns>
    public delegate PtfTestOutcome AssertExceptionHandler(Exception exception);
}

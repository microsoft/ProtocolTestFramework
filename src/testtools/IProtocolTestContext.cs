// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Test outcome.
    /// </summary>
    public enum PtfTestOutcome
    {
        /// <summary>
        /// Test case failed
        /// </summary>
        Failed = 0, 

        /// <summary>
        /// Test case status is inconclusive
        /// </summary>
        Inconclusive,  

        /// <summary>
        /// Test case passed
        /// </summary>
        Passed,

        /// <summary>
        /// Test case is in progress
        /// </summary>
        InProgress,

        /// <summary>
        /// Test case encounters an error
        /// </summary>
        Error, 

        /// <summary>
        /// Test case is time out
        /// </summary>
        Timeout,

        /// <summary>
        /// Test case is aborted
        /// </summary>
        Aborted,

        /// <summary>
        /// Test case status is unknown
        /// </summary>
        Unknown, 
    }

    /// <summary>
    /// Internal use only.
    /// </summary>
    public interface IProtocolTestContext
    {
        /// <summary>
        /// Gets and sets test deployment directory.
        /// </summary>
        string TestDeploymentDir { get; }

        /// <summary>
        /// Gets and sets test case run outcome.
        /// </summary>
        PtfTestOutcome TestOutcome { get; }

        /// <summary>
        /// Gets and sets running test method name.
        /// </summary>
        string TestMethodName { get; }
    }
}

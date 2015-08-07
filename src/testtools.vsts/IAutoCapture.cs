// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Represents errors that occurs during running auto-capture.
    /// </summary>
    public class AutoCaptureException : Exception
    {
        /// <summary>
        /// Stops running test cases when auto-capture error occurs.
        /// </summary>
        public bool StopRunning = false;

        /// <summary>
        /// Initializes a new instance of AutoCaptureException with error message specified.
        /// </summary>
        /// <param name="msg">Error message.</param>
        /// <param name="stopRunning">Stops running test cases when auto-capture error occurs.</param>
        public AutoCaptureException(string msg, bool stopRunning = false)
            : base(msg)
        {
            StopRunning = stopRunning;
        }
    }

    /// <summary>
    /// An interface for implementing automatic network message capture.
    /// </summary>
    public interface IAutoCapture
    {
        /// <summary>
        /// Initializes the AutoCapture class before test cases run.
        /// </summary>
        /// <param name="properties">Properties in PTF Configure file.</param>
        /// <param name="className">The test class name.</param>
        void Initialize(NameValueCollection properties, string className);

        /// <summary>
        /// Cleans up the autocapture class after all the test cases in a class are finished.
        /// </summary>
        void Cleanup();

        /// <summary>
        /// Starts a capture for a test case.
        /// </summary>
        /// <param name="testName">Test case name</param>
        void StartCapture(string testName);

        /// <summary>
        /// Stops the capture for a test case.
        /// </summary>
        void StopCapture();
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Interface of TestLog
    /// </summary>
    public interface ITestLog
    {
        /// <summary>
        /// BeginTest
        /// </summary>
        /// <param name="name">The test name</param>
        void BeginTest(string name);

        /// <summary>
        /// EndTest
        /// </summary>
        void EndTest();

        /// <summary>
        /// Checks condition together with description to by-pass assertion failure.
        /// </summary>
        /// <param name="condition">A bool condition</param>
        /// <param name="description">Description message for Assert</param>
        /// <returns>false if and only if condition is false and description is not by-passed.</returns>
        bool IsTrue(bool condition, string description);

        /// <summary>
        /// Assume
        /// </summary>
        /// <param name="condition">A bool condition</param>
        /// <param name="description">Description message for Assume</param>
        void Assume(bool condition, string description);

        /// <summary>
        /// Assert
        /// </summary>
        /// <param name="condition">A bool condition</param>
        /// <param name="description">Description message for Assert</param>
        void Assert(bool condition, string description);

        /// <summary>
        /// Comment
        /// </summary>
        /// <param name="description">Description message for a comment in log</param>
        void Comment(string description);

        /// <summary>
        /// Checkpoint
        /// </summary>
        /// <param name="description">Description message for a check point in log</param>
        void Checkpoint(string description);
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools.Checking
{
    /// <summary>
    /// An exception which is thrown when a debugging check result is failed or inconclusive.
    /// </summary>
    [Serializable]
    public class TestDebugException : Exception
    {
        /// <summary>
        /// Constructs a new instance of TestDebugException.
        /// </summary>
        public TestDebugException() { }

        /// <summary>
        /// Initializes a new instance of TestDebugException with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public TestDebugException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of TestDebugException with a specified error message and an inner exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception. If the parameter is not a
        /// null reference, the current exception is raised in a catch block that handles
        /// the inner exception. </param>
        public TestDebugException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Initializes a new instance of the TestDebugException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected TestDebugException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}

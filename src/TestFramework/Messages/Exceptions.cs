// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Protocols.TestTools.Messages.Runtime
{
    /// <summary>
    /// An exception which is thrown when variable is read before it is bound.
    /// </summary>
    [Serializable]
    public class UnboundVariableException : Exception
    {
        /// <summary>
        /// Constructs the UnboundVariableException.
        /// </summary>
        public UnboundVariableException()
            : base()
        { }

        /// <summary>
        /// Constructs the UnboundVariableException.
        /// </summary>
        /// <param name="message">error message</param>
        public UnboundVariableException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs the UnboundVariableException.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException">The inner exception.</param>
        public UnboundVariableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// An exception which is thrown when assertion failure is hit.
    /// </summary>
    [Serializable]
    public class TestFailureException : Exception
    {
        /// <summary>
        /// Constructs the TestFailureException.
        /// </summary>
        public TestFailureException()
            : base()
        { }

        /// <summary>
        /// Constructs the TestFailureException.
        /// </summary>
        /// <param name="message">error message</param>
        public TestFailureException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs the TestFailureException.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException">The inner exception.</param>
        public TestFailureException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Class representing the exceptional case of a failed transaction.
    /// </summary>
    [Serializable]
    public class TransactionFailedException : Exception
    {
        /// <summary>
        /// Constructs transaction failed exception
        /// </summary>
        public TransactionFailedException()
            : base()
        {
        }

        /// <summary>
        /// Constructs transaction failed exception
        /// </summary>
        /// <param name="message">The message text.</param>
        public TransactionFailedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs transaction failed exception
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="innerException">The inner exception.</param>
        public TransactionFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

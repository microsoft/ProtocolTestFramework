// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Protocols.TestTools.Test.Utilities
{
    /// <summary>
    /// An attrubite used for check if an expected exception is thrown in the case.
    /// It will check both exception type and message.
    /// </summary>
    public sealed class PTFExpectedException : ExpectedExceptionBaseAttribute
    {
        private Type ptfExpectedExceptionType;
        private string ptfExpectedExceptionMessage;

        public PTFExpectedException(Type expectedExceptionType)
        {
            ptfExpectedExceptionType = expectedExceptionType;
            ptfExpectedExceptionMessage = string.Empty;
        }

        public PTFExpectedException(Type expectedExceptionType, string expectedExceptionMessage)
        {
            ptfExpectedExceptionType = expectedExceptionType;
            ptfExpectedExceptionMessage = expectedExceptionMessage;
        }

        protected override void Verify(Exception exception)
        {
            Assert.IsNotNull(exception);

            Assert.IsInstanceOfType(
                exception,
                ptfExpectedExceptionType, 
                "Wrong type of exception was thrown.");

            if (!ptfExpectedExceptionMessage.Length.Equals(0))
            {
                int indexOfStackTrace = exception.Message.IndexOf("\r\n===== Stack Trace =====");
                string getActualExceptionMessage = exception.Message.Substring(0, indexOfStackTrace);
                Assert.AreEqual(
                    ptfExpectedExceptionMessage, getActualExceptionMessage,
                    "Wrong exception message was returned.");
            }
        }
    }
}

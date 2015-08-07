// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// All checker kinds that PTF supported
    /// </summary>
    public enum CheckerKinds
    {
        /// <summary>
        /// The assert checker
        /// </summary>
        AssertChecker,

        /// <summary>
        /// The assume checker
        /// </summary>
        AssumeChecker,

        /// <summary>
        /// The debug checker
        /// </summary>
        DebugChecker,
    }

    /// <summary>
    /// An interface which is used for providing test validation and verification infrastructure.
    /// </summary>
    /// <remarks>
    /// This interface describes a set of methods which allow validation of data. Protocol test code should direct all
    /// validation code to those methods.
    /// <para>
    /// The current test's execution stops when an assertion fails, and an according entry is automatically
    /// created in the test log. Depending on log settings, an entry may also be created if an assertion succeeds. In general,
    /// test code should not and does not need to provide extra logging output related to an assertion pass or failure.
    /// </para>
    /// <para>
    /// All assertion methods are required to have a message string format parameter and an optional array of objects which is applied to 
    /// that message format using <see cref="String.Format(string, object[])"/>. These methods does not support omitting that message, and also does not support 
    /// giving a simple string instead of a format string. Therefore, formatting characters must be always escaped
    /// in the format string (write "{{" for "{" and "}}" for "}").
    /// </para>
    /// </remarks>
    public interface IChecker 
    {
        /// <summary>
        /// Gets the test site which this checker object is hosted on.
        /// </summary>
        ITestSite Site { get; }

        /// <summary>
        /// Checks if any error occurs.
        /// </summary>
        void CheckErrors();

        /// <summary>
        /// Raises a failure assertion. 
        /// </summary>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing one or more objects to format.</param>
        void Fail(string message, params object[] parameters);

        /// <summary>
        /// Raises a successful assertion. 
        /// </summary>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing one or more objects to format.</param>
        void Pass(string message, params object[] parameters);

        /// <summary>
        /// Raises an inconclusive assertion.
        /// </summary>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing one or more objects to format.</param>        
        void Inconclusive(string message, params object[] parameters);

        /// <summary>Verifies that two specified values are equal. 
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare</typeparam>
        /// <param name="expected">The first object to compare. This is the object the test expects.</param>
        /// <param name="actual">The second object to compare. This is the object the test produced.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing zero or more objects to format.</param>
        void AreEqual<T>(T expected, T actual, string message, params object[] parameters);


        /// <summary>
        /// Verifies that two specified values are not equal.  
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare</typeparam>
        /// <param name="expected">The first object to compare. This is the object the test expects.</param>
        /// <param name="actual">The second object to compare. This is the object the test produced.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing zero or more objects to format.</param>        
        void AreNotEqual<T>(T expected, T actual, string message, params object[] parameters);

        /// <summary>
        /// Verifies that two specified object references refer to the same object.  
        /// </summary>
        /// <param name="expected">The first object to compare. This is the object the test expects.</param>
        /// <param name="actual">The second object to compare. This is the object the test produced.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing zero or more objects to format.</param>        
        void AreSame(object expected, object actual, string message, params object[] parameters);

        /// <summary>
        /// Verifies that two specified object references do not refer to the same object.  
        /// </summary>
        /// <param name="expected">The first object to compare. This is the object the test expects.</param>
        /// <param name="actual">The second object to compare. This is the object the test produced.</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing zero or more objects to format.</param>        
        void AreNotSame(object expected, object actual, string message, params object[] parameters);

        /// <summary>
        /// Verifies that the given bool value is true.
        /// </summary>
        /// <param name="value">The bool value to check</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing zero or more objects to format.</param>        
        void IsTrue(bool value, string message, params object[] parameters);

        /// <summary>
        /// Verifies that the given bool value is false.
        /// </summary>
        /// <param name="value">The bool value to check</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing zero or more objects to format.</param>        
        void IsFalse(bool value, string message, params object[] parameters);

        /// <summary>
        /// Verifies that the given object reference is not null.
        /// </summary>
        /// <param name="value">The object check</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing zero or more objects to format.</param>
        void IsNotNull(object value, string message, params object[] parameters);

        /// <summary>
        /// Verifies that the given reference is null.
        /// </summary>
        /// <param name="value">The object check</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing zero or more objects to format.</param>
        void IsNull(object value, string message, params object[] parameters);

        /// <summary>
        /// Verifies that the given object is an instance of the given type.
        /// </summary>
        /// <param name="value">The object value to check</param>
        /// <param name="type">The object type to check</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing zero or more objects to format.</param>        
        void IsInstanceOfType(object value, Type type, string message, params object[] parameters);

        /// <summary>
        /// Verifies that the given object is not an instance of the given type.
        /// </summary>
        /// <param name="value">The object value to check</param>
        /// <param name="type">The object type to check</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing zero or more objects to format.</param>        
        void IsNotInstanceOfType(object value, Type type, string message, params object[] parameters);


        /// <summary>
        /// Verifies that the given error code is indicating a successful result.
        /// </summary>
        /// <param name="hresult">The HRESULT value to check</param>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing zero or more objects to format.</param>        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        void IsSuccess(int hresult, string message, params object[] parameters);

        /// <summary>
        /// Indicates something that could not be verified currently.  
        /// </summary>
        /// <param name="message">A composite format string.</param>
        /// <param name="parameters">An Object array containing zero or more objects to format.</param>     
        void Unverified(string message, params object[] parameters);
    }
}

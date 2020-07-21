// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Protocols.TestTools.Messages.Runtime;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Internal use only. Interface between PTF and TCM.
    /// TCM should use this interface to inform PTF the status
    /// of overall tests run.
    /// An instance of this interface could be retrieved by
    /// ProtocolTestsManagerFactory.GetTestsManager()
    /// </summary>
    public interface IProtocolTestsManager
    {
        /// <summary>
        /// Adds an event to the event queue.
        /// </summary>
        /// <param name="eventInfo">The reflection information of the event.</param>
        /// <param name="target">
        /// The target object. Must be given for instance-based, non-adapter methods, 
        /// otherwise must be null.
        ///  </param>
        /// <param name="arguments">the arguments to the return method.</param>
        void AddEvent(EventInfo eventInfo, object target, params object[] arguments);

        /// <summary>
        /// Expect one or more events described by patterns.
        /// </summary>
        /// <param name="timeOut">Time to wait for event.</param>
        /// <param name="failIfNone">Behavior on failure.</param>
        /// <param name="expected">Expected events</param>
        /// <returns>
        ///  Returns index of expected which matched if event is available in time.
        ///  If event is not available, returns -1 if <c>failIfNone</c> is false, otherwise
        ///  produces test failure with regarding diagnostics.
        /// </returns>
        int ExpectEvent(TimeSpan timeOut, bool failIfNone, params ExpectedEvent[] expected);

        /// <summary>
        /// Adds a method return to the return queue.
        /// </summary>
        /// <param name="methodInfo">The reflection information of the method.</param>
        /// <param name="target">
        /// The target object. Must be given for instance-based, non-adapter methods, 
        /// must be null otherwise.
        ///  </param>
        /// <param name="arguments"></param>
        void AddReturn(MethodBase methodInfo, object target, params object[] arguments);

        /// <summary>
        /// Expect one or more returns described by patterns.
        /// </summary>
        /// <param name="timeOut">Time to wait for return.</param>
        /// <param name="failIfNone">Behavior on failure.</param>
        /// <param name="expected">Expected returns</param>
        /// <returns>
        ///  Returns index of expected which matched if return is available in time.
        ///  If return is not available, returns -1 if <c>failIfNone</c> is false, otherwise
        ///  produces test failure with regarding diagnostics.
        /// </returns>
        int ExpectReturn(TimeSpan timeOut, bool failIfNone, params ExpectedReturn[] expected);

        /// <summary>
        /// Begins executing a test case.
        /// </summary>
        /// <param name="name"></param>
        void BeginTest(string name);

        /// <summary>
        /// Ends executing a test case.
        /// </summary>
        void EndTest();

        /// <summary>
        /// Executes a test assertion.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="description"></param>
        void Assert(bool condition, string description);

        /// <summary>
        /// Executes a test assumption.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="description"></param>
        void Assume(bool condition, string description);

        /// <summary>
        /// Executes a checkpoint.
        /// </summary>
        /// <param name="description"></param>
        void Checkpoint(string description);

        /// <summary>
        /// Logs a comment about test execution.
        /// </summary>
        /// <param name="description"></param>
        void Comment(string description);

        /// <summary>
        /// Upon observation timeout, checks current event observation queue status and decides 
        /// whether case should pass or fail.
        /// </summary>
        /// <param name="isAcceptingState"></param>
        /// <param name="expected"></param>
        void CheckObservationTimeout(bool isAcceptingState, params ExpectedEvent[] expected);

        /// <summary>
        /// Selects one satisfied pre-constraint from one or more given preconstraints described by patterns.
        /// </summary>   
        /// <param name="printDiagnosisIfFail">Behavior on failure.</param>
        /// <param name="expected">Expected pre constraints</param>
        /// <returns>
        ///  Returns index of the first preconstraint which satisfies.
        ///  if none of the pre-constraints satisfies, returns -1 if <c>failIfNone</c> is false, otherwise
        ///  produces test failure with corresponding diagnostics.
        /// </returns>
        int SelectSatisfiedPreConstraint(bool printDiagnosisIfFail, params ExpectedPreConstraint[] expected);

        /// <summary>
        /// Creates a new variable which can be transacted.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        IVariable<T> CreateVariable<T>(string name);

        /// <summary>
        /// Begins a transaction. Note that the execution of a Checker happens implicitly within a transaction.
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// Ends transaction, either committing variables which have been bound, or rolling them back. 
        /// Note that the execution of a Checker happens implicitly within a transaction.
        /// </summary>
        /// <param name="commit"></param>
        void EndTransaction(bool commit);
    }
}

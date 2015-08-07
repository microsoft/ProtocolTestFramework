// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Protocols.TestTools.Messages.Runtime;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// The Identifier Binding Class
    /// </summary>
    /// <typeparam name="Target">The target type to which the identifier is bound.</typeparam>
    public class IdentifierBinding<Target> : 
        Microsoft.Protocols.TestTools.Messages.Runtime.IdentifierBinding<Target>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public IdentifierBinding()
            : base()
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">The test site</param>
        public IdentifierBinding(ITestSite site)
            : base(ExtensionHelper.GetRuntimeHost(site))
        { }
    }


    /// <summary>
    /// Provides a series of helper methods in testtools extension
    /// </summary>
    public static class ExtensionHelper
    {
        /// <summary>
        /// Binding PTF logger checker to Message Runtime Host.
        /// </summary>
        static void RegisterRuntimeHost(ITestSite site, IRuntimeHost host)
        {
            host.AssertChecker += new EventHandler<MessageLogEventArgs>(
                delegate(object sender, MessageLogEventArgs e)
                {
                    if (e == null)
                    {
                        throw new ArgumentNullException("MessageLogEventArgs");
                    }

                    if (e.Condition)
                    {
                        site.Assert.Pass(e.Message, e.Parameters);
                    }
                    else
                    {
                        site.Assert.Fail(e.Message, e.Parameters);
                    }
                });

            host.AssumeChecker += new EventHandler<MessageLogEventArgs>(
                delegate(object sender, MessageLogEventArgs e)
                {
                    if (e == null)
                    {
                        throw new ArgumentNullException("MessageLogEventArgs");
                    }

                    if (e.Condition)
                    {
                        site.Assume.Pass(e.Message, e.Parameters);
                    }
                    else
                    {
                        site.Assume.Fail(e.Message, e.Parameters);
                    }
                });

            host.DebugChecker += new EventHandler<MessageLogEventArgs>(
                delegate(object sender, MessageLogEventArgs e)
                {
                    if (e == null)
                    {
                        throw new ArgumentNullException("MessageLogEventArgs");
                    }

                    if (e.Condition)
                    {
                        site.Debug.Pass(e.Message, e.Parameters);
                    }
                    else
                    {
                        site.Debug.Fail(e.Message, e.Parameters);
                    }
                });

            host.MessageLogger += new EventHandler<MessageLogEventArgs>(
                delegate(object sender, MessageLogEventArgs e)
                {
                    if (e == null)
                    {
                        throw new ArgumentNullException("MessageLogEventArgs");
                    }

                    site.Log.Add(
                        LogKindToLogEntryKind(e.LogEntryKind),
                        e.Message, e.Parameters);
                });

            host.RequirementLogger += new EventHandler<RequirementCaptureEventArgs>(
                delegate(object sender, RequirementCaptureEventArgs e)
                {
                    if (e == null)
                    {
                        throw new ArgumentNullException("RequirementCaptureEventArgs");
                    }

                    site.CaptureRequirement(
                        e.ProtocolName, e.RequirementId, e.RequirementDescription);
                });
        }

        /// <summary>
        /// Runtime log kind to PTF log entry.
        /// </summary>
        static LogEntryKind LogKindToLogEntryKind(LogKind kind)
        {
            LogEntryKind entryKind;
            switch (kind)
            {
                case LogKind.CheckSucceeded:
                    entryKind = LogEntryKind.CheckSucceeded;
                    break;
                case LogKind.CheckFailed:
                    entryKind = LogEntryKind.CheckFailed;
                    break;
                case LogKind.Checkpoint:
                    entryKind = LogEntryKind.Checkpoint;
                    break;
                case LogKind.Comment:
                    entryKind = LogEntryKind.Comment;
                    break;
                case LogKind.Debug:
                    entryKind = LogEntryKind.Debug;
                    break;
                case LogKind.Warning:
                    entryKind = LogEntryKind.Warning;
                    break;
                default:
                    throw new InvalidOperationException("Unexpected LogKind.");
            }
            return entryKind;
        }

        /// <summary>
        /// Gets the runtime host.
        /// </summary>
        /// <param name="site">The test site</param>
        /// <returns>Returns the instance of the runtime host</returns>
        public static IRuntimeHost GetRuntimeHost(ITestSite site)
        {
            if (site == null)
            {
                return null;
            }

            bool marshallerTrace = site != null ? site.Properties["Marshaler.trace"] == "true" : false;
            bool disablevalidation = site != null ? site.Properties["disablevalidation"] == "true" : false;
            RuntimeHostProvider.Initialize(marshallerTrace, disablevalidation);
            IRuntimeHost host = RuntimeHostProvider.RuntimeHost;
            //only register once
            if (string.IsNullOrEmpty(site.Properties["IsRuntimeHostRegistered"]))
            {
                RegisterRuntimeHost(site, host);
                site.Properties["IsRuntimeHostRegistered"] = "true";
            }
            return host;
        }

        /// <summary>
        /// Handle VS UnitTest Framework exception
        /// </summary>
        /// <param name="e">The exception needs to be handled</param>
        /// <param name="site">The test site</param>
        public static void HandleVSException(Exception e, ITestSite site)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            if (site == null)
            {
                throw new ArgumentNullException("site");
            }
            //remove the dependency to the VS UnitTest Framework.
            Type exceptionType = e.GetType();
            string assertFailedException = "Microsoft.VisualStudio.TestTools.UnitTesting.AssertFailedException";
            string assertInconclusiveException = "Microsoft.VisualStudio.TestTools.UnitTesting.AssertInconclusiveException";
            if (string.Compare(exceptionType.FullName, assertFailedException, true) == 0)
            {
                site.Assert.Fail("asynchronous assertion failed in receive loop and has been logged");
            }
            else if (string.Compare(exceptionType.FullName, assertInconclusiveException, true) == 0)
            {
                site.Assert.Fail("asynchronous assumption failed in receive loop and has been logged");
            }
        }
    }
}

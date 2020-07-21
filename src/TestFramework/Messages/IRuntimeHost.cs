// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools.Messages.Runtime
{
    /// <summary>
    /// A host which provides logger and checker functions for the message runtime.
    /// </summary>
    public interface IRuntimeHost
    {
        /// <summary>
        /// Gets whether "marshaler" tracing is enabled.
        /// </summary>
        bool MarshallerTrace { get; }

        /// <summary>
        /// Gets whether validation is disabled.
        /// </summary>
        bool DisableValidation { get; }

        /// <summary>
        /// An event which is raised when doing assert check.
        /// </summary>
        event EventHandler<MessageLogEventArgs> AssertChecker;

        /// <summary>
        /// An event which is raised when doing assume check.
        /// </summary>
        event EventHandler<MessageLogEventArgs> AssumeChecker;

        /// <summary>
        /// An event which is raised when doing debug check.
        /// </summary>
        event EventHandler<MessageLogEventArgs> DebugChecker;

        /// <summary>
        /// An event which is raised when logging a message.
        /// </summary>
        event EventHandler<MessageLogEventArgs> MessageLogger;

        /// <summary>
        /// An event which is raised when capturing a requirement.
        /// </summary>
        event EventHandler<RequirementCaptureEventArgs> RequirementLogger;

        /// <summary>
        /// Do assert check and log error message when condition is failed.
        /// </summary>
        /// <param name="condition">The check condition</param>
        /// <param name="message">The message</param>
        /// <param name="parameters">The parameters for message format</param>
        void Assert(bool condition, string message, params object[] parameters);

        /// <summary>
        /// Do assume check and log error message when condition is failed.
        /// </summary>
        /// <param name="condition">The check condition</param>
        /// <param name="message">The message</param>
        /// <param name="parameters">The parameters for message format</param>
        void Assume(bool condition, string message, params object[] parameters);

        /// <summary>
        /// Do debug check and log error message when condition is failed.
        /// </summary>
        /// <param name="condition">The check condition</param>
        /// <param name="message">The message</param>
        /// <param name="parameters">The parameters for message format</param>
        void Debug(bool condition, string message, params object[] parameters);

        /// <summary>
        /// Add message to logger.
        /// </summary>
        /// <param name="kind">The log kind of the message</param>
        /// <param name="message">The message</param>
        /// <param name="parameters">The parameters for message format</param>
        void AddLog(LogKind kind, string message, params object[] parameters);

        /// <summary>
        /// Captures a requirement.
        /// </summary>
        /// <param name="protocolName">The protocol short name</param>
        /// <param name="requirementId">The requirement id</param>
        /// <param name="description">The requirement description</param>
        void CaptureRequirement(string protocolName, int requirementId, string description);
    }

    /// <summary>
    /// The class which represents event arguments used by event handler API in <see cref="IRuntimeHost"/> 
    /// </summary>
    public class RequirementCaptureEventArgs : EventArgs
    {
        private string protocolName;
        private int requirementId;
        private string requirementDescription;

        /// <summary>
        /// Constructor.
        /// </summary>
        public RequirementCaptureEventArgs() { }

        /// <summary>
        /// Overloaded Constructor.
        /// </summary>
        /// <param name="requirementId">The requirement id</param>
        /// <param name="description">The description of the requirement</param>
        public RequirementCaptureEventArgs(int requirementId, string description)
        {
            this.requirementId = requirementId;
            this.requirementDescription = description;
        }

        /// <summary>
        /// Overloaded Constructor.
        /// </summary>
        /// <param name="protocolName">The shot name of the protocol</param>
        /// <param name="requirementId">The requirement id</param>
        /// <param name="description">The description of the requirement</param>
        public RequirementCaptureEventArgs(string protocolName, int requirementId, string description)
        {
            this.protocolName = protocolName;
            this.requirementId = requirementId;
            this.requirementDescription = description;
        }

        /// <summary>
        /// Gets the protocol short name.
        /// </summary>
        public string ProtocolName
        {
            get
            {
                return this.protocolName;
            }
        }

        /// <summary>
        /// Gets the requirement id.
        /// </summary>
        public int RequirementId
        {
            get
            {
                return this.requirementId;
            }
        }

        /// <summary>
        /// Gets the requirement description.
        /// </summary>
        public string RequirementDescription
        {
            get
            {
                return this.requirementDescription;
            }
        }
    }
    /// <summary>
    /// The class which represents event arguments used by event handler API in <see cref="IRuntimeHost"/> 
    /// </summary>
    public class MessageLogEventArgs : EventArgs
    {
        private bool condition;
        private string message;
        private object[] parameters;
        private LogKind kind;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MessageLogEventArgs()
        { }

        /// <summary>
        /// Overloaded constructor.
        /// </summary>
        /// <param name="kind">message log type</param>
        /// <param name="message">The message to be logged</param>
        /// <param name="parameters">extended parameters</param>
        public MessageLogEventArgs(LogKind kind, string message, object[] parameters)
        {
            this.kind = kind;
            this.message = message;            
            this.parameters = parameters;
        }

        /// <summary>
        /// Overloaded constructor.
        /// </summary>
        /// <param name="kind">message log type</param>
        /// <param name="condition">The check condition</param>
        /// <param name="message">The error message to be logged</param>
        /// <param name="parameters">extended parameters</param>
        public MessageLogEventArgs(LogKind kind, bool condition, string message, object[] parameters)
        {
            this.kind = kind;
            this.condition = condition;
            this.message = message;
            this.parameters = parameters;
        }

        /// <summary>
        /// Gets the check condition.
        /// </summary>
        public bool Condition
        {
            get
            {
                return this.condition;
            }
        }

        /// <summary>
        /// Gets the message to be logged.
        /// </summary>
        public string Message
        {
            get
            {
                return this.message;
            }
        }

        /// <summary>
        /// Gets the log entry kind of the message.
        /// </summary>
        public LogKind LogEntryKind
        {
            get
            {
                return this.kind;
            }
        }

        /// <summary>
        /// Gets the parameters of the message format.
        /// </summary>
        public object[] Parameters
        {
            get
            {
                return this.parameters;
            }
        }
    }

    /// <summary>
    /// An enumeration type which represents the types of message log entries. 
    /// </summary>
    public enum LogKind
    {
        /// <summary>
        /// Indicates that no log entry kind is specified.
        /// </summary>
        UnKnown,

        /// <summary>
        /// Indicates a check point passed. Captured requirements should be logged as this kind.
        /// </summary>
        Checkpoint,

        /// <summary>
        /// Indicates an assertion verification has passed.
        /// </summary>
        CheckSucceeded,

        /// <summary>
        /// Indicates an assertion verification has failed.
        /// </summary>
        CheckFailed,

        /// <summary>
        /// A free-style log entry for comment information.
        /// </summary>
        Comment,

        /// <summary>
        /// A free-style log entry for warning information.
        /// </summary>
        Warning,

        /// <summary>
        /// A free-style log entry for debugging information. 
        /// </summary>
        Debug,
    }
}

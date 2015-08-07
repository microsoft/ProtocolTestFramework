// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Collections.Specialized;
using System.IO;
using System.Xml;

namespace Microsoft.Protocols.TestTools.Logging
{
    /// <summary>
    /// An abstract class which provides a generic view of a log sink.
    /// </summary>
    public abstract class LogSink : IDisposable
    {
        /// <summary>
        /// An enumeration type used to represents the mode which logging message should be logged as.
        /// </summary>
        protected enum LogMode
        {
            /// <summary>
            /// Indicates logging a message as an independ logging entry.
            /// </summary>
            EntryMode,

            /// <summary>
            /// Indicates logging a message as the beginning of a logging group. 
            /// For example, LogEntryKind.BeginGroup, LogEntryKind.EnterAdapter, LogEntryKind.EnterMethod, etc.
            /// </summary>
            BeginGroupMode, 

            /// <summary>
            /// Indicates logging a message as the end of a logging group.
            /// For example, LogEntryKind.ExitGroup, LogEntryKind.ExitAdapter, LogEntryKind.ExitMethod, etc.            
            /// </summary>
            EndGroupMode 
        }

        /// <summary>
        /// Disables the default constructor.
        /// </summary>
        private LogSink()
        {
        }

        /// <summary>
        /// Converts a log entry kind to the corresponding logging mode.
        /// </summary>
        /// <param name="kind">The kind of logging entry</param>
        /// <returns>The logging mode to be applied.</returns>
        protected static LogMode LogKindToMode(LogEntryKind kind)
        {
            switch (kind)
            {
                case LogEntryKind.BeginGroup:
                case LogEntryKind.EnterAdapter:
                case LogEntryKind.EnterMethod:
                    return LogMode.BeginGroupMode;
                case LogEntryKind.EndGroup:
                case LogEntryKind.ExitAdapter:
                case LogEntryKind.ExitMethod:
                    return LogMode.EndGroupMode;
            }
            // Normal log entries.
            return LogMode.EntryMode;
        }

        /// <summary>
        /// The stack for logged begin group entries to verify if they match correct end group entries.
        /// Should push a kind when logging a begin group entry and pop 
        /// </summary>
        private Stack<LogEntryKind> loggedBeginGroupEntries = new Stack<LogEntryKind>();

        /// <summary>
        /// Writes a logging message to the current log sink.
        /// </summary>
        /// <param name="information">The information of the log entry.</param>
        public void WriteEntry(Dictionary<string, object> information)
        {
            if (information == null)
            {
                throw new ArgumentNullException("information");
            }

            if (disposed)
            {
                throw new ObjectDisposedException("LogSink");
            }

            // Gets logging mode from kind.
            LogMode mode = LogKindToMode((LogEntryKind)information[LogInformationName.LogEntryKind]);

            // Writes messages by mode.
            if (mode == LogMode.EntryMode)
            {
                OnWriteEntry(information);
            }
            else if (mode == LogMode.BeginGroupMode)
            {
                OnWriteBeginGroup(information);
            }
            else if (mode == LogMode.EndGroupMode)
            {
                OnWriteEndGroup(information);
            }
        }

        /// <summary>
        /// Writes a non-group logging message to the current log sink.
        /// </summary>
        /// <param name="information">The information of the log entry.</param>
        protected abstract void OnWriteEntry(Dictionary<string, object> information);

        /// <summary>
        /// Writes a message as the beginning of a log group into the current log sink. 
        /// Entering and existing of a group logging message must be matched.
        /// </summary>
        /// <param name="information">The information of the log entry.</param>
        /// <remarks>Overridden this method should call the method of base to enable verifying
        /// if group entries are matched.</remarks>
        protected virtual void OnWriteBeginGroup(Dictionary<string, object> information)
        {
            EnsureLoggingGroupMatching((LogEntryKind)information[LogInformationName.LogEntryKind]);
        }

        /// <summary>
        /// Writes a message as the end of a log group into the current log sink. 
        /// Entering and existing of a group logging message must be matched.
        /// </summary>
        /// <param name="information">The information of the log entry.</param>
        /// <remarks>Overridden this method should call the method of base to enable verifying
        /// if group entries are matched.</remarks>
        protected virtual void OnWriteEndGroup(Dictionary<string, object> information)
        {
            EnsureLoggingGroupMatching((LogEntryKind)information[LogInformationName.LogEntryKind]);
        }

        /// <summary>
        /// Checks whether the beginning and the endding of a group logging message are matched.
        /// If they are not matched, an InvalidOperationException exception will be raised.
        /// </summary>
        /// <param name="kind">A beginning group log entry kind.</param>
        protected void EnsureLoggingGroupMatching(LogEntryKind kind)
        {
            LogMode mode = LogKindToMode(kind);

            if (mode == LogMode.BeginGroupMode)
            {
                // Push begin group kind.
                loggedBeginGroupEntries.Push(kind);
            }
            else if (mode == LogMode.EndGroupMode)
            {
                // Verifies if this is a matching group kind.
                if (loggedBeginGroupEntries.Count == 0)
                {
                    throw new InvalidOperationException(
                        "The end group kind must be present after a corresponding begin group kind.");
                }
                else if (kind != GetMatchedEndGroupKind(loggedBeginGroupEntries.Peek()))
                {
                    throw new InvalidOperationException("The end group kind is mismatched.");
                }

                // Pop the verified group kind.
                loggedBeginGroupEntries.Pop();
            }
        }

        /// <summary>
        /// Gets the corresponding end group kind of a beginning group log kind.
        /// </summary>
        /// <param name="kind">The beginning of a group log kind.</param>
        /// <returns>The corresponding end group kind.</returns>
        private static LogEntryKind GetMatchedEndGroupKind(LogEntryKind kind)
        {
            switch (kind)
            {
                case LogEntryKind.BeginGroup:
                    return LogEntryKind.EndGroup;
                case LogEntryKind.EnterAdapter:
                    return LogEntryKind.ExitAdapter;
                case LogEntryKind.EnterMethod:
                    return LogEntryKind.ExitMethod;
            }
            throw new ArgumentException("The inputted entry kind has not an end group kind.");
        }

        /// <summary>
        /// Replace the invalid Unicode characters with \uXXXX
        /// </summary>
        /// <param name="message">Original message</param>
        /// <returns>Message without invalid Unicode chars</returns>
        protected static string ReplaceInvalidChars(string message)
        {
            if (message == null) return null;
            StringBuilder sbOutput = new StringBuilder();
            char ch;
            for (int i = 0; i < message.Length; i++)
            {
                ch = message[i];
                
                //Refer to REC-xml-20040204 section 2.2
                //Available at http://www.w3.org/TR/2004/REC-xml-20040204/#charsets
                if ((ch >= 0x0020 && ch <= 0xD7FF) ||
                (ch >= 0xE000 && ch <= 0xFFFD) ||
                ch == 0x0009 || ch == 0x000A || ch == 0x000D)
                {
                    sbOutput.Append(ch);
                }
                else
                {
                    sbOutput.Append("\\u"+((UInt16)ch).ToString("X4"));
                }
            }
            return sbOutput.ToString();
        }

        private string name;

        /// <summary>
        /// Gets the name of the current sink.
        /// </summary>
        /// <value>The name of the sink.</value>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="LogSink"/> class. 
        /// </summary>
        /// <param name="name">The specified sink name.</param>
        protected LogSink(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Gets the value to specify whether the log sink need to notify immediately.
        /// </summary>
        public virtual bool NotifyImmediately
        {
            get { return false; }
        }

        /// <summary>
        /// Flushes the logging information into the current sink. 
        /// </summary>
        public abstract void Flush();

        /// <summary>
        /// Closes the current sink and releases resources.  
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        #region IDisposable Members

        /// <summary>
        /// Indicates if the current sink has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Gets or sets the disposing status.
        /// </summary>
        protected bool IsDisposed
        {
            get
            {
                return disposed;
            }
            set
            {
                disposed = value;
            }
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method is called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// </summary>
        /// <param name="disposing">
        /// If disposing equals false, the method is called by the 
        /// runtime from inside the finalizer and you should not reference 
        /// other objects. Only unmanaged resources can be disposed.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                Flush();
            }
            disposed = true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue 
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This destructor will run only if the Dispose method 
        /// does not get called.
        /// It gives your base class the opportunity to finalize.
        /// Do not provide destructors in types derived from this class.
        /// </summary>
        ~LogSink()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }


        #endregion
    }

    /// <summary>
    /// An abstract class which provides a generic view of a text log sink.
    /// </summary>
    public abstract class TextSink : LogSink
    {
        /// <summary>
        /// Gets the text stream writer for dumping logs.
        /// </summary>
        protected abstract TextWriter Writer { get; }

        /// <summary>
        /// The string is to be appended to the logging message as a return symbol (line terminator).
        /// </summary>
        protected const string newLine = "\r\n";

        /// <summary>
        /// The string to be inserted for an indent. 
        /// Can be several spaces or tabs. 
        /// </summary>
        private string indent = "   ";

        // The format string of a timestamp. Not using default format or "u" to fix the length of a representing timestamp.
        private const string timeStampFormat = "{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}.{6:D3}"; //e.g. 2007-05-16 16:50:23.412

        /// <summary>
        /// Current indent string which needs to be insert before a indented message.
        /// </summary>
        private string indentPositionString = String.Empty;

        /// <summary>
        /// Initializes a new instance of a TextSink.
        /// </summary>
        /// <param name="name">The name of the sink.</param>
        protected TextSink(string name)
            : base(name)
        {
        }

        #region private methods

        private void IncreaseIndent()
        {
            indentPositionString += indent;
        }

        private void DecreaseIndent()
        {
            if (indentPositionString.Length > 0)
            {
                indentPositionString = indentPositionString.Remove(0, indent.Length);
            }
        }

        #endregion

        #region override methods

        /// <summary>
        /// Implements <see cref="LogSink.Flush"/>.
        /// Flushes <see cref="TextSink.Writer"/>. 
        /// </summary>
        public override void Flush()
        {
            Writer.Flush();
        }

        /// <summary>
        /// Implements <see cref="LogSink.Dispose(bool)"/>.
        /// </summary>
        /// <param name="disposing">
        /// If disposing equals false, the method is called by the 
        /// runtime from inside the finalizer and you should not reference 
        /// other objects. Only unmanaged resources can be disposed.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

        }

        /// <summary>
        /// Implements <see cref="LogSink.OnWriteEntry"/>.
        /// </summary>
        /// <param name="information">The information of the log entry.</param>
        protected override void OnWriteEntry(Dictionary<string, object> information)
        {
            WriteEntry(
                (LogEntryKind)information[LogInformationName.LogEntryKind],
                (string)information[LogInformationName.Message],
                (DateTime)information[LogInformationName.TimeStamp]);
        }

        /// <summary>
        /// Implements <see cref="LogSink.OnWriteBeginGroup"/>.
        /// </summary>
        /// <param name="information">The information of the log entry.</param>
        protected override void OnWriteBeginGroup(Dictionary<string, object> information)
        {
            WriteEntry(
                (LogEntryKind)information[LogInformationName.LogEntryKind],
                (string)information[LogInformationName.Message],
                (DateTime)information[LogInformationName.TimeStamp]);
            IncreaseIndent();
            base.OnWriteBeginGroup(information);
        }

        /// <summary>
        /// Implements <see cref="LogSink.OnWriteEndGroup"/>.
        /// </summary>
        /// <param name="information">The information of the log entry.</param>
        protected override void OnWriteEndGroup(Dictionary<string, object> information)
        {
            base.OnWriteEndGroup(information);
            DecreaseIndent();
            WriteEntry(
                (LogEntryKind)information[LogInformationName.LogEntryKind],
                (string)information[LogInformationName.Message],
                (DateTime)information[LogInformationName.TimeStamp]);
        }

        #endregion

        #region Write overload methods
        /// <summary>
        /// Insert a time stamp and indent places.
        /// </summary>
        /// <param name="timeStamp">The corresponding time stamp.</param>
        private void WriteIndent(DateTime timeStamp)
        {
            // Write the time stamp
            // We don't want to use Format("u") to control the length exactly.
            Write(
                timeStampFormat,  // format sting
                timeStamp.Year,      // paramters
                timeStamp.Month,
                timeStamp.Day,
                timeStamp.Hour,
                timeStamp.Minute,
                timeStamp.Second,
                timeStamp.Millisecond);
            Write(" " + indentPositionString);
        }

        /// <summary>
        /// Replaces the format item in a specified message with the text equivalent 
        /// of the value of a corresponding object instance in a specified array. 
        /// </summary>
        /// <param name="message">A string containing zero or more format items. </param>
        /// <param name="args">An object array containing zero or more objects to format.</param>
        private void Write(string message, params object[] args)
        {
            if (!String.IsNullOrEmpty(message))
            {
                Writer.Write(String.Format(message, args));
            }
        }

        private void WriteLine(string message, params object[] args)
        {
            Write(message + newLine, args);
        }

        private void WriteLineWithIndent(DateTime timeStamp, string message, params object[] args)
        {
            WriteIndent(timeStamp);
            WriteLine(message, args);
        }

        private void WriteEntry(LogEntryKind kind, string message, DateTime timeStamp)
        {
            WriteLineWithIndent(timeStamp, "[{0}] {1}", kind.ToString(), message);
        }

        #endregion
    }

    /// <summary>
    /// An abstract class which provides a generic view of an xml log sink.
    /// </summary>
    public abstract class XmlSink : LogSink
    {
        /// <summary>
        /// Gets the text stream writer for dumping logs.
        /// </summary>
        protected abstract XmlWriter Writer { get; }

        // Indicates if it is first time of Running
        private bool firstRun = true;

        // temporary container of information for later usage in disposing.
        private Dictionary<string, object> info;

        /// <summary>
        /// Initializes a new instance of XmlSink .
        /// </summary>
        /// <param name="name">The name of the sink.</param>
        protected XmlSink(string name)
            : base(name)
        {
        }

        #region override methods

        /// <summary>
        /// Implements <see cref="LogSink.Flush"/>.
        /// Flushes <see cref="XmlSink.Writer"/>. 
        /// </summary>
        public override void Flush()
        {
            Writer.Flush();
        }

        /// <summary>
        /// Implements <see cref="LogSink.Dispose(bool)"/>.
        /// </summary>
        /// <param name="disposing">
        /// If disposing equals false, the method is called by the 
        /// runtime from inside the finalizer and you should not reference 
        /// other objects. Only unmanaged resources can be disposed.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && Writer != null)
                {
                    if (firstRun)
                    {
                        //if no entry writed, add the xml header
                        WriteXmlLogHeader();
                    }

                    // close LogEntries tag
                    Writer.WriteEndElement();
                    // close TestLog tag
                    Writer.WriteEndElement();

                    Writer.Close();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Implements <see cref="LogSink.OnWriteEntry"/>.
        /// </summary>
        /// <param name="information">The information of the log entry.</param>
        protected override void OnWriteEntry(Dictionary<string, object> information)
        {
            info = information;
            if (firstRun)
            {
                WriteXmlLogHeader();
                firstRun = false;
            }
            string testCaseName = string.Empty;
            if (information.ContainsKey(TestPropertyNames.CurrentTestCaseName))
            { 
                testCaseName = (string)information[TestPropertyNames.CurrentTestCaseName];
            }
            WriteEntry(
                (LogEntryKind)information[LogInformationName.LogEntryKind],
                (string)information[LogInformationName.Message],
                (DateTime)information[LogInformationName.TimeStamp],
                testCaseName);
        }

        /// <summary>
        /// Implements <see cref="LogSink.OnWriteBeginGroup"/>.
        /// </summary>
        /// <param name="information">The information of the log entry.</param>
        protected override void OnWriteBeginGroup(Dictionary<string, object> information)
        {
            OnWriteEntry(information);
            base.OnWriteBeginGroup(information);
        }

        /// <summary>
        /// Implements <see cref="LogSink.OnWriteEndGroup"/>.
        /// </summary>
        /// <param name="information">The information of the log entry.</param>
        protected override void OnWriteEndGroup(Dictionary<string, object> information)
        {
            base.OnWriteEndGroup(information);
            OnWriteEntry(information);
        }

        #endregion

        #region Write overload methods

        /// <summary>
        /// Runs at the first time of writing log entry, to produce xml log headers
        /// </summary>
        private void WriteXmlLogHeader()
        {
            Writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\" ");
            Writer.WriteStartElement("TestLog", "http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/TestLog");
            Writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
            Writer.WriteAttributeString("xsi", "schemaLocation", null, "http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/TestLog http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/TestLog.xsd");

            // start writing log entries
            Writer.WriteStartElement("LogEntries");
        }

        /// <summary>
        /// Gets a string which represents a timestamp in format of xs:dateTime.
        /// </summary>
        /// <param name="timeStamp">The corresponding timestamp.</param>
        /// <returns>a string presents the timestamp</returns>
        private static string GetTimeStampString(DateTime timeStamp)
        {
            // The format string of a timestamp. xs:dateTime UTC format.
            const string timeStampFormat = "{0:D4}-{1:D2}-{2:D2}T{3:D2}:{4:D2}:{5:D2}.{6:D3}Z"; //e.g. 2007-05-16 16:50:23.412Z
            timeStamp = timeStamp.ToUniversalTime();
            // return the time stamp
            // the format is xs:dateTime
            return String.Format(
                timeStampFormat,  // format string
                timeStamp.Year,      // paramters
                timeStamp.Month,
                timeStamp.Day,
                timeStamp.Hour,
                timeStamp.Minute,
                timeStamp.Second,
                timeStamp.Millisecond);
        }

        private void WriteEntry(LogEntryKind kind, string message, DateTime timeStamp, string testCaseName)
        {
            Writer.WriteStartElement("LogEntry");
            Writer.WriteAttributeString("kind", kind.ToString());
            Writer.WriteAttributeString("timeStamp", GetTimeStampString(timeStamp));
            if (kind == LogEntryKind.Checkpoint && !string.IsNullOrEmpty(testCaseName))
            {
                Writer.WriteAttributeString("testCase", testCaseName);
            }
            Writer.WriteElementString("Message", ReplaceInvalidChars(message));
            Writer.WriteEndElement();
        }
        #endregion
    }
}

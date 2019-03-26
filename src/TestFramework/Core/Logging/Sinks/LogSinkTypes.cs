// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;

namespace Microsoft.Protocols.TestTools.Logging
{
    /// <summary>
    /// Writer to write message to Console or STDOUT
    /// </summary>
    public class ConsoleWriter : TextWriter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="isRealTimeConsole">Whether or not to write messages to realtime console.</param>
        public ConsoleWriter(bool isRealTimeConsole)
        {

            IsRealTimeConsole = isRealTimeConsole;
        }

        /// <summary>
        /// The text color.
        /// </summary>
        public ConsoleColor Color;
        private bool IsRealTimeConsole;

        /// <summary>
        /// Returns the encoding in which the output is written.
        /// </summary>
        public override Encoding Encoding
        {
            get
            {
                return Console.Out.Encoding;
            }
        }

        /// <summary>
        /// Writes a string to the console.
        /// </summary>
        /// <param name="value"></param>
        public override void Write(string value)
        {
            if (IsRealTimeConsole)
            {

                Console.ForegroundColor = Color;
                using (var writerObj = new StreamWriter(Console.OpenStandardOutput()))
                {
                    writerObj.Write(value);
                    writerObj.Flush();
                }
            }
            else
            {
                Console.Out.Write(value);
            }
        }
    }
    /// <summary>
    /// A class which provides a sink for logging messages to the console.
    /// </summary>
    public class ConsoleSink : TextSink
    {
        private ConsoleWriter writer;
        /// <summary>
        /// Constructs an instance of the ConsoleSink class.
        /// </summary>
        /// <param name="name">The name of the sink.</param>
        public ConsoleSink(string name)
            : base(name)
        {
            ConsoleColor color = ConsoleColor.White;
            bool realTime = true;
            switch (name.ToLower())
            {
                case "redconsole": color = ConsoleColor.Red; break;
                case "greenconsole": color = ConsoleColor.Green; break;
                case "whiteconsole": color = ConsoleColor.White; break;
                case "yellowconsole": color = ConsoleColor.Yellow; break;
                case "commandlineconsole": color = ConsoleColor.White; break;
                default:
                    realTime = false;
                    break;

            }

            writer = new ConsoleWriter(realTime);
            writer.Color = color;
        }

        /// <summary>
        /// Implements <see cref="TextSink.Writer"/>
        /// </summary>
        protected override TextWriter Writer
        {
            get
            {
                return writer;
            }
        }
    }

    /// <summary>
    /// A class which provides a log sink for logging messages to a plain-text file.
    /// </summary>
    public class PlainTextSink : TextSink
    {
        StreamWriter writer;

        /// <summary>
        /// Constructs an instance of PlainTextSink class.
        /// </summary>
        /// <param name="name">The name of the sink.</param>
        /// <param name="logFilename">The log file name which messages should be logged to.</param>
        public PlainTextSink(string name, string logFilename)
            : base(name)
        {
            writer = new StreamWriter(logFilename);
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
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            // Should not access managed resouses if Dispose is called from the finalizer
            // when disposing is true.
            if (disposing && writer != null)
            {
                writer.Close();
                writer = null;
            }
        }

        /// <summary>
        /// Flushes the writer.
        /// </summary>
        public override void Flush()
        {
            if (writer != null)
                writer.Flush();
        }

        /// <summary>
        /// Gets the writer.
        /// </summary>
        protected override TextWriter Writer
        {
            get { return writer; }
        }
    }

    /// <summary>
    /// A class which provides a sink for logging messages to xml.
    /// </summary>
    public class XmlTextSink : XmlSink
    {
        XmlWriter writer;
        string logFilename;

        /// <summary>
        /// Constructs an instance of XmlTextSink class.
        /// </summary>
        /// <param name="name">The name of the sink.</param>
        /// <param name="logFilename">The log filename which messages should be logged to.</param>
        public XmlTextSink(string name, string logFilename)
            : base(name)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = ("    ");
            settings.NewLineChars = "\r\n";
            settings.CloseOutput = true;
            writer = XmlWriter.Create(logFilename, settings);
            this.logFilename = logFilename;
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
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            writer = null;
        }

        /// <summary>
        /// Gets the writer.
        /// </summary>
        protected override XmlWriter Writer
        {
            get { return writer; }
        }

        /// <summary>
        /// Gets the full name of the log file.
        /// </summary>
        internal string LogFileName
        {
            get
            {
                if (File.Exists(logFilename))
                {
                    return logFilename;
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Cannot find log file: {0}", logFilename));
                }
            }
        }
    }
}

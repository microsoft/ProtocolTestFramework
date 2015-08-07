// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Protocols.TestTools.Messages.Marshaling;
using Microsoft.Protocols.TestTools.Messages.Runtime.Marshaling;
using System.IO;

namespace Microsoft.Protocols.TestTools.Messages
{
    /// <summary>
    /// The wrapper of the Microsoft.Protocols.TestTools.Messages.Runtime.Marshaling.Marshaler
    /// which is backward compatible with old Marshaler.
    /// </summary>
    public class Marshaler : Microsoft.Protocols.TestTools.Messages.Runtime.Marshaling.Marshaler
    {
        ITestSite site;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">Test site</param>
        /// <param name="configuration">The marshaling configuration</param>
        public Marshaler(ITestSite site, MarshalingConfiguration configuration)
            : base(ExtensionHelper.GetRuntimeHost(site), configuration)
        {
            this.site = site;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">The marshaling configuration</param>
        public Marshaler(MarshalingConfiguration configuration)
            : base(configuration)
        {
        }

        /// <summary>
        /// Gets the associated test site.
        /// </summary>
        public ITestSite Site
        {
            get
            {
                return site;
            }
        }
    }

    /// <summary>
    /// The wrapper of the Microsoft.Protocols.TestTools.Messages.Runtime.Marshaling.MessageUtils
    /// which is backward compatible with old MessageUtils.
    /// </summary>
    public class MessageUtils : Microsoft.Protocols.TestTools.Messages.Runtime.MessageUtils
    {
        ITestSite site;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">The test site</param>
        public MessageUtils(ITestSite site)
            : base(ExtensionHelper.GetRuntimeHost(site))
        {
            this.site = site;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">The test site</param>
        /// <param name="config">The marshaling configuration</param>
        public MessageUtils(ITestSite site, MarshalingConfiguration config)
            : base(ExtensionHelper.GetRuntimeHost(site), config)
        {
            this.site = site;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">The test site</param>
        /// <param name="type">The message type</param>
        public MessageUtils(ITestSite site, MessageType type)
            : base(ExtensionHelper.GetRuntimeHost(site), type)
        {
            this.site = site;
        }

        /// <summary>
        /// Gets the associated test site.
        /// </summary>
        public ITestSite Site
        {
            get
            {
                return site;
            }
        }
    }

    /// <summary>
    /// The wrapper of the Microsoft.Protocols.TestTools.Messages.Runtime.Marshaling.Channel
    /// which is backward compatible with old Channel.
    /// </summary>
    public class Channel : Microsoft.Protocols.TestTools.Messages.Runtime.Channel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">The test site</param>
        /// <param name="stream">The data stream</param>
        public Channel(ITestSite site, Stream stream)
            : base(ExtensionHelper.GetRuntimeHost(site), stream)
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">The test site</param>
        /// <param name="stream">The data stream</param>
        /// <param name="marshalingConfig">The marshaling configuration</param>
        public Channel(ITestSite site, Stream stream, MarshalingConfiguration marshalingConfig)
            : base(ExtensionHelper.GetRuntimeHost(site), stream, marshalingConfig)
        { }
    }

    /// <summary>
    /// Derived Channel which will validate the value when reading or writing values
    /// </summary>
    public class ValidationChannel : Channel
    {
        private MessageUtils messageUtil;

        /// <summary>
        /// Constructs a typed stream which uses underlying stream and default marshaling configuration
        /// for block protocols.
        /// </summary>
        /// <param name="site">Test site</param>
        /// <param name="stream">The NetworkStream object.</param>
        public ValidationChannel(ITestSite site, Stream stream)
            : this(site, stream, BlockMarshalingConfiguration.Configuration)
        {
        }

        /// <summary>
        /// Constructs a channel which uses underlying stream and given marshaler configuration. 
        /// </summary>
        /// <param name="site">Test site</param>
        /// <param name="stream">The general stream object.</param>
        /// <param name="marshalingConfig">The marshaling configuration.</param>
        public ValidationChannel(ITestSite site, Stream stream, MarshalingConfiguration marshalingConfig)
            : base(site, stream, marshalingConfig)
        {
            messageUtil = new MessageUtils(site);
        }

        /// <summary>
        /// Reads a value of the given type T from the stream which uses the underlying marshaler to unmarshal it.
        /// And the value will be validated.
        /// </summary>
        /// <typeparam name="T">The type of the value to be read.</typeparam>
        /// <returns>The value read from the channel.</returns>
        public override T Read<T>()
        {
            T value = base.Read<T>();
            messageUtil.Validate(value);
            return value;
        }

        /// <summary>
        /// Writes a value of given type T to the stream which uses the underlying marshaler to marshal it.
        /// And the value will be validated.
        /// </summary>
        /// <typeparam name="T">The type of the value which is written to the stream.</typeparam>
        /// <param name="value">The value which is written to the stream.</param>
        public override void Write<T>(T value)
        {
            messageUtil.Validate(value);
            base.Write<T>(value);
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        /// <remarks>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If the parameter 'disposing' equals true, the method is called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If the parameter 'disposing' equals false, the method is called by the 
        /// runtime from the inside of the finalizer and you should not refer to 
        /// other objects. Therefore, only unmanaged resources can be disposed.
        /// </remarks>
        /// <param name="disposing">Indicates if Dispose is called by the user.</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (messageUtil != null)
                    {
                        messageUtil.Dispose();
                    }
                }
            }
            catch
            {
                throw new InvalidOperationException(
                    "Fail to dispose resources.");
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }

    /// <summary>
    /// The Event Queue
    /// </summary>
    public class EventQueue :
        Microsoft.Protocols.TestTools.Messages.Runtime.EventQueue
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">The test site</param>
        /// <param name="maxSize">maximum size of the queue</param>
        public EventQueue(ITestSite site, int maxSize)
            : base(ExtensionHelper.GetRuntimeHost(site), maxSize)
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">The test site</param>
        public EventQueue(ITestSite site) :
            base(ExtensionHelper.GetRuntimeHost(site))
        { }
    }
}
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Protocols.TestTools.Messages.Marshaling;
using System.IO;
using System.Net.Security;



namespace Microsoft.Protocols.TestTools.Messages
{

    /// <summary>
    /// An interface which TCP adapters should derive from.
    /// </summary>
    public interface ITcpAdapter : IAdapter
    {
        /// <summary>
        /// Establishes the connection to the configured endpoint.
        /// </summary>
        void Connect();
    }

    /// <summary>
    /// An abstract base class for TCP adapters.
    /// </summary>
    public abstract class TcpAdapterBase : ManagedAdapterBase, ITcpAdapter
    {
        string adapterName;

        TcpClient tcpClient;
        /// <summary>
        /// Gets the underlying TcpClient.
        /// </summary>
        public virtual TcpClient TcpClient
        {
            get
            {
                if (tcpClient == null)
                {
                    tcpClient = new TcpClient(NetworkAddressFamily);
                    tcpClient.NoDelay = true;
                }
                return tcpClient;
            }
        }

        /// <summary>
        /// Gets the end point which is used for TcpClient connection.
        /// </summary>
        public virtual IPEndPoint IPEndPoint
        {
            get
            {
                IPEndPoint endPoint = null;
                string hostName = GetRequiredProperty(HostPropertyName);
                int port = GetRequiredIntProperty(PortPropertyName);
                try
                {
                    foreach (IPAddress ipAddress in Dns.GetHostAddresses(hostName))
                    {
                        if (ipAddress.AddressFamily == this.NetworkAddressFamily)
                        {
                            endPoint = new IPEndPoint(ipAddress, port);
                            break;
                        }
                    }
                }
                catch (ArgumentOutOfRangeException e)
                {
                    Site.Assume.Fail(e.Message);
                }
                catch (SocketException e)
                {
                    Site.Assume.Fail(e.Message);
                }
                if (endPoint == null)
                {
                    throw new InvalidOperationException(
                        "Cannot get the host address for the configured address family.");
                }

                return endPoint;
            }
        }

        
        /// <summary>
        /// Gets the underlying NetworkStream. The property can be overrode.
        /// If visit the NetworkStream before connection or after close, 
        /// null value will be returned.
        /// </summary>
        public virtual NetworkStream NetworkStream
        {
            get
            {
                if (TcpClient == null || !TcpClient.Connected)
                {
                    return null;
                }
                return TcpClient.GetStream();
            }
        }

        /// <summary>
        /// Gets the underlying Stream. The property can be overrided.
        /// If visit the Stream before connection or after close, 
        /// null value will be returned.
        /// </summary>
        public virtual Stream Stream
        {
            get
            {
                return NetworkStream;
            }
        }

        Channel channel;
        MarshalingConfiguration marshalingConfig;
        Thread listener;

        /// <summary>
        /// Constructs a TCP adapter. 
        /// The parameter adapterName is used for constructing default PTF property names in the configuration. 
        /// </summary>
        /// <param name="adapterName">The adapter name</param>
        protected TcpAdapterBase(string adapterName)
        {
            this.adapterName = adapterName;
            this.marshalingConfig = BlockMarshalingConfiguration.Configuration;
        }

        /// <summary>
        /// Constructs a TCP adapter with given marshaling configuration. 
        /// The parameter adapterName is used for constructing default PTF property names in the configuration. 
        /// </summary>
        /// <param name="adapterName">The adapter name</param>
        /// <param name="config">The marshaling configuration</param>
        protected TcpAdapterBase(string adapterName, MarshalingConfiguration config)
        {
            this.adapterName = adapterName;
            this.marshalingConfig = config;
        }

        /// <summary>
        /// Gets the PTF property name to be used for looking up the hostname of the TCP connection.
        /// The default format is adapterName.hostname.
        /// </summary>
        public virtual string HostPropertyName
        {
            get
            {
                return String.Format("{0}.hostname", adapterName);
            }
        }

        /// <summary>
        /// Gets the PTF property name to be used for looking up the port of the TCP connection.
        /// The default format is adapterName.port.
        /// </summary>
        public virtual string PortPropertyName
        {
            get
            {
                return String.Format("{0}.port", adapterName);
            }
        }

        /// <summary>
        /// Gets the PTF property name to be used for looking up the address family of the UDP connection.
        /// The default format is adapterName.addressfamily.
        /// </summary>
        public virtual string AddressFamilyPropertyName
        {
            get
            {
                return String.Format("{0}.addressfamily", adapterName);
            }
        }
       
        /// <summary>
        /// Initializes the adapter.
        /// This method does not include setting up a connection.
        /// </summary>
        /// <param name="testSite">The test site</param>
        public override void Initialize(ITestSite testSite)
        {
            base.Initialize(testSite);
        }

        /// <summary>
        /// Resets the adapter.
        /// This method closes opened connection, if any. 
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            CloseConnection();
        }

        /// <summary>
        /// Disposes the adapter.
        /// This method closes opened connection, if any.
        /// </summary>
        /// <param name="disposing">Indicates whether Dispose is called by user</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!IsDisposed)
                {
                    if (disposing)
                    {
                        CloseConnection();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Gets the Channel which is associated with this TcpAdapterBase.
        /// The Channel is used for reading and writing to the TCP connection.
        /// The Channel property is null if no connection is established.
        /// </summary>
        public virtual Channel Channel
        {
            get 
            {
                if (channel == null && Stream != null)
                {
                    if (DisableValidation)
                    {
                        channel = new Channel(Site, Stream, this.marshalingConfig);
                    }
                    else
                    {
                        channel = new ValidationChannel(Site, Stream, this.marshalingConfig);
                    }
                }
                return channel; 
            }
        }

     
        /// <summary>
        /// A virtual method which implements reading from the channel. 
        /// This method is executed in an asynchronous thread within TcpAdapterBase
        /// when a connection is established. It should not return until receiving
        /// finished. This method should only use Checkers of ITestSite to report errors.
        /// </summary>
        protected virtual void ReceiveLoop()
        {
        }

        /// <summary>
        /// Invokes ReceiveLoop() and processes the exceptions thrown by the method ReceiveLoop().
        /// </summary>
        // The following suppression is adopted because any exception thrown in ReceiveLoop() must be caught by TcpAdapterBase.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal void InternalReceive()
        {
            try
            {
                ReceiveLoop();
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                ExtensionHelper.HandleVSException(e, Site);
                string msg = e != null ? e.ToString() : "unexpected internal error";
                Site.Assert.Fail("unexpected exception in receive loop: {0}", msg);
            }
        }
        
        /// <summary>
        /// Closes the connection and releases all resources.
        /// This method releases the resources including Thread, Channel, Stream and TcpClient orderly.
        /// The method can be overrided, and users should close Stream, NetworkStream before
        /// closing TcpClient in the overrided CloseConnection method.
        /// </summary>
        protected virtual void CloseConnection()
        {
            try
            {
                //ignore the thread abort exception
                //and release all resourcese.
                if (listener != null)
                {
                    listener.Abort();
                }
            }
            finally
            {
                listener = null;
                if (Channel != null)
                {
                    Channel.Dispose();
                }
                if (channel != null)
                {
                    channel.Dispose();
                    channel = null;
                }
                if (Stream != null)
                {
                    Stream.Close();
                }
                if (TcpClient != null)
                {
                    TcpClient.Client.Close();
                    TcpClient.Close();
                }
                if (tcpClient != null)
                {
                    tcpClient.Close();
                    tcpClient = null;
                }
            }
        }

        /// <summary>
        /// Establishes the connection to the configured endpoint.  Including 
        /// connect TcpClient with spceified end point and start the receive loop
        /// in a stand-alone thread.
        /// This method must be called before any communication in the channel or
        /// before visit the underlying stream or network stream.
        /// </summary>
        public void Connect()
        {
            CloseConnection();
            try
            {
                TcpClient.Connect(IPEndPoint);
            }
            catch (ArgumentOutOfRangeException e)
            {
                Site.Assume.Fail(e.Message);
            }
            catch (SocketException e)
            {
                Site.Assume.Fail(e.Message);
            }

            listener = new Thread(InternalReceive);
            listener.Start();            
        }

        /// <summary>
        /// Returns the value of address family for this tcp adapter.
        /// </summary>
        AddressFamily NetworkAddressFamily
        {
            get
            {
                AddressFamily addressFamily = AddressFamily.InterNetwork;

                string addressFamilyName = Site.Properties[AddressFamilyPropertyName];
                if (!string.IsNullOrEmpty(addressFamilyName))
                {
                    try
                    {
                        addressFamily = (AddressFamily)Enum.Parse(typeof(AddressFamily), addressFamilyName);
                    }
                    catch (ArgumentException)
                    {
                        throw new InvalidOperationException(
                            String.Format("The address family '{0}' in PTF config is invalid.", addressFamilyName));
                    }
                }

                return addressFamily;
            }
        }

        /// <summary>
        /// Returns the String value of required property.
        /// </summary>
        /// <param name="name">The property name</param>
        /// <returns>The string value of the property</returns>
        string GetRequiredProperty(string name)
        {
            string value = Site.Properties[name];
            if (value == null)
                throw new InvalidOperationException(
                    String.Format("Required PTF property '{0}' undefined",
                                                    name));
            return value;
        }

        /// <summary>
        /// Returns the value to indicate whether the validation is disabled.
        /// </summary>
        bool DisableValidation
        {
            get
            {
                if (Site == null)
                {
                    return false;
                }
                bool disableValidation = false;
                string disableValidationName = "disablevalidation";
                string disableValidationValue = Site.Properties[disableValidationName];
                if (!string.IsNullOrEmpty(disableValidationValue))
                {
                    try
                    {
                        disableValidationValue = disableValidationValue.Trim().ToLower();
                        disableValidation = bool.Parse(disableValidationValue);
                    }
                    catch (ArgumentException)
                    {
                        throw new InvalidOperationException(
                            String.Format("The property '{0}' in PTF config is invalid.", disableValidationName));
                    }
                }

                return disableValidation;
            }
        }

        /// <summary>
        /// Returns the integer value of required property
        /// </summary>
        /// <param name="name">The property name</param>
        /// <returns>The integer value of the property</returns>
        int GetRequiredIntProperty(string name)
        {
            string value = GetRequiredProperty(name);
            try
            {
                return Int32.Parse(value);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException(
                    String.Format("Required PTF property'{0}' not assigned to a valid number",
                                name));
            }
        }
    }

}
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// A class which simulating some kind of network event.
    /// (Should be used with MSProtocolTestSuiteNetworkEventSimulator filter)
    /// </summary>
    public static class NetworkEventSimulator
    {
        /// <summary>
        /// Port of command packet
        /// </summary>
        private const int nesPort = 0x8888;

        /// <summary>
        /// Uses UDP as transport
        /// </summary>
        private static UdpClient udpClient = null;

        /// <summary>
        /// Broadcasts the command packet
        /// </summary>
        private static string broadcastHostname = IPAddress.Broadcast.ToString();

        /// <summary>
        /// Search the local unicast IP according to the MAC
        /// </summary>
        /// <param name="macAddress">Local MAC address broadcasting the command packet</param>
        /// <returns>Unicast IP binding to this MAC</returns>
        private static IPAddress GetIPAddress4FromMACAddress(PhysicalAddress macAddress)
        {
            if (macAddress == null)
                return null;

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            IPAddress address = null;
            foreach (NetworkInterface adapter in adapters)
            {
                if (macAddress.Equals(adapter.GetPhysicalAddress()))
                {
                    IPInterfaceProperties ipProperties = adapter.GetIPProperties();
                    foreach (IPAddressInformation addressInfo in ipProperties.UnicastAddresses)
                    {
                        if (addressInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            address = addressInfo.Address;
                            break;
                        }
                    }
                    break;
                }
            }

            return address;
        }


        /// <summary>
        /// Binds UDP client to a specific IP adderss
        /// </summary>
        /// <param name="ipAddress">IP address to bind</param>
        private static void BindLocalIP(IPAddress ipAddress)
        {
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 0);
            if (udpClient == null)
            {
                udpClient = new UdpClient(ipEndPoint);
            }
            else
            {
                if (((IPEndPoint)udpClient.Client.LocalEndPoint).Address != ipEndPoint.Address)
                {
                    udpClient.Close();
                    udpClient = new UdpClient(ipEndPoint);
                }
            }
        }

        /// <summary>
        /// Sends command packet to control the MSProtocolTestSuiteNetworkEventSimulator filter
        /// </summary>
        /// <param name="sCommand">Flag to specify the command type</param>
        /// <param name="localMacAddress">The MAC address used to specify the local NIC used to send the command packet</param>
        /// <param name="remoteMacAddress">The MAC address specifying the target NIC</param>
        /// <returns>Flag to specify whether it has sent the command packet successfully</returns>
        private static bool SendCommand(SimulatorCommand sCommand, PhysicalAddress localMacAddress, PhysicalAddress remoteMacAddress = null)
        {
            IPAddress ipv4Address = GetIPAddress4FromMACAddress(localMacAddress);
            if (ipv4Address == null)
                return false;
            BindLocalIP(ipv4Address);

            byte[] buffer = SimulatorCommandBuilder.GetCommandPacket(sCommand, remoteMacAddress);

            if (buffer == null)
                return false;
            udpClient.Send(buffer, buffer.Length, broadcastHostname, nesPort);

            return true;
        }

        /// <summary>
        /// Loses all the connection binding to the local MAC. That means all packets sent and received by this NIC will be dropped.
        /// </summary>
        /// <param name="localMacAddress">The MAC address specifying the target NIC</param>
        /// <returns>Flag to specify whether it has sent the command packet successfully</returns>
        public static bool LoseLocalConnection(PhysicalAddress localMacAddress)
        {
            return SendCommand(SimulatorCommand.LoseLocalConnection, localMacAddress);
        }

        /// <summary>
        /// Restores all the connection binding to the local MAC.
        /// </summary>
        /// <param name="localMacAddress">The MAC address used to specify the target NIC</param>
        /// <returns>Flag to specify whether it has sent the command packet successfully</returns>
        public static bool RestoreLocalConnection(PhysicalAddress localMacAddress)
        {
            return SendCommand(SimulatorCommand.RestoreLocalConnection, localMacAddress);
        }

        /// <summary>
        /// Loses all the connection binding to a remote MAC. That means all packets sent and received by the NIC will be dropped.
        /// (You should know that which local NIC connected with the remote one)
        /// </summary>
        /// <param name="remoteMacAddress">The MAC address used to specify the target NIC</param>
        /// <param name="localMacAddress">The MAC address used to specify the local NIC used to send the command packet</param>
        /// <returns>Flag to specify whether it has sent the command packet successfully</returns>
        public static bool LoseRemoteConnection(PhysicalAddress remoteMacAddress, PhysicalAddress localMacAddress)
        {
            return SendCommand(SimulatorCommand.LoseRemoteConnection, localMacAddress, remoteMacAddress);
        }

        /// <summary>
        /// Restores all the connection binding to a remote MAC.
        /// (You should know that which local NIC connected with the remote one)
        /// </summary>
        /// <param name="remoteMacAddress">The MAC address used to specify the target NIC</param>
        /// <param name="localMacAddress">The MAC address used to specify the local NIC used to send the command packet</param>
        /// <returns>Flag to specify whether it has sent the command packet successfully</returns>
        public static bool RestoreRemoteConnection(PhysicalAddress remoteMacAddress, PhysicalAddress localMacAddress)
        {
            return SendCommand(SimulatorCommand.RestoreRemoteConnection, localMacAddress, remoteMacAddress);
        }

        //All the followings are for loca used only
        //Enumation of each command
        internal enum SimulatorCommand
        {
            LoseLocalConnection = 0,
            RestoreLocalConnection = 1,
            LoseRemoteConnection = 2,
            RestoreRemoteConnection = 3
        }

        //Class used to build the command packet
        //The packet format is defined as following
        //typedef struct _NES_PACKET
        //{
        //    unsigned char guid[16];
        //    unsigned char fillValue;
        //    unsigned char cmd : 4;
        //    unsigned char loc : 4;
        //    unsigned char macAddress[6];
        //}NES_PACKET, * PNES_PACKET;
        internal static class SimulatorCommandBuilder
        {
            private enum NESNICLocation
            {
                Local = 0x01,
                Remote = 0x02
            }

            private enum NESCommand
            {
                Restore = 0x00,
                Lose = 0x01
            }

            //GUID used for the command packet
            private static readonly byte[] guid = { 0x0A, 0xDB, 0x85, 0x75, 0xB4, 0x0C, 0x4A, 0x38, 0x89, 0x76, 0x70, 0x67, 0x7D, 0xDE, 0x03, 0xF7 };
 
            //Fill one byte after guid
            private const byte paddingByte = 0x00;

            //If the command packet is to control the local NIC, the MAC field in the packet should be all 0;
            private static readonly byte[] localUsedMac = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            //Combine the 4 low bits of two bytes to one byte
            private static byte BuildCommandByte(byte high, byte low)
            {
                return (byte)(((high & 0x0F) << 4) | (low & 0x0F));
            }

            //Build the command packet in bytes according to the value of each field
            public static byte[] GetCommandPacket(SimulatorCommand sCommand, PhysicalAddress destMAC = null)
            {
                //If the command packet is for local NIC, we should set the MAC field to all 0
                //If the command packet is for remote NIC, we should set the MAC field to remote MAC address 
                byte cmdByte = 0x00;
                byte[] macInBytes = null;
                switch (sCommand)
                {
                    case SimulatorCommand.LoseLocalConnection:
                        cmdByte = BuildCommandByte((byte)NESNICLocation.Local, (byte)NESCommand.Lose);
                        macInBytes = localUsedMac;
                        break;
                    case SimulatorCommand.RestoreLocalConnection:
                        cmdByte = BuildCommandByte((byte)NESNICLocation.Local, (byte)NESCommand.Restore);
                        macInBytes = localUsedMac;
                        break;
                    case SimulatorCommand.LoseRemoteConnection:
                        cmdByte = BuildCommandByte((byte)NESNICLocation.Remote, (byte)NESCommand.Lose);
                        if (destMAC != null)
                            macInBytes = destMAC.GetAddressBytes();
                        break;
                    case SimulatorCommand.RestoreRemoteConnection:
                        cmdByte = BuildCommandByte((byte)NESNICLocation.Remote, (byte)NESCommand.Restore);
                        if (destMAC != null)
                            macInBytes = destMAC.GetAddressBytes();
                        break;
                    default:
                        break;
                }
                if (cmdByte == paddingByte || macInBytes == null)
                    return null;

                int packetLength = guid.Length + 2 + macInBytes.Length; //The Length of Command Packet = GUID(16) + paddingByte(1) + commandByte(1) + MAC(1)
                int curIdx = 0;
                byte[] res = new byte[packetLength];

                Array.Copy(guid, res, guid.Length);
                curIdx = guid.Length;
                res[curIdx++] = paddingByte;
                res[curIdx++] = cmdByte;
                Array.Copy(macInBytes, 0, res, curIdx, macInBytes.Length);
                curIdx += macInBytes.Length;

                return res;
            }
        }
    }
}
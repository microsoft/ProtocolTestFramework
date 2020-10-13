// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.Protocols.TestTools;

namespace Microsoft.Protocols.TestSuites.XXXX.Adapter
{
    /// <summary>
    /// Implements the XXXX protocol adapter
    /// </summary>
    public class XXXX_Adapter : ManagedAdapterBase, IXXXX_Adapter
    {
        /// <summary>
        /// Resets all the states of the adapter
        /// </summary>
        public override void Reset()
        {
        }

        /// <summary>
        /// Sends a request to SUT
        /// </summary>
        /// <param name="SUTIPAddress">Indicats IP address of SUT</param>
        /// <returns>Indicates if the request is sent succesfully</returns>
        public bool SendRequest(string SUTIPAddress)
        {
            // Add code here to construct a requst message and then send it to SUT
            // It's useful when the message is encrypted.
            return true;
        }

        /// <summary>
        /// Waits for a resonse from SUT
        /// </summary>
        /// <param name="status">Indicates the status in the response</param>
        /// <param name="timeout">Indicates the timeout in seconds when waiting for the response</param>
        /// <returns>Indicates if the response is received successfully</returns>
        public bool WaitForResponse(out int status, int timeout)
        {
            // Add code here to receive data from SUT, and parse the data to a message structure
            // It's useful when the message is encrypted.
            status = 0;

            // It's useful when the message is encrypted.
            return true;
        }
    }
}

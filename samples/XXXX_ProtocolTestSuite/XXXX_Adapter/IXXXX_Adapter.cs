// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.Protocols.TestTools;

namespace Microsoft.Protocols.TestSuites.XXXX.Adapter
{
    /// <summary>
    /// Defines the interface of the XXXX protocol adapter
    /// </summary>
    public interface IXXXX_Adapter: IAdapter
    {
        /// <summary>
        /// Sends a request to SUT
        /// </summary>
        /// <param name="SUTIPAddress">Indicats IP address of SUT</param>
        /// <returns>Indicates if the request is sent succesfully</returns>
        bool SendRequest(string SUTIPAddress);

        /// <summary>
        /// Waits for a resonse from SUT
        /// </summary>
        /// <param name="status">Indicates the status in the response</param>
        /// <param name="timeout">Indicates the timeout in seconds when waiting for the response</param>
        /// <returns>Indicates if the response is received successfully</returns>
        bool WaitForResponse(out int status, int timeout);
    }
}
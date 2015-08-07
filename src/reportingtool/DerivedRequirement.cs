// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.ReportingTool
{
    internal enum DerivedType
    {
        Unknown = 0,
        Inferred,
        Partial,
        Cases
    }

    internal enum CoveredStatus
    {
        Unverified = 0,
        Partial,
        Verified
    }

    /// <summary>
    /// A structure represent the current derived requirement.
    /// use this structure to build a graph of derived requirements.
    /// </summary>
    internal struct DerivedRequirement
    {
        private string id;
        private List<string> originalReqs;
        private Dictionary<string, DerivedType> derivedReqs;
        private CoveredStatus status;
        private string timeStamp;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="id">The requirement ID.</param>
        /// <param name="status">The requirement covered status.</param>
        public DerivedRequirement(string id, CoveredStatus status)
        {
            this.id = id;
            this.status = status;
            this.timeStamp = string.Empty;
            originalReqs = new List<string>();
            derivedReqs = new Dictionary<string, DerivedType>();
        }

        /// <summary>
        /// Add parent requirement.
        /// </summary>
        /// <param name="reqID">The requirement ID.</param>
        public void AddOriginalRequirement(string reqID)
        {
            if (!originalReqs.Contains(reqID))
            {
                originalReqs.Add(reqID);
            }
            else
            {
                if (ReportingLog.Log != null)
                {
                    ReportingLog.Log.TraceWarning(
                        string.Format("Found duplicate original requirement {0} in requirement {1}.", reqID, id));
                }
            }
        }

        /// <summary>
        /// Remove the relationship of the requirements
        /// </summary>
        /// <param name="reqId">The target requirment ID</param>
        public void RemoveOriginalRequirement(string reqId)
        {
            if (originalReqs.Contains(reqId))
            {
                originalReqs.Remove(reqId);
            }
        }

        /// <summary>
        /// Add child requirement.
        /// </summary>
        /// <param name="reqID">The requirement ID.</param>
        /// <param name="type">The derived requirement type.</param>
        public void AddDerivedRequirement(string reqID, DerivedType type)
        {
            //self loop
            if (reqID == this.id)
            {
                throw new InvalidOperationException("Found loop in the derived requirements: " + reqID + " is derived from itself.");
            }
            if (!derivedReqs.ContainsKey(reqID))
            {
                derivedReqs.Add(reqID, type);
            }
            else
            {
                if (ReportingLog.Log != null)
                {
                    ReportingLog.Log.TraceWarning(
                        string.Format("Found duplicate derived requirement {0} in requirement {1}.", reqID, id));
                }
                derivedReqs[reqID] = type;
            }
        }

        /// <summary>
        /// remove relationship of the requirements
        /// </summary>
        /// <param name="reqID">The target requirement ID</param>
        public void RemoveDerivedRequirement(string reqID)
        {
            if (derivedReqs.ContainsKey(reqID))
            {
                derivedReqs.Remove(reqID);
            }
        }

        /// <summary>
        /// Gets the ID of current derived requirement.
        /// </summary>
        public string ReqID
        {
            get
            {
                return id;
            }
        }

        /// <summary>
        /// Gets or Sets the covered status of current derived requirement.
        /// </summary>
        public CoveredStatus CoveredStatus
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
            }
        }

        /// <summary>
        /// Gets the parent requirements.
        /// </summary>
        public List<string> OriginalReqs
        {
            get
            {
                return originalReqs;
            }
        }

        /// <summary>
        /// Gets all the direct or undirect derived requirements.
        /// </summary>
        public Dictionary<string, DerivedType> DerivedReqs
        {
            get
            {
                return derivedReqs;
            }
        }

        /// <summary>
        /// Gets the count of case type requirements derived from the current requirement.
        /// </summary>
        public uint CaseCount
        {
            get
            {
                uint count = 0;
                foreach (KeyValuePair<string, DerivedType> kvp in derivedReqs)
                {
                    if (kvp.Value == DerivedType.Cases)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Gets or sets the time stamp string for the current requirement.
        /// </summary>
        public string TimeStamp
        {
            get
            {
                if (string.IsNullOrEmpty(timeStamp))
                {
                    return null;
                }
                return timeStamp;
            }
            set
            {
                timeStamp = value;
            }
        }
    }
}

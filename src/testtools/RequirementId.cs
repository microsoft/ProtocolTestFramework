// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// A class which creates the unified message for captured requirements.
    /// This class can be used from modeling code as well as from test suite code. 
    /// </summary>
    public static class RequirementId
    {
        private static Dictionary<string, string> dict = new Dictionary<string, string>();

        /// <summary>
        /// Makes a requirement entry, inserts into the requirement table if necessary,
        /// and returns the requirement ID.
        /// </summary>
        /// <remarks>
        /// The decription of requirement string must be unique. If more than descriptions are specified for
        /// a requirement, an InvalidOperationException will be raised.
        /// </remarks>
        /// <param name="docShortName">The requirement specification document short name.</param>
        /// <param name="number">The requirement number.</param>
        /// <param name="description">The requirement description string.</param>
        /// <returns>The requirement id. Format &lt;docShortName&gt;_R&lt;number&gt;.</returns>
        public static string Make(string docShortName, int number, string description)
        {
            if (String.IsNullOrEmpty(description) || 
                String.IsNullOrEmpty(description.Trim()))
            {
                throw new ArgumentException("description can't be null or empty", "description");
            }
            string reqId = MakeId(docShortName, number);

            if (!dict.ContainsKey(reqId))
            {
                dict.Add(reqId, description);
            }
            else
            {
                if (dict[reqId] != description)
                    throw new InvalidOperationException(
                        String.Format(
                                "Trying to override existed description with different new one.\r\nexisted description:{0}\r\nnew description:{1}",
                                dict[reqId],
                                description)
                        );
            }

            return reqId;
        }

        /// <summary>
        /// Empties the requirement table.
        /// </summary>
        public static void ClearRequirementTable()
        {
            dict.Clear();
        }

        private static string MakeId(string docShortName, int number)
        {
            if (String.IsNullOrEmpty(docShortName) ||
                String.IsNullOrEmpty(docShortName.Trim()))
            {
                throw new ArgumentException("docShortName can't be null or empty", "docShortName");
            }
            if (number < 0)
            {
                throw new ArgumentException("number should be non-negative.", "number");
            }
            string reqId = docShortName + "_R" + number;
            return reqId;
        }

        /// <summary>
        /// Gets the corresponding description by docShortName and number.
        /// </summary>
        /// <param name="docShortName">Document short name, eg. MS-KILE</param>
        /// <param name="number">Number in doc, eg. 24</param>
        /// <returns>The description string, null if not found.</returns>
        internal static string GetDescription(string docShortName, int number)
        {
            return GetDescription(MakeId(docShortName, number));
        }

        /// <summary>
        /// Gets the corresponding description by Requirement Id.
        /// </summary>
        /// <param name="reqId">Requirement id, eg. MS-KILE_24</param>
        /// <returns>The description string, null if not found.</returns>
        internal static string GetDescription(string reqId)
        {
            if (String.IsNullOrEmpty(reqId))
            {
                throw new ArgumentException("reqId can't be null or empty", "reqId");
            }
            string description;
            if (dict.TryGetValue(reqId, out description))
            {
                return description;
            }
            else
            {
                return null;
            }
        }

        internal static bool IsEmpty()
        {
            return (dict.Count == 0);
        }

        internal static IEnumerable<KeyValuePair<string, string>> Entries
        {
            get
            {
                foreach (KeyValuePair<string, string> kvp in dict)
                {
                    yield return kvp;
                }
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Collections;

namespace Microsoft.Protocols.TestTools.Logging
{
    /// <summary>
    /// Provides a view of log profiles which defined in a log configuration.
    /// </summary>
    public class LogProfile
    {
        /// <summary>
        /// A dictionary contains all the defined sinks.
        /// </summary>
        private Dictionary<string, LogSink> allSinks = new Dictionary<string, LogSink>();

        /// <summary>
        /// Retrieves all defined sink instances.
        /// </summary>
        public Dictionary<string, LogSink>.ValueCollection AllSinks
        {
            get { return allSinks.Values; }
        }

        /// <summary>
        /// Dictionary(profile name, Dictionary(sink name, List(LogEntryKind)))
        /// </summary>
        private Dictionary<string, Dictionary<string, List<LogEntryKind>>> profilesMap = new Dictionary<string, Dictionary<string, List<LogEntryKind>>>();

        /// <summary>
        /// Gets a table which represents profile entries.
        /// </summary>
        public Dictionary<string, Dictionary<string, List<LogEntryKind>>> ProfilesMap
        {
            get { return profilesMap; }
        }
        
        /// <summary>
        /// Initializes a new instance of LogProfile.
        /// </summary>
        public LogProfile()
        {
        }

        /// <summary>
        /// Indicates whether a sink with given name has been defined
        /// </summary>
        /// <param name="logSinkName">The name of the sink.</param>
        /// <returns>true indicates a sink is defined; otherwise, false</returns>
        public bool IsSinkExist(string logSinkName)
        {
            return allSinks.ContainsKey(logSinkName);
        }

        /// <summary>
        /// Adds a named log sink to the current logging profiles context.
        /// </summary>
        /// <param name="logSinkName">The name of the sink</param>
        /// <param name="sink">An instance of LogSink.</param>
        public void AddSink(string logSinkName, LogSink sink)
        {
            if (allSinks.ContainsKey(logSinkName))
            {
                throw new InvalidOperationException(String.Format("The log sink {0} already exists.", logSinkName));
            }

            allSinks.Add(logSinkName, sink);
        }

        /// <summary>
        /// Removes an entry from the specified logging profile.
        /// </summary>
        /// <param name="profileName">Logging profile name.</param>
        /// <param name="kind">The kind of the log entry.</param>
        /// <param name="sinkName">The name of the sink.</param>
        public void RemoveProfileEntry(string profileName, LogEntryKind kind, string sinkName)
        {
            // Verify if the name of the sink is valid.
            if (!allSinks.ContainsKey(sinkName))
            {
                throw new ArgumentException("The log sink doesn't exist.", "sinkName");
            }

            // Verify if the name of the profile exists.
            if (!profilesMap.ContainsKey(profileName))
            {
                throw new ArgumentException(String.Format("The profile \"{0}\" which you try to delete the rule from is not defined.", profileName), "profileName");
            }

            if (!profilesMap[profileName].ContainsKey(sinkName))
            {
                return;
            }

            if (!profilesMap[profileName][sinkName].Contains(kind))
            {
                return;
            }

            // Ignore the result
            profilesMap[profileName][sinkName].Remove(kind);
        }

        /// <summary>
        /// Adds an entry to the specified logging profile.
        /// </summary>
        /// <param name="profileName">Logging profile name.</param>
        /// <param name="kind">The kind of the log entry.</param>
        /// <param name="sinkName">The name of the sink.</param>
        public void AddProfileEntry(string profileName, LogEntryKind kind, string sinkName)
        {
            // Verify if the name of the sink is valid.
            if (!allSinks.ContainsKey(sinkName))
            {
                throw new ArgumentException("The log sink doesn't exist.", "sinkName");
            }
            
            if (!profilesMap.ContainsKey(profileName))
            {
                throw new ArgumentException(String.Format("The profile \"{0}\" does not exist.", profileName), "profileName");
            }

            // Gets the profile entry
            Dictionary<string, List<LogEntryKind>> entry = profilesMap[profileName];

            // Adds a kinds list if doesn't exsit.
            List<LogEntryKind> logNeededKinds;
            if (entry.ContainsKey(sinkName))
            {
                logNeededKinds = entry[sinkName];
            }
            else
            {
                logNeededKinds = new List<LogEntryKind>();
                entry[sinkName] = logNeededKinds;
            }

            // Add a log-needed kind into the 
            if (!logNeededKinds.Contains(kind))
            {
                logNeededKinds.Add(kind);
            }
        }

        #region ILogProfile Members

        /// <summary>
        /// Gets all the log sink instances of a profile.
        /// </summary>
        /// <param name="profileName">The name of the profile.</param>
        /// <param name="kind">The log kind which the result sinks associated with.</param>
        /// <returns>A list which contains the log sinks.</returns>
        internal List<LogSink> GetSinksOfProfile(string profileName, LogEntryKind kind)
        {
            List<LogSink> sinks = new List<LogSink>();

            // Gets specified profile information.
            Dictionary<string, List<LogEntryKind>> profile = profilesMap[profileName];

            // Scans all sinks associated with the profile.
            foreach (string sinkName in profile.Keys)
            {
                // Retrieves the sink instance by name.
                LogSink sink = allSinks[sinkName];

                // Check if this log entry kind can be accepted by this sink.
                if (profile[sinkName].Contains(kind))
                {
                    sinks.Add(sink);
                }
            }

            return sinks;
        }

        /// <summary>
        /// Gets a list of active logging kinds.
        /// </summary>
        /// <param name="profileName">The log profile name.</param>
        /// <returns>A list which contains the log kindss.</returns>
        /// <remarks>An active kind means at least one sink is receiving logs with this kind.</remarks>
        internal List<LogEntryKind> GetActiveKindsOfProfile(string profileName)
        {
            // Verify profile name.
            if (!profilesMap.ContainsKey(profileName))
            {
                throw new InvalidOperationException(String.Format("The log profile '{0}' could not be found.", profileName));
            }

            List<LogEntryKind> activeKinds = new List<LogEntryKind>();

            Dictionary<string, List<LogEntryKind>> sinkToKindMap = profilesMap[profileName];

            // Scan kinds in a sink.
            foreach (List<LogEntryKind> kindsOfSink in sinkToKindMap.Values)
            {
                foreach (LogEntryKind kd in kindsOfSink)
                {
                    // Test if an active kind has been added.
                    if (!activeKinds.Contains(kd))
                    {
                        activeKinds.Add(kd);
                    }
                }
            }
            return activeKinds;
        }

        #endregion
    }
}

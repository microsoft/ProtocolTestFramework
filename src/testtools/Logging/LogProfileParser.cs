// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.IO;
using System.Collections.ObjectModel;

namespace Microsoft.Protocols.TestTools.Logging
{
    /// <summary>
    /// Parses the logging profile related information from the configuration file.
    /// </summary>
    sealed class LogProfileParser
    {
        private LogProfileParser()
        {
        }

        private LogProfile logProfile;

        private static string activeProfileName = String.Empty;

        /// <summary>
        /// Gets the active profile name of specified in the configuration.
        /// </summary>
        public static string ActiveProfileNameInConfig
        {
            get { return activeProfileName; }
        }

        /// <summary>
        /// Parses the configuration file and creates an instance of <see cref="LogProfile"/> .
        /// </summary>
        /// <returns>An instance of LogProfile.</returns>
        public static LogProfile CreateLogProfileFromConfig(IConfigurationData config, string testAssemblyName)
        {
            if (config == null)
            {
                return null;
            }

            // Create a temp instance of parser.
            LogProfileParser parser = new LogProfileParser();

            // Gets the active log profile name.
            if ((activeProfileName = config.DefaultProfile) == null)
            {
                throw new InvalidOperationException("The active profile name is not present.");
            }

            // Create LogProfile instance.
            parser.logProfile = new LogProfile();

            parser.ParseSinks(config.LogSinks, testAssemblyName);

            parser.ParseProfiles(config.Profiles);

            return parser.logProfile;
        }

        private static string CreateUniqueFileName(FileLogSinkConfig fileSink, string testAssemblyName)
        {
            string timeStampFormat = "{0:D4}-{1:D2}-{2:D2} {3:D2}_{4:D2}_{5:D2}_{6:D3}";

            if (testAssemblyName == null)
            {
                throw new ArgumentNullException("Test Assembly Name");
            }

            string uniqueName = string.Empty;
            string extension = string.Empty;
            if (0 == string.Compare("text", fileSink.Format, StringComparison.CurrentCultureIgnoreCase))
            {
                extension = ".txt";
            }
            else if (0 == string.Compare("xml", fileSink.Format, StringComparison.CurrentCultureIgnoreCase))
            {
                extension = ".xml";
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format("Unexpected file format for file sink {0}, the format for file sink can only be 'text' or 'xml'.", fileSink.Name));
            }

            //use the time stamp to make the file name unique.
            DateTime timeStamp = DateTime.Now;
            string timeStampInfo = string.Format(timeStampFormat,
                                                timeStamp.Year,
                                                timeStamp.Month,
                                                timeStamp.Day,
                                                timeStamp.Hour,
                                                timeStamp.Minute,
                                                timeStamp.Second,
                                                timeStamp.Millisecond);

            if (string.IsNullOrEmpty(fileSink.File))
            {
                uniqueName = "[" + testAssemblyName + "_" + fileSink.Name + "]" + timeStampInfo + extension;
                if (File.Exists(Path.Combine(fileSink.Directory, uniqueName)))
                {
                    throw new InvalidOperationException(
                        "File already exist: " + uniqueName);
                }
            }
            else
            {
                uniqueName = fileSink.File;
                if(File.Exists(Path.Combine(fileSink.Directory, uniqueName)))
                {
                    uniqueName = "[" + testAssemblyName + "_" + fileSink.Name + "]" + timeStampInfo + " " + fileSink.File;

                    if (File.Exists(Path.Combine(fileSink.Directory, uniqueName)))
                    {
                        throw new InvalidOperationException(
                            "File already exist: " + uniqueName);
                    }
                }
            }

            return uniqueName;
        }

        private void ParseSinks(Collection<LogSinkConfig> logSinks, string testAssemblyName)
        {
            // Build sinks and add them into the sinks collection.
            foreach (LogSinkConfig sink in logSinks)
            {
                bool sinkSupported = false;

                CustomLogSinkConfig customSink = sink as CustomLogSinkConfig;
                if (customSink != null)
                {
                    sinkSupported = true;
                    AddCustomSink(
                            customSink.Name,
                            customSink.Type);
                }

                FileLogSinkConfig fileSink = sink as FileLogSinkConfig;
                if (fileSink != null)
                {
                    sinkSupported = true;
                    AddFileSink(
                            fileSink.Name,
                            fileSink.Directory,
                            CreateUniqueFileName(fileSink, testAssemblyName),
                            fileSink.Format);
                }

                ConsoleLogSinkConfig consoleSink = sink as ConsoleLogSinkConfig;
                if (consoleSink != null)
                {
                    sinkSupported = true;
                    AddConsoleSink(
                            consoleSink.ID);
                }

                if (!sinkSupported)
                {
                    throw new InvalidOperationException("The specified sink type is not supported.");
                }
            }
        }

        private void ParseProfiles(Collection<ProfileConfig> profilesConfig)
        {
            foreach (ProfileConfig profile in profilesConfig)
            {
                // Get current profile name.
                string name = profile.Name;

                if (name == null)
                {
                    throw new InvalidOperationException("The name attribute of profile is not found.");
                }

                // Extends the current parsing profile if needed.
                AddOriginalProfileEntriesIfExtend(name, profile.BaseProfile);

                AddProfileIfNotExist(profile.Name);

                // Parse all entries of a profile.
                foreach (ProfileRuleConfig rule in profile.Rules)
                {

                    LogEntryKind kind = ToKind(rule.Kind);
                    string sink = rule.Sink;

                    // Add or remove a profile entry.
                    if (rule.Delete)
                    {
                        logProfile.RemoveProfileEntry(name, kind, rule.Sink);
                    }
                    else
                    {
                        logProfile.AddProfileEntry(name, kind, rule.Sink);
                    }

                }
            }
        }

        private static bool IsDeleteEntry(XmlNode entry)
        {
            XmlAttribute att = entry.Attributes["delete"];
            if (null == att)
            {
                // The attribute "delete" doesn't present.
                return false;
            }

            return TestToolHelpers.XmlBoolToBool(att.Value);
        }

        private void AddOriginalProfileEntriesIfExtend(
            string currentName,
            string orgProfileName)
        {
            // Extend the current profile if needed.
            if (orgProfileName != null)
            {
                Dictionary<string, Dictionary<string, List<LogEntryKind>>> profilesMap =
                    logProfile.ProfilesMap;

                // Check if the orginial profile exists.
                if (!profilesMap.ContainsKey(orgProfileName))
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "The profile {0} is not defined which the profile {1} extends.",
                            orgProfileName,
                            currentName));
                }

                // The current parsing profile should not exist.
                if (profilesMap.ContainsKey(currentName))
                {
                    throw new InvalidOperationException(
                        String.Format("The profile {0} already exist.", currentName));
                }
                else
                {
                    // Add a new entry for the current profile.
                    profilesMap[currentName] = new Dictionary<string, List<LogEntryKind>>();

                    // Enumerate all original sink entries and copy them to the current profile.
                    foreach (string sinkName in profilesMap[orgProfileName].Keys)
                    {
                        List<LogEntryKind> kinds = new List<LogEntryKind>();
                        kinds.AddRange(profilesMap[orgProfileName][sinkName]);
                        profilesMap[currentName].Add(sinkName, kinds);
                    }
                }
            }
        }

        private void AddProfileIfNotExist(string profileName)
        {
            if (!logProfile.ProfilesMap.ContainsKey(profileName))
            {
                logProfile.ProfilesMap[profileName] = new Dictionary<string, List<LogEntryKind>>();
            }
        }

        private static LogEntryKind ToKind(string kindString)
        {
            try
            {
                return (LogEntryKind)Enum.Parse(typeof(LogEntryKind), kindString);
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException(
                    String.Format("The log entry kind ({0}) could not be parsed.", kindString), e);
            }
        }

        private void AddFileSink(
            string name, 
            string directory, 
            string file, 
            string format)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (String.Compare(format, "text", true) == 0)
            {
                logProfile.AddSink(name, new PlainTextSink(name, Path.Combine(directory, file)));
            }

            if (string.Compare(format, "xml", true) == 0)
            {
                logProfile.AddSink(name, new XmlTextSink(name, Path.Combine(directory, file)));
            }
        }

        private void AddConsoleSink(string name)
        {
            logProfile.AddSink(name, new ConsoleSink(name));
        }

        private void AddCustomSink(
            string name, 
            string type)
        {
            // Create instance of custom type sink from a assembly.
            try
            {
                LogSink sink = (LogSink)TestToolHelpers.CreateInstanceFromTypeName(type, new object[] { name });
                if (sink == null)
                {
                    throw new InvalidOperationException(String.Format("Failed to create custom sink: {0}.", type));
                }
                logProfile.AddSink(name, sink);
            }
            catch (FileNotFoundException e)
            {
                throw new XmlException(
                    String.Format("The assembly of the custom sink ({0}) could not be found.", name), e);
            }
            catch (ArgumentException e)
            {
                throw new XmlException(
                    String.Format("The type of the custom sink ({0}) could not be found.", name), e);
            }
        }
    }
}

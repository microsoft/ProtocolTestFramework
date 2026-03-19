// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Minimal in-memory IConfigurationData stub for unit tests.
    /// </summary>
    internal class FakeConfigurationData : IConfigurationData
    {
        public Collection<LogSinkConfig> LogSinks { get; set; } = new Collection<LogSinkConfig>();
        public Collection<ProfileConfig> Profiles { get; set; } = new Collection<ProfileConfig>();
        public string DefaultProfile { get; set; } = "Default";
        public NameValueCollection Properties { get; set; } = new NameValueCollection();
        public AdapterConfig GetAdapterConfig(string adapterName) => null;

        /// <summary>
        /// Builds a config with a single XML file sink and the patch flag enabled.
        /// </summary>
        public static FakeConfigurationData WithPatch(string directory, string sinkFile, string profileName = "Default")
            => Build(directory, sinkFile, profileName, patchEnabled: true);

        /// <summary>
        /// Builds a config with a single XML file sink and the patch flag disabled.
        /// </summary>
        public static FakeConfigurationData WithoutPatch(string directory, string sinkFile, string profileName = "Default")
            => Build(directory, sinkFile, profileName, patchEnabled: false);

        private static FakeConfigurationData Build(string directory, string sinkFile, string profileName, bool patchEnabled)
        {
            var props = new NameValueCollection();
            if (patchEnabled)
                props["PTF.LogProfileParserPatch.Enabled"] = "true";

            string format = sinkFile.EndsWith(".xml", System.StringComparison.OrdinalIgnoreCase) ? "xml" : "text";

            var sinks = new Collection<LogSinkConfig>
            {
                new FileLogSinkConfig("TestSink", directory, sinkFile, format)
            };

            var rules = new Collection<ProfileRuleConfig>
            {
                new ProfileRuleConfig("Comment", "TestSink", false)
            };

            var profiles = new Collection<ProfileConfig>
            {
                new ProfileConfig(profileName, null, rules)
            };

            return new FakeConfigurationData
            {
                LogSinks = sinks,
                Profiles = profiles,
                DefaultProfile = profileName,
                Properties = props
            };
        }
    }
}

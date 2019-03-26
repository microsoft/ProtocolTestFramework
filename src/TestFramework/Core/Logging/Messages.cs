// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Protocols.TestTools.Logging
{
    /// <summary>
    /// A static class which contains the XML node names in the PTF configuration files.
    /// </summary>
    public static class Messages
    {
        // TODO: it seems dotnet core does not support l10n right now

        /// <summary>
        ///   Looks up a localized string similar to Expected: &lt;{0}&gt;, Actual: &lt;{1}&gt;. {2}.
        /// </summary>
        public const string AreEqualFailMsg = "Expected: <{0}>, Actual: <{1}>. {2}";

        /// <summary>
        ///   Looks up a localized string similar to Expected: &lt;{0}&gt;, Actual: &lt;{1}&gt;. {2}.
        /// </summary>
        public const string AreNotEqualFailMsg = "Expected: <{0}>, Actual: <{1}>. {2}";

        /// <summary>
        ///   Looks up a localized string similar to {0}.{1} failed. {2}.
        /// </summary>
        public const string CheckFailed = "{0}.{1} failed. {2}";

        /// <summary>
        ///   Looks up a localized string similar to {0}.{1} failed on requirement {2}. {3}.
        /// </summary>
        public const string CheckFailedOnReqId = "{0}.{1} failed on requirement {2}. {3}";

        /// <summary>
        ///   Looks up a localized string similar to {0}.{1} is inconclusive. {2}.
        /// </summary>
        public const string CheckInconclusive = "{0}.{1} is inconclusive. {2}";

        /// <summary>
        ///   Looks up a localized string similar to {0}.{1} succeeded. {2}.
        /// </summary>
        public const string CheckSucceeded = "{0}.{1} succeeded. {2}";

        /// <summary>
        ///   Looks up a localized string similar to The kind of message log entry must match..
        /// </summary>
        public const string EntryKindMissMatch = "The kind of message log entry must match.";

        /// <summary>
        ///   Looks up a localized string similar to entryType does not implement the ILogEntry interface.
        /// </summary>
        public const string EntryTypeLogFilterArguementMessage = "entryType does not implement the ILogEntry interface";

        /// <summary>
        ///   Looks up a localized string similar to Expected Type: &lt;{0}&gt;, Actual Type: &lt;{1}&gt;. {2}.
        /// </summary>
        public const string IsInstanceOfFailMsg = "Expected Type: <{0}>, Actual Type: <{1}>. {2}";

        /// <summary>
        ///   Looks up a localized string similar to Wrong Type: &lt;{0}&gt;, Actual Type: &lt;{1}&gt;. {2}.
        /// </summary>
        public const string IsNotInstanceOfFailMsg = "Wrong Type: <{0}>, Actual Type: <{1}>. {2}";
    }
}

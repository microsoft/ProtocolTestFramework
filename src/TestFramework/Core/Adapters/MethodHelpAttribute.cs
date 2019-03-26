// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Provides a helper attribute used by adapter methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class MethodHelpAttribute : Attribute
    {
        readonly string helpMessage;

        /// <summary>
        /// Disables the default constructor.
        /// </summary>
        private MethodHelpAttribute()
        {
        }

        /// <summary>
        /// Initializes the attribute with specified message.
        /// </summary>
        /// <param name="helpMessage">The helper message string.</param>
        public MethodHelpAttribute(string helpMessage)
        {
            this.helpMessage = helpMessage;
        }

        /// <summary>
        /// Gets the content of the helper message.
        /// </summary>
        public string HelpMessage
        {
            get
            {
                return this.helpMessage;
            }
        }
    }
}

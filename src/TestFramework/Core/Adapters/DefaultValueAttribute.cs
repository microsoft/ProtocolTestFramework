// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Provides a default return value attribute used by adapter methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class DefaultValueAttribute : Attribute
    {
        private readonly string defaultValue;
        /// <summary>
        /// Disables the default constructor.
        /// </summary>
        private DefaultValueAttribute()
        {
        }

        /// <summary>
        /// Initializes the attribute with specified default value.
        /// </summary>
        /// <param name="defaultValue">The default value string.</param>
        public DefaultValueAttribute(string defaultValue)
        {
            this.defaultValue = defaultValue;
        }

        /// <summary>
        /// Gets the content of the helper message.
        /// </summary>
        public string DefaultValue
        {
            get
            {
                return this.defaultValue;
            }
        }
    }
}

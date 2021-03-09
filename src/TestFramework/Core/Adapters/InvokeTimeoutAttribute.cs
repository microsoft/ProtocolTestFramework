// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Protocols.TestTools
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class InvokeTimeoutAttribute: Attribute
    {
        private readonly int defaultValue;
        /// <summary>
        /// Disables the default constructor.
        /// </summary>
        private InvokeTimeoutAttribute()
        {
        }

        /// <summary>
        /// Initializes the attribute with specified default value.
        /// </summary>
        /// <param name="defaultValue">The default timeout minutes.</param>
        public InvokeTimeoutAttribute(int defaultValue)
        {
            this.defaultValue = defaultValue;
        }

        /// <summary>
        /// Gets the value of InvokeTimeout
        /// </summary>
        public int InvokeTimeoutValue
        {
            get
            {
                return this.defaultValue;
            }
        }
    }
}

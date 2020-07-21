// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Protocols.TestTools.Messages.Runtime
{
    /// <summary>
    /// A type to describe an expected pre constraint
    /// </summary>
    public struct ExpectedPreConstraint
    {
        /// <summary>
        /// The checker to be called when test manager intend to check preconstraint attached to transition.
        /// </summary>
        /// <remarks>
        /// The checker has no parameter or return value.
        /// </remarks>
        public Delegate Checker
        {
            get;
            private set;
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="checker"></param>
        public ExpectedPreConstraint(Delegate checker) : this()
        {
            this.Checker = checker;
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Text;

namespace Microsoft.Protocols.TestTools.Messages.Runtime
{
    /// <summary>
    /// A type to describe an expected method return.
    /// </summary>
    public struct ExpectedReturn
    {
        /// <summary>
        /// The method for which a return is expected,
        /// identified by its reflection representation
        /// </summary>
        public MethodBase Method
        {
            get;
            private set;
        }

        /// <summary>
        /// The target of the method (the instance object where the method
        /// belongs too), or null, if it is a static or an adapter method.
        /// </summary>
        public object Target
        {
            get;
            private set;
        }

        /// <summary>
        /// The checker to be called when the method return
        /// arrives. 
        /// </summary>
        public Delegate Checker
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructs an expected method return.
        /// </summary>
        /// <param name="methodInfo">The reflection information of the method.</param>
        /// <param name="target">The target object. Must be null for static methods and for adapter methods.</param>
        /// <param name="checker">
        ///     The checker. Must match the type of the method. A compatible type is a delegate type
        ///     either taking an array of objects as arguments, exactly the arguments of the method outputs, or
        ///     exactly the arguments of the method outputs preceded by an instance of the method target.     
        /// </param>
        /// <returns></returns>
        public ExpectedReturn(MethodBase methodInfo, object target, Delegate checker) : this()
        {
            this.Method = methodInfo;
            this.Target = target;
            this.Checker = checker;
        }

        /// <summary>
        /// Delivers readable representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            if (Target != null)
            {
                result.Append(MessageRuntimeHelper.Describe(Target));
                result.Append(".");
            }
            result.Append("event ");
            result.Append(Method.Name);
            result.Append("(...)");
            return result.ToString();
        }

    }
}

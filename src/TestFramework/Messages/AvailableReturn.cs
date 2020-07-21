// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Text;

namespace Microsoft.Protocols.TestTools.Messages.Runtime
{
    /// <summary>
    /// A type to describe an available return.
    /// </summary>
    public class AvailableReturn
    {
        /// <summary>
        /// The method identifier by
        /// its reflection representation,
        /// </summary>
        public MethodBase Method
        {
            get;
            private set;
        }

        /// <summary>
        /// The parameters passed to the return.
        /// </summary>
        public object[] Parameters
        {
            get;
            private set;
        }

        /// <summary>
        /// construct
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="parameters"></param>
        public AvailableReturn(MethodBase methodInfo, object[] parameters)
        {
            this.Method = methodInfo;
            this.Parameters = parameters;
        }

        /// <summary>
        /// Delivers readable representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.Append("return ");
            result.Append(Method.Name);
            result.Append("(");
            bool first = true;
            Type returnType;
            if (Method is MethodInfo)
                returnType = ((MethodInfo)Method).ReturnType;
            else
                returnType = null;
            bool hasReturn = returnType != null && returnType != typeof(void);
            int i = hasReturn ? 1 : 0;
            foreach (ParameterInfo pinfo in Method.GetParameters())
            {
                if (pinfo.ParameterType.IsByRef)
                {
                    if (first)
                        first = false;
                    else
                        result.Append(",");
                    if ((pinfo.Attributes & ParameterAttributes.Out) != ParameterAttributes.None)
                        result.Append("out ");
                    else
                        result.Append("ref ");
                    result.Append(Parameters[i++]);
                }
            }
            result.Append(")");
            if (hasReturn)
            {
                result.Append("/");
                result.Append(MessageRuntimeHelper.Describe(Parameters[0]));
            }
            return result.ToString();
        }
    }
}

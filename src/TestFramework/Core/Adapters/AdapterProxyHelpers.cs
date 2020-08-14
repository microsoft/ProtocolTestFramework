// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Microsoft.Protocols.TestTools
{
    internal static class AdapterProxyHelpers
    {
        /// <summary>
        /// Gets the help attribute content for the calling method.
        /// </summary>
        /// <param name="targetMethod">The method from the adapter proxy.</param>
        /// <returns>The help message content.</returns>
        internal static string GetHelpMessage(MethodInfo targetMethod)
        {
            if (targetMethod == null)
            {
                throw new ArgumentNullException("targetMethod");
            }
            // Find the MethodHelp attribute and the corresponding message.
            object[] attrs = targetMethod.GetCustomAttributes(typeof(MethodHelpAttribute), false);
            if (attrs.Length > 0)
            {
                return ((MethodHelpAttribute)attrs[0]).HelpMessage;
            }
            return String.Empty;
        }

        /// <summary>
        /// Parses the result and convert it to the corresponding type.
        /// </summary>
        /// <param name="type">The type of the result which should be converted to.</param>
        /// <param name="result">A string containing the name or value to convert. </param>
        /// <returns></returns>
        internal static object ParseResult(Type type, string result)
        {
            if (result == null)
            {
                // Empty string should be accepted by 'Parse', hence only check null reference.
                throw new ArgumentNullException("result");
            }

            // Convert to non-ref types
            if (type.IsByRef)
            {
                type = type.GetElementType();
            }

            MethodInfo mi;
            // Specially processing String type.
            if (type == typeof(String))
            {
                // Ingore String type.
                return result;
            }
            // Specially processing Enum type.
            else if (type.IsAssignableFrom(typeof(Enum)) || type.IsEnum)
            {
                try
                {
                    return Enum.Parse(type, result, true);
                }
                catch (ArgumentException)
                {
                    throw new FormatException();
                }
            }
            else
            {
                // Check if T has a 'Parse' method.
                try
                {
                    mi = type.GetMethod(
                        "Parse",
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new Type[] { typeof(string) },
                        new ParameterModifier[0]
                        );
                }
                catch (AmbiguousMatchException e)
                {
                    throw new FormatException(
                        String.Format("More than one 'Parse' method is found in {0}.", type), e
                        );
                }
                if (mi == null)
                {
                    throw new FormatException(
                        String.Format(
                            "Can not parse the result, " +
                            "due to the type {0} doesn't contain a method 'public static {0} Parse (String)'.", type)
                        );
                }

                // Invoke and get the result.
                object res = null;
                try
                {
                    res = mi.Invoke(null, new object[] { result });
                }
                catch (TargetInvocationException e)
                {
                    if (e.InnerException != null && e.InnerException is FormatException)
                    {
                        throw e.InnerException;
                    }
                    else
                    {
                        throw;
                    }
                }

                return res;
            }
        }
    }
}

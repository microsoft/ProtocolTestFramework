// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Protocols.TestTools.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Helper for TestProtocolManager
    /// </summary>
    public static class TestManagerHelpers
    {
        /// <summary>
        /// Equality helper class to compare whether two objects are equal.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool Equality(object left, object right)
        {
            if (left == null && right == null)
                return true;
            else if (left == null || right == null)
                return false;
            Type leftType = left.GetType();
            Type rightType = right.GetType();
            if (leftType == rightType)
            {
                if (leftType.IsClass)
                    return Object.ReferenceEquals(left, right);
                else
                    return left.Equals(right);
            }
            else
            {
                throw new NotSupportedException(string.Format("Test Manager doesn't know how to compare left {0} and right {1} value", left, right));
            }
        }

        #region Reflection helpers

        /// <summary>
        /// Return method info for given meta-data information. Throws exception if method cannot be resolved.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="parameterTypes"></param>
        /// <returns></returns>
        public static MethodInfo GetMethodInfo(Type type, string name, params Type[] parameterTypes)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            MethodInfo info = type.GetMethod(name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                null, parameterTypes, null);
            if (info == null)
                throw new InvalidOperationException(String.Format("Cannot resolve method '{0}' in type '{1}'",
                                                                    name, type));
            return info;
        }

        /// <summary>
        /// Return event for given meta-data information. Throws exception if event cannot be resolved.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static EventInfo GetEventInfo(Type type, string name)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            EventInfo info = type.GetEvent(name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (info == null)
                throw new InvalidOperationException(String.Format("Cannot resolve event '{0}' for type '{1}'",
                                                                name, type));
            return info;
        }

        #endregion

        #region Assertion Helpers

        /// <summary>
        /// Asserts two values are equal.
        /// </summary>
        /// <typeparam name="T">Type of values.</typeparam>
        /// <param name="manager">The test manager.</param>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="context">The description of the context under which both values are compared.</param>
        public static void AssertAreEqual<T>(IProtocolTestsManager manager, T expected, T actual, string context)
        {
            manager.Assert(
                Object.Equals(expected, actual),
                string.Format("expected \'{0}\', actual \'{1}\' ({2})", MessageRuntimeHelper.Describe(expected), MessageRuntimeHelper.Describe(actual), context)
                );
        }

        /// <summary>
        /// Asserts two values are equal.
        /// </summary>
        /// <typeparam name="T">Type of values.</typeparam>
        /// <param name="manager">The test manager.</param>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="context">The description of the context under which both values are compared.</param>
        public static void AssertAreEqual<T>(IProtocolTestsManager manager, IList<T> expected, IList<T> actual, string context)
        {
            bool listEqual = false;
            if (expected != null && actual != null)
            {
                listEqual = expected.All(actual.Contains) && expected.Count == actual.Count;
            }
            else if (expected == null && actual == null)
            {
                listEqual = true;
            }

            StringBuilder expectedDescribeBuilder = new StringBuilder();
            foreach (var item in expected)
            {
                expectedDescribeBuilder.AppendFormat("{0},", MessageRuntimeHelper.Describe<object>(item));
            }

            StringBuilder actualDescribeBuilder = new StringBuilder();
            foreach (var item in actual)
            {
                actualDescribeBuilder.AppendFormat("{0},", MessageRuntimeHelper.Describe<object>(item));
            }
            if (expectedDescribeBuilder.Length > 0)
            {
                expectedDescribeBuilder.Remove(expectedDescribeBuilder.Length - 1, 1);
            }
            if (actualDescribeBuilder.Length > 0)
            {
                actualDescribeBuilder.Remove(actualDescribeBuilder.Length - 1, 1);
            }

            manager.Assert(
                listEqual,
                string.Format("expected \'{0}\', actual \'{1}\' ({2})", expectedDescribeBuilder.ToString(), actualDescribeBuilder.ToString(), context)
                );
        }

        /// <summary>
        /// Asserts a variable's equality to a value or bind the variable to a value if it hasn't been bound yet.
        /// </summary>
        /// <typeparam name="T">Type of the variable and value.</typeparam>
        /// <param name="manager">The test manager.</param>
        /// <param name="var">The variable.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="context">The description of the context under which the comparison or binding happens.</param>
        public static void AssertBind<T>(IProtocolTestsManager manager, IVariable<T> var, T actual, string context)
        {
            if (var.IsBound)
            {
                AssertAreEqual<T>(manager, var.Value, actual,
                    context + "; expected value originates from previous binding");
            }
            else
            {
                var.Value = actual;
            }
        }

        /// <summary>
        /// Asserts equality of two variables, or bind one variable to another if only one of them is bound. 
        /// If neither of the two variables are bound, this API does nothing.
        /// </summary>
        /// <typeparam name="T">Type of the variables.</typeparam>
        /// <param name="manager">The test manager.</param>
        /// <param name="v1">The first variable.</param>
        /// <param name="v2">The second variable.</param>
        /// <param name="context">The context under which the comparison or binding happens.</param>
        public static void AssertBind<T>(IProtocolTestsManager manager, IVariable<T> v1, IVariable<T> v2, string context)
        {
            if ((v1.IsBound && v2.IsBound))
            {
                AssertAreEqual<T>(manager, v1.Value, v2.Value,
                    context + "; values originate from previous binding");
                return;
            }
            if (v1.IsBound)
            {
                v2.Value = v1.Value;
            }
            else
            {
                if (v2.IsBound)
                {
                    v1.Value = v2.Value;
                }
            }
        }

        /// <summary>
        /// Asserts a value is not null.
        /// </summary>
        /// <param name="manager">The test manager.</param>
        /// <param name="actual">The value under check.</param>
        /// <param name="context">The context under which the value is checked.</param>
        public static void AssertNotNull(IProtocolTestsManager manager, object actual, string context)
        {
            manager.Assert(actual != null, string.Format("expected non-null value ({0})", context));
        }

        /// <summary>
        /// Describes a (possibly symbolic) value.
        /// </summary>
        /// <typeparam name="T">type</typeparam>
        /// <param name="value">(possibly symbolic) value</param>
        /// <returns>description</returns>
        public static string Describe<T>(T value)
        {
            return MessageRuntimeHelper.Describe(value);
        }
        #endregion
    }
}

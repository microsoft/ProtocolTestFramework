// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security.Permissions;
using System.Security;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Only for internal use. An abstract base class for adapter implementations based on dispatch
    /// proxies. Implements adapter standard methods.
    /// </summary>
    public abstract class AdapterProxyBase : DispatchProxy
    {
        MethodInfo adapterInitializeMethod;
        MethodInfo adapterGetSiteMethod;
        MethodInfo adapterResetMethod;
        MethodInfo disposableDisposeMethod;
        MethodInfo objectGetHashCodeMethod;

        /// <summary>
        /// Adapter interface type.
        /// </summary>
        private Type proxyType;

        /// <summary>
        /// Gets the adapter interface type.
        /// </summary>
        protected Type ProxyType
        {
            get { return proxyType; }
        }

        /// <summary>
        /// The test site which is defined after initialization has been performed.
        /// </summary>
        protected ITestSite TestSite;

        /// <summary>
        /// For derived class to set the proxyType variable.
        /// </summary>
        /// <param name="proxy">The proxy instance.</param>
        /// <param name="typeToProxy">The adapter type.</param>
        protected static void SetParameters(AdapterProxyBase proxy, Type typeToProxy)
        {
            proxy.proxyType = typeToProxy;
            proxy.adapterInitializeMethod = typeof(IAdapter).GetMethod("Initialize");
            proxy.adapterGetSiteMethod = typeof(IAdapter).GetMethod("get_Site");
            proxy.adapterResetMethod = typeof(IAdapter).GetMethod("Reset");
            proxy.disposableDisposeMethod = typeof(IDisposable).GetMethod("Dispose");
            proxy.objectGetHashCodeMethod = typeof(object).GetMethod("GetHashCode");

            if (!typeof(IAdapter).IsAssignableFrom(typeToProxy))
                throw new InvalidOperationException(String.Format("Type '{0}' is not a valid adapter type.", typeToProxy));
        }

        /// <summary>
        /// Implements Invoke method of DispatchProxy. Delegates standard adapter methods to the equivalent proxy methods,
        /// and delegates all other invocations to abstract invoke method.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <param name="args">The arguments the caller passed to the method.</param>
        /// <returns>The object to return to the caller, or null for void methods.</returns>
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod == null)
                throw new ArgumentNullException("targetMethod");

            if (targetMethod == adapterInitializeMethod)
                return Initialize(targetMethod, args);
            if (targetMethod == adapterGetSiteMethod)
               return GetSite(targetMethod);
            if (targetMethod == adapterResetMethod)
                return Reset(targetMethod);
            if (targetMethod == disposableDisposeMethod)
                return Dispose(targetMethod);
            if (targetMethod == objectGetHashCodeMethod)
               return GetHashCode(targetMethod);

            if (TestSite == null)
                throw new InvalidOperationException("Calling method on uninitialized adapter");

            TestSite.CheckErrors();

            return ExecuteMethod(targetMethod, args);
        }

        /// <summary>
        /// To be implemented by derived classes to realize invocation of methods
        /// which are not from the IAdapter interface.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <param name="args">The arguments the caller passed to the method.</param>
        /// <returns>The return value of the ExecuteMethod implementation.</returns>
        protected abstract object ExecuteMethod(MethodInfo targetMethod, object[] args);

        /// <summary>
        /// Initializes the instance of AdapterProxyBase.
        /// Can be overridden by derived classes to do special initialization code, and derived classes should
        /// call base to ensure the test site is initialized.
        /// </summary>
        /// <remarks >
        /// This method will be called automatically by <see cref="ITestSite.GetAdapter"/>. User needs not call it directly.
        /// </remarks>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <param name="args">The arguments the caller passed to the method.</param>
        /// <returns>The return value of the Initialize implementation.</returns>
        protected virtual object Initialize(MethodInfo targetMethod, object[] args)
        {
            TestSite = (ITestSite)args[0];
            return null;
        }

        /// <summary>
        /// Can be overridden by derived classes to do special processing of TestSite getter.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <returns>The return value of the GetSite implementation.</returns>
        protected virtual object GetSite(MethodInfo targetMethod)
        {
           return TestSite;
        }

        /// <summary>
        /// Can be overridden by derived classes to do special processing of Reset.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <returns>The return value of the Reset implementation.</returns>
        protected virtual object Reset(MethodInfo targetMethod)
        {
            return null;
        }

        /// <summary>
        /// Can be overridden by derived classes to do special processing of Dispose.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <returns>The return value of the Dispose implementation.</returns>
        protected virtual object Dispose(MethodInfo targetMethod)
        {
            return null;
        }

        /// <summary>
        /// Can be overridden by derived classes to do special processing of GetHashCode.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <returns>The return value of the Dispose implementation.</returns>
        protected virtual object GetHashCode(MethodInfo targetMethod)
        {
           return GetHashCode();
        }

    }
}

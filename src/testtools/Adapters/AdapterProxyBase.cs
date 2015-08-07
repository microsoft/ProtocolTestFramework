// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
using System.Reflection;
using System.Security.Permissions;
using System.Security;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Only for internal use. An abstract base class for adapter implementations based on transparent
    /// proxies. Implements adapter standard methods.
    /// </summary>
    public abstract class AdapterProxyBase : RealProxy
    {

        MethodBase adapterInitializeMethod;
        MethodBase adapterGetSiteMethod;
        MethodBase adapterResetMethod;
        MethodBase disposableDisposeMethod;
        MethodBase objectGetHashCodeMethod;

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
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected ITestSite TestSite;

        /// <summary>
        /// Constructs a new instance of AdapterProxyBase.
        /// </summary>
        /// <param name="typeToProxy">The Type of the remote object for which to create a proxy.</param>
        protected AdapterProxyBase(Type typeToProxy)
            : base(typeToProxy)
        {
            proxyType = typeToProxy;
            adapterInitializeMethod = typeof(IAdapter).GetMethod("Initialize");
            adapterGetSiteMethod = typeof(IAdapter).GetMethod("get_Site");
            adapterResetMethod = typeof(IAdapter).GetMethod("Reset");
            disposableDisposeMethod = typeof(IDisposable).GetMethod("Dispose");
            objectGetHashCodeMethod = typeof(object).GetMethod("GetHashCode");

            if (!typeof(IAdapter).IsAssignableFrom(typeToProxy))
                throw new InvalidOperationException(String.Format("Type '{0}' is not a valid adapter type.", typeToProxy));
        }

        /// <summary>
        /// Implements Invoke method of RealProxy. Delegates standard adapter methods to the equivalent proxy methods,
        /// and delegates all other invocations to abstract invoke method.        
        /// </summary>
        /// <param name="msg">An IMessage that contains a IDictionary of information about the method call. </param>
        /// <returns>The message returned by the delegated method, containing the return value and any out or ref parameter.</returns>
        public override IMessage Invoke(IMessage msg)
        {

            if (msg == null)
                throw new ArgumentNullException("msg");

            IMethodCallMessage methodCall = msg as IMethodCallMessage;

            if (msg == null)
            {
                throw new InvalidOperationException("Method call is expected.");
            }

            if (methodCall.MethodBase == adapterInitializeMethod)
                return Initialize(methodCall);
            if (methodCall.MethodBase == adapterGetSiteMethod)
                return GetSite(methodCall);
            if (methodCall.MethodBase == adapterResetMethod)
                return Reset(methodCall);
            if (methodCall.MethodBase == disposableDisposeMethod)
                return Dispose(methodCall);
            if (methodCall.MethodBase == objectGetHashCodeMethod)
                return GetHashCode(methodCall);

            if (TestSite == null)
                throw new InvalidOperationException("Calling method on uninitialized adapter");

            TestSite.CheckErrors();

            return Invoke(methodCall);

        }

        /// <summary>
        /// To be implemented by derived classes to realize invocation of methods
        /// which are not from the IAdapter interface.
        /// </summary>
        /// <param name="methodCall">An IMessage that contains a IDictionary of information about the method call.</param>
        /// <returns>The message returned by the Invoke implementation.</returns>
        protected abstract IMessage Invoke(IMethodCallMessage methodCall);


        /// <summary>
        /// Initializes the instance of AdapterProxyBase.
        /// Can be overridden by derived classes to do special initialization code, and derived classes should
        /// call base to ensure the test site is initialized.
        /// </summary>
        /// <remarks >
        /// This method will be called automatically by <see cref="ITestSite.GetAdapter"/>. User needs not call it directly.
        /// </remarks>
        /// <param name="methodCall">An IMessage that contains a IDictionary of information about the method call.</param>
        /// <returns>The message returned by the Initialize implementation.</returns>
        protected virtual IMessage Initialize(IMethodCallMessage methodCall)
        {
            TestSite = (ITestSite)methodCall.Args[0];
            return new ReturnMessage(null, null, 0, methodCall.LogicalCallContext, methodCall);
        }

        /// <summary>
        /// Can be overridden by derived classes to do special processing of TestSite getter.
        /// </summary>
        /// <param name="methodCall">An IMessage that contains a IDictionary of information about the method call.</param>
        /// <returns>The message returned by the GetSite implementation.</returns>
        protected virtual IMessage GetSite(IMethodCallMessage methodCall)
        {
            return new ReturnMessage(TestSite, null, 0, methodCall.LogicalCallContext, methodCall);
        }

        /// <summary>
        /// Can be overridden by derived classes to do special processing of Reset.
        /// </summary>
        /// <param name="methodCall">An IMessage that contains a IDictionary of information about the method call.</param>
        /// <returns>The message returned by the Reset implementation.</returns>
        protected virtual IMessage Reset(IMethodCallMessage methodCall)
        {
            return new ReturnMessage(null, null, 0, methodCall.LogicalCallContext, methodCall);
        }

        /// <summary>
        /// Can be overridden by derived classes to do special processing of Dispose.
        /// </summary>
        /// <param name="methodCall">An IMessage that contains a IDictionary of information about the method call.</param>
        /// <returns>The message returned by the Dispose implementation.</returns>
        protected virtual IMessage Dispose(IMethodCallMessage methodCall)
        {
            return new ReturnMessage(null, null, 0, methodCall.LogicalCallContext, methodCall);
        }

        /// <summary>
        /// Can be overridden by derived classes to do special processing of GetHashCode.
        /// </summary>
        /// <param name="methodCall">An IMessage that contains a IDictionary of information about the method call.</param>
        /// <returns>The message returned by the Dispose implementation.</returns>
        protected virtual IMessage GetHashCode(IMethodCallMessage methodCall)
        {
            return new ReturnMessage(GetHashCode(), null, 0, methodCall.LogicalCallContext, methodCall);
        }

    }
}

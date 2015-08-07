// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.Remoting.Messaging;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// An abstract base class of managed adapters.
    /// </summary>
    public abstract class ManagedAdapterBase : IAdapter
    {
        private ITestSite site;

        #region IAdapter Members

        /// <summary>
        /// Implements <see cref="IAdapter.Site"/>. 
        /// </summary>  
        public ITestSite Site
        {
            get { return site; }
        }

        /// <summary>
        /// Implements <see cref="IAdapter.Initialize"/>. 
        /// </summary>
        /// <remarks >
        /// This method is called automatically by <see cref="DefaultTestSite.GetAdapter"/>. User needs not call it directly.
        /// </remarks>
        /// <param name="testSite">The test site instance associated with the current adapter.</param>
        public virtual void Initialize(ITestSite testSite)
        {
            site = testSite;
        }

        /// <summary>
        /// Implements <see cref="IAdapter.Reset"/>.
        /// </summary>
        public virtual void Reset()
        {
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Indicates whether a given type implements <see cref="IAdapter"/>.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>true if the type implements <see cref="IAdapter"/>; otherwise, false.</returns>
        public static bool IsAdapterType(Type type)
        {
            return typeof(IAdapter) != type && typeof(IAdapter).IsAssignableFrom(type);
        }

        /// <summary>
        /// Indicates whether a given method is declared in an adapter interface (i.e., an interface derived from
        /// <see cref="IAdapter"/>).
        /// </summary>
        /// <param name="method">The method to check</param>
        /// <returns>true if the method is declared in an adapter interface; otherwise, false.</returns>
        public static bool IsAdapterMethod(MemberInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }
            return IsAdapterType(method.DeclaringType);
        }

        #endregion

        #region IDisposable Members

        private bool disposed;

        /// <summary>
        /// Gets or sets the disposing status.
        /// </summary>
        protected bool IsDisposed
        {
            get
            {
                return disposed;
            }
            set
            {
                disposed = value;
            }
        }

        /// <summary>
        /// Implements <see cref="IDisposable.Dispose"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue 
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method is called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// </summary>
        /// <param name="disposing">
        /// If disposing equals false, the method is called by the 
        /// runtime from inside the finalizer and you should not reference 
        /// other objects. Only unmanaged resources can be disposed.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    site = null;
                }

                // Call the appropriate methods to clean up 
                // unmanaged resources here.
                // If disposing is false, 
                // only the following code is executed.
            }
            disposed = true;
        }


        /// <summary>
        /// This destructor runs only if the Dispose method does not get called.
        /// It gives your base class the opportunity to finalize.
        /// Do not provide destructors in types derived from this class.
        /// </summary>
        ~ManagedAdapterBase()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        #endregion
    }

    class ManagedAdapterProxy : AdapterProxyBase
    {
        private ManagedAdapterBase instance;
        private Type trueType;

        /// <summary>
        /// Constructs a new managed adapter proxy.
        /// </summary>
        /// <param name="adapterType">The managed adapter type</param>
        /// <param name="typeToProxy">The type of adapter which the proxy works for.</param>
        public ManagedAdapterProxy(Type adapterType, Type typeToProxy)
            : base(typeToProxy)
        {
            trueType = adapterType;
            try
            {
                instance = TestToolHelpers.CreateInstanceFromTypeName(adapterType.FullName) as ManagedAdapterBase;
            }
            catch (InvalidOperationException)
            {
            }

            if (instance == null)
            {
                throw new InvalidOperationException(
                   String.Format("Adapter {0} instance creation failed",
                                 typeToProxy.FullName));
            }
            else if (!typeToProxy.IsAssignableFrom(instance.GetType()))
            {
                throw new InvalidOperationException(
                    String.Format("Adapter {0} does not implement {1}",
                                  adapterType.Name, typeToProxy.FullName));
            }
        }

        /// <summary>
        /// Initializes the managed adapter.
        /// This method can be overridden by extenders to do special initialization code.
        /// It calls base class's Initialize method to ensure the test site is initialized.
        /// </summary>
        /// <param name="methodCall"></param>
        /// <returns></returns>
        protected override IMessage Initialize(IMethodCallMessage methodCall)
        {
            base.Initialize(methodCall);
            TestSite.Log.Add(LogEntryKind.Comment,
                String.Format("Adapter {0} implements {1}",
                                  trueType.FullName, ProxyType.FullName));
            TestSite.Log.Add(LogEntryKind.EnterAdapter,
                    "Managed adapter: {0}, method: {1}",
                    ProxyType.Name,
                    methodCall.MethodName);
            try
            {
                instance.Initialize((ITestSite)methodCall.Args[0]);
            }
            catch (Exception ex)
            {
                TestSite.Log.Add(LogEntryKind.Debug, ex.ToString());
                throw;
            }
            finally
            {
                TestSite.Log.Add(LogEntryKind.ExitAdapter,
                        "Managed adapter: {0}, method: {1}",
                        ProxyType.Name,
                        methodCall.MethodName);
            }
            return new ReturnMessage(null, null, 0, methodCall.LogicalCallContext, methodCall);
        }

        /// <summary>
        /// Can be overridden by extenders to do special processing of TestSite getter.
        /// </summary>
        /// <param name="methodCall"></param>
        /// <returns></returns>
        protected override IMessage GetSite(IMethodCallMessage methodCall)
        {
            return new ReturnMessage(TestSite, null, 0, methodCall.LogicalCallContext, methodCall);
        }

        /// <summary>
        /// Can be overridden by extenders to do special processing of Reset.
        /// </summary>
        /// <param name="methodCall"></param>
        /// <returns></returns>
        protected override IMessage Reset(IMethodCallMessage methodCall)
        {
            TestSite.Log.Add(LogEntryKind.EnterAdapter,
                    "Managed adapter: {0}, method: {1}",
                    ProxyType.Name,
                    methodCall.MethodName);
            try
            {
                instance.Reset();
            }
            catch (Exception ex)
            {
                TestSite.Log.Add(LogEntryKind.Debug, ex.ToString());
                throw;
            }
            finally
            {
                TestSite.Log.Add(LogEntryKind.ExitAdapter,
                        "Managed adapter: {0}, method: {1}",
                        ProxyType.Name,
                        methodCall.MethodName);
            }
            return new ReturnMessage(null, null, 0, methodCall.LogicalCallContext, methodCall);
        }

        /// <summary>
        /// Can be overridden by extenders to do special processing of Dispose.
        /// </summary>
        /// <param name="methodCall"></param>
        /// <returns></returns>
        protected override IMessage Dispose(IMethodCallMessage methodCall)
        {
            try
            {
                instance.Dispose();
            }
            finally
            {
                base.Dispose(methodCall);
            }
            return new ReturnMessage(null, null, 0, methodCall.LogicalCallContext, methodCall);
        }

        /// <summary>
        /// Proxy method for substitution of executing methods in adapter interface.
        /// </summary>
        /// <param name="methodCall">The IMethodCallMessage containing method invoking data.</param>
        /// <returns>The IMessage containing method return data.</returns>
        protected override IMessage Invoke(IMethodCallMessage methodCall)
        {
            object retVal = null;
            object[] args = methodCall.Args;
            // Check if this is a method from IAdapter. Any IAdapter methods should be ignored.
            if (!AdapterType.IsAdapterTypeFullName(methodCall.MethodBase.DeclaringType.FullName)
                && (methodCall.MethodBase.DeclaringType.FullName != typeof(IDisposable).FullName)
                )
            {
                TestSite.Log.Add(LogEntryKind.EnterAdapter,
                        "Managed adapter: {0}, method: {1}",
                        ProxyType.Name,
                        methodCall.MethodName);

                try
                {
                    if (methodCall.MethodBase.IsStatic)
                    {
                        retVal = methodCall.MethodBase.Invoke(null, args);
                    }
                    else
                    {
                        retVal = methodCall.MethodBase.Invoke(instance, args);
                    }
                }
                catch (Exception ex)
                {
                    if (ex is TargetInvocationException)
                    {
                        // thrown by methods invoked through reflection
                        // InnerException contains the actual exception thrown by methods
                        TestSite.Log.Add(LogEntryKind.Debug, ex.InnerException.ToString());
                        throw ex.InnerException;
                    }
                    else
                    {
                        TestSite.Log.Add(LogEntryKind.Debug, ex.ToString());
                        throw;
                    }
                }
                finally
                {
                    TestSite.Log.Add(LogEntryKind.ExitAdapter,
                        "Managed adapter: {0}, method: {1}",
                        ProxyType.Name,
                        methodCall.MethodName);
                }
            }
            ReturnMessage mret = new ReturnMessage(
                retVal,
                args,
                methodCall.ArgCount,
                methodCall.LogicalCallContext,
                methodCall);
            return mret;
        }
    }
}

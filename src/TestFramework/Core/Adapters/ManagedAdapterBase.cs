// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

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

    /// <summary>
    /// A class which is used as proxy for constructing IAdapter using managed code
    /// and executing methods in IAdapter.
    /// </summary>
    public class ManagedAdapterProxy : AdapterProxyBase
    {
        private ManagedAdapterBase instance;
        private Type trueType;

        /// <summary>
        /// Create an instance of the managed adapter.
        /// </summary>
        /// <typeparam name="T">The type of the adapter.</typeparam>
        /// <param name="adapterType">The type of the implementation.</param>
        /// <param name="typeToProxy">The type of the adapter.</param>
        /// <returns>The managed adapter</returns>
        public static T Wrap<T>(Type adapterType, Type typeToProxy) where T : IAdapter
        {
            object proxy = Create<T, ManagedAdapterProxy>();
            ManagedAdapterProxy self = (ManagedAdapterProxy)proxy;

            AdapterProxyBase.SetParameters((ManagedAdapterProxy)proxy, typeToProxy);
            self.trueType = adapterType;

            try
            {
                self.instance = TestToolHelpers.CreateInstanceFromTypeName(adapterType.FullName) as ManagedAdapterBase;
            }
            catch (InvalidOperationException)
            {
            }

            if (self.instance == null)
            {
                throw new InvalidOperationException(
                   String.Format("Adapter {0} instance creation failed",
                                 typeToProxy.FullName));
            }
            else if (!typeToProxy.IsAssignableFrom(self.instance.GetType()))
            {
                throw new InvalidOperationException(
                    String.Format("Adapter {0} does not implement {1}",
                                  adapterType.Name, typeToProxy.FullName));
            }

            return (T)proxy;
        }

        /// <summary>
        /// Initializes the managed adapter.
        /// This method can be overridden by extenders to do special initialization code.
        /// It calls base class's Initialize method to ensure the test site is initialized.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <param name="args">The arguments the caller passed to the method.</param>
        /// <returns>The return value of the Initialize implementation.</returns>
        protected override object Initialize(MethodInfo targetMethod, object[] args)
        {
            base.Initialize(targetMethod, args);
            TestSite.Log.Add(LogEntryKind.Comment,
                String.Format("Adapter {0} implements {1}",
                                  trueType.FullName, ProxyType.FullName));
            TestSite.Log.Add(LogEntryKind.EnterAdapter,
                    "Managed adapter: {0}, method: {1}",
                    ProxyType.Name,
                    targetMethod.Name);
            try
            {
                instance.Initialize((ITestSite)args[0]);
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
                        targetMethod.Name);
            }
            return null;
        }

        /// <summary>
        /// Can be overridden by extenders to do special processing of TestSite getter.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <returns>The return value of the GetSite implementation.</returns>
        protected override object GetSite(MethodInfo targetMethod)
        {
           return base.GetSite(targetMethod);
        }

        /// <summary>
        /// Can be overridden by extenders to do special processing of Reset.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <returns>The return value of the Reset implementation.</returns>
        protected override object Reset(MethodInfo targetMethod)
        {
            TestSite.Log.Add(LogEntryKind.EnterAdapter,
                    "Managed adapter: {0}, method: {1}",
                    ProxyType.Name,
                    targetMethod.Name);
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
                        targetMethod.Name);
            }
            return null;
        }

        /// <summary>
        /// Can be overridden by extenders to do special processing of Dispose.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <returns>The return value of the Dispose implementation.</returns>
        protected override object Dispose(MethodInfo targetMethod)
        {
            try
            {
                instance.Dispose();
            }
            finally
            {
                base.Dispose(targetMethod);
            }
            return null;
        }

        /// <summary>
        /// Proxy method for substitution of executing methods in adapter interface.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <param name="args">The arguments the caller passed to the method.</param>
        /// <returns>The return value of the ExecuteMethod implementation.</returns>
        protected override object ExecuteMethod(MethodInfo targetMethod, object[] args)
        {
            object retVal = null;
            // Check if this is a method from IAdapter. Any IAdapter methods should be ignored.
            if (!AdapterType.IsAdapterTypeFullName(targetMethod.DeclaringType.FullName)
                && (targetMethod.DeclaringType.FullName != typeof(IDisposable).FullName)
                )
            {
                TestSite.Log.Add(LogEntryKind.EnterAdapter,
                        "Managed adapter: {0}, method: {1}",
                        ProxyType.Name,
                        targetMethod.Name);

                try
                {
                    if (targetMethod.IsStatic)
                    {
                        retVal = targetMethod.Invoke(null, args);
                    }
                    else
                    {
                        retVal = targetMethod.Invoke(instance, args);
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
                        targetMethod.Name);
                }
            }

            return retVal;
        }
    }
}

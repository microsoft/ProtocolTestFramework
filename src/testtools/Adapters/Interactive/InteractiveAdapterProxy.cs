// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
using System.Windows.Forms;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// A class which is used as proxy for constructing IAdapter of default type
    /// and executing methods in IAdapter.
    /// </summary>
    class InteractiveAdapterProxy : AdapterProxyBase
    {

        /// <summary>
        /// Constructs a new default type adapter proxy.
        /// </summary>
        /// <param name="typeToProxy">The type of adapter which the proxy works for.</param>
        public InteractiveAdapterProxy(Type typeToProxy)
            : base(typeToProxy)
        {
        }

        /// <summary>
        /// Proxy method for substitution of executing methods in adapter interface.
        /// </summary>
        /// <param name="methodCall">The IMessage containing method invoking data.</param>
        /// <returns>The IMessage containing method return data.</returns>
        protected override IMessage Invoke(IMethodCallMessage methodCall)
        {
            ReturnMessage mret = null;

            // Check if this is a method from IAdapter. Any IAdapter methods should be ignored.
            if ((methodCall.MethodBase.DeclaringType.FullName != typeof(IAdapter).FullName)
                && (methodCall.MethodBase.DeclaringType.FullName != typeof(IDisposable).FullName)
                )
            {
                TestSite.Log.Add(LogEntryKind.EnterAdapter, 
                    "Interactive adapter: {0}, method: {1}", 
                    ProxyType.Name,
                    methodCall.MethodName);
                try
                {
                    // Instantiate a new UI window.
                    using (InteractiveAdapterDialog adapterDlg = new InteractiveAdapterDialog(methodCall, TestSite.Properties))
                    {
                        DialogResult dialogResult = adapterDlg.ShowDialog();

                        if (dialogResult != DialogResult.OK)
                        {
                            string msg = "Failed";
                            TestSite.Assume.Fail(msg);
                        }
                        else
                        {
                            mret = new ReturnMessage(
                                adapterDlg.ReturnValue,
                                adapterDlg.OutArgs.Length > 0 ? adapterDlg.OutArgs : null,
                                adapterDlg.OutArgs.Length,
                                methodCall.LogicalCallContext,
                                methodCall);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TestSite.Log.Add(LogEntryKind.Debug, ex.ToString());
                    throw;
                }
                finally
                {
                    TestSite.Log.Add(LogEntryKind.ExitAdapter,
                        "Interactive adapter: {0}, method: {1}",
                        ProxyType.Name,
                        methodCall.MethodName);
                }
            }
            else
            {
                // TODO: Do we need to take care ReturnMessage (Exception, IMethodCallMessage) ?
                mret = new ReturnMessage(null, null, 0, methodCall.LogicalCallContext, methodCall);
            }

            return mret;
        }
    }
}

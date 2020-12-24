// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// 
    /// </summary>
    public class InteractiveAdapterProxy : AdapterProxyBase
    {
        private string scriptDirectory;

        /// <summary>
        /// Create an instance of the powershell adapter.
        /// </summary>
        /// <typeparam name="T">The type of the adapter.</typeparam>
        /// <param name="scriptDirectory">The folder containing the script files.</param>
        /// <param name="typeToProxy">The type of the adapter.</param>
        /// <returns>The powershell adapter</returns>
        public static T Wrap<T>(Type typeToProxy) where T : IAdapter
        {
            object proxy = Create<T, InteractiveAdapterProxy>();
            InteractiveAdapterProxy self = (InteractiveAdapterProxy)proxy;

            AdapterProxyBase.SetParameters(self, typeToProxy);
            //self.scriptDirectory = scriptDirectory.Replace("\\", "/");

            return (T)proxy;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodCall"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected override object ExecuteMethod(MethodInfo methodCall, object[] args)
        {
            int retVal = 0;

            // Check if this is a method from IAdapter. Any IAdapter methods should be ignored.
            if (!AdapterType.IsAdapterTypeFullName(methodCall.DeclaringType.FullName)
                && (methodCall.DeclaringType.FullName != typeof(IDisposable).FullName)
                )
            {
                TestSite.Log.Add(LogEntryKind.EnterAdapter,
                        "Managed adapter: {0}, method: {1}",
                        ProxyType.Name,
                        methodCall.Name);

                try
                {
                    string msg = "Failed";
                    using (InteractiveAdapterConsole consoleAdapter = new InteractiveAdapterConsole(methodCall, TestSite.Properties, args))
                    {
                        retVal = consoleAdapter.ProcessArguments();
                        if (retVal == 0) // Abort case.
                        {
                            TestSite.Assume.Fail(msg);
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
                        "PowerShell adapter: {0}, method: {1}",
                        ProxyType.Name,
                        methodCall.Name);
                }
            }

            return retVal;
        }
    }
}

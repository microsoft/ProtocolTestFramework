// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using System.IO;
using System.Data;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// A class which is used as proxy for constructing IAdapter of Shell script type
    /// and executing methods in IAdapter.
    /// </summary>
    class ShellAdapterProxy : AdapterProxyBase
    {
        private readonly string scriptDirectory;
        private ParameterDataBuilder builder;
        private object[] outArgs;
        private bool compatMode;
        private string lastOutput;
        private StringBuilder errorMsg;

        /// <summary>
        /// Constructs a new command script adapter proxy.
        /// </summary>
        /// <param name="scriptDirectory">The path and file name of command script files.</param>
        /// <param name="typeToProxy">The type of adapter which the proxy works for.</param>
        public ShellAdapterProxy(string scriptDirectory, Type typeToProxy)
            : base(typeToProxy)
        {
            this.scriptDirectory = scriptDirectory;
        }

        /// <summary>
        /// Can be overridden by extenders to do special initialization code.
        /// Call base to ensure the test site is initialized.
        /// </summary>
        /// <param name="methodCall"></param>
        /// <returns></returns>
        protected override IMessage Initialize(IMethodCallMessage methodCall)
        {
            base.Initialize(methodCall);
            return this.Invoke(methodCall);
        }

        /// <summary>
        /// Can be overridden by extenders to do special processing of Reset.
        /// </summary>
        /// <param name="methodCall"></param>
        /// <returns></returns>
        protected override IMessage Reset(IMethodCallMessage methodCall)
        {
            return this.Invoke(methodCall);
        }

        /// <summary>
        /// Proxy method for substitution of executing Initialize/Reset methods in adapter interface.
        /// </summary>
        /// <param name="methodCall">The IMethodCallMessage containing method invoking data.</param>
        /// <returns>Always void.</returns>
        private IMessage InvokeCompat(IMethodCallMessage methodCall)
        {
            string path = LookupScript(methodCall.MethodName);
            if (path != null)
            {
                TestSite.Log.Add(LogEntryKind.EnterAdapter,
                        "Shell adapter: {0}, method: {1}",
                        ProxyType.Name,
                        methodCall.MethodName);

                try
                {
                    int scriptRet = InvokeScript(path, "");
                    if (scriptRet != 0)
                    {
                        TestSite.Assume.Fail(
                            "Script {0}.sh exited with non-zero code. Error code: {1}.",
                            methodCall.MethodName,
                            scriptRet);
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
                        "Shell adapter: {0}, method: {1}",
                        ProxyType.Name,
                        methodCall.MethodName);
                }
            }
            else
            {
                TestSite.Log.Add(LogEntryKind.Debug,
                        "Shell adapter: {0}, method: {1} not found, skipped.",
                        ProxyType.Name,
                        methodCall.MethodName);
            }
            ReturnMessage mret = new ReturnMessage(
                null,
                null,
                0,
                methodCall.LogicalCallContext,
                methodCall);
            return mret;
        }

        /// <summary>
        /// Proxy method for substitution of executing methods in adapter interface.
        /// </summary>
        /// <param name="mcall">The IMethodCallMessage containing method invoking data.</param>
        /// <returns>The IMessage containing method return data.</returns>
        protected override IMessage Invoke(IMethodCallMessage mcall)
        {
            // set to compat Mode to allow running of Initialize.sh or Reset.sh if call from IAdapter context
            compatMode = ((mcall.MethodName == "Initialize" || mcall.MethodName == "Reset")
                && AdapterType.IsAdapterTypeFullName(mcall.MethodBase.DeclaringType.FullName)
                );
            if (compatMode)
                return InvokeCompat(mcall);

            // Build parameter on each invoke
            builder = new ParameterDataBuilder(mcall);
            builder.Build();

            outArgs = mcall.Args;
            lastOutput = null;
            errorMsg = new StringBuilder();

            object retVal = null;
            string arguments = BuildScriptArguments();

            // Check if this is a method from IAdapter. Any IAdapter methods should be ignored.
            if (!AdapterType.IsAdapterTypeFullName(mcall.MethodBase.DeclaringType.FullName)
                && (mcall.MethodBase.DeclaringType.FullName != typeof(IDisposable).FullName)
                )
            {
                TestSite.Log.Add(LogEntryKind.EnterAdapter,
                    "Shell adapter: {0}, method: {1}",
                    ProxyType.Name,
                    mcall.MethodName);

                try
                {
                    string path = LookupScript(mcall.MethodName);
                    if (path == null)
                    {
                        TestSite.Assume.Fail(
                            "Shell script file ({0}.sh) can not be found.",
                            mcall.MethodName);
                    }
                    else
                    {
                        int scriptRet = InvokeScript(path, arguments);

                        if (scriptRet != 0)
                        {
                            string exceptionMessage = string.Format("Exception thrown when executing {0}.sh.\nExit code: {1}\nError message: {2}\n", mcall.MethodName, scriptRet, errorMsg);
                            throw new InvalidOperationException(exceptionMessage);
                        }

                        if (builder.HasReturnVal)
                        {
                            retVal = AdapterProxyHelpers.ParseResult(builder.RetValType, lastOutput);
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
                        "Shell adapter: {0}, method: {1}",
                        ProxyType.Name,
                        mcall.MethodName);
                }
            }

            ReturnMessage mret = new ReturnMessage(
                retVal,
                (outArgs != null && outArgs.Length > 0) ? outArgs : null,
                (outArgs != null) ? outArgs.Length : 0,
                mcall.LogicalCallContext,
                mcall);
            return mret;
        }

        private string LookupScript(string methodname)
        {
            string dir = scriptDirectory;

            string foundFile = Path.Combine(dir, methodname + ".sh");
            if (File.Exists(foundFile))
            {
                foundFile = foundFile.Replace('\\', '/');
                return foundFile;
            }
            else
            {
                return null;
            }
        }

        private string BuildScriptArguments()
        {
            StringBuilder arguments = new StringBuilder();
            arguments.Append(BuildInArguments());

            return arguments.ToString();
        }

        private string BuildInArguments()
        {
            if (!builder.HasInArg)
            {
                return "";
            }

            StringBuilder ret = new StringBuilder();

            int i = 0;
            foreach (Type t in builder.InArgTypes)
            {
                var row = builder.InArgDataTable.Rows[i++];
                string value = (string)row["Value"];

                // escape single quotes
                value = value.Replace("'", "'\\''");

                // put argument inside single quotes to escape special charaters
                ret.Append(String.Format("'{0}' ", value));
            }

            return ret.ToString();
        }

        /// <summary>
        /// Invokde script by given file path and arguments.
        /// </summary>
        /// <param name="path">The file path to the sh script.</param>
        /// <param name="arguments">The argument to be passed to the script.</param>
        /// <returns>The return value of script executation.</returns>
        private int InvokeScript(string path, string arguments)
        {
            int exitCode = 0;
            try
            {
                using (Process proc = new Process())
                {
                    string winDir = Environment.GetEnvironmentVariable("WINDIR");
                    string wslPath;
                    if (Environment.Is64BitProcess)
                    {
                        wslPath = string.Format(@"{0}\System32\bash.exe", winDir);
                    }
                    else
                    {
                        wslPath = string.Format(@"{0}\Sysnative\bash.exe", winDir);
                    }

                    if (!File.Exists(wslPath))
                    {
                        TestSite.Assume.Fail("Windows Subsystem for Linux (WSL) is not installed.");
                    }

                    proc.StartInfo.FileName = wslPath;
                    proc.StartInfo.Arguments = String.Format("{0} {1}", path, arguments);
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.RedirectStandardOutput = true;

                    List<string> wslEnvs = new List<string>();

                    // set ptfconfig properties as environment variables
                    foreach (string key in TestSite.Properties.AllKeys)
                    {
                        string envVar = "PTFProp_" + key.Replace(".", "_");
                        if (proc.StartInfo.EnvironmentVariables.ContainsKey(envVar))
                        {
                            proc.StartInfo.EnvironmentVariables.Remove(envVar);
                        }
                        proc.StartInfo.EnvironmentVariables.Add(envVar, TestSite.Properties[key]);

                        wslEnvs.Add(envVar + "/u");
                    }

                    // Set WSLENV to pass those environment variables into WSL
                    proc.StartInfo.EnvironmentVariables.Add("WSLENV", String.Join(":", wslEnvs));

                    proc.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceivedHandler);
                    proc.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceivedHandler);
                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                    proc.WaitForExit();
                    exitCode = proc.ExitCode;
                    proc.Close();
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                TestSite.Assume.Fail(ex.Message);
            }
            return exitCode;
        }

        private void ErrorDataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                TestSite.Log.Add(LogEntryKind.Comment, "STDERR: {0}", e.Data);

                errorMsg.Append(e.Data);
            }
        }

        private void OutputDataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                TestSite.Log.Add(LogEntryKind.Comment, "STDOUT: {0}", e.Data);

                if (!String.IsNullOrEmpty(e.Data.Trim()))
                {
                    lastOutput = e.Data.Trim();
                }
            }
        }
    }
}

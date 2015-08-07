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
    /// A class which is used as proxy for constructing IAdapter of command script type
    /// and executing methods in IAdapter.
    /// </summary>
    class ScriptAdapterProxy : AdapterProxyBase
    {
        private string scriptDirectory;
        private ParameterDataBuilder builder;
        private string ptfAdFailureMessage;
        private static Regex normalizePattern = new Regex(@"[\s]+", RegexOptions.Compiled);
        private object[] outArgs;
        private bool compatMode;

        /// <summary>
        /// Constructs a new command script adapter proxy.
        /// </summary>
        /// <param name="scriptDirectory">The path and file name of command script files.</param>
        /// <param name="typeToProxy">The type of adapter which the proxy works for.</param>
        public ScriptAdapterProxy(string scriptDirectory, Type typeToProxy)
            : base(typeToProxy)
        {
            this.scriptDirectory = scriptDirectory;
        }

        /// <summary>
        /// Can be overridden by extenders to do special initialization code.
        /// Call base to ensure the test site is initialized.
        /// </summary>
        /// <param name="mcall"></param>
        /// <returns></returns>
        protected override IMessage Initialize(IMethodCallMessage mcall)
        {
            base.Initialize(mcall);
            return this.Invoke(mcall);
        }

        /// <summary>
        /// Can be overridden by extenders to do special processing of Reset.
        /// </summary>
        /// <param name="mcall"></param>
        /// <returns></returns>
        protected override IMessage Reset(IMethodCallMessage mcall)
        {
            return this.Invoke(mcall);
        }

        /// <summary>
        /// Proxy method for substitution of executing Initialize/Reset methods in adapter interface.
        /// </summary>
        /// <param name="mcall">The IMethodCallMessage containing method invoking data.</param>
        /// <returns>Always void.</returns>
        private IMessage InvokeCompat(IMethodCallMessage mcall)
        {
            string path = LookupScript(mcall.MethodName);
            if (path != null)
            {
                TestSite.Log.Add(LogEntryKind.EnterAdapter,
                        "Script adapter: {0}, method: {1}",
                        ProxyType.Name,
                        mcall.MethodName);

                try
                {
                    int scriptRet = InvokeScript(path, "");
                    if (scriptRet != 0)
                    {
                        TestSite.Assume.Fail(
                            "Script {0}.cmd exited with non-zero code. Error code: {1}. Failure message: {2}",
                            mcall.MethodName,
                            scriptRet,
                            ptfAdFailureMessage);
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
                        "Script adapter: {0}, method: {1}",
                        ProxyType.Name,
                        mcall.MethodName);
                }
            }
            else
            {
                TestSite.Log.Add(LogEntryKind.Debug,
                        "Script adapter: {0}, method: {1} not found, skipped.",
                        ProxyType.Name,
                        mcall.MethodName);
            }
            ReturnMessage mret = new ReturnMessage(
                null,
                null,
                0,
                mcall.LogicalCallContext,
                mcall);
            return mret;
        }

        /// <summary>
        /// Proxy method for substitution of executing methods in adapter interface.
        /// </summary>
        /// <param name="mcall">The IMethodCallMessage containing method invoking data.</param>
        /// <returns>The IMessage containing method return data.</returns>
        protected override IMessage Invoke(IMethodCallMessage mcall)
        {
            // set to compat Mode to allow running of Initialize.cmd or Reset.cmd if call from IAdapter context
            compatMode = ((mcall.MethodName == "Initialize" || mcall.MethodName == "Reset")
                && AdapterType.IsAdapterTypeFullName(mcall.MethodBase.DeclaringType.FullName)
                );
            if (compatMode)
                return InvokeCompat(mcall);

            // Build parameter on each invoke
            builder = new ParameterDataBuilder(mcall);
            builder.Build();

            // Initial error message on each invoke
            ptfAdFailureMessage = null;

            object retVal = null;
            outArgs = mcall.Args;
            string methodhelp = AdapterProxyHelpers.GetHelpMessage(mcall);
            string arguments = BuildScriptArguments(methodhelp);

            // Check if this is a method from IAdapter. Any IAdapter methods should be ignored.
            if (!AdapterType.IsAdapterTypeFullName(mcall.MethodBase.DeclaringType.FullName)
                && (mcall.MethodBase.DeclaringType.FullName != typeof(IDisposable).FullName)
                )
            {
                TestSite.Log.Add(LogEntryKind.EnterAdapter,
                    "Script adapter: {0}, method: {1}",
                    ProxyType.Name,
                    mcall.MethodName);

                try
                {
                    string path = LookupScript(mcall.MethodName);
                    if (path == null)
                    {
                        TestSite.Assume.Fail(
                            "The invoking script file ({0}.cmd) can not be found.",
                            mcall.MethodName);
                    }
                    else
                    {
                        int scriptRet = InvokeScript(path, arguments);
                        if (scriptRet != 0)
                        {
                            TestSite.Assume.Fail(
                                "Script {0}.cmd exited with non-zero code. Error code: {1}. Failure message: {2}",
                                mcall.MethodName,
                                scriptRet,
                                ptfAdFailureMessage);
                        }
                        retVal = GetReturnValue();
                        GetOutArgumentsValues();
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
                        "Script adapter: {0}, method: {1}",
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

            string foundFile = Path.Combine(dir, methodname + ".cmd");
            if (File.Exists(foundFile))
            {
                return foundFile;
            }
            else
            {
                return null;
            }
        }

        private string BuildScriptArguments(string methodHelp)
        {
            // first parameter: return and out/ref parameter types
            StringBuilder arguments = new StringBuilder();
            arguments.Append(BuildOutTypesArgument());

            // second parameter: in/ref parameter types
            arguments.Append(' ');
            arguments.Append(BuildInTypesArgument());

            // third parameter: methodHelp
            arguments.Append(' ');
            arguments.Append(NormalizeParameter(methodHelp));

            // action parameters
            arguments.Append(' ');
            arguments.Append(BuildInArguments());

            return arguments.ToString();
        }

        private string BuildOutTypesArgument()
        {
            // PtfAdReturns="[returnValue:<type of returnValue>;][<name of outParam1>:<type of outParam1>[;<name of outParam2>:<type of outParam2>]�]"
            string ret = "\"";
            if (builder.HasReturnVal)
            {
                ret += "PtfAdReturn:" + builder.RetValType.ToString() + ";";
            }

            if (builder.HasOutArg)
            {
                int k = builder.HasReturnVal ? 1 : 0;
                foreach (Type t in builder.OutArgTypes)
                {
                    string name = (string)builder.OutArgDataTable.Rows[k++]["Name"];
                    ret += name + ":" + t.ToString() + ";";
                }
            }

            ret = ret.TrimEnd(new char[] { ';' });

            ret += "\"";
            return ret;
        }

        private string BuildInTypesArgument()
        {
            string ret = "\"";

            if (builder.HasInArg)
            {
                int k = 0;
                foreach (Type t in builder.InArgTypes)
                {
                    string name = (string)builder.InArgDataTable.Rows[k++]["Name"];
                    ret += name + ":" + t.ToString() + ";";
                }
            }

            ret = ret.TrimEnd(new char[] { ';' });

            ret += "\"";
            return ret;
        }

        private string BuildInArguments()
        {
            // [<name of param1>:"<value of param1>"][ <name of param2>:"<value of param2>"]�]
            StringBuilder ret = new StringBuilder();
            DataTable dt = builder.InArgDataTable;

            string value = String.Empty;
            foreach (DataRow dr in dt.Rows)
            {
                value = NormalizeParameter(dr["Value"].ToString());
                ret.Append(value);
                ret.Append(' ');
            }
            return ret.ToString();
        }

        /// <summary>
        /// Normalize a string value to a safe paramter to script
        /// </summary>
        /// <param name="value">The parameter to normalize</param>
        /// <returns>Normalized parameter</returns>
        private static string NormalizeParameter(string value)
        {
            // create placeholder for a null or empty parameter
            if (String.IsNullOrEmpty(value))
                return "\"\"";
            // if the parameter has been quoted, just return it.
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                return value;
            }
            // if contains blank chars, quote it. else, left it as is.
            if (normalizePattern.IsMatch(value))
            {
                return "\"" + value + "\"";
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Invokde script by given file path and arguments.
        /// </summary>
        /// <param name="path">The file path to the cmd script.</param>
        /// <param name="arguments">The argument to be passed to the script.</param>
        /// <returns>The return value of script executation.</returns>
        private int InvokeScript(string path, string arguments)
        {
            int exitCode = 0;
            try
            {
                using (Process proc = new Process())
                {
                    proc.StartInfo.FileName = path;
                    proc.StartInfo.Arguments = arguments;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.RedirectStandardOutput = true;
                    foreach (string key in TestSite.Properties.AllKeys)
                    {
                        string envVar = "PTFProp" + key;
                        if (proc.StartInfo.EnvironmentVariables.ContainsKey(envVar))
                        {
                            proc.StartInfo.EnvironmentVariables.Remove(envVar);
                        }
                        proc.StartInfo.EnvironmentVariables.Add(envVar, TestSite.Properties[key]);
                    }
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
            catch (System.ComponentModel.Win32Exception)
            {
                ptfAdFailureMessage = string.Format("Verify if the file {0} is a valid and non-empty .cmd script.", path);
                TestSite.Assume.Fail(ptfAdFailureMessage);
            }
            return exitCode;
        }

        private void ErrorDataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                TestSite.Log.Add(LogEntryKind.Comment, "STDERR: {0}", e.Data);
            }
        }

        private void OutputDataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                TestSite.Log.Add(LogEntryKind.Comment, "STDOUT: {0}", e.Data);
                ParsePtfAdValues(e.Data);
            }
        }

        private void ParsePtfAdValues(string input)
        {
            if (String.IsNullOrEmpty(input))
                return;

            // split using ";
            // eg., x="10";y="20" will be splitted to x="10 and y="20"
            string split = "\";";
            string[] strs = input.Split(new string[] { split }, StringSplitOptions.RemoveEmptyEntries);
            KeyValuePair<string, string> kvp;
            for (int k = 0; k < strs.Length; k++)
            {
                // add missed char " at the end of string if needed
                if (!strs[k].EndsWith("\"") && strs[k].Contains("=\""))
                {
                    strs[k] = strs[k] + "\"";
                }

                kvp = ParseStatement(strs[k]);
                if (k == 0)
                {
                    // if the leading statement is not PtfAd*, ignore the output
                    if (kvp.Key != "PtfAdReturn" && kvp.Key != "PtfAdFailureMessage")
                        return;
                    else
                    {
                        if (!compatMode && builder.HasReturnVal && kvp.Key == "PtfAdReturn")
                        {
                            builder.OutArgDataTable.Rows[0]["Value"] = kvp.Value;
                        }
                        else
                        {
                            ptfAdFailureMessage = kvp.Value;
                        }
                        continue;
                    }
                }
                if (!compatMode)
                {
                    foreach (DataRow dr in builder.OutArgDataTable.Rows)
                    {
                        if ((string)dr["Name"] == kvp.Key)
                            dr["Value"] = kvp.Value;
                    }
                }
            }
        }

        private object GetReturnValue()
        {
            string strValue; // The string value can be parsed by 'Parse' method.
            DataTable dt = builder.OutArgDataTable;
            object retVal = null;
            if (builder.HasReturnVal)
            {
                if (dt.Rows[0][1] is DBNull)
                {
                    throw new InvalidOperationException(
                        string.Format("The {0} return value is expected.", builder.RetValType));
                }
                else
                {
                    strValue = (string)dt.Rows[0]["Value"];
                }
                retVal = AdapterProxyHelpers.ParseResult(builder.RetValType, strValue);
            }
            return retVal;
        }

        private void GetOutArgumentsValues()
        {
            string strValue; // The string value can be parsed by 'Parse' method.
            DataTable dt = builder.OutArgDataTable;
            int count = dt.Rows.Count;
            int i = builder.HasReturnVal ? 1 : 0;
            int j = 0; // Indexing out argument position of the passed-in arguments.
            foreach (Type t in builder.OutArgTypes)
            {
                object o = (object)dt.Rows[i++][1];
                if (o is DBNull)
                {
                    strValue = String.Empty;
                }
                else
                {
                    strValue = (string)o;
                }
                outArgs[builder.OutArgIndexes[j]] = AdapterProxyHelpers.ParseResult(t, strValue);
                j++;
            }
        }

        private static KeyValuePair<string, string> ParseStatement(string input)
        {
            KeyValuePair<string, string> kvp = new KeyValuePair<string, string>();

            if (String.IsNullOrEmpty(input))
                return kvp;

            Regex re = new Regex("(?<variable>\\w+)=\"(?<value>[^\"]*)\"");

            Match m = re.Match(input);
            if (m.Success)
            {
                kvp = new KeyValuePair<string, string>(m.Result("${variable}"), m.Result("${value}"));
            }
            return kvp;
        }
    }
}

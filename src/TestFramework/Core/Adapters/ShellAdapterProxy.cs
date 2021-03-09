// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Data;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// A class which is used as proxy for constructing IAdapter of Shell script type
    /// and executing methods in IAdapter.
    /// </summary>
    public class ShellAdapterProxy : AdapterProxyBase
    {
        private string scriptDirectory;
        private ParameterDataBuilder builder;
        private string lastOutput;
        private StringBuilder errorMsg;

        /// <summary>
        /// Create an instance of the shell adapter.
        /// </summary>
        /// <typeparam name="T">The type of the adapter.</typeparam>
        /// <param name="scriptDirectory">The folder containing the script files.</param>
        /// <param name="typeToProxy">The type of the adapter.</param>
        /// <returns>The shell adapter</returns>
        public static T Wrap<T>(string scriptDirectory, Type typeToProxy) where T : IAdapter
        {
            object proxy = Create<T, ShellAdapterProxy>();
            ShellAdapterProxy self = (ShellAdapterProxy)proxy;

            AdapterProxyBase.SetParameters(self, typeToProxy);
            self.scriptDirectory = scriptDirectory.Replace("\\", "/");

            return (T)proxy;
        }

        /// <summary>
        /// Can be overridden by extenders to do special initialization code.
        /// Call base to ensure the test site is initialized.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <param name="args">The arguments the caller passed to the method.</param>
        /// <returns>The return value of the Initialize implementation.</returns>
        protected override object Initialize(MethodInfo targetMethod, object[] args)
        {
            base.Initialize(targetMethod, args);
            return this.ExecuteMethod(targetMethod, args);
        }

        /// <summary>
        /// Can be overridden by extenders to do special processing of Reset.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <returns>The return value of the Reset implementation.</returns>
        protected override object Reset(MethodInfo targetMethod)
        {
            return this.ExecuteMethod(targetMethod, null);
        }

        /// <summary>
        /// Proxy method for substitution of executing methods in adapter interface.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <param name="args">The arguments the caller passed to the method.</param>
        /// <returns>The return value of the ExecuteMethod implementation.</returns>
        protected override object ExecuteMethod(MethodInfo targetMethod, object[] args)
        {
            //get help message from attribute
            string methodhelp = AdapterProxyHelpers.GetHelpMessage(targetMethod);

            bool compactMode = ((targetMethod.Name == "Initialize" || targetMethod.Name == "Reset")
                && AdapterType.IsAdapterTypeFullName(targetMethod.DeclaringType.FullName)
                );

            if (compactMode)
                return ExecuteMethodCompact(targetMethod, methodhelp);

            // Build parameter on each invoke
            builder = new ParameterDataBuilder(targetMethod);
            builder.Build(args);

            lastOutput = null;
            errorMsg = new StringBuilder();

            object retVal = null;
            string arguments = BuildScriptArguments();

            // Check if this is a method from IAdapter. Any IAdapter methods should be ignored.
            if (!AdapterType.IsAdapterTypeFullName(targetMethod.DeclaringType.FullName)
                && (targetMethod.DeclaringType.FullName != typeof(IDisposable).FullName)
                )
            {
                TestSite.Log.Add(LogEntryKind.EnterAdapter,
                    "Shell adapter: {0}, method: {1}",
                    ProxyType.Name,
                    targetMethod.Name);

                try
                {
                    string path = LookupScript(targetMethod.Name);
                    if (path == null)
                    {
                        TestSite.Assume.Fail(
                            "Shell script file ({0}.sh) can not be found.",
                            targetMethod.Name);
                    }
                    else
                    {
                        int timeout = AdapterProxyHelpers.GetTimeout(targetMethod, int.Parse(TestSite.Properties["AdapterInvokeTimeout"]));

                        Task<int> invokeTask = Task.Run<int>(() =>
                        {
                            TestSite.Log.Add(LogEntryKind.Debug, $"Start to invoke shell {targetMethod.Name}.sh, timeout: {timeout}");
                            int invokeResult = InvokeScript(path, arguments);
                            TestSite.Log.Add(LogEntryKind.Debug, $"Complete execute shell {targetMethod.Name}.sh");

                            return invokeResult;
                        });

                        TimeSpan waiter = TimeSpan.FromMinutes(timeout);
                        if (invokeTask.Wait(waiter))
                        {
                            if (invokeTask.Result != 0)
                            {
                                string exceptionMessage = string.Format("Exception thrown when executing {0}.sh.\nExit code: {1}\nError message: {2}\n", targetMethod.Name, invokeTask.Result, errorMsg);
                                throw new InvalidOperationException(exceptionMessage);
                            }

                            if (builder.HasReturnVal)
                            {
                                retVal = AdapterProxyHelpers.ParseResult(builder.RetValType, lastOutput);
                            }
                        }
                        else
                        {
                            throw new TimeoutException($"Invoke adapater method timeout after wait {timeout} minutes.");
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
                        targetMethod.Name);
                }
            }
            return retVal;
        }

        /// <summary>
        /// Proxy method for substitution of executing Initialize/Reset methods in adapter interface.
        /// </summary>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <param name="helpMessage">The help message from the attribute</param>
        /// <returns>Always null.</returns>
        private object ExecuteMethodCompact(MethodInfo targetMethod, string helpMessage)
        {
            string path = LookupScript(targetMethod.Name);
            if (path != null)
            {
                TestSite.Log.Add(LogEntryKind.EnterAdapter,
                        "Shell adapter: {0}, method: {1}",
                        ProxyType.Name,
                        targetMethod.Name);

                try
                {
                    int scriptRet = InvokeScript(path, "");
                    if (scriptRet != 0)
                    {
                        TestSite.Assume.Fail(
                            "Script {0}.sh exited with non-zero code. Error code: {1}.",
                            targetMethod.Name,
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
                        targetMethod.Name);
                }
            }
            else
            {
                TestSite.Log.Add(LogEntryKind.Debug,
                        "Shell adapter: {0}, method: {1} not found, skipped.",
                        ProxyType.Name,
                        targetMethod.Name);
            }

            return null;
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

                // put argument inside single quotes to escape special charaters
                ret.Append(String.Format("\"{0}\"", value));
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
                    // For Linux/macOS, use the shell on current system
                    string wslPath = "/bin/bash";

                    // Detect current OS
                    bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    if (isWindows)
                    {
                        // For Windows, WSL is needed
                        string winDir = Environment.GetEnvironmentVariable("WINDIR");
                        if (!string.IsNullOrEmpty(winDir))
                        {
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
                        }
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
            if (e.Data != null && !String.IsNullOrEmpty(e.Data.Trim()))
            {
                TestSite.Log.Add(LogEntryKind.Comment, "STDOUT: {0}", e.Data);

                if (!String.IsNullOrEmpty(e.Data.Trim()))
                {
                    lastOutput = e.Data.Trim();
                }
            }
        }
    }

    internal class ParameterDataBuilder
    {
        private MethodInfo methodInfo;
        private List<string> inArgNames = new List<string>(); // in-arg names
        private List<OutArgs> outArgs = new List<OutArgs>(); // out-arg names
        private Type retValType; // The return value type
        private List<Type> inArgTypes = new List<Type>(); // in-arg types
        private List<Type> outArgTypes = new List<Type>(); // out-arg types
        private DataTable inArgDataTable; // in-arg data table
        private DataTable outArgDataTable; // out-arg data table
        private bool hasReturnVal;
        private bool hasInArg;
        private bool hasOutArg;
        private int[] outArgIndexes;

        # region Resulting properties
        /// <summary>
        /// The type of the method return value.
        /// </summary>
        public Type RetValType
        {
            get { return retValType; }
        }

        /// <summary>
        /// The types of the out-arguments.
        /// </summary>
        public List<Type> InArgTypes
        {
            get { return inArgTypes; }
        }

        /// <summary>
        /// The types of the out-arguments.
        /// </summary>
        public List<Type> OutArgTypes
        {
            get { return outArgTypes; }
        }

        /// <summary>
        /// The data table presenting the in-arguments.
        /// </summary>
        public DataTable InArgDataTable
        {
            get { return inArgDataTable; }
        }

        /// <summary>
        /// The data table presenting the out-arguments.
        /// </summary>
        public DataTable OutArgDataTable
        {
            get { return outArgDataTable; }
        }

        /// <summary>
        /// Indicates if the method has a return value.
        /// </summary>
        public bool HasReturnVal
        {
            get { return hasReturnVal; }
        }

        /// <summary>
        /// Indicates if the method has in-arguments
        /// </summary>
        public bool HasInArg
        {
            get { return hasInArg; }
        }

        /// <summary>
        /// Indicates if the method has out-arguments
        /// </summary>
        public bool HasOutArg
        {
            get { return hasOutArg; }
        }

        /// <summary>
        /// Indicates the out argument positions in the passed-in parameters.
        /// </summary>
        public int[] OutArgIndexes
        {
            get { return outArgIndexes; }
        }
        #endregion

        /// <summary>
        /// Initializes the builder instance.
        /// </summary>
        /// <param name="methodInfo">IMethodCallMessage from the adapter proxy containing the method information.</param>
        public ParameterDataBuilder(MethodInfo methodInfo)
        {
            this.methodInfo = methodInfo;
        }

        /// <summary>
        ///  Parses method information and creates corresponding data tables.
        /// </summary>
        public void Build(object[] args)
        {
            ParseArguments(args);
            CreateDataTable(args);
        }

        class OutArgs
        {
            public OutArgs(string name, string defaultValue, Type type)
            {
                Name = name;
                DefaultValue = defaultValue;
                Type = type;
            }
            public string Name { get; set; }
            public string DefaultValue { get; set; }
            public Type Type { get; set; }
        }

        private void ParseArguments(object[] args)
        {
            // Get input paramter names and output parameter names.
            ParameterInfo[] pis = methodInfo.GetParameters();

            List<int> oais = new List<int>();
            int i = 0;
            foreach (ParameterInfo pi in pis)
            {
                // in-arguments
                if (IsInArg(pi))
                {
                    inArgNames.Add(pi.Name);
                    inArgTypes.Add(pi.ParameterType);
                    hasInArg = true;
                }

                // out-arguments
                if (IsOutArg(pi))
                {
                    var defaultValue = pi.GetCustomAttributes(typeof(DefaultValueAttribute), false) as DefaultValueAttribute[];
                    var outArg = new OutArgs(pi.Name, null, pi.ParameterType);
                    outArg.DefaultValue = (defaultValue.Length > 0) ? defaultValue[0].DefaultValue : null;
                    outArgs.Add(outArg);
                    outArgTypes.Add(pi.ParameterType);
                    hasOutArg = true;
                    oais.Add(i);
                }
                i++; // arg position.
            }

            outArgIndexes = oais.ToArray();

            // return value
            if (methodInfo.ReturnType != typeof(void))
            {
                ParameterInfo pi = methodInfo.ReturnParameter;

                var outArg = new OutArgs("Return Value", null, pi.ParameterType);
                var defaultValue = methodInfo.GetCustomAttributes(typeof(DefaultValueAttribute), false) as DefaultValueAttribute[];
                outArg.DefaultValue = (defaultValue.Length > 0) ? defaultValue[0].DefaultValue : null;
                outArgs.Insert(0, outArg);
                retValType = pi.ParameterType;
                hasReturnVal = true;
            }

            // The input parameter count should equal the count of the method type.
            if (inArgNames.Count != args.Length)
            {
                throw new InvalidOperationException(
                    String.Format("Calling '{0}' with {1} input paramters which doesn't equal to expected count: {2}.",
                        methodInfo.Name,
                        args.Length,
                        inArgNames.Count));
            }
        }

        private void CreateDataTable(object[] args)
        {
            // Create the input parameter DataTable and bind to the DataGridView
            DataTable dt;
            DataRow dr;

            dt = NewDataTable();

            int i = 0;
            foreach (string name in inArgNames)
            {
                dr = dt.NewRow();
                // Set names in the first column
                dr[0] = name;
                // Set values in the second column
                dr[1] = args[i++].ToString();
                dt.Rows.Add(dr);
            }

            inArgDataTable = dt;

            // Create the output parameter DataTable and bind to the DataGridView
            dt = NewDataTable();

            dt.Columns[0].ReadOnly = true;

            foreach (var outArg in outArgs)
            {
                dr = dt.NewRow();
                // Set names in the first column
                dr[0] = outArg.Name;
                dr[1] = outArg.DefaultValue;
                dr[2] = outArg.Type;
                dt.Rows.Add(dr);
            }

            outArgDataTable = dt;
        }

        private static DataTable NewDataTable()
        {
            DataTable dt = new DataTable();
            dt.Locale = System.Globalization.CultureInfo.CurrentCulture;
            dt.Columns.Add("Name", typeof(String));
            dt.Columns.Add("Value", typeof(String));
            dt.Columns.Add("Type", typeof(Type));

            return dt;
        }

        private static bool IsInArg(ParameterInfo pi)
        {
            // In C#, ParameterInfo.IsIn is always false though it is an input parameter.
            // In: IsIn = false, IsOut = false, IsByRef = false.
            // Out: IsIn = false, IsOut = true, IsByRef = true.
            // Ref: IsIn = false, IsOut = false, IsByRef = true.
            return (
                pi.IsIn ||
                ((!pi.IsIn) && (!pi.IsOut)) ||
                (pi.ParameterType.IsByRef && !pi.IsOut) // Ref
                );
        }

        private static bool IsOutArg(ParameterInfo pi)
        {
            // In: IsIn = false, IsOut = false, IsByRef = false.
            // Out: IsIn = false, IsOut = true, IsByRef = true.
            // Ref: IsIn = false, IsOut = false, IsByRef = true.
            return (
                (!pi.IsIn && pi.IsOut) ||
                ((!pi.IsIn) && (!pi.IsOut) && pi.ParameterType.IsByRef) // Ref
                );
        }
    }
}
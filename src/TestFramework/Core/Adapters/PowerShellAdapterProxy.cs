// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Runspaces;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// A class which is used as proxy for constructing IAdapter of PowerShell script type
    /// and executing methods in IAdapter.
    /// </summary>
    public class PowerShellAdapterProxy : AdapterProxyBase
    {
        private string scriptDirectory;

        /// <summary>
        /// Create an instance of the powershell adapter.
        /// </summary>
        /// <typeparam name="T">The type of the adapter.</typeparam>
        /// <param name="scriptDirectory">The folder containing the script files.</param>
        /// <param name="typeToProxy">The type of the adapter.</param>
        /// <returns>The powershell adapter</returns>
        public static T Wrap<T>(string scriptDirectory, Type typeToProxy) where T : IAdapter
        {
            object proxy = Create<T, PowerShellAdapterProxy>();
            PowerShellAdapterProxy self = (PowerShellAdapterProxy)proxy;

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

            object retVal = null;
            object[] outArgs = args;

            // Check if this is a method from IAdapter. Any IAdapter methods should be ignored.
            if (!AdapterType.IsAdapterTypeFullName(targetMethod.DeclaringType.FullName)
                && (targetMethod.DeclaringType.FullName != typeof(IDisposable).FullName)
                )
            {
                TestSite.Log.Add(LogEntryKind.EnterAdapter,
                    "PowerShell adapter: {0}, method: {1}",
                    ProxyType.Name,
                    targetMethod.Name);

                try
                {
                    string path = LookupScript(targetMethod.Name);
                    if (path == null)
                    {
                        TestSite.Assume.Fail(
                            "PowerShell script file ({0}.ps1) can not be found.",
                            targetMethod.Name);
                    }
                    else
                    {
                        int timeout = AdapterProxyHelpers.GetTimeout(targetMethod, int.Parse(TestSite.Properties["AdapterInvokeTimeout"]));
                        Task<PSParameterBuilder> invokeTask = Task.Run<PSParameterBuilder>(() =>
                        {
                            TestSite.Log.Add(LogEntryKind.Debug, $"Start to invoke Script {targetMethod.Name}.ps1, timeout: {timeout}");
                            var invokeResult = InvokeScript(path, targetMethod, args, methodhelp);
                            TestSite.Log.Add(LogEntryKind.Debug, $"Complete execute Script {targetMethod.Name}.ps1");
                            return invokeResult;
                        });

                        TimeSpan waiter = TimeSpan.FromMinutes(timeout);

                        if (invokeTask.Wait(waiter))
                        {
                            PSParameterBuilder resultBuilder = invokeTask.Result;
                            if (resultBuilder != null)
                            {
                                retVal = resultBuilder.RetValue;

                                if (resultBuilder.OutArguments != null)
                                {
                                    int argsIndex = 0;
                                    int outArgsIndex = 0;
                                    foreach (ParameterInfo pi in targetMethod.GetParameters())
                                    {
                                        if (pi.ParameterType.IsByRef)
                                        {
                                            outArgs[argsIndex] = resultBuilder.OutArguments[outArgsIndex++];
                                        }
                                        argsIndex++;
                                    }
                                }

                                //clear builder
                                resultBuilder = null;
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
                    if (ex.InnerException != null)
                    {
                        ex = ex.InnerException;
                    }
                    TestSite.Log.Add(LogEntryKind.Debug, ex.ToString());
                    throw ex;
                }
                finally
                {
                    TestSite.Log.Add(LogEntryKind.ExitAdapter,
                        "PowerShell adapter: {0}, method: {1}",
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
                        "PowerShell adapter: {0}, method: {1}",
                        ProxyType.Name,
                        targetMethod.Name);

                try
                {
                    PSParameterBuilder builder = InvokeScript(path, targetMethod, null, helpMessage);
                    //should always return null
                    TestSite.Assert.IsNull(builder, "Compact mode should always return null");
                }
                catch (Exception ex)
                {
                    TestSite.Log.Add(LogEntryKind.Debug, ex.ToString());
                    throw;
                }
                finally
                {
                    TestSite.Log.Add(LogEntryKind.ExitAdapter,
                        "Power Shell adapter: {0}, method: {1}",
                        ProxyType.Name,
                        targetMethod.Name);
                }
            }
            else
            {
                TestSite.Log.Add(LogEntryKind.Debug,
                        "Power Shell adapter: {0}, method: {1} not found, skipped.",
                        ProxyType.Name,
                        targetMethod.Name);
            }

            return null;
        }

        #region private methods

        private string LookupScript(string methodname)
        {
            string dir = scriptDirectory;

            string foundFile = Path.GetFullPath(Path.Combine(dir, methodname + ".ps1"));
            if (File.Exists(foundFile))
            {
                return foundFile;
            }
            else
            {
                return null;
            }
        }


        private void SetPTFVariables(
            SessionStateProxy proxy)
        {
            //set all properties as variables
            foreach (string key in this.TestSite.Properties.AllKeys)
            {
                string propName = "PTFProp_" + key.Replace(".", "_");
                proxy.SetVariable(propName, this.TestSite.Properties[key]);
            }
        }

        private KeyValuePair<string, object> GetRetValueFromCollection(Collection<PSObject> returnedCollection)
        {
            if (returnedCollection.Count == 0)
            {
                throw new InvalidOperationException("Return value is not found, please return a value in your sript");
            }

            //get the returned object value, the type is PSObject
            //skip other output in pipeline, and treat the last value as return value.
            object realReturnValue = null;
            string realReturnType = null;

            for (int i = 0; i < returnedCollection.Count; i++)
            {
                PSObject psPipelineObject = returnedCollection[i];
                object realObject = null;
                string realType = null;
                string format = "PowerShell adapter pipeline element[{0}] type:'{1}' value:'{2}'";

                if (psPipelineObject != null)
                {
                    realObject = psPipelineObject.ImmediateBaseObject;
                    Collection<string> typeNames = psPipelineObject.TypeNames;
                    realType = typeNames[0];

                    this.TestSite.Log.Add(LogEntryKind.Comment, format, i + 1, realType, realObject);
                }
                else
                {
                    realObject = null;
                    realType = null;
                    this.TestSite.Log.Add(LogEntryKind.Comment, format, i + 1, "null", "null");
                }

                //treated the last pipeline value as return value;
                if (i == returnedCollection.Count - 1)
                {
                    realReturnType = realType;
                    realReturnValue = realObject;
                }
            }
            return new KeyValuePair<string, object>(realReturnType, realReturnValue);
        }

        private void CheckErrorsInPipeline(Pipeline pipeline)
        {
            PipelineReader<object> pErrors = pipeline.Error;

            if (pErrors != null)
            {
                int errorCount = pErrors.Count;

                if (errorCount > 0)
                {
                    Collection<object> errors = pErrors.Read(errorCount);

                    if (errors == null)
                    {
                        throw new InvalidOperationException(
                            "Invoke Read method from System.Management.Automation.Runspaces.PipelineReader<T> fail.");
                    }

                    foreach (object error in errors)
                    {
                        object errorRecordInstance = (error as PSObject).ImmediateBaseObject;

                        if (errorRecordInstance != null)
                        {
                            object invocationInfoInstance = (errorRecordInstance as ErrorRecord).InvocationInfo;

                            if (invocationInfoInstance != null)
                            {
                                string positionMessage = (invocationInfoInstance as InvocationInfo).PositionMessage;
                                TestSite.Log.Add(LogEntryKind.CheckFailed, "PowerShell script write error '{0}' {1}", error, positionMessage);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Invokde script by given file path and arguments.
        /// </summary>
        /// <param name="path">The file path to the cmd script.</param>
        /// <param name="targetMethod">The method the caller invoked.</param>
        /// <param name="args">The argument to be passed to the script.</param>
        /// <param name="helpMessage">The help message from the attribute.</param>
        /// <returns>The return value of script executation.</returns>
        private PSParameterBuilder InvokeScript(string path, MethodInfo targetMethod, object[] args, string helpMessage)
        {
            //the patameter builder to handle all parameters
            PSParameterBuilder builder = null;

            //use the Dot operation in PowerShell to make all variables can be accessed in whole runspace.
            string scriptContent = string.Format(". \"{0}\"", Path.GetFullPath(path));

            //call static method, and create the instance of runspace type
            Runspace runspace = RunspaceFactory.CreateRunspace();

            //open run space
            runspace.Open();

            //call runspace.CreatePipeline to create an instance of Pipeline
            Pipeline pipeline = runspace.CreatePipeline();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //set execution policy for Windows in order to load and run local script files
                pipeline.Commands.AddScript("Set-ExecutionPolicy -Scope Process RemoteSigned");
            }

            pipeline.Commands.AddScript(scriptContent);

            SessionStateProxy sessionStateProxy = runspace.SessionStateProxy;
            //set current location to the folder containing the script
            sessionStateProxy.Path.SetLocation(Path.GetDirectoryName(path));
            //set variables which can be used in PowerShell script
            SetPTFVariables(sessionStateProxy);

            //set all parameters as variables which can be used
            //by users directly in the PowerShell script
            builder = new PSParameterBuilder(targetMethod);
            builder.SetAllParametersAsVariables(sessionStateProxy, args, helpMessage);

            try
            {
                if (builder != null)
                {
                    //invoke script and get the return value and out/ref parameters
                    if (builder.HasRetValue)
                    {
                        Collection<PSObject> returnValueCollection = pipeline.Invoke();

                        //get return value object
                        KeyValuePair<string, object> retValue = GetRetValueFromCollection(returnValueCollection);

                        if (retValue.Value != null)
                        {
                            if (builder.RetType.IsInstanceOfType(retValue.Value))
                            {
                                builder.RetValue = retValue.Value;
                            }
                            else
                            {
                                throw new InvalidOperationException("The returned type is mismatched");
                            }
                        }
                        else
                        {
                            builder.RetValue = null;
                        }
                    }
                    else
                    {
                        pipeline.Invoke();
                    }

                    //get out parameters values
                    builder.GetAllOutParameterValues(
                        sessionStateProxy,
                        targetMethod.GetParameters().Length);
                }
                else
                {
                    pipeline.Invoke();
                }

                //check errors in the error pipeline
                CheckErrorsInPipeline(pipeline);
            }
            catch (RuntimeException ex)
            {
                string errorMessage = ex.Message;
                string traceInfo = ex.ErrorRecord.InvocationInfo.PositionMessage;
                string ptfAdFailureMessage = string.Format(
                    "Exception thrown in PowerShell Adapter: {0} {1}", errorMessage, traceInfo);
                throw new InvalidOperationException(ptfAdFailureMessage);
            }

            //close runspace and release resources
            runspace.Close();

            return builder;
        }

        #endregion
    }

    /// <summary>
    /// A class that stores parameter information of the invoked adapter method.
    /// This class is only for PowerShell adapter internal use.
    /// </summary>
    class PSParameterBuilder
    {
        private MethodInfo methodInfo;
        private Type retType;
        private object retValue;
        private List<string> outParameterNames;
        private object[] outArguments;
        private bool hasRetValue;

        /// <summary>
        /// Constructor of the PowerShell parameter builder class.
        /// </summary>
        /// <param name="methodInfo">Method call message</param>
        public PSParameterBuilder(
            MethodInfo methodInfo)
        {
            //initialize
            this.outParameterNames = new List<string>();
            this.methodInfo = methodInfo;

            //get return value information
            if (methodInfo.ReturnType != typeof(void))
            {
                retType = methodInfo.ReturnParameter.ParameterType;
                hasRetValue = true;
            }
            else
            {
                this.retType = typeof(void);
            }
        }

        #region properties

        /// <summary>
        /// Gets whether the invoked method has return value.
        /// </summary>
        public bool HasRetValue
        {
            get
            {
                return hasRetValue;
            }
        }

        /// <summary>
        /// Gets the return type.
        /// </summary>
        public Type RetType
        {
            get
            {
                return retType;
            }
        }

        /// <summary>
        /// Gets or sets the return value.
        /// </summary>
        public object RetValue
        {
            get
            {
                return retValue;
            }
            set
            {
                retValue = value;
            }
        }

        /// <summary>
        /// Gets all the out/ref parameters.
        /// </summary>
        public object[] OutArguments
        {
            get
            {
                return outArguments;
            }
        }

        #endregion

        /// <summary>
        /// Sets all parameters/properties as variables.
        /// </summary>
        /// <param name="proxy">SessionStateProxy instance.</param>
        /// <param name="args">The argument to be passed to the script.</param>
        /// <param name="helpMessage">he help message from the attribute.</param>
        internal void SetAllParametersAsVariables(SessionStateProxy proxy, object[] args, string helpMessage)
        {
            //set help message as variable
            proxy.SetVariable("PtfHelpMessage", helpMessage);

            //set all parameters as variables
            foreach (ParameterInfo pi in methodInfo.GetParameters())
            {
                string parameterName = pi.Name;
                object argumentValue = null;
                if (pi.ParameterType.IsByRef)
                {
                    if (pi.IsOut)
                    {
                        //sets all "out" parameters by default value
                        argumentValue = pi.DefaultValue;
                    }
                    else
                    {
                        argumentValue = args[pi.Position];
                    }

                    //stores all out/ref parameters
                    outParameterNames.Add(parameterName);
                }
                else
                {
                    argumentValue = args[pi.Position];
                }

                proxy.SetVariable(parameterName, argumentValue);
            }
        }

        /// <summary>
        /// Gets all the out/ref parameter values.
        /// </summary>
        /// <param name="proxy">SessionStateProxy instance</param>
        /// <param name="paramsLength">Length of parameters</param>
        internal void GetAllOutParameterValues(SessionStateProxy proxy, int paramsLength)
        {
            if (paramsLength > 0)
            {
                int outParamIndex = 0;
                if (outParameterNames.Count > 0)
                {
                    outArguments = new object[outParameterNames.Count];

                    foreach (string outParam in outParameterNames)
                    {
                        object outArgument = proxy.GetVariable(outParam);
                        if (outArgument != null)
                        {
                            if (outArgument.GetType().IsValueType)
                            {
                                outArguments[outParamIndex++] = outArgument;
                            }
                            else
                            {
                                outArguments[outParamIndex++] = ResolveObjectFromPSObject(outArgument);
                            }
                        }
                        else
                        {
                            outArguments[outParamIndex++] = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A helper method which resolve psobject to real object.
        /// </summary>
        /// <param name="psObjectInstance">psobject instance</param>
        /// <returns>Returns the real object</returns>
        internal static object ResolveObjectFromPSObject(object psObjectInstance)
        {
            if (psObjectInstance.GetType() != typeof(PSObject))
            {
                return psObjectInstance;
            }

            return (psObjectInstance as PSObject).ImmediateBaseObject;
        }
    }
}

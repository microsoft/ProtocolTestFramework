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
using System.Collections.ObjectModel;
using Microsoft.Win32;

namespace Microsoft.Protocols.TestTools
{
    class PsWrapperAdapterProxy : AdapterProxyBase
    {
        public PsWrapperAdapterProxy(string scriptFile, Type typeToProxy)
            : base(typeToProxy)
        {
            this.scriptFile = scriptFile;
        }

        private string scriptFile;

        protected override IMessage Initialize(IMethodCallMessage methodCall)
        {
            base.Initialize(methodCall);
            return this.Invoke(methodCall);
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
        /// <param name="helpMessage">The help message from the attribute</param>
        /// <returns>Always void.</returns>
        private IMessage InvokeCompact(IMethodCallMessage mcall, string helpMessage)
        {

            TestSite.Log.Add(LogEntryKind.Debug,
                    "Power Shell adapter: {0}, method: {1} not found, skipped.",
                    ProxyType.Name,
                    mcall.MethodName);

            ReturnMessage mret = new ReturnMessage(
                null,
                null,
                0,
                mcall.LogicalCallContext,
                mcall);
            return mret;
        }

        protected override IMessage Invoke(IMethodCallMessage methodCall)
        {
            //get help message from attribute
            string methodhelp = AdapterProxyHelpers.GetHelpMessage(methodCall);

            bool compactMode = ((methodCall.MethodName == "Initialize" || methodCall.MethodName == "Reset")
                && AdapterType.IsAdapterTypeFullName(methodCall.MethodBase.DeclaringType.FullName)
                );
            if (compactMode)
                return InvokeCompact(methodCall, methodhelp);

            object retVal = null;
            object[] outArgs = methodCall.Args;

            // Check if this is a method from IAdapter. Any IAdapter methods should be ignored.
            if (!AdapterType.IsAdapterTypeFullName(methodCall.MethodBase.DeclaringType.FullName)
                && (methodCall.MethodBase.DeclaringType.FullName != typeof(IDisposable).FullName)
                )
            {
                TestSite.Log.Add(LogEntryKind.EnterAdapter,
                    "Power Shell adapter: {0}, method: {1}",
                    ProxyType.Name,
                    methodCall.MethodName);

                try
                {
                    if (scriptFile == null || !File.Exists(scriptFile))
                    {
                        TestSite.Assume.Fail(
                            "The invoking Power Shell script file ({0}) can not be found.",
                            scriptFile);
                    }
                    else
                    {
                        PsWrapperParameterBuilder builder = InvokeScript(scriptFile, methodCall, methodhelp);
                        if (builder != null)
                        {
                            retVal = builder.RetValue;

                            if (builder.OutArguments != null)
                            {
                                int argsIndex = 0;
                                int outArgsIndex = 0;
                                foreach (ParameterInfo pi in methodCall.MethodBase.GetParameters())
                                {
                                    if (pi.ParameterType.IsByRef)
                                    {
                                        outArgs[argsIndex] = builder.OutArguments[outArgsIndex++];
                                    }
                                    argsIndex++;
                                }
                            }

                            //clear builder
                            builder = null;
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
                        "Power Shell adapter: {0}, method: {1}",
                        ProxyType.Name,
                        methodCall.MethodName);
                }
            }

            ReturnMessage mret = new ReturnMessage(
                retVal,
                (outArgs != null && outArgs.Length > 0) ? outArgs : null,
                (outArgs != null) ? outArgs.Length : 0,
                methodCall.LogicalCallContext,
                methodCall);
            return mret;
        }

        #region private methods

        private KeyValuePair<string, object> GetRetValueFromCollection(object returnedCollection, Type psObject)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;

            //get enumerator of the collection
            MethodInfo getEnumerator = returnedCollection.GetType().GetMethod("GetEnumerator", flag);
            object enumeratorValues = getEnumerator.Invoke(returnedCollection, null);
            List<object> values = new List<object>();
            MethodInfo moveNext = enumeratorValues.GetType().GetMethod("MoveNext", flag);

            //get all objects in the collection
            while ((bool)moveNext.Invoke(enumeratorValues, null))
            {
                values.Add(
                    enumeratorValues.GetType().InvokeMember(
                    "Current", BindingFlags.GetProperty, null, enumeratorValues, null)
                    );
            }

            if (values.Count == 0)
            {
                throw new InvalidOperationException("No return value is found, please return a value in your sript");
            }
            //get the returned object value, the type is PSObject
            //skip other output in pipeline, and treat the last value as return value.
            object realReturnValue = null;
            string realReturnType = null;
            for (int i = 0; i < values.Count; i++)
            {
                object psPipelineObject = values[i];
                object realObject = null;
                string realType = null;
                string format = "The PowerShell adapter pipeline element[{0}] type:'{1}' value:'{2}'";
                if (psPipelineObject != null)
                {
                    //get the real object from the PSObject
                    realObject = psObject.InvokeMember(
                        "ImmediateBaseObject", 
                        BindingFlags.GetProperty,
                        null,
                        psPipelineObject, 
                        null);

                    //get the real type informations from the PSObject
                    Collection<string> typeNames = 
                        ((Collection<string>)psObject.InvokeMember(
                            "TypeNames", 
                            BindingFlags.GetProperty, 
                            null,
                            psPipelineObject, 
                            null));
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
                if (i == values.Count - 1)
                {
                    realReturnType = realType;
                    realReturnValue = realObject;
                }
            }
            return new KeyValuePair<string, object>(realReturnType, realReturnValue);
        }

        private static Type GetPSType(Assembly sysMgmtAutoAssembly, string typeName)
        {
            Type type = sysMgmtAutoAssembly.GetType(typeName);

            if (type == null)
            {
                throw new InvalidOperationException(
                    string.Format("Cannot get the type '{0}' from assembly.", typeName)
                    );
            }

            return type;
        }

        private PsWrapperParameterBuilder SetPSVariables(
            IMethodCallMessage methodCall,
            Type sessionStateProxy,
            object proxyInstance,
            string helpMessage)
        {
            //get "SetVariable" method which will set all param as variable.
            MethodInfo methodSetVariable = sessionStateProxy.GetMethod(
                "SetVariable",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod,
                null,
                new Type[] { typeof(string), typeof(object) },
                null
                );

            if (methodSetVariable == null)
            {
                throw new InvalidOperationException("Cannot get 'SetVariable' method from SessionStateProxy.");
            }

            //set all properties and help message as variable 
            //which can be used in PowerShell script.
            //set help message as variable
            methodSetVariable.Invoke(proxyInstance, new object[] { "PtfHelpMessage", helpMessage });
            //set all properties as variables
            foreach (string key in this.TestSite.Properties.AllKeys)
            {
                string propName = "PTFProp" + key;
                methodSetVariable.Invoke(
                    proxyInstance,
                    new object[] { propName, this.TestSite.Properties[key] }
                    );
            }

            methodSetVariable.Invoke(
                proxyInstance,
                new object[] { "MethodName", methodCall.MethodName}
            );

            if (methodCall != null)
            {
                //set all parameters as variables which can be used 
                //by users directly in the PowerShell script
                PsWrapperParameterBuilder builder = new PsWrapperParameterBuilder(methodCall);
                builder.SetAllParametersAsVariables(methodSetVariable, proxyInstance);

                return builder;
            }

            return null;
        }

        private static string GetPSScriptPositionMessage(Assembly sysMgmtAutoAssembly, Exception innerException)
        {
            string positionMessage = null;
            BindingFlags flags = BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public;
            Type runtimeException = GetPSType(sysMgmtAutoAssembly, "System.Management.Automation.RuntimeException");
            try
            {
                if (runtimeException.IsInstanceOfType(innerException))
                {
                    object errorRecordInstance = runtimeException.GetProperty(
                        "ErrorRecord", flags).GetValue(innerException, null);
                    if (errorRecordInstance != null)
                    {
                        Type errorRecord = GetPSType(sysMgmtAutoAssembly, "System.Management.Automation.ErrorRecord");
                        object invocationInfoInstance = errorRecord.GetProperty(
                            "InvocationInfo", flags).GetValue(errorRecordInstance, null);
                        if (invocationInfoInstance != null)
                        {
                            Type invocationInfo = GetPSType(sysMgmtAutoAssembly, "System.Management.Automation.InvocationInfo");
                            positionMessage = (string)invocationInfo.GetProperty(
                                "PositionMessage", flags).GetValue(invocationInfoInstance, null);
                        }
                    }

                }
            }
            catch (TargetInvocationException)
            {
                //do nothing just skip it
            }
            return positionMessage;
        }

        private void CheckErrorsInPipeline(Assembly sysMgmtAutoAssembly, Type pipeline, object pipelineInstance)
        {
            BindingFlags pFlags = BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public;
            object pErrors = pipeline.InvokeMember("Error", pFlags, null, pipelineInstance, null);
            if (pErrors != null)
            {
                int errorCount = (int)pErrors.GetType().InvokeMember("Count", pFlags, null, pErrors, null);
                if (errorCount > 0)
                {
                    BindingFlags mFlag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;

                    Collection<object> errors = (Collection<object>)pErrors.GetType().InvokeMember(
                        "Read", mFlag, null, pErrors, new object[] { errorCount });

                    if (errors == null)
                    {
                        throw new InvalidOperationException(
                            "Invoke Read method from System.Management.Automation.Runspaces.PipelineReader<T> fail.");
                    }
                    Type psObject = GetPSType(sysMgmtAutoAssembly, "System.Management.Automation.PSObject");
                    Type errorRecord = GetPSType(sysMgmtAutoAssembly, "System.Management.Automation.ErrorRecord");
                    Type invocationInfo = GetPSType(sysMgmtAutoAssembly, "System.Management.Automation.InvocationInfo");
                    foreach (object error in errors)
                    {

                        object errorRecordInstance = psObject.InvokeMember(
                            "ImmediateBaseObject", pFlags, null, error, null);
                        if (errorRecordInstance != null)
                        {
                            object invocationInfoInstance = errorRecord.InvokeMember(
                                "InvocationInfo", pFlags, null, errorRecordInstance, null);
                            if (invocationInfoInstance != null)
                            {
                                string positionMessage = (string)invocationInfo.InvokeMember(
                                    "PositionMessage", pFlags, null, invocationInfoInstance, null);
                                TestSite.Log.Add(LogEntryKind.CheckFailed, "PowerShell script write error '{0}' {1}", error, positionMessage);
                            }
                        }
                    }
                }
            }
        }

        //use reflection to invoke PowerShell script via APIs in System.Management.Automation.dll
        private PsWrapperParameterBuilder InvokeScript(string path, IMethodCallMessage methodCall, string helpMessage)
        {
            //the patameter builder to handle all parameters
            PsWrapperParameterBuilder builder = null;
            Assembly sysMgmtAutoAssembly = null;

            try
            {
                sysMgmtAutoAssembly = Assembly.Load(PSConstant.SystemManagementAutomationAssemblyNameV3);
            }
            catch { }
            // If loading System.Management.Automation, Version=3.0.0.0 failed, try Version=1.0.0.0
            if (sysMgmtAutoAssembly == null)
            {
                try
                {
                    sysMgmtAutoAssembly = Assembly.Load(PSConstant.SystemManagementAutomationAssemblyNameV1);
                }
                catch
                {
                    throw new InvalidOperationException("Can not find system management automation assembly from GAC." +
                                                        "Please make sure your PowerShell installation is valid." +
                                                        "Or you need to reinstall PowerShell.");
                }
            }

            if (sysMgmtAutoAssembly != null)
            {
                //use the Dot operation in PowerShell to make all variables can be accessed in whole runspace.
                string scriptContent = string.Format(". \"{0}\"", Path.GetFullPath(path));

                BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;

                //call static method, and create the instance of runspace type
                Type runspaceFactory =
                    GetPSType(sysMgmtAutoAssembly, "System.Management.Automation.Runspaces.RunspaceFactory");

                object runspaceInstance = runspaceFactory.InvokeMember(
                    "CreateRunspace", BindingFlags.InvokeMethod, null, null, null);

                //open run space
                Type runspace =
                    GetPSType(sysMgmtAutoAssembly, "System.Management.Automation.Runspaces.Runspace");

                runspace.InvokeMember("Open", flag, null, runspaceInstance, null);

                //call runspace.CreatePipeline to create an instance of Pipeline
                Type pipeline =
                    GetPSType(sysMgmtAutoAssembly, "System.Management.Automation.Runspaces.Pipeline");

                object pipelineInstance = runspace.InvokeMember(
                    "CreatePipeline", flag, null, runspaceInstance, null);

                //get the Commands property of the pipeline instance
                object commandsInstance = pipeline.InvokeMember(
                    "Commands", BindingFlags.GetProperty, null, pipelineInstance, null);

                //add commands to invoke script
                Type commands =
                    GetPSType(sysMgmtAutoAssembly, "System.Management.Automation.Runspaces.CommandCollection");

                commands.InvokeMember(
                    "AddScript", flag, null, commandsInstance, new object[] { scriptContent });

                //get "SessionStateProxy" instance from runspace
                Type sessionStateProxy =
                    GetPSType(sysMgmtAutoAssembly, "System.Management.Automation.Runspaces.SessionStateProxy");

                object proxyInstance = runspace.InvokeMember(
                    "SessionStateProxy",
                    BindingFlags.GetProperty,
                    null,
                    runspaceInstance,
                    null);

                //set variables which can be used in PowerShell script
                builder = SetPSVariables(methodCall, sessionStateProxy, proxyInstance, helpMessage);

                try
                {
                    if (builder != null)
                    {
                        //invoke script and get the return value and out/ref parameters
                        if (builder.HasRetValue)
                        {
                            object returnValueCollection =
                                pipeline.InvokeMember("Invoke", flag, null, pipelineInstance, null);
                            Type psObject =
                                GetPSType(sysMgmtAutoAssembly, "System.Management.Automation.PSObject");

                            //get return value object
                            KeyValuePair<string, object> retValue =
                                this.GetRetValueFromCollection(returnValueCollection, psObject);

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
                            pipeline.InvokeMember("Invoke", flag, null, pipelineInstance, null);
                        }

                        //get out parameters values
                        builder.GetAllOutParameterValues(
                            sysMgmtAutoAssembly,
                            sessionStateProxy,
                            proxyInstance,
                            methodCall.MethodBase.GetParameters().Length);
                    }
                    else
                    {
                        pipeline.InvokeMember("Invoke", flag, null, pipelineInstance, null);
                    }

                    //check errors in the error pipeline
                    CheckErrorsInPipeline(sysMgmtAutoAssembly, pipeline, pipelineInstance);
                }
                catch (TargetInvocationException ex)
                {
                    string innerException = string.Empty;
                    string traceInfo = string.Empty;
                    Exception e = ex as Exception;
                    if (null != e.InnerException)
                    {
                        innerException = e.InnerException.Message;
                        traceInfo = GetPSScriptPositionMessage(sysMgmtAutoAssembly, e.InnerException);
                    }
                    string ptfAdFailureMessage = string.Format(
                        "Exception thrown. InnerException: {1} {2}",
                        path, innerException, traceInfo);
                    throw new InvalidOperationException(ptfAdFailureMessage);
                }

                //close runspace and release resources
                runspace.InvokeMember("Close", flag, null, runspaceInstance, null);

            }
            else
            {
                throw new InvalidOperationException("Can not find system management automation assembly from GAC." +
                                                    "Please make sure your PowerShell installation is valid." +
                                                    "Or you need to reinstall PowerShell.");
            }

            return builder;
        }

        #endregion
    }

    /// <summary>
    /// A class store parameter information of the invoked adapter method.
    /// This class is only for PowerShell adapter internal use.
    /// </summary>
    class PsWrapperParameterBuilder
    {
        private IMethodCallMessage methodCallMessage;
        private Type retType;
        private object retValue;
        private List<string> outParameterNames;
        private object[] outArguments;
        private bool hasRetValue;

        /// <summary>
        /// Constructor of the PowerShell parameter builder class.
        /// </summary>
        /// <param name="methodCall">Method call message</param>
        public PsWrapperParameterBuilder(
            IMethodCallMessage methodCall)
        {
            //initialize
            this.outParameterNames = new List<string>();
            this.methodCallMessage = methodCall;

            //get return value information
            MethodInfo mi = (MethodInfo)(methodCall.MethodBase);
            if (mi.ReturnType != typeof(void))
            {
                retType = mi.ReturnParameter.ParameterType;
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
        /// <param name="setVariable">SessionStateProxy.SetVariable method</param>
        /// <param name="proxyInstance">SessionStateProxy instance</param>
        internal void SetAllParametersAsVariables(MethodInfo setVariable, object proxyInstance)
        {
            int argIndex = 0;

            //set all parameters as variables
            foreach (ParameterInfo pi in methodCallMessage.MethodBase.GetParameters())
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
                        argumentValue = methodCallMessage.GetArg(argIndex);
                    }

                    //stores all out/ref parameters
                    outParameterNames.Add(parameterName);
                }
                else
                {
                    argumentValue = methodCallMessage.GetArg(argIndex);
                }

                //sessionStateProxy.SetVariable
                setVariable.Invoke(
                    proxyInstance,
                    new object[] { parameterName, argumentValue });

                argIndex++;
            }
        }

        /// <summary>
        /// Gets all the out/ref parameter values.
        /// </summary>
        /// <param name="sessionStateProxy">Type of SessionStateProxy</param>
        /// <param name="paramsLength">Length of parameters</param>
        /// <param name="proxyInstance">SessionStateProxy instance</param>
        /// <param name="sysMgmtAutoAssembly">System management automation assembly</param>
        internal void GetAllOutParameterValues(Assembly sysMgmtAutoAssembly, Type sessionStateProxy, object proxyInstance, int paramsLength)
        {
            if (paramsLength > 0)
            {
                MethodInfo methodGetVariable = sessionStateProxy.GetMethod(
                                "GetVariable",
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod,
                                null,
                                new Type[] { typeof(string) },
                                new ParameterModifier[] { new ParameterModifier(paramsLength) }
                                );

                if (methodGetVariable == null)
                {
                    throw new InvalidOperationException("Cannot get the 'GetVariable' method from SessionStateProxy.");
                }


                int outParamIndex = 0;
                if (outParameterNames.Count > 0)
                {
                    outArguments = new object[outParameterNames.Count];

                    //sessionStateProxy.GetVariable
                    foreach (string outParam in outParameterNames)
                    {
                        object outArgument =
                            methodGetVariable.Invoke(proxyInstance, new object[] { outParam });
                        if (outArgument != null)
                        {
                            if (outArgument.GetType().IsValueType)
                            {
                                outArguments[outParamIndex++] = outArgument;
                            }
                            else
                            {
                                outArguments[outParamIndex++] =
                                    ResolveObjectFromPSObject(sysMgmtAutoAssembly, outArgument);
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
        /// <param name="sysMgmtAutoAssembly">System management automation assembly</param>
        /// <param name="psObjectInstance">psobject instance</param>
        /// <returns>Returns the real object</returns>
        internal static object ResolveObjectFromPSObject(Assembly sysMgmtAutoAssembly, object psObjectInstance)
        {
            Type psObject = sysMgmtAutoAssembly.GetType("System.Management.Automation.PSObject");
            if (psObjectInstance.GetType() != psObject)
            {
                return psObjectInstance;
            }
            if (psObject == null)
            {
                throw new InvalidOperationException(
                    "Cannot get the type 'System.Management.Automation.PSObject' from assembly.");
            }

            BindingFlags flag = BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance;

            return psObject.InvokeMember("ImmediateBaseObject", flag, null, psObjectInstance, null);
        }
    }
}

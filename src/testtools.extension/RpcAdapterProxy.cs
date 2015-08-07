// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.InteropServices;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
using Microsoft.Protocols.TestTools;
using Microsoft.Protocols.TestTools.Messages.Marshaling;

using System.Reflection;
using System.Reflection.Emit;
using System.IO;


namespace Microsoft.Protocols.TestTools.Messages
{
    internal class RpcAdapterProxy : AdapterProxyBase
    {
        class RpcStub
        {
            internal MethodInfo proxyMethod;
            internal MethodBuilder stubBuilder;
            internal MethodInfo stubMethod;
            internal RpcParameter[] parameters;
            internal RpcParameter returnParameter;
        }

        struct RpcParameter
        {
            internal MarshalingDescriptor context;
            internal bool IsByRef;
            internal bool IsOut;
            internal Type nativeType;
            internal IRegion marshallingRegion;

            internal RpcParameter(ParameterInfo paramInfo,
                                  Type paramType,
                                  Type nativeType)
            {
                Type t = paramType;
                if (paramType.Name == "Nullable`1")
                {
                    t = paramType.GetGenericArguments()[0];
                }

                this.context = new MarshalingDescriptor(t, paramInfo);
                this.IsByRef = paramInfo.ParameterType.IsByRef;
                this.IsOut = paramInfo.IsOut;
                this.nativeType = nativeType;
                this.marshallingRegion = null;
            }

            internal void AllocateMemoryRegion(Marshaler marshaler, object value)
            {
                if (marshallingRegion != null)
                {
                    marshaler.MarkMemoryForDispose(this.marshallingRegion.NativeMemory);
                    this.marshallingRegion = null;
                }

                if (context.Type != typeof(void))
                {
                    int s = marshaler.GetSize(context, value);
                    IntPtr p = Marshal.AllocHGlobal(s);
                    this.marshallingRegion = marshaler.MakeRegion(p, s);
                }
                else
                    this.marshallingRegion = null;
            }

            unsafe internal object Get()
            {
                switch (NativeTypeFullName)
                {
                    case "System.Byte":
                        return *(byte*)marshallingRegion.NativeMemory.ToPointer();
                    case "System.SByte":
                        return *(sbyte*)marshallingRegion.NativeMemory.ToPointer();
                    case "System.Int16":
                        return *(short*)marshallingRegion.NativeMemory.ToPointer();
                    case "System.UInt16":
                        return *(ushort*)marshallingRegion.NativeMemory.ToPointer();
                    case "System.Int32":
                        return *(int*)marshallingRegion.NativeMemory.ToPointer();
                    case "System.UInt32":
                        return *(uint*)marshallingRegion.NativeMemory.ToPointer();
                    case "System.Int64":
                        return *(long*)marshallingRegion.NativeMemory.ToPointer();
                    case "System.UInt64":
                        return *(ulong*)marshallingRegion.NativeMemory.ToPointer();
                    case "System.IntPtr":
                        return *(IntPtr*)marshallingRegion.NativeMemory.ToPointer();
                    case "System.Single":
                        return *(float*)marshallingRegion.NativeMemory.ToPointer();
                    case "System.Double":
                        return *(double*)marshallingRegion.NativeMemory.ToPointer();
                    case "System.Boolean":
                        return *(bool*)marshallingRegion.NativeMemory.ToPointer();
                    case "System.Char":
                        return *(char*)marshallingRegion.NativeMemory.ToPointer();
                    default:
                        throw new InvalidOperationException(String.Format("unexpected native RPC parameter type '{0}'",
                                                                NativeTypeFullName));
                }
            }

            unsafe internal void Set(object value)
            {
                switch (NativeTypeFullName)
                {
                    case "System.Byte":
                        *(byte*)marshallingRegion.NativeMemory.ToPointer() = (byte)value;
                        break;
                    case "System.SByte":
                        *(sbyte*)marshallingRegion.NativeMemory.ToPointer() = (sbyte)value;
                        break;
                    case "System.Int16":
                        *(short*)marshallingRegion.NativeMemory.ToPointer() = (short)value;
                        break;
                    case "System.UInt16":
                        *(ushort*)marshallingRegion.NativeMemory.ToPointer() = (ushort)value;
                        break;
                    case "System.Int32":
                        *(int*)marshallingRegion.NativeMemory.ToPointer() = (int)value;
                        break;
                    case "System.UInt32":
                        *(uint*)marshallingRegion.NativeMemory.ToPointer() = (uint)value;
                        break;
                    case "System.Int64":
                        *(long*)marshallingRegion.NativeMemory.ToPointer() = (long)value;
                        break;
                    case "System.UInt64":
                        *(ulong*)marshallingRegion.NativeMemory.ToPointer() = (ulong)value;
                        break;
                    case "System.IntPtr":
                        *(IntPtr*)marshallingRegion.NativeMemory.ToPointer() = (IntPtr)value;
                        break;
                    case "System.Single":
                        *(float*)marshallingRegion.NativeMemory.ToPointer() = (float)value;
                        break;
                    case "System.Double":
                        *(double*)marshallingRegion.NativeMemory.ToPointer() = (double)value;
                        break;
                    case "System.Boolean":
                        *(bool*)marshallingRegion.NativeMemory.ToPointer() = (bool)value;
                        break;
                    case "System.Char":
                        *(char*)marshallingRegion.NativeMemory.ToPointer() = (char)value;
                        break;
                    default:
                        throw new InvalidOperationException(String.Format("unexpected native RPC parameter type '{0}'",
                                                                NativeTypeFullName));
                }
            }


            string NativeTypeFullName
            {
                get
                {
                    string fullname;
                    if (nativeType.IsEnum)
                    {
                        fullname = Enum.GetUnderlyingType(nativeType).FullName;
                    }
                    else
                    {
                        fullname = nativeType.FullName;
                    }

                    return fullname;
                }
            }

            internal bool HasDynamicExpression
            {
                get
                {
                    if (this.context.Attributes.IsDefined(typeof(SizeAttribute), false)
                        || this.context.Attributes.IsDefined(typeof(LengthAttribute), false)
                        || this.context.Attributes.IsDefined(typeof(SwitchAttribute), false))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        Dictionary<MethodBase, RpcStub> stubs = new Dictionary<MethodBase, RpcStub>();
        Marshaler msr;
        MethodBase getHandleMethod;
        MethodBase setHandleMethod;
        bool handleImplicit;
        string handle;
        Type proxiedType;
        bool initialized;
        private readonly object initializeLock = new object();

        // Memory free method
        MethodInfo rpcFreeMethod;

        // The site field is used by messageUtils, so we sould exclude the fxcop warning.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        ITestSite site;

        MessageUtils messageUtils;
        bool needAutoValidate = true;
        static CallingConvention callingConvention = CallingConvention.Winapi; //by default
        static CharSet charset = CharSet.Auto; //by default

        public RpcAdapterProxy(Type typeToProxy, ITestSite site)
            : base(typeToProxy)
        {

            this.proxiedType = typeToProxy;
            this.site = site;
            this.messageUtils = new MessageUtils(site, Marshaling.NativeMarshalingConfiguration.Configuration);

            RpcAdapterConfig rpcConfig = site.Config.GetAdapterConfig(typeToProxy.Name) as RpcAdapterConfig;
            if (rpcConfig == null)
            {
                throw new InvalidOperationException(String.Format("cannot get adapter config for type '{0}'", typeToProxy.Name));
            }
            this.needAutoValidate = rpcConfig.NeedAutoValidate;


            callingConvention = rpcConfig.CallingConvention;
            charset = rpcConfig.Charset;

            if (!typeof(IRpcAdapter).IsAssignableFrom(proxiedType))
                throw new InvalidOperationException(String.Format("adapter type '{0}' is not an rpc adapter", typeToProxy));

            if (typeof(IRpcImplicitHandleAdapter).IsAssignableFrom(proxiedType))
            {
                getHandleMethod = typeof(IRpcImplicitHandleAdapter).GetMethod("get_Handle");
                setHandleMethod = typeof(IRpcImplicitHandleAdapter).GetMethod("set_Handle");
                handleImplicit = true;
            }

        }

        static void CopyStub(string sourceFile, string directory, string fileName)
        {
            string tempFile = string.Empty;
            try
            {
                tempFile = Path.Combine(directory, fileName);
                File.Copy(sourceFile, tempFile);
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException(e.Message);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        static string TryGetTempFileName()
        {
            string tempFileName;
            try
            {
                tempFileName = Path.GetTempFileName();
                if (string.IsNullOrEmpty(tempFileName))
                {
                    tempFileName = string.Empty;
                }
            }
            catch (IOException)
            {
                tempFileName = string.Empty;
            }

            return tempFileName;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")]
        void Initialize()
        {
            lock (initializeLock)
            {
                if (initialized)
                    return;
                initialized = true;

                if (handleImplicit && handle == null)
                {
                    // retrieve handle default value
                    string handlePropertyName = proxiedType.Name + ".handle";
                    handle = TestSite.Properties[handlePropertyName];
                    if (handle == null)
                        throw new InvalidOperationException(
                            String.Format("RPC adapter with implicit handle passing has undefined handle. Set property '{0}' in the configuration.",
                                            handlePropertyName));
                }

                string dllPropertyName = proxiedType.Name + ".dllimport";
                string stubDllName = TestSite.Properties[dllPropertyName];
                if (stubDllName == null)
                    stubDllName = proxiedType.Name.ToLower() + "_rpcstubs.dll";

                string stubDllNameWithoutExtension =
                    System.IO.Path.GetFileNameWithoutExtension(stubDllName)
                    + "_" + Guid.NewGuid();

                string dllName = stubDllNameWithoutExtension + ".dll";

                msr = new Marshaler(site, NativeMarshalingConfiguration.Configuration);

                AppDomain currentDomain = AppDomain.CurrentDomain;
                string assName = "pinvoke_" + stubDllNameWithoutExtension;
                string fileName = string.Empty;
                string tempFileName = TryGetTempFileName();
                bool useTempFile = false;
                if (!string.IsNullOrEmpty(tempFileName))
                {
                    useTempFile = true;
                    fileName = Path.GetFileName(tempFileName);
                }
                else
                {
                    fileName = assName + ".dll";
                    tempFileName = fileName;
                }

                //Copy the stub dll into the same directory as the pinvoke assembly.
                string tempDir = Path.GetDirectoryName(tempFileName);
                CopyStub(stubDllName, tempDir, dllName);

                AssemblyName assemblyName = new AssemblyName(assName);
                AssemblyBuilder assemblyBuilder = null;
                if (useTempFile)
                {
                    assemblyBuilder = currentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save, Path.GetTempPath());
                }
                else
                {
                    assemblyBuilder = currentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save);
                }
                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assName, fileName);
                TypeBuilder typeBuilder = moduleBuilder.DefineType(assName);

                foreach (MethodInfo method in proxiedType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    ParameterInfo[] paramInfos = method.GetParameters();
                    RpcStub stub = new RpcStub();
                    Type[] stubParameterTypes = new Type[paramInfos.Length];
                    stub.parameters = new RpcParameter[paramInfos.Length];
                    stub.proxyMethod = method;
                    for (int i = 0; i < paramInfos.Length; i++)
                    {
                        ParameterInfo paramInfo = paramInfos[i];
                        Type proxyType = paramInfo.ParameterType;
                        Type nativeType;
                        if (proxyType.IsByRef)
                        {
                            proxyType = proxyType.GetElementType();
                            nativeType = typeof(IntPtr);
                        }
                        else
                        {
                            nativeType = GetNativeType(paramInfo, proxyType);
                        }
                        stubParameterTypes[i] = nativeType;
                        stub.parameters[i] = new RpcParameter(paramInfo, proxyType, nativeType);
                    }
                    stub.returnParameter =
                        new RpcParameter(method.ReturnParameter, method.ReturnType,
                                         GetNativeType(method.ReturnParameter, method.ReturnType));

                    if (handleImplicit)
                    {
                        List<Type> stubParametersTypeList = new List<Type>(stubParameterTypes);
                        stubParametersTypeList.Insert(0, typeof(IntPtr));
                        stubParameterTypes = stubParametersTypeList.ToArray();
                    }
                    stub.stubBuilder =
                        typeBuilder.DefinePInvokeMethod(
                            method.Name, dllName,
                            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl,
                            CallingConventions.Standard,
                            stub.returnParameter.nativeType, stubParameterTypes,
                            callingConvention, charset);
                    stub.stubBuilder.SetImplementationFlags(MethodImplAttributes.PreserveSig);
                    stubs[method] = stub;

                }

                typeBuilder.CreateType();

                // Build types for memory management types.
                string UserRpcMemMgmtTypeName = "UserRpcMemMgmt";
                string userFreeMethodName = "MIDL_user_free";

                TypeBuilder UserRpcMemMgmtBuilder =
                    moduleBuilder.DefineType(UserRpcMemMgmtTypeName);
                BuildMemoryManagementType(UserRpcMemMgmtBuilder, typeof(UserRpcMemoryManagement), dllName);

                // Save and reload the pinvoke assembly
                try
                {
                    assemblyBuilder.Save(fileName);
                }
                catch (IOException e)
                {
                    throw new InvalidOperationException(
                            String.Format("cannot create P/Invoke stub assembly {0}: {1}", fileName, e.Message));
                }
                Assembly reloaded = null;
                if (useTempFile)
                {
                    reloaded = Assembly.LoadFrom(tempFileName);
                }
                else
                {
                    reloaded = Assembly.LoadFrom(fileName);
                }
                Type stubType = reloaded.GetType(assName);
                foreach (RpcStub stub in stubs.Values)
                {
                    stub.stubMethod = stubType.GetMethod(stub.proxyMethod.Name);
                }

                string fullDllName = dllName;
                if (useTempFile)
                {
                    fullDllName = Path.Combine(tempDir, dllName);
                }
                if (NativeMethods.HasMethod(fullDllName, userFreeMethodName))
                {
                    Type userRpcMemMgmtType = reloaded.GetType(UserRpcMemMgmtTypeName);
                    rpcFreeMethod = userRpcMemMgmtType.GetMethod(userFreeMethodName);
                }
            }
        }

        static Type BuildMemoryManagementType(TypeBuilder typeBuilder, Type memMgmtType, string dllName)
        {
            foreach (MethodInfo method in memMgmtType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                ParameterInfo[] paramInfos = method.GetParameters();
                Type[] parameterTypes = new Type[paramInfos.Length];
                for (int i = 0; i < paramInfos.Length; i++)
                {
                    parameterTypes[i] = paramInfos[i].ParameterType;
                }

                ParameterInfo returnParamInfo = method.ReturnParameter;
                MethodBuilder methodBuilder =
                    typeBuilder.DefinePInvokeMethod(
                    method.Name, dllName,
                    MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl,
                    CallingConventions.Standard,
                    returnParamInfo.ParameterType, parameterTypes,
                    callingConvention, charset);

                methodBuilder.SetImplementationFlags(MethodImplAttributes.PreserveSig);
            }


            return typeBuilder.CreateType();
        }

        Type GetNativeType(ParameterInfo paramInfo, Type type)
        {
            if (type == typeof(void))
                return type;

            MarshalingDescriptor desc = new MarshalingDescriptor(type, paramInfo);
            Type nativeType = msr.GetNativeType(desc);
            if (nativeType == null)
                msr.TestAssumeFail(desc,
                    "Unsupported native type representation. (You can use [Indirect] to pass a pointer.)",
                    paramInfo.Name, type);
            return nativeType;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        protected override IMessage Invoke(IMethodCallMessage mcall)
        {
            Marshaler marshaler = new Marshaler(site, NativeMarshalingConfiguration.Configuration);

            foreach (Type type in proxiedType.Assembly.GetTypes())
            {
                marshaler.DefineCustomType(type.Name, type);
            }

            if (mcall.MethodBase == getHandleMethod)
                return GetHandle(mcall);
            if (mcall.MethodBase == setHandleMethod)
                return SetHandle(mcall);

            Initialize();

            RpcStub stub;
            if (!stubs.TryGetValue(mcall.MethodBase, out stub))
                throw new InvalidOperationException(String.Format("cannot find stub for method '{0}'", mcall.MethodBase));

            // marshal parameters
            int n = stub.parameters.Length;
            int actualsOffs = handleImplicit ? 1 : 0;
            object[] actuals = new object[n + actualsOffs];
            if (handleImplicit)
            {
                if (handle == null)
                    marshaler.TestAssumeFail(
                        "handle undefined for rpc interface '{0}' with implicit handle passing",
                        proxiedType);
                IntPtr ptr = Marshal.StringToHGlobalUni(handle);
                marshaler.MarkMemoryForDispose(ptr);
                actuals[0] = ptr;
            }
            marshaler.EnterContext();
            for (int i = 0; i < n; i++)
            {
                if (!stub.parameters[i].IsOut)
                {
                    marshaler.DefineSymbol(mcall.GetArgName(i), mcall.Args[i]);
                }
            }

            ParameterInfo[] parameterInfos = mcall.MethodBase.GetParameters();
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < n; i++)
                {
                    if (pass == 0 && stub.parameters[i].HasDynamicExpression)
                    {
                        continue;
                    }

                    if (pass == 1
                        && !stub.parameters[i].HasDynamicExpression)
                    {
                        continue;
                    }

                    stub.parameters[i].AllocateMemoryRegion(marshaler, mcall.Args[i]);

                    RpcParameter rp = stub.parameters[i];
                    if (rp.marshallingRegion != null)
                        rp.marshallingRegion.TryReset();
                    if (!rp.IsOut
                        && mcall.Args[i] != null)
                    {
                        object value = mcall.Args[i];

                        // Validate the value of the parameter.
                        if (this.needAutoValidate)
                            CheckParameter(parameterInfos[i], value, mcall, marshaler.SymbolStore);

                        marshaler.MarshalInto(rp.context, rp.marshallingRegion, mcall.Args[i]);
                    }
                    else
                    {
                        marshaler.EnterRegion(rp.marshallingRegion);
                        marshaler.Clear(marshaler.GetSize(rp.context, null));
                        marshaler.ExitRegion();
                    }
                    if (!rp.IsByRef)
                    {
                        actuals[i + actualsOffs] = rp.Get();
                    }
                    else
                    {
                        if (mcall.Args[i] == null && !rp.IsOut)
                        {
                            actuals[i + actualsOffs] = IntPtr.Zero;
                        }
                        else
                        {
                            actuals[i + actualsOffs] = rp.marshallingRegion.NativeMemory;
                        }
                    }

                    marshaler.DefineSymbol(mcall.GetArgName(i), actuals[i + actualsOffs]);
                }
            }

            // call
            object result;
            try
            {
                result = stub.stubMethod.Invoke(null, actuals);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }

            // marshal output parameters
            object[] resultArgs = new object[n];

            marshaler.IsProbingUnmarshaling = true;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < n; i++)
                {
                    RpcParameter rp = stub.parameters[i];

                    if (rp.marshallingRegion != null)
                        rp.marshallingRegion.TryReset();
                    if (rp.IsByRef)
                    {
                        object res = resultArgs[i] =
                            marshaler.UnmarshalFrom(rp.context, rp.marshallingRegion);

                        if (marshaler.IsProbingUnmarshaling == false)
                        {
                            RpcAdapterValidate(res, rp.context.Attributes, marshaler.SymbolStore);

                            if (rp.IsOut && marshaler.GetNativeType(rp.context) == typeof(IntPtr))
                            {
                                foreach (IntPtr ptr in marshaler.ForeignMemory)
                                {
                                    if (ptr != IntPtr.Zero && rpcFreeMethod != null)
                                    {
                                        rpcFreeMethod.Invoke(null, new object[] { ptr });
                                    }
                                }
                            }
                        }
                        marshaler.ForeignMemory.Clear();
                        if (!rp.IsByRef)
                        {
                            marshaler.DefineSymbol(mcall.GetArgName(i), rp.Get());
                        }
                        else
                        {
                            marshaler.DefineSymbol(mcall.GetArgName(i),
                                rp.marshallingRegion.NativeMemory);
                        }
                    }
                    else
                    {
                        resultArgs[i] = mcall.Args[i];
                    }
                }
                marshaler.IsProbingUnmarshaling = false;
            }
            if (stub.returnParameter.nativeType != typeof(void))
            {
                stub.returnParameter.AllocateMemoryRegion(marshaler, result);

                stub.returnParameter.marshallingRegion.TryReset();
                stub.returnParameter.Set(result);
                result = marshaler.UnmarshalFrom(
                            stub.returnParameter.context,
                            stub.returnParameter.marshallingRegion);

                RpcAdapterValidate(result, null, null);
            }
            marshaler.ExitContext();
            marshaler.FreeMemory();
            marshaler.Dispose();
            marshaler = null;

            CheckOperation(mcall);
            ReturnMessage mret = new ReturnMessage(result, resultArgs, resultArgs.Length, mcall.LogicalCallContext, mcall);
            return mret;
        }

        private void CheckParameter(ParameterInfo parameterInfo, object parameterValue,
            IMethodCallMessage mcall, IDictionary<string, object> symbolStore)
        {
            Type type = parameterValue.GetType();

            if (!type.IsPrimitive)
            {
                RpcAdapterValidate(parameterValue, parameterInfo, symbolStore);
                return;
            }

            bool checkSuccess = false;
            if ((parameterInfo.IsDefined(typeof(PossibleValueAttribute), false)
                    || parameterInfo.IsDefined(typeof(PossibleValueRangeAttribute), false)))
            {
                Attribute[] attributes = 
                    (Attribute[])parameterInfo.GetCustomAttributes(typeof(Attribute), false);
                foreach (Attribute attribute in attributes)
                {
                    if (attribute is PossibleValueRangeAttribute && !checkSuccess)
                    {
                        checkSuccess =
                            MessageUtils.CheckValueByRange(parameterValue, (PossibleValueRangeAttribute)attribute);
                    }

                    if (attribute is PossibleValueAttribute && !checkSuccess)
                    {
                        checkSuccess = 
                            messageUtils.CheckValueByEnum(parameterValue, ((PossibleValueAttribute)attribute).EnumType);
                    }
                }
            }
            else
            {
                checkSuccess = true;
            }

            if (!checkSuccess)
            {
                throw new InvalidOperationException(
                           String.Format("Value of parameter '{0}' in method '{1}' is not one of its specified values",
                                        parameterInfo.Name, mcall.MethodBase));
            }
        }

        private void CheckOperation(IMethodCallMessage mcall)
        {
            Object[] methodAttributes = 
                ((MemberInfo)(mcall.MethodBase)).GetCustomAttributes(typeof(RequirementAttribute), true);

            foreach (Object methodAttibute in methodAttributes)
            {
                if (((System.Type)(((Attribute)methodAttibute).TypeId)) == typeof(RequirementAttribute))
                {
                    RequirementAttribute opAttribute = (RequirementAttribute)methodAttibute;
                    site.CaptureRequirement(opAttribute.ProtocolDocName, opAttribute.RequirementID, opAttribute.Description);
                }
            }
        }

        private void RpcAdapterValidate(object value, ICustomAttributeProvider provider, IDictionary<string, object> symbolStore)
        {
            if (this.needAutoValidate)
            {
                messageUtils.Validate(value, provider, symbolStore);
            }
        }

        override protected IMessage Initialize(IMethodCallMessage mcall)
        {
            IMessage result = base.Initialize(mcall);
            return result;
        }


        override protected IMessage Reset(IMethodCallMessage mcall)
        {
            if (msr != null)
            {
                msr.FreeMemory();
                Marshaler.Reset();
            }
            return base.Reset(mcall);
        }

        override protected IMessage Dispose(IMethodCallMessage mcall)
        {
            IMessage result;
            try
            {
                if (msr != null)
                {
                    msr.Dispose();
                }
            }
            finally
            {
                result = base.Dispose(mcall);
            }

            return result;
        }

        IMessage GetHandle(IMethodCallMessage mcall)
        {
            return new ReturnMessage(handle, null, 0, mcall.LogicalCallContext, mcall);
        }

        IMessage SetHandle(IMethodCallMessage mcall)
        {
            handle = (string)mcall.Args[0];
            return new ReturnMessage(null, null, 0, mcall.LogicalCallContext, mcall);
        }
    }

    interface UserRpcMemoryManagement
    {
        IntPtr MIDL_user_allocate(uint size);
        void MIDL_user_free(IntPtr pointer);
    }

    internal static class NativeMethods
    {
        [DllImport("kernel32")]
        internal extern static IntPtr LoadLibrary(string lpLibFileName);
        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal extern static bool FreeLibrary(IntPtr hLibModule);
        [DllImport("kernel32", CharSet = CharSet.Ansi)]
        internal extern static IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        internal static bool HasMethod(string dllName, string methodName)
        {
            IntPtr hModule = LoadLibrary(dllName);
            if (hModule == IntPtr.Zero)
                return false;
            IntPtr address = GetProcAddress(hModule, methodName);
            FreeLibrary(hModule);

            return address == IntPtr.Zero ? false : true;
        }
    }

}

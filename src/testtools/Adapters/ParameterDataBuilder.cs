// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.Data;
using System.Reflection;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// This class is used to parse invoking method information and covert them to
    /// corresponding data tables.
    /// </summary>
    internal class ParameterDataBuilder
    {
        private IMethodCallMessage methodCall; // Data passed in
        private List<string> inArgNames = new List<string>(); // in-arg names
        private List<OutArgs> outArgs = new List<OutArgs>(); // out-arg names
        private Type retValType; // The return value type
        private List<Type> inArgTypes = new List<Type>(); // in-arg types
        private List<Type> outArgTypes = new List<Type>(); // out-arg types
        private DataTable outArgDataTable; // out-arg data table
        private DataTable inArgDataTable; // in-arg data table
        bool hasReturnVal;
        bool hasInArg;
        bool hasOutArg;
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
        /// The data table presenting the out-arguments.
        /// </summary>
        public DataTable OutArgDataTable
        {
            get { return outArgDataTable; }
        }

        /// <summary>
        /// The data table presenting the in-arguments.
        /// </summary>
        public DataTable InArgDataTable
        {
            get { return inArgDataTable; }
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
        /// <param name="methodCall">IMethodCallMessage from the adapter proxy containing the method information.</param>
        public ParameterDataBuilder(IMethodCallMessage methodCall)
        {
            this.methodCall = methodCall;
        }

        /// <summary>
        ///  Parses method information and creates corresponding data tables.
        /// </summary>
        public void Build()
        {
            ParseArguments();
            CreateDataTable();
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

        private void ParseArguments()
        {
            // Get input paramter names and output parameter names.
            ParameterInfo[] pis = methodCall.MethodBase.GetParameters();

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
            MethodInfo mi = (MethodInfo)(methodCall.MethodBase);
            if (mi.ReturnType != typeof(void))
            {
                ParameterInfo pi = mi.ReturnParameter;

                var outArg = new OutArgs("Return Value", null, pi.ParameterType);
                var defaultValue = mi.GetCustomAttributes(typeof(DefaultValueAttribute), false) as DefaultValueAttribute[];
                outArg.DefaultValue = (defaultValue.Length > 0) ? defaultValue[0].DefaultValue : null;
                outArgs.Insert(0, outArg);
                retValType = pi.ParameterType;
                hasReturnVal = true;
            }

            // The input parameter count should equal the count of the method type.
            if (inArgNames.Count != methodCall.InArgCount)
            {
                throw new InvalidOperationException(
                    String.Format("Calling '{0}' with {1} input paramters which doesn't equal to expected count: {2}.",
                        methodCall.MethodName,
                        methodCall.InArgCount,
                        inArgNames.Count));
            }
        }

        private void CreateDataTable()
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
                dr[1] = methodCall.InArgs[i++].ToString();
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

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Protocols.TestTools.AdapterConsole;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Microsoft.Protocols.TestTools
{
    public class InteractiveAdapterConsole : IDisposable
    {
        private int returnValue;
        private object[] outArgs;
        private NameValueCollection properties;
        private ParameterDataBuilder builder;
        private List<ParaCommandInfo> paraCommands;

        /// <summary>
        /// Console Caption
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Helper Message
        /// </summary>
        public string HelperMessage { get; set; }

        /// <summary>
        /// Creates a new instance of InteractiveAdapterDialog class.
        /// </summary>
        /// <param name="methodCall">An IMessage that contains a IDictionary of information about the method call.</param>
        /// <param name="ptfProp">A NameValueCollection of settings from the test configuration file.</param>
        /// <param name="args">args.</param>
        public InteractiveAdapterConsole(MethodInfo methodCall, NameValueCollection ptfProp, object[] args)
        {
            if (methodCall == null)
            {
                throw new ArgumentNullException("methodCall");
            }

            if (ptfProp == null)
            {
                throw new ArgumentNullException("ptfProp");
            }

            // Stores all arguments for generating the 'OutArgs' property,
            // since ReturnMessage needs all arguments including in args.
            outArgs = methodCall.GetGenericArguments();

            // Change caption
            this.Text = methodCall.Name;

            // Set the help message
            this.HelperMessage = AdapterProxyHelpers.GetHelpMessage(methodCall);

            // Set the properties
            this.properties = ptfProp;

            // Set data grid views
            builder = BuildInputParamsCommand(methodCall, args);
            paraCommands = new List<ParaCommandInfo>();
        }

        private ParameterDataBuilder BuildInputParamsCommand(MethodInfo methodCall, object[] args)
        {
            ParameterDataBuilder builder = new ParameterDataBuilder(methodCall);
            builder.Build(args);

            // Bind to the out-arguments data
            if (builder.HasOutArg)
            {
                int index = 0;
                foreach (DataRow row in builder.OutArgDataTable.Rows)
                {
                    if (index == 0) // skip add Return Value
                        continue;

                    Type type = (Type)row[2];
                    if (type.IsByRef) type = type.GetElementType();

                    paraCommands.Add(new ParaCommandInfo()
                    {
                        Title = string.Format("Please enter value for {0} ({1})", row[0].ToString(), type.Name),
                        IsExecute = false,
                        Content = string.Empty,
                        ParameterName = row[0].ToString(),
                        ParameterIndex = index,
                        Type = type.ToString(),
                        Value = null
                    });

                    index++;
                }
            }
            return builder;
        }

        public int ProcessArguments()
        {
            ArgDetail consoleArg = BuildConsoleArgs();

            int consoleReturn = ConsoleHelper.PopNewConsole(consoleArg);
            if(consoleReturn != 0) //Process execute failed
            {
                throw new Exception($"Interactive console process execute failed, ErrorCode: {consoleReturn}");
            }
            else
            {
                ProcessResult result = JsonSerializer.Deserialize<ProcessResult>(File.ReadAllText(consoleArg.OutFilePath));
                if (result != null)
                {
                    returnValue = Convert.ToInt32(result.ReturnValue);
                    if (result.OutArgValues != null && result.OutArgValues.Count > 0)
                    {
                        int i = 0;
                        foreach (var outArg in result.OutArgValues)
                        {
                            outArgs[i] = outArg.Value;
                            i++;
                        }
                    }
                }
            }
            
            return returnValue;
        }

        private ArgDetail BuildConsoleArgs()
        {
            // Print Helper text
            StringBuilder consoleMsgBuilder = new StringBuilder();
            consoleMsgBuilder.AppendLine("Help Message:");
            consoleMsgBuilder.AppendLine(GetDividingLine());
            consoleMsgBuilder.AppendLine(this.HelperMessage);
            //Console.WriteLine(consoleMsgBuilder.ToString());
            consoleMsgBuilder.AppendLine(GetDividingLine());

            // Print Action Parameter (In Arguments)
            if (builder.InArgDataTable.Rows.Count > 0)
            {
                consoleMsgBuilder.AppendLine("Action Parameters:");
                consoleMsgBuilder.AppendLine(GetDividingLine());

                foreach (DataRow row in builder.InArgDataTable.Rows)
                {
                    consoleMsgBuilder.AppendLine(ConsoleHelper.GenerateLineRow(new string[] { "" + row[0], "" + row[1] }));
                }
                consoleMsgBuilder.AppendLine(GetDividingLine());
            }

            consoleMsgBuilder.AppendLine("Action Result:");
            consoleMsgBuilder.AppendLine(GetDividingLine());

            // output message, out parma, tempfile path
            ParaCommandInfo returnParam = null;
            int firstOutArgIndex = 0;
            string keyVaule = string.Empty;
            if (builder.HasReturnVal)
            {
                //returnValue = GetValueFromConsole("" + builder.OutArgDataTable.Rows[firstOutArgIndex][0], builder.RetValType);
                returnParam = new ParaCommandInfo()
                {
                    Content = "" + builder.OutArgDataTable.Rows[firstOutArgIndex][0],
                    ParameterIndex = 0,
                    ParameterName = "" + builder.OutArgDataTable.Rows[firstOutArgIndex][0],
                    Title = "Please enter [Y] when you're ready to perform interactive action or enter [N] to abort the case.",
                    Type = builder.RetValType.ToString(),
                };

                firstOutArgIndex = 1;
            }

            ArgDetail arg = new ArgDetail()
            {
                HelpMsg = consoleMsgBuilder.ToString(),
                OutFilePath = Path.GetTempFileName(),
                ReturnParam = returnParam,
                OutParams = paraCommands
            };
            return arg;
        }

        public List<ParaCommandInfo> GetParameters()
        {
            return this.paraCommands;
        }

        private const int BufferWidth = 80;
        private string GetDividingLine()
        {
            return new String('-', BufferWidth);
        }

        private int GetValueFromConsole(string prompt, Type type)
        {
            string keyVaule = ConsoleHelper.ReadFromConsole(prompt);
            try
            {
                var result = "" + AdapterProxyHelpers.ParseResult(type, keyVaule);
                if (string.IsNullOrEmpty(result) || result.Equals("0"))
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            catch (FormatException)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The return value is invalid, please input a valid value.");
                Console.ForegroundColor = originalColor;
                return GetValueFromConsole(prompt, type);
            }
        }

        public void Dispose()
        {
            if (builder != null && builder.InArgDataTable != null)
            {
                builder.InArgDataTable.Dispose();
            }

            if (builder != null && builder.OutArgDataTable != null)
            {
                builder.OutArgDataTable.Dispose();
            }
            builder.OutArgTypes.Clear();
            paraCommands.Clear();
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Microsoft.Protocols.TestTools.AdapterConsole
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                ProcessResult result = new ProcessResult();
                var arg = JsonSerializer.Deserialize<ArgDetail>(args[0]);
                ConsoleHelper.WriteToConsole(string.Empty); //write empty line
                ConsoleHelper.WriteToConsole(arg.HelpMsg);

                if (arg.ReturnParam != null)
                {
                    result.ReturnValue =""+ ConsoleHelper.GetValueFromConsole(arg.ReturnParam.Title, Type.GetType(arg.ReturnParam.Type));
                }

                if (arg.OutParams != null && arg.OutParams.Count > 0)
                {
                    result.OutArgValues = new System.Collections.Generic.Dictionary<string, string>();
                    foreach (var outarg in arg.OutParams)
                    {
                        var outValue = "" + ConsoleHelper.GetValueFromConsole($"Please enter {outarg.Title}", Type.GetType(outarg.Type));
                        if (!result.OutArgValues.ContainsKey(outarg.ParameterName))
                        {
                            result.OutArgValues.Add(outarg.ParameterName, outValue);
                        }
                        else
                        {
                            result.OutArgValues[outarg.ParameterName] = outValue;
                        }
                    }
                }

                string resultContent = JsonSerializer.Serialize<ProcessResult>(result);
                File.WriteAllText(arg.OutFilePath, resultContent, Encoding.UTF8);
            }
            else
            {
                return -1;
            }

            return 0;
        }
    }
}

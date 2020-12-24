// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Microsoft.Protocols.TestTools.AdapterConsole
{
    public static class ConsoleHelper
    {
        public static void WriteToConsole(string content)
        {
            Console.WriteLine(content);
        }

        public static string GenerateLineRow(string[] args, int cellLength = 10)
        {
            StringBuilder formatBuilder = new StringBuilder();

            foreach(var arg in args)
            {
                formatBuilder.AppendFormat("|{0," + cellLength + "}", arg);
            }
            return formatBuilder.ToString();
        }

        public static string ReadFromConsole(string promptContent = "", string @defaultReturn = "")
        {
            if (!promptContent.EndsWith(">"))
            {
                promptContent += "> ";
            }

            Console.Write(promptContent);
            KeyHandler keyHandler = new KeyHandler();
            string text = ReadConsoleText(keyHandler);

            if (string.IsNullOrWhiteSpace(text) && !String.IsNullOrWhiteSpace(@defaultReturn))
            {
                text = @defaultReturn;
            }

            return text;
        }

        private static string ReadConsoleText(KeyHandler keyHandler)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            while (keyInfo.Key != ConsoleKey.Enter)
            {
                keyHandler.Handle(keyInfo);
                keyInfo = Console.ReadKey(true);
            }

            Console.WriteLine();
            return keyHandler.Text;
        }

        // output message, out parma, tempfile path
        public static int PopNewConsole(ArgDetail consoleArg)
        {
            string paraJsonString = JsonSerializer.Serialize(consoleArg);
            using (Process process = new Process())
            {
                Assembly executeAssembly = Assembly.GetExecutingAssembly();
                string assemblyFolder = Path.GetDirectoryName(executeAssembly.Location);
                string consoleFileName = Path.Combine(assemblyFolder, $"{executeAssembly.GetName().Name}.dll");

                process.StartInfo.FileName = "dotnet";
                process.StartInfo.ArgumentList.Add(consoleFileName);
                process.StartInfo.ArgumentList.Add(paraJsonString);
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.UseShellExecute = true;
                process.Start();
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        public static object GetValueFromConsole(string prompt, Type type)
        {
            string keyVaule = ConsoleHelper.ReadFromConsole(prompt);
            try
            {
                return ConsoleHelper.ParseResult(type, keyVaule);
            }
            catch (FormatException)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The return value is invalid, please input a valid value. Required Type:"+type.ToString());
                Console.ForegroundColor = originalColor;
                return GetValueFromConsole(prompt, type);
            }
        }

        /// <summary>
        /// Parses the result and convert it to the corresponding type.
        /// </summary>
        /// <param name="type">The type of the result which should be converted to.</param>
        /// <param name="result">A string containing the name or value to convert. </param>
        /// <returns>Return parsed user input value </returns>
        internal static object ParseResult(Type type, string result)
        {
            if (result == null)
            {
                // Empty string should be accepted by 'Parse', hence only check null reference.
                throw new ArgumentNullException("result");
            }

            // Convert to non-ref types
            if (type.IsByRef)
            {
                type = type.GetElementType();
            }

            MethodInfo mi;
            // Specially processing String type.
            if (type == typeof(String))
            {
                // Ingore String type.
                return result;
            }
            // Specially processing Enum type.
            else if (type.IsAssignableFrom(typeof(Enum)) || type.IsEnum)
            {
                try
                {
                    return Enum.Parse(type, result, true);
                }
                catch (ArgumentException)
                {
                    throw new FormatException();
                }
            }
            else
            {
                // Check if T has a 'Parse' method.
                try
                {
                    mi = type.GetMethod(
                        "Parse",
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new Type[] { typeof(string) },
                        new ParameterModifier[0]
                        );
                }
                catch (AmbiguousMatchException e)
                {
                    throw new FormatException(
                        String.Format("More than one 'Parse' method is found in {0}.", type), e
                        );
                }
                if (mi == null)
                {
                    throw new FormatException(
                        String.Format(
                            "Can not parse the result, " +
                            "due to the type {0} doesn't contain a method 'public static {0} Parse (String)'.", type)
                        );
                }

                // Invoke and get the result.
                object res = null;
                try
                {
                    res = mi.Invoke(null, new object[] { result });
                }
                catch (TargetInvocationException e)
                {
                    if (e.InnerException != null && e.InnerException is FormatException)
                    {
                        throw e.InnerException;
                    }
                    else
                    {
                        throw;
                    }
                }

                return res;
            }
        }
    }
}

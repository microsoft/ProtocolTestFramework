// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml.Schema;
using System.Text;
using System.Collections;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Configuration data provider
    /// </summary>
    public static class ConfigurationDataProvider
    {
        private static ConfigurationReader reader;

        /// <summary>
        /// Gets the configuration data.
        /// </summary>
        /// <param name="ptfconfigDirectory">Ptfconfig directory</param>
        /// <param name="testSuiteName">Test suite name</param>
        /// <returns>The configuration data</returns>
        public static IConfigurationData GetConfigurationData(string ptfconfigDirectory, string testSuiteName)
        {
            // Get fullnames of PTF config files.

            string[] configFiles = GetConfigFileShortNames(testSuiteName);
            string[] configFileFullnames = new string[configFiles.Length];


            for (int i = 0; i < configFiles.Length; i++)
            {
                configFileFullnames[i] = GetTestSuitePtfconfigFileName(ptfconfigDirectory, configFiles[i]);
            }

            //site.ptfconfig is required.
            if (configFileFullnames[0] == null)
            {
                throw new InvalidOperationException("Cannot find site.ptfconfig. Please make sure it is placed in PTF installation directory.");
            }

            //<TestSuiteName>.ptfconfig is required.
            if (!File.Exists(Path.Combine(ptfconfigDirectory, String.Format("{0}.ptfconfig", testSuiteName))))
            {
                throw new InvalidOperationException(
                    String.Format("Cannot find {0}. Please make sure it is placed in {1}.",
                    testSuiteName + ".ptfconfig", ptfconfigDirectory));
            }

            reader = new ConfigurationReader(configFileFullnames);

            return reader;
        }

        /// <summary>
        /// Gets config file short names based on the given test suite name.
        /// </summary>
        /// <param name="testSuiteName">The test suite name</param>
        /// <returns>string array which contrains the names of configuration files</returns>
        private static string[] GetConfigFileShortNames(string testSuiteName)
        {
            return new string[]
            {
                "site.ptfconfig",
                testSuiteName + ".ptfconfig",
                testSuiteName + ".deployment.ptfconfig"
            };
        }

        /// <summary>
        /// Gets the full path name of the ptfconfig file name.
        /// </summary>
        /// <param name="ptfconfigDirectory">The path of ptfconfig directory.</param>
        /// <param name="filename">The configuration file short name.</param>
        /// <returns>The full path name of the ptfconfig file name.</returns>
        private static string GetTestSuitePtfconfigFileName(string ptfconfigDirectory, string filename)
        {
            string[] files = null;

            try
            {
                files = System.IO.Directory.GetFiles(ptfconfigDirectory, filename, SearchOption.TopDirectoryOnly);
            }
            catch (System.UnauthorizedAccessException) { }
            catch (System.ArgumentNullException) { }
            catch (System.IO.IOException) { }
            catch (System.ArgumentException) { }

            if (files != null && files.Length > 0)
                return files[0];
            else
                return null;
        }

        /// <summary>
        /// Tries to get the check the configuration data.
        /// </summary>
        /// <typeparam name="T">check configuration type.</typeparam>
        /// <param name="checkConfig">check configuration data.</param>
        /// <returns>Returns true if successfully get the check configuration data, otherwise return false.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static bool TryGetCheckerConfig<T>(out T checkConfig)
        {
            checkConfig = default(T);
            if (null != reader)
            {
                object value = reader;
                try
                {
                    checkConfig = (T)value;
                    return true;
                }
                catch
                {
                    //ignore the cast exception and return false.
                }
            }
            return false;
        }
    }
}

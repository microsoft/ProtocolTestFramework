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
        /// <summary>
        /// Registry Key path indicates PTF installation information entry.
        /// </summary>
        internal const string InstallRegistryKey = "SOFTWARE\\Microsoft\\ProtocolTestFramework";
        internal const string Wow64InstallRegistryKey = "SOFTWARE\\Wow6432Node\\Microsoft\\ProtocolTestFramework";
        private static ConfigurationReader reader;

        /// <summary>
        /// Gets the configuration XML file schema short name.
        /// </summary>
        private static string SchemaFileShortName
        {
            get
            {
                return "testconfig.xsd";
            }
        }

        /// <summary>
        /// Gets the configuration data.
        /// </summary>
        /// <param name="testDeploymentDirectory">Test development directory</param>
        /// <param name="testSuiteName">Test suite name</param>
        /// <returns>The configuration data</returns>
        public static IConfigurationData GetConfigurationData(string testDeploymentDirectory, string testSuiteName)
        {
            // Get fullnames of PTF config files.

            string[] configFiles = GetConfigFileShortNames(testSuiteName);
            string[] configFileFullnames = new string[configFiles.Length];


            for (int i = 0; i < configFiles.Length; i++)
            {
                configFileFullnames[i] = GetTestSuiteDeployConfigFileName(testDeploymentDirectory, configFiles[i]);
            }

            // If site.ptfconfig cannot be found in deployment directory, then try to load it from the installation directory.
            if (configFileFullnames[0] == null)
            {
                configFileFullnames[0] = GetInstallationConfigFileName(configFiles[0]);
            }

            //site.ptfconfig is required.
            if (configFileFullnames[0] == null)
            {
                throw new InvalidOperationException("Cannot find site.ptfconfig. Please make sure it is placed in PTF installation directory.");
            }

            //<TestSuiteName>.ptfconfig is required.
            if (!File.Exists(testDeploymentDirectory + "\\" + testSuiteName + ".ptfconfig"))
            {
                throw new InvalidOperationException(
                    String.Format("Cannot find {0}. Please make sure it is placed in test deployment directory {1}.",
                    testSuiteName + ".ptfconfig", testDeploymentDirectory));
            }

            string schema = GetTestSuiteDeployConfigFileName(testDeploymentDirectory, SchemaFileShortName);

            // If TestConfig.xsd cannot be found in deployment directory, then try to load it from the installation directory.
            if (schema == null)
            {
                // Finds the schema file which is used to validate the config files.
                schema = GetInstallationConfigFileName(SchemaFileShortName);

            }
            reader = new ConfigurationReader(configFileFullnames, schema);

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
        /// Gets the full path name of the deployment configuration file name.
        /// </summary>
        /// <param name="testDeploymentDirectory">The path of test suites deployment directory.</param>
        /// <param name="filename">The configuration file short name.</param>
        /// <returns>The full path name of the deployment configuration file name.</returns>
        private static string GetTestSuiteDeployConfigFileName(string testDeploymentDirectory, string filename)
        {
            string[] files = null;

            try
            {
                files = System.IO.Directory.GetFiles(testDeploymentDirectory, filename, SearchOption.TopDirectoryOnly);
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

        /// <summary>
        /// Gets config file based on the path of PTF installation directory which is specified by
        /// <see cref="ConfigurationDataProvider.InstallRegistryKey"/> or <see cref="ConfigurationDataProvider.Wow64InstallRegistryKey"/>.
        /// Gets config file based on the path of PTF installation directory which is specified by <see cref="ConfigurationDataProvider.InstallRegistryKey"/>.
        /// </summary>
        /// <param name="filename">The configuration file name.</param>
        /// <returns>Full path name of the given configuration file.</returns>
        private static string GetInstallationConfigFileName(string filename)
        {
            string[] files = null;

            /*
            RegistryKey hklm = null;
            string registryKey = InstallRegistryKey;
            if (Environment.Is64BitProcess)
            {
                registryKey = Wow64InstallRegistryKey;
            }

            try
            {
                hklm = Registry.LocalMachine.OpenSubKey(registryKey);
            }
            catch (System.Security.SecurityException e)
            {
                throw new InvalidOperationException("Can not open the Registry Key. Please make sure you have the authority to access Registry.", e);
            }
            catch (System.ArgumentException e)
            {
                throw new InvalidOperationException("Can not open the Registry Key. Please make sure your PTF installation is valid. " +
                                                    "Or you need to reinstall PTF.", e);
            }

            using (hklm)
            {
                if (hklm != null)
                {
                    try
                    {
                        string installDir = hklm.GetValue("InstallDir") as string;
                        if (string.IsNullOrEmpty(installDir))
                        {
                            throw new InvalidOperationException("Can not find PTF installation directory from Registry. " +
                                                    "Please make sure your PTF installation is valid. " +
                                                    "Or you need to reinstall PTF.");
                        }
                        files = System.IO.Directory.GetFiles(installDir, filename, SearchOption.TopDirectoryOnly);
                    }
                    catch (ArgumentException e)
                    {
                        throw new InvalidOperationException(String.Format("Can not find PTF config file: {0}.", filename), e);
                    }
                    catch (IOException e)
                    {
                        throw new InvalidOperationException(String.Format("Can not find PTF config file: {0}.", filename), e);
                    }
                }
                else
                {
                    throw new InvalidOperationException("Can not find PTF installation directory from Registry. " +
                                                    "Please make sure your PTF installation is valid. " +
                                                    "Or you need to reinstall PTF.");
                }
            }
            */

            if (files != null && files.Length > 0)
                return files[0];
            else
                return null;
        }
    }
}

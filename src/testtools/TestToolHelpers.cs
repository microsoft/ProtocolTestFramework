// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Globalization;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Provides a series of helper methods in test tool
    /// </summary>
    public static class TestToolHelpers
    {
        /// <summary>
        /// Resolves the Type with the specified name, performing a case-sensitive search 
        /// in the assemblies in the current AppDomain or specified assembly.
        /// </summary>
        /// <param name="nameOfType">The name of the assembly-qualified name of the Type. </param>
        /// <param name="assemblyLoadDir">Specified directory where to search the assembly.</param>
        /// <returns>The Type with the specified name, if found.</returns>
        /// <remarks>This method first searches the type in assemblies loaded in current AppDomain, then, if fails, 
        /// searches into the assembly specified in the assembly qualified type name.</remarks>
        internal static Type ResolveTypeFromAssemblies(string nameOfType, string assemblyLoadDir)
        {
            Type type = null;

            Assembly[] cands =AppDomain.CurrentDomain.GetAssemblies();
            
            try
            {
                // Resolves the requesting type from the assemblies in the current AppDomain. 
                foreach (Assembly assm in cands)
                {
                    type = assm.GetType(nameOfType, false);
                        
                    if (type != null)
                        break;
                }

                // Resolves the requesting type from the qualified assembly specified in "nameOfType" if
                // the type can not be resolved from the assemblies in the current AppDomain.
                if (type == null)
                {
                    type = Type.GetType(nameOfType, false);
                }

                if (type == null && !string.IsNullOrEmpty(assemblyLoadDir))
                {
                    type = ResolveTypeByLoadAssembly(nameOfType, assemblyLoadDir);
                }
            }
            catch (TypeLoadException e)
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.InvariantCulture, "The specified type {0} could not be resolved.", nameOfType),
                    e);
            }
            catch (ReflectionTypeLoadException e)
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.InvariantCulture, "The specified type {0} could not be resolved.", nameOfType),
                    e);
            }
            return type;
        }

        /// <summary>
        /// Resolves the type by search the assembly from specified directory.
        /// </summary>
        /// <param name="nameOfType">Assembly qualified name of the type.</param>
        /// <param name="assemblyLoadDir">Specified directory where to search the assembly.</param>
        /// <returns>The Type with the specified name, if found.</returns>
        private static Type ResolveTypeByLoadAssembly(string nameOfType, string assemblyLoadDir)
        {
            Type type = null;

            int index = nameOfType.IndexOf(',');
            if (index != -1)
            {
                string typeName = nameOfType.Substring(0, index);
                string adapterAssemblyName = nameOfType.Substring(index + 1).Trim();
                string path = Path.Combine(assemblyLoadDir, adapterAssemblyName + ".dll");
                if (File.Exists(path))
                {
                    try
                    {
                        AssemblyName assemblyName = AssemblyName.GetAssemblyName(path);
                        Assembly adapterAssm = Assembly.Load(assemblyName);
                        type = adapterAssm.GetType(typeName, false);
                    }
                    catch (ArgumentException e)
                    {
                        throw new InvalidOperationException(
                            String.Format(CultureInfo.InvariantCulture, "The specified type {0} could not be resolved.", nameOfType),
                            e);
                    }
                    catch (FileLoadException e)
                    {
                        throw new InvalidOperationException(
                              String.Format(CultureInfo.InvariantCulture, "The specified type {0} could not be resolved.", nameOfType),
                              e);
                    }
                    catch (TypeLoadException e)
                    {
                        throw new InvalidOperationException(
                              String.Format(CultureInfo.InvariantCulture, "The specified type {0} could not be resolved.", nameOfType),
                              e);
                    }
                }
                else
                {
                    //search current working folder only once.
                    if (assemblyLoadDir != Environment.CurrentDirectory)
                    {
                        type = ResolveTypeByLoadAssembly(nameOfType, Environment.CurrentDirectory);
                    }
                }                
            }

            return type;
        }

        /// <summary>
        /// Creates an instance of the specified type using the constructor that best matches the specified parameters.
        /// </summary>
        /// <param name="nameOfType">The type name of object to create. </param>
        /// <param name="args">An array of arguments that matches in number, order, and type the parameters 
        /// of the constructor to invoke.</param>
        /// <returns>A reference to the newly created object. </returns>
        internal static object CreateInstanceFromTypeName(string nameOfType, params object[] args)
        {
            Type type = ResolveTypeFromAssemblies(nameOfType, null);

            if (type == null)
            {
                return null;
            }

            object instance = Activator.CreateInstance(type, args);
            return instance;
        }

        /// <summary>
        /// Creates an instance of the specified type using the default constructor.
        /// </summary>
        /// <param name="nameOfType">The type name of object to create. </param>
        /// <returns>A reference to the newly created object. </returns>
        internal static object CreateInstanceFromTypeName(string nameOfType)
        {
            return CreateInstanceFromTypeName(nameOfType, null);
        }

        /// <summary>
        /// Get all types derived from the base type.
        /// </summary>
        /// <param name="mostDerivedType">The most derived type</param>
        /// <returns>Returns a set of derived types</returns>
        internal static IList<Type> GetAllDerivedTypes(Type mostDerivedType)
        {
            List<Type> derivedTypes = new List<Type>();
            derivedTypes.Add(mostDerivedType);
            Type currentType = mostDerivedType;
            while (currentType.BaseType != null)
            {
                derivedTypes.Add(currentType.BaseType);
                currentType = currentType.BaseType;
            }
            return derivedTypes;
        }

        /// <summary>
        /// Gets all methods in the decleared type which are marked with the specified attribute.
        /// </summary>
        /// <param name="attributeType">Attribute type which is used for finding methods.</param>
        /// <param name="declearedType">The type where methods declared.</param>
        /// <param name="inherit">True indicates that it needs to find methods also in base class.</param>
        /// <returns>Returns a collection of methods which are marked with the specified attribute.</returns>
        public static IList<MethodInfo> GetMethodsByAttribute(
            Type attributeType, Type declearedType, bool inherit)
        {
            IList<MethodInfo> methods = new List<MethodInfo>();
            IList<Type> derivedTypes = new List<Type>();
            if (inherit)
            {
                derivedTypes = GetAllDerivedTypes(declearedType);
            }
            else
            {
                derivedTypes.Add(declearedType);
            }

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            foreach (Type t in derivedTypes)
                foreach (MethodInfo mi in t.GetMethods(flags))
                {
                    Attribute attr = Attribute.GetCustomAttribute(mi, attributeType, false);
                    if (attr != null &&
                        attr.GetType() == attributeType &&
                        !methods.Contains(mi))
                    {
                        if (mi.GetParameters().Length != 0)
                        {
                            throw new InvalidOperationException(
                                "Test cleanup method cannot contain any parameters.");
                        }
                        else if (mi.ReturnType != typeof(void))
                        {
                            throw new InvalidOperationException(
                                "Test cleanup method cannot have return value.");
                        }
                        methods.Add(mi);
                    }
                }

            return methods;
        }

        /// <summary>
        /// Convert XML Boolean to .Net Boolean
        /// </summary>
        /// <param name="value">The boolean value in xml will be true, false, 1, 0</param>
        /// <returns>true or false</returns>
        public static bool XmlBoolToBool(string value)
        {
            bool ret = false;

            //tread "" as false.
            if (!string.IsNullOrEmpty(value))
            {
                if (string.Compare("true", value, true) == 0 ||
                    string.Compare("1", value, true) == 0)
                {
                    ret = true;
                }
            }
            
            return ret;
        }
    }
}

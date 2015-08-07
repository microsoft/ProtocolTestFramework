// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Net.Sockets;
using System.Net;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// A library class which provides additional convertor 
    /// to convert existing strings into values. 
    /// </summary>
    public class PtfOptionParser
    {
        private ITestSite site;
        private const string argumentNullException = 
            "The value of configured property \"{0}\" cannot be null.";
        private const string formatException =
            "The value \"{0}\" of property \"{1}\" cannot be converted to {2} type.";
        private const string overflowException =
            "The value \"{0}\" of property \"{1}\" is too large for {2} type.";
        /// <summary>
        /// Disable the default constructor 
        /// </summary>
        private PtfOptionParser()
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testSuiteName">The test suite name which is used to get test site.</param>
        public PtfOptionParser(string testSuiteName)
        {
            if (string.IsNullOrEmpty(testSuiteName))
            {
                throw new InvalidOperationException("The test suite name cannot be null or empty.");
            }
            if (string.IsNullOrEmpty(testSuiteName.Trim()))
            {
                throw new InvalidOperationException("The test suite name cannot be null or empty.");
            }
            site = TestSiteProvider.GetTestSite(testSuiteName.Trim());

            if (site == null)
            {
                throw new InvalidOperationException(
                    "The test site specified dose not exist, please check whether the name is correct.");
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">The test site which is used to log error message.</param>
        public PtfOptionParser(ITestSite site)
        {
            if (site == null)
            {
                throw new InvalidOperationException(
                    "Test site cannot be null.");
            }
            this.site = site;
        }

        /// <summary>
        /// Tries to get the value of property by specified property name.
        /// </summary>
        /// <param name="propertyName">Specified property name</param>
        /// <param name="value">Out parameter: return the value of property get from ptfconfig.</param>
        /// <returns>Returns true if the property value is successfully retrieved; otherwise, returns false.</returns>
        public bool TryGetPropertyByName(string propertyName, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(propertyName))
            {
                site.Log.Add(LogEntryKind.Debug,
                    "The property name cannot be null or empty," + 
                    " please provide a correct name which is configured in ptfconfig.");
                return false;
            }
            else
            {
                if (string.IsNullOrEmpty(propertyName.Trim()))
                {
                    site.Log.Add(LogEntryKind.Debug,
                        "Property name cannot be null or empty.");
                    return false;
                }

                value = site.Properties[propertyName.Trim()];

                if (string.IsNullOrEmpty(value))
                {
                    site.Log.Add(LogEntryKind.Debug,
                        "The property \"{0}\" does not exist or the value of the property is null.",
                        propertyName);
                    return false;
                }
            }

            return true;
        }

        #region convertor methods

        /// <summary>
        /// Gets the String value from configured property.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public String GetString(string propertyName, String dflt)
        {
            string value;
            if (TryGetPropertyByName(propertyName, out value))
            {
                return value;
            }

            return dflt;
        }

        /// <summary>
        /// Tries to parse the string value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetString(string propertyName, out String value)
        {
            return TryGetPropertyByName(propertyName, out value);
        }

        /// <summary>
        /// Gets the Int32 value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public Int32 GetInt32(string propertyName, Int32 dflt)
        {
            string int32String;
            if (TryGetPropertyByName(propertyName, out int32String))
            {
                try
                {
                    Int32 val = Int32.Parse(
                        int32String, 
                        System.Globalization.NumberStyles.Any, 
                        CultureInfo.CurrentCulture);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        int32String, propertyName, "Int32");
                }
                catch (OverflowException)
                {
                    site.Log.Add(LogEntryKind.Debug, overflowException, 
                        int32String, propertyName, "Int32");
                }
            }

            return dflt;
        }

        /// <summary>
        /// Tries to parse int32 value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetInt32(string propertyName, out Int32 value)
        {
            value = default(Int32);
            string int32String;

            if (!TryGetPropertyByName(propertyName, out int32String))
            {
                return false;
            }

            return Int32.TryParse(
                int32String, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// Gets the SByte value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public SByte GetSByte(string propertyName, SByte dflt)
        {
            string sbyteString;
            if (TryGetPropertyByName(propertyName, out sbyteString))
            {
                try
                {
                    SByte val = SByte.Parse(
                        sbyteString, NumberStyles.Any, CultureInfo.CurrentCulture);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        sbyteString, propertyName, "SByte");
                }
                catch (OverflowException)
                {
                    site.Log.Add(LogEntryKind.Debug, overflowException, 
                        sbyteString, propertyName, "SByte");
                }
            }

            return dflt;
        }

        /// <summary>
        /// Tries to parse SByte value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetSByte(string propertyName, out SByte value)
        {
            value = default(SByte);
            string sbyteString;

            if (!TryGetPropertyByName(propertyName, out sbyteString))
            {
                return false;
            }

            return SByte.TryParse(
                sbyteString, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// Gets the Int16 value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public Int16 GetInt16(string propertyName, Int16 dflt)
        {
            string int16String;
            if (TryGetPropertyByName(propertyName, out int16String))
            {
                try
                {
                    Int16 val = Int16.Parse(
                        int16String, NumberStyles.Any, CultureInfo.CurrentCulture);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        int16String, propertyName, "Int16");
                }
                catch (OverflowException)
                {
                    site.Log.Add(LogEntryKind.Debug, overflowException,
                        int16String, propertyName, "Int16");
                }
            }
            return dflt;
        }

        /// <summary>
        /// Tries to parse int16 value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetInt16(string propertyName, out Int16 value)
        {
            value = default(Int16);
            string int16String;

            if (!TryGetPropertyByName(propertyName, out int16String))
            {
                return false;
            }

            return Int16.TryParse(
                int16String, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// Gets the Int64 value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public Int64 GetInt64(string propertyName, Int64 dflt)
        {
            string int64String;
            if (TryGetPropertyByName(propertyName, out int64String))
            {
                try
                {
                    Int64 val = Int64.Parse(
                        int64String, NumberStyles.Any, CultureInfo.CurrentCulture);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        int64String, propertyName, "Int64");
                }
                catch (OverflowException)
                {
                    site.Log.Add(LogEntryKind.Debug, overflowException,
                        int64String, propertyName, "Int64");
                }
            }
            return dflt;
        }

        /// <summary>
        /// Tries to parse int64 value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetInt64(string propertyName, out Int64 value)
        {
            value = default(Int64);
            string int64String;

            if (!TryGetPropertyByName(propertyName, out int64String))
            {
                return false;
            }

            return Int64.TryParse(
                int64String, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// Gets the Byte value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public Byte GetByte(string propertyName, Byte dflt)
        {
            string byteString;
            if (TryGetPropertyByName(propertyName, out byteString))
            {
                try
                {
                    Byte val = Byte.Parse(
                        byteString, NumberStyles.Any, CultureInfo.CurrentCulture);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        byteString, propertyName, "Byte");
                }
                catch (OverflowException)
                {
                    site.Log.Add(LogEntryKind.Debug, overflowException,
                        byteString, propertyName, "Byte");
                }
            }
            return dflt;
        }

        /// <summary>
        /// Tries to parse Byte value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetByte(string propertyName, out Byte value)
        {
            value = default(Byte);
            string byteString;

            if (!TryGetPropertyByName(propertyName, out byteString))
            {
                return false;
            }

            return Byte.TryParse(
                byteString, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// Gets the UInt16 value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public UInt16 GetUInt16(string propertyName, UInt16 dflt)
        {
            string uint16String;
            if (TryGetPropertyByName(propertyName, out uint16String))
            {
                try
                {
                    UInt16 val = UInt16.Parse(
                        uint16String, System.Globalization.NumberStyles.Any, CultureInfo.CurrentCulture);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        uint16String, propertyName, "UInt16");
                }
                catch (OverflowException)
                {
                    site.Log.Add(LogEntryKind.Debug, overflowException,
                        uint16String, propertyName, "UInt16");
                }
            }
            return dflt;
        }

        /// <summary>
        /// Tries to parse Byte value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetUInt16(string propertyName, out UInt16 value)
        {
            value = default(UInt16);
            string uint16String;

            if (!TryGetPropertyByName(propertyName, out uint16String))
            {
                return false;
            }

            return UInt16.TryParse(
                uint16String, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// Gets the UInt32 value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public UInt32 GetUInt32(string propertyName, UInt32 dflt)
        {
            string uint32String;
            if (TryGetPropertyByName(propertyName, out uint32String))
            {
                try
                {
                    UInt32 val = UInt32.Parse(
                        uint32String, NumberStyles.Any, CultureInfo.CurrentCulture);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        uint32String, propertyName, "UInt32");
                }
                catch (OverflowException)
                {
                    site.Log.Add(LogEntryKind.Debug, overflowException,
                        uint32String, propertyName, "UInt32");
                }
            }
            return dflt;
        }

        /// <summary>
        /// Tries to parse UInt32 value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetUInt32(string propertyName, out UInt32 value)
        {
            value = default(UInt32);
            string uint32String;

            if (!TryGetPropertyByName(propertyName, out uint32String))
            {
                return false;
            }

            return UInt32.TryParse(
                uint32String, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// Gets the UInt64 value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public UInt64 GetUInt64(string propertyName, UInt64 dflt)
        {
            string uint64String;
            if (TryGetPropertyByName(propertyName, out uint64String))
            {
                try
                {
                    UInt64 val = UInt64.Parse(
                        uint64String, NumberStyles.Any, CultureInfo.CurrentCulture);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        uint64String, propertyName, "UInt64");
                }
                catch (OverflowException)
                {
                    site.Log.Add(LogEntryKind.Debug, overflowException,
                        uint64String, propertyName, "UInt64");
                }
            }
            return dflt;
        }

        /// <summary>
        /// Tries to parse UInt64 value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetUInt64(string propertyName, out UInt64 value)
        {
            value = default(UInt64);
            string uint64String;

            if (!TryGetPropertyByName(propertyName, out uint64String))
            {
                return false;
            }

            return UInt64.TryParse(
                uint64String, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// Gets the Single value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public Single GetSingle(string propertyName, Single dflt)
        {
            string floatString;
            if (TryGetPropertyByName(propertyName, out floatString))
            {
                try
                {
                    Single val = Single.Parse(
                        floatString, NumberStyles.Any, CultureInfo.CurrentCulture);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        floatString, propertyName, "Float");
                }
                catch (OverflowException)
                {
                    site.Log.Add(LogEntryKind.Debug, overflowException,
                        floatString, propertyName, "Float");
                }
            }
            return dflt;
        }

        /// <summary>
        /// Tries to parse float value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetSingle(string propertyName, out Single value)
        {
            value = default(float);
            string floatString;

            if (!TryGetPropertyByName(propertyName, out floatString))
            {
                return false;
            }

            return Single.TryParse(
                floatString, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// Gets the Double value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public Double GetDouble(string propertyName, Double dflt)
        {
            string doubleString;
            if (TryGetPropertyByName(propertyName, out doubleString))
            {
                try
                {
                    Double val = Double.Parse(
                        doubleString, NumberStyles.Any, CultureInfo.CurrentCulture);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        doubleString, propertyName, "Double");
                }
                catch (OverflowException)
                {
                    site.Log.Add(LogEntryKind.Debug, overflowException,
                        doubleString, propertyName, "Double");
                }
            }
            return dflt;
        }

        /// <summary>
        /// Tries to parse Double value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetDouble(string propertyName, out Double value)
        {
            value = default(Double);
            string doubleString;

            if (!TryGetPropertyByName(propertyName, out doubleString))
            {
                return false;
            }

            return Double.TryParse(
                doubleString, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// Gets the Decimal value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public Decimal GetDecimal(string propertyName, Decimal dflt)
        {
            string decimalString;
            if (TryGetPropertyByName(propertyName, out decimalString))
            {
                try
                {
                    Decimal val = Decimal.Parse(
                        decimalString, NumberStyles.Any, CultureInfo.CurrentCulture);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        decimalString, propertyName, "Decimal");
                }
                catch (OverflowException)
                {
                    site.Log.Add(LogEntryKind.Debug, overflowException,
                        decimalString, propertyName, "Decimal");
                }
            }
            return dflt;
        }

        /// <summary>
        /// Tries to parse Decimal value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetDecimal(string propertyName, out Decimal value)
        {
            value = default(Decimal);
            string decimalString;

            if (!TryGetPropertyByName(propertyName, out decimalString))
            {
                return false;
            }

            return Decimal.TryParse(
                decimalString, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// Gets the Boolean value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public Boolean GetBoolean(string propertyName, Boolean dflt)
        {
            string boolString;
            if (TryGetPropertyByName(propertyName, out boolString))
            {
                try
                {
                    Boolean val = Boolean.Parse(boolString);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        boolString, propertyName, "Boolean");
                }
            }
            return dflt;
        }

        /// <summary>
        /// Tries to parse Boolean value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetBoolean(string propertyName, out Boolean value)
        {
            value = false;
            string boolString;

            if (!TryGetPropertyByName(propertyName, out boolString))
            {
                return false;
            }

            return Boolean.TryParse(boolString, out value);
        }

        /// <summary>
        /// Gets the DateTime value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public DateTime GetDateTime(string propertyName, DateTime dflt)
        {
            string dateTimeString;
            if (TryGetPropertyByName(propertyName, out dateTimeString))
            {
                try
                {
                    DateTime val = DateTime.Parse(dateTimeString, CultureInfo.CurrentCulture);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        dateTimeString, propertyName, "DateTime");
                }
            }
            return dflt;
        }

        /// <summary>
        /// Tries to parse DateTime value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetDateTime(string propertyName, out DateTime value)
        {
            value = default(DateTime);
            string dateTimeString;

            if (!TryGetPropertyByName(propertyName, out dateTimeString))
            {
                return false;
            }

            return DateTime.TryParse(
                dateTimeString, CultureInfo.CurrentCulture, DateTimeStyles.None, out value);
        }

        /// <summary>
        /// Gets the TimeSpan value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public TimeSpan GetTimeSpan(string propertyName, TimeSpan dflt)
        {
            string timeSpanString;
            if (TryGetPropertyByName(propertyName, out timeSpanString))
            {
                try
                {
                    TimeSpan val = TimeSpan.Parse(timeSpanString);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        timeSpanString, propertyName, "TimeSpan");
                }
                catch (OverflowException)
                {
                    site.Log.Add(LogEntryKind.Debug, overflowException,
                        timeSpanString, propertyName, "TimeSpan");
                }
            }
            return dflt;
        }

        /// <summary>
        /// Tries to parse TimeSpan value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetTimeSpan(string propertyName, out TimeSpan value)
        {
            value = default(TimeSpan);
            string timeSpanString;

            if (!TryGetPropertyByName(propertyName, out timeSpanString))
            {
                return false;
            }

            return TimeSpan.TryParse(timeSpanString, out value);
        }

        /// <summary>
        /// Gets the AddressFamily value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public AddressFamily GetAddressFamily(string propertyName, AddressFamily dflt)
        {
            string addressFamilyString;
            if (TryGetPropertyByName(propertyName, out addressFamilyString))
            {
                try
                {
                    AddressFamily val = 
                        (AddressFamily)Enum.Parse(typeof(AddressFamily), addressFamilyString, true);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (ArgumentException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                    "The value of property \"{0}\" cannot be null or empty. " +
                    "Or the value of property is not one of the named constants defined for the AddressFamily.",
                    propertyName);
                }
            }
            return dflt; 
        }

        /// <summary>
        /// Tries to parse AddressFamily value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetAddressFamily(string propertyName, out AddressFamily value)
        {
            value = default(AddressFamily);
            string addressFamilyString;

            if (!TryGetPropertyByName(propertyName, out addressFamilyString))
            {
                return false;
            }

            try
            {
                value =
                    (AddressFamily)Enum.Parse(typeof(AddressFamily), addressFamilyString, true);
                return true;
            }
            catch (ArgumentException)
            {
                site.Log.Add(LogEntryKind.Debug,
                    "The value of property \"{0}\" cannot be null or empty. " + 
                    "Or the value of property is not one of the named constants defined for the AddressFamily.", 
                    propertyName);
            }

            return false;
        }

        /// <summary>
        /// Gets the IPAddress value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public IPAddress GetIPAddress(string propertyName, IPAddress dflt)
        {
            string ipAddressString;
            if (TryGetPropertyByName(propertyName, out ipAddressString))
            {
                try
                {
                    IPAddress val = IPAddress.Parse(ipAddressString);
                    return val;
                }
                catch (ArgumentNullException)
                {
                    site.Log.Add(LogEntryKind.Debug,
                        argumentNullException, propertyName);
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        ipAddressString, propertyName, "IPAddress");
                }
            }
            return dflt;
        }

        /// <summary>
        /// Tries to parse IPAddress value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetIPAddress(string propertyName, out IPAddress value)
        {
            value = default(IPAddress);
            string ipAddressString;

            if (!TryGetPropertyByName(propertyName, out ipAddressString))
            {
                return false;
            }

            return IPAddress.TryParse(ipAddressString, out value);
        }

        /// <summary>
        /// Gets the Char value from the property configured.
        /// </summary>
        /// <param name="propertyName">The property name to be converted</param>
        /// <param name="dflt">The default value which will be returned when it fails to get value.</param>
        /// <returns>Returns parsed value if it is got successfully; otherwise, returns the default value.</returns>
        public Char GetChar(string propertyName, Char dflt)
        {
            string charString;
            if (TryGetPropertyByName(propertyName, out charString))
            {
                try
                {
                    Char val = Char.Parse(charString);
                    return val;
                }
                catch (FormatException)
                {
                    site.Log.Add(LogEntryKind.Debug, formatException,
                        charString, propertyName, "Char");
                }
            }
            return dflt;
        }

        /// <summary>
        /// Tries to parse Char value from the configured string.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Out parameter, to return the parsed value.</param>
        /// <returns>true if it was converted successfully; otherwise, false.</returns>
        public bool TryGetChar(string propertyName, out Char value)
        {
            value = default(Char);
            string charString;

            if (!TryGetPropertyByName(propertyName, out charString))
            {
                return false;
            }

            return Char.TryParse(charString, out value);
        }

        #endregion
    }
}

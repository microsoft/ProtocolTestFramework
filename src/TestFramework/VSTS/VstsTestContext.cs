// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Protocols.TestTools
{
    class VstsTestContext:IProtocolTestContext
    {
        TestContext context;
        string testAssemblyDir;

        public VstsTestContext(TestContext context)
        {
            this.context = context;

            string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            this.testAssemblyDir = Path.GetDirectoryName(assemblyPath);
        }

        #region IProtocolTestContext Members

        public string TestAssemblyDir
        {
            get
            {
                return testAssemblyDir;
            }
        }

        public string PtfconfigDir
        {
            get
            {
                if (context.Properties.Contains("PtfconfigDirectory"))
                {
                    return (string)context.Properties["PtfconfigDirectory"];
                }
                else
                {
                    return testAssemblyDir;
                }
            }
        }

        public PtfTestOutcome TestOutcome
        {
            get
            {
                if (context == null)
                {
                    throw new InvalidOperationException(
                        "The Protocol Test Context can't be null");
                }

                return UnitTestOutcomeToPtfTestOutcome(context.CurrentTestOutcome);
            }
        }

        public string TestMethodName
        {
            get
            {
                if (context == null)
                {
                    throw new InvalidOperationException(
                        "The Protocol Test Context can't be null");
                }

                return context.TestName;
            }
        }

        internal void Update(TestContext newContext)
        {
            if (this.context != newContext)
            {
                this.context = newContext;
            }
        }

        private static PtfTestOutcome UnitTestOutcomeToPtfTestOutcome(UnitTestOutcome uto)
        {
            PtfTestOutcome pto = PtfTestOutcome.Unknown;
            switch (uto)
            {
                case UnitTestOutcome.Failed:
                    pto = PtfTestOutcome.Failed;
                    break;
                case UnitTestOutcome.Inconclusive:
                    pto = PtfTestOutcome.Inconclusive;
                    break;
                case UnitTestOutcome.Passed:
                    pto = PtfTestOutcome.Passed;
                    break;
                case UnitTestOutcome.InProgress:
                    pto = PtfTestOutcome.InProgress;
                    break;
                case UnitTestOutcome.Error:
                    pto = PtfTestOutcome.Error;
                    break;
                case UnitTestOutcome.Timeout:
                    pto = PtfTestOutcome.Timeout;
                    break;
                case UnitTestOutcome.Aborted:
                    pto = PtfTestOutcome.Aborted;
                    break;
                default:
                    pto = PtfTestOutcome.Unknown;
                    break;
            }

            return pto;
        }

        #endregion
    }
}

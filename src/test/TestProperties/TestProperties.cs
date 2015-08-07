// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using Microsoft.Protocols.TestTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Protocols.TestTools.Test.TestProperties
{
    /// <summary>
    /// Test cases to test reading properties defined in .ptfconfig file
    /// </summary>
    [TestClass]
    public class TestProperties : TestClassBase
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            TestClassBase.Initialize(testContext, "TestProperties");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            TestClassBase.Cleanup();
        }

        [TestMethod]
        [TestCategory("TestProperties")]
        public void ReadNormalProperty()
        {
            BaseTestSite.Assert.AreEqual(
                "NormalPropertyValue",
                BaseTestSite.Properties["NormalPropertyName"],
                "Value of property \"NormalPropertyName\" should be \"NormalPropertyValue\"");
        }

        [TestMethod]
        [TestCategory("TestProperties")]
        public void ReadDuplicateProperty()
        {
            BaseTestSite.Assert.AreEqual(
                "TestProperties",
                BaseTestSite.Properties["DuplicatePropertyName"],
                "Value of property \"DuplicatePropertyName\" should be \"TestProperties\"");
        }

        [TestMethod]
        [TestCategory("TestProperties")]
        public void ReadIncludedProperty()
        {
            BaseTestSite.Assert.AreEqual(
                "BasePropertyValue",
                BaseTestSite.Properties["BasePropertyName"],
                "Value of property \"BasePropertyName\" should be \"BasePropertyValue\"");
        }

        [TestMethod]
        [TestCategory("TestProperties")]
        public void ReadRootGroupProperty()
        {
            BaseTestSite.Assert.AreEqual(
                "RootPropertyValue",
                BaseTestSite.Properties["Root.Name"],
                "Value of property \"Root.Name\" should be \"RootPropertyValue\"");
        }

        [TestMethod]
        [TestCategory("TestProperties")]
        public void ReadLeafGroupProperty()
        {
            BaseTestSite.Assert.AreEqual(
                "LeafPropertyValue",
                BaseTestSite.Properties["Root.Leaf.Name"],
                "Value of property \"Root.Leaf.Name\" should be \"LeafPropertyValue\"");
        }

        [TestMethod]
        [TestCategory("TestProperties")]
        public void ReadEmptyProperty()
        {
            BaseTestSite.Assert.AreEqual(
                "",
                BaseTestSite.Properties[""],
                "Value of property \"\" should be \"\"");
        }

        [TestMethod]
        [TestCategory("TestProperties")]
        public void ReadDeploymentProperty()
        {
            BaseTestSite.Assert.AreEqual(
                "DeploymentPropertyValue",
                BaseTestSite.Properties["DeploymentPropertyName"],
                "Value of property \"DeploymentPropertyName\" should be \"DeploymentPropertyValue\"");
        }

        [TestMethod]
        [TestCategory("TestProperties")]
        public void ReadNonExistedProperty()
        {
            BaseTestSite.Assert.AreEqual(
                null,
                BaseTestSite.Properties["NonExistedProperty"],
                "Read a non existed property should return null");
        }
    }
}

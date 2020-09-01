# Getting Started Guide for the Protocol Test Framework


## Abstract
This document provides an overview and a set of samples to help you get started with the Protocol Test Framework.

## Purpose of the Protocol Test Framework
The Protocol Test Framework is intended to support Microsoft Protocol Test Suites. Please refer to the [Microsoft Protocol Test Suite](http://www.microsoft.com/openspecifications/en/us/applied-interoperability/testing/default.aspx) documentation for further information regarding protocol test suites.

Refer to Creating a Test Suite Manually included as a simple test suite example. This is only intended to provide as a simple protocol test suite and test case.  It is not intended to provide guidance for the purpose of creating protocol test suites.



# Table of Contents

* [Protocol Test Environment Requirement](#2)
* [Key Features of Protocol Test Framework](#4)
    * [Default test environment](#4.1)
    * [PTF Configuration -- ptfconfig](#4.2)
    * [Protocol Test Adapters](#4.3)
    * [Extensive Logging Support](#4.4)
    * [Checkers](#4.5)
    * [Requirement Tracing](#4.6)
    * [Display Expected/Actual runtime of testsuite](#4.7)
* [Creating a Protocol Test Suite Manually](#5)
    * [Create a Hello World Unit Test Project](#5.1)
    * [Using Adapters](#5.2)
    * [Run test cases](#5.3)



# <a name="2"> Protocol Test Environment Requirements 

## Computer Systems

* SUT: system under test
    * Computer that is intended to be tested
* Test Driver Computer running Windows(Windows 8.1/Windows Server 2012 R2 or later), Linux or macOS

## Network Infrastructure
* A test network is required to connect the test computer systems
* It must consist of an isolated hub or switch
* It must not be connected to a production network or used for any other business or personal communications or operations
* It must not be connected to the internet
* IP addresses must be assigned for a test network
* Computer names should be assigned in a test network infrastructure
* User credentials used on the system must be dedicated to the test network infrastructure
* Details including computer IP addresses, names and credentials are saved in log files
    * Refer to the Detailed Logging Support section

Refer to the Privacy Statement and EULA for futher information


# <a name="4"> Key Features of Protocol Test Framework 

## <a name="4.1"> Default test environment 

PTF provides an interface, `ITestSite`, to represent the environment for test cases execution. The interface allows access to basic context information (like OS information), configuration, functions for error checking, and adapter instance generation. PTF also contains a default implementation of this interface.

`ITestSite` provides three validation types: `Assume`, `Assert` and `Debug`

* __Assume__ is for designating conditions not necessarily related to the tested protocol

```
Assume.IsTrue(setupSuccess, "Create initial environment") 
```
    
* __Assert__ is for identifying properties that, if false, would be violations of the tested protocol

```
Assert.AreEqual<int>(0x40, bitFlag, "The G bit for NEGOTIATE was not set")
```
    
* __Debug__ is for identify internal code problems

```
Debug.IsNotNull(myObj, "Could not allocate new myObj")
```

## <a name="4.2"> PTF Configuration -- ptfconfig 

PTF configuration files provide the user an interface to change the behavior of the test suites without changing the code. You can see the configuration file in the example. 
Here is a partial list of some of the changes you can make by editing the PTF configuration file:

* PTF users can define the basic test suites information, for example, protocol version and operating system version.
* Generally, test suites utilize the adapter interface and get an instance by calling `BaseTestSite.GetAdapter()` method. PTF configuration allows users to dynamically define the type of adapter implementation used for the specified interface.
* PTF configurations also allow users to define different sinks for logs.
* Users can have multiple PTF configuration files. Users can use the `<Include>` tag, see below, to include extra PTF configuration files.
* PTF configurations can instruct the framework to run custom scripts at the start and end of each test run. It allows the scripts to set up the test environment exactly as required.

### &lt;Include> tag

The Include tag allows you to include additional PTF configuration files. If you have some common properties for your test suites, you can put the properties in a common configuration file. You can then add an `<Include>` tag in the `<TestSuiteName>.ptfconfig` and `<TestSuiteName>.deployment.ptfconfig` files to include the common PTF configuration file.

```
<Include>
    <File name="CommonTestSuite.ptfconfig"/>
</Include>
```

If the same property is defined in multiple PTF configuration files, the rule of the property value is as follow:

* The property value in a ptfconfig file overwrites the value in the included file.
* The value in `<TestSuiteName>.deployment.ptfconfig` overwrites the value in `<TestSuiteName>.ptfconfig` file.

See the example below:

![](images/IncludeTag.png "include tag")

### &lt;Group> tag

If the number of properties in you test suite is very large, you can group these properties using the `<Group>` tag to make the PTFConfig file clearer.

You can use the following name to access the properties in test suites:

```
<Group Name>[.<Group Name>…].<Property Name>
```
        
Example:

```
<Group name="Common">
    <Property name="Timeout" value="8">
        <Description>
            Timeout in seconds for SMB2 connection over transport
        </Description>
    </Property>
</Group>
```

## <a name="4.3"> Protocol Test Adapters 

Adapters are configured in the test suite configuration file and can be retrieved from the ITestSite API.

ITestSite is initialized during the TestClassBase.Initialize method call. The user must call it in the class Initialize method.

ITestSite is disposed in the TestClassBase.Cleanup method call. The user must call it in the class Cleanup method.

The configuration allows an adapter to be created in various ways:

* PowerShell adapter

    PowerShell adapter maps interface methods to PowerShell scripts. Users are able to pass objects between their managed test code and PowerShell scripts directly. This avoids the parameter and return value parsing issue.

* Managed adapter

    The managed adapter allows users to use managed code to implement the interface methods.

### PowerShell Adapter

Users can configure a PowerShell Adapter by defining an adapter as "PowerShell" in the PTF configuration files. The PTF will run the corresponding PowerShell script when one of the adapter's methods is called.

__Benefits__

PowerShell script adapters are much more powerful and much easier to use than script adapter. Users are able to pass object between their managed test code and PowerShell script directly. This avoids the parameter and return value parsing issue. PowerShell adapter is suitable for local/remote environment setup.

__Usage__

Configure <Adapters> section of .ptfconfig file like this:
```
<Adapter xsi:type="powershell" name="IAdapterInterfaceName" scriptdir="." />
```

* "xsi:type": The value of this attribute must be "powershell".
* "name": The value of this attribute is the name of the adapter interface which will be bound to the PowerShell adapter proxy.
* "scriptdir": The value of this attribute is the path of the directory which contains the corresponding PowerShell scripts.

__Parameters__

Parameters are passed from PowerShell adapter to PowerShell script directly so that users can use them without any parsing. Users can also set values to out/ref parameters directly in their PowerShell scripts and the PowerShell adapter will pass them back to the test suite. The parameter name is the same as defined in the adapter interface. For example, If a parameter `int a` is defined, then you can use `$a` in the PowerShell adapter directly.

__Example__

The following example shows how to use PowerShell adapter.
Define the adapter interface:

```
public interface ISampleAdapter : IAdapter
    {
        int AMethod(string parameter1, int parameter2);
    }

```

Configure the PowerShell adapter in .ptfconfig file:

```
<Adapter xsi:type="powershell" name="ISampleAdapter" scriptdir="."/>
```

In the script AMethod.ps1, you can access the value of parameters using $parameter1 or $parameter2:

```
if($parameter2 -eq 2)
{
    return 0
}
return 1
```

Call the adapter method in your test code:

```
[TestMethod]
public void TestPowerShellAdapter()
{
  ISampleAdapter sampleAdapter;
  sampleAdapter = BaseTestSite.GetAdapter<ISampleAdapter>();
  int r = sampleAdapter.AMethod("This is parameter1.", 2);
  BaseTestSite.Assert.AreEqual<int>(0, r, "Verify the return value.");
}
```

## <a name="4.4"> Extensive Logging Support 

PTF provides problem-oriented logging capabilities (begin/end test group, verification pass/failure, requirement capture, debugging). Most of the logging is done automatically so that the test suite developer does not have to write them manually.

* Test Asserting failure/success,
* Entering/Exiting Adapter Code.

PTF provides the ability to define "logging profiles" in PTF configuration files. These profiles provide a way to define which logging event is piped to which sink. Logging profiles can be set dynamically by testing code. 

Note: If the user wants to log EnterMethod and ExitMethod, user should log EnterMethod first. The corresponding ExitMethod must not be omitted.

### File Logging

File logging provides a way for PTF users to log necessary messages to files. PTF provides two logging file formats, plain text and XML. The plain text format is suitable for viewing the log file using text editors. The XML format is for analysing the test log using tools.

__Usage__

Define file logging sinks in the <sinks\> tag of the PTF Configure file. 

For example:

```
<Sinks>    
…
  
   <File id="SinkID1" directory=".\" file="xmllog.xml" format="xml" />
   <File id="SinkID2" directory=".\" file="textlog.txt" format="text" />
…
</Sinks>
```

The format attribute is to specify the file format. Use `xml` for XML file and `text` for plain text file.

The directory attribute is to specify the location of the log file.

The file attribute is the name of the log file. If a file with the name already exists, PTF will keep the existing file and create a new file with the following prefix in the same folder.

```
[<TestSuiteName>_<SinkName>]<TimeStamp>
```

For example:

```
[TestLogging_SinkID1]2014-02-11 11_25_04_063 xmllog.xml
```

### Console Logging

Enabling Console logging lets the user see the log of the test run as it proceeds. The user can terminate a running test case by looking at its progress. This is useful in the scenarios where the tests are long and taking time.

Messages corresponding to the log rules configured for ‘CommandLineConsole’ sink is displayed to the Console window. A separate Console window is opened for each test suite and log messages of multiple tests will be displayed to the same window.

__Usage__

Enable console logging by specifying a set of log kinds associated with CommandLineConsole sink:

```
<Rule kind="CheckSucceeded" sink="CommandLineConsole" delete="false" />
<Rule kind="TestPassed" sink="CommandLineConsole" delete="false" />
<Rule kind="TestFailed" sink="CommandLineConsole" delete="false" />
```

When running the test suite, a Console window will be opened and configured log messages will be displayed on it.

The size of the window is customizable.

```
<Property name="ConsoleBufferHeight" value="600"/>
<Property name="ConsoleWidth" value="100"/>
<Property name="ConsoleHeight" value="50"/>
```

### Color Console Logging

Log message displayed in the Console can be colorized. You can use different color to display different log messages.

__Usage__

* Define console sink with color.Only the following four colors are available: red, green, yellow and white.

```
<Sinks>    
… 
      <Console id="RedConsole" />
      <Console id="GreenConsole" />
      <Console id="YellowConsole" />
      <Console id="WhiteConsole" />
…
</Sinks>
```

* Associate log entries with the defined sinks:

```
<Profiles>
    <Profile name="Verbose" extends="Error">        
      <Rule kind="CheckSucceeded" sink="GreenConsole" delete="false" />
      <Rule kind="TestPassed" sink="GreenConsole" delete="false" />
      <Rule kind="TestFailed" sink="RedConsole" delete="false" /> 
      <Rule kind="Comment" sink="YellowConsole" delete="false"/>
    </Profile>
</Profiles>
```

### Creating custom log sinks

PTF also allows users to create their own log sink. To create a custom log sink, users must inherit the abstract class `Microsoft.Protocols.TestTools.Logging.LogSink` in PTF, or the existing implementation of LogSink (For example, TextSink). Below is an example:

```
public class MyCustomerSink : TextSink
{
    StreamWriter sw;
    public MyCustomerSink(string name)
        : base(name)
    {
        String logFilename = "MyLog.txt"
        sw = new StreamWriter(logFilename);
    }

    protected override TextWriter Writer
    {
        get { return sw; }
    }
}
```

In order to use this new sink class, users should modify the configuration file as follows:

```
…
<Sinks>      
      <Sink id="MySink" type="CustomerSink.MyCustomerSink, CustomerSink" />
</Sinks>
…
<Profiles>
      <Profile name="Verbose" extends="Error">        
        <Rule kind="CheckSucceeded" sink="Console" delete="false" />        
        <Rule kind="Comment" sink="Console" delete="false"/>
        <Rule kind="Debug" sink="MySink" delete="false" />
        <Rule kind="CheckSucceeded" sink="MySink" delete="false" />
        <Rule kind="Comment" sink="MySink" delete="false"/>
        <Rule kind="Debug" sink="MySink" delete="false" />
      </Profile>
</Profiles>
…
```

## <a name="4.5"> Checkers 

PTF provides checkers to test validation and verification infrastructure. Protocol test code should direct all validation code to those checkers. The current test's execution will stop when an assertion fails and a corresponding entry will be automatically created in the test log. Depending on log settings, an entry may also be created if an assertion succeeds. In general, test code does not need to provide extra logging output related to an assertion pass or failure. 

As discussed previously, ITestSite provides three checkers: Assume, Assert and Debug.

The checkers could also be used in the thread created by a user to report an asynchronous error.

## <a name="4.6"> Requirement Tracing 

The requirement tracing feature enables users to track the technical document requirement coverage by Model and Adapter. The requirement tracing feature is derived from an XML sink type. The user can use an XML log sink by adding the following line to the configuration

```
<File id="xmlfile" directory=".\log" file="Sample.xml" format="xml " />
```

In a model, a requirement can be captured by adding the following line. 

```
Requirements.Capture(Microsoft.Protocols.TestTools.RequirementId.Make(string docShortName, int number, string description));
```

In an adapter, a requirement can be captured by adding the following line.

```
BaseTestSite.Log.Add(LogEntryKind.Checkpoint, 
     RequirementId.Make(string docShortName, int number, string description) );
```

Or

```
BaseTestSite.CaptureRequirement(string protocolDocShortName, int requirementId
     , string description);
```

Then a Checkpoint log entry will be written to the log sink while running. 

### Extend requirement class

The Extend Requirement Class provides a set of new APIs that can capture requirements more conveniently. If users want to capture their requirements in an adapter, the new Site.CaptureRequirementIf* APIs can be used instead of assert and log check point. 

A user can assert and capture requirement (in this case ProtocolName-1) in only one step:

```
BaseTestSite.CaptureRequirementIfAreEqual<int>(7, 3 + 4, "ProtocolName", 1, " Must assert before log requirements ");
```

Or more typically if Site.DefaultProtocolDocShortName is set to ProtocolName.

```
BaseTestSite.CaptureRequirementIfAreEqual<int>(7, 3 + 4, 1, " Must assert before log requirements ");
```

ProtocolName is typically provided only when a protocol is testing requirements from another protocol.

### Requirement type

This feature enables users to control the test suite execution for specific types of requirements in a single test pass depending on their choice. By providing an option in PTFConfig file, users should be able to restrict or select their choice of requirements to be validated.

In order to use this feature, the user needs to specify the type of requirement in code:

```
BaseTestSite.CaptureRequirementIfIsTrue(
    true, 1, "a Product requirement",RequirementType.Product);
BaseTestSite.CaptureRequirementIfIsTrue(
    true, 2, "a MAY requirement",RequirementType.May);
BaseTestSite.CaptureRequirementIfAreEqual(
    actual, Int32.MaxValue, 3, "a SHOULD requirement", RequirementType.Should);
```

The below configuration in PTFConfig skips SHOULD and Product requirements from getting validated:

```
<Property name="SkipSHOULDRequirements" value="true"/>
<Property name="SkipPRODUCTRequirements" value="true"/>
```

For the requirements which are not MUST, users can input their own expected values to the test suite for validation of requirements. In the case that the user’s expected value is different from the one mentioned in the technical document for a particular requirement.

```
<Property name="RequirementMS-XXXXX_R3" value="10" />
```

### Skip exceptional requirements

Usually when a test case encounters a failure, the test case stops and the following code is skipped. This feature enables users to get control the test case pass/fail criteria by marking requirements exceptional.  When an exceptional requirement fails, the test case continues running with the failure logged in entry CheckFailed and Exceptional Requirement.

__Example:__

The following code shows how to mark requirement MS-XXX_R1 and MS_YYYR10 as Exceptional Requirements in ptfconfig.

```
<!--Requirements Section begins. All the property elements for the Requirements information should be in this section.-->
 <!--Type:string! Usage: Exceptional requirement ID list, separating by comma -->
 <Property name="ExceptionalRequirements" value=="MS-XXX_R1,MS-YYY_R10" />
<!--Requirements Section ends.-->
```

If MS-XXX_R1 fails, the test case does not stop and the following message will be logged.

```
2012-04-01 16:14:55.310    [CheckFailed] Assert.IsTrue failed on requirement MS-XXX_R1. a MUST requirement
2012-04-01 16:14:55.466    [ExceptionalRequirement] Assert.IsTrue failed on requirement MS-XXX_R1. a MUST requirement
```

Note:

Requirement ID in this property is case sensitive. 

Do not have any leading zeroes in requirement ID. (For example, "MS-XXX_R01" will always be ignored)




## <a name="4.7"> Display Expected/Actual runtime of testsuite 

This feature enables a user to display expected/actual runtime of the test suite on the console.

Expected runtime of the test suite configuration will be added to the .ptfconfig file.

```
<!-- TestSuite Section begins. All the property elements for the TestSuite information should be in this section.-->
<!--Type:string! Usage: Expected run time of the test suite (in seconds)!-->
<Property name="ExpectedExecutionTime" value="5000" />
<!-- TestSuite Section ends.-->
```

Test suite will log and display the Actual time after the test run is completed.




# <a name="5"> Creating a Protocol Test Suite Manually

The overall steps for creating a protocol test suite using PTF are as follows:

* Create a Unit Test Project
* Create a PTF configuration file
* Create a test class
* Deploy the files

In the following sections, we will go through each of these steps in detail.

In this example, we will test a multiplication operation: 5*4 = 20.


## <a name="5.1"> Create a Hello World Unit Test Project 

The PTF project is hosted in the Unit Test Project. To create a PTF project:

* Create a new project in Visual Studio.
* Use Unit Test Project template in the Test category of Visual C#.
* Add references to the PTF assemblies Microsoft.Protocols.TestTools.
    There're two ways to add PTF reference
    * Install PTF by NuGet Package Manager
        * Open the project/solution in Visual Studio, and open the console using the Tools > NuGet Package Manager > Package Manager Console command
        * Run the install command: Install-Package Microsoft.Protocols.TestTools -Version 2.0.0
    * Manual add Microsoft.Protocols.TestTools.dll
        * Right click "References" from Solution Explorer and choose "Add Reference…".
        * Select `Microsoft.Protocols.TestTools`.
* Delete automatically generated file `UnitTest1.cs`.

### Create PTF configuration file

The PTF configuration file is an XML file.

* Create a helloworld.ptfconfig file.
* Copy the content from the following example.

Below is helloworld.ptfconfig used in this example:

```
<?xml version="1.0" encoding="utf-8" ?>
<TestSite xmlns="http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/TestConfig"
xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
xsi:schemaLocation="http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/TestConfig http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/TestConfig.xsd">

  <Properties>
    
    <!-- This is a required property.The name of the test suite. 
         Normally, it is the name of a being tested protocol. 
         Will map to ITestSite.FeatureName. -->
    <Property name="FeatureName" value="PTF:HelloWorld" />
    
    <!-- This is a required property.The version of the protocol technical document that this test suite is verifying. -->
    <Property name="Version" value="1.0" />
        
  </Properties>
    <!-- The default profile name.  Provide maximum logging. -->
    <TestLog defaultprofile="Verbose">
    
    <Sinks>
      <!-- File log sink. Id is name, directory is the dir which log will stored in. File is the name of log. Format only can be ‘text’-->
      <File id="MyLog" directory=".\" file="Mylog.txt" format="text"/>       
    </Sinks>
      
    <Profiles>
      <!-- Name of the profile.-->
      <Profile name="Verbose" extends="Error">
        <!--"kind" is the name of log entry. "sink" is where the log messages.
         Will log. Delete="true" means disable the rule. -->
        <Rule kind="CheckSucceeded" sink="Console" delete="false" />        
        <Rule kind="Comment" sink="Console" delete="false"/>
        <Rule kind="Debug" sink="Console" delete="false" />
        <Rule kind="CheckSucceeded" sink="MyLog" delete="false" />
        <Rule kind="Comment" sink="MyLog" delete="false"/>        
      </Profile>
    </Profiles>
  </TestLog>
</TestSite>
```

### Create a test class

A test class must inherit from base class TestClassBase and must have TestClass attribute.

Create a C# file -- HelloWorld.cs:

```
using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Protocols.TestTools;

namespace HelloWorld
{
  /// <summary>
  /// Hello World PTF test class
  /// </summary>
  [TestClass]
  public class HelloWorld : TestClassBase
  {
    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
       TestClassBase.Initialize(testContext, "helloworld");
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        TestClassBase.Cleanup();
    }

    protected override void TestInitialize()
    {
      BaseTestSite.Log.Add(LogEntryKind.Comment, "Test Initialize");       
    }

    protected override void TestCleanup()
    {
      BaseTestSite.Log.Add(LogEntryKind.Comment, "Test Cleanup");
    }
    [TestMethod]         
    public void MultiplyTest()
    {
      BaseTestSite.Log.Add(LogEntryKind.Comment, "MultiplyTest");
      BaseTestSite.Assert.AreEqual<int>(20, 5 * 4, "Multiplication check");
    }
 }
}
```


## <a name="5.2"> Using Adapters 

The overall steps to create a protocol test suite with adapters using PTF are as follows:

* Create an adapter
* Add <adapters\>  </adapters\> tag to the PTF config file
* Implement the managed adapter
* Create a test class that uses the adapter
* Deploy the files

In subsequent sections, we will walk through each of these steps in detail.

### Create adapter interfaces

Following the previous example, we now add the addition operation into IOperation adapter which does the actual computation operation (which could be distributed, in native code, etc.).  We use a script adapter to "Setup the environment"

Creates a C# file – Adapters.cs as follows:

```
using System;
using Microsoft.Protocols.TestTools;
/// <summary>
/// Example of typical types of adapters:  ServerControl with MethodHelp  and Protocol with abstract actions
/// </summary>
namespace HelloWorld
{
    /// <summary>
    /// Example of typical ServerControl configuration adapter.
    /// </summary>
    public interface ISetup : IAdapter
    {
        [MethodHelp("Return 0 for successful setup server or non zero error code.")]
        int Setup(string message);
    }

    /// <summary>
    /// Example of abstract action (protocol adapter).
    /// </summary> 
    public interface IOperation : IAdapter
    {
        int ComputeAddition(int x, int y);
    }
}
```

Copy the previous helloworld.ptfconfig file and add the following <adapters\> tag to the helloworld.ptfconfig file after the </Properties\> tag.

```
  <Adapters>
    <!-- Script adapter declaration. Scriptdir is the scripts location.-->
    <Adapter xsi:type="script" name="ISetup" scriptdir="."/>
    <!-- Abstract actions (protocol adapters) are not typically changed, but this one could be -->
    <Adapter xsi:type="managed" name="IOperation" adaptertype="HelloWorld.OperationAdapter"/>
  </Adapters>
```

### Implement the managed adapter

From the above configuration, we can see that the managed Adapter, IOperation is directed to the Adapter, HelloWorld.OperationAdapter. 

Here are the steps for implementing a managed adapter:

* Create a C# file OperationAdapter.cs.
* Implement IOperation interface to OperationAdapter class.  

```
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Protocols.TestTools;

namespace HelloWorld
{
    public class OperationAdapter : ManagedAdapterBase, IOperation
    {
        public int ComputeAddition(int x, int y)
        {
            return x + y;
        }

    }
}
```

### Create script file for script adapter

Create script file Setup.sh.

```
echo Hello, setting up

# First argument of the method
echo arg1: $1
# Second argument of the method
echo arg2: $2

# Do something...

# Set exit code as 0 to indicate success.
# Exit with a non-zero code to indicate failure.
exit 0
```

Enable the deployment of the script file.

* Open Testsettings1.testsettings file from the Solution Explorer.
* Add Setup.sh file from the Deployment page. (You need to choose "All files" for the file type in the File dialog.)

### Use the adapters in test case

Now we can use these adapters in the test cases.

```
using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Protocols.TestTools;

namespace HelloWorld
{
    /// <summary>
    /// Sample PTF test class
    /// </summary>
    [TestClass]
    public class HelloWorld : TestClassBase
    {
        ISetup setup;
        IOperation operation;
        ICleanup cleanup;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            TestClassBase.Initialize(testContext, "HelloWorld");
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            TestClassBase.Cleanup();
        }

        protected override void TestInitialize()
        {
            setup = BaseTestSite.GetAdapter<ISetup>();
            operation = BaseTestSite.GetAdapter<IOperation>();
            cleanup = BaseTestSite.GetAdapter<ICleanup>();            
        }

        protected override void TestCleanup()
        {
            setup.Reset();
            operation.Reset();
            cleanup.Reset();
        }

        [TestMethod]
        public void MyTest()
        {
            int r = setup.Setup(Site.TestSuiteName + " " + BaseTestSite.Properties.Get("Version") + " setup");
            BaseTestSite.Assume.AreEqual<int>(0, r, "setup must succeed to continue testing");

            r = operation.ComputeAddition(3, 4);
            BaseTestSite.Assert.AreEqual<int>(3 + 4, r, "operation must yield addition of inputs");

            r = cleanup.Cleanup(BaseTestSite.TestSuiteName + " " + BaseTestSite.Properties.Get("Version") + " cleanup");
            BaseTestSite.Assert.AreEqual<int>(0, r, "cleans up server after helloworld testing ");
        }
    }
}
```

## <a name="5.3"> Run test cases 

To run test cases from Visual Studio, you can open the Test Explorer from the Menu: Test > Windows > Test Explorer.

Test cases are automatically listed in the Test Explorer after you compile the test project. You can run the test cases and view the results from the Test Explorer.

![](images/TestExplorer.png "Test Explorer")

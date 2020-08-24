# Protocol Test Framework

The Protocol Test Framework (PTF) is designed to support Microsoft Protocol Test Suites for both Windows and Office Protocol Interoperability testing. 
It implements the fundamentals to support Protocol Test Suite, including logging, checker, configuration and etc.

## Prerequisites

PTF is based on Windows platform.
You should install the softwares listed below based on your testing purpose, including their own dependencies.
1. [Visual Studio](https://visualstudio.microsoft.com/downloads/) 2017 or higher ([Visual Studio 2017 Community](https://aka.ms/vs/15/release/vs_community.exe) recommended), installed with these individual components from the installer:

    |Section|Individual Component in Visual Studio 2017|Individual Component in Visual Studio 2019|Run Windows Protocol Test Suites|Build Windows Protocol Test Suites from source code|
    |---|---|---|---|---|
    |.NET|.NET Framework 4.7.1 SDK|.NET Framework 4.7.1 SDK||Required|
    |.NET|.NET Framework 4.7.1 targeting pack|.NET Framework 4.7.1 targeting pack|Required|Required|
    |Compilers, build tools, and runtime|C# and Visual Basic Roslyn compilers|C# and Visual Basic Roslyn compilers||Required|
    |Debugging and testing|Testing tools core features||Required<sup>[1](#footnote1)</sup>|Required<sup>[1](#footnote1)</sup>|

    Note:

    <a name="footnote1">1</a>: This individual component is installed in Visual Studio 2019 by default.

1. [Spec Explorer 2010 v3.5.3146.0](https://visualstudiogallery.msdn.microsoft.com/271d0904-f178-4ce9-956b-d9bfa4902745/)

   It is only required if you want to build or run the test suites that contain Model-Based Test cases. If you want to regenerate Model-Based Test cases, you must install Visual Studio 2012.
1. Enable .NET Framework 3.5.1

   1. _Turn Windows features on or off_
   1. Enable _.NET Framework 3.5 (includes .NET 2.0 and 3.0)_

   This is necessary for WiX Toolset.
1. [WiX Toolset v3.14](https://wixtoolset.org/releases/v3-14-0-2927/)

1. [WiX Toolset Visual Studio 2017 Extension](https://marketplace.visualstudio.com/items?itemName=WixToolset.WixToolsetVisualStudio2017Extension) or [Wix Toolset Visual Studio 2019 Extension](https://marketplace.visualstudio.com/items?itemName=WixToolset.WixToolsetVisualStudio2019Extension)

   WiX Toolset components required if you want to build test suites or Protocol Test Manager from source code.


You can use the script in **InstallPrerequisites** folder to automatically download and install these software.
Tips when using the script in **InstallPrerequisites** folder:

* To run the script, open **Windows PowerShell**, and execute the commands below in the **PowerShell Window**:

```
cd ProtocolTestFramework\InstallPrerequisites
.\InstallPrerequisitesForPTF.ps1
```

* If you meet errors about **Execution Policy**, make sure you run **Windows PowerShell** as **Administrator**, and type the following and enter:

```
Set-ExecutionPolicy RemoteSigned
```

You could run the command below to verify if the **Execution Policy** is correctly set:

```
Get-ExecutionPolicy
```

## Build

After you clone a copy of this repo, change to the ProtocolTestFramework directory:

```
cd ProtocolTestFramework
```

Change to src directory and run build.cmd:

  ```
  cd src
  build.cmd
  ```

After the build succeeds, ProtocolTestFrameworkInstaller.msi should be generated in the folder `drop\ProtocolTestFramework\installer\`.


## Samples

You can find samples of how to develop a protocol test suite using PTF [here](./samples).

## Documentation

* [User Guide](./docs/) describes the features of PTF, and how to use them to develop a new protocol test suite.

## Contribute

You can find contributing guide [here](./CONTRIBUTING.md).

## License

PTF is under the [MIT license](./LICENSE.txt).

## Microsoft Open Source Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments. 

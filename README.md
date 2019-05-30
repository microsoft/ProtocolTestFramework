# Protocol Test Framework

The Protocol Test Framework (PTF) is designed to support Microsoft Protocol Test Suites for both Windows and Office Protocol Interoperability testing. 
It implements the fundamentals to support Protocol Test Suite, including logging, checker, configuration and etc.

## Prerequisites

PTF is based on Windows platform.
You should install the softwares listed below based on your testing purpose, including their own dependencies.
* [Visual Studio 2017](https://visualstudio.microsoft.com/downloads/) with some individual components required by installing in Visual Studio Installer:
 
    |Individual Component\Purpose|Run test suites based on Protocol Test Framework (PTF)|Build Protocol Test Framework (PTF) from source code|
    |---|---|---|
    |.NET Framework 4.7.1 targeting pack|Required|Required|
    |Testing tools core features|Required|Required|
    |.NET Framework 4.7.1 SDK||Required|
    |C# and Visual Basic Roslyn compilers||Required|
* [Spec Explorer 2010 v3.5.3146.0](https://visualstudiogallery.msdn.microsoft.com/271d0904-f178-4ce9-956b-d9bfa4902745/). It is required if you want to build Protocol Test Framework (PTF) from source code.
* [Wix Toolset v3.11](https://github.com/wixtoolset/wix3/releases/tag/wix3111rtm) and [Wix Toolset Visual Studio 2017 Extension](https://marketplace.visualstudio.com/items?itemName=RobMensching.WixToolsetVisualStudio2017Extension). It is required if you want to build Protocol Test Framework (PTF) from source code.


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

After the build succeeds, ProtocolTestFrameworkInstaller.msi should be generated in the folder drop\ProtocolTestFramework\installer\.


## Samples

You can find samples of how to develop a protocol test suite using PTF [here](https://github.com/Microsoft/ProtocolTestFramework/tree/master/samples).

## Documentation

* [User Guide](https://github.com/Microsoft/ProtocolTestFramework/tree/master/docs/) describes the features of PTF, and how to use them to develop a new protocol test suite.

## Contribute

You can find contributing guide [here](https://github.com/Microsoft/ProtocolTestFramework/blob/master/CONTRIBUTING.md).

## License

PTF is under the [MIT license](https://github.com/Microsoft/ProtocolTestFramework/blob/master/LICENSE.txt).

## Microsoft Open Source Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

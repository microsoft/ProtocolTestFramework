# Protocol Test Framework

The Protocol Test Framework (PTF) is designed to support Microsoft Protocol Test Suites for both Windows and Office Protocol Interoperability testing. 
It implements the fundamentals to support Protocol Test Suite, including logging, checker, configuration and etc.

## Prerequisites

PTF is based on Windows platform.
You should install the following list of software in order to build PTF from source code.
You can use the script in **InstallPrerequisites** folder to automatically download and install these software.

* .Net framework 4.7.1 or higher
* Wix toolset [v3.11](https://github.com/wixtoolset/wix3/releases/tag/wix3111rtm)
* Visual Studio version 2017
* [Spec Explorer](https://visualstudiogallery.msdn.microsoft.com/271d0904-f178-4ce9-956b-d9bfa4902745/)

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

# Protocol Test Framework

The Protocol Test Framework (PTF) is designed to support Microsoft Protocol Test Suites for both Windows and Office Protocol Interoperability testing. 
It implements the fundamentals to support Protocol Test Suite, including logging, checker, configuration and etc.

## Prerequisites
PTF is based on Windows platform.
You should install the following list of software in order to build PTF from source code.

* .Net framework 4.0 or higher
* Wix toolset [v3.10](https://wix.codeplex.com/releases/view/619491)
* Visual Studio or Visual Studio test agent, version 2012 or higher
* [Spec Explorer](https://visualstudiogallery.msdn.microsoft.com/271d0904-f178-4ce9-956b-d9bfa4902745/). It is only required if you want to build a PTF version that supports the test suites which contain [Model-Based Test](https://msdn.microsoft.com/en-us/library/ee620469.aspx) cases.

## Build

After you clone a copy of this repo, change to the ProtocolTestFramework directory:
```
cd ProtocolTestFramework
```

PTF can be built into two versions:

* The **nomodel** version is used to support the protocol test suite that does not contain Model-Based Test cases, change to src directory and run build.cmd
  ```
  cd src
  build.cmd
  ```
After the build succeeds, ProtocolTestFrameworkInstaller.msi should be generated in the folder drop\ProtocolTestFramework\installer\.

* The **formodel** version is used to support the protocol test suite that contains Model-Based Test cases, you should install [Spec Explorer](https://visualstudiogallery.msdn.microsoft.com/271d0904-f178-4ce9-956b-d9bfa4902745/) first and then build PTF with the option **formodel**.
  ```
  cd src
  build.cmd formodel
  ```
After the build succeeds, ProtocolTestFrameworkInstallerForModel.msi should be generated in the folder drop\ProtocolTestFramework\installer\.

## Samples
You can find samples of how to develop a protocol test suite using PTF [here](https://github.com/Microsoft/ProtocolTestFramework/tree/master/samples).

## Documentation

*  [User Guide](https://github.com/Microsoft/ProtocolTestFramework/tree/master/docs/) describes the features of PTF, and how to use them to develop a new protocol test suite.

## Contribute

You can find contributing guide [here](https://github.com/Microsoft/ProtocolTestFramework/blob/master/CONTRIBUTING.md).

## License

PTF is under the [MIT license](https://github.com/Microsoft/ProtocolTestFramework/blob/master/LICENSE.txt).

## Microsoft Open Source Code of Conduct
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

# Protocol Test Framework

The Protocol Test Framework (PTF) is designed to support Microsoft Protocol Test Suites for both Windows and Office Protocol Interoperability testing. 
It implements the fundamentals to support Protocol Test Suite, including logging, checker, configuration and etc.

## Prerequisites
PTF is based on Windows platform.
You should install the following list of software in order to build PTF from source code.

* .Net framework 4.0 or higher
* Wix toolset v3.7 or higher
* Visual Studio or Visual Studio test agent, version 2012 or higher

## Build

After you clone a copy of this repo, change to the ProtocolTestFramework directory:

```
cd ProtocolTestFramework
```

Change to src directory and run build.cmd

```
cd src
build.cmd
```
After the build succeeds, ProtocolTestFrameworkInstaller.msi should be generated in the folder ProtocolTestFramework\src\Bin\deploy\installer\.

If you need to develop a protocol test suite using Model Based Testing tool [Spec Explorer](https://visualstudiogallery.msdn.microsoft.com/271d0904-f178-4ce9-956b-d9bfa4902745/), 
you should install **Spec Explorer** first and then build PTF with the option **formodel**.
```
build.cmd formodel
```

## Samples
You can find samples of how to develop a protocol test suite using PTF [here](https://github.com/Microsoft/ProtocolTestFramework/tree/master/samples).

## Documentation

*  [User Guide](https://github.com/Microsoft/ProtocolTestFramework/tree/master/docs/) describes the features of PTF, and how to use them to develop a new protocol test suite.

## Contribute

You can find contributing guide [here](https://github.com/Microsoft/ProtocolTestFramework/blob/master/CONTRIBUTING.md).

## License

PTF is under the [MIT license](https://github.com/Microsoft/ProtocolTestFramework/blob/master/LICENSE.txt).

# Protocol Test Framework

The Protocol Test Framework (PTF) is designed to support Microsoft Protocol Test Suites for both Windows and Office Protocol Interoperability testing. 
It implements the fundamentals to support Protocol Test Suite, including logging, checker, configuration and etc.

## Prerequisites

PTF is based on .NET Core and support cross-platform. It runs on Windows, macOS and multiple distributions of Linux.
You should install the software listed below based on your testing purpose, including their own dependencies.
1. [.NET Core SDK](https://dotnet.microsoft.com/download/dotnet/5.0)

## Build

After you clone a copy of this repo, change to the ProtocolTestFramework directory:

```
cd ProtocolTestFramework
```

Change to src directory and build PTF:

  ```
  cd src
  dotnet build --configuration Release
  ```
Note:

   For more information about how to use dotnet build please refer to :[dotnet build](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-build/)

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

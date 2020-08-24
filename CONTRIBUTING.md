# Contributing to Protocol Test Framework
There are many ways to contribute to PTF.

* Report bugs and help verify fixes when they are checked in.
* Submit updates and improvements to the [documentation](./docs).
* Contribute bug fixes.
* Add new features. But firstly you should log an issue to notify the team before you spend a lot of time on it.

## CLA
Contributors must sign a [Contribution License Agreement (CLA)](https://cla.microsoft.com/) before any pull requests will be considered. 
This is a one time job. Once you have signed a CLA for any project sponsored by Microsoft, you are good to go for all the repos sponsored by Microsoft.

## Coding Style
The basic rule is following the coding style of the existing code. 

## Test
Every time you make changes to PTF, you should run the [unit test cases](./src/test) to avoid regression.
If you add new features other than minor changes or bug fixing, you should add the relative test cases.

To build test project:
```
cd ProtocoTestFramework\src\test
buildtest.cmd
```

To run all the test cases:
```
runtest.cmd
```



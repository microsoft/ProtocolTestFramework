:: Copyright (c) Microsoft. All rights reserved.
:: Licensed under the MIT license. See LICENSE file in the project root for full license information.

@echo off

set currentPath=%~dp0
set PTFTEST_Root=%currentPath%..\
call "%PTFTEST_Root%common\setBuildTool.cmd"
call "%PTFTEST_Root%common\setVsPath.cmd"

%buildtool% TestPTF.sln /t:clean;rebuild
if ErrorLevel 1 (
    echo Error: Failed to build TestPTF
    exit /b 1
)

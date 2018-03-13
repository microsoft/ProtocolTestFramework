:: Copyright (c) Microsoft. All rights reserved.
:: Licensed under the MIT license. See LICENSE file in the project root for full license information.

@echo off

set currentPath=%~dp0
set PTFTEST_Root=%currentPath%..\..\

call "%PTFTEST_Root%src\common\setVsPath.cmd"

:: Does not run Interactive adapter cases in automation test
%vspath%..\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe "TestProperties\bin\Debug\TestProperties.dll" "TestChecker\bin\Debug\TestChecker.dll" "TestLogging\bin\Debug\TestLogging.dll" "TestRequirementCapture\bin\Debug\TestRequirementCapture.dll" "TestAdapter\bin\Debug\TestAdapter.dll" /TestCaseFilter:"Name!=InteractiveAdapterAbort&Name!=InteractiveAdapterReturnInt&Name!=InteractiveAdapterReturnString" /Settings:Local.testsettings /Logger:trx
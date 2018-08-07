:: Copyright (c) Microsoft. All rights reserved.
:: Licensed under the MIT license. See LICENSE file in the project root for full license information.

@echo off

set currentPath=%~dp0
set PTF_Root=%currentPath%..\

call "%PTF_Root%src\common\setBuildTool.cmd"
if ErrorLevel 1 (
	exit /b 1
)

call "%PTF_Root%src\common\setVsPath.cmd"
if ErrorLevel 1 (
	exit /b 1
)

if not defined WIX (
	echo WiX Toolset version 3.11 should be installed
	exit /b 1
)

if not defined ptfsnk (
	set ptfsnk=%currentPath%\TestKey.snk
)

::Get build version from AssemblyInfo
set path="%currentPath%\SharedAssemblyInfo.cs"
set FindExe="%SystemRoot%\system32\findstr.exe"
set versionStr="[assembly: AssemblyVersion("1.0.0.0")]"
for /f "delims=" %%i in ('""%FindExe%" "AssemblyVersion" "%path%""') do set versionStr=%%i
set PTF_VERSION=%versionStr:~28,-3%

%buildtool% "%PTF_Root%src\ptf.sln" /t:Clean /p:Configuration="Release"
if ErrorLevel 1 (
    echo Error: Failed to build Protocol Test Framework
    exit /b 1
)

if exist "%PTF_Root%drop" (
 rd /s /q "%PTF_Root%drop"
)

%buildtool% %currentPath%\deploy\Installer\ProtocolTestFrameworkInstaller.wixproj /t:Clean;Rebuild /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=%ptfsnk% /p:Configuration="Release"

if ErrorLevel 1 (
    echo Error: Failed to generate the msi installer
    exit /b 1
)

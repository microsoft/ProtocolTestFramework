:: Copyright (c) Microsoft. All rights reserved.
:: Licensed under the MIT license. See LICENSE file in the project root for full license information.

@echo off

set currentPath=%~dp0
set PTF_Root=%currentPath%..\

call "%PTF_Root%src\common\setEnv.cmd"

if not defined WIX (
	echo WiX Toolset version 3.11 should be installed
	exit /b 1
)

if "%WIX:3.11=%"=="%WIX%" (
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

%buildtool% "%PTF_Root%src\ptf.sln" /t:Clean

if exist "%PTF_Root%drop" (
 rd /s /q "%PTF_Root%drop"
)

if /i "%~1"=="formodel" (
	%buildtool% %currentPath%\deploy\Installer\ProtocolTestFrameworkInstaller.wixproj /p:FORMODEL="1" /t:Clean;Rebuild /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=%ptfsnk%
) else (
	%buildtool% %currentPath%\deploy\Installer\ProtocolTestFrameworkInstaller.wixproj /t:Clean;Rebuild /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=%ptfsnk%
)

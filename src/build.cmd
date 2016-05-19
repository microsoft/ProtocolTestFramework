:: Copyright (c) Microsoft. All rights reserved.
:: Licensed under the MIT license. See LICENSE file in the project root for full license information.

@echo off

if not defined buildtool (
	for /f %%i in ('dir /b /ad /on "%windir%\Microsoft.NET\Framework\v4*"') do (@if exist "%windir%\Microsoft.NET\Framework\%%i\msbuild".exe set buildtool=%windir%\Microsoft.NET\Framework\%%i\msbuild.exe)
)

if not defined buildtool (
	echo No msbuild.exe was found, install .Net Framework version 4.0 or higher
	goto :eof
)

if not defined WIX (
	echo WiX Toolset version 3.10 should be installed
	goto :eof
)

if "%WIX:3.10=%"=="%WIX%" (
	echo WiX Toolset version 3.10 should be installed
	goto :eof
)

:: Check if visual studio or test agent is installed, since HtmlTestLogger depends on that.
if not defined vspath (
	if defined VS110COMNTOOLS (
		set vspath="%VS110COMNTOOLS%"
	) else if defined VS120COMNTOOLS (
		set vspath="%VS120COMNTOOLS%"
	) else if defined VS140COMNTOOLS (
		set vspath="%VS140COMNTOOLS%"
	) else (
		echo Visual Studio or Visual Studio test agent should be installed, version 2012 or higher
		goto :eof
	)
)

set currentPath=%~dp0
set PTF_Root=%currentPath%..\

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


:: Copyright (c) Microsoft. All rights reserved.
:: Licensed under the MIT license. See LICENSE file in the project root for full license information.

:: build PTF -- call build.cmd
:: build PTF with specific build number -- call build.cmd x.x.x.x
:: build PTF for model based testing -- call build.cmd formodel
:: build PTF for model based testing with specific build number -- call build.cmd x.x.x.x formodel

@echo off

if not defined buildtool (
	for /f %%i in ('dir /b /ad /on "%windir%\Microsoft.NET\Framework\v4*"') do (@if exist "%windir%\Microsoft.NET\Framework\%%i\msbuild".exe set buildtool=%windir%\Microsoft.NET\Framework\%%i\msbuild.exe)
)

if not defined buildtool (
	echo No msbuild.exe was found, install .Net Framework version 4.0 or higher
	goto :eof
)

if not defined WIX (
	echo WiX Toolset version 3.7 or higher should be installed
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

if not defined ptfsnk (
	set ptfsnk=..\TestKey.snk
)

::Get build version from AssemblyInfo
set path=SharedAssemblyInfo.cs
set FindExe="%SystemRoot%\system32\findstr.exe"
set versionStr="[assembly: AssemblyVersion("1.0.0.0")]"
for /f "delims=" %%i in ('""%FindExe%" "AssemblyVersion" "%path%""') do set versionStr=%%i
set PTF_VERSION=%versionStr:~28,-3%

%buildtool% ptf.sln /t:clean

if /i "%~1"=="formodel" (
	%buildtool% deploy\Installer\ProtocolTestFrameworkInstaller.wixproj /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=%ptfsnk% /p:FORMODEL="1" /t:Clean;Rebuild
) else (
	%buildtool% deploy\Installer\ProtocolTestFrameworkInstaller.wixproj /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=%ptfsnk% /t:Clean;Rebuild
)

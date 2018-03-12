:: Copyright (c) Microsoft. All rights reserved.
:: Licensed under the MIT license. See LICENSE file in the project root for full license information.

@echo off

:: Set buildtool
:: Find Visual Studio 2017 if existed
if exist "%programfiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" (
    set buildtool="%programfiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\msbuild.exe"
) else if exist "%programfiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe" (
    set buildtool="%programfiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"
) else if exist "%programfiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" (
    set buildtool="%programfiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
) else (
    for /f %%i in ('dir /b /ad /on "%windir%\Microsoft.NET\Framework\v4*"') do (@if exist "%windir%\Microsoft.NET\Framework\%%i\msbuild".exe set buildtool=%windir%\Microsoft.NET\Framework\%%i\msbuild.exe)
)

if not defined buildtool (
    echo No msbuild.exe was found, install .Net Framework version 4.7.1 or higher
    exit /b 1
)

:: Set vspath
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\Common7\Tools\" (
    set VS150COMNTOOLS = "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\Common7\Tools\"
)

if not defined vspath (
    if defined VS150COMNTOOLS (
        set vspath="VS150COMNTOOLS"
    ) else if defined VS140COMNTOOLS (
        set vspath="%VS140COMNTOOLS%"
    ) else if defined VS120COMNTOOLS (
        set vspath="%VS120COMNTOOLS%"
    ) else (
        echo Visual Studio or Visual Studio test agent should be installed, version 2013 or higher
        exit /b 1
	)
)

# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

$slnRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Create the folder, which will be used to store the final .nupkg file
New-Item -ItemType Directory -Path $slnRoot\..\drop

Push-Location $slnRoot\TestFramework

$csproj = [xml](Get-Content .\TestFramework.csproj)
$assembly = $csproj.Project.PropertyGroup.AssemblyName
$version = $csproj.Project.PropertyGroup.Version

# Build the nuget package
dotnet clean --configuration Release
dotnet pack --configuration Release

# Copy the nuget package to drop folder
Copy-Item bin\Release\$assembly.$version.nupkg $slnRoot\..\drop\

Pop-Location

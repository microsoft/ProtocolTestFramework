# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

$slnRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
Push-Location $slnRoot
if(Test-Path drop){
    Remove-Item drop\* -Recurse -Force
}else{
    New-Item -ItemType Directory -Path drop
}
dotnet build .\TestFramework\TestFramework.csproj --configuration Release

dotnet publish .\TestFramework\TestFramework.csproj -o drop -f net5.0

dotnet publish .\TestFramework\TestFramework.csproj -o drop/net6 -f net6.0

nuget pack drop\TestFramework.nuspec
$packageFile = [xml](Get-Content drop\TestFramework.nuspec)
$assembly = $packageFile.package.metadata.id
$version = $packageFile.package.metadata.version


Copy-Item .\$assembly.$version.nupkg drop
Pop-Location
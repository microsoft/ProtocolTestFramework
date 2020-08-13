# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

slnRoot=$(pwd -P)

# Create the folder, which will be used to store the final .nupkg file
mkdir $slnRoot/../drop

pushd $slnRoot/TestFramework

csproj='TestFramework.csproj'
assembly=$(awk -F "[><]" '/AssemblyName/{print $3}' $csproj)
version=$(awk -F "[><]" '/Version/{print $3}' $csproj)

# Build the nuget package
dotnet clean --configuration Release
dotnet pack --configuration Release

# Copy the nuget package to drop folder
cp ./bin/Release/$assembly.$version.nupkg $slnRoot/../drop/

popd

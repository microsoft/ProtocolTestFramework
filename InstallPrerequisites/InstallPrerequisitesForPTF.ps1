# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Create a tempoary folder under current folder, which is used to store downloaded files.
Function CreateTemporaryFolder
{
    #create temporary folder for downloading tools
    $tempPath = (get-location).ToString() + "\" + [system.guid]::newguid().ToString()
    Write-Host "Create temporary folder for downloading files"``
    $outFile = New-Item -ItemType Directory -Path $tempPath
    Write-Host "Temporary folder $outFile is created"

    return $outFile.FullName
}

# Download files
Function DownloadFiles
{
    param(        
        $Files
    )

	ForEach ($File in $Files)
    {
        $Uri = $prefix + $File.title
        $OutFile = $tempFolder + "\" + $File.title
        try {
            Invoke-WebRequest -Uri $Uri -OutFile $OutFile
        }
        catch
        {
            try
            {
                (New-Object System.Net.WebClient).DownloadFile($Uri, $OutFile)
            }
            catch
            {
                Write-host "Download $item.Name failed with exception: $_.Exception.Message"
                Return
            }
        }
    }    
}

$ifInstallPrerequisitesExist = Test-Path .\InstallPrerequisites.ps1
$ifInstallVs2017Community = Test-Path .\InstallVs2017Community.cmd
$ifPrerequisitesConfigExist = Test-Path .\PrerequisitesConfig.xml
if (!$ifInstallPrerequisitesExist -or
    !$ifInstallVs2017Community -or
    !$ifPrerequisitesConfigExist) {
    
    try {
        $prefix = 'https://raw.githubusercontent.com/Microsoft/WindowsProtocolTestSuites/staging/InstallPrerequisites/'
        $WebResponse = Invoke-WebRequest https://github.com/Microsoft/WindowsProtocolTestSuites/tree/staging/InstallPrerequisites/ -UseBasicParsing

        # Download all .cmd Files
        $CmdFiles = $WebResponse.Links |?{$_.href -match ".cmd"}
        DownloadFiles -Files $CmdFiles

        # Download all .xml Files
        $XmlFiles = $WebResponse.Links |?{$_.href -match ".xml"}
        DownloadFiles -Files $XmlFiles

        # Download all .ps1 Files
        $Ps1Files = $WebResponse.Links |?{$_.href -match ".ps1"}
        DownloadFiles -Files $Ps1Files
    }
    catch {
        Write-Host "Please download the following InstallPrerequisites-related files manually from Github page https://github.com/Microsoft/WindowsProtocolTestSuites/tree/staging/InstallPrerequisites"
        Write-Host "1. InstallPrerequisites.ps1"
        Write-Host "2. InstallVs2017Community.cmd"
        Write-Host "3. PrerequisitesConfig.xml"
        exit
    }
}

# Execute InstallPrerequisites.ps1
.\InstallPrerequisites.ps1 -Category 'PTF'

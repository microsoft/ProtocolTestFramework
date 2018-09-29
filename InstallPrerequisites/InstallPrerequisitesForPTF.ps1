# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Download files
Function DownloadFile
{
    param(        
        $File
    )

    $Uri = $prefix + $File.title
    $OutFile = $File.title
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

$ifInstallPrerequisitesExist   = Test-Path .\InstallPrerequisites.ps1
$ifInstallVs2017CommunityExist = Test-Path .\InstallVs2017Community.cmd
$ifPrerequisitesConfigExist    = Test-Path .\PrerequisitesConfig.xml
if (!$ifInstallPrerequisitesExist   -or
    !$ifInstallVs2017CommunityExist -or
    !$ifPrerequisitesConfigExist) {
    
    try {
        $prefix = 'https://raw.githubusercontent.com/Microsoft/WindowsProtocolTestSuites/staging/InstallPrerequisites/'
        $WebResponse = Invoke-WebRequest https://github.com/Microsoft/WindowsProtocolTestSuites/tree/staging/InstallPrerequisites/ -UseBasicParsing

        # Download InstallVs2017Community.cmd
        if (!$ifInstallVs2017CommunityExist) {
            $CmdFile = $WebResponse.Links |?{$_.href -match "InstallVs2017Community.cmd"}
            DownloadFile -File $CmdFile
        }

        # Download PrerequisitesConfig.xml
        if (!$ifPrerequisitesConfigExist) {
            $XmlFile = $WebResponse.Links |?{$_.href -match "PrerequisitesConfig.xml"}
            DownloadFile -File $XmlFile
        }

        # Download InstallPrerequisites.ps1
        if (!$ifInstallPrerequisitesExist) {
            $Ps1File = $WebResponse.Links |?{$_.href -match "InstallPrerequisites.ps1"}
            DownloadFile -File $Ps1File
        }
    }
    catch {
        Write-Host "Please download the following InstallPrerequisites-related files manually from WindowsProtocolTestSuites Github page https://github.com/Microsoft/WindowsProtocolTestSuites/tree/staging/InstallPrerequisites"
        Write-Host "1. InstallPrerequisites.ps1"
        Write-Host "2. InstallVs2017Community.cmd"
        Write-Host "3. PrerequisitesConfig.xml"
        exit
    }
}

# Execute InstallPrerequisites.ps1
.\InstallPrerequisites.ps1 -Category 'PTF'

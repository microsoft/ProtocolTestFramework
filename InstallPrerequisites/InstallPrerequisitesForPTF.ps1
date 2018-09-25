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

$tempFolder = CreateTemporaryFolder
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

# Execute InstallPrerequisites.ps1
cd $tempFolder
.\InstallPrerequisites.ps1 -Category 'PTF'
cd ..

if(Test-Path $tempFolder)
{
    Write-Host "Remove temporary folder"
    Remove-Item -Path $tempFolder -Recurse -Force
}

# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

Function DownloadFiles
{
    param(        
        $Files
    )

	ForEach ($File in $Files)
    {
        $Uri = $prefix + $File.title
        try {
            Invoke-WebRequest -Uri $Uri -OutFile $File.title
        }
        catch
        {
            try
            {
                (New-Object System.Net.WebClient).DownloadFile($Uri, $File.title)
            }
            catch
            {
                Write-host "Download $item.Name failed with exception: $_.Exception.Message"
                Return
            }
        }
    }    
}

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
.\InstallPrerequisites.ps1 -Category 'PTF'

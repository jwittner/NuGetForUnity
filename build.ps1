param([string]$OutputDirectory = ".\bin")

Import-Module UnitySetup -ErrorAction Stop -MinimumVersion 4.0.97

Write-Host "Build NuGetForUnity " -ForegroundColor Green

# Launch Unity to export the NuGetForUnity package
Start-UnityEditor -Project ".\" -BatchMode -Quit -Wait -ExportPackage "Assets/NuGet Assets/csc.rsp .\NuGetForUnity.unitypackage" -LogFile ".\NuGetForUnity.unitypackage.log"

# Copy artifacts to output directory
if ( !(Test-Path $OutputDirectory) ) { New-Item -ItemType Directory $OutputDirectory }
Move-Item ".\NuGetForUnity.unitypackage*" $OutputDirectory

param(
	[Parameter(Mandatory=$true, HelpMessage = "path for DWIN_SET folder")] [string] $Path,
	[Parameter(Mandatory=$false, HelpMessage = "COM port")] [string] $Port
)
Write-Output "DgusDude by Udi Azulay 2021"
function LoadDgusAsm
{
	if ([System.Type]::GetType("DgusDude.Device", $false)) { return }
	$dllPath = ""
	$path = $PSScriptRoot, "$PSScriptRoot/../bin/Debug/net4.8/"
	foreach ($p in $path) {
		$dllPath = "$($p)DgusDude.dll"
		if ([System.IO.File]::Exists($dllPath)) { break }
	}
	if(-Not $dllPath) { $dllPath = Read-Host "Enter location of DgusDude.dll" }
	[System.Reflection.Assembly]::LoadFrom($dllPath)
}

function New-DgusDevice
{
	param([string] $comPort)
	[void](LoadDgusAsm)
	$savePort = $false
	$device = New-Object -TypeName DgusDude.Devices.T5UID1
	if (-Not $comPort) { $comPort = $global:DgusDevicePort }
	if (-Not $comPort) {
		#Write-Output "Config", $device.Config
		$comPort = Read-Host "COM-Port ($([string]::Join(", ", [System.IO.Ports.SerialPort]::GetPortNames())))"
		$savePort = $true
	}
	$device.Open($comPort)
	if ($savePort) { $global:DgusDevicePort = $comPort }
	return $device
}
function Update-DgusDevice
{
	PARAM (
		[Parameter(Mandatory=$true)][DgusDude.Device] $device,
		[Parameter(Mandatory=$true)][string] $dwinset_dir
	)
	BEGIN { Write-Output "Uploading $($dwinset_dir)" }
	PROCESS {
		foreach($file in Get-ChildItem($dwinset_dir)) {
			Write-Output $file
			if(-Not $device.Upload($file, $false)) { Write-Output "Skipped" }
		}
		$device.Reset()
	} 
	END { Write-Output "Upload complete" }
}
function Convert-Image
{
	Param([string] $fileName, [System.Drawing.Size] $size)
	Import-Module System.Drawing 
	Import-Module System.Drawing.Imaging
	Use-Object(($image = [system.drawing.image]::FromFile($fileName))){
		$image.Save($outFile, $encoder.FormatId)
	}
}

if ($Path) { 
	$dev = New-DgusDevice($Port)
	try{
		Update-DgusDevice $dev $Path
	}finally{
		$dev.Close()
	}
}

#example: loop all pictures on device with 1s delay
Import-Module $PSScriptRoot\DgusDevice.ps1
$dev1 = New-DgusDevice
try{
	Write-Output "Start pictures loop"
	for ($pic = 0; $pic -lt $dev1.Pictures.Length; $pic++)
	{
		$dev1.Pictures.Current = $pic
		Start-Sleep -Seconds 1
	}
	Write-Output "End pictures loop"
}finally{
	$dev1.Close()
}

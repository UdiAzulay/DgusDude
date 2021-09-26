#example: Reset dgus device
Import-Module $PSScriptRoot\DgusDevice.ps1
$device = New-DgusDevice
try {
    $device.Reset()
}finally{
    $device.Close()
}
#example: print device info

Import-Module $PSScriptRoot\DgusDevice.ps1
$device = New-DgusDevice
try {
    Write-Output $device
    if (-Not $device.Connected) { return }
    Write-Output ("Touch        : " + $device.GetTouch().ToString())
    if ($device -is [DgusDude.T5.T5Device]) {
        Write-Output ("DeviceConfig : " + $device.GetDeviceConfig().ToString())
        Write-Output ("Brightness   : " + $device.GetBrightness().ToString())
        Write-Output "", $device.DeviceInfo
    }
}finally{
    $device.Close()
}
#example: print device info

Import-Module $PSScriptRoot\DgusDevice.ps1
$device = New-DgusDevice
try {
    Write-Output $device
    if (-Not $device.Connected) { return }
    Write-Output $device.GetDeviceInfo()
    if ($device -is [DgusDude.T5.T5Core]) {
        Write-Output ("DeviceConfig : " + $device.GetDeviceConfig().ToString())
        Write-Output ("Brightness   : " + $device.GetBrightness().ToString())
    }
    if ($device.Touch) {
        Write-Output ("Touch        : " + $device.Touch.Current.ToString())
    }
}finally{
    $device.Close()
}
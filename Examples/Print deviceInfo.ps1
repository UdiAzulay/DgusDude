#example: print device info

Import-Module $PSScriptRoot\DgusDevice.ps1
$device = New-DgusDevice
try {
    Write-Output "Device", $device
    Write-Output "DeviceInfo: ", $device.DeviceInfo
    Write-Output "DeviceConfig: ", $device.DeviceConfig
    Write-Output "SRAM: ", $device.SRAM
    Write-Output "Register: ", $device.Register
    Write-Output "Nand: ", $device.Nand
    Write-Output "Nor: ", $device.Nor
    Write-Output "Pictures: ", $device.Pictures
    Write-Output "Music: ", $device.Music
    Write-Output "ADC: ", $device.ADC
    Write-Output "PWM: ", $device.PWM
}finally{
    $device.Close()
}
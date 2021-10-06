#example - update PC CPU time and free memory to dgus device and wait for key press

Import-Module $PSScriptRoot\..\DgusDevice.ps1
Add-Type -AssemblyName System.Windows.Forms

$device = New-DgusDevice
try {
    #start with uploading DWIN_SET project to device
    $shouldUpload = Read-Host("Upload DWIN_SET?[y/n]")
    if ($shouldUpload -match "[yY]") { 
        Update-DgusDevice $device "$PSScriptRoot/DWIN_SET/*.*" 
        Start-Sleep -Seconds 4
    }
    Write-Output "Start update pc status" 
    $device.Pictures.Current = 1
    $data = New-Object DgusDude.UserPacket -ArgumentList $device.VP, 0x6000, 64
    while ($true) {
        $values = 
            [float](Get-Counter '\Processor(_Total)\% Processor Time').CounterSamples.CookedValue, 
            [int](Get-Counter '\Memory\Available MBytes').CounterSamples.CookedValue
        #Write-Output $values
        $data.SetValue(0, [long] ($values[0] * 100))
        $data.SetValue(8, $values[1])
        $data.Update()
        
        if (-Not $device.Touch){ Continue }
        #read buttons
        $readData = $device.Touch.ReadKey(1000) #wait 1 s
        if (-Not $readData) { Continue }
        $btnId = $readData.GetShort(1)
        if ($btnId -Eq 1) { 
            Write-Output "DoAlert"
            [void][System.Windows.Forms.Messagebox]::Show("Message from DGUS device")
        } elseif ($btnId -Eq 2) { 
            Write-Output "DoSleep"
            [System.Windows.Forms.Application]::SetSuspendState("Suspend", $false, $false)
        }
    }
} finally {
    $device.Close()
}

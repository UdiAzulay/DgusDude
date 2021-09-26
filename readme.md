DWIN DGUS T5UIDx .Net library and download tool by Udi Azulay 2021

a .Net libaray to control DGUS devices throu serial port

-About
I originally developed it after failing to upload a custom firmware to my 3d printer using the SD card,
for some reason my SD Card stop loading the update screen and
it seems that my version of DWIN OS is NoAck (does not response 0x4F4B for 0x83 commands)
so for me, DWIN uploader natural tool fail with timeout error

This project also contains small script that allow you to upload DWIN_SET folder without SD card  (for supported devices)

-Supported files 
	.BMP, JPG, BIN, LIB, HZK, DZK, ICO, DWINOS*.BIN

-Direct Upload from powershell
.Examples/DgusDevice.ps1 -Path "DWIN_SET/*.*"

-Direct upload from CMD
Powershell -executionpolicy remotesigned -File Examples\DgusDevice.ps1 -Path "DWIN_SET/*.*"

-Example DWIN project to show CPU usage and MEM usage on DGUS device
see Examples directory for help and samples


a .Net libaray and download tool to control DWIN DGUS devices over serial port

Supported devices: T5, T5L, K600, Mini DUGS (partial support)

Supported file types: BMP, JPG, BIN, LIB, HZK, DZK, ICO, ICL, DWINOS*.BIN

About:
I originally developed it after failing to upload a custom firmware to my 3d printer using the SD card,
for some reason my SD interface stop loading the update screen and
it seems that my version of DWIN OS is NoAck (does not response 0x4F4B for 0x83 commands)
so for me, DWIN uploader natural tool fail with timeout error

This project also contains small script that allows you to upload DWIN_SET folder without SD card  (for supported devices)
and an Example DWIN project that shows local PC CPU and MEM usage on connected DGUS device 
	(see <a href="Examples">Examples</a> directory for more help and samples)

known issues:
WAV uploader not working, only integer VP values can be updated (issue with double and TEXT mapping)

**tested only on T5UID device (the only device I own), 
other devices implemented based on <a target="_blank" href="http://www.dwin.com.cn/service/en/file/id/13">DWIN documentation</a> but never tested.
in any case, USE ON YOUR OWN RISK!!!

Direct upload from PowerShell
<pre>.Examples/DgusDevice.ps1 -Path "DWIN_SET/*.*"</pre>

Direct upload from CMD
<pre>Powershell -executionpolicy remotesigned -File Examples\DgusDevice.ps1 -Path "DWIN_SET/*.*"</pre>

Usage:
<pre>
using DgusDude;

//create using parameters
var d = Device.Create(Platform.T5 | Platform.UID1 | Platform.TouchScreen, 
		new DgusDude.Core.Screen(480, 270, 0 /*pixel format*/, 4.3), null /*custom flash size*/);
	
//or create using model number
//var d = Device.Create("DMT48270C043_06WT");

using (d) {				//implicit dispose
	d.Open("com1", 115200);		//open serial port
	d.Upload(@"c:\3 image.jpg");	//upload picture to location 3
	d.Pictures.Current = 3;		//set device current picture
	var pressed = d.Touch?.ReadKey();	//wait for device touch screen button press
	d.Close();
}
</pre>

Output of print device info example: (for T5UID1)
<pre>
PS D:\DgusDude\Examples> D:\DgusDude\Examples\Print deviceInfo.ps1
DgusDude by Udi Azulay 2021
COM-Port (COM6, COM7, COM10, COM14): com14

Platform     : T5, UID1, TouchScreen
Screen       : 4.3 Inch, 272x480 2bpp
Registers    : 2kb      (Align:1, Block:248, Page:256)
RAM          : 128kb    (Align:2, Block:248, Page:0)
Storage      : 65536kb  (Align:4, Block:32768, Page:524288)
UserSettings : 320kb    (Align:4, Block:0, Page:4096)
Buffer       : 64kb, 0x10000:20000
VP           : VP On SRAM
Pictures     : Max 64 items
Music        : Max 256 items
PWM          : 3 Items
ADC          : 4 Items
Config       : Header: 5A-A5, Retries: 10, Options: NoAckRAM
Connection   : System.IO.Ports.SerialPort
Connected    : True

DeviceID      : 8671135961630377990
Version       : (20, 32)
Time          : 01/01/0001 0:00:00
IsIdle        : True
Vcc           : 3.28027833730086  
CpuTemprature : 0
SDUploadDir   :

DeviceConfig : TouchTone, SDEnabled, InitWith22Config, CheckCRC, Touch Mode: 7 Sensitivity: 20
Brightness   : Normal: 100, StandBy: 100 after: 2000ms
Touch        : X:250 Y:150 Lift
</pre>
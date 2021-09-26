ORG 1000H; The current version of T5 OS 2.07 and above has been required to strictly follow the standard format of the following lines
GOTO MAIN; The first instruction of the code must be GOTO
NOP ;GOTO T0INT; When an interrupt is generated, jump to the T0 interrupt handler, GOTO must be used, CALL cannot be used
NOP ;GOTO T1INT interrupt is not used
NOP ;GOTO T2INT ;When an interrupt occurs, jump to the T2 interrupt handler
ORG 1080H; Jump to the main function MAIN

MAIN:
	NOP
	GOTO MAIN
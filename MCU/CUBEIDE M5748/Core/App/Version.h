#ifndef _VERSION_H_
#define _VERSION_H_


/*
  --- d1.01 15/10/2024 ---
main.c: TIM1 channel 2 inverter - for syncronizing the two HPFs in 180deg.
Communication speed increased from 100mSec to 50mSec:
  - mgmt.c: removed 5mSec delay between end of tel reception and transmition of the next command
  - when switching comunication to previously inactive module (or transmitting to bothe modules) a delay of at lest 170uSec should be allow for the 
      transmiter to exit shutdown mod. previously a full cycle loop time was alocated for that (5mSec). To save this time several functions were added such
      intUart_Handle(), isSwitchDelayEnded_Handler() and other to allow switching module and transmitting after only 400 uSec. 
      This change enables to save 15mSec during each comm cycle.
  - TODO: all mechanizm that relay on communication cycle timing should be verified like Iout and Vout corrections
  - Command.c: fillBuckCmd() modified to support simultanious Vout set of both BUCKs
  - Command.c: OPCODE_BUCK_TEL_REQUEST implemented
  - adding HPF,ISO,BUCK ready as a conditions for turning on the output - without that it was posible to "turn On" the output durib unput U.V. for example and get output UV failure
  - added ResetSDReason flag to HPF tel request (tel request lenght increased to 2)
  - added SDReason mechanizm
  - tcp_ecoserver and SCPI was extancively modified to support simultanius communication from two ports
    several bugs were handaled during this process
*/

//#warning "disable stats"
// undefine LWIP_STATS in lwipopts.h
// and MEM_STATS, MEMP_STATS, TCP_STATS in opt.h

/*
  --- v1.00 30/9/2024 ---
  based on M5521 v1.04

*/





#define SOFTWARE_VERSION 0x0004
#define INTERFACE_VERISION 0x00
#define SOFTWARE_VERSION_STR "M5748-601 Batt v0.04"

#endif //_VERSION_H_

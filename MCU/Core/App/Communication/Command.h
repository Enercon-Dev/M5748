#ifndef _COMMAND_H_
#define _COMMAND_H_

#include "IntComm.h"
#include "DataBuffer.h"

#define OPCODE_SIZE 1
//#define OPCODE_ACK 0x8000

//--- Commands ---
#define OPCODE_TEL_STATUS_REQUEST  0x01
#define OPCODE_ISO_TEL_REQUEST  0x02
#define OPCODE_BUCK_SET_STATE   0x04
#define OPCODE_BUCK_TEL_REQUEST 0x03

//--- Telemetries ---
#define OPCODE_TEL_STATUS_RESP  0x81
#define HPF_STATUS_TEL_LENGTH 27
#define OPCODE_ISO_STATUS_TEL  0x82
#define ISO_STATUS_TEL_LENGTH 15
#define OPCODE_BUCK_STATUS_TEL 0x83
#define BUCK_STATUS_TEL_LENGTH 28
#define OPCODE_OUTPUT_CONTROL_TEL  0x84


//Boot Loader Commands:
#define OPCODE_WRITE_MEMORY  0x12
#define OPCODE_CLEAR_MEMORY  0x13
#define OPCODE_CHANGE_BOOT   0x14
#define OPCODE_READ_MEMORY   0x52

#define LAST_TEL_LENGTH (HPF_STATUS_TEL_LENGTH+ISO_STATUS_TEL_LENGTH+BUCK_STATUS_TEL_LENGTH)
extern uint8_t lastTelBuffer[MODULES_NUM][LAST_TEL_LENGTH+20];

AckCode decodeCommand(DataBuffer* db);


AckCode fillTelemetry(DataBuffer* db, int opCode);
AckCode fillControlStatusTel(DataBuffer* db);
AckCode decodeOutputControl(DataBuffer* db);

//Bootloader and SW upload commands
AckCode clearMemCmd(DataBuffer* db);
AckCode writeMemCmd(DataBuffer* db);


#endif

#ifndef _COMMAND_H_
#define _COMMAND_H_

#include "IntComm.h"
#include "DataBuffer.h"

#define OPCODE_SIZE 1
#define OPCODE_ACK 0x80

//--- Commands ---
#define OPCODE_TEL_REQUEST_CMD 0xF0

//Boot Loader Commands:
#define OPCODE_WRITE_MEMORY  0x12
#define OPCODE_CLEAR_MEMORY  0x13
#define OPCODE_CHANGE_BOOT   0x14
#define OPCODE_READ_MEMORY   0x52

//--- Telemetries ---
#define OPCODE_CHARGER_STATUS_TEL  0x81
#define CHARGER_STATUS_TEL_LENGTH 10
#define OPCODE_MASTER_STATUS_TEL  0x82
#define MASTER_STATUS_TEL_LENGTH 30
#define OPCODE_BATT_STATUS_TEL 0x83
#define BATT_STATUS_TEL_LENGTH 10
#define OPCODE_SOFTWARE_VERSION_TEL 0x8F
#define OPCODE_READ_MEMORY_TEL 0x91
#define OPCODE_WRITE_ACK_TEL 0x92



#define LAST_TEL_LENGTH (CHARGER_STATUS_TEL_LENGTH+MASTER_STATUS_TEL_LENGTH+BATT_STATUS_TEL_LENGTH*BATT_NUM)
extern uint8_t lastTelBuffer[LAST_TEL_LENGTH+10];

AckCode decodeCommand(DataBuffer* db, struct TelRequest* tel);


AckCode fillTelemetry(DataBuffer* db, struct TelRequest* tel);
//AckCode fillControlStatusTel(DataBuffer* db);
#endif

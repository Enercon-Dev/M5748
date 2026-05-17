
#include "general.h"
#include "DataBuffer.h"
#include "Version.h"
#include "RowIO.h"
#include "IO.h"
#include "IntComm.h"
#include "Command.h"
//#include "StatisticsTel.h"
//#include "BootLoader.h"
//#include "PersistentData.h"
#include "flash_if.h"
//#include "ReportedData.h"
#include "Mgmt.h"
#include <string.h>

//#include "DBG.h"


uint8_t lastTelBuffer[LAST_TEL_LENGTH+10];

void setTel(struct TelRequest* tel, int telShort, int telLong, int delay);
//int setOutputParams(DataBuffer* db, int out, uint32_t bToSet);
AckCode decodeTelRequestCmd(DataBuffer* db,  struct TelRequest* tel);
AckCode decodeMasterStatusTel(DataBuffer* db,  struct TelRequest* tel);
AckCode writeMemCmd(DataBuffer* db,  struct TelRequest* tel);
AckCode clearMemCmd(DataBuffer* db,  struct TelRequest* tel);
AckCode changeBootCmd(DataBuffer* db,  struct TelRequest* tel);

AckCode fillChargerStatusTel(DataBuffer* db, struct TelRequest* tel);
AckCode fillSwVersion(DataBuffer* db, struct TelRequest* tel);
AckCode fillWriteAckTel(DataBuffer* db, struct TelRequest* tel);
AckCode fillReadMemoryTel(DataBuffer* db, struct TelRequest* tel);


//-----------------------------------------------------------------------------
//----------------------------- Command Decoders ------------------------------
//-----------------------------------------------------------------------------
struct CmdDecodeMap
{
  int opCode;
  AckCode (*decodeFunc)(DataBuffer* db,  struct TelRequest* tel);
};

static const struct CmdDecodeMap cmdDecodeMap[] =
{
  {OPCODE_TEL_REQUEST_CMD, decodeTelRequestCmd},
  {OPCODE_MASTER_STATUS_TEL, decodeMasterStatusTel},
  {OPCODE_WRITE_MEMORY, writeMemCmd}, 
  {OPCODE_CLEAR_MEMORY, clearMemCmd},
  {OPCODE_CHANGE_BOOT, changeBootCmd}
};
#define SIZEOF_CMD_DECODE_MAP (sizeof(cmdDecodeMap) / sizeof(struct CmdDecodeMap))


AckCode decodeCommand(DataBuffer* db,  struct TelRequest* tel)
{
  int i;
  AckCode result;

  int opCode = getByte(db);
  for (i=0; i<SIZEOF_CMD_DECODE_MAP; i++)
  {
    if (opCode == cmdDecodeMap[i].opCode)
    {
       result = cmdDecodeMap[i].decodeFunc(db, tel);
       //if (result == Ack_NoError)
       //  mgmt.lastReceivedTel = opCode;
      return result;
    }
  }
  
  // OpCode not found in the cmdDecodeMap
  // not standart command decode may be added here
 
  return Ack_WrongOpCode;
}


AckCode decodeTelRequestCmd(DataBuffer* db,  struct TelRequest* tel)
{
  int i;
  //check tel tength
  if (getRemainingLength(db) < 1)
    return Ack_WrongLength;
  
  uint16_t telOpCode = getByte(db);
  
  tel->ackTelTmp = AckTel_Long;
  setTel(tel, telOpCode, telOpCode, 0);
  
  
  for (i=0; i<TEL_REQUEST_PARAM_LENGTH ; i++)
  {
    if (getRemainingLength(db) <= 0)
      break;
    tel->param[i] = getByte(db);
  }
  tel->paramLength = i;
  
  return Ack_NoError;
}

AckCode decodeMasterStatusTel(DataBuffer* db,  struct TelRequest* tel)
{
  int32_t flags;
  
  if (getRemainingLength(db) < (MASTER_STATUS_TEL_LENGTH-1))
    return Ack_WrongLength;
  
  setPosition(db, getPosition(db)+22);
  flags = getByte(db);
  mgmt.bOutputOK_CMD         = GETBIT(flags, 7);
  mgmt.bISOFault_CMD         = GETBIT(flags, 6);
  mgmt.bFaultDetected_CMD    = GETBIT(flags, 5);
  mgmt.bFaultDetectedLed_CMD = GETBIT(flags, 4);
  mgmt.bSpareOut_CMD         = GETBIT(flags, 2);
  
  return Ack_NoError;
}

/*
int decodeHpfStatus(DataBuffer* db, struct ReportedHpfData* hpfData)
{
  
  int32_t flags;
  
  if (getRemainingLength(db) < (HPF_STATUS_TEL_LENGTH-1))
    return -1;
  
  hpfData->version = getByte(db);
  getShort(db); //not decoded flags
  flags = getByte(db);
  hpfData->bHPFSync      = GETBIT(flags, 4);
  hpfData->bInrushEnded  = GETBIT(flags, 3);
  hpfData->bUvcc_aux     = GETBIT(flags, 2);
  hpfData->BusHOV        = GETBIT(flags, 1);
  hpfData->BusLOV        = GETBIT(flags, 0);
  
  getShort(db); //PH_A
  getShort(db); //PH_B
  getShort(db); //PH_C
  hpfData->bus_p = getShort(db) * 54989 / 1024; //10mV resolution
  hpfData->bus_n = getShort(db) * 54989 / 1024; //10mV resolution
  hpfData->bus = hpfData->bus_p + hpfData->bus_n;
  hpfData->temp = tempConvert(getShort(db)*4);
  hpfData->vcc3_3 = getShort(db) * 500 / 1024;
  hpfData->vinRect = getShort(db) * 100004 / 1024;
  hpfData->errorAmp = getShort(db);
  hpfData->balancerErrorAmp = getShort(db);
  
  flags = getByte(db);
  hpfData->bInrushEn     = GETBIT(flags, 7);
  hpfData->bConvertersEn = GETBIT(flags, 6);
  hpfData->bLoadEn       = GETBIT(flags, 5);
  hpfData->bPhaseLoss    = GETBIT(flags, 4);
  hpfData->bInOk         = GETBIT(flags, 3);
  hpfData->bDelayedInOk  = GETBIT(flags, 2);
  hpfData->OVLock        = GETBIT(flags, 1);
  hpfData->ShdnReason = getByte(db);
  //total 26 bytes
  return 0;
}
*/

#define LOADER_PASSWORD 0xBCE31F90

AckCode writeMemCmd(DataBuffer* db,  struct TelRequest* tel)
{
  if (getRemainingLength(db) < 8 + 2) {
    // not enough data in buffer for address and length
    return Ack_WrongLength;
  }
  
  if (getLong(db) != LOADER_PASSWORD) {
    //wrong password
    return Ack_WrongParameter;
  }
  
  uint32_t baseAddr = getLong(db);
  uint32_t dataLen = getShort(db);
  
  if (!bVerifyBlockIndex(baseAddr, dataLen)) {
    //wrong Address or data length
    return Ack_WrongParameter;
  }
  
  if (dataLen > getRemainingLength(db)) {
    // not enough data in buffer
    return Ack_WrongLength;
  }
  
  if (!bVerifyBlockEmpty(baseAddr, dataLen)) {
    // block is not empty
    return ACK_GenerlError;
  }
  
  if (!writeBlock(baseAddr, dataLen, db)) {
    // write Fail
    return ACK_GenerlError;
  }
  
  //Prepare ACK tel
  tel->ackTelTmp = AckTel_Long;
  setTel(tel, OPCODE_WRITE_ACK_TEL, OPCODE_WRITE_ACK_TEL, 0);
  
  DataBuffer telDB;
  telDB.buffer = tel->param;
  telDB.length = TEL_REQUEST_PARAM_LENGTH;
  telDB.offset = 0;
  
  fillByte(&telDB, Ack_NoError);
  fillLong(&telDB, baseAddr);
  fillShort(&telDB, dataLen);
  tel->paramLength = telDB.offset;
  
  return Ack_NoError;
}


AckCode clearMemCmd(DataBuffer* db,  struct TelRequest* tel)
{
  if (getRemainingLength(db) < 4 ) {
    // not enough data in buffer for address and length
    return Ack_WrongLength;
  }
  
  if (getLong(db) != LOADER_PASSWORD) {
    //wrong password
    return Ack_WrongParameter;
  }
  
//  uint32_t blockAddr = getLong(db);
  
//  if (!bVerifyClearBlockIndex(blockAddr)) {
//    //wrong Address or data length
//    return Ack_WrongParameter;
//  }

//  if (!resetBlock(blockAddr)) {
//    //fail to reset the block
//    return ACK_GenerlError;
//  }
  
  FLASH_If_Erase(USER_FLASH_FIRST_PAGE_ADDRESS ,USER_FLASH_END_ADDRESS);
  //TODO: create a dedicated erase functionin Flesh_IT file
  
  return Ack_NoError;
}

//this is just a CPU reser command the actual firmware change is performed by boot loader
AckCode changeBootCmd(DataBuffer* db,  struct TelRequest* tel)
{
  if (getRemainingLength(db) < 4 ) {
    // not enough data in buffer password
    return Ack_WrongLength;
  }

  if (getLong(db) != LOADER_PASSWORD) {
    //wrong password
    return Ack_WrongParameter;
  }  


  if (!checkFlashCrc(TRUE)) {
    // CRC test fail
    return ACK_GenerlError;
  }
  
  NVIC_SystemReset();
  return Ack_NoError;
}



//-----------------------------------------------------------------------------
//----------------------------- Telemetry Fillers -----------------------------
//-----------------------------------------------------------------------------
struct TelDecodeMap
{
  int opCode;
  AckCode (*fillFunc)(DataBuffer* db, struct TelRequest* tel);
};

static const struct TelDecodeMap telDecodeMap[] = 
{
  {OPCODE_CHARGER_STATUS_TEL, fillChargerStatusTel},
  {OPCODE_WRITE_ACK_TEL, fillWriteAckTel},
  {OPCODE_READ_MEMORY_TEL, fillReadMemoryTel},
  {OPCODE_SOFTWARE_VERSION_TEL, fillSwVersion}
};
#define SIZEOF_TEL_DECODE_MAP (sizeof(telDecodeMap) / sizeof(struct TelDecodeMap))

AckCode fillTelemetry(DataBuffer* db, struct TelRequest* tel)
{
  int i;
  
  fillByte(db, tel->opCode);
  
  for (i=0; i<SIZEOF_TEL_DECODE_MAP; i++)
  {
    if ( tel->opCode == telDecodeMap[i].opCode)
    {
      return telDecodeMap[i].fillFunc(db, tel);
    }
  }
  
  // Tel OpCode not found in the telDecodeMap
  // not standart telemetry decode may be added here
  
  return Ack_WrongTelOpCode;
}

void setTel(struct TelRequest* tel, int telShort, int telLong, int delay)
{
  if (tel == NULL)
    return;

  if (tel->ackTelTmp == AckTel_Shotr)
    tel->opCode = telShort;
  else if (tel->ackTelTmp == AckTel_Long)
    tel->opCode = telLong;
  else
    return;
  tel->paramLength = 0;
  tel->delay = delay;
}


AckCode fillSwVersion(DataBuffer* db, struct TelRequest* tel)
{
  //const uint8_t boardId = BOARD_DEPENDENT(0,1,2);
  
  //fillByte(db,boardId);
  fillShort(db, SOFTWARE_VERSION); //SW version
  fillShort(db, INTERFACE_VERSION); //Interface version
  fillString(db, SOFTWARE_VERSION_STR, 32);
  return Ack_NoError;
}


AckCode fillChargerStatusTel(DataBuffer* db, struct TelRequest* tel)
{
  uint16_t flags;
  
  fillShort(db, SOFTWARE_VERSION);
  
  flags = 
    (inputs.sEPO.state        ? BIT(7) : 0) |
    (inputs.sEN_380VDC.state  ? BIT(6) : 0) |
    (inputs.sBattleMode.state ? BIT(5) : 0) |
    (inputs.sInSpare.state    ? BIT(4) : 0) ;
  fillByte(db, flags);

  flags = 
    (rowOut.bFault      ? BIT(7) : 0) |
    (rowOut.bFault_LED  ? BIT(6) : 0) |
    (rowOut.bISO_FLR    ? BIT(5) : 0) |
    (rowOut.bOUT350V_OK ? BIT(4) : 0) |
    (rowOut.bOutSpare   ? BIT(3) : 0) ;
  fillByte(db, flags);

  fillLongAsShort(db, anIn.PCBtemp);
  fillLongAsShort(db, anIn.VCC_IO);
  fillLongAsShort(db, anIn.In28V);
  fillLongAsShort(db, anIn.TestRef);
  
  
  return Ack_NoError; //12 bytes
}

AckCode fillWriteAckTel(DataBuffer* db, struct TelRequest* tel)
{
  fillArray(db, tel->param, tel->paramLength);
  return Ack_NoError;
}

AckCode fillReadMemoryTel(DataBuffer* db, struct TelRequest* tel)
{
  DataBuffer telDB;
  telDB.buffer = tel->param;
  telDB.length = tel->paramLength;
  telDB.offset = 0;
  
  if (getRemainingLength(&telDB) < 8 + 2) {
    // not enough data in buffer for address and length
    return Ack_WrongLength;
  }
  
  if (getLong(&telDB) != LOADER_PASSWORD) {
    //wrong password
    return Ack_WrongParameter;
  }
  
  uint32_t baseAddr = getLong(&telDB);
  uint32_t dataLen = getShort(&telDB);
  
  if (!bVerifyBlockIndex(baseAddr, dataLen)) {
    //wrong Address or data length
    return Ack_WrongParameter;
  }
  
  fillLong(db, baseAddr);
  fillShort(db, dataLen);
  fillArray(db, (uint8_t*)baseAddr,  dataLen);
  return Ack_NoError;
}



#include "general.h"
#include "DataBuffer.h"
#include "Version.h"
#include "RowIO.h"
#include "IO.h"
#include "IntComm.h"
#include "Command.h"
//#include "StatisticsTel.h"
//#include "BootLoader.h"
#include "flash_if.h"
#include "Mgmt.h"
#include <string.h>

//#include "DBG.h"


void setTel(struct TelRequest* tel, int telShort, int telLong, int delay);
//int setOutputParams(DataBuffer* db, int out, uint32_t bToSet);
AckCode decodeTelRequestCmd(DataBuffer* db,  struct TelRequest* tel);
AckCode decodeMasterStatusTel(DataBuffer* db,  struct TelRequest* tel);
AckCode writeMemCmd(DataBuffer* db,  struct TelRequest* tel);
AckCode clearMemCmd(DataBuffer* db,  struct TelRequest* tel);
AckCode changeBootCmd(DataBuffer* db,  struct TelRequest* tel);
AckCode decodeSwVersion(DataBuffer* db, struct TelRequest* tel);
AckCode decodeSetConstCmd(DataBuffer* db , struct TelRequest* tel);

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
  {OPCODE_SOFTWARE_VERSION_TEL, decodeSwVersion},
  {OPCODE_SET_CONST_CMD, decodeSetConstCmd},
  {OPCODE_WRITE_MEMORY, writeMemCmd}, 
  {OPCODE_CLEAR_MEMORY, clearMemCmd},
  {OPCODE_CHANGE_BOOT, changeBootCmd}
};
#define SIZEOF_CMD_DECODE_MAP (sizeof(cmdDecodeMap) / sizeof(struct CmdDecodeMap))


AckCode decodeCommand(DataBuffer* db,  struct TelRequest* tel)
{
  int i;
  AckCode result;

  mgmt.bCommRoundCompleted = FALSE;
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
  
  //telemetry request received so now the line is free and the tel can be returned
  lineIsFree(SOURCE_ADDRESS(tel->addressesTmp));
  
  return Ack_NoError;
}

AckCode decodeMasterStatusTel(DataBuffer* db,  struct TelRequest* tel)
{
  int32_t flags;
  
  if (getRemainingLength(db) < (MASTER_STATUS_TEL_LENGTH-1))
    return Ack_WrongLength;
  
  setPosition(db, getPosition(db)+4);
  
  masterData.OutL = getShort(db);
  masterData.OutR = getShort(db);
  masterData.OutMBD = getShort(db);
  
  setPosition(db, getPosition(db)+12);
  
  flags = getByte(db); // Outputs control signals
  masterData.bOutputOK_CMD         = GETBIT(flags, 7);
  masterData.bISOFault_CMD         = GETBIT(flags, 6);
  masterData.bFaultDetected_CMD    = GETBIT(flags, 5);
  masterData.bFaultDetectedLed_CMD = GETBIT(flags, 4);
  masterData.bSpareOut_CMD         = GETBIT(flags, 2);
  
  flags = getByte(db);
  masterData.bOutputLOK          = GETBIT(flags, 7);
  masterData.bOutputROK          = GETBIT(flags, 6);
  masterData.bOutputMbdOK        = GETBIT(flags, 5);
  masterData.bInterlock          = GETBIT(flags, 4);
  masterData.bDeratedPerformance = GETBIT(flags, 3);
  
  flags = getByte(db);
  masterData.BattStatusCode = (flags >> 4) & 0x0F; //4 bits
  masterData.bBattCharging  = GETBIT(flags, 3);
  masterData.bBattOk        = GETBIT(flags, 2);
  
  masterData.soc = getShort(db);
  
  //Master telemetry received this is the last frame in a communication sesion 
  //so now the line is free and a frame from the Host (CAN bus) can be forwarded
  lineIsFree(SOURCE_ADDRESS(tel->addressesTmp));
  mgmt.bCommRoundCompleted = TRUE;
  
  return Ack_NoError;
}


AckCode decodeSwVersion(DataBuffer* db,  struct TelRequest* tel)
{
  if (getRemainingLength(db) < SOFTWARE_VERSION_TEL_LENGTH)
    return Ack_WrongLength;
  
  uint32_t version;
  version = getShort(db);
  int address = SOURCE_ADDRESS(tel->addressesTmp);
  int b = ADDRESS_TO_BATT(address)-1; //gett battery nubmer from addresses (zero-based)
  
  if (address == MASTER_ADDRESS)
    masterData.version = version;
  else if (b >=0 && b < BATT_NUM)
  {
    masterData.battVersion[b] = version;
  }
  
  return Ack_NoError;
}


AckCode decodeSetConstCmd(DataBuffer* db , struct TelRequest* tel)
{
  if (getRemainingLength(db) < 4 ) {
    // not enough data in buffer for address and length
    return Ack_WrongLength;
  }
  
 if (getLong(db) != 0xDF4C72A0) {
    //wrong password
    return Ack_WrongParameter;
  }
  
  int dataLen = db->length - db->offset;
  if (dataLen < 4 || dataLen > BLOCK_SIZE-8) {
    //-8 is just in case
    return Ack_WrongLength;
  }
    
  FLASH_If_Erase(CONSTANTS_FIRST_ADDRESS ,CONSTANTS_END_ADDRESS);
  
  if (!writeBlock(CONSTANTS_FIRST_ADDRESS, dataLen, db)) {
    // write Fail
    return ACK_GenerlError;
  }
  
  return Ack_NoError;
}


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
  
  if (getRemainingLength(db) < dataLen)
    return Ack_WrongLength;
  
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
  
  if (getLong(db) != LOADER_PASSWORD) { //
    //wrong password
    return Ack_WrongParameter;
  }
  
  FLASH_If_Erase(USER_FLASH_FIRST_PAGE_ADDRESS ,USER_FLASH_END_ADDRESS);
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
  
  HAL_NVIC_SystemReset();
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
  fillShort(db, SOFTWARE_VERSION); //SW version
  fillShort(db, constData.hardwareVersion);
  fillShort(db, constData.serialNumber);
  fillShort(db, constData.mfgDate);
  fillString(db, SOFTWARE_VERSION_STR, 32);
  return Ack_NoError;
}


AckCode fillChargerStatusTel(DataBuffer* db, struct TelRequest* tel)
{
  uint16_t flags;
  
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
  
  flags = 
    (mgmt.sInputOk.state           ? BIT(7) : 0) |
    (masterData.bAllVersionsCollected ? BIT(6) : 0) ;
  fillByte(db, flags);
  
  return Ack_NoError; //11 bytes
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


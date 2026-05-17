
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
#include "IntUartDataLink.h" // for dbg - will be removed!!!
#include "ReportedData.h"
#include "Mgmt.h"
#include "flash_if.h"
#include <string.h>

//#include "I2C_Periphery.h"
//#include "PeripheryComm.h"
extern BatteryType_t battType;
extern UART_HandleTypeDef huart1;


//#include "DBG.h"



void setTel(struct TelRequest* tel, int telShort, int telLong, int delay);
AckCode decodeTelRequestCmd(DataBuffer* db,  struct TelRequest* tel);

//int setOutputParams(DataBuffer* db, int out, uint32_t bToSet);
AckCode decodeHpfTel(DataBuffer* db);
int decodeHpfStatus(DataBuffer* db, struct ReportedHpfData* hpfData);
AckCode decodeIsoTel(DataBuffer* db);
int decodeIsoStatus(DataBuffer* db, struct ReportedIsoData* isoData);
AckCode decodeBuckTel(DataBuffer* db);
AckCode fillChargerStatusTel(DataBuffer* db, struct TelRequest* tel);
AckCode fillSwVersion(DataBuffer* db, struct TelRequest* tel);
int decodeBuckStatus(DataBuffer* db, struct ReportedBuckData* buckData);
AckCode decodeStatusCmd(DataBuffer* db);
AckCode fillWriteAckTel(DataBuffer* db, struct TelRequest* tel);
AckCode fillReadMemoryTel(DataBuffer* db, struct TelRequest* tel);
AckCode fillChargerStatusTel(DataBuffer* db, struct TelRequest* tel);
AckCode fillSwVersion(DataBuffer* db, struct TelRequest* tel);


//------------------ Command Decoders ------------------
// since M5477 acts as master only this sectionis actually decodes received telemetries
struct CmdDecodeMap
{
  int opCode;
  AckCode (*decodeFunc)(DataBuffer* db,  struct TelRequest* tel);
};

static const struct CmdDecodeMap cmdDecodeMap[] =
{
 {OPCODE_TEL_REQUEST_CMD, decodeTelRequestCmd},
  {OPCODE_CHARGER_STATUS_TEL, decodeStatusCmd},
  {OPCODE_OUTPUT_CONTROL_TEL,decodeOutputControl},
  {OPCODE_WRITE_MEMORY, writeMemCmd}, //boot loader tel
  {OPCODE_CLEAR_MEMORY, clearMemCmd}, //boot loader tel
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


AckCode decodeOutputControl(DataBuffer* db)
{
uint8_t flags = getByte(db);

//rowOut.bOut_dCh_EN2 = GETBIT(flags, 7);
 //rowOut.bOut_dCh_EN1   =  GETBIT(flags, 6);
 rowOut.bOut_Heater_En  = GETBIT(flags, 5);
 //rowOut.bOut_Charge_Sw_En  =  GETBIT(flags, 4);
// rowOut.bOut_MCU_EN       = GETBIT(flags, 3);
 rowOut.bOut_OV_Test       =  GETBIT(flags, 2);
 rowOut.bOut_BP_RST         =   GETBIT(flags, 1);
 rowOut.bOut_DSBL_test       =   GETBIT(flags, 0);

}

AckCode decodeStatusCmd(DataBuffer* db)
{
    DataBuffer *db_tel;
   db_tel = intUart_getTxBuffer(0);
   if(db_tel == NULL)
     return Ack_UnkonwnError;

     intUart_quickSend(0);

}
AckCode fillControlStatusTel(DataBuffer* db  , struct TelRequest* tel)
{



    uint16_t flags;
  flags =

    (rowOut.bOut_dCh_EN2                  ? BIT(7) : 0) |
    (rowOut.bOut_dCh_EN1                  ? BIT(6) : 0) |
    (rowOut.bOut_Heater_En                ? BIT(5) : 0) |
    (rowOut.bOut_Charge_Sw_En             ? BIT(4) : 0) |
    (rowOut.bOut_MCU_EN                   ? BIT(3) : 0) |
    (rowOut.bOut_OV_Test                  ? BIT(2) : 0) |
    (rowOut.bOut_BP_RST                   ? BIT(1) : 0) |
    (rowOut.bOut_DSBL_test               ? BIT(0) : 0);

  fillByte(db,flags);


  flags =
    (rowIn.bIn_ID1                  ? BIT(7) : 0) |
    (rowIn.bIn_ID0                  ? BIT(6) : 0) |
    (rowIn.bIn_HeaterDisable                ? BIT(5) : 0) |
    (rowIn.bIn_dO_Dscharge             ? BIT(4) : 0) |
    (rowIn.bIn_dO_Charge                   ? BIT(3) : 0) |
    (inputs.sEPO.state                    ? BIT(2) : 0) |
   (rowIn.bIn_dBattleMode               ? BIT(1) : 0) |
  (inputs.sDSBL_FB.state                   ? BIT(0) : 0) ;

  fillByte(db,flags);


fillByte(db,battType);


  fillLongAsShort(db, rowAnIn.temp1);
  fillLongAsShort(db, rowAnIn.temp2);
  fillLongAsShort(db, rowAnIn.temp3);
  fillLongAsShort(db, rowAnIn.temp4);
  fillLongAsShort(db, rowAnIn.vBatt);
  fillLongAsShort(db, rowAnIn.charger);
  fillLongAsShort(db, rowAnIn.heater_N);
  fillLongAsShort(db, rowAnIn.Ich);
  //fillLongAsShort(db, rowAnIn.Spare_1);
  fillLongAsShort(db, rowAnIn.test_Ref);
  fillLongAsShort(db, mgmt.batt_SOC);

   flags =
    (mgmt.sFullBatt.state                 ? BIT(7) : 0) |
	    (mgmt.sDisbalance.state           ? BIT(6) : 0) ;
  fillByte(db,flags);



return Ack_NoError; //23 * 4  bytes

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
//  mgmt.bOutputOK_CMD         = GETBIT(flags, 7);
//  mgmt.bISOFault_CMD         = GETBIT(flags, 6);
//  mgmt.bFaultDetected_CMD    = GETBIT(flags, 5);
//  mgmt.bFaultDetectedLed_CMD = GETBIT(flags, 4);
//  mgmt.bSpareOut_CMD         = GETBIT(flags, 2);
  
  return Ack_NoError;
}



AckCode clearMemCmd(DataBuffer* db , struct TelRequest* tel)
{
if (getRemainingLength(db) < 4 ) {
    // not enough data in buffer for address and length
    return Ack_WrongLength;
  } //
 if (getLong(db) != LOADER_PASSWORD) {
    //wrong password
    return Ack_WrongParameter;
  }
  //uint32_t blockAddr = getLong(db); // start address

FLASH_If_Erase(USER_FLASH_FIRST_PAGE_ADDRESS ,USER_FLASH_END_ADDRESS);
  return Ack_NoError;

}

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

 // Prepare ACK tel
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
  {OPCODE_SOFTWARE_VERSION_TEL, fillSwVersion},
  {OPCODE_READ_MEMORY_TEL, fillReadMemoryTel}, // should opcode be OPCODE_READ_MEMORY_TEL ?
  {OPCODE_BATT_STATUS_TEL, fillControlStatusTel}



};
#define SIZEOF_TEL_DECODE_MAP (sizeof(telDecodeMap) / sizeof(struct TelDecodeMap))


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




AckCode fillSwVersion(DataBuffer* db, struct TelRequest* tel)
{
  
	  fillShort(db, SOFTWARE_VERSION); //SW version
	  fillShort(db, INTERFACE_VERISION); //Interface version
	  fillString(db, SOFTWARE_VERSION_STR, 32);
  return Ack_NoError;
}


AckCode fillChargerStatusTel(DataBuffer* db, struct TelRequest* tel)
{
//  uint16_t flags;
//
//  fillShort(db, SOFTWARE_VERSION);
//
//  flags =
//    (inputs.sEPO.state        ? BIT(7) : 0) |
//    (inputs.sEN_380VDC.state  ? BIT(6) : 0) |
//    (inputs.sBattleMode.state ? BIT(5) : 0) |
//    (inputs.sInSpare.state    ? BIT(4) : 0) ;
//  fillByte(db, flags);
//
//  flags =
//    (rowOut.bFault      ? BIT(7) : 0) |
//    (rowOut.bFault_LED  ? BIT(6) : 0) |
//    (rowOut.bISO_FLR    ? BIT(5) : 0) |
//    (rowOut.bOUT350V_OK ? BIT(4) : 0) |
//    (rowOut.bOutSpare   ? BIT(3) : 0) ;
//  fillByte(db, flags);
//
//  fillLongAsShort(db, anIn.PCBtemp);
//  fillLongAsShort(db, anIn.VCC_IO);
//  fillLongAsShort(db, anIn.In28V);
//  fillLongAsShort(db, anIn.TestRef);
//
  
  return Ack_NoError; //12 bytes
}

AckCode fillWriteAckTel(DataBuffer* db, struct TelRequest* tel)
{
  fillArray(db, tel->param, tel->paramLength);
  return Ack_NoError;
}

AckCode fillReadMemoryTel(DataBuffer* db , struct TelRequest* tel)
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


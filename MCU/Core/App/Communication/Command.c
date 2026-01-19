
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


//#define MASTER_COMMAND_KEY 0x500DB848
uint8_t lastTelBuffer[MODULES_NUM][LAST_TEL_LENGTH+20];

//void setTel(struct TelRequest* tel, int telShort, int telLong, int delay);
//int setOutputParams(DataBuffer* db, int out, uint32_t bToSet);
AckCode decodeHpfTel(DataBuffer* db);
int decodeHpfStatus(DataBuffer* db, struct ReportedHpfData* hpfData);
AckCode decodeIsoTel(DataBuffer* db);
int decodeIsoStatus(DataBuffer* db, struct ReportedIsoData* isoData);
AckCode decodeBuckTel(DataBuffer* db);
int decodeBuckStatus(DataBuffer* db, struct ReportedBuckData* buckData);
AckCode decodeStatusCmd(DataBuffer* db);
AckCode fillHpfCmd(DataBuffer* db);
AckCode fillIsoCmd(DataBuffer* db);
AckCode fillBuckCmd(DataBuffer* db);
AckCode fillBuckTelCmd(DataBuffer* db);



//------------------ Command Decoders ------------------
// since M5477 acts as master only this sectionis actually decodes received telemetries
struct CmdDecodeMap
{
  int opCode;
  AckCode (*decodeFunc)(DataBuffer* db);
};

static const struct CmdDecodeMap cmdDecodeMap[] =
{
  {OPCODE_TEL_STATUS_RESP, decodeStatusCmd},
  {OPCODE_ISO_STATUS_TEL, decodeIsoTel},
  {OPCODE_BUCK_STATUS_TEL, decodeBuckTel},
  {OPCODE_OUTPUT_CONTROL_TEL,decodeOutputControl},
  {OPCODE_WRITE_MEMORY, writeMemCmd}, //boot loader tel 
  {OPCODE_CLEAR_MEMORY, clearMemCmd} //boot loader tel 
};
#define SIZEOF_CMD_DECODE_MAP (sizeof(cmdDecodeMap) / sizeof(struct CmdDecodeMap))


AckCode decodeCommand(DataBuffer* db)
{
  int i;
  AckCode result;

  int opCode = getByte(db);
  for (i=0; i<SIZEOF_CMD_DECODE_MAP; i++)
  {
    if (opCode == cmdDecodeMap[i].opCode)
    {
       result = cmdDecodeMap[i].decodeFunc(db);
       if (result == Ack_NoError)
         mgmt.lastReceivedTel = opCode;
      return result;
    }
  }
  
  // OpCode not found in the cmdDecodeMap
  // not standart command decode may be added here
 
  return Ack_WrongOpCode;
}


static inline int currentCommModuleIndex()
{
  if (mgmt.CurrentCommModule == ModuleB)
    return 1;
  else
    return 0;
}

AckCode decodeHpfTel(DataBuffer* db)
{
  int reply;
  int moduleIndex = currentCommModuleIndex();
  
  //save the telemetry buffer for debug telemetry:
  
  if (getRemainingLength(db) >= (HPF_STATUS_TEL_LENGTH-1))
  {
    lastTelBuffer[moduleIndex][0] = OPCODE_TEL_STATUS_RESP;
    memcpy(lastTelBuffer[moduleIndex]+1, db->buffer + db->offset, HPF_STATUS_TEL_LENGTH-1);
  }
  
  reply = decodeHpfStatus(db, &hpfData[moduleIndex]);
  if (reply != 0)
    return Ack_WrongLength;
  
  SetDebounce(&mgmt.sHpfCommProblem[moduleIndex], FALSE);
  return Ack_NoError;
}

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
AckCode decodeStatusCmd(DataBuffer* db)
{
    DataBuffer *db_tel;
   db_tel = intUart_getTxBuffer(0);
   if(db_tel == NULL)
     return Ack_UnkonwnError;
   fillControlStatusTel(db_tel);
   
     intUart_quickSend(0);

}

AckCode decodeIsoTel(DataBuffer* db)
{
  int reply;
  int moduleIndex = currentCommModuleIndex();
  
  //save the telemetry buffer for debug telemetry:
  if (getRemainingLength(db) >= (ISO_STATUS_TEL_LENGTH-1))
  {
    lastTelBuffer[moduleIndex][HPF_STATUS_TEL_LENGTH] = OPCODE_ISO_STATUS_TEL;
    memcpy(lastTelBuffer[moduleIndex]+1+HPF_STATUS_TEL_LENGTH, db->buffer + db->offset, ISO_STATUS_TEL_LENGTH-1);
  }
  
  reply = decodeIsoStatus(db, &isoData[moduleIndex]);
  if (reply != 0)
    return Ack_WrongLength;
  
  SetDebounce(&mgmt.sIsoCommProblem[moduleIndex], FALSE);
  return Ack_NoError;
}

int decodeIsoStatus(DataBuffer* db, struct ReportedIsoData* isoData)
{
  int32_t flags;
  
  if (getRemainingLength(db) < (ISO_STATUS_TEL_LENGTH-1))
    return -1;
  
  
  isoData->version = getByte(db);
  
  flags = getByte(db);
  isoData->bUvccHigh  = GETBIT(flags, 2);
  isoData->bHPF_Ready = GETBIT(flags, 1);
  isoData->bOV        = GETBIT(flags, 0);
  
  isoData-> vIso = getShort(db) * 52500 / 1024;
  getShort(db); //isoData->Iprimary; //not decoded
  isoData->temp1 = tempConvert(getShort(db)*4);
  isoData->temp2 = tempConvert(getShort(db)*4);
  getShort(db); //isoData->Iprimary2; //not decoded
  
  flags = getByte(db);
  isoData->bLoadEn        = GETBIT(flags, 4);
  //ISO_EN
  isoData->bCurrentFault  = GETBIT(flags, 2);
  isoData->bOVLock        = GETBIT(flags, 1);
  isoData->bWakeUpTimeOut = GETBIT(flags, 0);
  
  isoData->ShdnReason = getByte(db);
  //total 14 bytes
  return 0;
}


AckCode decodeBuckTel(DataBuffer* db)
{
  int reply;
  int moduleIndex = currentCommModuleIndex();
  
  //save the telemetry buffer for debug telemetry:
  if (getRemainingLength(db) >= (BUCK_STATUS_TEL_LENGTH-1))
  {
    lastTelBuffer[moduleIndex][HPF_STATUS_TEL_LENGTH+ISO_STATUS_TEL_LENGTH] = OPCODE_BUCK_STATUS_TEL;
    memcpy(lastTelBuffer[moduleIndex]+1+HPF_STATUS_TEL_LENGTH+ISO_STATUS_TEL_LENGTH, db->buffer + db->offset, BUCK_STATUS_TEL_LENGTH-1);
  }
        
  reply = decodeBuckStatus(db, &buckData[moduleIndex]);
  if (reply != 0)
    return Ack_WrongLength;
  
  SetDebounce(&mgmt.sBuckCommProblem[moduleIndex], FALSE);
  return Ack_NoError;
}

int decodeBuckStatus(DataBuffer* db, struct ReportedBuckData* buckData)
{
  int32_t flags;
  
  if (getRemainingLength(db) < (BUCK_STATUS_TEL_LENGTH-1))
    return -1;
  
  
  buckData->version = getByte(db);
  
  flags = getByte(db);
  buckData->bUvccAux     = GETBIT(flags, 3);
  buckData->bRowIsoReady = GETBIT(flags, 2);
  //bComp1; //not decoded
  //bComp2 //not decoded
  
  flags = getByte(db);
  buckData->bIsoReady    = GETBIT(flags, 1);
  buckData->bEnableCmd   = GETBIT(flags, 0);
  
  
  buckData->voutCmd = getShort(db);
  buckData->ioutCmd = getShort(db);
  buckData->overVoltageCmd = getShort(db);
  buckData->overCurrentCmd = getShort(db);
  
  buckData->vin     = getShort(db) * 17080 / 1024;
  buckData->iout_wtOffset    = getShort(db) * (3472/4) / 1024; //with 4.17A offset
  buckData->iout = buckData->iout_wtOffset > IOUT_BASIC_OFFSET ? buckData->iout_wtOffset - IOUT_BASIC_OFFSET : 0;
  buckData->temp    = tempConvert(getShort(db)*4);
  buckData->ptcTemp = tempConvert(getShort(db)*4);
  buckData->vcc3_3  = getShort(db) * 500 / 1024;
  buckData->vout    = getShort(db) * (45773/4) / 1024; //(46231/4) / 1024;
  
  getShort(db); //Iout_DAC
  flags = getByte(db);
  //= GETBIT(flags, 7); //Comm WD
  // = GETBIT(flags, 6); //Current Fault
  buckData->bCCMode     = GETBIT(flags, 5);
  buckData->bOcLock     = GETBIT(flags, 4);
  buckData->bOvLock     = GETBIT(flags, 3);
  buckData->bBuckReady  = GETBIT(flags, 2);
  buckData->bADCFail    = GETBIT(flags, 1);
  buckData->bBuckEnable = GETBIT(flags, 0);
  buckData->ShdnReason = getByte(db);
  
  //total 27 bytes
  return 0;
}



//------------------ Telemetry Fillers ------------------
struct TelDecodeMap
{
  int opCode;
  AckCode (*fillFunc)(DataBuffer* db);
};

static const struct TelDecodeMap telDecodeMap[] = 
{
  {OPCODE_TEL_STATUS_REQUEST, fillHpfCmd},
  {OPCODE_ISO_TEL_REQUEST, fillIsoCmd},
  {OPCODE_BUCK_SET_STATE, fillBuckCmd},
  {OPCODE_BUCK_TEL_REQUEST, fillBuckTelCmd}
};
#define SIZEOF_TEL_DECODE_MAP (sizeof(telDecodeMap) / sizeof(struct TelDecodeMap))

AckCode fillTelemetry(DataBuffer* db, int opCode)//struct TelRequest* tel)
{
  int i;
  
  fillByte(db, opCode);
  
  for (i=0; i<SIZEOF_TEL_DECODE_MAP; i++)
  {
    if (opCode == telDecodeMap[i].opCode)
    {
      return telDecodeMap[i].fillFunc(db);
    }
  }
  
  // Tel OpCode not found in the telDecodeMap
  // not standart telemetry decode may be added here
  
  return Ack_WrongTelOpCode;
}



AckCode fillHpfCmd(DataBuffer* db) // status dbg message
{

//fillShort(db,rowOut.bOut_dCh_EN1);  
//fillShort(db,rowOut.bOut_dCh_EN2);  
//fillShort(db,rowOut.bOut_Heater_En);  
//fillShort(db,rowOut.bOut_Charge_Sw_En);  
//fillShort(db,rowOut.bOut_DSBL_test);  
//fillShort(db,rowOut.bOut_MCU_EN);  
//fillShort(db,rowOut.bOut_OV_Test); 
//fillShort(db,rowOut.bOut_BP_RST); 
//
//
//fillShort(db,rowIn.bIn_ID0);  
//fillShort(db,rowIn.bIn_ID1);  
//fillShort(db,battType);
//fillShort(db,rowIn.bIn_HeaterDisable);  
//fillShort(db,rowIn.bIn_dO_Dscharge);
//fillShort(db,rowIn.bIn_dO_Charge);  
//
//  fillShort(db, rowAnIn.temp1);
//  fillShort(db, rowAnIn.temp2);
//  fillShort(db, rowAnIn.temp3);
//  fillShort(db, rowAnIn.temp4);
//  fillShort(db, rowAnIn.vBatt);
//  fillShort(db, rowAnIn.charger);
//  fillShort(db, rowAnIn.heater_N);
//  fillShort(db, rowAnIn.Ich);
//  fillShort(db, rowAnIn.Spare_1);
//  fillShort(db, rowAnIn.test_Ref);
//
//    return Ack_NoError; //23 * 4  bytes

}


AckCode fillIsoCmd(DataBuffer* db)
{
//#ifndef M5480
  int SDReset;
  SDReset = !mgmt.bOutputEnable && (mgmt.SR_CommCounter <= 0);
  //there could be some very rear scenarion where HPF (and as the result the ISO as well) will turn OFF and then ON just before the BUCK output is turned ON
  // this scenarion will probably include some comunication problems and delays.
  //should it happened the SDReason should be reseted during opereation.
  int moduleIndex = currentCommModuleIndex();
  if (mgmt.bOutputEnable && isoData[moduleIndex].ShdnReason != 0)
    SDReset = TRUE;
  
  fillByte(db, SDReset ? 1:0); //ResetSDReason flag
//#endif
  return Ack_NoError;
}

AckCode fillBuckCmd(DataBuffer* db)
{
  int32_t correctionA, correctionB, vOut;
  
  //if (mgmt.CurrentCommModule == ModuleA)
    correctionA = mgmt.voltageCorrection/2 + mgmt.voltageTrimming;
  //else
    correctionB = -(mgmt.voltageCorrection - mgmt.voltageCorrection/2) + mgmt.voltageTrimming;
  
  //Vout = Vref*273.3 + Iout*0.072+2.36
  //the addition of 1.5v is to put the voltage slop in the middle of the setting
  vOut = mgmt.VoutCMD - VOLTAGE(2.36) + VOLTAGE(1.5) - mgmt.IoutCMD/2 * 74/1024;
  
  uint8_t flags;
  flags = (mgmt.bOutputEnable ? BIT(0) : 0);
  fillByte(db,flags);
#ifdef M5480
  fillLongAsShort(db, mgmt.VoutCMD*420/4096); //Vout A Command: V/100/399 * 4096 
  fillLongAsShort(db, mgmt.VoutCMD*420/4096); //Vout B Command
#else
  fillLongAsShort(db, vOut*246/4096 + correctionA); //Vout A Command: V/100/683 * 4096 
  fillLongAsShort(db, vOut*246/4096 + correctionB); //Vout B Command
#endif
  //Note: if VoutCMD resolution is changed then the MAX_VOLTAGE_CORRECTION constant should be changed as well
  fillLongAsShort(db, mgmt.IoutCMD*755/4096/2); //Iout Command: I /100/55.55 * 1024  //each BUCK is set half of the total current
  fillLongAsShort(db, (mgmt.IoutCMD/2 + IOUT_BASIC_OFFSET) *1209/4096); //Iout_meas Command: V/100/34.7 * 1024 //each BUCK is set half of the total current
  fillLongAsShort(db, mgmt.OverVoltageCMD*93/4096); //Over Voltage Command:  V /100/457.7 * 1024 * 1.01 (+1% correction)
  fillLongAsShort(db, (mgmt.OverCurrentCMD/2 + IOUT_BASIC_OFFSET) *1209/4096); //Over Current Command
  
  return Ack_NoError;
}

AckCode fillBuckTelCmd(DataBuffer* db)
{
  uint8_t flags;
  if (mgmt.CurrentCommModule == ModuleA)
    flags = 0;
  else
    flags = 1;
  
  fillByte(db,flags);
  
  return Ack_NoError;
}

AckCode decodeOutputControl(DataBuffer* db)
{
uint8_t flags = getByte(db);

rowOut.bOut_dCh_EN2 = GETBIT(flags, 7);
 rowOut.bOut_dCh_EN1   =  GETBIT(flags, 6);
 rowOut.bOut_Heater_En  = GETBIT(flags, 5);
 rowOut.bOut_Charge_Sw_En  =  GETBIT(flags, 4);
 rowOut.bOut_MCU_EN       = GETBIT(flags, 3);
 rowOut.bOut_OV_Test       =  GETBIT(flags, 2);
 rowOut.bOut_BP_RST         =   GETBIT(flags, 1);
 rowOut.bOut_DSBL_test       =   GETBIT(flags, 0);
 
 decodeStatusCmd(db);
}


AckCode fillControlStatusTel(DataBuffer* db)
{
  fillByte(db,0x81);
  
    uint16_t flags;
  flags = 
    (mgmt.sFullBatt.state                 ? BIT(8) : 0) |
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
    (mgmt.sFullBatt.state                 ? BIT(7) : 0);
  fillByte(db,flags);
fillByte(db,VERSION_MAJOR);
fillByte(db,VERSION_MINOR);


return Ack_NoError; //23 * 4  bytes
//  uint16_t flags;
//  flags = 
//    (rowOut.bInputRelayEN       ? BIT(7) : 0) |
//    (rowOut.bInterlock_OUT      ? BIT(6) : 0) |
//    (rowOut.bFAN_24V_EN         ? BIT(5) : 0) |
//    (rowOut.bHPF_PWM_EN         ? BIT(4) : 0) |
//    (inputs.sInterlock_In.state ? BIT(3) : 0) |
//    (rowIn.bFP_SW_SNS           ? BIT(2) : 0) ;
//      
//  fillByte(db, flags);
//  fillShort(db, mgmt.idCode);
//  fillLongAsShort(db, anIn.temp);
//  fillLongAsShort(db, anIn.vcc3_3);
//  fillLongAsShort(db, anIn.auxA);
//  fillLongAsShort(db, anIn.auxB);
//  fillLongAsShort(db, anIn.fan24v);
//  fillLongAsShort(db, anIn.FanTechometer);
//  
//  
//  fillLongAsShort(db, mgmt.hotSpot);
//  fillLongAsShort(db, rowOut.fanSpeed);
//  fillLongAsShort(db, isoData[0].temp1);
//  fillByte(db, (signed char)mgmt.voltageCorrection);
//  fillByte(db, (signed char)mgmt.voltageTrimming);
//  
//  
//  flags = 
//    ( mgmt.bIntCommProblem    ? BIT(15) : 0) |
//    ( mgmt.bInterlockFailure  ? BIT(14) : 0) |
//    (!mgmt.bModulesVersionOK  ? BIT(13) : 0) |
//    ( mgmt.bInternalFailure   ? BIT(12) : 0) |
//    ( mgmt.sReady.state       ? BIT(11) : 0) |
//    ( !bIAP_enabled           ? BIT(10) : 0) |
//    ( mgmt.sHpfEn.state       ? BIT(9) : 0) |
//    ( mgmt.bSelfTestEnded     ? BIT(8) : 0) |
//    ( !mgmt.bFaultDetected    ? BIT(7) : 0) |
//    ( !mgmt.bIPReset          ? BIT(6) : 0) |
//    ( mgmt.sInterlock.state   ? BIT(5) : 0) |
//    ( mgmt.bOutputReady       ? BIT(4) : 0) ;
//    
//  fillShort(db, flags);
  
  //tel length must be less then MAX_DBG_BUFFER_LENGTH = 50
}

AckCode clearMemCmd(DataBuffer* db)
{
if (getRemainingLength(db) < 8 ) {
    // not enough data in buffer for address and length
    return Ack_WrongLength;
  }

 if (getLong(db) != LOADER_PASSWORD) {
    //wrong password
    return Ack_WrongParameter;
  }
  uint32_t blockAddr = getLong(db); // start address 

FLASH_If_Erase(blockAddr,USER_FLASH_END_ADDRESS);
  return Ack_NoError;

}

AckCode writeMemCmd(DataBuffer* db)
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
     if (!writeBlock(baseAddr, dataLen, db)) 
    // write Fail
        return ACK_GenerlError;

    return Ack_NoError;
    
}


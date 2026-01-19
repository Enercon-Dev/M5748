#include "general.h"
#include "tcp_echoserver.h"
#include "scpi.h"
#include "lwip.h"
#include "lwip/tcp.h"
#include "netif.h"
#include "Mgmt.h"
#include "MacEeprom.h"
#include "PersistentData.h"
#include "Command.h"
#include <stdlib.h>
#include <errno.h>
#include <math.h>
#include <ctype.h> 
#include "base64.h"
#include "version.h"
#include "tftpserver.h"


//static char cmdBuffer[MAX_SCPI_CMD_LENGTH+10];
//static uint32_t cmdBufferOffset = 0;

#define MAX_REPLY_LENGTH 120
static char replyBuffer[MAX_REPLY_LENGTH+10];
static uint32_t replyDataSize = 0;
struct tcp_echoserver_struct *current_es;

#define QUEUE_SIZE 10
static ScpiErrorCode errorQueue[QUEUE_SIZE];
static uint32_t errorQueueIndex = 0;

#define MAX_DBG_BUFFER_LENGTH 50
static uint8_t dbgBuffer[MAX_DBG_BUFFER_LENGTH];

//Standard Event Status Register
#define ESR_QUERY_ERROR      BIT(2)
#define ESR_EXECUTION_ERROR  BIT(4)
#define ESR_COMMAND_ERROR    BIT(5)
#define ESR_POWER_ON         BIT(7)
static uint32_t esr_reg = ESR_POWER_ON;

//Status Byte Register
#define STB_PROTECTION_EVENT_FLAG BIT(1)
#define STB_ERROR_QUEUE           BIT(2)
#define STB_MASSAGE_AVAILABLE     BIT(4)
#define STB_ESB                   BIT(5)
static uint32_t stb_reg = 0;

//Protection Condition Register
#define PCR_OUTPUT_OVER_VOLTAGE    BIT(0)
#define PCR_OUTPUT_OVER_CURRENT    BIT(1)
#define PCR_OUTPUT_REGULATION_FAIL BIT(2)
#define PCR_OUTPUT_OVER_LOAD       BIT(3)
#define PCR_OVER_TEMP_SHDN         BIT(4)
#define PCR_FAN_FAIL               BIT(5)
#define PCR_OPEN_INTERLOCK         BIT(6)
#define PCR_AC_UNDER_VOLTAGE       BIT(7)
#define PCR_AC_MISSING_PHASE       BIT(8)
#define PCR_COMM_WATCHDOG          BIT(9)
#define PCR_INTERNAL_FAILURE       BIT(10)
#define PCR_MARGINAL_PROTECTION    BIT(11)
uint32_t pcr_reg = 0;
static uint32_t per_reg = 0; 

static void updateRegisters();
static void updatePCRRegister();
static void sendLine();
static int scpiSaveError(ScpiErrorCode error);
ScpiErrorCode ScpiExecute(char *str);
ScpiErrorCode execDBG(char *str);
ScpiErrorCode execFAN(char *str);
ScpiErrorCode execCLS(char *str);
ScpiErrorCode execESR(char *str);
ScpiErrorCode execIDN(char *str);
ScpiErrorCode execRST(char *str);
ScpiErrorCode execSTB(char *str);
ScpiErrorCode execMeasIout(char *str);
ScpiErrorCode execMeasVout(char *str);
ScpiErrorCode execMeasTemp(char *str);
ScpiErrorCode execVout(char *str);
ScpiErrorCode execOverVoltage(char *str);
ScpiErrorCode execIout(char *str);
ScpiErrorCode execOverCurrent(char *str);
ScpiErrorCode execErrorQueue(char *str);
ScpiErrorCode execCommApply(char *str);
ScpiErrorCode execGate(char *str);
ScpiErrorCode execIpAddr(char *str);
ScpiErrorCode execMask(char *str);
ScpiErrorCode execClearFaults(char *str);
ScpiErrorCode execOutEn(char *str);
ScpiErrorCode execSDReason(char *str);
ScpiErrorCode execSetSN(char *str);
ScpiErrorCode execRegMode(char *str);
ScpiErrorCode execOPMode(char *str);
ScpiErrorCode execMac(char *str);
ScpiErrorCode execPCR(char *str);
ScpiErrorCode execPER(char *str);
ScpiErrorCode execFirmEn(char *str);
ScpiErrorCode execFirmStat(char *str);
ScpiErrorCode execFirmApply(char *str);


static int startsWith(char **string, const char *restrict prefix);
static int parseFloat(char **str, float *val);
static int isEmpty(const char *str);
static char* ScpiErrorDescription(ScpiErrorCode error);
static char* IapErrorDescription(iap_status_t error);
static int printIP(char *str, int maxSize, uint32_t ip1, uint32_t ip2);

struct ScpiDecodeMap
{
  char* command;
  ScpiErrorCode (*execFunc)(char *str);
};

static const struct ScpiDecodeMap scpiDecodeMap[] =
{
  {"DBG", execDBG},
  //{"FAN", execFAN},
  {"*CLS",  execCLS},
  {"*ESR?", execESR},
  {"*IDN?", execIDN},
  {"*RST", execRST},
  {"*STB?", execSTB},
  {"MEAS:CURR?", execMeasIout},
  {"MEAS:VOLT?", execMeasVout},
  {"MEAS:TEMP?", execMeasTemp},
  {"OUTP:PROT:CLE", execClearFaults},
  {"OUTP:STAT", execOutEn},
  {"OUTP:SDRES?", execSDReason},
  {"PROD:SETSN_JS8M", execSetSN}, //Undocumented command
  {"SOUR:CCORCVMODE?", execRegMode},
  {"SOUR:OPMODE", execOPMode},
  {"SOUR:VOLT:PROT", execOverVoltage},
  {"SOUR:VOLT", execVout},
  {"SOUR:CURR:PROT", execOverCurrent},
  {"SOUR:CURR", execIout},
  {"SYST:ERR?", execErrorQueue},
  {"SYST:COMM:APPLY", execCommApply},
  {"SYST:COMM:GATE", execGate},
  {"SYST:COMM:ADDR", execIpAddr},
  {"SYST:COMM:MAC?", execMac},
  {"SYST:COMM:MASK", execMask},
  {"STAT:PROT:COND?", execPCR},
  {"STAT:PROT:EVEN?", execPER},
  {"SYST:FIRM:UPDATE:EN", execFirmEn},
  {"SYST:FIRM:UPDATE:STAT?", execFirmStat},
  {"SYST:FIRM:UPDATE:APPLY", execFirmApply}
  
};
#define SIZEOF_SCPI_DECODE_MAP (sizeof(scpiDecodeMap) / sizeof(struct ScpiDecodeMap))


static void updateRegisters()
{
  //ESR - Standard Event Status Register
  //the only two active bits are set during command parsing and execution
  
  //PCR - Protection Condition Register
  //PER - Protection Event Register
  //updated by updatePCRRegister() function
  
  //STB - Status Byte Register
  stb_reg = 
    ((per_reg != 0)        ? STB_PROTECTION_EVENT_FLAG : 0) |
    ((errorQueueIndex > 0) ? STB_ERROR_QUEUE           : 0) |
     //STB_MASSAGE_AVAILABLE 
    ((esr_reg != 0)        ? STB_ESB                   : 0);
}


static void updatePCRRegister()
{
  pcr_reg = 0;
  
  if (!mgmt.bSelfTestEnded)
  {
    //do not report errors during self test as the unit is not fully initialized 
    per_reg = 0;
    return;
  }
  
  if (mgmt.ovLock != 0)
    pcr_reg |= PCR_OUTPUT_OVER_VOLTAGE;

  if (mgmt.ocLock != 0)
    pcr_reg |= PCR_OUTPUT_OVER_CURRENT;

  if (mgmt.sHighVoltage.state || mgmt.sHighCurrent.state || mgmt.sLowPower.state)
    pcr_reg |= PCR_OUTPUT_REGULATION_FAIL;
  
  if (mgmt.sOverLoad.state)
    pcr_reg |= PCR_OUTPUT_OVER_LOAD;
  
  if (mgmt.sOverTemp.state)
    pcr_reg |= PCR_OVER_TEMP_SHDN;
  
  if (mgmt.sFanFail.state)
    pcr_reg |= PCR_FAN_FAIL;
  
  if(!mgmt.sInterlock.state && mgmt.bInterlockReady)
    pcr_reg |= PCR_OPEN_INTERLOCK;
  
  if (!hpfData[0].bInOk || !hpfData[1].bInOk)
    pcr_reg |= PCR_AC_UNDER_VOLTAGE;
  
  if (hpfData[0].bPhaseLoss || hpfData[1].bPhaseLoss)
    pcr_reg |= PCR_AC_MISSING_PHASE;
  
  if (mgmt.sUserCommWD.state)
    pcr_reg |= PCR_COMM_WATCHDOG;
  
  if (mgmt.bInternalFailure)
    pcr_reg |= PCR_INTERNAL_FAILURE;
  
  if (mgmt.OverVoltageCMD < mgmt.VoutCMD*110/100 ||  //OV < Vout* 110%
      mgmt.OverCurrentCMD < mgmt.IoutCMD*115/100)    //OC < Iout* 115%
    pcr_reg |= PCR_MARGINAL_PROTECTION;
    
  
  
  //PER - Protection Event Register
  per_reg |= pcr_reg;
}
  

static void sendLine()
{
  //size sanity check
  if (replyDataSize > MAX_REPLY_LENGTH)
    replyDataSize = MAX_REPLY_LENGTH;
  
  if (replyDataSize == 0)
    return;
  
  //append new line if needed
  if (replyBuffer[replyDataSize-1] != '\n')
  {
    if(replyDataSize > MAX_REPLY_LENGTH - 2)
      replyDataSize = MAX_REPLY_LENGTH - 2;
    replyBuffer[replyDataSize++] = '\r';
    replyBuffer[replyDataSize++] = '\n';
  }
  
  
  err_t wr_err;
  wr_err = tcp_write(current_es->pcb, replyBuffer, replyDataSize, 1);
  if (wr_err == ERR_OK) //sent sucssesfully
  {
    tcp_output(current_es->pcb);
    replyDataSize = 0;
  }
  else
  {
    //discard the sent data.
    replyDataSize = 0;
    scpiSaveError(Scpi_QueryError); //indicate that the output data was lost
  }
}


// saves the error in errorQueue buffer, sets the approperate bit in ECR
// returns true if the error was caused by a legal command
static int scpiSaveError(ScpiErrorCode error)
{
  if (error == Scpi_NoError)
    return TRUE; //legal comand
  
  //save the error in the error queue:
  if (errorQueueIndex >= QUEUE_SIZE-1)
  {
    errorQueueIndex = QUEUE_SIZE;
    errorQueue[QUEUE_SIZE-1] = Scpi_QueueOverflow;
  }
  else
  {
    errorQueue[errorQueueIndex++] = error;
  }
    
  //record the error in ESR register:
  if ((error >= -299) && (error <=-200)) //Execution Error per SCPI spec par 21.8.10
  {
    esr_reg |= ESR_EXECUTION_ERROR;
    
    return TRUE; //legal comand
  }
  else if ((error >= -499) && (error <=-400)) //Query Error per SCPI spec par 21.8.12
  {
    esr_reg |= ESR_QUERY_ERROR;
    return TRUE; //legal comand
  }
  else
  {
    esr_reg |= ESR_COMMAND_ERROR;
  }
  return FALSE;
}


//gets command from TCP port and executes them
void scpiHandle(struct tcp_echoserver_struct *es)
{
  char* str;
  ScpiErrorCode error;
  
  current_es = es; //it is not nice to use global var but it is much simpler then passing it all the way to sendLine() function
  
  updatePCRRegister();
  updateRegisters();
  
  str = es_getLine(es);
  if (str == NULL)
    return;
  
  error = ScpiExecute(str);
  
  //save the error in the error queue:
  if (scpiSaveError(error))
  {
    //Command executed succesfully
    mgmt.bScpiCommandReceived = TRUE; //used for LED blinking
    SetDebounce(&mgmt.sUserCommWD, FALSE); // reset the comm WD
  }
}


ScpiErrorCode ScpiExecute(char *str)
{
  
  //trim leading spaces if any
  while (*str == ' ')
    str++;
  
  
  
  int i;
  ScpiErrorCode result;

  for (i=0; i<SIZEOF_SCPI_DECODE_MAP; i++)
  {
    if (startsWith(&str, scpiDecodeMap[i].command))
    {
      result = scpiDecodeMap[i].execFunc(str);
      return result;
    }
  }
  
  return Scpi_CmdHeaderError; //unknown command
}


//DBG Command
ScpiErrorCode execDBG(char *str)
{
  if (str[0] == 0 || str[1] != '?')
    return Scpi_CmdHeaderError;
  int module;
  DataBuffer db;
  
  db.buffer = dbgBuffer;
  db.length = MAX_DBG_BUFFER_LENGTH;
  db.offset = 0;
  
  switch (str[0])
  {
  case '1':
  case '2':
    replyBuffer[0] = 'D';
    replyBuffer[1] = 'B';
    replyBuffer[2] = 'G';
    replyBuffer[3] = str[0];
    
    module = (str[0] == '1') ? 0:1;
    
    Base64_encode((char *)lastTelBuffer[module], LAST_TEL_LENGTH,  replyBuffer+4, MAX_REPLY_LENGTH-4);
    replyDataSize = 4 * ((LAST_TEL_LENGTH + 2) / 3) + 4;
    sendLine();
    return Scpi_NoError;
    
  case '3':
    replyBuffer[0] = 'D';
    replyBuffer[1] = 'B';
    replyBuffer[2] = 'G';
    replyBuffer[3] = '3';
    fillControlStatusTel(&db);
    Base64_encode((char *)db.buffer, db.offset, replyBuffer+4, MAX_REPLY_LENGTH-4);
    replyDataSize = 4 * ((db.offset + 2) / 3) + 4;
    sendLine();
    return Scpi_NoError;
  }
  
  return Scpi_CmdHeaderError; 
}

ScpiErrorCode execFAN(char *str)
{
  float param;
  
  if (str[0] == '?')
  {
    str++;
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra character in the command
    
    param = (float)mgmt.FanCMD;
    
    replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%.1f", param);
    sendLine();
    return Scpi_NoError;
  }
  else if (str[0] == ' ')
  {
    str++;
    if (!parseFloat(&str, &param))
      return Scpi_SyntaxError; // number convertion error
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra character in the command
    
    int32_t val;
    val = (int)(param);
    //val = val * VOLTAGE(1)/VOLTAGE(0.1); //convert to correct internal resolution
    
    if ((val > 100))
      return Scpi_DataOutOfRange; //value out of range
    
    //command is correct - save the new Vout value
    mgmt.FanCMD = val; 
    return Scpi_NoError;
  }
  else
  {
    return Scpi_CmdHeaderError;
  }
  
}

//*CLS Command
ScpiErrorCode execCLS(char *str)
{
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
    
  esr_reg = 0; //clear the ESR - Standard Event Status Register
  errorQueueIndex = 0; //clear the Error Queue
  per_reg = 0; //clear the PER - Protection Event Register
  return Scpi_NoError;  
}

//*ESR? Command
ScpiErrorCode execESR(char *str)
{
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
    
  replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%d", esr_reg & 0xFF);
  sendLine();
  
  esr_reg = 0; //this command clears the register
  updateRegisters();
  
  return Scpi_NoError; 
}


//*IDN? Command
ScpiErrorCode execIDN(char *str)
{
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command

#ifdef M5480
  replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "Milpower Source 5YWX2, M5480-1, %03d, %s", persData.serialNumber, VERSION_STR);
#else
  replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "Milpower Source 5YWX2, M5477-1, %03d, %s", persData.serialNumber, VERSION_STR);
#endif
  sendLine();
  return Scpi_NoError;
}


//*RST Command
ScpiErrorCode execRST(char *str)
{
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
    
  //TODO: same params as in mgmtInit - best to call an int from here
  mgmt.VoutCMD = DEFAULT_VOUT_CMD;
  mgmt.IoutCMD = DEFAULT_IOUT_CMD;
  mgmt.OverVoltageCMD = DEFAULT_OV_CMD;
  mgmt.bOutputEnableCMD = FALSE;
  return Scpi_NoError;
}


//*STB? Command
ScpiErrorCode execSTB(char *str)
{
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
  
  replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%d", stb_reg & 0xFF);
  sendLine();
  return Scpi_NoError;
}


//MEAS:CURR? Command
ScpiErrorCode execMeasIout(char *str)
{
  float param;
  
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
  
  param = (float)mgmt.iOut / CURRENT(1);
  replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%.1f", param);
  sendLine();
  return Scpi_NoError;
}

//MEAS:VOLT? Command
ScpiErrorCode execMeasVout(char *str)
{
  float param;
  
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
  
  param = (float)(mgmt.vOutDisplay) / VOLTAGE(1);
  replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%.1f", param);
  sendLine();
  return Scpi_NoError;
}


//MEAS:TEMP? Command
ScpiErrorCode execMeasTemp(char *str)
{
  float param;
  
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
  
  param = ((float)(mgmt.hotSpot) - TEMPERATURE(0)) / (TEMPERATURE(1) - TEMPERATURE(0));
  replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%.1f", param);
  sendLine();
  return Scpi_NoError;
}


//SOUR:VOLT command
ScpiErrorCode execVout(char *str)
{
  float param;
  
  if (str[0] == '?')
  {
    str++;
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra character in the command
    
    param = (float)mgmt.VoutCMD / VOLTAGE(1);
    
    replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%.1f", param);
    sendLine();
    return Scpi_NoError;
  }
  else if (str[0] == ' ')
  {
    str++;
    if (!parseFloat(&str, &param))
      return Scpi_SyntaxError; // number convertion error
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra character in the command
    
    uint32_t val;
    val = (int)(param * 10 + 0.5); //0.1V resolution + rounding
    val = val * VOLTAGE(1)/VOLTAGE(0.1); //convert to correct internal resolution
    
    if ((val > MAX_VOUT_CMD) || (val < MIN_VOUT_CMD))
      return Scpi_DataOutOfRange; //value out of range
    
    if (val > mgmt.OverVoltageCMD)
      return Scpi_SettingsConflict;
    
    //command is correct - save the new Vout value
    mgmt.VoutCMD = val; 
    return Scpi_NoError;
  }
  else
  {
    return Scpi_CmdHeaderError;
  }
}


//SOUR:VOLT:PROT Command
ScpiErrorCode execOverVoltage(char *str)
{
  uint32_t param_i;
  float param_f;
  
  if (str[0] == '?')
  {
    str++;
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra character in the command
    
    param_i = mgmt.OverVoltageCMD / VOLTAGE(1);
    
    replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%d", param_i);
    sendLine();
    return Scpi_NoError;
  }
  else if (str[0] == ' ')
  {
    str++;
    if (!parseFloat(&str, &param_f))
      return Scpi_SyntaxError; // number convertion error
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra character in the command
    
    uint32_t val;
    val = (int)(param_f * 1 + 0.5); //1V resolution + rounding
    val = val * VOLTAGE(1); //convert to correct internal resolution
    
    if ((val > MAX_OV_CMD) || (val < MIN_OV_CMD))
      return Scpi_DataOutOfRange; //value out of range
    
    if (val < mgmt.VoutCMD)
      return Scpi_SettingsConflict;
    
    mgmt.OverVoltageCMD = val; //command is correct - save the new value
    return Scpi_NoError;
  }
  else
  {
    return Scpi_CmdHeaderError;
  }
}


//SOUR:CURR command
ScpiErrorCode execIout(char *str)
{
  float param;
  
  if (str[0] == '?')
  {
    str++;
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra character in the command
    
    param = (float)mgmt.IoutCMD / CURRENT(1);
    
    replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%.1f", param);
    sendLine();
    return Scpi_NoError;
  }
  else if (str[0] == ' ')
  {
    str++;
    if (!parseFloat(&str, &param))
      return Scpi_SyntaxError; // number convertion error
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra character in the command
    
    uint32_t val;
    val = (int)(param * 10 + 0.5); //0.1A resolution + rounding
    val = val * CURRENT(1)/CURRENT(0.1); //convert to correct internal resolution
    
    if ((val > MAX_IOUT_CMD) || (val < MIN_IOUT_CMD))
      return Scpi_DataOutOfRange; //value out of range
    
    if (val > mgmt.OverCurrentCMD)
      return Scpi_SettingsConflict;
    
    mgmt.IoutCMD = val;
    return Scpi_NoError;
  }
  else
  {
    return Scpi_CmdHeaderError;
  }
}

    
//SOUR:CURR:PROT command
ScpiErrorCode execOverCurrent(char *str)
{
  uint32_t param_i;
  float param_f;
  
  if (str[0] == '?')
  {
    str++;
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra character in the command
    
    param_i = mgmt.OverCurrentCMD / CURRENT(1);
    
    replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%d", param_i);
    sendLine();
    return Scpi_NoError;
  }
  else if (str[0] == ' ')
  {
    str++;
    if (!parseFloat(&str, &param_f))
      return Scpi_SyntaxError; // number convertion error
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra character in the command
    
    uint32_t val;
    val = (int)(param_f * 10 + 0.5); //0.1A resolution + rounding
    val = val * CURRENT(1)/CURRENT(0.1); //convert to correct internal resolution
    
    if ((val > MAX_OC_CMD) || (val < MIN_OC_CMD))
      return Scpi_DataOutOfRange; //value out of range
    
    if (val < mgmt.IoutCMD)
      return Scpi_SettingsConflict;
    
    mgmt.OverCurrentCMD = val; //command is correct - save the new value
    return Scpi_NoError;
  }
  else
  {
    return Scpi_CmdHeaderError;
  }
}


//SYST:ERR? Command
ScpiErrorCode execErrorQueue(char *str)
{
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
  
  if (errorQueueIndex > QUEUE_SIZE)
    errorQueueIndex = QUEUE_SIZE; //just in case

  int errorCode;  
  if (errorQueueIndex > 0)
  {
    errorCode = errorQueue[0];
    //remove the firs element from the errorQueue FIFO;
    errorQueueIndex--;
    int i;
    for (i = 0; i < errorQueueIndex; i++)
      errorQueue[i] = errorQueue[i+1];
  
    updateRegisters();
  }
  else
  {
    //error queue is empty
    errorCode = Scpi_NoError;
  }

  replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%d, %s", 
                           errorCode, 
                           ScpiErrorDescription((ScpiErrorCode)errorCode));
  sendLine();
  return Scpi_NoError;
}


//SYSY:COMM:APPLY Command
ScpiErrorCode execCommApply(char *str)
{
  if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra characters in the command
  
  if (!OutOffCertain())
    return Scpi_SettingsConflict;
  
  if (savePersistentData() < 0) //write the Persistent Data to EEPROM (blocking)
  {
    //write failed
    return Scpi_HardwareError;
  }
  
  //TODO: if posible close the TCP port before reset
  
  HAL_NVIC_SystemReset(); //reset the MCU for the IP parameter changes to take affect
  return Scpi_NoError; //the return statement will never be executed but the compiler do not know that
}


//SYST:COMM:GATE Command
extern struct netif gnetif;
ScpiErrorCode execGate(char *str)
{
  uint32_t currentGate, savedGate;
  
  if (str[0] == '?')
  {
    str++;
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra characters in the command
    
    //Query command:
    currentGate = ntohl(netif_ip4_gw(&gnetif)->addr);
    savedGate = persData.defaultGateway;
    replyDataSize = printIP (replyBuffer, MAX_REPLY_LENGTH, currentGate, savedGate);
    
    sendLine();
    return Scpi_NoError;
  }
  else if (str[0] == ' ')
  {
    str++;
    ip4_addr_t ip;
    if (!ipaddr_aton(str, &ip))
      return Scpi_SyntaxError;
    
    //there is no easy way to check if there anoter extra parameter after the IP address so it will be ignored 
    
    persData.defaultGateway = ntohl(ip.addr);
    return Scpi_NoError;
  }
  else
  {
    return Scpi_CmdHeaderError;
  }
}


//SYST:COMM:ADDR command
ScpiErrorCode execIpAddr(char *str)
{
  uint32_t currentIp, savedIp;
  //int i,j;
  //char *reply;
  
  if (str[0] == '?')
  {
    str++;
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra characters in the command
    
    //Query command:
    currentIp = ntohl(netif_ip4_addr(&gnetif)->addr);
    savedIp = persData.ipAddress;
    replyDataSize = printIP (replyBuffer, MAX_REPLY_LENGTH, currentIp, savedIp);
    
    /*  
    reply = replyBuffer;
    //return the current IP address
    //ip4addr_ntoa_r(&currentIp, reply, MAX_REPLY_LENGTH-4);
    if (currentIp != savedIp)
    {
      //find the end of ip address string
      for (i=0; i<MAX_REPLY_LENGTH-4; i++)
      {
        if (*reply == 0)
          break;
        reply++;            
      }
      //return the "new" IP address
      *reply++ = ' ';
      *reply++ = '(';
      //ip4addr_ntoa_r(&savedIp, reply, MAX_REPLY_LENGTH-4-i);
      for (j=0; j<MAX_REPLY_LENGTH-4-i; j++)
      {
        if (*reply == 0)
          break;
        reply++;            
      }
      *reply++ = ')';
      *reply++ = 0;
    }
      */
    sendLine();
    return Scpi_NoError;
  }
  else if (str[0] == ' ')
  {
    str++;
    ip4_addr_t ip;
    if (!ipaddr_aton(str, &ip))
      return Scpi_SyntaxError;
    
    //there is no easy way to check if there anoter extra parameter after the IP address so it will be ignored 
    
    persData.ipAddress = ntohl(ip.addr);
    return Scpi_NoError;
  }
  else
  {
    return Scpi_CmdHeaderError;
  }  
}


//SYST:COMM:MASK Command
ScpiErrorCode execMask(char *str)
{
  uint32_t currentMask, savedMask;
  
  if (str[0] == '?')
  {
    str++;
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra characters in the command
    
    //Query command:
    currentMask = ntohl(netif_ip4_netmask(&gnetif)->addr);
    savedMask = persData.subNetMask;
    replyDataSize = printIP (replyBuffer, MAX_REPLY_LENGTH, currentMask, savedMask);
    
    sendLine();
    return Scpi_NoError;
  }
  else if (str[0] == ' ')
  {
    str++;
    ip4_addr_t ip;
    if (!ipaddr_aton(str, &ip))
      return Scpi_SyntaxError;
    
    //there is no easy way to check if there anoter extra parameter after the IP address so it will be ignored 
    
    persData.subNetMask = ntohl(ip.addr);
    return Scpi_NoError;
  }
  else
  {
    return Scpi_CmdHeaderError;
  }
}


//OUTP:PROT:CLE Command
ScpiErrorCode execClearFaults(char *str)
{
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
  
  ClearFaults();
  pcr_reg = 0;
  updatePCRRegister();
  
  return Scpi_NoError;
}


//OUTP:STAT command
ScpiErrorCode execOutEn(char *str)
{
  int bEnable;
  if (str[0] == '?')
  {
    str++;
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra characters in the command
    
    //Query command:
    replyBuffer[0] = mgmt.bOutputEnableCMD ? '1' : '0';
    replyDataSize = 1;
    sendLine();
    return Scpi_NoError;  
  }
  else if (str[0] == ' ')
  {
    str++;
    while (isspace(*str))
      str++; //remove white spacess before the parametter (if any)
    
    //decod the parametter
    if (str[0] == '1')
    {
      str++;
      bEnable = TRUE;
    }
    else if (str[0] == '0')
    {
      str++;
      bEnable = FALSE;
    }
    else if (startsWith(&str, "ON"))
    {
      bEnable = TRUE;
    }
    else if (startsWith(&str, "OFF"))
    {
      bEnable = FALSE;
    }
    else
    {
      return Scpi_SyntaxError; //wrong parameter
    }
    
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra character in the command
    
    
    if (bEnable && !mgmt.bOutputReady) //can not turn the output ON
      return Scpi_SettingsConflict; 
    
    //command correct - save the new command value
    mgmt.bOutputEnableCMD = bEnable;
    
    if (bEnable)
      clearSDReason(); //clear previous shutdown reason when turnin on the output
    
    return Scpi_NoError;
  }
  else
  {
    return Scpi_CmdHeaderError;
  }
}


//OUTP:SDRES command
ScpiErrorCode execSDReason(char *str)
{
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
  
  int i;
  replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%02X %04X ", 
                           (mgmt.SR_Normal) & 0xFF,
                           (mgmt.SR_Failure) & 0xFFFF);
  for (i=0; i<MODULES_NUM; i++)
    {
      replyDataSize += snprintf(replyBuffer+replyDataSize, MAX_REPLY_LENGTH-replyDataSize, "%04X %04X %04X ", 
                             mgmt.SR_HPF[i] & 0xFFFF,
                             mgmt.SR_ISO[i] & 0xFFFF,
                             mgmt.SR_Buck[i] & 0xFFFF);
    }
  
  sendLine();
  
  return Scpi_NoError;
}


//PROD:SETSN_JS8M Command
ScpiErrorCode execSetSN(char *str)
{
  if (str[0] != ' ')
    return Scpi_CmdHeaderError;
  str++;
  char *endStr;
  
  long sn = strtol(str, &endStr, 10);
  if ((sn==0) || (sn < 0) || (sn > 9999))
    return Scpi_SyntaxError; //wrong parameter
  
  if (!OutOffCertain())
    return Scpi_SettingsConflict;
    
  writeSerialNumber(sn);
  //the SN is written to eeprom but the persData.serialNumber is not updated
  // the unit should be restarted for the new SN to take effect
  return Scpi_NoError;
}


//SOUR:CCORCVMODE? Command
ScpiErrorCode execRegMode(char *str)
{
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
  
  replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, mgmt.sCCMode.state ? "CC" : "CV");
  sendLine();
  return Scpi_NoError;
}


//SOUR:OPMODE Command
ScpiErrorCode execOPMode(char *str)
{
  int bPSMode;
  if (str[0] == '?')
  {
    str++;
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra characters in the command
    
    //Query command:
    replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, mgmt.bPSMode ? "PS" : "CH");
    sendLine();
    return Scpi_NoError;  
  }
  else if (str[0] == ' ')
  {
    str++;
    while (isspace(*str))
      str++; //remove white spacess before the parametter (if any)
    
    //decod the parametter
    if (startsWith(&str, "PS"))
    {
      bPSMode = TRUE;
    }
    else if (startsWith(&str, "CH"))
    {
      bPSMode = FALSE;
    }
    else
    {
      return Scpi_SyntaxError; //wrong parameter
    }
    
    //command correct - save the new command value 
    if (mgmt.bOutputEnableCMD && (bPSMode != mgmt.bPSMode)) //can not change Operation Mode while the output is ON
      return Scpi_SettingsConflict;
    
    mgmt.bPSMode = bPSMode;
    
    return Scpi_NoError;
  }
  else
  {
    return Scpi_CmdHeaderError;
  }
}


//SYST:COMM:MAC? Command
ScpiErrorCode execMac(char *str)
{
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
  
  /*
  replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%02X-%02X-%02X-%02X-%02X-%02X", 
                           heth.Init.MACAddr[0],heth.Init.MACAddr[1],heth.Init.MACAddr[2],
                           heth.Init.MACAddr[3],heth.Init.MACAddr[4],heth.Init.MACAddr[5]);
*/
  replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%02X-%02X-%02X-%02X-%02X-%02X",
                           gnetif.hwaddr[0], gnetif.hwaddr[1], gnetif.hwaddr[2],
                           gnetif.hwaddr[3], gnetif.hwaddr[4], gnetif.hwaddr[5]);
  sendLine();
  return Scpi_NoError;
}


//STAT:PROT:COND? Command
ScpiErrorCode execPCR(char *str)
{
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
    
  replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%d", pcr_reg & 0xFFFF);
  sendLine();
  return Scpi_NoError; 
}


//STAT:PROT:EVEN? Command
ScpiErrorCode execPER(char *str)
{
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
    
  replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%d", per_reg & 0xFFFF);
  sendLine();
  per_reg = 0; //this command clears the register
  updateRegisters();
  return Scpi_NoError; 
}


//SYST:FIRM:UPDATE:EN Command
ScpiErrorCode execFirmEn(char *str)
{
  if (str[0] == '?')
  {
    str++;
    if (!isEmpty(str))
      return Scpi_SyntaxError; //there are some extra characters in the command
    
    //Query command:
    replyBuffer[0] = bIAP_enabled ? '1' : '0';
    replyDataSize = 1;
    sendLine();
    return Scpi_NoError;  
  }
  else if (str[0] == ' ')
  {
    str++;
    while (isspace(*str))
      str++; //remove white spacess before the parametter (if any)
    
    //decod the parametter
    if (startsWith(&str, "1"))
    {
      if (!OutOffCertain())
        return Scpi_SettingsConflict;

    }
    else //note: there is no "0" parameter, perform reset disable the IAP
    {
      return Scpi_SyntaxError; //wrong parameter
    }
    
    //command correct - start the TFTP server
    if (!bIAP_enabled)
    {
      if (IAP_tftpd_init() < 0)
        return Scpi_ExecutionError;
    }
   
    return Scpi_NoError;
  }
  else
  {
    return Scpi_CmdHeaderError;
  }
}


//SYST:FIRM:UPDATE:STAT? Command
ScpiErrorCode execFirmStat(char *str)
{
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
    
  replyDataSize = snprintf(replyBuffer, MAX_REPLY_LENGTH, "%s", 
                           IapErrorDescription(IAP_status));
  sendLine();
  return Scpi_NoError;
}


//SYST:FIRM:UPDATE:APPLY
ScpiErrorCode execFirmApply(char *str)
{
  if (!isEmpty(str))
    return Scpi_SyntaxError; //there are some extra characters in the command
  
  if (!bIAP_enabled)
    return Scpi_SettingsConflict;
  
  //TODO: if posible close the TCP port before reset
  
  HAL_NVIC_SystemReset(); //reset the MCU to switch to the new firmware
  return Scpi_NoError; //the return statement will never be executed but the compiler do not know that
}



///////////////////////////////////////////////////////////
//////  Helper functions                             //////
///////////////////////////////////////////////////////////


//prefix should be in Upper case
//when true is returned then the string pointer is increment no the next char after the prefix
static int startsWith(char **string, const char *restrict prefix)
{
  char *str = *string;
  while(*prefix)
  {
    if(*prefix++ != toupper(*str++))
      return 0;
  }
  
  *string = str;
  return 1;
}


static int parseFloat(char **str, float *val)
{
    char *temp;
    int rc = 1;
    
    //errno = 0;
    //for some reason errno can not be used - it generates a linker error.
    //but is seems that it is not critical as underflow and overflow errors will be detected later at parameters boundry check
    
    *val = strtof(*str, &temp);

    if (temp == *str || *temp != '\0' ||
        /*errno != 0 ||*/ 
        isnan(*val) || isinf(*val))
    {
        rc = 0;
    }
    else
    {
      *str=temp;
    }
    //errno != 0 may be a to simple check as FLT_MIN will be cosidered an error while it actually a valid result for most cases.

    return rc;
}


static int isEmpty(const char *str)
{
  while (*str != '\0')
  {
    if (!isspace(*str))
      return 0;
    str++;
  }

  return 1;  
}


static char* ScpiErrorDescription(ScpiErrorCode error)
{
  switch (error)
  {
  case Scpi_NoError: //0
    return "No Error";
  case Scpi_CmdHeaderError: //-100
    return "Command error";
  case Scpi_SyntaxError: //-102
    return "Syntax error";
  case Scpi_HeaderError: //-110
    return "Command header error";
  case Scpi_ExecutionError: //-200
    return "Execution Error";
  case Scpi_SettingsConflict: //-221
    return "Settings Conflict";
  case Scpi_DataOutOfRange: //-222
    return "Data out of range";
  case Scpi_HardwareError: //-240
    return "Hardware Error";
  case Scpi_QueueOverflow: //-350
    return "Queue overflow";
  case Scpi_QueryError: //-400
    return "Query Error";
  default :
    return "";
  }  
}


static char* IapErrorDescription(iap_status_t error)
{
  switch (error)
  {
  case IAP_DISABLED:
    return "Disabled";
  case IAP_IDLE:
    return "Idle";
  case IAP_RECEIVING:
    return "Receiving";
  case IAP_RECEIVED_OK:
    return "Received OK";
  case IAP_RECEIVED_CRC_ERROR:
    return "CRC Error";
  case IAP_RECEIVED_LENGTH_ERROR:
    return "Length Error";
  case IAP_ERROR:
    return "Error";
  default :
    return "";
  }  
}


static int printIP(char *str, int maxSize, uint32_t ip1, uint32_t ip2)
{
  int stringSize;
    stringSize = snprintf(str, maxSize, "%d.%d.%d.%d", 
                             (ip1>>24)&0xFF,
                             (ip1>>16)&0xFF,
                             (ip1>>8) &0xFF,
                             (ip1)    &0xFF);
    if (ip1 != ip2)
    {
      stringSize += snprintf(str+stringSize, maxSize-stringSize, " (%d.%d.%d.%d)", 
                                (ip2>>24)&0xFF,
                                (ip2>>16)&0xFF,
                                (ip2>>8) &0xFF,
                                (ip2)    &0xFF);
    }
    return stringSize;
}
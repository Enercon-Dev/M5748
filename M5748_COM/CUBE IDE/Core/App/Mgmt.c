
#include "general.h"
#include "Timing.h"
#include "DataBuffer.h"
#include "RowIO.h"
#include "IO.h"
#include "Mgmt.h"
#include "IntUart.h"
#include "IntComm.h"
#include "Command.h"
#include "flash_if.h"


struct Managment mgmt = {0};
struct ReportedMasterData masterData = {0};
struct ConstantData constData;

static void CommWDHandle();
static void updateColectedData();
static void loadConstantData();


void managment()
{

  //Over Temp - TODO: verify that the limit are OK
  DebounceWithHysteresis(&mgmt.sOverTemp, anIn.PCBtemp < TEMPERATURE(65.0),  
                                          anIn.PCBtemp < TEMPERATURE(55.0),
                         T_1SEC,1,1, T_1SEC,1,1);
  
  // 28v input OK 
  DebounceWithHysteresis(&mgmt.sInputOk, anIn.In28V > VOLTAGE_LOW(17.5), anIn.In28V < VOLTAGE_LOW(17),
                         41*T_10mSEC,1,1, 15*T_10mSEC,1,1); //410mSec to declare input OK, 150mSec to declare input Fail
/*  
  uint32_t failur_Code = 0, enable_Code = 0;
  if (mgmt.bSelfTestEnded)
  {
    //TODO: add other internal failures
    failur_Code = (( mgmt.bIntCommProblem    ? BIT(0) : 0) |
                   ( mgmt.bInterlockFailure  ? BIT(1) : 0) |
                   (!mgmt.bModulesVersionOK  ? BIT(2) : 0));
    mgmt.bInternalFailure = (failur_Code != 0);                //SCPI: PCR-> Internal Failure
  
    
    failur_Code |= ((mgmt.sUserCommWD.state ? BIT(8)  : 0) |   //SCPI: PCR-> Comm WD
                    (mgmt.sOverLoad.state   ? BIT(9)  : 0) |   //SCPI: PCR-> Over Load
                    (mgmt.sOverTemp.state   ? BIT(10) : 0) |   //SCPI: PCR-> Over Temp
                    ((mgmt.ovLock != 0)     ? BIT(11) : 0) |   //SCPI: PCR-> Over Voltage
                    ((mgmt.ocLock != 0)     ? BIT(12) : 0));   //SCPI: PCR-> Over Current
      
    mgmt.bFaultDetected = (failur_Code != 0); //bFaultDetected includes bInternalFailure
    
    Debounce(&mgmt.sReady, hpfData[0].bLoadEn && hpfData[1].bLoadEn && //HPF Ready
                           isoData[0].bLoadEn && isoData[1].bLoadEn && //ISO Ready
                           buckData[0].bBuckReady && buckData[1].bBuckReady, //Buck Ready
                           2*T_100mSEC, 1, 2*T_100mSEC, 0,1,1);
                  
      
    mgmt.bOutputReady = mgmt.sInterlock.state &&  //SCPI: PCR->  Open Interlock 
                         !mgmt.bIPReset &&         //non - comm non operational
                         !mgmt.bFaultDetected &&   //SCPI: PCR
                         mgmt.bSelfTestEnded &&    //non - temporary ststus
                         rowIn.bFP_SW_SNS &&       //FRONT pannel Switch - TODO: add to PCR
                         mgmt.sHpfEn.state &&                                      //since now there are Ready signals, this one can be replaced with bFP_SW_SNS. bFP_SW_SNS should be added to SCPI report and to SHDN reasons
                         !bIAP_enabled &&          //SCPI: SYST:FIRM:UPDATE:EN?
                         mgmt.sReady.state;
                                                  
    //normal (not faulty) Shutdown Reasons
    enable_Code = ((!mgmt.sInterlock.state  ? SR_NORM_INTERLOCK : 0) |
                   (!rowIn.bFP_SW_SNS       ? SR_NORM_FP_SWITCH : 0) |
                   (!mgmt.bOutputEnableCMD  ? SR_NORM_SCPI_COMMAND : 0));
    
    outEn_old = mgmt.bOutputEnable;
    mgmt.bOutputEnable = mgmt.bOutputReady && mgmt.bOutputEnableCMD;
    
    
  }
  else
  {
    //self test in progress - keep the output off and do not rize the faultes flags - as the unit is not initialized yet
    mgmt.bInternalFailure = FALSE;
    mgmt.bFaultDetected = FALSE;
    mgmt.bOutputReady = FALSE;
    mgmt.bOutputEnable = FALSE;
  }
*/


  CommWDHandle();
  updateColectedData();

}



//if the COMMAND_PERIOD is changed then the clock flag that syncronize the tel transmition int InternalCommHandle() should be changed as well
#define COMMAND_PERIOD T_200mSEC
static void CommWDHandle()
{
  /*
  int i;
  int bCommProblem = FALSE;
  //sChargerCommProblem - indicates that the last command is lost
  for (i=0; i<MODULES_NUM; i++)
  {
    here the sXXXCommProblem counter only counts up, it is reseted when a charger telemetry received;
    ConservativeDebounce(&mgmt.sHpfCommProblem[i], TRUE, COMMAND_PERIOD*4, 1);
    ConservativeDebounce(&mgmt.sIsoCommProblem[i], TRUE, COMMAND_PERIOD*4, 1);
    ConservativeDebounce(&mgmt.sBuckCommProblem[i], TRUE, COMMAND_PERIOD*4, 1);
    
    bCommProblem = bCommProblem || mgmt.sHpfCommProblem[i].state || mgmt.sIsoCommProblem[i].state || mgmt.sBuckCommProblem[i].state;
  }
  
  mgmt.bIntCommProblem = bCommProblem;
*/
}


#define MAX(a,b) ((a) > (b) ? (a) : (b))
static void updateColectedData()
{
  int b;
  int bVersionsOk = TRUE;
  
  if (!mgmt.bCommRoundCompleted)
    return;
  
  if (!masterData.bAllVersionsCollected)
  {
    //check if all version telemetries received;
    for (b=0; b<BATT_NUM; b++)
    {
      if (masterData.battVersion[b] == 0)
        bVersionsOk = FALSE;
    }
    
    if (masterData.version == 0)
      bVersionsOk = FALSE;
    
    masterData.bAllVersionsCollected = bVersionsOk;
  }
}



void outputsUpate()
{
  rowOut.bFault = masterData.bFaultDetected_CMD;
  
  mgmt.faultLed = masterData.bFaultDetectedLed_CMD ? Led_Blink : Led_Off;
  switch (mgmt.faultLed)
  {
  case Led_Off:
    rowOut.bFault_LED = 0;
    break;
  case Led_On:
    rowOut.bFault_LED = 1;
    break;
  case Led_Blink:
    rowOut.bFault_LED = b500mS_gate;
    break;
  }
  
  rowOut.bISO_FLR = masterData.bISOFault_CMD;
  rowOut.bOUT350V_OK = masterData.bOutputOK_CMD;
  rowOut.bOutSpare = masterData.bSpareOut_CMD;
}


static void loadConstantData()
{
  DataBuffer db;

  //load data from internal Flash
  db.buffer = (uint8_t *)CONSTANTS_FIRST_ADDRESS;
  db.length = BLOCK_SIZE-8;
  db.offset = 0;
  
  constData.hardwareVersion = getShort(&db); //hardware version
  constData.serialNumber = getShort(&db);
  constData.mfgDate = getShort(&db); //MSB=year LSB=week
}


void mgmtInit()
{
  loadConstantData();
}

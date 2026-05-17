
#include "general.h"
#include "Timing.h"
#include "DataBuffer.h"
#include "RowIO.h"
#include "IO.h"
#include "Mgmt.h"
#include "IntUart.h"
#include "IntComm.h"
#include "Command.h"
//#include "PersistentData.h"



//struct ReportedHpfData hpfData[MODULES_NUM] = {0};
//struct ReportedIsoData isoData[MODULES_NUM] = {0};
//struct ReportedBuckData buckData[MODULES_NUM] = {0};
struct Managment mgmt = {0};

static void InternalCommHandle();
static void CommWDHandle();
static void updateColectedData();



void managment()
{

  //Over Temp - TODO: verify that the limit are OK
  DebounceWithHysteresis(&mgmt.sOverTemp, anIn.PCBtemp < TEMPERATURE(65.0),  
                                          anIn.PCBtemp < TEMPERATURE(55.0),
                         T_1SEC,1,1, T_1SEC,1,1);
  
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
#define COMMAND_PERIOD T_50mSEC
static void CommWDHandle()
{
  int i;
  int bCommProblem = FALSE;
  //sChargerCommProblem - indicates that the last command is lost
  for (i=0; i<MODULES_NUM; i++)
  {
    //here the sXXXCommProblem counter only counts up, it is reseted when a charger telemetry received;
    ConservativeDebounce(&mgmt.sHpfCommProblem[i], TRUE, COMMAND_PERIOD*4, 1);
    ConservativeDebounce(&mgmt.sIsoCommProblem[i], TRUE, COMMAND_PERIOD*4, 1);
    ConservativeDebounce(&mgmt.sBuckCommProblem[i], TRUE, COMMAND_PERIOD*4, 1);
    
    bCommProblem = bCommProblem || mgmt.sHpfCommProblem[i].state || mgmt.sIsoCommProblem[i].state || mgmt.sBuckCommProblem[i].state;
  }
  
  mgmt.bIntCommProblem = bCommProblem;
}


#define MAX(a,b) ((a) > (b) ? (a) : (b))
static void updateColectedData()
{
  int i;
  uint32_t hotSpot;
 /* 
  if (buckData[0].bOvLock)
    mgmt.ovLock |= MODULE_A;
  if (buckData[1].bOvLock)
    mgmt.ovLock |= MODULE_B;
      
  if (buckData[0].bOcLock)
    mgmt.ocLock |= MODULE_A; 
    
  if (buckData[1].bOcLock)
    mgmt.ocLock |= MODULE_B;
  
  if (!mgmt.bCommRoundCompleted)
    return; //rest of the function is updates only when data from all modules is collected
  
  // check modules firmware version
  int versionOk = TRUE;
  for (i=0; i<MODULES_NUM; i++)
  {
    versionOk = versionOk && ((hpfData[i].version & 0xF0) != 0);
    versionOk = versionOk && ((isoData[i].version & 0xF0) != 0);
    versionOk = versionOk && ((buckData[i].version & 0xF0) != 0);
  }
  mgmt.bModulesVersionOK = versionOk;
  
  
  //calculate hotspot
  hotSpot = 0; 
  for (i=0; i<MODULES_NUM; i++)
  {
    hotSpot = MAX(hotSpot, hpfData[i].temp);
    hotSpot = MAX(hotSpot, isoData[i].temp2);
    hotSpot = MAX(hotSpot, buckData[i].temp);
  }
  mgmt.baseHotSpot = hotSpot; //hot spot of all base plate termistors
  
  hotSpot = MAX(hotSpot, anIn.temp); //Front Pannel Temp.
  for (i=0; i<MODULES_NUM; i++)
  {
    hotSpot = MAX(hotSpot, isoData[i].temp1);
    if (!mgmt.bOutputEnable)
    {
      hotSpot = MAX(hotSpot, buckData[i].ptcTemp);
      //PTC temp afects the output (prevents turn on only when the output is off
      //for that reason it is reported only when the output is off
    }
  }
  mgmt.hotSpot = hotSpot; //hot spot of all termistors
  
  mgmt.vOut = (buckData[0].vout + buckData[1].vout)/2;
  
  //Vout value to be dispalayed and reported via Ethernet
  if (abs((int32_t)buckData[0].vout - (int32_t)buckData[1].vout) < VOLTAGE(10))
    mgmt.vOutDisplay = mgmt.vOut;
  else
    mgmt.vOutDisplay = MAX(buckData[0].vout, buckData[1].vout);
  
  
  mgmt.iOut = buckData[0].iout + buckData[1].iout;
  ConservativeDebounce(&mgmt.sCCMode, buckData[0].bCCMode && buckData[1].bCCMode, 2,2);
  //the small filter should handle the situation when CC "flips" from one buck to another when voltage is changed/trimed
  //  such "flip" is theoretial and never been witnessed
*/
}



void outputsUpate()
{
  rowOut.bFault = mgmt.bFaultDetected_CMD;
  
  mgmt.faultLed = mgmt.bFaultDetectedLed_CMD ? Led_Blink : Led_Off;
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
  
  rowOut.bISO_FLR = mgmt.bISOFault_CMD;
  rowOut.bOUT350V_OK = mgmt.bOutputOK_CMD;
  rowOut.bOutSpare = mgmt.bSpareOut_CMD;
}


void mgmtInit()
{

}

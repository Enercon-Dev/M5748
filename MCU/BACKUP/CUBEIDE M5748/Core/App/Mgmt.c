
#include "general.h"
#include "Timing.h"
#include "DataBuffer.h"
#include "RowIO.h"
#include "IO.h"
#include "Mgmt.h"
#include "IntUart.h"
#include "IntComm.h"
#include "Command.h"
#include "PersistentData.h"


#define FAM_START_UP_TIME (15*T_1SEC)
#define LIMITS_UPDATE_DELAY (2*T_1SEC)
#define KELVIN_TO_CELSIUS_FACTOR (273.15f)

struct ReportedHpfData hpfData[MODULES_NUM] = {0};
struct ReportedIsoData isoData[MODULES_NUM] = {0};
struct ReportedBuckData buckData[MODULES_NUM] = {0};
struct Managment mgmt = {0};
extern struct PersistentData persData;

static void InternalCommHandle();
static void CommWDHandle();
//static void updateColectedData();
static void remoteLedUpdate();
static void idCodeHandle();
static void protectionLimitsUpdate();
static void handleRegulationFailure();
static void manageCurrentSharing();
static void manageVoltageTrimming();
static void overLoadProtection();
//TEMP managment
static void manageBattTemperature();
static void checkTempDelta(uint32_t deltaTemp);

static void chargerAndSwitchControl();
static void manageBattCharge();

void managment()
{
  
  //manageBattTemperature();
 chargerAndSwitchControl();
 manageBattCharge();

  if(!inputs.sEPO.state && !inputs.sDSBL_FB.state)
  {
      rowOut.bOut_MCU_EN = 1;
  }
  else
      rowOut.bOut_MCU_EN = 0;

  if(mgmt.sLowVbatt.state || mgmt.sDisbalance.state )
      rowOut.bOut_MCU_EN = 0;

  //idCodeHandle();
  

  //CommWDHandle();

 
}



static HeaterState_t heaterState = HEATER_OFF;
static uint32_t heaterOffTime_s = 0;
static uint32_t heaterOffDuration_s = 0;

static void manageBattTemperature()
{
  int32_t vbattTemp1 = rowAnIn.temp1 - KELVIN_TO_CELSIUS_FACTOR;
  int32_t vbattTemp2 = rowAnIn.temp2 - KELVIN_TO_CELSIUS_FACTOR;
  int32_t heaterTemp1 = rowAnIn.temp3 - KELVIN_TO_CELSIUS_FACTOR;
  int32_t heaterTemp2 = rowAnIn.temp4 - KELVIN_TO_CELSIUS_FACTOR;

  if(vbattTemp1 < BATT_BLOCK_TEMP || vbattTemp2 < BATT_BLOCK_TEMP)
  {
    rowOut.bOut_dCh_EN1 = 0;
    rowOut.bOut_dCh_EN2 = 0;
    rowOut.bOut_Charge_Sw_En = 0;
    rowOut.bOut_MCU_EN = 0;
  }
  
  int32_t deltaTemp = vbattTemp1 - vbattTemp2;
  checkTempDelta(deltaTemp < 0 ? (deltaTemp * (-1)) : (deltaTemp));
  deltaTemp = heaterTemp1 - heaterTemp2;
  checkTempDelta(deltaTemp < 0 ? (deltaTemp * (-1)) : (deltaTemp));

  
  if (heaterState == HEATER_COOLDOWN)
{
    heaterOffTime_s++;

    if ((heaterOffTime_s > HEATER_COOLDOWN_TIMEOUT_S) &&
        (heaterTemp1 < HEATER_MIN_VOLTAGE_THRESHOLD || heaterTemp2 < HEATER_MIN_VOLTAGE_THRESHOLD ))
    {
        mgmt.bHeaterProblem = 1;
        heaterState = HEATER_ON; // continue heating 
    }
    else
        mgmt.bHeaterProblem = 0;

}
  
  switch(heaterState)
  {
  case HEATER_OFF:
   
     rowOut.bOut_Heater_En = 0;
        
     if(vbattTemp1 < BATT_HEAT_ON_TEMP || vbattTemp2 < BATT_HEAT_ON_TEMP)
     {
       heaterState = HEATER_ON;

     }
     break;
  
  case HEATER_ON:
    
    rowOut.bOut_Heater_En = 1;
    
     if(heaterTemp1 > HEATER_MAX_TEMP_THRESHOLD || heaterTemp2 > HEATER_MAX_TEMP_THRESHOLD)
     {
        heaterState = HEATER_COOLDOWN;
        heaterOffTime_s = 0;
        rowOut.bOut_Heater_En = 0;

     }
    break;
    
   case HEATER_COOLDOWN:
            rowOut.bOut_Heater_En = 0;
            
             if(heaterTemp1 < HEATER_MAX_TEMP_THRESHOLD && heaterTemp2 < HEATER_MAX_TEMP_THRESHOLD)
             {
             heaterState = HEATER_ON;
             }
             else
               heaterState = HEATER_OFF;
      break;
    
  default:
       heaterState = HEATER_OFF;
        rowOut.bOut_Heater_En = 0;
   
  }
    
}

static void checkTempDelta(uint32_t deltaTemp)
{
  if (heaterState == HEATER_OFF)
    {
        heaterOffDuration_s++;

        if (heaterOffDuration_s > SENSOR_CHECK_TIME_S &&
            deltaTemp > SENSOR_DELTA_TEMP)
        {
            mgmt.bTempSensorsProblem = 1;
        }
    }
    else
    {
        heaterOffDuration_s = 0;
        mgmt.bTempSensorsProblem = 0;
    }
}

static void chargerAndSwitchControl()
{
  //NEW CODE
  if(!rowOut.bOut_Heater_En && !rowOut.bOut_Charge_Sw_En )
  {
    rowOut.bOut_dCh_EN1 = 0;
    rowOut.bOut_dCh_EN2 = 0;
  }
  
  else if(rowOut.bOut_Heater_En  && rowOut.bOut_Charge_Sw_En )
  {
      rowOut.bOut_dCh_EN1 = 1;
      rowOut.bOut_dCh_EN2 = 1;

  }
  
  else
  {
       rowOut.bOut_dCh_EN1 = 0;
       rowOut.bOut_dCh_EN2 = 1;
  }

  // TODO: Inrush protection
  if(rowOut.bOut_dCh_EN1 || rowOut.bOut_dCh_EN2)
  {
     rowOut.bOut_Charge_Sw_En = 0;
  }
  
  
  //OLD CODE
   if(mgmt.batt_SOC < SOC(98.0))
   {
      if(anIn.charger < anIn.vBatt + VOLTAGE(1))
      {
        rowOut.bOut_Charge_Sw_En = 1;
      }    
   }
 
   else
   {
    if(mgmt.batt_SOC == SOC(100.0)) // In case of a bug in charging
        rowOut.bOut_Charge_Sw_En = 0;
   }
   
   if(!mgmt.sFullBatt.state)
    {
       ConservativeDebounce(&mgmt.sFullBatt,
       rowOut.bOut_MCU_EN && rowOut.bOut_Charge_Sw_En && anIn.charger > VOLTAGE(45) && anIn.vBatt > VOLTAGE(45) && anIn.Ich < CURRENT(0.2f), 10 *T_1SEC, 1);
    
       if(mgmt.sFullBatt.state)
       {
          mgmt.batt_SOC = SOC(100.0);
          rowOut.bOut_Charge_Sw_En = 0;
          persData.lastFullChargeTime = HAL_GetTick();
       }
       
    }
   //TODO : handle o_Charge and raise DISBALANCE
//   if(rowIn.bIn_dO_Charge) // modify this to debounce
//   {
//     if(rowOut.bOut_MCU_EN)
//       mgmt.sDisbalance.state = 1;
//   }
//   else
//       mgmt.sDisbalance.state = 0;
//
  
}

static void manageBattCharge()
{
  if(!mgmt.sLowVbatt.state)
  {
   ConservativeDebounce(&mgmt.sLowVbatt,rowOut.bOut_MCU_EN && anIn.vBatt < VOLTAGE(32) , 10 * T_1SEC, 1);
   if(mgmt.sLowVbatt.state)
   {
	   mgmt.batt_SOC = SOC(0);

   }
  }
       
   // save to eeprom
  //persData.BattCharge = anIn.charger; // To be calculated
}

void outputsUpate()
{

}




static void overLoadProtection()
{
 
  
}


void ClearFaults()
{
//  SetDebounce(&mgmt.sOverLoad, FALSE);
//  mgmt.ovLock = 0;
//  mgmt.ocLock = 0;
}

void clearSDReason()
{
//  int i;
//  mgmt.SR_CommCounter = 0;
//  mgmt.SR_Failure = 0;
//  mgmt.SR_Normal = 0;
//  for (i=0; i<MODULES_NUM; i++)
//  {
//    mgmt.SR_HPF[i]  = 0;
//    mgmt.SR_ISO[i]  = 0;
//    mgmt.SR_Buck[i] = 0;
//  }
}



static void InternalCommHandle()
{
//  int bNextState = FALSE;
//  //CommModule module;
//  
//  if (mgmt.ComTimeout > 3*T_500mSEC)
//    mgmt.ComTimeout = T_500mSEC; //just in case
//  
//  if (mgmt.ComTimeout > 0)
//    mgmt.ComTimeout--;
//  
//  //varify that the CurrentCommModule is correct
//  if ((mgmt.CurrentCommModule != ModuleA) &&
//      (mgmt.CurrentCommModule != ModuleB))
//  {
//    //error - should never happend
//    mgmt.commState = 0;
//    mgmt.CurrentCommModule = ModuleA; 
//  }
//  
//  //note: mgmt.commState is initialized to "1" in order to start comunication immidiately without waiting for the first b100mS_flg
//  mgmt.bCommRoundCompleted = FALSE;
//  switch (mgmt.commState)
//  {
//  case 0:
//    //perform comunication sesion eack 50mS
//    if ((mgmt.CurrentCommModule != ModuleA) || (b50mS_flg))
//    {
//      mgmt.commState++;
//      //fallthrough to next case
//    }
//    else
//    {
//      break;
//    }
//    
//  case 1: //request HPF Status
//    //note: this case is duplicated in the last case
//    //note: to case 1 we can arrive either from case 1 or from the last case
//    if (sendCommand(mgmt.CurrentCommModule, OPCODE_HPF_TEL_REQUEST))
//    {
//      mgmt.commState++;
//      mgmt.ComTimeout = T_50mSEC;
//    }
//    break;
//    
//  case 2: //receive HPF Status
//    //note: to case 2 we can arrive either from case 1 or from the last case
//    if (mgmt.lastReceivedTel == OPCODE_HPF_STATUS_TEL)
//    {
//      mgmt.commState++;
//      //fallthrough to next case
//    }
//    else if(mgmt.ComTimeout == 0)
//    {
//      mgmt.commState++;
//      //fallthrough to next case
//    }
//    else
//    {
//      break;
//    }
//    
//  case 3: //request ISO Status
//    if (sendCommand(mgmt.CurrentCommModule, OPCODE_ISO_TEL_REQUEST))
//    {
//      mgmt.commState++;
//      mgmt.ComTimeout = T_50mSEC;
//    }
//    break;
//    
//  case 4: //receive ISO Status
//    if (mgmt.lastReceivedTel == OPCODE_ISO_STATUS_TEL)
//    {
//      mgmt.commState++;
//      //fallthrough to next case
//    }
//    else if(mgmt.ComTimeout == 0)
//    {
//      mgmt.commState++;
//      //fallthrough to next case
//    }
//    else
//    {
//      break;
//    }
//    
//
//  case 5: //set Buck State
//    //send SET command to both modules simultaniously during module A comunication phase
//    //the SET command saves the telemetry in a buffer for later retrieval so bothe modules are sampled at the same time
//    if (mgmt.CurrentCommModule != ModuleA)
//    {
//      mgmt.commState++;
//      //fallthrough to next case
//    }
//    
//    else if (sendCommand(ModuleAll, OPCODE_BUCK_SET_STATE))
//    {
//      mgmt.commState++;
//      //mgmt.ComTimeout = T_50mSEC;
//      //mgmt.bBuckSyncUpdateRequiered = FALSE;
//      break;
//    }
//    else
//    {
//      break;
//    }
//    
//
//  case 6: //request Buck Status
//    if (sendCommand(mgmt.CurrentCommModule, OPCODE_BUCK_TEL_REQUEST))
//    {
//      mgmt.commState++;
//      mgmt.ComTimeout = T_50mSEC;
//    }
//    break;
//
//    
//  case 7: //receive Buck Status
//    if (mgmt.lastReceivedTel == OPCODE_BUCK_STATUS_TEL)
//    {
//      bNextState = TRUE;
//    }
//    else if(mgmt.ComTimeout == 0)
//    {
//      bNextState = TRUE;
//    }
//    
//    if (bNextState) //switch module
//    {
//#ifdef M5480
//      //memcpy(hpfData[1], hpfData[0], sizeof (ReportedHpfData));
//      hpfData[1] = hpfData[0];
//      isoData[1] = isoData[0];
//      buckData[1] = buckData[0];
//      SetDebounce(&mgmt.sHpfCommProblem[1], FALSE);
//      SetDebounce(&mgmt.sIsoCommProblem[1], FALSE);
//      SetDebounce(&mgmt.sBuckCommProblem[1], FALSE);
//#endif
//      
//#ifndef M5480
//      if (mgmt.CurrentCommModule == ModuleA)
//      {
//        mgmt.CurrentCommModule = ModuleB;
//        mgmt.commState = 1;
//        //case 1 duplication:
//        if (sendCommand(mgmt.CurrentCommModule, OPCODE_HPF_TEL_REQUEST))
//        {
//          mgmt.commState++;
//          mgmt.ComTimeout = T_50mSEC;
//        }
//      }
//      else
//#endif
//      {
//        mgmt.CurrentCommModule = ModuleA;
//        mgmt.bCommRoundCompleted = TRUE;
//        mgmt.commState = 0;
//        //here cycle loop is lost. iI need be, case 0 and case 1 can be incorporated here to save this time
//      }
//    }
//    
//    //mgmt.CurrentCommModule = ModuleA; //for debug only
//    uart_SelectModule(mgmt.CurrentCommModule);
//    break;
//    
//  default:
//    mgmt.commState = 0;
//    mgmt.CurrentCommModule = ModuleA;
//    uart_SelectModule(mgmt.CurrentCommModule);
//  }
//  
}

//if the COMMAND_PERIOD is changed then the clock flag that syncronize the tel transmition int InternalCommHandle() should be changed as well
#define COMMAND_PERIOD T_50mSEC
static void CommWDHandle()
{

}




static void idCodeHandle() //TODO: maybe use this instead of battery decode?
{
//  static int timeCounter = 10;
//  static int idState = 0;
//  uint32_t idCode;
//  
//  if (mgmt.bIDCodeReady)
//    return;
//  
//  // The ID inputs have 3 cycles debounce. so the timeCounter must be >=3;
//  // The state machine has 4 states so it takes 5ms*5*4=100mS to finish
//  if (timeCounter < 5)
//  {
//    timeCounter++;
//    return;
//  }
//  timeCounter = 0;
//  
//  idCode = //ID4, ID3, ID2, ID1
//    (inputs.sIn_ID1.state ? BIT(0) : 0) |
//    (inputs.sIn_ID2.state ? BIT(1) : 0) |
//    (inputs.sIn_ID3.state ? BIT(2) : 0) |
//    (inputs.sIn_ID4.state ? BIT(3) : 0) ;
//  // 4bit ID Code (the ID 5,6 are not in use)
//  // since any 2 ID pins can be shorted it enable 10 codes
//  
//  switch (idState++)
//  {
//  case 0:
//    rowOut.bOut_ID1 = 1;
//    break;
//  
//  case 1:
//    mgmt.idCode |= (idCode & 0x0F);
//    rowOut.bOut_ID1 = 0;
//    rowOut.bOut_ID2 = 1;
//    break;
//  
//  case 2: 
//    mgmt.idCode |= (idCode & 0x0F) << 4;
//    rowOut.bOut_ID2 = 0;
//    rowOut.bOut_ID3 = 1;
//    break;
//  
//  case 3: 
//    mgmt.idCode |= (idCode & 0x0F) << 8;
//    rowOut.bOut_ID3 = 0;
//    rowOut.bOut_ID4 = 1;
//    break;
//    
//  case 4: 
//    mgmt.idCode |= (idCode & 0x0F) << 12;
//    rowOut.bOut_ID4 = 0;
//    mgmt.bIDCodeReady = TRUE;
//    break;
//    
//  default:
//    mgmt.idCode = 0;
//    timeCounter = 10;
//    idState = 0;
//    return;
//  }
//  
//  if (mgmt.bIDCodeReady && mgmt.idCode == 0x8433) //ID1 and ID2 shorted
//  {
//    mgmt.bIPReset = resetIpSettings();
//  }
}

         

static void protectionLimitsUpdate()
{
  
}


static void handleRegulationFailure()
{
  
}


//returns TRUE if the output is certainly OFF
int OutOffCertain()
{
  if (mgmt.bOutputEnable || buckData[0].bEnableCmd || buckData[1].bEnableCmd)
    return FALSE;
  else
    return TRUE;
  //TODO: add verification that the buckData is valid
}


void mgmtInit() 
{
  
  //TODO: UART GET ALL IO STATES and REPORT + send BATT_EN
  
  // TODO: READ FROM EEPROM!!
  mgmt.commState = 1;
  readID();
  loadPersistentData();
  //same params as in SCPI *RST
//  mgmt.bOutputEnableCMD = FALSE;
//  mgmt.VoutCMD = DEFAULT_VOUT_CMD;
//  mgmt.IoutCMD = DEFAULT_IOUT_CMD;
//  mgmt.OverVoltageCMD = DEFAULT_OV_CMD;
//  mgmt.OverCurrentCMD = DEFAULT_OC_CMD;
//  
//  mgmt.FanCMD = -1;
//  mgmt.SR_Normal = SR_NORM_RESET;
}

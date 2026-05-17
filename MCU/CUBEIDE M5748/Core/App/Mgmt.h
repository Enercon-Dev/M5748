#ifndef _MGMT_H_
#define _MGMT_H_

#include "ReportedData.h"

#define DEFAULT_VOUT_CMD  VOLTAGE(250)
#define MAX_VOUT_CMD      VOLTAGE(355)
#define MIN_VOUT_CMD      VOLTAGE(150)

//MAX_VOLTAGE_CORRECTION is in command resolution units (166mV) so max correction is about +-10V
#define MAX_VOLTAGE_CORRECTION 60
//MAX_VOLTAGE_TRIMMING  is in command resolution units as well so max trimming is about +-5V
#define MAX_VOLTAGE_TRIMMING 30

#define DEFAULT_IOUT_CMD  CURRENT(50)
#define MAX_IOUT_CMD      CURRENT(50)
#define MIN_IOUT_CMD      CURRENT(5)

#define DEFAULT_OV_CMD  VOLTAGE(400)
#define MAX_OV_CMD      VOLTAGE(420)
#define MIN_OV_CMD      VOLTAGE(150)

#define DEFAULT_OC_CMD  CURRENT(58)
#define MAX_OC_CMD      CURRENT(58)
#define MIN_OC_CMD      CURRENT(5)

#define SR_NORM_RESET        BIT(0)
#define SR_NORM_INTERLOCK    BIT(1)
#define SR_NORM_FP_SWITCH    BIT(2)
#define SR_NORM_SCPI_COMMAND BIT(3)

#define BATT_BLOCK_TEMP        (10 * 10)    // 10.0�C
#define BATT_HEAT_ON_TEMP      (15 * 10)    // 15.0�C
#define HEATER_MAX_TEMP_THRESHOLD (50 * 10) // default in  (deg C) X 10 will be  TBD!
#define HEATER_MIN_VOLTAGE_THRESHOLD (20) //min heater voltage voltage - DEFAULT IS TBD!
#define HEATER_COOLDOWN_TIMEOUT_S   (240)
#define SENSOR_DELTA_TEMP      (15*10)
#define SENSOR_CHECK_TIME_S   (240)

typedef enum {
 BATT_OFF = 0,
 BATT_TURNING_ON,
 BATT_CONNECTED,
 BATT_FAILED
}Batt_State_t;

typedef enum{
 STATE_OFF,
 STATE_CHARGE_ONLY,
 STATE_OPERATIONAL

}System_State_t;

struct Managment
{
  // Battery Temp
  int bHeaterProblem;
  int bTempSensorsProblem;
  
  int bResistanceFault;

  //Charger SW 
  int batt_SOC; // state of charge 
  Signal_t sFullBatt;
  Signal_t sDisbalance;
  Signal_t sLowVbatt;

  //internal communication
  CommModule CurrentCommModule;
  uint32_t commState;
  uint32_t lastReceivedTel;
  int bCommRoundCompleted;
  
  Signal_t sHpfCommProblem[MODULES_NUM];
  Signal_t sIsoCommProblem[MODULES_NUM];
  Signal_t sBuckCommProblem[MODULES_NUM];
  int bIntCommProblem;
  int bModulesVersionOK;
  
  //modules data
  uint32_t iOut; //the total Output Current
  uint32_t vOut; //the Average Output Voltage
  uint32_t vOutDisplay; //Vout value to be displayed and reported
  uint32_t baseHotSpot;
  uint32_t hotSpot;
  Signal_t sCCMode;
  uint32_t ovLock;
  uint32_t ocLock;
  
  //user commands:
  int bOutputEnableCMD;
  uint32_t VoutCMD; //10mV resolution
  int32_t voltageCorrection;
  int32_t voltageTrimming;
  uint32_t IoutCMD; //10mA resolution
  uint32_t OverVoltageCMD; //10mV resolution
  uint32_t OverCurrentCMD; //10mA resolution
  int bPSMode; //PowerSuply Mode (default = Charger Mode)
  int32_t FanCMD;
  
  // data from master
  int bEnableFromMaster;
  int bHeaterTestFromMaster;
  System_State_t systemState;
  uint16_t qCharge;
  uint16_t qDischarge;
  
  //unit managment
  Signal_t sEnableHPF;
  Signal_t sInputReleyEn;
  Signal_t sHpfEn;
  Batt_State_t batt_State;

  int bOutputReady; //old name: bOutputEnable
  int bOutputEnable;
  int bFaultDetected;
  int bInternalFailure;
  int bInterlockReady;
  Signal_t sReady;
  Signal_t sOverLoad;
  uint32_t fanStartUpDelay;
  Signal_t sFanFail;
  Signal_t sOverTemp;
  
  uint32_t voltageLowLimit;
  uint32_t voltageHighLimit;
  uint32_t voltageLimitDelay;
  uint32_t setVoltage_prev;
  Signal_t sHighVoltage;
  Signal_t sHighCurrent;
  Signal_t sLowPower;
    
  
  uint32_t linkBlinkCounter;
  int bScpiCommandReceived;
  Signal_t sUserCommWD;
  
  //ID Code
  int bIDCodeReady;
  uint32_t idCode;
  int bIPReset;
  
  //Interlock
  int bInterlockFailure;
  Signal_t sInterlock;
  
  //display self test
  int bSelfTestEnded;
  uint32_t SelfTestCounter;
  
  //Shutdown Readon:
  uint16_t SR_Failure;
  uint16_t SR_Normal;
  uint32_t SR_HPF[MODULES_NUM];
  uint32_t SR_ISO[MODULES_NUM];
  uint32_t SR_Buck[MODULES_NUM];
  int SR_CommCounter;
};


extern struct ReportedHpfData hpfData[MODULES_NUM];
extern struct ReportedIsoData isoData[MODULES_NUM];
extern struct ReportedBuckData buckData[MODULES_NUM];
extern struct Managment mgmt;

void managment();
void outputsUpate();
void ClearFaults();
void clearSDReason();
int OutOffCertain();
//void mstrOutputsInit();
void mgmtInit();


#endif //_MGMT_H_

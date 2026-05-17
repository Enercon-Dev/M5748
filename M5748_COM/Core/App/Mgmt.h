#ifndef _MGMT_H_
#define _MGMT_H_

//#include "ReportedData.h"

typedef enum {Led_Off, Led_On, Led_Blink}LedState;
#define MODULES_NUM 2

struct Managment
{
  Signal_t sHpfCommProblem[MODULES_NUM];
  Signal_t sIsoCommProblem[MODULES_NUM];
  Signal_t sBuckCommProblem[MODULES_NUM];
  int bIntCommProblem;
  //int bModulesVersionOK;
  
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
  //int bOutputEnableCMD;
  
  
  //unit managment
  Signal_t sOverTemp;
  Signal_t sUserCommWD;
  
  LedState faultLed;
  
  //commands from Master:
  int bOutputOK_CMD;
  int bISOFault_CMD;
  int bFaultDetected_CMD;
  int bFaultDetectedLed_CMD;
  int bSpareOut_CMD;
};


//extern struct ReportedHpfData hpfData[MODULES_NUM];
//extern struct ReportedIsoData isoData[MODULES_NUM];
//extern struct ReportedBuckData buckData[MODULES_NUM];
extern struct Managment mgmt;

void managment();
void outputsUpate();
void mgmtInit();


#endif //_MGMT_H_

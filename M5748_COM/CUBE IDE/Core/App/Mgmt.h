#ifndef _MGMT_H_
#define _MGMT_H_

#include "ReportedData.h"

typedef enum {Led_Off, Led_On, Led_Blink}LedState;
//#define MODULES_NUM 2

struct Managment
{
  //Signal_t sHpfCommProblem[MODULES_NUM];
  //Signal_t sIsoCommProblem[MODULES_NUM];
  //Signal_t sBuckCommProblem[MODULES_NUM];
  int bIntCommProblem;
  int bCommRoundCompleted;

  //unit managment
  Signal_t sOverTemp;
  Signal_t sInputOk;
  
  LedState faultLed;
};


struct ConstantData
{
  uint32_t hardwareVersion; 
  uint32_t serialNumber;
  uint32_t mfgDate;
};

extern struct Managment mgmt;
extern struct ReportedMasterData masterData;
extern struct ConstantData constData;

void managment();
void outputsUpate();
void mgmtInit();


#endif //_MGMT_H_

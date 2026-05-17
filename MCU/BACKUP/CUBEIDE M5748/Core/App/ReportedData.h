
#ifndef _REPORTRD_DATA_H_
#define _REPORTRD_DATA_H_

//#include "Mgmt.h"

//TODO: this is not the best place for these macros
#define CURRENT(x) ((int32_t)((x)*1000))
#define VOLTAGE(x) ((uint32_t)((x)*100))
#define TEMPERATURE(x) ((uint32_t)(2731 + (x)*10))
#define SOC(x)((uint32_t)((x)*100))
#define IOUT_BASIC_OFFSET CURRENT(4.17)


//TODO: update this header for M5748
struct ReportedHpfData
{
  uint32_t version;
  //ZeroCross,CurrentLimit, Comp - not decoded
  int bHPFSync;
  int bInrushEnded;
  int bUvcc_aux;
  int BusHOV;
  int BusLOV;
  
  //uint32_t ph_A; //not decoded
  //uint32_t ph_B; //not decoded
  //uint32_t ph_C; //not decoded
  uint32_t bus_p;
  uint32_t bus_n;
  uint32_t bus;
  uint32_t temp;
  uint32_t vcc3_3;
  uint32_t vinRect;
  uint32_t errorAmp;
  uint32_t balancerErrorAmp;
  
  int bInrushEn;
  int bConvertersEn;
  int bLoadEn;
  int bPhaseLoss;
  int bInOk;
  int bDelayedInOk;
  int OVLock;
  
  uint32_t ShdnReason;
  //int bIntFailure;
  //int bBusOV;
  //int bBusUV;
};

struct ReportedIsoData
{
  uint32_t version;
  int bUvccHigh;
  int bHPF_Ready;
  int bOV;
  
  uint32_t vIso;
  //uint32_t Iprimary; //not decoded
  uint32_t temp1;
    uint32_t temp2;
  //uint32_t Iprimary2; //not decoded
  int bLoadEn;
  int bCurrentFault;
  int bOVLock;
  int bWakeUpTimeOut;
  
  uint32_t ShdnReason;
};

struct ReportedBuckData
{
  uint32_t version;
  int bUvccAux;
  int bRowIsoReady;
  //int bComp1; //not decoded
  //int bComp2 //not decoded
  int bIsoReady;
  int bEnableCmd;
  uint32_t voutCmd;
  uint32_t ioutCmd;
  uint32_t ioutMeasCmd;
  uint32_t overVoltageCmd;
  uint32_t overCurrentCmd;
  
  uint32_t vin;
  uint32_t iout_wtOffset;
  uint32_t iout;
  uint32_t temp;
  uint32_t ptcTemp;
  uint32_t vcc3_3;
  uint32_t vout;
  
  int bCCMode;
  int bOcLock;
  int bOvLock;
  int bADCFail;
  int bBuckEnable;
  int bBuckReady;
  
  uint32_t ShdnReason;
};

#endif // _REPORTRD_DATA_H_

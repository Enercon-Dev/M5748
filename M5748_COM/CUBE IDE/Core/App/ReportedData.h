
#ifndef _REPORTRD_DATA_H_
#define _REPORTRD_DATA_H_

struct ReportedMasterData
{
  int bUpdated;
  uint32_t version;
  uint32_t battVersion[BATT_NUM]; //not realy a master data
  int bAllVersionsCollected;
  
  //Analog
  uint32_t OutR, OutL, OutMBD;
  
  //commands from Master:
  int bOutputOK_CMD;
  int bISOFault_CMD;
  int bFaultDetected_CMD;
  int bFaultDetectedLed_CMD;
  int bSpareOut_CMD;
  
  //status
  int bOutputLOK;
  int bOutputROK;
  int bOutputMbdOK;
  int bInterlock;
  int bDeratedPerformance;
  uint8_t BattStatusCode; //4 bits
  int bBattCharging;
  int bBattOk;
  uint32_t soc;
};

#endif // _REPORTRD_DATA_H_
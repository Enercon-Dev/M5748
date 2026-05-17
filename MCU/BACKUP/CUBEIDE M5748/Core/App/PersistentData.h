#ifndef _PERSISTENT_DATA_H_
#define _PERSISTENT_DATA_H_



struct PersistentData
{
  uint32_t serialNumber; //located on a seperate page from other data
  uint32_t mfgDate;
  uint32_t BattCharge ;
  uint32_t lastFullChargeTime;
};

extern struct PersistentData persData;

int loadPersistentData();
int savePersistentData();
int bPersistentDataValid();


#endif //_PERSISTENT_DATA_H_

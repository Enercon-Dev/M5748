#include "stm32f1xx_hal.h"
#include "general.h"
#include "MacEeprom.h"
#include "PersistentData.h"
#include "DataBuffer.h"


#define PD_VERSION 0x01

#define PERS_BUFFER_LENGTH 13
uint8_t persBuffer[PERS_BUFFER_LENGTH+4]; //the last +4 is just in case

struct PersistentData persData;
static int bPersDataValid = FALSE;


//this is a blocking function
int loadPersistentData()
{
  DataBuffer db;
  uint8_t version;
  
  if (bPersDataValid)
    return 0; //data already loaded
  
  persData.serialNumber = getSerialNumber();
  
  if (getPersistantData(persBuffer, PERS_BUFFER_LENGTH) != 0)
    return -1; //load failed
  
  db.buffer = persBuffer;
  db.length = PERS_BUFFER_LENGTH;
  db.offset = 0;
  
  version = getByte(&db);
  
  if (version != PD_VERSION)
    return -2;
  
  //TODO: add CRC
  
//  persData.ipAddress = getLong(&db);
//  persData.subNetMask = getLong(&db);
//  persData.defaultGateway = getLong(&db);
  
  bPersDataValid = TRUE;
  
  return 0;
}


//this is a blocking function
int savePersistentData()
{
  DataBuffer db;
  
  db.buffer = persBuffer;
  db.length = PERS_BUFFER_LENGTH;
  db.offset = 0;
  
  fillByte(&db, PD_VERSION);
//  fillLong(&db, persData.ipAddress);
//  fillLong(&db, persData.subNetMask);
//  fillLong(&db, persData.defaultGateway);
  //TODO: add CRC
  
  return writePersistantData(persBuffer, PERS_BUFFER_LENGTH);
}


int bPersistentDataValid()
{
  return bPersDataValid;
}

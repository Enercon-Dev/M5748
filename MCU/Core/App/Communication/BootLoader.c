
#include "general.h"
#include "DataBuffer.h"
#include "IntComm.h"
#include "IntUart.h"
#include "FlashCommon.h"
#include <string.h>

static int bVerifyBlockIndex(uint32_t baseAddr, uint32_t dataLen);
static int bVerifyAddress(uint32_t baseAddr);
static int bVerifyBlockEmpty(uint32_t baseAddr, uint32_t dataLen);
static int writeBlock(uint32_t baseAddr, uint32_t dataLen, DataBuffer* db);
static void flashUnlock();
static void flashLock();
AckCode fillReadMemoryTel(DataBuffer* db);
static int bVerifyClearBlockIndex(uint32_t blockAddr);
static int resetBlock(uint32_t blockAddr);
AckCode changeBootCmd(DataBuffer* db);
static int bTestBootCRC();
static void stopAllInterrupts();
static int swapBootBlock();

#define LOADER_PASSWORD 0xBCE31F90

AckCode writeMemCmd(DataBuffer* db)
{
  if (getRemainingLength(db) < 8 + 2) {
    // not enough data in buffer for address and length
    return Ack_WrongLength;
  }
  
  if (getLong(db) != LOADER_PASSWORD) {
    //wrong password
    return Ack_WrongParameter;
  }
  
  uint32_t baseAddr = getLong(db);
  uint32_t dataLen = getShort(db);
  
  if (!bVerifyBlockIndex(baseAddr, dataLen)) {
    //wrong Address or data length
    return Ack_WrongParameter;
  }
  
  if (dataLen > getRemainingLength(db)) {
    // not enough data in buffer
    return Ack_WrongLength;
  }
  
  if (!bVerifyBlockEmpty(baseAddr, dataLen)) {
    // block is not empty
    return ACK_GenerlError;
  }
  
  if (!writeBlock(baseAddr, dataLen, db)) {
    // write Fail
    return ACK_GenerlError;
  }
  
  return Ack_NoError;
}


static int bVerifyBlockIndex(uint32_t baseAddr, uint32_t dataLen)
{
  if (!bVerifyAddress(baseAddr))
  {
    // block out of static memory range
    return FALSE;
  }
  
  unsigned long offsetInBlock = baseAddr & (BLOCK_SIZE-1);
  if ((offsetInBlock & 0x3) || (dataLen & 0x03)) {
    // illegal allignment
    return FALSE;
  }

  if ((offsetInBlock + dataLen) > BLOCK_SIZE) {
    // Do not allow multiple block operations
    return FALSE;
  }  
  
  return TRUE;
}


static int bVerifyAddress(uint32_t baseAddr)
{
#ifdef BOOT_LOADER
  if (!(baseAddr >= BOOT_PERSISTENT_TOP && baseAddr < BOOT_MEMORY_TOP) &&
      !(baseAddr >= BOOT_COPY_BLOCK && baseAddr < BOOT_LOADER_BASE))
#else //regular program
  if (!(baseAddr >= BOOT_COPY_BLOCK && baseAddr < BOOT_LOADER_TOP))
#endif
  {
    // block out of static memory range
    return FALSE;
  }
  return TRUE;
}


// **************************************************************
// verify that target block is empty - 0xFF
// **************************************************************
static int bVerifyBlockEmpty(uint32_t baseAddr, uint32_t dataLen)
{
  if (baseAddr == 0) {
    return FALSE;
  }

  unsigned long topAddr = baseAddr + dataLen;
  unsigned long targetAddr;
  for (targetAddr = baseAddr; 
       targetAddr < topAddr;
       targetAddr += 2)
  {
    uint16_t thisVal = *(uint16_t*)targetAddr;
    if (thisVal != (uint16_t)0xFFFF) {
      return FALSE;
    }
  }
  
  return TRUE;
}
      
      
// **************************************************************
// reads block from stream and write it into flash memory
// **************************************************************
static int writeBlock(uint32_t baseAddr, uint32_t dataLen, DataBuffer* db)
{
  if (baseAddr == 0) {
    return FALSE;
  }
  
  flashUnlock();

  unsigned long topAddr = baseAddr + dataLen;
  unsigned long targetAddr;
  for (targetAddr = baseAddr; 
       targetAddr < topAddr;
       targetAddr += 2)
  {
    //note: the memory is Little Endian
    uint16_t thisShort = getByte(db);
    thisShort = ((uint16_t)getByte(db))<<8 | thisShort;
    
    if (FLASH_ProgramHalfWord(targetAddr, thisShort) != FLASH_COMPLETE) {
      break;
    }
    
    if (thisShort != *(uint16_t*)targetAddr) {
      // internal error
      break;
    }  
  }

  flashLock();
  
  if (targetAddr < topAddr)
    return FALSE; //write fail
  else
    return TRUE; //block write succeeded
}

      
static void flashUnlock()
{
  FLASH_Unlock();

  FLASH_Status flashstatus = FLASH_GetStatus();
  if (flashstatus == FLASH_ERROR_PG) { 
    FLASH_ClearFlag(FLASH_FLAG_PGERR);
  }
  if (flashstatus == FLASH_ERROR_WRP) {
    FLASH_ClearFlag(FLASH_FLAG_WRPRTERR);
  }
}


static void flashLock()
{
  FLASH_Lock();
}


AckCode fillReadMemoryTel(DataBuffer* db, struct TelRequest* tel)
{
  DataBuffer telDB;
  telDB.buffer = tel->param;
  telDB.length = tel->paramLength;
  telDB.offset = 0;
  
  if (getRemainingLength(&telDB) < 8 + 2) {
    // not enough data in buffer for address and length
    return Ack_WrongLength;
  }
  
  if (getLong(&telDB) != LOADER_PASSWORD) {
    //wrong password
    return Ack_WrongParameter;
  }
  
  uint32_t baseAddr = getLong(&telDB);
  uint32_t dataLen = getShort(&telDB);
  
  if (!bVerifyBlockIndex(baseAddr, dataLen)) {
    //wrong Address or data length
    return Ack_WrongParameter;
  }
  
  fillLong(db, baseAddr);
  fillShort(db, dataLen);
  fillArray(db, (uint8_t*)baseAddr,  dataLen);
  return Ack_NoError;
}


AckCode clearMemCmd(DataBuffer* db)
{
  if (getRemainingLength(db) < 8 ) {
    // not enough data in buffer for address and length
    return Ack_WrongLength;
  }
  
  if (getLong(db) != LOADER_PASSWORD) {
    //wrong password
    return Ack_WrongParameter;
  }
  
  uint32_t blockAddr = getLong(db);
  
  if (!bVerifyClearBlockIndex(blockAddr)) {
    //wrong Address or data length
    return Ack_WrongParameter;
  }

  if (!resetBlock(blockAddr)) {
    //fail to reset the block
    return ACK_GenerlError;
  }
  
  return Ack_NoError;
}


static int bVerifyClearBlockIndex(uint32_t blockAddr)
{
  if (!bVerifyAddress(blockAddr))
  {
    // block out of static memory range
    return FALSE;
  }

  uint32_t offsetInBlock = blockAddr & (((uint32_t)BLOCK_SIZE)-1);
  if (offsetInBlock != 0) {
    // support only head of block cleanup.
    return FALSE;
  }
  
  return TRUE;
}

// **************************************************************
// resets block in flash memory
// **************************************************************
static int resetBlock(uint32_t blockAddr)
{
  int succes = TRUE;
  
  if (blockAddr == 0) {
    return FALSE;
  }
  
  flashUnlock();

  if (FLASH_ErasePage(blockAddr) != FLASH_COMPLETE) {
    succes = FALSE;
  }
  
  flashLock();
  return succes;
}


//char dummyKeepMemory(); //defined in "memoryKeeper.c"
AckCode changeBootCmd(DataBuffer* db)
{
  if (getRemainingLength(db) < 4 ) {
    // not enough data in buffer for address and length
    return Ack_WrongLength;
  }

  if (getLong(db) != LOADER_PASSWORD) {
    //wrong password
    return Ack_WrongParameter;
  }  
  
  if (!bTestBootCRC()) {
    // CRC test fail
    return ACK_GenerlError;
  }
  
  stopAllInterrupts();
  swapBootBlock();
  NVIC_SystemReset();
  //return (AckCode)dummyKeepMemory();
  return Ack_NoError;
}


// **************************************************************
// verifies all block's CRC
// returns 'true' on success, 'false' on CRC error
// **************************************************************
static int bTestBootCRC()
{
  const unsigned char* curBlock;
  const unsigned short* crcBlock = (const unsigned short*)BOOT_CRC_BLOCK;

  for (curBlock =  (const unsigned char*)BOOT_COPY_BLOCK;
       curBlock < (const unsigned char*)BOOT_MEMORY_TOP;
       curBlock +=BOOT_CRC_BLOCK_SIZE, crcBlock++)
  {
    if (curBlock == (const unsigned char*)BOOT_CRC_BLOCK) {
      // skip the CRC block
      continue;
    }
#ifdef BOOT_LOADER
    if (curBlock >= (const unsigned char*)BOOT_LOADER_BASE && 
        curBlock < (const unsigned char*)BOOT_PERSISTENT_TOP) {
      // skip boot loader, as well as persistent area.
      continue;
    }
#else
    if (curBlock >= (const unsigned char*)BOOT_LOADER_TOP){
      break;
    }
#endif
   

    unsigned short crcCalc = CalcCRC((uint8_t*)curBlock, BOOT_CRC_BLOCK_SIZE);
    if (*crcBlock != crcCalc) {
      return FALSE;
    }
  }
  
  return TRUE;
}
AckCode readMemCmd(DataBuffer* db)
{
  int i;

  //todo chack comand minimum length
  
  tel->ackTelTmp = AckTel_Long;
  setTel(tel, OPCODE_READ_MEMORY_TEL, OPCODE_READ_MEMORY_TEL, 0);
  
  for (i=0; i<TEL_REQUEST_PARAM_LENGTH ; i++)
  {
    if (getRemainingLength(db) <= 0)
      break;
    tel->param[i] = getByte(db);
  }
  tel->paramLength = i;
  
  
  return Ack_NoError;
}

// **************************************************************
// stops clock interrupts for update procedure
// we copy the nvic table anyway just before clearing boot sector.
// **************************************************************
static void stopAllInterrupts()
{

  __disable_irq(); //global interrupts disable

  
  
  //TODO: why is is requiered to copy the interrupt vector?
  /*
  // Copy interrupt vector to memory.
  unsigned long nvicBase = (unsigned long)mIntVec;
  // align 'intVec' to the nearest 0x80
  nvicBase += 0x7F;
  nvicBase &= 0xFFFFFF80;
  memcpy ((char*)(nvicBase), (char*)BOOT_BLOCK_BASE, 0x200);
  SCB->VTOR = nvicBase;
  */
}


// **************************************************************
// does actual block swapping
// **************************************************************
static int swapBootBlock()
{
  flashUnlock();

  int i;
  for (i=0; i<10; i++) {  //TODO: why repeat 10 times?
    // clear boot block
    if (FLASH_ErasePage(BOOT_BLOCK_BASE) != FLASH_COMPLETE) {
      continue;
    }

    unsigned long targetAddr;
    unsigned long srcAddr;
    for (targetAddr = BOOT_BLOCK_BASE, srcAddr = BOOT_COPY_BLOCK; 
         targetAddr < (BOOT_BLOCK_BASE + BOOT_BLOCK_SIZE);
         targetAddr += 4, srcAddr += 4) {
      long thisWord = *(long*)(srcAddr);
      FLASH_ProgramWord(targetAddr, thisWord);
    }

    if (memcmp((void*)BOOT_BLOCK_BASE, (void*)BOOT_COPY_BLOCK, BOOT_BLOCK_SIZE) == 0) {
      break;
    }
  }
  
  flashLock();
  
  return TRUE;
}
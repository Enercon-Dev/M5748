

#include "stm32f1xx_hal.h"
#include "general.h"
#ifdef DEBUG
//compile this file only in DEBUG mode

//#include "DataBuffer.h"
//#include "IntUart.h"
#include <stdio.h>
#include <stdarg.h>
#include "DBG.h"


/*
#define DBG_BUFFER_LENGTH (1024*5)
static uint8_t dbg_buffer[DBG_BUFFER_LENGTH];
static DataBuffer db = {dbg_buffer, DBG_BUFFER_LENGTH, 0};
int bDelayPrint = FALSE;

uint16_t dbgArray[DBG_ARRAY_LENGTH] = {0};
int dbgArrayIndex = 0;


void dbgArrayPush(uint16_t data)
{
  dbgArrayIndex++;
  
  if (dbgArrayIndex >= DBG_ARRAY_LENGTH)
    dbgArrayIndex = 0;
  
  dbgArray[dbgArrayIndex] = data;
}

void fillDbgArray(DataBuffer* db)
{
  int i;
  
  for(i=dbgArrayIndex+1; i!=dbgArrayIndex; i++)
  {
    if (i >= DBG_ARRAY_LENGTH)
    i = 0;
    
    fillShort(db, dbgArray[i]);
  }
}


void print(char * fmt, ...)
{
  va_list argptr;  
  va_start(argptr,fmt); 
  db.offset += vsprintf((char*)(db.buffer+db.offset), fmt, argptr);
  va_end(argptr);
  if (!bDelayPrint)
    flush();
}

void delayPrint(int bDelay)
{
  bDelayPrint = bDelay;
  if (!bDelay)
  {
    flush();
  }
}

void flush()
{
  if (db.offset <= 0)
    return;
  
  while (uart_bIsTransmitting(3)); //wait for previos transmition to end
  uart_Send(3, db.buffer, db.offset);
  while (uart_bIsTransmitting(3)); //wait for transmition to end
  
  db.offset = 0;
}

*/

#endif //DEBUG


#include "general.h"
#include "DataBuffer.h"
#include <string.h>

int getRemainingLength(DataBuffer* db)
{
  return (db->length - db->offset);
}

int getPosition(DataBuffer* db)
{
  return (db->offset);
}

void setPosition(DataBuffer* db, int position)
{
  if (position < 0)
    return;

  if (position >= db->length)
  {
    db->offset = db->length;
  }
  else
  {
    db->offset = position;
  }
}

void fillByte(DataBuffer* db, uint8_t data)
{
  if (getRemainingLength(db) < 1)
    return;

  db->buffer[db->offset] = data;
  db->offset++;
}

void fillShort(DataBuffer* db, uint16_t data)
{
  if (getRemainingLength(db) < 2)
    return;

  db->buffer[db->offset] = (data>>8) & 0xFF;
  db->buffer[db->offset+1] = data & 0xFF;
  db->offset += 2;
}

void fillShort_LE(DataBuffer* db, uint16_t data)
{
  if (getRemainingLength(db) < 2)
    return;

  db->buffer[db->offset] = data & 0xFF;
  db->buffer[db->offset+1] =(data>>8) & 0xFF;
  db->offset += 2;  
}

void fillLong(DataBuffer* db, uint32_t data)
{
  if (getRemainingLength(db) < 4)
    return;

  db->buffer[db->offset] = (data>>24) & 0xFF;
  db->buffer[db->offset+1] = (data>>16) & 0xFF;
  db->buffer[db->offset+2] = (data>>8) & 0xFF;
  db->buffer[db->offset+3] = data & 0xFF;
  db->offset += 4;
}

void fillLong_LE(DataBuffer* db, uint32_t data)
{
  if (getRemainingLength(db) < 4)
    return;

  db->buffer[db->offset] = data & 0xFF;
  db->buffer[db->offset+1] = (data>>8) & 0xFF;
  db->buffer[db->offset+2] = (data>>16) & 0xFF;
  db->buffer[db->offset+3] = (data>>24) & 0xFF;
  db->offset += 4;
}

void fillLongAsShort(DataBuffer* db, uint32_t data)
{
  if (data > 0xFFFF)
    fillShort(db, 0xFFFF);
  else
    fillShort(db, data);
}

int16_t int32ToInt16(int32_t data)
{
  if (data > 0x7FFF)
    return 0x7FFF;
  else if (data < -0x7FFE)
    return -0x7FFE;
  else
    return data;
}

void fillArray(DataBuffer* db, uint8_t* data, int length)
{
  if (getRemainingLength(db) < length)
    return;
  
  memcpy(db->buffer + db->offset, data, length);
  db->offset += length;
}

void fillStringFixed(DataBuffer* db, uint8_t* data, int length)
{
  int i;
  
  if (getRemainingLength(db) < length)
    return;
  
  for (i=0; i<length; i++)
  {
    if (data[i] == 0)
      break;
    db->buffer[db->offset+i] = data[i];
  }
  for(; i<length; i++)
  {
    db->buffer[db->offset+i] = 0;
  }
  db->offset += length;
}

void fillString(DataBuffer* db, uint8_t* data, int length)
{
  int i;
  
  if (getRemainingLength(db) < length || length==0)
    return;
  
  for (i=0; i<length; i++)
  {
    db->buffer[db->offset+i] = data[i];
    if (data[i] == 0)
    {
      i++;
      break;
    }
  }
  
  db->offset += i;
}


uint8_t getByte(DataBuffer* db)
{
  if (getRemainingLength(db) < 1)
    return 0;

  uint8_t data = db->buffer[db->offset];
  db->offset++;
  return data;
}

uint16_t getShort(DataBuffer* db)
{
  if (getRemainingLength(db) < 2)
    return 0;

  uint16_t data = ((uint16_t)(db->buffer[db->offset])<<8) |
                  ((uint16_t)(db->buffer[db->offset+1]));
  db->offset += 2;
  return data;
}

uint32_t getLong(DataBuffer* db)
{
  if (getRemainingLength(db) < 4)
    return 0;

  uint32_t data = ((uint16_t)(db->buffer[db->offset])<<24) + 
                  ((uint16_t)(db->buffer[db->offset+1])<<16) + 
                  ((uint16_t)(db->buffer[db->offset+2])<<8) + 
                  ((uint16_t)(db->buffer[db->offset+3]));
  db->offset += 4;
  return data;
}

void getArray(DataBuffer* db, uint8_t* data, int length)
{
  if (getRemainingLength(db) < length)
    return;
  
  memcpy(data, db->buffer + db->offset, length);
  db->offset += length;
}

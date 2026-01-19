#ifndef _DATABUFFER_H_
#define _DATABUFFER_H_

typedef struct DataBuffer
{
  uint8_t* buffer;
  int length;
  int offset;
}DataBuffer;

int getRemainingLength(DataBuffer* DB);
int getPosition(DataBuffer* db);
void setPosition(DataBuffer* db, int position);

void fillByte(DataBuffer* DB, uint8_t data);
void fillShort(DataBuffer* DB, uint16_t data);
void fillShort_LE(DataBuffer* db, uint16_t data);
void fillLong(DataBuffer* DB, uint32_t data);
void fillLong_LE(DataBuffer* db, uint32_t data);
void fillLongAsShort(DataBuffer* db, uint32_t data);
int16_t int32ToInt16(int32_t data);
void fillArray(DataBuffer* db, uint8_t* data, int length);
void fillStringFixed(DataBuffer* db, uint8_t* data, int length);
void fillString(DataBuffer* db, uint8_t* data, int length);

uint8_t getByte(DataBuffer* DB);
uint16_t getShort(DataBuffer* DB);
uint32_t getLong(DataBuffer* DB);


#endif //_DATABUFFER_H_
#include "stm32f1xx_hal.h"
#include "general.h"
#include "main.h"
#include "MacEeprom.h"

#define MAC_CLK_WRITE(state) HAL_GPIO_WritePin(MAC_SCL_GPIO_Port, MAC_SCL_Pin, (state))
#define MAC_CLK_SET MAC_CLK_WRITE(GPIO_PIN_SET)
#define MAC_CLK_CLR MAC_CLK_WRITE(GPIO_PIN_RESET)

#define MAC_DATA_WRITE(state) HAL_GPIO_WritePin(MAC_SDA_GPIO_Port, MAC_SDA_Pin, (state))
#define MAC_DATA_SET MAC_DATA_WRITE(GPIO_PIN_SET)
#define MAC_DATA_CLR MAC_DATA_WRITE(GPIO_PIN_RESET)

#define MAC_DATA_GET  (HAL_GPIO_ReadPin(MAC_SDA_GPIO_Port, MAC_SDA_Pin) != GPIO_PIN_RESET)

//#define MAC_DELAY {wait(2);}
#define MAC_DELAY shortDelay(6);
#define MAC_SHORT_DELAY shortDelay(2);

#define E2_PAGE_SIZE 8
//EEPROM page size is 8 bytes
//EEPROM address range is 00h-7Fh (128 bytes, 16 pages)
//EEPROM MAP:
// Address  Size  Page    Content
//   00h    4+4     0    Serial number (two copies)
//   08h     1      1    Active Page Flag
//   10h    30h    2-7   Page A
//   40h    30h    8-13  Page B

#define SN_ADDRESS         0x00
#define PAGE_FLAG_ADDRESS  0x08
#define PAGE_A_ADDRESS     0x10
#define PAGE_B_ADDRESS     0x40
#define MAX_PAGE_SIZE      0x30


static void start()
{
  MAC_DATA_SET;
  MAC_DELAY;
  MAC_CLK_SET;
  MAC_DELAY;
  
  MAC_DATA_CLR;
  MAC_DELAY;
  MAC_CLK_CLR;
  MAC_DELAY;
}


static void stop()
{
  MAC_DATA_CLR;
  MAC_DELAY;
  MAC_CLK_SET;
  MAC_DELAY;
  MAC_DATA_SET;
  MAC_DELAY;
}

static int getAck()
{
  int result;
  
  MAC_DATA_SET;
  MAC_DELAY;
  MAC_CLK_SET;
  MAC_DELAY;
  if (MAC_DATA_GET)
    result = -1; //no ack
  else
    result = 0; //ack recived
  MAC_CLK_CLR;
  MAC_DELAY;
  return result;
}


static void generateAck(int bAck)
{
  if (bAck)
  {
    MAC_DATA_CLR;
  }
  else
  {
    MAC_DATA_SET;
  }
  
  MAC_DELAY;
  MAC_CLK_SET;
  MAC_DELAY;
  MAC_CLK_CLR;
  MAC_DELAY;
}


static int writeByte(uint8_t data)
{
  int i;
  
  //write the data
  for (i=0; i < 8; i++)
  {
    if (data & 0x80)
    {
      MAC_DATA_SET;
    }
    else
    {
      MAC_DATA_CLR;
    }
    
    MAC_DELAY;
    MAC_CLK_SET;
    MAC_DELAY;
    MAC_CLK_CLR;
    MAC_SHORT_DELAY; //MAC_DELAY; //optional ???
    
    data = data<<1;
  }
  
  return getAck();
}


static uint8_t readByte()
{
  int i;
  uint8_t data = 0;
  
  MAC_DATA_SET;
  MAC_DELAY;
  
  for (i=0; i < 8; i++)
  {
    MAC_CLK_SET;
    MAC_DELAY;

    data = data << 1;
    if (MAC_DATA_GET)
    {
      data |= 1;
    }
   
    
    MAC_CLK_CLR;
    MAC_DELAY;
  }
  
  return data;
}


static void sync()
{
  int i;
  
  for (i=0; i<9; i++)
  {
    start();
  }
  stop();
}


static int readBuffer(uint8_t address, int length, uint8_t* buffer)
{
  int i;

  if (length <= 0) {
    return -4;
  }

  start();

  if (writeByte(0xA0) < 0)
    return -1;

  if (writeByte(address) < 0)
    return -2;

  start();

  if (writeByte(0xA1) < 0)
    return -3;
  
  for (i=0; i < length-1; i++)
  {
    buffer[i] = readByte();
    generateAck(1);
  }
  
  buffer[length-1] = readByte();
  generateAck(0);
  stop();
  return 0;
}


static int writeBuffer(uint8_t address, int length, uint8_t* buffer)
{
  int i;

  if ((length <= 0) || (length > E2_PAGE_SIZE)) {
    return -4;
  }
  
  start();

  if (writeByte(0xA0) < 0)
    return -1;

  if (writeByte(address) < 0)
    return -2;
  
  for (i=0; i < length; i++)
  {
    if (writeByte(buffer[i]) < 0)
    {
      stop();
      return -3;
    }
  }
  
  stop();
  return 0;
}


static void waitForWriteComplition()
{
  //the maximum 24AA02E48 EEPROM write time is 5mS
  shortDelay(3000); //3mS delay
  shortDelay(3000); //3mS delay
}

#define MAC_ADDRESS_OFFSET 0xFA
int getMacAddress(uint8_t* buffer)
{
  sync();
  
  return readBuffer(MAC_ADDRESS_OFFSET, 6, buffer);
}


uint32_t getSerialNumber()
{
  uint8_t buffer[8];
  
  sync();
  
  if (readBuffer(SN_ADDRESS, 8, buffer) != 0)
    return 0;
  
  if ((buffer[0] != buffer[4]) || (buffer[1] != buffer[5]) || (buffer[2] != buffer[6]) || (buffer[3] != buffer[7]))
    return 0;
  
  //Serial Number encoded MSB first
  return (buffer[0]<<24) | (buffer[1]<<16) | (buffer[2]<<8) | buffer[3];
}


int writeSerialNumber(uint32_t SN)
{
  uint8_t buffer[8];
  
  buffer[0] = (SN>>24) & 0xFF;
  buffer[1] = (SN>>16) & 0xFF;
  buffer[2] = (SN>>8)  & 0xFF;
  buffer[3] =  SN      & 0xFF;
  buffer[4] = buffer[0];
  buffer[5] = buffer[1];
  buffer[6] = buffer[2];
  buffer[7] = buffer[3];
  
  int result = writeBuffer(SN_ADDRESS, 8, buffer);
  if (result == 0) //write succeeded
    waitForWriteComplition();
  
  return result;
}


int getPersistantData(uint8_t *buffer, int length)
{
  uint8_t pageFlag;
  
  if ((length < 0) || (length > MAX_PAGE_SIZE))
    return -1;
  
  
  int result = readBuffer(PAGE_FLAG_ADDRESS, 1, &pageFlag);
  if (result < 0)
    return result;
  
  uint8_t pageAddress = (pageFlag == 0) ? PAGE_A_ADDRESS : PAGE_B_ADDRESS;
  
  return readBuffer(pageAddress, length, buffer); 
}


int writePersistantData(uint8_t *buffer, int length)
{
  uint8_t pageFlag;
  
  if ((length < 0) || (length > MAX_PAGE_SIZE))
    return -1;
  
  int result = readBuffer(PAGE_FLAG_ADDRESS, 1, &pageFlag);
  if (result < 0)
    return result;
  
  uint8_t pageAddress = (pageFlag == 0) ? PAGE_B_ADDRESS : PAGE_A_ADDRESS;
  int byteCounter;
    
  //write the data to the currently unused page
  for (byteCounter=length; byteCounter > 0; byteCounter-=E2_PAGE_SIZE)
  {
    int writeSize = (byteCounter > E2_PAGE_SIZE) ? E2_PAGE_SIZE : byteCounter;
    result = writeBuffer(pageAddress, writeSize, buffer);
    if (result < 0) //write failed
      return result;
    waitForWriteComplition();
    pageAddress += E2_PAGE_SIZE;
    buffer += E2_PAGE_SIZE;
  }
  
  //TODO: add write verification
  
  //change the page flag
  pageFlag = (pageFlag == 0) ? 1 : 0;
  result = writeBuffer(PAGE_FLAG_ADDRESS, 1, &pageFlag);
  if (result < 0) //write failed
    return result;
  
  return 0;
}

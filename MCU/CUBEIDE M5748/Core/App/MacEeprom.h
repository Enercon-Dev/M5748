#ifndef _MACEEPROM_H_
#define _MACEEPROM_H_


//#define IP_OFFSET          0x00
//#define SUB_NET_OFFSET     0x04
//#define DEF_GATE_OFFSET    0x08
//#define PAGE_DATA_SIZE     12

int getMacAddress(uint8_t* buffer);
uint32_t getSerialNumber();
int writeSerialNumber(uint32_t SN);
int getPersistantData(uint8_t *buffer, int length);
int writePersistantData(uint8_t *buffer, int length);

#endif //_MACEEPROM_H_
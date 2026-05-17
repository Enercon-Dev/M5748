
#ifndef _ROWIO_H_
#define _ROWIO_H_

struct RowInputs
{
  int bEPO;
  int bEN_380VDC;
  int bBattleMode;
  int bInSpare;
};

struct RowOutputs
{
  int bFault;
  int bFault_LED;
  int bISO_FLR;
  int bOUT350V_OK;
  int bOutSpare;
};


struct RowAnInputs
{
  uint32_t PCBtemp;
  uint32_t VCC_IO;
  uint32_t In28V;
  uint32_t TestRef;
};


extern struct RowInputs rowIn;
extern struct RowOutputs rowOut;
extern struct RowAnInputs rowAnIn;


void readDigitalInputs();
void writeDigitalOutputs();
void readAnalogInputs();
uint32_t tempConvert(uint32_t temp);

#endif //_ROWIO_H_

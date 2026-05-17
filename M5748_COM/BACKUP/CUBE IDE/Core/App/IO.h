
#ifndef _IO_H_
#define _IO_H_

//defined in ReportedData.h
//#define CURRENT(x) ((int32_t)((x)*1000))
//#define VOLTAGE(x) ((uint32_t)((x)*100))
#define TEMPERATURE(x) ((uint32_t)(2731 + (x)*10))

struct Inputs
{
  Signal_t sEPO;
  Signal_t sEN_380VDC;
  Signal_t sBattleMode;
  Signal_t sInSpare;
};

extern struct Inputs inputs;
extern struct RowAnInputs anIn;

void debounceDigitalInputs();
void filterAnalogInputs();


#endif //_IO_H_
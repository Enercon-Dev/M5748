
#ifndef _IO_H_
#define _IO_H_

//defined in ReportedData.h
//#define CURRENT(x) ((int32_t)((x)*1000))
//#define VOLTAGE(x) ((uint32_t)((x)*100))
//#define TEMPERATURE(x) ((uint32_t)(2731 + (x)*10))

struct Inputs
{
  //Signal_t sFP_SW_SNS; -this is a fast signal that is not debounced
  Signal_t sIn_ID0;
  Signal_t sIn_ID1;
  Signal_t sEPO;
  Signal_t sDSBL_FB;
};

extern struct Inputs inputs;
extern struct RowAnInputs anIn;



void debounceDigitalInputs();
void filterAnalogInputs();
void initInputs();

#endif //_IO_H_

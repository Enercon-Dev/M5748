#ifndef _DISPLAY_H_
#define _DISPLAY_H_


#define SEGMENTS_NUM 4
typedef enum {Led_Off, Led_Red, Led_Green, Led_Yellow}LedState;

struct DisplayData
{
  LedState RemoteLed;
  LedState DisableLed;
  LedState FailLed;
  LedState OverTempLed;
  
  LedState CVLed;
  LedState PSLed;
  LedState CCLed;
  LedState SetPointLed;
  LedState ActualLed;
  
  char VSeg[SEGMENTS_NUM];
  int bVoltageDecimalPoint;
  char ISeg[SEGMENTS_NUM];
  int bCurrentDecimalPoint;
};

extern struct DisplayData displayData;

void displayUpdate();
char hexToChar(uint8_t hex);
void itoDisplay(uint32_t num, char seg[SEGMENTS_NUM]);

#endif //_DISPLAY_H_
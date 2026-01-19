
#include "general.h"
#include "Display.h"
#include "SPI_Display.h"

#define DISPLAY_BUFFER_SIZE 10
static uint8_t  Display_rxBuffer[DISPLAY_BUFFER_SIZE];

struct DisplayData displayData = {Led_Off};

uint8_t charTo7seg(char ch);
uint8_t ledToBit(LedState led);

static void displayDataToBuffer();

void displayUpdate()
{
  //TODO: chack if the comm is not busy
  displayDataToBuffer();
#ifdef M5480
  spi_Send(&(Display_rxBuffer[8]), 2);
#else
  spi_Send(Display_rxBuffer, DISPLAY_BUFFER_SIZE);
#endif
}


static void displayDataToBuffer()
{
  Display_rxBuffer[0] = charTo7seg(displayData.ISeg[0]); //Current LSB - Bottom row LSB
  Display_rxBuffer[1] = charTo7seg(displayData.ISeg[1]); //
  Display_rxBuffer[2] = charTo7seg(displayData.ISeg[2]); //
  Display_rxBuffer[3] = charTo7seg(displayData.ISeg[3]); //Current MSB
  Display_rxBuffer[1] |= displayData.bCurrentDecimalPoint ? charTo7seg('.') : 0;
  
  Display_rxBuffer[4] = charTo7seg(displayData.VSeg[0]); //Voltage LSB - Top row LSB
  Display_rxBuffer[5] = charTo7seg(displayData.VSeg[1]); //
  Display_rxBuffer[6] = charTo7seg(displayData.VSeg[2]); //
  Display_rxBuffer[7] = charTo7seg(displayData.VSeg[3]); //Voltage MSB
  Display_rxBuffer[5] |= displayData.bVoltageDecimalPoint ? charTo7seg('.') : 0;
  
  Display_rxBuffer[8] = ((displayData.ActualLed   == Led_Off) ? 0 : 0x80) | //Actual LED
                        ((displayData.SetPointLed == Led_Off) ? 0 : 0x40) | //Set Point LED
                        ((displayData.CCLed       == Led_Off) ? 0 : 0x20) | //CC LED
                        ((displayData.PSLed       == Led_Off) ? 0 : 0x08) | //PS LED
                        ((displayData.CVLed       == Led_Off) ? 0 : 0x04);  //CV LED
#ifndef M5480
  Display_rxBuffer[9] = ((displayData.OverTempLed == Led_Off) ? 0 : 0x80) | //Over Temp LED
                        ((displayData.FailLed     == Led_Off) ? 0 : 0x40) | //Fail LED
                        (ledToBit(displayData.DisableLed) << 4) |           //Disable LED
                        (ledToBit(displayData.RemoteLed) << 2);             //Remote LED
#else
  Display_rxBuffer[9] = (ledToBit(displayData.OverTempLed) << 6) |          //Over Temp LED
                        (ledToBit(displayData.FailLed)     << 4) |          //Fail LED
                        (ledToBit(displayData.DisableLed)  << 2) |          //Disable LED
                        (ledToBit(displayData.RemoteLed));                  //Remote LED
#endif
}


//    __ a
// f |  | b
//    -- g
// e |  | c
//    -- d
static uint8_t charTo7seg(char ch)
{
  switch (ch)
  {                      // abc_ defg
  case ' ': return 0x00; // 000_ 0000
  case '0': return 0xEE; // 111_ 1110
  case '1': return 0x60; // 011_ 0000
  case '2': return 0xCD; // 110_ 1101
  case '3': return 0xE9; // 111_ 1001
  case '4': return 0x63; // 011_ 0011
  case '5': return 0xAB; // 101_ 1011
  case '6': return 0xAF; // 101_ 1111
  case '7': return 0xE0; // 111_ 0000
  case '8': return 0xEF; // 111_ 1111
  case '9': return 0xEB; // 111_ 1011
  case 'A': return 0xE7; // 111_ 0111
  case 'C': return 0x8E; // 100_ 1110
  case 'E': return 0x8F; // 100_ 1111
  case 'F': return 0x87; // 100_ 0111
  case 'H': return 0x67; // 011_ 0111
  case 'L': return 0x0E; // 000_ 1110
  case 'P': return 0xC7; // 110_ 0111
  case 'S': return 0xAB; // 101_ 1011
  case 'U': return 0x6E; // 011_ 1110
  case 'd': return 0x6D; // 011_ 1101
  case 'r': return 0x05; // 000_ 0101
  case 't': return 0x0F; // 000_ 1111
  case '-': return 0x01; // 000_ 0001
  case '.': return 0x10; // 0001 0000
  default:  return 0x00;
  }
}

static uint8_t ledToBit(LedState led)
{
// blink mechanisme reduce the intencity of green led when the LED should be Yellow
  // activate if needed 
  
  static uint32_t blink;
  
  blink++;
  if (blink >= 3)
    blink = 0;
    
  if (blink >= 1 && led == Led_Yellow)
    led=Led_Red;
    
  
  switch (led)
  {
  case Led_Off:    return 0x00;
  case Led_Red:    return 0x01;
  case Led_Green:  return 0x02;
  case Led_Yellow: return 0x03;
  default:         return 0x00;
  }
}

char hexToChar(uint8_t hex)
{
  if (hex <= 0x09)
    return '0' + hex;
  else if (hex <= 0x0F)
    return 'A' + hex - 0x0A;
  else
    return 0;
}
  
void itoDisplay(uint32_t num, char seg[SEGMENTS_NUM])
{
  int bEnableChar = FALSE;
  uint32_t val;
  
  if (num > 99990)
  {
    seg[3] = '9';
    seg[2] = '9';
    seg[1] = '9';
    seg[0] = '9';
  }
  else
  {
    
    //MSB
    val = num / 1000;
    num = num % 1000;
    if (val > 0) bEnableChar = TRUE;
    seg[3] = bEnableChar ? val+'0' : ' ';
    
    val = num / 100;
    num = num % 100;
    if (val > 0) bEnableChar = TRUE;
    seg[2] = bEnableChar ? val+'0' : ' ';
    
    val = num /10;
    num = num % 10;
    seg[1] = val+'0';
    
    val = num;
    seg[0] = val+'0';
  }
}


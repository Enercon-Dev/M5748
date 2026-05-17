#ifndef _UART_GENERAL_H_
#define _UART_GENERAL_H_

struct UartStatistics
{
  uint32_t noise;
  uint32_t byteError;
  uint32_t receivedBytes;
  uint32_t sentBytes;
};


struct UartDataLinkStatistics
{
  uint32_t frameErrors;
  uint32_t receivedFrames;
  uint32_t sentFrames;
  uint32_t discardedTxFrames;
  uint32_t discardedRxFrames;
};


//UART_CHANNELS defines the number of UARTS used. in M5477 there is only one USART.
//this usart is connected to two transmitters that are used alternately
#define UART_CHANNELS 1
 
#define FRAME_PREAMBLE 0x96CFE4BE
#define FRAME_PREAMBLE_SIZE 4
#define FRAME_LENGTH_SIZE 1
#define FRAME_MIN_DATA_SIZE 2
//min Data size: Address + OpCode
#define FRAME_MAX_DATA_SIZE 100
//if FRAME_MAX_DATA_SIZE is changed then MAX_PROPRIETARY_A should be changed as well
#define FRAME_CRC_SIZE 1

#define FRAME_HEADER_SIZE (FRAME_PREAMBLE_SIZE + FRAME_LENGTH_SIZE)  //4+1=5
#define FRAME_EMPTY_SIZE (FRAME_HEADER_SIZE + FRAME_CRC_SIZE)       //5+1=6
#define FRAME_MIN_SIZE (FRAME_EMPTY_SIZE + FRAME_MIN_DATA_SIZE)     //6+2=8
#define FRAME_MAX_SIZE (FRAME_EMPTY_SIZE + FRAME_MAX_DATA_SIZE)     //8+100=108




#endif //_UART_GENERAL_H_

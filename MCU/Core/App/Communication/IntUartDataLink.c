
#include "general.h"
#include "IntUartDataLink.h"
#include "IntUart.h"


#define LINE_FREE_TIMEOUT (T_5mSEC*3)
#define TRANSMIT_DELAY T_100mSEC
#define MAX_TRANSMIT_DELAY T_500mSEC


#define TX_BUFFER_SIZE FRAME_MAX_SIZE
#define RX_BUFFER_SIZE FRAME_MAX_SIZE
static uint8_t tx_Buffer[UART_CHANNELS][TX_BUFFER_SIZE];
static DataBuffer txBuffer[UART_CHANNELS] = {0};
static uint8_t rx_Buffer[UART_CHANNELS][RX_BUFFER_SIZE];
static DataBuffer rxBuffer[UART_CHANNELS] = {0};

static uint32_t rxPreamble[UART_CHANNELS] = {0};
static int bDataReadyForTx[UART_CHANNELS] = {FALSE};
//static Signal_t sLineFree[UART_CHANNELS] = {0};
//static uint32_t transmitDelay[UART_CHANNELS] = {0};
static void intUart_ClearReceived();
static struct UartDataLinkStatistics dataLinkStat[UART_CHANNELS] = {0};


//should be called periodically during main loop "Idle" time
void intUart_Handle()
{
  int ch;
  int bDelayEnded;
  bDelayEnded = isSwitchDelayEnded_Handler();

  for (ch=0; ch<UART_CHANNELS; ch++)
  {
   
    if (bDataReadyForTx[ch] && bDelayEnded && !uart_bIsTransmitting(ch))
    {
      //there is a FRAME waiting to be transmitted and the line is free - start transmission
      intUart_ClearReceived();
      uart_Send(ch, txBuffer[ch].buffer, txBuffer[ch].offset);
      bDataReadyForTx[ch] = FALSE;
      dataLinkStat[ch].sentFrames++;
      //note: this is one of two places where transmission may be started, the other is from the intUart_quickSend() function
    }
  }
}


/*
void intUart_Handle()
{
  int ch;
  
  for (ch=0; ch<UART_CHANNELS; ch++)
  {
    if(!uart_bIsTransmitting(ch))
    {
      //disabe transmitter once the transmission ended thus releasing the half duplex comm. line
      //uart_EnableTranceiver(ch, FALSE); //M5477 - done automaticaly at callback function
    }
    else //still transmitting
    {
      if (transmitDelay[ch] < TRANSMIT_DELAY)
        transmitDelay[ch] = TRANSMIT_DELAY; //wait (at least) 50mSec AFTER a command transmission ends for a telemetry replay
    }
  
    if (transmitDelay[ch] > MAX_TRANSMIT_DELAY) //do not allow delay greater then 500msec
      transmitDelay[ch] = MAX_TRANSMIT_DELAY;
    else if (transmitDelay[ch] > 0)
      transmitDelay[ch]--;
  
  
    Debounce(&sLineFree[ch], !uart_bIsRecived(ch),
             LINE_FREE_TIMEOUT,1,LINE_FREE_TIMEOUT, 1,1,1);
  
    if (bDataReadyForTx[ch] && sLineFree[ch].state && transmitDelay[ch] == 0)
    {
      //there is a FRAME waiting to be transmitted and the line is free - start transmission
      uart_EnableTranceiver(ch, TRUE);
      uart_Send(ch, txBuffer[ch].buffer, txBuffer[ch].offset);
      bDataReadyForTx[ch] = FALSE;
      dataLinkStat[ch].sentFrames++;
      //note: this is one of two places where transmission may be started, the other is from the intUart_quickSend() function
    }
  }
}


void intUart_clearTransmitDelay(uint32_t ch)
{
  transmitDelay[ch] = 0;
}
*/

DataBuffer* intUart_getTxBuffer(uint32_t ch)
{
  if (uart_bIsTransmitting(ch))
    return NULL;

  if (bDataReadyForTx[ch])
  {
    //if there were a Command waiting to be transmitted
    return NULL;
//    bDataReadyForTx[ch] = FALSE;
//    dataLinkStat[ch].discardedTxFrames++;
  }
  
  txBuffer[ch].buffer = tx_Buffer[ch];
  txBuffer[ch].length = TX_BUFFER_SIZE;
  txBuffer[ch].offset = 0;
  
  fillLong(&txBuffer[ch], FRAME_PREAMBLE);
  fillByte(&txBuffer[ch], 0); //place holder for Length feild

  return &txBuffer[ch];
}


int intUart_quickSend(uint32_t ch)
{
  uint16_t crc;
  int dataLength;
  
  if ((txBuffer[ch].buffer == NULL) ||
      (txBuffer[ch].offset < (FRAME_MIN_SIZE-FRAME_CRC_SIZE)))
    return -1;
  
  if (uart_bIsTransmitting(ch))
    return -2;
  
  //fill Length feild
  dataLength = txBuffer[ch].offset - FRAME_HEADER_SIZE;
  txBuffer[ch].buffer[4] = dataLength & 0xFF;
  
  //fill CRC
  crc = CalcCRC(txBuffer[ch].buffer, txBuffer[ch].offset);
  fillByte(&txBuffer[ch], crc);
  
//  if (sLineFree[ch].state && transmitDelay[ch] == 0)
  if (isSwitchDelayEnded_Handler())
  {
    //transmittion is allowed - start transmitting
    intUart_ClearReceived();
    //uart_EnableTranceiver(ch, TRUE);  
    uart_Send(ch, txBuffer[ch].buffer, txBuffer[ch].offset);
    dataLinkStat[ch].sentFrames++;
    //note: this is one of two places where transmission may be started, the other is from the intUart_Handle() function
  }
  else
  {
    //transmission is not allowed - mark that there is a frame ready to be transmitted.
    //  it will be sent once the line is ready (by intUart_Handle function)
    bDataReadyForTx[ch] = TRUE;
  }
  return 0;
}

#ifdef DEBUG
void intUart_SendStringBlocking(uint32_t ch, uint8_t *buffer)
{
  int i;

  if (buffer == NULL)
    return;
  
  while (uart_bIsTransmitting(ch)); //wait for previos transmission to end
  
  //copy the string to the tx buffer
  for (i=0; i<TX_BUFFER_SIZE; i++)
  {
    tx_Buffer[ch][i] = buffer[i];
    if (tx_Buffer[ch][i] == 0)
      break;
  }
  intUart_ClearReceived();
  //uart_EnableTranceiver(ch, TRUE);  
  uart_Send(ch, tx_Buffer[ch], i);
  while (uart_bIsTransmitting(ch)); //wait for transmission to end
}
#endif


void intUart_ClearReceived()
{
  rxBuffer[0].offset = 0;
  rxPreamble[0] = 0;
  //todo: should we clear the rx sDMA buffer as well?
}

DataBuffer* intUart_receive(uint32_t ch)
{
  int dataLength = 0;
  uint16_t crc;

  while (TRUE)
  {
    if (rxPreamble[ch] == 0)
    {
      rxBuffer[ch].buffer = rx_Buffer[ch];
      rxBuffer[ch].length = RX_BUFFER_SIZE;
      rxBuffer[ch].offset = 0;   
    }
    
    //scan for preamble
    while (rxPreamble[ch] != FRAME_PREAMBLE)
    {
      if (!uart_bIsRecived(ch)) 
        return NULL;
      rxPreamble[ch] = rxPreamble[ch]<<8 | uart_GetByte(ch);
      
      if (rxPreamble[ch] == FRAME_PREAMBLE)
        fillLong(&rxBuffer[ch], rxPreamble[ch]);
    }
    
    //get minimal number of bytes for frame
    while (rxBuffer[ch].offset < FRAME_MIN_SIZE)
    {
      if (!uart_bIsRecived(ch)) 
        return NULL;
      fillByte(&rxBuffer[ch], uart_GetByte(ch));
    }
    
    //calc frame data length
    dataLength = rxBuffer[ch].buffer[4];
    
    //the dataLength field is iligal - reset reception
    if (dataLength > FRAME_MAX_DATA_SIZE)
    {
      rxPreamble[ch] = 0;
      rxBuffer[ch].offset = 0;
      dataLinkStat[ch].frameErrors++;
      continue;
    }
    
    //get entire frame
    while (rxBuffer[ch].offset < dataLength + FRAME_EMPTY_SIZE)
    {
      if (!uart_bIsRecived(ch)) 
        return NULL;
      fillByte(&rxBuffer[ch], uart_GetByte(ch));
    }
    
    rxPreamble[ch] = 0; //reset next reception
    crc = CalcCRC(rxBuffer[ch].buffer, rxBuffer[ch].offset - FRAME_CRC_SIZE);
    //rxBuffer[ch].offset -= 1;
    //if (crc == getByte(&rxBuffer[ch]))
#warning "return CRC calc"
    if(1)
    {
      rxBuffer[ch].length = rxBuffer[ch].offset - FRAME_CRC_SIZE;
      rxBuffer[ch].offset = FRAME_HEADER_SIZE;
      dataLinkStat[ch].receivedFrames++;
      return &rxBuffer[ch];
    }
    else
    {
      dataLinkStat[ch].frameErrors++;
      continue;
    }
  }
}


struct UartDataLinkStatistics* intUart_GetStatistics(uint32_t ch)
{
  return &dataLinkStat[ch];
}


void intUart_ClearStatistics(uint32_t ch)
{
  int i;
  
  for (i=0; i<sizeof(dataLinkStat[ch]); i++)
    ((char*)(&dataLinkStat[ch]))[i] = 0;
}

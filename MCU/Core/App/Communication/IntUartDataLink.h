
#ifndef _INT_UART_DATA_LINK_H_
#define _INT_UART_DATA_LINK_H_

#include "DataBuffer.h"
#include "UartGeneral.h"


void intUart_Handle();
//void intUart_clearTransmitDelay(uint32_t ch);
DataBuffer* intUart_getTxBuffer(uint32_t ch);
int intUart_quickSend(uint32_t ch);
#ifdef DEBUG
void intUart_SendStringBlocking(uint32_t ch, uint8_t *buffer);
#endif
DataBuffer* intUart_receive(uint32_t ch);

struct UartDataLinkStatistics* intUart_GetStatistics(uint32_t ch);
void intUart_ClearStatistics(uint32_t ch);

#endif //_INT_UART_DATA_LINK_H_


#ifndef _INT_UART_H_
#define _INT_UART_H_

#include "UartGeneral.h"


void uart_EnableTranceiver(uint32_t ch, int bEnable);

void uart_Enable(uint32_t ch);

int uart_bIsTransmitting(uint32_t ch);
void uart_Send(uint32_t ch, uint8_t buffer[], uint32_t byfferSize);

int uart_bIsRecived(uint32_t ch);
int uart_bRecivedBytesNum(uint32_t ch);
uint8_t uart_GetByte(uint32_t ch);

struct UartStatistics* uart_GetStatistics(uint32_t ch);
void uart_ClearStatistics(uint32_t ch);

#endif //_INT_UART_H_

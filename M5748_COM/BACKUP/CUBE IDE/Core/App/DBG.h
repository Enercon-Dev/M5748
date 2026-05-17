#ifndef _DBG_H_
#define _DBG_H_

//#define TP_LED0(led_state) HAL_GPIO_WritePin(TP_LED0_GPIO_Port, TP_LED0_Pin, !(led_state) ? GPIO_PIN_SET : GPIO_PIN_RESET)
//#define TP_LED1(led_state) HAL_GPIO_WritePin(TP_LED1_GPIO_Port, TP_LED1_Pin, !(led_state) ? GPIO_PIN_SET : GPIO_PIN_RESET)

#ifdef DEBUG
/*
#include "DataBuffer.h"

#define DBG_ARRAY_LENGTH 128
extern uint16_t dbgArray[DBG_ARRAY_LENGTH];
extern int dbgArrayIndex;
void dbgArrayPush(uint16_t data);
void fillDbgArray(DataBuffer* db);

void print(char * fmt, ...);
void delayPrint(int bDelay);
void flush();

*/

#else

#define fillDbgArray(db)

#endif //DEBUG
#endif //_DBG_H_
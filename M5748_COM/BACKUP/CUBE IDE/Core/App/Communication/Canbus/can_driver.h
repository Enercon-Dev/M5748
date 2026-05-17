/*
 * can_driver.h
 *
 *  Created on: Apr 27, 2026
 *      Author: yakir.zafrani
 */

#ifndef APP_COMMUNICATION_CANBUS_CAN_DRIVER_H_
#define APP_COMMUNICATION_CANBUS_CAN_DRIVER_H_

extern uint8_t TxData[8];
extern uint8_t CanRxData[FRAME_MAX_DATA_SIZE];
extern uint8_t CanRxDataSize = 0;

void CAN_SendProprietary_A(uint8_t* data, uint16_t length);


#endif /* APP_COMMUNICATION_CANBUS_CAN_DRIVER_H_ */

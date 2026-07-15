/*
 * can_driver.h
 *
 *  Created on: Apr 27, 2026
 *      Author: yakir.zafrani
 */

#ifndef APP_COMMUNICATION_CANBUS_CAN_DRIVER_H_
#define APP_COMMUNICATION_CANBUS_CAN_DRIVER_H_

extern int newCanMsgReceived;

DataBuffer* CanGetFrame();
DataBuffer* can_getTxBuffer();
void CAN_SendProprietary_A();
void CAN_Poll_Task(void);
void CAN_Send_Proprietary_B();




#endif /* APP_COMMUNICATION_CANBUS_CAN_DRIVER_H_ */

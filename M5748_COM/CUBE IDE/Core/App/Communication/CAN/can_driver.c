#include "general.h"
#include "IntUartDataLink.h"
#include "Hardware/Hardware.h"
#include "Open_SAE_J1939/Open_SAE_J1939.h"

extern CAN_HandleTypeDef hcan;
extern struct TelRequest intTelRequest;

CAN_TxHeaderTypeDef TxHeader;
CAN_RxHeaderTypeDef RxHeader;
ENUM_J1939_RX_MSG rxMsgType;
uint32_t mailbox[4];

uint8_t TxData[8];
uint8_t CanRxData[FRAME_MAX_DATA_SIZE];
uint8_t CanRxDataSize = 0;
DataBuffer CanRxDataBuffer;
DataBuffer CanTxDataBuffer;
volatile int newCanMsgReceived = 0;
uint8_t count = 0;
extern J1939 j1939;
extern int TP_Prop_A_MsgReceived;

HAL_StatusTypeDef STM32_PLC_CAN_Transmit(uint8_t TxData[],
		CAN_TxHeaderTypeDef *TxHeader) {
	uint32_t TxMailbox;
	return HAL_CAN_AddTxMessage(&hcan, TxHeader, TxData, &TxMailbox);
}

void STM32_PLC_CAN_Get_ID_Data(uint32_t *ID, uint8_t data[],
		bool *is_new_message) { // TODO: return reference for data size
	CAN_RxHeaderTypeDef RxHeader = { 0 };
	uint8_t RxData[8] = { 0 };
	HAL_StatusTypeDef status = HAL_CAN_GetRxMessage(&hcan, CAN_RX_FIFO0,
			&RxHeader, RxData);
	if (status != HAL_OK)
		return;

	/* Check the length of the data */
	if (RxHeader.DLC == 0) {
		*is_new_message = false;
		return;
	}

	/* Read ID */
	if (RxHeader.IDE == CAN_ID_STD)
		*ID = RxHeader.StdId;
	else
		*ID = RxHeader.ExtId;

	/* Read data */
	memcpy(data, RxData, 8);

	/* Leave the function by saying that the message is new */
	*is_new_message = true;
}

//void HAL_CAN_RxFifo0MsgPendingCallback(CAN_HandleTypeDef *hcan)
//{
//	// can is set to loopback so we should trigger after sending byte
//
//	rxMsgType = Open_SAE_J1939_Listen_For_Messages(&j1939); // TODO: move to main loop
//	if(rxMsgType == RX_MSG_RESP_REQ_PROPRIETARY_A || TP_Prop_A_MsgReceived == 1 )
//	{
//		TP_Prop_A_MsgReceived = 0;
//
//		if(newCanMsgReceived == 0)
//		{
//			uint16_t dataSize = j1939.from_other_ecu_proprietary.proprietary_A.total_bytes;
//                        if(dataSize <= FRAME_MAX_DATA_SIZE && dataSize >= FRAME_MIN_DATA_SIZE)
//			{
//				memcpy(CanRxData,j1939.from_other_ecu_proprietary.proprietary_A.data, dataSize);
//				CanRxDataSize = dataSize;
//				newCanMsgReceived = 1;
//			}
//
//		}
//		// new PROPRIETARY_A message received
//	}
//
//}

void CAN_Poll_Task(void) {
	rxMsgType = Open_SAE_J1939_Listen_For_Messages(&j1939);

	if (rxMsgType == RX_MSG_RESP_REQ_PROPRIETARY_A
			|| TP_Prop_A_MsgReceived == 1) {
		TP_Prop_A_MsgReceived = 0;

		if (newCanMsgReceived == 0) {
			uint16_t dataSize =
					j1939.from_other_ecu_proprietary.proprietary_A.total_bytes;

			if (dataSize <= FRAME_MAX_DATA_SIZE
					&& dataSize >= FRAME_MIN_DATA_SIZE) {
				memcpy(CanRxData,
						j1939.from_other_ecu_proprietary.proprietary_A.data,
						dataSize);

				CanRxDataSize = dataSize;

				newCanMsgReceived = 1;
			}
		}
	}

	if(b250mS_flg)
	{
		// TODO:send prop_B messages here
		//SAE_J1939_Response_Request_Proprietary_B
	}



}
DataBuffer* CanGetFrame() {
	if (!newCanMsgReceived)
		return NULL;

	CanRxDataBuffer.buffer = CanRxData;
	CanRxDataBuffer.length = CanRxDataSize;
	CanRxDataBuffer.offset = 0;
	return &CanRxDataBuffer;
}

DataBuffer* can_getTxBuffer() {
	//if (uart_bIsTransmitting(ch)) //TODO: check if buffer busy --> buffer is never busy YZ
	//  return NULL;

	CanTxDataBuffer.buffer = j1939.this_proprietary.proprietary_A.data;
	CanTxDataBuffer.length = MAX_PROPRIETARY_A;
	CanTxDataBuffer.offset = 0;

	return &CanTxDataBuffer;
}

void CAN_SendProprietary_A() {

	j1939.this_proprietary.proprietary_A.total_bytes = CanTxDataBuffer.offset;
	//TODO: check if transport layer buffer is busy transmitting before sending new data
	SAE_J1939_Response_Request_Proprietary_A(&j1939,
			j1939.from_other_ecu_proprietary.proprietary_A.from_ecu_address);

}


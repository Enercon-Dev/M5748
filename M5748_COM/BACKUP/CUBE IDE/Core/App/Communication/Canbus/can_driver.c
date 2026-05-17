
#include "general.h"
#include "IntUartDataLink.h"
#include "../../../Hardware/Hardware.h"
#include "../../../Open_SAE_J1939/Open_SAE_J1939.h"

extern CAN_HandleTypeDef hcan;
extern  struct TelRequest intTelRequest ;


CAN_TxHeaderTypeDef TxHeader;
CAN_RxHeaderTypeDef RxHeader;
ENUM_J1939_RX_MSG rxMsgType;
uint32_t mailbox[4];

uint8_t TxData[8];
uint8_t CanRxData[FRAME_MAX_DATA_SIZE];
uint8_t CanRxDataSize = 0;
volatile int newCanMsgReceived = 0;
uint8_t count = 0;
extern J1939 j1939 ;
extern int TP_Prop_A_MsgReceived ;


HAL_StatusTypeDef STM32_PLC_CAN_Transmit(uint8_t TxData[], CAN_TxHeaderTypeDef *TxHeader) {
	uint32_t TxMailbox;
	return HAL_CAN_AddTxMessage(&hcan, TxHeader, TxData, &TxMailbox);
}

void STM32_PLC_CAN_Get_ID_Data(uint32_t* ID, uint8_t data[], bool* is_new_message) {
	CAN_RxHeaderTypeDef RxHeader = {0};
	uint8_t RxData[8] = {0};
	HAL_StatusTypeDef status = HAL_CAN_GetRxMessage(&hcan, CAN_RX_FIFO0, &RxHeader, RxData);
	if (status != HAL_OK)
		Error_Handler();

	/* Check the length of the data */
	if(RxHeader.DLC == 0){
		*is_new_message = false;
		return;
	}

	/* Read ID */
	if(RxHeader.IDE == CAN_ID_STD)
		*ID = RxHeader.StdId;
	else
		*ID = RxHeader.ExtId;

	/* Read data */
	memcpy(data, RxData, 8);

	/* Leave the function by saying that the message is new */
	*is_new_message = true;
}

void HAL_CAN_RxFifo0MsgPendingCallback(CAN_HandleTypeDef *hcan)
{
	// can is set to loopback so we should trigger after sending byte

	rxMsgType = Open_SAE_J1939_Listen_For_Messages(&j1939);
	if(rxMsgType == RX_MSG_RESP_REQ_PROPRIETARY_A || TP_Prop_A_MsgReceived == 1 )
	{
		TP_Prop_A_MsgReceived = 0;

		if(newCanMsgReceived == 0)
		{
			if(j1939.from_other_ecu_proprietary.proprietary_A.total_bytes <= FRAME_MAX_DATA_SIZE )
			{
				memcpy(CanRxData,j1939.from_other_ecu_proprietary.proprietary_A.data,j1939.from_other_ecu_proprietary.proprietary_A.total_bytes);
				CanRxDataSize = j1939.from_other_ecu_proprietary.proprietary_A.total_bytes;
				newCanMsgReceived = 1;
			}

		}
		// new PROPRIETARY_A message received
	}

}
void CAN_SendProprietary_A(uint8_t* data, uint16_t length)
{
    uint8_t DA = 0x00; // Broadcast
    uint32_t PGN = 0x00EF00;
    // TODO: check msg size and choose tp layer or regular can transmit
    // 🔹 DATA
    if(length > 8) // use tp layer
    {
        memcpy(j1939.this_ecu_tp_dt.data, data, length);

    // 🔹 CM config
    j1939.this_ecu_tp_cm.control_byte = CONTROL_BYTE_TP_CM_BAM;
    j1939.this_ecu_tp_cm.total_message_size_being_transmitted = length;
    j1939.this_ecu_tp_cm.number_of_packages_being_transmitted = (length + 6) / 7;
    j1939.this_ecu_tp_cm.PGN_of_the_packeted_message = PGN;

    j1939.from_other_ecu_tp_cm.control_byte = CONTROL_BYTE_TP_CM_BAM;
    // 🔹 SEND CM (BAM)
    SAE_J1939_Send_Transport_Protocol_Connection_Management(&j1939, DA);
    HAL_Delay(2);
    // 🔹 SEND DATA
    SAE_J1939_Send_Transport_Protocol_Data_Transfer(&j1939, DA);
    }
    else // send regular CAN message
    {
    	CAN_Send_Message(j1939.ID, data);
    }
}



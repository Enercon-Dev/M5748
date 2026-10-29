
#include "general.h"
#include "IntUartDataLink.h"
#include "Hardware/Hardware.h"
#include "Open_SAE_J1939/Open_SAE_J1939.h"

extern CAN_HandleTypeDef hcan;
extern  struct TelRequest intTelRequest ;


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
extern int TP_Prop_A_MsgReceived ;


HAL_StatusTypeDef STM32_PLC_CAN_Transmit(uint8_t TxData[], CAN_TxHeaderTypeDef *TxHeader) {
	uint32_t TxMailbox;
	return HAL_CAN_AddTxMessage(&hcan, TxHeader, TxData, &TxMailbox);
}

void STM32_PLC_CAN_Get_ID_Data(uint32_t* ID, uint8_t data[], uint8_t *dlc, bool* is_new_message)
{
	CAN_RxHeaderTypeDef RxHeader = {0};
	uint8_t RxData[8] = {0};
	HAL_StatusTypeDef status = HAL_CAN_GetRxMessage(&hcan, CAN_RX_FIFO0, &RxHeader, RxData);
	if (status != HAL_OK) {
		*is_new_message = false;
		return;
    }

	/* Check the length of the data */
	if(RxHeader.DLC == 0){
		*is_new_message = false;
		return;
	}

	/* Read ID */
	*ID  = (RxHeader.IDE == CAN_ID_STD) ? RxHeader.StdId : RxHeader.ExtId;
    *dlc = (uint8_t)RxHeader.DLC;

	/* Read data */
    // Only copy the bytes that actually arrived
	memcpy(data, RxData, *dlc);
    if (*dlc < 8) memset(data + *dlc, 0xFF, 8 - *dlc); //zero (or 0xFF per J1939) the rest for safety

	/* Leave the function by saying that the message is new */
	*is_new_message = true;
}

//void HAL_CAN_RxFifo0MsgPendingCallback(CAN_HandleTypeDef *hcan)
//{
//	// can is set to loopback so we should trigger after sending byte
//
//	rxMsgType = Open_SAE_J1939_Listen_For_Messages(&j1939);
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

	if (rxMsgType == RX_MSG_RESP_REQ_PROPRIETARY_A || TP_Prop_A_MsgReceived == 1) {
		TP_Prop_A_MsgReceived = 0;

		if (newCanMsgReceived == 0) {
			uint16_t dataSize = j1939.from_other_ecu_proprietary.proprietary_A.total_bytes;


			if (dataSize <= FRAME_MAX_DATA_SIZE && dataSize >= FRAME_MIN_DATA_SIZE) {
				memcpy(CanRxData,	j1939.from_other_ecu_proprietary.proprietary_A.data, dataSize);

				CanRxDataSize = dataSize;

				newCanMsgReceived = 1;
			}
		}
	}

}


void SAE_J1939_TP_Timeout_Task(J1939 *j1939) // for j1939 timeouts during tp
{
	if(j1939->tp_rx_busy == 1 || j1939->tp_tx_busy == 1)
	{
		if (j1939->timer > J1939_MAX_TIMEOUT)
			j1939->timer = J1939_MAX_TIMEOUT;
		else if (j1939->timer > 0)
			j1939->timer--;
    
		if(j1939->timer <= 0)
		{
			if (j1939->timeoutType == 5 && j1939->pendingResumeCts)
			{
        // Send the follow-up real CTS now
			  j1939->this_ecu_tp_cm.control_byte = CONTROL_BYTE_TP_CM_CTS;
			  j1939->this_ecu_tp_cm.number_of_packets_to_be_transmitted =j1939->from_other_ecu_tp_cm.max_number_of_packages_to_send;
			  j1939->this_ecu_tp_cm.next_packet_number_transmitted =j1939->from_other_ecu_tp_dt.packets_in_current_window + 1;
			  SAE_J1939_Send_Transport_Protocol_Connection_Management(j1939, j1939->from_other_ecu_tp_dt.from_ecu_address);
			  j1939->pendingResumeCts = 0;
			  j1939->isTimerCounting = 0;
			  j1939->timeoutType = 0;
			  //  restart T2 since we sent a normal CTS and expect DTs again
			  j1939->timer = J1939_T2_mSEC;
			  j1939->isTimerCounting = 1;
			  j1939->timeoutType = 2;
			  return;
			}
		  else
		  {
		    SAE_J1939_Send_TP_Abort(j1939, j1939->from_other_ecu_tp_cm.from_ecu_address,j1939->this_ecu_tp_cm.PGN_of_the_packeted_message, 3); // 3 is a timeout reason code
		    j1939->tp_rx_busy = 0;
		    j1939->tp_tx_busy = 0;
		    j1939->timeoutType = 0;
		    j1939->isTimerCounting = 0;
		    memset(&j1939->from_other_ecu_tp_dt, 0, sizeof(j1939->from_other_ecu_tp_dt));
		    memset(&j1939->from_other_ecu_tp_cm, 0, sizeof(j1939->from_other_ecu_tp_cm));
		  }
		   //memset(&j1939->from_other_ecu_tp_dt, 0, sizeof(j1939->from_other_ecu_tp_dt));
		   // memset(&j1939->from_other_ecu_tp_cm, 0, sizeof(j1939->from_other_ecu_tp_cm));
	  }

	}

}


DataBuffer* CanGetFrame()
{
  if (!newCanMsgReceived)
    return NULL;
  
  CanRxDataBuffer.buffer = CanRxData;
  CanRxDataBuffer.length = CanRxDataSize;
  CanRxDataBuffer.offset = 0;
  return &CanRxDataBuffer;
}


DataBuffer* can_getTxBuffer()
{
  //if (uart_bIsTransmitting(ch)) //TODO: check if buffer busy --> buffer is never busy YZ
  //  return NULL;
  
  if (j1939.tp_tx_busy || j1939.tp_rx_busy)
    return NULL; //The transport layer is busy so if the message is longer then 8 bytes it wont be sent. To prevent this we will not fill the proprietry A buffer as long as the transport layer is busy

  CanTxDataBuffer.buffer = j1939.this_proprietary.proprietary_A.data;
  CanTxDataBuffer.length = MAX_PROPRIETARY_A;
  CanTxDataBuffer.offset = 0;

	return &CanTxDataBuffer;
}


void CAN_SendProprietary_A()
{
	j1939.this_proprietary.proprietary_A.total_bytes = CanTxDataBuffer.offset;
	//TODO: check if transport layer buffer is busy transmitting before sending new data
	SAE_J1939_Response_Request_Proprietary_A(&j1939,
			j1939.from_other_ecu_proprietary.proprietary_A.from_ecu_address);

}


void CAN_SendProprietary_B(uint32_t PGN)
{
  bool is_supported;
  SAE_J1939_Response_Request_Proprietary_B(&j1939, 0xFF, PGN, &is_supported);
}


#include "general.h"
#include "IntUartDataLink.h"
#include "Hardware/Hardware.h"
#include "Open_SAE_J1939/Open_SAE_J1939.h"

#define TP_TIMEOUT_MS 750

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
static void Build_ALPS_Status1(uint8_t* data); // for debug only YZ
static void Build_ALPS_Analog_Status(uint8_t* data); // for debug only YZ



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
void SendPeriodicProp_B()
{
	if(b250mS_flg)
		{
		    b250mS_flg = 0;

		    // ALPS STATUS1
		    Build_ALPS_Status1(j1939.this_proprietary.proprietary_B[0].data);

		    CAN_Send_Proprietary_B(&j1939, PGN_ALPS_STATUS1);


		    // ALPS ANALOG STATUS
		    Build_ALPS_Analog_Status(j1939.this_proprietary.proprietary_B[1].data);

		    CAN_Send_Proprietary_B(&j1939, PGN_ALPS_ANALOG_STATUS);

		}
}
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

}
void SAE_J1939_TP_Timeout_Task(J1939 *j1939) // for j1939 timeouts during tp
{
	uint32_t now = HAL_GetTick();

	    if (j1939->tp_rx_busy &&
	        (now - j1939->tp_rx_t1_timer) > J1939_TP_T1_MS) {
	        SAE_J1939_Send_TP_Abort(j1939, j1939->from_other_ecu_tp_dt.from_ecu_address,
	                                 j1939->from_other_ecu_tp_cm.PGN_of_the_packeted_message, 3); /* 3 = timeout */
	        memset(&j1939->from_other_ecu_tp_dt, 0, sizeof(j1939->from_other_ecu_tp_dt));
	        memset(&j1939->from_other_ecu_tp_cm, 0, sizeof(j1939->from_other_ecu_tp_cm));
	        j1939->tp_rx_busy = 0;
	    }

	    if (j1939->tp_tx_busy &&
	        (now - j1939->tp_tx_t2_timer) > J1939_TP_T2_MS) {
	        SAE_J1939_Send_TP_Abort(j1939, j1939->from_other_ecu_tp_cm.from_ecu_address,
	                                 j1939->this_ecu_tp_cm.PGN_of_the_packeted_message, 3);
	        j1939->tp_tx_busy = 0;
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

void CAN_Send_Proprietary_B(J1939* j1939,   uint32_t PGN) // YZ
{
    struct Proprietary_B * proprietary_B = Get_Proprietary_B_By_PGN( &j1939->this_proprietary,PGN);

    if(proprietary_B == NULL)
        return ;

    uint32_t ID =
        (0x18UL << 24) |
        (PGN << 8) |
        j1939->information_this_ECU.this_ECU_address;

     CAN_Send_Message(ID,proprietary_B->data);
}

static void Build_ALPS_Status1(uint8_t* data)
{
    memset(data, 0xFF, 8);

    data[0] = 0;
    data[3] = 0x00;
    data[5] = 0xFF;
    data[6] = 0xFF;
    data[7] = 0xFF;
}

static void Build_ALPS_Analog_Status(uint8_t* data)
{
    memset(data, 0xFF, 8);

    uint16_t vin     = 1;
    uint16_t vright  = 2;
    uint16_t vleft   = 3;
    uint16_t vmbd    = 4;

    data[0] = vin & 0xFF;
    data[1] = vin >> 8;

    data[2] = vright & 0xFF;
    data[3] = vright >> 8;

    data[4] = vleft & 0xFF;
    data[5] = vleft >> 8;

    data[6] = vmbd & 0xFF;
    data[7] = vmbd >> 8;
}


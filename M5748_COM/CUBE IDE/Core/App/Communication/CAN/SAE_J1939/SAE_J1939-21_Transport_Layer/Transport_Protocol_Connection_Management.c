/*
 * Transport_Protocol_Connection_Management.c
 *
 *  Created on: 14 juli 2021
 *      Author: Daniel Mårtensson
 */

#include "Transport_Layer.h"
#include "Timing.h"

/*
 * Store information about sequence data packages from other ECU who are going to send to this ECU
 * PGN: 0x00EC00 (60416)
 */
void SAE_J1939_Read_Transport_Protocol_Connection_Management(J1939 *j1939, uint8_t SA, uint8_t data[]) {
	/* Read the control byte */
	j1939->from_other_ecu_tp_cm.control_byte = data[0];

	/* PGN */
	j1939->from_other_ecu_tp_cm.PGN_of_the_packeted_message = (data[7] << 16) | (data[6] << 8) | data[5];

	/* Check the control byte */
	switch (data[0]) {
	case CONTROL_BYTE_TP_CM_RTS:
		/* Set the RTS values */
		if(j1939->tp_rx_busy || j1939->tp_tx_busy) // in case another RTS request in received while another sesion is active
		{
		   SAE_J1939_Send_TP_Abort(j1939, j1939->from_other_ecu_tp_cm.from_ecu_address,j1939->this_ecu_tp_cm.PGN_of_the_packeted_message, 1); // 1 is a reason code for an already managed session
			return;
		}
		if(SA == 0xFF) // if source is brodcast, drop the message
			return;


		/* Source address */
			j1939->from_other_ecu_tp_cm.from_ecu_address = SA;
		//j1939->sendCtsHold = 1; // debug - test T4 C#
		j1939->from_other_ecu_tp_cm.total_message_size_being_transmitted = (data[2] << 8) | data[1];
		if(j1939->from_other_ecu_tp_cm.total_message_size_being_transmitted > MAX_TP_DT )
		{
			SAE_J1939_Send_TP_Abort(j1939, j1939->from_other_ecu_tp_cm.from_ecu_address,j1939->this_ecu_tp_cm.PGN_of_the_packeted_message, 9); // 9 is "total message size > 1785 bytes" reason code
		  j1939->isTimerCounting = 0;
			j1939->tp_tx_busy = 0;
		  j1939->tp_rx_busy = 0;
			return;
		}

		j1939->tp_rx_busy = 1;
		j1939->from_other_ecu_tp_cm.number_of_packages_being_transmitted = data[3];
		j1939->from_other_ecu_tp_cm.max_number_of_packages_to_send = data[4]; // YZ


		/* Send CTS */
		j1939->this_ecu_tp_cm.control_byte = CONTROL_BYTE_TP_CM_CTS;
		j1939->this_ecu_tp_cm.number_of_packets_to_be_transmitted = data[4]; // YZ- was 1
		j1939->this_ecu_tp_cm.next_packet_number_transmitted = 1;
		j1939->this_ecu_tp_cm.PGN_of_the_packeted_message = (data[7] << 16) | (data[6] << 8) | data[5];
		j1939->from_other_ecu_tp_dt.packets_in_current_window = 0;
		//HAL_Delay(1250); // debug - test T3 timeout for C#
		SAE_J1939_Send_Transport_Protocol_Connection_Management(j1939, SA);
		j1939->isTimerCounting = 1;
		j1939->timer = J1939_T2_mSEC; // start counting T2
		j1939->timeoutType = 2;
		break;

	case CONTROL_BYTE_TP_CM_CTS:
		if (!j1939->tp_tx_busy)
			return;
		if(j1939->from_other_ecu_tp_cm.from_ecu_address != SA) // Checking if this CTS source is from the same dest we set, OR from BAM address
			return;

		j1939->from_other_ecu_tp_cm.number_of_packets_to_be_transmitted = data[1];
		j1939->from_other_ecu_tp_cm.next_packet_number_transmitted = data[2];

		if(j1939->from_other_ecu_tp_cm.number_of_packets_to_be_transmitted == 0) // received cts hold
		{
			j1939->timer = J1939_T4_mSEC;
			j1939->timeoutType = 4;
			j1939->isTimerCounting = 1;
			break;
		}

		SAE_J1939_Send_Transport_Protocol_Data_Transfer(j1939, SA);
		break;
	case CONTROL_BYTE_TP_CM_BAM:
		//cuurently BAM is not supported
		//j1939->from_other_ecu_tp_cm.total_message_size_being_transmitted = (data[2] << 8) | data[1];
		//j1939->from_other_ecu_tp_cm.number_of_packages_being_transmitted = data[3];
		//SAE_J1939_Send_Transport_Protocol_Data_Transfer(j1939, SA);
		break;

	case CONTROL_BYTE_TP_CM_ABORT:
		if(j1939->from_other_ecu_tp_cm.from_ecu_address != SA) // Checking if this abort source address is from the same dest we set
			return;
	    /* Peer aborted -- tear down both directions and clear busy flags */
	    //memset(&j1939->from_other_ecu_tp_dt, 0, sizeof(j1939->from_other_ecu_tp_dt));
	    //memset(&j1939->from_other_ecu_tp_cm, 0, sizeof(j1939->from_other_ecu_tp_cm));
		  j1939->this_ecu_tp_cm.total_message_size_being_transmitted = 0; //just in case
	    j1939->tp_rx_busy = 0;
	    j1939->tp_tx_busy = 0;
	    j1939->timeoutType = 0;
	    j1939->isTimerCounting = 0;
	    break;
	case CONTROL_BYTE_TP_CM_EndOfMsgACK:
		if (!j1939->tp_tx_busy)
			return;
		if(j1939->from_other_ecu_tp_cm.from_ecu_address != SA) // Checking if this EOM source address is from the same dest we set
			return;
		j1939->from_other_ecu_tp_cm.total_number_of_bytes_received = (data[2] << 8) | data[1];
		j1939->from_other_ecu_tp_cm.total_number_of_packages_received = data[3];
		j1939->this_ecu_tp_cm.total_message_size_being_transmitted = 0; //just in case
		j1939->tp_tx_busy = 0;
		j1939->timeoutType = 0;
		j1939->isTimerCounting = 0;


	}
}

/*
 * Send information to other ECU about how much sequence data packages this ECU is going to send to other ECU
 * PGN: 0x00EC00 (60416)
 */
ENUM_J1939_STATUS_CODES SAE_J1939_Send_Transport_Protocol_Connection_Management(J1939 *j1939, uint8_t DA) {
	uint32_t ID = (0x1CEC << 16) | (DA << 8) | j1939->information_this_ECU.this_ECU_address;
	uint8_t data[8] = { 0 };

	if(DA == 0xFF) // DA of BAM - currently the BAM is not supported
		return STATUS_SEND_ERROR;
	/* Read the control byte */
	data[0] = j1939->this_ecu_tp_cm.control_byte;

	/* Check the control byte */
	switch (data[0]) {
	case CONTROL_BYTE_TP_CM_RTS:
		j1939->from_other_ecu_tp_cm.from_ecu_address = DA;
		data[1] = j1939->this_ecu_tp_cm.total_message_size_being_transmitted;
		data[2] = j1939->this_ecu_tp_cm.total_message_size_being_transmitted >> 8;
		data[3] = j1939->this_ecu_tp_cm.number_of_packages_being_transmitted;
		data[4] = 0x10; 															/* Max number of packages to be transmitted at once */
		j1939->isTimerCounting = 1;
		j1939->timer = J1939_T3_mSEC;
		j1939->timeoutType = 3;
		j1939->tp_tx_busy = 1;
		break;
	case CONTROL_BYTE_TP_CM_CTS:
		j1939->this_ecu_tp_dt.remaining_packages = j1939->this_ecu_tp_cm.number_of_packets_to_be_transmitted; // YZ
		data[1] = j1939->this_ecu_tp_cm.number_of_packets_to_be_transmitted;
		data[2] = j1939->this_ecu_tp_cm.next_packet_number_transmitted;
		data[3] = 0xFF; 															/* Reserved */
		data[4] = 0xFF; 															/* Reserved */
		break;
	case CONTROL_BYTE_TP_CM_BAM:
		data[1] = j1939->this_ecu_tp_cm.total_message_size_being_transmitted;
		data[2] = j1939->this_ecu_tp_cm.total_message_size_being_transmitted >> 8;
		data[3] = j1939->this_ecu_tp_cm.number_of_packages_being_transmitted;
		data[4] = 0xFF; 															/* Reserved */
		break;
	case CONTROL_BYTE_TP_CM_EndOfMsgACK:
		data[1] = j1939->this_ecu_tp_cm.total_number_of_bytes_received;
		data[2] = j1939->this_ecu_tp_cm.total_number_of_bytes_received >> 8;
		data[3] = j1939->this_ecu_tp_cm.total_number_of_packages_received;
		data[4] = 0xFF; 															/* Reserved */
		j1939->tp_rx_busy = 0;
		j1939->isTimerCounting = 0;
	}
	
	/* PGN */
	data[5] = j1939->this_ecu_tp_cm.PGN_of_the_packeted_message;
	data[6] = j1939->this_ecu_tp_cm.PGN_of_the_packeted_message >> 8;
	data[7] = j1939->this_ecu_tp_cm.PGN_of_the_packeted_message >> 16;

	return CAN_Send_Message(ID, data);
}

ENUM_J1939_STATUS_CODES SAE_J1939_Send_TP_Abort(J1939 *j1939, uint8_t DA, uint32_t PGN, uint8_t reason)
{
    uint32_t ID = (0x1CEC << 16) | (DA << 8) | j1939->information_this_ECU.this_ECU_address;
    uint8_t data[8] = { CONTROL_BYTE_TP_CM_ABORT, reason, 0xFF, 0xFF, 0xFF,
                        (uint8_t)PGN, (uint8_t)(PGN >> 8), (uint8_t)(PGN >> 16) };

    //j1939->isTimerCounting = 0;
    //j1939->tp_tx_busy = 0;
    //j1939->tp_rx_busy = 0;
    return CAN_Send_Message(ID, data);
}

#ifndef _INT_COMM_H_
#define _INT_COMM_H_


typedef enum {AckTel_Non = 0, AckTel_Min = 1, AckTel_Shotr = 2, AckTel_Long = 3} AckTel;

typedef enum {Ack_Non = 0, Ack_NoError, Ack_WrongOpCode, Ack_WrongTelOpCode, Ack_WrongLength, 
              Ack_WrongParameter, ACK_GenerlError, Ack_UnkonwnError,
              Ack_PostponeTel = 0x100, Ack_CancelTel} AckCode;

typedef enum {IntUart=0, UserCanBus=1} CommChannel;

#define TEL_REQUEST_PARAM_LENGTH 16
struct TelRequest
{
  int opCode;
  int id;
  int addresses;
  int addressesTmp;
  uint8_t param[TEL_REQUEST_PARAM_LENGTH];
  int paramLength;
  AckTel ackTel;
  AckTel ackTelTmp;
  int delay;
};


#define HOST_ADDRESS 0x0F
#define MASTER_ADDRESS 0x0A
#define CHARGER_ADDRESS 0x0B
#define CONVR_ADDRESS 0x0C
#define CONVL_ADDRESS 0x0D
#define CONV_BROADCAST_ADDRESS 0x0E
#define BATT1_ADDRESS 0x01
#define BATT_ADDRESS(batt) ((batt) + BATT1_ADDRESS-1)
#define ADDRESS_TO_BATT(addr) ((addr) - BATT1_ADDRESS + 1)
#define BATT_BROADCAST_ADDRESS 0x00

#define DEST_ADDRESS_MASK 0x0F
#define DEST_ADDRES_SIZE 4
#define SOURCE_ADDRESS_MASK (~DEST_ADDRESS_MASK)
#define FRAME_ADDRESS(source, dest) (((dest) & DEST_ADDRESS_MASK) | ((source << DEST_ADDRES_SIZE)  & SOURCE_ADDRESS_MASK))
#define SOURCE_ADDRESS(adr) (((adr) & SOURCE_ADDRESS_MASK) >> (8-DEST_ADDRES_SIZE))
#define DEST_ADDRESS(adr) ((adr) & DEST_ADDRESS_MASK)
#define TRANSMIT_ADDRESSES(adr) FRAME_ADDRESS(DEST_ADDRESS(adr), SOURCE_ADDRESS(adr))

//int sendCommand(CommModule module, int opCode);
void Command_Handler();
void Telemetry_Handler();
DataBuffer* intCanbus_receive(uint32_t ch);

#endif //_INT_COMM_H_

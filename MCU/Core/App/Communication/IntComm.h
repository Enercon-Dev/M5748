#ifndef _INT_COMM_H_
#define _INT_COMM_H_


typedef enum {AckTel_Non = 0, AckTel_Min = 1, AckTel_Shotr = 2, AckTel_Long = 3} AckTel;

typedef enum {Ack_Non = 0, Ack_NoError, Ack_WrongOpCode, Ack_WrongTelOpCode, Ack_WrongLength, 
              Ack_WrongParameter, ACK_GenerlError, Ack_UnkonwnError,
              Ack_PostponeTel = 0x100, Ack_CancelTel} AckCode;

typedef enum {MainUart=0} CommChannel;

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
#define MASTER_ADDRESS 0x01
#define CHARGERA_ADDRESS 0x02
#define CHARGERB_ADDRESS 0x03
#define CHARGER_ADDRESS(ch) ((ch) + CHARGERA_ADDRESS)
#define ADDRESS_TO_CHARGER(addr) ((addr) - CHARGERA_ADDRESS)
#define CHARGER_BROADCAST_ADDRESS 0x0E

#define DEST_ADDRESS_MASK 0x0F
#define DEST_ADDRES_SIZE 4
#define SOURCE_ADDRESS_MASK (~DEST_ADDRESS_MASK)
#define FRAME_ADDRESS(source, dest) (((dest) & DEST_ADDRESS_MASK) | ((source << DEST_ADDRES_SIZE)  & SOURCE_ADDRESS_MASK))
#define SOURCE_ADDRESS(adr) (((adr) & SOURCE_ADDRESS_MASK) >> (8-DEST_ADDRES_SIZE))
#define DEST_ADDRESS(adr) ((adr) & DEST_ADDRESS_MASK)
#define TRANSMIT_ADDRESSES(adr) FRAME_ADDRESS(DEST_ADDRESS(adr), SOURCE_ADDRESS(adr))

int sendCommand(CommModule module, int opCode);
void Command_Handler();

#endif //_INT_COMM_H_
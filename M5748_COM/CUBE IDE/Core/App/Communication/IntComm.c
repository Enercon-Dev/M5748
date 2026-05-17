
#include "general.h"
#include "DataBuffer.h"
#include "UartGeneral.h"
//#include "IntUart.h"
#include "IntComm.h"
#include "IntUartDataLink.h"
#include "Command.h"
#include "Mgmt.h"
#include "CAN\can_driver.h"


static struct TelRequest intTelRequest = {0};
static struct TelRequest canTelRequest = {0};
#define UART_BUFFER_SIZE FRAME_MAX_SIZE

DataBuffer* getTxBuffer(uint8_t dest);
int bIsForProcess(CommChannel commCh, uint8_t source, uint8_t dest, uint8_t opCode);
int bIsForRoutOut(CommChannel commCh, uint8_t source, uint8_t dest);
void RoutFrame(DataBuffer* rxData, CommChannel commCh, struct TelRequest *telRequest);
void procesIncomingFrame(DataBuffer *rxData, uint8_t addresses, struct TelRequest* telRequest);
void quickSend(uint8_t dest);
void processTelemetryRequest(struct TelRequest* telRequest);

static uint8_t routBuffer[UART_BUFFER_SIZE];
static DataBuffer routBufferDB = {0};
static int routBufferValid = FALSE;
static uint8_t routDest = 0;

//this function should be periodicly called from Main
void Command_Handler()
{
  RoutFrame(intUart_receive(IntUart), IntUart, &intTelRequest);
  
  if (newCanMsgReceived)
  {
    RoutFrame(CanGetFrame(), UserCanBus, &canTelRequest);
    newCanMsgReceived = FALSE;
  }
  
  return;
  //TODO: routing mechanizm should be inserted here from M5521 Code
  
  DataBuffer* rxData;
  
  rxData = intUart_receive(IntUart);
  
  if (rxData == NULL)
    return;
  
  decodeCommand(rxData, &intTelRequest);
  
  if (intTelRequest.ackTelTmp != AckTel_Non)
  {
    intTelRequest.ackTel = intTelRequest.ackTelTmp;
    //TODO: verify tat the sourse addres is CHARGERA or CHARGERB only (in order to prevent infinite circular telemetry transmition - back and forth between Charger and Master)
    //intTelRequest.addresses = TRANSMIT_ADDRESSES(addresses);//TODO: change so telRequest->addresses holds the not inverted address
  }
}


int bIsForProcess(CommChannel commCh, uint8_t source, uint8_t dest, uint8_t opCode)
{
  if (commCh == UserCanBus)
  {
    if (source != HOST_ADDRESS || dest != CHARGER_ADDRESS)
      return FALSE;
  }
  else if (commCh == IntUart)
  {
    if (opCode == OPCODE_SOFTWARE_VERSION_TEL)
      return TRUE; //process Software version telemetries regardless of the destination
    if (dest != CHARGER_ADDRESS)
      return FALSE;
  }
  else
    return FALSE;  //BUG - wrong commCh parameter value
  
  return TRUE;
}


int bIsForRoutOut(CommChannel commCh, uint8_t source, uint8_t dest)
{
  if (commCh == UserCanBus)
  {
    if (source != HOST_ADDRESS)
      return FALSE;
    if (dest == CHARGER_ADDRESS)
      return FALSE;
  }
  else if (commCh == IntUart)
  {
    if (dest != HOST_ADDRESS)
      return FALSE;
  }
  else
    return FALSE;
  
  return TRUE;
}

void RoutFrame(DataBuffer* rxData, CommChannel commCh, struct TelRequest *telRequest)
{
  uint8_t addresses,source,dest,opCode;
  
  if (rxData == NULL)
    return;
  
  //read addresses byte and split to source and dest:
  addresses = getByte(rxData);
  source = SOURCE_ADDRESS(addresses);
  dest = DEST_ADDRESS(addresses);
  opCode = getByte(rxData);
  rxData->offset--; //get the OpCode without "removing" it from the buffer
  
  //this code for master only:
//  if (commCh == ChargerAUart && dest == MASTER_ADDRESS) //frame received that is not from self
//    intUart_clearTransmitDelay(ChargerAUart); //it probably be the answer for prev command - next command can be transmited
//  
//  if (commCh == ChargerBUart && dest == MASTER_ADDRESS) //frame received that is not from self
//    intUart_clearTransmitDelay(ChargerBUart); //it probably be the answer for prev command - next command can be transmited
  
  
  //the COM MCU processes and collects data from all intUart communication except from frames intended for the user.
  if (bIsForRoutOut(commCh, source,dest))
  {
    if (dest == HOST_ADDRESS)
    {
      //TODO: send the frame to CAN
    }
    else
    {
      //rout out the frame - copy it to the routBufferDB.
      //  the frame will be transmitted by the Telemetry_Handler() function
      routBufferDB.buffer = routBuffer;
      routBufferDB.length = UART_BUFFER_SIZE;
      routBufferDB.offset = 0;
    
      //the routBufferDB holds the data without the preamble, length and CRC
      fillByte(&routBufferDB, addresses);
      fillArray(&routBufferDB, rxData->buffer + rxData->offset, rxData->length - rxData->offset);
      routDest = dest;
      routBufferValid = TRUE;
    }
  }
  else if (bIsForProcess(commCh, source,dest, opCode))
  {
    //Procces the frame (decode and execute) but only if the address are OK
    procesIncomingFrame(rxData, addresses, telRequest);
  }
  else
  {
    //do nothing - drop the frame
  }
}


void procesIncomingFrame(DataBuffer *rxData, uint8_t addresses, struct TelRequest* telRequest)
{
  AckCode result;
  uint8_t id;
  
  //if the decoding function prepars a telemetry it will change the ackTelTmp
  telRequest->ackTelTmp = AckTel_Non; 
  //TODO: this mechanism is probably not needed as the decoding function can directly set the ackTel and rise a flag to
  // inform the caller that a telemetry request was set
 
  //next line added in M5521. The idia is to store the addres in telRequest before decoding the command so the
  // decode function would be able do know which addres the command is sent from
  telRequest->addressesTmp = addresses;
  
  // decode and execute the command
  result = decodeCommand(rxData, telRequest);

  if (result == Ack_Non) //jast in case, shuld never happen
    result = Ack_NoError; 

  //execution error handeling
  if (result == Ack_CancelTel)
  {
    //do not send any response
    return; 
  }
  if ((result != Ack_NoError))
  {
    if (SOURCE_ADDRESS(addresses) == HOST_ADDRESS) //send ACK/NACK only to the host
    {
      //SetAckTelRequest(result, telRequest); //prepare ACK/NACK tel request
      //M5748: we may need this option to send CAN BUS ACK/NACK 
    }
  }
  else
  {
    if (telRequest->ackTelTmp == AckTel_Non)
    {
      //send ACK even if host did not requested it
      //TODO: change so ACK be sent only for USER request (change Boot Loader C# software to request ACK)
      if (SOURCE_ADDRESS(addresses) == HOST_ADDRESS) //send ACK/NACK only to the host
      {
        //SetAckTelRequest(Ack_NoError, telRequest); //prepare ACK tel request
      }
    }
  }

  if (telRequest->ackTelTmp != AckTel_Non)
  {
    telRequest->ackTel = telRequest->ackTelTmp;
    //TODO: verify tat the sourse addres is CHARGERA or CHARGERB only (in order to prevent infinite circular telemetry transmition - back and forth between Charger and Master)
    telRequest->addresses = TRANSMIT_ADDRESSES(addresses);//TODO: change so telRequest->addresses holds the not inverted address
  }
  //TODO: verify that user receives ACK/NACK
  //  and that all oter dosn't receive ack/nack
  //TODO: verify that ACK from user dosnt generate any response
  
}
/*
int sendCommand(CommModule module, int opCode)
{
  DataBuffer *db;
  db = intUart_getTxBuffer(0);
  if (db == NULL)
    return 0;
  
  //if (module != ModuleAll)
  uart_SelectModule(module);
  AckCode result = fillTelemetry(db, opCode);

  intUart_quickSend(0);
  mgmt.lastReceivedTel = 0;
  return 1;
}*/

void Telemetry_Handler()
{
  processTelemetryRequest(&intTelRequest);

  processTelemetryRequest(&canTelRequest);

  //the routBuffer holdes only frames from CAN to be transmited on internal UART
  //such frame should be sent only immidiatly after communication session so the command and the possible reply
  //be transmitted before the next communication session
  DataBuffer* txBuffer;
  if (routBufferValid && mgmt.bCommRoundCompleted)
  {
    txBuffer = getTxBuffer(routDest);
    if (txBuffer == NULL)
      return;
    fillArray(txBuffer, routBufferDB.buffer, routBufferDB.offset);
    quickSend(routDest);
    
    routBufferValid = FALSE;
    routDest = 0;
  }
}

DataBuffer* getTxBuffer(uint8_t dest)
{
  if (dest == HOST_ADDRESS)
    return can_getTxBuffer();
  else
    return intUart_getTxBuffer(0);
}

void lineIsFree(uint8_t dest)
{
  if (dest != HOST_ADDRESS)
    intUart_LineIsFree(0);
}

void quickSend(uint8_t dest)
{
  if (dest == HOST_ADDRESS)
    CAN_SendProprietary_A(); //TODO: send to CAN
  else
    intUart_quickSend(0);
}


void processTelemetryRequest(struct TelRequest* telRequest)
{
  //int dbPosition;
  DataBuffer *db;
  
  if (telRequest->delay > 0)
  {
    telRequest->delay--;
    return;
  }
  
  if (telRequest->ackTel == AckTel_Non)
    return;

  db = getTxBuffer(DEST_ADDRESS(telRequest->addresses));
  if (db == NULL)
    return;
  
  fillByte(db, telRequest->addresses);
  //dbPosition = getPosition(db);
  
  AckCode result = fillTelemetry(db, telRequest);

  if (result == Ack_PostponeTel)
    return;
  
  if (result == Ack_CancelTel)
  {
    //clear the telRequest
    telRequest->opCode = 0;
    telRequest->ackTel = AckTel_Non;
    return;
  }
    
  if (result == Ack_Non)  //jast in case
    result = Ack_NoError;
  
  if (result != Ack_NoError)
  {
    //M5748 does not implement ACK so in case of error, we clear the tel request 
    //clear the telRequest
    telRequest->opCode = 0;
    telRequest->ackTel = AckTel_Non;
    return;
    
    //prepere NACK tel
    //SetAckTelRequest(result, telRequest);
    //setPosition(db, dbPosition);
    //fillShort(db, OPCODE_ACK);
    //fillAckTel(db, telRequest);
  }
  
  //clear the telRequest
  telRequest->opCode = 0;
  telRequest->ackTel = AckTel_Non;
  
  //sent the telemetry
  quickSend(DEST_ADDRESS(telRequest->addresses));
}


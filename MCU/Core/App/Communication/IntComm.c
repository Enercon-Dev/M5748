
#include "general.h"
#include "DataBuffer.h"
#include "UartGeneral.h"
#include "IntUart.h"
#include "IntComm.h"
#include "IntUartDataLink.h"
#include "Command.h"
#include "Mgmt.h"



#define UART_BUFFER_SIZE FRAME_MAX_SIZE



//this function should be periodicly called from Main
void Command_Handler()
{
  DataBuffer* rxData;
  
  rxData = intUart_receive(MainUart);
  
  if (rxData == NULL)
    return;
  
  decodeCommand(rxData);
}


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
}


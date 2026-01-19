
#include "stm32f1xx_hal.h"
#include "general.h"
#include "IntUart.h"
#include "main.h"

extern UART_HandleTypeDef huart1;

//static USART_TypeDef * USART[UART_CHANNELS] = {USART2, USART2};
//static DMA_Channel_TypeDef * USART_TxDMAChannel[UART_CHANNELS] = {DMA1_Channel4, DMA1_Channel7, DMA1_Channel2};
//static uint32_t USART_TxDMATCFlag[UART_CHANNELS] = {DMA1_FLAG_TC4, DMA1_FLAG_TC7, DMA1_FLAG_TC2};
//static DMA_Channel_TypeDef * USART_RxDMAChannel[UART_CHANNELS] = {DMA1_Channel5, DMA1_Channel6, DMA1_Channel3};

//#define USART           USART1
//#define USART_TxDMAChannel DMA1_Channel4
//#define USART_TxDMATCFlag  DMA1_FLAG_TC4
//#define USART_RxDMAChannel DMA1_Channel5

//static uint16_t USART_TX_PIN[UART_CHANNELS] = {GPIO_Pin_9, GPIO_Pin_5, GPIO_Pin_10};
//static GPIO_TypeDef * USART_TX_PORT[UART_CHANNELS] = {GPIOA, GPIOD, GPIOC};
//#define UART3_EN_PIN GPIO_Pin_9
//#define UART3_EN_Port GPIOC

//#define UART_TxDMAEnabled(ch) (((USART_TxDMAChannel[ch])->CCR & ((uint32_t)0x01)) != 0)

#define UART_TX_A_EN(enable) HAL_GPIO_WritePin(TX_EN_A_GPIO_Port, TX_EN_A_Pin, (enable) ? GPIO_PIN_SET : GPIO_PIN_RESET)


#define UART_RX_BUFFER_SIZE 128
static uint8_t  uartRxBuffer[UART_RX_BUFFER_SIZE+10]; 
static uint32_t rxIndex = 0;
struct UartStatistics uartStat[UART_CHANNELS] = {0};
static CommModule selectedModule = ModuleA;
static int sendBoth = FALSE;
static int moduleSwitchDelayStart = -1;

void uart_SelectModule(CommModule module);
static void uart_EnableTranceiver(uint32_t ch, int bEnable);
void uart_Enable(uint32_t ch);
static void uart_TxComplete_Callback(UART_HandleTypeDef *huart);
void uart_Send(uint32_t ch, uint8_t buffer[], uint32_t byfferSize);
int uart_bIsTransmitting(uint32_t ch);
int uart_bIsRecived(uint32_t ch);
int uart_bRecivedBytesNum(uint32_t ch);
uint8_t uart_GetByte(uint32_t ch);



void uart_SelectModule(CommModule module)
{
  if (module == ModuleAll)
  {
    //prepare for single transmition to both modules
    // the selectedModule is not changed
    sendBoth = TRUE; //SendBoth active for one command only (untill uart_Send() is called)
    uart_EnableTranceiver(0, TRUE);
    moduleSwitchDelayStart = startDelay();
  }
  else if (selectedModule != module)
  {
    selectedModule = module;
    //module was change enable the new channel and set it to receive state;
 
  }
     uart_EnableTranceiver(0, FALSE);
    moduleSwitchDelayStart = startDelay();
}

/*
void uart_SelectBothModules()
{
  sendBoth = TRUE;
  uart_EnableTranceiver(0, TRUE);
  //disable receiver DMA
  //HAL_UART_DMAStop(&huart2);
  //rxIndex = 0;
  
  //Module A transmit enable
  //UART_TX_A_EN(TRUE);
  //UART_RX_A_EN(FALSE);
  
  //Module B transmit enable
  //UART_TX_B_EN(TRUE);
  //UART_RX_B_EN(FALSE);
}*/


//shoudl be caled periodically during main loop "Idle" time
//return TRUE if deley ended FALSE otherwise
int isSwitchDelayEnded_Handler()
{
  if (moduleSwitchDelayStart < 0)
    return TRUE;
  
  if (isDeleyEnded(moduleSwitchDelayStart, 400)) //400uSec delay
  {
    moduleSwitchDelayStart = -1;
    return TRUE;
  }  
  return FALSE;
}

//Transmit when bEnable=1, Receive when bEnable=0
//this function should not be called twice "just in case" as it restards the reciver discarding all the previous received data
//it uses globals "selectedModule" and "sendBoth" 
static void uart_EnableTranceiver(uint32_t ch, int bEnable)
{
  if (bEnable)
  {
    //disable receiver DMA
    HAL_UART_DMAStop(&huart1);
     UART_TX_A_EN(TRUE);

    rxIndex = 0;
  }
  else
  {
    //enable receiver DMA
    UART_TX_A_EN(FALSE);

    HAL_UART_Receive_DMA(&huart1, uartRxBuffer, UART_RX_BUFFER_SIZE);
  }
 
}


/*******************************************************************************
** Name:         uart_Enable
** Description:  Enables UPS uart hardware and interrupts.
**               should be called after RS-232 transceivers is ready
** Parameters:   non
** Return value: non
** Note:         This function is platform depended
*******************************************************************************/
/*
int bErrorFlag=0;
int bAbortFlag = 0;
int bAbortRxFlag = 0;
void uart_error_Callback(UART_HandleTypeDef *huart)
{
  bErrorFlag = 1;
  if ((huart2.Instance->CR3 & 0x0040) == 0)
  {
    TP_LED0(0);
  }
}

void uart_abortComp_Callback(UART_HandleTypeDef *huart)
{
  bAbortFlag = 1;
}

void uart_abortRx_Callback(UART_HandleTypeDef *huart)
{
  bAbortRxFlag = 1;
}*/


//this is an initialization function - called once from main()
void uart_Enable(uint32_t ch)
{
  HAL_UART_RegisterCallback(&huart1, HAL_UART_TX_COMPLETE_CB_ID, uart_TxComplete_Callback);
  
  //HAL_UART_RegisterCallback(&huart2, HAL_UART_ERROR_CB_ID, uart_error_Callback);
  //HAL_UART_RegisterCallback(&huart2, HAL_UART_ABORT_COMPLETE_CB_ID, uart_abortComp_Callback);
  //HAL_UART_RegisterCallback(&huart2, HAL_UART_ABORT_RECEIVE_COMPLETE_CB_ID, uart_abortRx_Callback);
                            
  //no need to enable DMA on sturtup. it will be enabled after first command transmition (when it actually neaded)
  //HAL_UART_Receive_DMA(&huart2, uartRxBuffer, UART_RX_BUFFER_SIZE);
  
  //select module A so the tranciver will be ready when the comm starts
  uart_SelectModule(ModuleA); 
}


static void uart_TxComplete_Callback(UART_HandleTypeDef *huart)
{
  //TODO: verify that this function is called when the data is fully transmitted (and not when TX DMA finished)
  
  //disable transmitters: 
  uart_EnableTranceiver(0, FALSE);
}



/*******************************************************************************
** Name:         uart_Send
** Description:  Start the transmission of the buffer using DMA
** Parameters:   buffer - buffer to be transmitted
**               byfferSize - buffer length
** Return value: non
** Note:         This function is platform depended
*******************************************************************************/
void uart_Send(uint32_t ch, uint8_t buffer[], uint32_t byfferSize)
{
  uart_EnableTranceiver(ch, TRUE);
  HAL_UART_Transmit_DMA(&huart1, buffer, byfferSize);
  sendBoth = FALSE;
  
  uartStat[ch].sentBytes += byfferSize;
}


/*******************************************************************************
** Name:         uart_bTransmitting
** Description:  check if there is a telemetry in transmission
** Parameters:   non
** Return value: TRUE if currently transmitting telem. FALSE if not
** Note:         This function is platform depended
*******************************************************************************/
int uart_bIsTransmitting(uint32_t ch)
{
  return (huart1.gState == HAL_UART_STATE_BUSY_TX);
  //Uasrt's TC flag is set when all the data is sent (after the last stop bit)
  // so it can be used to disable the transmitter
/*  if ((UART_TxDMAEnabled(ch)) && (DMA_GetFlagStatus(USART_TxDMATCFlag[ch]) == RESET))
    return TRUE;
  if (USART_GetFlagStatus(USART[ch], USART_FLAG_TC) == RESET)
    return TRUE;
  else
    return FALSE; */
}



int uart_bIsRecived(uint32_t ch)
{
  int rxPutIndex = UART_RX_BUFFER_SIZE - (huart1.hdmarx->Instance->CNDTR);
  
  if (rxPutIndex >= UART_RX_BUFFER_SIZE)
    return FALSE; //meaning that DMA CNDTR register is zero which is not a legal value for circular mode (may happend if the DAM is reseted or not initialized yet)
  else if (rxIndex == rxPutIndex)
    return FALSE;
  else
    return TRUE;
}


int uart_bRecivedBytesNum(uint32_t ch)
{
  int rxPutIndex = UART_RX_BUFFER_SIZE - (huart1.hdmarx->Instance->CNDTR);
  
  if (rxPutIndex >= UART_RX_BUFFER_SIZE)
    return 0; //meaning that DMA CNDTR register is zero which is not a legal value for circular mode (may happend if the DAM is reseted or not initialized yet)
  else if (rxPutIndex >= rxIndex)
    return rxPutIndex - rxIndex;
  else
    return UART_RX_BUFFER_SIZE + rxPutIndex - rxIndex;
}

uint8_t uart_GetByte(uint32_t ch)
{
  uint8_t data;
  static uint16_t dmaPosition;
  uint16_t prevDmaPosition;
  
  if (!uart_bIsRecived(ch))
    return 0;
  
  data = uartRxBuffer[rxIndex];
  rxIndex++;
  if (rxIndex >= UART_RX_BUFFER_SIZE)
    rxIndex = 0;
  
  //UART statistics update
  uartStat[ch].receivedBytes++;
  prevDmaPosition = dmaPosition;
  dmaPosition = (huart1.hdmarx->Instance->CNDTR);
  if (prevDmaPosition != dmaPosition)
  {
    if(__HAL_UART_GET_FLAG(&huart1, UART_FLAG_NE))
      uartStat[ch].noise++;
    if(__HAL_UART_GET_FLAG(&huart1, UART_FLAG_FE)) // || USART_GetFlagStatus(USART, USART_FLAG_PE) == SET)
      uartStat[ch].byteError++;
  }
  return data;
}


struct UartStatistics* uart_GetStatistics(uint32_t ch)
{
  return &uartStat[ch];
}


void ClearStatistics(uint32_t ch)
{
  int i;
  
  for (i=0; i<sizeof(uartStat[ch]); i++)
    ((char*)(&(uartStat[ch])))[i] = 0;
}

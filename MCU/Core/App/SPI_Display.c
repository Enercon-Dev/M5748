
#include "stm32f1xx_hal.h"
#include "general.h"
#include "main.h"
#include "SPI_Display.h"

extern SPI_HandleTypeDef hspi3;

#define LED_EN_PORT LED_EN_GPIO_Port
#define LED_EN_PIN LED_EN_Pin
#define SPI_PORT LED_STB_GPIO_Port
#define SPI_LE_PIN LED_STB_Pin

static int bBusy=0;


void spi_Send(uint8_t txBuffer[], uint32_t byfferSize);


void spi_Send(uint8_t txBuffer[], uint32_t byfferSize)
{
  //TODO: verify that there is no ongoing communication
  bBusy = TRUE;
  
  HAL_SPI_Transmit_DMA(&hspi3, txBuffer, byfferSize);
}




//should be called continiusly during main loop delay
void spi_generateStrobe()
{
  
  
  if (bBusy && !__HAL_SPI_GET_FLAG(&hspi3, SPI_FLAG_BSY))
  {
    //generate strobe when communication ends;
    shortDelay(25); //25uSec delay to let the last clock puls finish //TODO: find a better way to detect communication end
    HAL_GPIO_WritePin(SPI_PORT, SPI_LE_PIN, GPIO_PIN_SET);
    shortDelay(50); //50uSec reset pulse
    HAL_GPIO_WritePin(SPI_PORT, SPI_LE_PIN, GPIO_PIN_RESET);
    HAL_GPIO_WritePin(LED_EN_PORT, LED_EN_PIN, GPIO_PIN_RESET); //clear LED_EN (only after the leds are written for the first time
    bBusy = FALSE;
    
  }
}
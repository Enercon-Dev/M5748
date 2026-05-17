
#include "stm32f1xx_hal.h"
#include "general.h"
#include "main.h"
#include "RowIO.h"

extern ADC_HandleTypeDef hadc1; //defined in main.c
extern TIM_HandleTypeDef htim1; //defined in main.c
extern TIM_HandleTypeDef htim2; //defined in main.c
extern TIM_HandleTypeDef htim3; //defined in main.c

struct RowInputs rowIn = {0};
struct RowOutputs rowOut = {0};
struct RowAnInputs rowAnIn = {0};

static void convertAnalogInputs();



void readDigitalInputs()
{
  int i;
  int fpCounter,ilCounter;
  
  //map input bits to hardware independent variables
  rowIn.bEPO        = !HAL_GPIO_ReadPin(EPO_IN_GPIO_Port, EPO_IN_Pin);
  rowIn.bEN_380VDC  = HAL_GPIO_ReadPin(EN_350VDC_GPIO_Port, EN_350VDC_Pin);
  rowIn.bBattleMode = HAL_GPIO_ReadPin(BattleMode_GPIO_Port, BattleMode_Pin);
  rowIn.bInSpare    = HAL_GPIO_ReadPin(InSpare_GPIO_Port, InSpare_Pin);
}

  
void writeDigitalOutputs()
{
  HAL_GPIO_WritePin(Fault_GPIO_Port,         Fault_Pin,         rowOut.bFault      ? GPIO_PIN_SET : GPIO_PIN_RESET);
  HAL_GPIO_WritePin(Fault_LED_GPIO_Port,     Fault_LED_Pin,     rowOut.bFault_LED  ? GPIO_PIN_SET : GPIO_PIN_RESET);
  HAL_GPIO_WritePin(ISO_FLR_GPIO_Port,       ISO_FLR_Pin,       rowOut.bISO_FLR    ? GPIO_PIN_SET : GPIO_PIN_RESET);
  HAL_GPIO_WritePin(Output350V_OK_GPIO_Port, Output350V_OK_Pin, rowOut.bOUT350V_OK ? GPIO_PIN_SET : GPIO_PIN_RESET);
  HAL_GPIO_WritePin(OutSpare_GPIO_Port,      OutSpare_Pin,      rowOut.bOutSpare   ? GPIO_PIN_SET : GPIO_PIN_RESET);
}


static uint32_t readA2D(uint32_t a2d_channel)
{
  ADC_ChannelConfTypeDef sConfig = {0};
  
  //ADC Channel Config
  sConfig.Channel = a2d_channel;
  sConfig.Rank = ADC_REGULAR_RANK_1;
  sConfig.SamplingTime = ADC_SAMPLETIME_1CYCLE_5;
  if (HAL_ADC_ConfigChannel(&hadc1, &sConfig) != HAL_OK)
  {
    Error_Handler();
  }
  
  //ADC sampling:
  //it seamse that the hadc1 is defined in main.c 
  HAL_ADC_Start(&hadc1); //start ADC conversions
  HAL_ADC_PollForConversion(&hadc1, 1); //Wait for ADC conversion completion, Timout = 1mSec
  return HAL_ADC_GetValue(&hadc1);
    
  //HAL_ADC_Stop() //is it needed???
  
}


void readAnalogInputs()
{
  rowAnIn.PCBtemp = readA2D(ADC_CHANNEL_0);
  rowAnIn.VCC_IO  = readA2D(ADC_CHANNEL_1);
  rowAnIn.In28V   = readA2D(ADC_CHANNEL_2);
  rowAnIn.TestRef = readA2D(ADC_CHANNEL_3);
  
  convertAnalogInputs();
}


static void convertAnalogInputs()
{
  rowAnIn.PCBtemp = tempConvert(rowAnIn.PCBtemp);
  rowAnIn.VCC_IO  = rowAnIn.VCC_IO  * 69300 / 4096; //1mV resolution
  rowAnIn.In28V   = rowAnIn.In28V   * 69300 / 4096; //1mV resolution
  rowAnIn.TestRef = rowAnIn.TestRef * 3300  / 4096; //1mV resolution
}

//converts ADC data to deg-Kelvin * 10
//min temp -40deg; max temp 100deg
const LinearSegment temp10_table[] = {{259,3731},{333,3641},{419,3561},{593,3441},{816,3331},{1055,3241},
  {1500,3111},{2502,2881},{3206,2711},{3548,2601},{3767,2501},{3899,2411},{3976,2331}};
#define TEMP10_TABLE_SIZE 13

//min temp -40deg; max temp 120deg
const LinearSegment temp5_table[] = {{294,3931},{325,3891},{430,3781},{587,3661},{802,3541},{1092,3421},
{1534,3281},{2926,2931},{3393,2791},{3662,2681},{3832,2581},{3934,2491},{3995,2411},{4035,2331}};
#define TEMP5_TABLE_SIZE 14


uint32_t tempConvert(uint32_t temp)
{
  return linearAproximation(temp, TEMP10_TABLE_SIZE, temp10_table);
}

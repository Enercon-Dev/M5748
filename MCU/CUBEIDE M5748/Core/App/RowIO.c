
#include "stm32f1xx_hal.h"
#include "general.h"
#include "main.h"
#include "RowIO.h"

extern ADC_HandleTypeDef hadc1; //defined in main.c

struct RowInputs rowIn = {0};
struct RowOutputs rowOut = {0};
struct RowAnInputs rowAnIn = {0};

static void convertAnalogInputs();
static void ID_SetInput(GPIO_TypeDef *port, uint16_t pin);
static void ID_SetOutputLow(GPIO_TypeDef *port, uint16_t pin);

BatteryType_t battType = BATT_UNKNOWN;


void readID()
{
  battType = Detect_Battery();
   ID_SetInput(ID0_GPIO_Port,ID0_Pin);
   ID_SetInput(ID1_GPIO_Port,ID1_Pin); 
}

BatteryType_t Detect_Battery()
{
  rowIn.bIn_ID0 = HAL_GPIO_ReadPin(ID0_GPIO_Port, ID0_Pin);
  rowIn.bIn_ID1 = HAL_GPIO_ReadPin(ID1_GPIO_Port, ID1_Pin);
  
  if(rowIn.bIn_ID0 == 0 && rowIn.bIn_ID1 == 0)
    return BATT_6;
  
  if(rowIn.bIn_ID0 == 1 && rowIn.bIn_ID1 == 0)
    return BATT_5;

  if(rowIn.bIn_ID0 == 0 && rowIn.bIn_ID1 == 1)
    return BATT_4;
  
  if(rowIn.bIn_ID0 == 1 && rowIn.bIn_ID1 == 1)
  {
    ID_SetOutputLow(ID0_GPIO_Port,ID0_Pin);
    HAL_Delay(1);
    rowIn.bIn_ID1 = HAL_GPIO_ReadPin(ID1_GPIO_Port, ID1_Pin);
    
    if(rowIn.bIn_ID1 == 0)
    {
          ID_SetInput(ID0_GPIO_Port,ID0_Pin);
            return BATT_2;

    }
    
    ID_SetInput(ID0_GPIO_Port,ID0_Pin);
    HAL_Delay(1);
    ID_SetOutputLow(ID1_GPIO_Port,ID1_Pin);
    HAL_Delay(1);
    rowIn.bIn_ID0 = HAL_GPIO_ReadPin(ID0_GPIO_Port, ID0_Pin);
     if(rowIn.bIn_ID0 == 1)
     {
      ID_SetInput(ID1_GPIO_Port,ID1_Pin); 
      return BATT_1;
     }
     
        

     return BATT_3;
    
    
  }
return BATT_UNKNOWN;

}

void readDigitalInputs() // TODO: update according to new inputs
{
   rowIn.bIn_ID0 = HAL_GPIO_ReadPin(ID0_GPIO_Port, ID0_Pin);
   rowIn.bIn_ID1 = HAL_GPIO_ReadPin(ID1_GPIO_Port, ID1_Pin);
   rowIn.bIn_dO_Charge = HAL_GPIO_ReadPin(dO_Charge_GPIO_Port, dO_Charge_Pin);
   rowIn.bIn_dO_Dscharge = HAL_GPIO_ReadPin(dO_Dscharge_GPIO_Port, dO_Dscharge_Pin);
   rowIn.bIn_HeaterDisable = HAL_GPIO_ReadPin(HeaterDisable_GPIO_Port, HeaterDisable_Pin);
   rowIn.bIn_dEPO = HAL_GPIO_ReadPin(dEPO_GPIO_Port, dEPO_Pin);
   rowIn.bIn_dBattleMode = HAL_GPIO_ReadPin(dBattleMode_GPIO_Port, dBattleMode_Pin);
   rowIn.bIn_DSBL_FB = !HAL_GPIO_ReadPin(DSBL_FB_GPIO_Port, DSBL_FB_Pin);
}

    
void writeDigitalOutputs() // update according to new outputs
{
 
  HAL_GPIO_WritePin(dCH_EN1_GPIO_Port, dCH_EN1_Pin, rowOut.bOut_dCh_EN1 ? GPIO_PIN_SET : GPIO_PIN_RESET);
  HAL_GPIO_WritePin(dCH_EN2_GPIO_Port, dCH_EN2_Pin, rowOut.bOut_dCh_EN2 ? GPIO_PIN_SET : GPIO_PIN_RESET);
  HAL_GPIO_WritePin(HEATER_EN_GPIO_Port, HEATER_EN_Pin, rowOut.bOut_Heater_En ? GPIO_PIN_SET : GPIO_PIN_RESET);
  HAL_GPIO_WritePin(Charge_SW_EN_GPIO_Port, Charge_SW_EN_Pin, rowOut.bOut_Charge_Sw_En ? GPIO_PIN_SET : GPIO_PIN_RESET);
  HAL_GPIO_WritePin(DSBL_Test_GPIO_Port, DSBL_Test_Pin, rowOut.bOut_DSBL_test ? GPIO_PIN_SET : GPIO_PIN_RESET);
  HAL_GPIO_WritePin(MCU_EN_GPIO_Port, MCU_EN_Pin, rowOut.bOut_MCU_EN ? GPIO_PIN_SET : GPIO_PIN_RESET);
  HAL_GPIO_WritePin(OV_Test_PWM_GPIO_Port, OV_Test_PWM_Pin, rowOut.bOut_OV_Test ? GPIO_PIN_SET : GPIO_PIN_RESET);
  HAL_GPIO_WritePin(BP_RST_GPIO_Port, BP_RST_Pin, rowOut.bOut_BP_RST ? GPIO_PIN_SET : GPIO_PIN_RESET);

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



void readAnalogInputs() // read all IN
{
    rowAnIn.test_Ref   = readA2D(ADC_CHANNEL_9); 
    if((rowAnIn.test_Ref * 3300 / 4096) < 2.0f) // error in vref,TODO:  prevent heating in this case! 
      return;
  
  rowAnIn.temp1   = readA2D(ADC_CHANNEL_0);
  rowAnIn.temp2   = readA2D(ADC_CHANNEL_1);
  rowAnIn.temp3   = readA2D(ADC_CHANNEL_2);
  rowAnIn.temp4   = readA2D(ADC_CHANNEL_3);

  rowAnIn.vBatt   = readA2D(ADC_CHANNEL_4);
  rowAnIn.charger = readA2D(ADC_CHANNEL_5);
  rowAnIn.heater_N  = readA2D(ADC_CHANNEL_6);
  rowAnIn.Ich = readA2D(ADC_CHANNEL_7);
  
//  rowAnIn.Spare_1   = readA2D(ADC_CHANNEL_8);

  
  convertAnalogInputs();
  
  if (b500mS_flg)
  {
    //rowAnIn.FanTechometer = readTechometer(); //in Hz
  }
}


static void convertAnalogInputs() // update vars according to  modified rowAnIn
{
  rowAnIn.temp1   = tempConvert(rowAnIn.temp1) ;
  rowAnIn.temp2   = tempConvert(rowAnIn.temp2);
  rowAnIn.temp3   = tempConvert(rowAnIn.temp3);
  rowAnIn.temp4   = tempConvert(rowAnIn.temp4);
  
  rowAnIn.vBatt = rowAnIn.vBatt * 13556/ 4096   ; // Max reading � 135.56V
  rowAnIn.charger = rowAnIn.charger * 13556/ 4096; // multiply by scale factor / 4096 
  rowAnIn.heater_N = rowAnIn.heater_N * 13556/ 4096; // multiply by scale factor / 4096 
  
  rowAnIn.Ich = rowAnIn.Ich * 1100 / 4096 ; // Max reading � 1.1A 
  //rowAnIn.Spare_1 = rowAnIn.Spare_1 ; // multiply by scale factor / 4096 
  rowAnIn.test_Ref = rowAnIn.test_Ref * 3300 / 4096 ; // Max reading � 3.3V 
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

//min temp -50deg; max temp 120deg
const LinearSegment temp_table[] = {{152,3931},{198,3831},{259,3731},{333,3641},{419,3561},{593,3441},{816,3331},{1055,3241},
  {1500,3111},{2502,2881},{3206,2711},{3548,2601},{3767,2501},{3899,2411},{3976,2331},{4010,2281},{4035,2231}};
#define TEMP_TABLE_SIZE 17


//min temp -40deg; max temp 120deg for M5748
const LinearSegment temp15_table[] = {{102,3931},{117,3881},{153,3781},{203,3681},{280,3571},{391,3461},{531,3361},{746,3251},{1042,3141},{1512,3011},{2758,2741},{3326,2601},{3622,2501},{3808,2411},{3918,2331}};
#define TEMP15_TABLE_SIZE 15

uint32_t tempConvert(uint32_t temp) // TODO: verify convertaion and table values
{
    //return linearAproximation(temp, TEMP5_TABLE_SIZE, temp5_table);
  //return linearAproximation(temp, TEMP_TABLE_SIZE, temp_table);

  return linearAproximation(temp, TEMP15_TABLE_SIZE, temp15_table);
}

//helper functions for ID decode
static void ID_SetInput(GPIO_TypeDef *port, uint16_t pin)
{
    GPIO_InitTypeDef gpio ;
    gpio.Pin  = pin;
    gpio.Mode = GPIO_MODE_INPUT;
    gpio.Pull = GPIO_NOPULL;
    HAL_GPIO_Init(port, &gpio);
}

static void ID_SetOutputLow(GPIO_TypeDef *port, uint16_t pin)
{
    GPIO_InitTypeDef gpio ;
    gpio.Pin   = pin;
    gpio.Mode  = GPIO_MODE_OUTPUT_PP;
    gpio.Pull  = GPIO_NOPULL;
    gpio.Speed = GPIO_SPEED_FREQ_LOW;
    HAL_GPIO_Init(port, &gpio);

    HAL_GPIO_WritePin(port, pin, GPIO_PIN_RESET);
}

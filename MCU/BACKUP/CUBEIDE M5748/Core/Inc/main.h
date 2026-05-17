/* USER CODE BEGIN Header */
/**
  ******************************************************************************
  * @file           : main.h
  * @brief          : Header for main.c file.
  *                   This file contains the common defines of the application.
  ******************************************************************************
  * @attention
  *
  * Copyright (c) 2026 STMicroelectronics.
  * All rights reserved.
  *
  * This software is licensed under terms that can be found in the LICENSE file
  * in the root directory of this software component.
  * If no LICENSE file comes with this software, it is provided AS-IS.
  *
  ******************************************************************************
  */
/* USER CODE END Header */

/* Define to prevent recursive inclusion -------------------------------------*/
#ifndef __MAIN_H
#define __MAIN_H

#ifdef __cplusplus
extern "C" {
#endif

/* Includes ------------------------------------------------------------------*/
#include "stm32f1xx_hal.h"

/* Private includes ----------------------------------------------------------*/
/* USER CODE BEGIN Includes */

/* USER CODE END Includes */

/* Exported types ------------------------------------------------------------*/
/* USER CODE BEGIN ET */

/* USER CODE END ET */

/* Exported constants --------------------------------------------------------*/
/* USER CODE BEGIN EC */

/* USER CODE END EC */

/* Exported macro ------------------------------------------------------------*/
/* USER CODE BEGIN EM */

/* USER CODE END EM */

/* Exported functions prototypes ---------------------------------------------*/
void Error_Handler(void);

/* USER CODE BEGIN EFP */

/* USER CODE END EFP */

/* Private defines -----------------------------------------------------------*/
#define dEPO_Pin GPIO_PIN_13
#define dEPO_GPIO_Port GPIOC
#define dBattleMode_Pin GPIO_PIN_14
#define dBattleMode_GPIO_Port GPIOC
#define DSBL_FB_Pin GPIO_PIN_15
#define DSBL_FB_GPIO_Port GPIOC
#define AN_TEMP1_Pin GPIO_PIN_0
#define AN_TEMP1_GPIO_Port GPIOA
#define AN_TEMP2_Pin GPIO_PIN_1
#define AN_TEMP2_GPIO_Port GPIOA
#define AN_TEMP3_Pin GPIO_PIN_2
#define AN_TEMP3_GPIO_Port GPIOA
#define AN_TEMP4_Pin GPIO_PIN_3
#define AN_TEMP4_GPIO_Port GPIOA
#define AN_VBATT_Pin GPIO_PIN_4
#define AN_VBATT_GPIO_Port GPIOA
#define AN_Charger_Pin GPIO_PIN_5
#define AN_Charger_GPIO_Port GPIOA
#define AN_HEATHER_N_Pin GPIO_PIN_6
#define AN_HEATHER_N_GPIO_Port GPIOA
#define AN_Ich_Pin GPIO_PIN_7
#define AN_Ich_GPIO_Port GPIOA
#define AN_SPARE1_Pin GPIO_PIN_0
#define AN_SPARE1_GPIO_Port GPIOB
#define test_ref_Pin GPIO_PIN_1
#define test_ref_GPIO_Port GPIOB
#define HEATER_EN_Pin GPIO_PIN_2
#define HEATER_EN_GPIO_Port GPIOB
#define MAC_SDA_Pin GPIO_PIN_10
#define MAC_SDA_GPIO_Port GPIOB
#define MAC_SCL_Pin GPIO_PIN_11
#define MAC_SCL_GPIO_Port GPIOB
#define DSBL_Test_Pin GPIO_PIN_12
#define DSBL_Test_GPIO_Port GPIOB
#define MCU_EN_Pin GPIO_PIN_13
#define MCU_EN_GPIO_Port GPIOB
#define OV_Test_PWM_Pin GPIO_PIN_14
#define OV_Test_PWM_GPIO_Port GPIOB
#define BP_RST_Pin GPIO_PIN_15
#define BP_RST_GPIO_Port GPIOB
#define TX_EN_A_Pin GPIO_PIN_8
#define TX_EN_A_GPIO_Port GPIOA
#define dCH_EN2_Pin GPIO_PIN_11
#define dCH_EN2_GPIO_Port GPIOA
#define HeaterDisable_Pin GPIO_PIN_3
#define HeaterDisable_GPIO_Port GPIOB
#define dO_Dscharge_Pin GPIO_PIN_4
#define dO_Dscharge_GPIO_Port GPIOB
#define dO_Charge_Pin GPIO_PIN_5
#define dO_Charge_GPIO_Port GPIOB
#define ID0_Pin GPIO_PIN_6
#define ID0_GPIO_Port GPIOB
#define ID1_Pin GPIO_PIN_7
#define ID1_GPIO_Port GPIOB
#define dCH_EN1_Pin GPIO_PIN_8
#define dCH_EN1_GPIO_Port GPIOB
#define Charge_SW_EN_Pin GPIO_PIN_9
#define Charge_SW_EN_GPIO_Port GPIOB

/* USER CODE BEGIN Private defines */

/* USER CODE END Private defines */

#ifdef __cplusplus
}
#endif

#endif /* __MAIN_H */

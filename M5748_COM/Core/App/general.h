#ifndef _general_h_
#define _general_h_

#include "stm32f1xx_hal.h"
#include "Timing.h"


#define DEBUG 1
//#define MANUAL_CONTROL 1

#define M5480 1

#define TRUE 1
#define FALSE 0
//#define NULL 0

#define GETBIT(data, bit)  (((data) & (1<<bit)) != 0)
#define BIT(bit)  (1<<bit)

//typedef enum {ModuleA = 0, ModuleB = 1, ModuleAll = -1} CommModule;
//#define MODULES_NUM 2

//#define MODULE_A BIT(0)
//#define MODULE_B BIT(1)
#define BATT_NUM 6




typedef struct
{
  uint8_t state; //signal state after debounce
  uint8_t changed; //set for one debounce tic when state changes
  int32_t debounceCounter; //counter (max 24 days)
}Signal_t;

uint8_t CalcCRC(uint8_t buffer[], int buf_length);


void DebounceWithHysteresis(Signal_t* Signal, uint8_t SetCondition, uint8_t ResetCondition,
              int16_t UpThreshold,  int16_t UpStep,  int16_t UpBackStep,
              int16_t DownThreshold,  int16_t DownStep,  int16_t DownBackStep);
void Debounce(Signal_t* Signal, uint8_t CurrentState,
              int16_t UpThreshold,  int16_t UpStep,  int16_t UpBackStep,
              int16_t DownThreshold,  int16_t DownStep,  int16_t DownBackStep);

void ConservativeDebounce(Signal_t* Signal, uint8_t CurrentState, 
                          int16_t UpThreshold, int16_t DownThreshold);
void SetDebounce(Signal_t* Signal, uint8_t NewState);

typedef struct
{
  uint16_t x;
  uint16_t y;
} LinearSegment;

uint16_t linearAproximation(uint16_t data, uint32_t segmentsNumber, const LinearSegment table[]);


#include "DBG.h"

#endif

#include "stm32f1xx_hal.h"
#include "general.h"
#include "Timing.h"

int b50mS_flg, b100mS_flg,  b500mS_flg,  b1S_flg,  b1Min_flg;
int b100mS_gate=0, b500mS_gate=0, b1S_gate=0;
int bSlowBlinkin_gate;
static int slowBlinkingCounter=0;

uint64_t wakeCounter = 0;
uint32_t shortCounter = 0;
void updateTimingVar()
{
  wakeCounter++; //increment wake counter each time the main loop start (5msec)
  shortCounter++;
  
  //update timing flags:
  /*
  b100mS_flg = ((wakeCounter % T_100mSEC) == 0)     ? TRUE : FALSE;
  b500mS_flg = ((wakeCounter % (T_100mSEC*5)) == 0) ? TRUE : FALSE;
  b1S_flg    = ((wakeCounter % T_1SEC) == 0)        ? TRUE : FALSE;
  b1Min_flg  = ((wakeCounter % T_1MIN) == 0)        ? TRUE : FALSE;
  */
  b50mS_flg  = ((shortCounter % T_50mSEC) == 0)      ? TRUE : FALSE;
  b100mS_flg = ((shortCounter % T_100mSEC) == 0)     ? TRUE : FALSE;
  b500mS_flg = ((shortCounter % (T_100mSEC*5)) == 0) ? TRUE : FALSE;
  b1S_flg    = ((shortCounter % T_1SEC) == 0)        ? TRUE : FALSE;
  b1Min_flg  = ((shortCounter % T_1MIN) == 0)        ? TRUE : FALSE;
  if (b1Min_flg)
    shortCounter = 0;
  
  //update timing gates:
  if (b100mS_flg)
    b100mS_gate = !b100mS_gate;
  if (b500mS_flg)
    b500mS_gate = !b500mS_gate;
  if (b1S_flg)
    b1S_gate = !b1S_gate;
  
  if (b100mS_flg)
  {
      slowBlinkingCounter++;
      if (slowBlinkingCounter >= 20)
        slowBlinkingCounter = 0;

      if (slowBlinkingCounter < 10)
        bSlowBlinkin_gate = !b500mS_gate;
      else
        bSlowBlinkin_gate = 0;
  }
}


//Blocking delay in 1uSec resolutuin
//this function uses the main loop timer for delay counting
#if (MAIN_LOOP_RESOLUTION != 1)
#error "The shortDelay function assumes thet the main loop timer resolution is 1uSec"
#endif
extern TIM_HandleTypeDef htim6;
void shortDelay(int delay)
{
  int start,now,stop;

  start = __HAL_TIM_GET_COUNTER(&htim6);
#warning "TODO"
  stop = start+delay;
  
  if ((delay <= 0) || (delay >= MAIN_LOOP_PERIOD))
    return;
  
  do
  {
    now = __HAL_TIM_GET_COUNTER(&htim6);
    #warning "TODO"
    if (now < start)
      now += MAIN_LOOP_PERIOD;
  }while (now < stop);
}


// startDelay() + isDeleyEnded() are set of functions for non blocking short delayes in 1uSec resolutuin
// startDelay() should be called at the start of a delay and its return value saved.
// isDeleyEnded() receives the saved value from startDelay() and the requiered delay time.
// the delay MUST be shorter then the "spare/idle" time at the end of main loop period 

int startDelay()
{
  return __HAL_TIM_GET_COUNTER(&htim6);
}

int isDeleyEnded(int start, int delay)
{
  int now,stop;
  
  if ((delay <= 0) || (delay >= MAIN_LOOP_PERIOD/10))
    return TRUE; //wrong delay parameter - abort the delay
  
  if (start >= MAIN_LOOP_PERIOD)
    return TRUE; //wrong start parameter - abort the delay
  
  stop = start+delay;
  now = __HAL_TIM_GET_COUNTER(&htim6);
  if (now < start)
      now += MAIN_LOOP_PERIOD;
  
  if (now < stop)
    return FALSE;
  else return TRUE;
}
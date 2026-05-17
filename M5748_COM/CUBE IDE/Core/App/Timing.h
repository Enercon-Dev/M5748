
#ifndef _TIMING_H_
#define _TIMING_H_

#define MAIN_LOOP_RESOLUTION 1 //1uSec
#define MAIN_LOOP_PERIOD (4975*MAIN_LOOP_RESOLUTION) //5msec
//this is actualy a main loop timer period value
//#define MAIN_LOOP_TIMER TIM6


#define T_5mSEC   (1)
#define T_15mSEC  (3)
#define T_10mSEC  (2)
#define T_50mSEC  (5 * T_10mSEC)
#define T_100mSEC (10 * T_10mSEC)
#define T_250mSEC (25 * T_10mSEC)
#define T_500mSEC (5 * T_100mSEC)
#define T_1SEC    (10 * T_100mSEC)
#define T_1MIN    (60 * T_1SEC)
#define T_10MIN   (10 * T_1MIN)
#define T_1HOUR   (60 * T_10MIN)

extern int b50mS_flg, b100mS_flg,b250mS_flg, b500mS_flg,  b1S_flg,  b1Min_flg;
extern int b100mS_gate, b500mS_gate, b1S_gate;
extern int bSlowBlinkin_gate;

extern uint64_t wakeCounter;


void updateTimingVar();
void shortDelay(int delay);
int startDelay();
int isDeleyEnded(int start, int delay);

#endif //_TIMING_H_

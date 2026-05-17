#ifndef BATTRESMONITOR_H_
#define BATTRESMONITOR_H_

#include <stdint.h>
//TODO: Modify these values based on system
#define DELTA_V_THRESHOLD_MV      200
#define MIN_DELTA_I_MA            3000
#define SYNC_WINDOW_MS            50
#define RESISTANCE_MAX_MOHM       200


typedef enum
{
    RES_IDLE,
    RES_WAITING_FOR_I
} ResistanceState_t;

typedef struct
{
    ResistanceState_t state;

    int deltaV_mV;
    int deltaI_mA;
    long requestTime;

}ResistanceMonitor_t;

static ResistanceMonitor_t resMon;

void Resistance_CheckVoltage(uint32_t vbatt);
void Resistance_RequestDeltaI();
void Resistance_Task(void);
void Resistance_Process(uint32_t R_mOhm);


#endif /* BATTRESMONITOR_H_ */

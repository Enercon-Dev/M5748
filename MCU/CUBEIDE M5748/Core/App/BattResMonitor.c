#include "BattResMonitor.h"
#include "IntComm.h"
#include "Command.h"



 static int32_t prev = 0;
extern struct TelRequest intTelRequest;

void Resistance_CheckVoltage(uint32_t vbatt)
{

    int32_t delta = vbatt - prev;
    if (delta < 0)
        delta = -delta;

    if (resMon.state == RES_IDLE &&
        delta > DELTA_V_THRESHOLD_MV)
    {
        resMon.deltaV_mV = delta;

      //  Resistance_RequestDeltaI();

        resMon.requestTime = HAL_GetTick();
        resMon.state = RES_WAITING_FOR_I;
    }

    prev = vbatt;
}



void Resistance_SetDeltaI(int32_t deltaI)
{
    if (resMon.state != RES_WAITING_FOR_I)
        return;

    if (deltaI < MIN_DELTA_I_MA)
    {
        resMon.state = RES_IDLE;
        return;
    }

    resMon.deltaI_mA = deltaI;

    uint32_t R_mOhm =
        (resMon.deltaV_mV * 1000) / resMon.deltaI_mA;

    Resistance_Process(R_mOhm);

    resMon.state = RES_IDLE;
}

void Resistance_Task(void)
{
    if (resMon.state == RES_WAITING_FOR_I)
    {
        if ((HAL_GetTick() - resMon.requestTime) > 100)
        {
            resMon.state = RES_IDLE;  // timeout
        }
    }
}



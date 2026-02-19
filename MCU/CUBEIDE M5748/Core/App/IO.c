
#include "stm32f1xx_hal.h"
#include "general.h"
#include "RowIO.h"
#include "IO.h"

struct Inputs inputs = {0};
struct RowAnInputs anIn = {0};
static struct RowAnInputs anIn_sum = {0};

void initInputs()
{
	SetDebounce(&inputs.sEPO, 1);
}

void debounceDigitalInputs()
{
  //ConservativeDebounce(&inputs.sMode_PB, rowIn.bMode_PB, 4,4);
  //SetDebounce(&inputs.sFP_SW_SNS, rowIn.bFP_SW_SNS); //fast signal - not debounced
  Debounce(&inputs.sIn_ID0, rowIn.bIn_ID0, 3,1,1,3,1,1);
  Debounce(&inputs.sIn_ID1, rowIn.bIn_ID1, 3,1,1,3,1,1);
  Debounce(&inputs.sEPO, rowIn.bIn_dEPO, T_100mSEC,1,1,T_100mSEC,1,1);
  Debounce(&inputs.sDSBL_FB, rowIn.bIn_DSBL_FB, T_50mSEC,1,1,T_100mSEC,1,1);

  
}


void filterAnalogInputs()
{
  anIn_sum.temp1 += rowAnIn.temp1;
  anIn_sum.temp2 += rowAnIn.temp2;
  anIn_sum.temp3 += rowAnIn.temp3;
  anIn_sum.temp4 += rowAnIn.temp4;
  anIn_sum.vBatt += rowAnIn.vBatt;
  anIn_sum.charger += rowAnIn.charger;
  anIn_sum.heater_N += rowAnIn.heater_N;
  anIn_sum.Ich += rowAnIn.Ich;
  anIn_sum.test_Ref += rowAnIn.test_Ref;
  
  if (b100mS_flg)
  {
    anIn.vBatt = anIn_sum.vBatt / T_100mSEC;
    anIn_sum.vBatt = 0;
    
    anIn.charger = anIn_sum.charger / T_100mSEC;
    anIn_sum.charger = 0;
      
    anIn.heater_N = anIn_sum.heater_N / T_100mSEC;
    anIn_sum.heater_N = 0;

    anIn.Ich = anIn_sum.Ich / T_100mSEC;
    anIn_sum.Ich = 0;    
  }
  
  if (b500mS_flg)
  {
    anIn.temp1 = anIn_sum.temp1 / T_500mSEC;
    anIn_sum.temp1 = 0;
      
    anIn.temp2 = anIn_sum.temp2 / T_500mSEC;
    anIn_sum.temp2 = 0;

    anIn.temp3 = anIn_sum.temp3 / T_500mSEC;
    anIn_sum.temp3 = 0;

    anIn.temp4 = anIn_sum.temp4 / T_500mSEC;
    anIn_sum.temp4 = 0;

    anIn.test_Ref = anIn_sum.test_Ref / T_500mSEC;
    anIn_sum.test_Ref = 0;
}
}

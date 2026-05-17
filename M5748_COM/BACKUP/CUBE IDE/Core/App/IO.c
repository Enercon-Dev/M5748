
#include "stm32f1xx_hal.h"
#include "general.h"
#include "RowIO.h"
#include "IO.h"

struct Inputs inputs = {0};
struct RowAnInputs anIn = {0};
static struct RowAnInputs anIn_sum = {0};

void debounceDigitalInputs()
{
  ConservativeDebounce(&inputs.sEPO,        rowIn.bEPO,        4,4);
  ConservativeDebounce(&inputs.sEN_380VDC,  rowIn.bEN_380VDC,  4,4);
  ConservativeDebounce(&inputs.sBattleMode, rowIn.bBattleMode, 4,4);
  ConservativeDebounce(&inputs.sInSpare,    rowIn.bInSpare,    4,4);
}


void filterAnalogInputs()
{
  anIn_sum.PCBtemp += rowAnIn.PCBtemp;
  anIn_sum.VCC_IO += rowAnIn.VCC_IO;
  anIn_sum.In28V += rowAnIn.In28V;
  anIn_sum.TestRef += rowAnIn.TestRef;
  
  if (b500mS_flg)
  {
    anIn.PCBtemp = anIn_sum.PCBtemp / T_500mSEC;
    anIn_sum.PCBtemp = 0;
      
    anIn.VCC_IO = anIn_sum.VCC_IO / T_500mSEC;
    anIn_sum.VCC_IO = 0;

    anIn.In28V = anIn_sum.In28V / T_500mSEC;
    anIn_sum.In28V = 0;

    anIn.TestRef = anIn_sum.TestRef / T_500mSEC;
    anIn_sum.TestRef = 0;
  }
}


#ifndef _ROWIO_H_
#define _ROWIO_H_

struct RowInputs
{
  int bIn_ID0;
  int bIn_ID1;
  int bIn_HeaterDisable;
  int bIn_dO_Dscharge;
  int bIn_dO_Charge;
  int bIn_dEPO;
  int bIn_dBattleMode;
  int bIn_DSBL_FB;
};


struct RowOutputs 
{
  int bOut_dCh_EN1;
  int bOut_dCh_EN2;
  int bOut_Heater_En;
  int bOut_Charge_Sw_En;
  int bOut_DSBL_test;
  int bOut_MCU_EN;
  int bOut_OV_Test;
  int bOut_BP_RST;

};


struct RowAnInputs
{
  uint32_t temp1;
  uint32_t temp2;
  uint32_t temp3;
  uint32_t temp4;
  uint32_t vBatt;
  uint32_t charger;
  uint32_t heater_N;
  uint32_t Ich;
  uint32_t Spare_1;  
  uint32_t test_Ref;
};

typedef enum 
{
    BATT_UNKNOWN = 0,
    BATT_1,
    BATT_2,
    BATT_3,
    BATT_4,
    BATT_5,
    BATT_6
}BatteryType_t ;

typedef enum
{
    HEATER_OFF,
    HEATER_ON,
    HEATER_COOLDOWN
} HeaterState_t;


extern struct RowInputs rowIn;
extern struct RowOutputs rowOut;
extern struct RowAnInputs rowAnIn;
void initOutputs();
void readID();
BatteryType_t Detect_Battery();
void readDigitalInputs();
void writeDigitalOutputs();
void readAnalogInputs();
uint32_t tempConvert(uint32_t temp);


#endif //_ROWIO_H_

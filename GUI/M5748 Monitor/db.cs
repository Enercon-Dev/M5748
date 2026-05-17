using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MPS_Tools;

namespace M5748_Monitor
{
    public partial class MainForm : Form
    {
        private void dbInit()
        {
            FlagDecoder flagDecoder = new FlagDecoder(1, false);

            //Battery Fields:
            db.AddField(new DBFlagField("Out.dch_EN2", "", flagDecoder));
            db.AddField(new DBFlagField("Out.dch_EN1", "", flagDecoder));
            db.AddField(new DBFlagField("Out.Heater_En", "", flagDecoder));
            db.AddField(new DBFlagField("Out.Charge_Sw_En", "", flagDecoder));
            db.AddField(new DBFlagField("Out.Mcu_En", "", flagDecoder));
            db.AddField(new DBFlagField("Out.Ov_Test", "", flagDecoder));
            db.AddField(new DBFlagField("Out.Bp_Rst", "", flagDecoder));
            db.AddField(new DBFlagField("Out.Dsbl_test", "", flagDecoder));

            db.AddField(new DBFlagField("In.ID0", "", flagDecoder));
            db.AddField(new DBFlagField("In.ID1", "", flagDecoder));
            db.AddField(new DBFlagField("In.HeaterDisable", "", flagDecoder));
            db.AddField(new DBFlagField("In.dO_Dscharge", "", flagDecoder));
            db.AddField(new DBFlagField("In.dO_Charge", "", flagDecoder));
            db.AddField(new DBFlagField("In.dEPO", "", flagDecoder));
            db.AddField(new DBFlagField("In.dBattleMode", "", flagDecoder));
            db.AddField(new DBFlagField("In.DSBL_FB", "", flagDecoder));

            db.AddField(new DBRawIntField("BattNumber", "", new RawIntDecoder(8)));

            db.AddField(new DBAnalogField("AnIn.Temp1", "", new AnalogDecoder(16, 0.1, -273.1), "°"));
            db.AddField(new DBAnalogField("AnIn.Temp3", "", new AnalogDecoder(16, 0.1, -273.1), "°"));
            db.AddField(new DBAnalogField("AnIn.Temp4", "", new AnalogDecoder(16, 0.1, -273.1), "°"));
            db.AddField(new DBAnalogField("AnIn.Temp2", "", new AnalogDecoder(16, 0.1, -273.1), "°"));
            db.AddField(new DBAnalogField("AnIn.vBatt", "", new AnalogDecoder(16, 0.01, 0), "V"));
            db.AddField(new DBAnalogField("AnIn.Charger", "", new AnalogDecoder(16, 0.01, 0), "V"));
            db.AddField(new DBAnalogField("AnIn.Heater_N", "", new AnalogDecoder(16, 0.01, 0), "V"));
            db.AddField(new DBAnalogField("AnIn.Ich", "", new AnalogDecoder(16, 0.001, 0), "A"));
           // db.AddField(new DBAnalogField("AnIn.Spare1", "", new AnalogDecoder(16, 1, 0), ""));
            db.AddField(new DBAnalogField("AnIn.TestRef", "", new AnalogDecoder(16, 0.001, 0), "V"));
            db.AddField(new DBAnalogField("BattSOC", "", new AnalogDecoder(16, 0.01, 0), "%"));

            db.AddField(new DBFlagField("State_FullBatt", "", flagDecoder));


            //Charger Fields:
            db.AddField(new DBFlagField("Charger.In.EPO", "", flagDecoder));
            db.AddField(new DBFlagField("Charger.In.EN380", "", flagDecoder));
            db.AddField(new DBFlagField("Charger.In.BattleMode", "", flagDecoder));
            db.AddField(new DBFlagField("Charger.In.InSpare", "", flagDecoder));

            db.AddField(new DBFlagField("Charger.Out.Fault", "", flagDecoder));
            db.AddField(new DBFlagField("Charger.Out.FaultLed", "", flagDecoder));
            db.AddField(new DBFlagField("Charger.Out.ISO_FLR", "", flagDecoder));
            db.AddField(new DBFlagField("Charger.Out.350V_OK", "", flagDecoder));
            db.AddField(new DBFlagField("Charger.Out.OutSpare", "", flagDecoder));

            db.AddField(new DBAnalogField("Charger.AnIn.PCBTemp", "", new AnalogDecoder(16, 0.1, -273.1), "°"));
            db.AddField(new DBAnalogField("Charger.AnIn.VCC_IO", "", new AnalogDecoder(16, 0.001, 0), "V"));
            db.AddField(new DBAnalogField("Charger.AnIn.IN28V", "", new AnalogDecoder(16, 0.001, 0), "V"));
            db.AddField(new DBAnalogField("Charger.AnIn.TestRef", "", new AnalogDecoder(16, 0.001, 0), "V"));

            db.AddField(new DBRawIntField("Charger.Version.Major", "", new RawIntDecoder(8)));
            db.AddField(new DBRawIntField("Charger.Version.Minor", "", new RawIntDecoder(8)));
            db.AddField(new DBStringField("Charger.Version.String", "", new StringDecoder(32, false)));


            //Master
            db.AddField(new DBFlagField("Master.In.EPO", "", flagDecoder));
            db.AddField(new DBFlagField("Master.In.OutEnable", "", flagDecoder));
            db.AddField(new DBFlagField("Master.In.BattleMode", "", flagDecoder));
            db.AddField(new DBFlagField("Master.In.InterlockIn", "", flagDecoder));

            db.AddField(new DBFlagField("Master.Out.InterlockOut", "", flagDecoder));
            db.AddField(new DBFlagField("Master.Out.DischargeDisable", "", flagDecoder));
            db.AddField(new DBFlagField("Master.Out.Sync", "", flagDecoder));
            db.AddField(new DBFlagField("Master.Out.IMD_DRVL", "", flagDecoder));
            db.AddField(new DBFlagField("Master.Out.IMD_DRVH", "", flagDecoder));

            db.AddField(new DBAnalogField("Master.AnIn.VBATT", "", new AnalogDecoder(16, 0.01, 0), "V"));
            db.AddField(new DBAnalogField("Master.AnIn.VBUS_A", "", new AnalogDecoder(16, 0.01, 0), "V"));
            db.AddField(new DBAnalogField("Master.AnIn.VBUS_B", "", new AnalogDecoder(16, 0.01, 0), "V"));
            db.AddField(new DBAnalogField("Master.AnIn.MBD", "", new AnalogDecoder(16, 0.01, 0), "V"));
            db.AddField(new DBAnalogField("Master.AnIn.AUX", "", new AnalogDecoder(16, 0.001, 0), "V"));
            db.AddField(new DBAnalogField("Master.AnIn.AUX_BU", "", new AnalogDecoder(16, 0.001, 0), "V"));
            db.AddField(new DBAnalogField("Master.AnIn.ImdBalance", "", new AnalogDecoder(16, 0.01, 0), "V"));
            db.AddField(new DBAnalogField("Master.AnIn.Temp", "", new AnalogDecoder(16, 0.1, -273.1), "°"));
            db.AddField(new DBAnalogField("Master.AnIn.BUSR", "", new AnalogDecoder(16, 0.01, 0), "V"));
            db.AddField(new DBAnalogField("Master.AnIn.TestRef", "", new AnalogDecoder(16, 0.001, 0), "V"));

            db.AddField(new DBFlagField("Master.OutputOK", "", flagDecoder));
            db.AddField(new DBFlagField("Master.ISOFault", "", flagDecoder));
            db.AddField(new DBFlagField("Master.Fault", "", flagDecoder));
            db.AddField(new DBFlagField("Master.FaultLED", "", flagDecoder));

            db.AddField(new DBAnalogField("Master.IMD_IsoRes", "", new AnalogDecoder(16, 0.1, 0), "KΩ"));
            db.AddField(new DBAnalogField("Master.IMD_VLow", "", new AnalogDecoder(16, 0.01, 0), "V"));
            db.AddField(new DBAnalogField("Master.IMD_VHigh", "", new AnalogDecoder(16, 0.01, 0), "V"));


            db.AddField(new DBRawIntField("Master.Version.Major", "", new RawIntDecoder(8)));
            db.AddField(new DBRawIntField("Master.Version.Minor", "", new RawIntDecoder(8)));
            db.AddField(new DBStringField("Master.Version.String", "", new StringDecoder(32, false)));


            //M5477 - BUCK:
            db.AddField(new DBRawIntField("Conv.Version.Major", "", new RawIntDecoder(4)));
            db.AddField(new DBRawIntField("Conv.Version.Minor", "", new RawIntDecoder(4)));
            //inputs
            db.AddField(new DBFlagField("Conv.InrushReady", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.OverVoltage", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.DCM", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.Sync", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.UVccAUX", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.Spare", "", flagDecoder));

            db.AddField(new DBFlagField("Conv.BattleMode", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.OutEnable", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.EPO", "", flagDecoder));

            db.AddField(new DBFlagField("Conv.BuckCL_A", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.BuckCL_B", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.NegCL_A", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.NegCL_B", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.CompA", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.CompB", "", flagDecoder));
            //outputs
            db.AddField(new DBFlagField("Conv.OV_Test", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.BuckSS", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.BoostSS", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.InrushEN", "", flagDecoder));


            db.AddField(new DBFlagField("Conv.InrushEnCMD", "", flagDecoder));
            db.AddField(new DBFlagField("Conv.OutEnCMD", "", flagDecoder));
 
            db.AddField(new DBAnalogField("Conv.AnIn.I_A", "", new AnalogDecoder(16, 0.01832, 13.063), "A"));
            db.AddField(new DBAnalogField("Conv.AnIn.I_B", "", new AnalogDecoder(16, 0.01832, 13.063), "A"));
            db.AddField(new DBAnalogField("Conv.AnIn.Temp", "", new TempDecoder(16, 1.0 / 1024, 0, 4990), "°"));
            db.AddField(new DBAnalogField("Conv.AnIn.Vout", "", new AnalogDecoder(16, 439 / 1024, 0), "V"));
            db.AddField(new DBAnalogField("Conv.AnIn.Vrtn", "", new AnalogDecoder(16, 439 / 1024, 0), "V"));
            db.AddField(new DBAnalogField("Conv.AnIn.vcc3_3", "", new AnalogDecoder(16, 5 / 1024, 0), "V"));
            
            /*
            db.AddField(new DBAnalogField("BUCK.IoutDac", "", new AnalogDecoder(16, 5.0 / 1024 / 0.09, 0), "A")); //90mV/A
            
            db.AddField(new DBFlagField("BUCK.CommWD", "", flagDecoder));
            db.AddField(new DBFlagField("BUCK.CurrentFaultSD", "", flagDecoder));
            db.AddField(new DBFlagField("BUCK.CCMode", "", flagDecoder));
            db.AddField(new DBFlagField("BUCK.BuckOCLock", "", flagDecoder));
            db.AddField(new DBFlagField("BUCK.BuckOVLock", "", flagDecoder));
            db.AddField(new DBFlagField("BUCK.BuckReady", "", flagDecoder));
            db.AddField(new DBFlagField("BUCK.ADCFail", "", flagDecoder));
            db.AddField(new DBFlagField("BUCK.BuckEnable", "", flagDecoder));
            db.AddField(new DBRawIntField("BUCK.SHDNReason", "", new RawIntDecoder(8)));
            */
            //ESR - Event Status Register 
            db.AddField(new DBFlagField("SCPI.ESR.QueryError", "", flagDecoder));
            db.AddField(new DBFlagField("SCPI.ESR.ExecutionError", "", flagDecoder));
            db.AddField(new DBFlagField("SCPI.ESR.CommandError", "", flagDecoder));
            db.AddField(new DBFlagField("SCPI.ESR.PowerOn", "", flagDecoder));

            //STB - Status Byte Register
            db.AddField(new DBFlagField("SCPI.STB.ProtectionEventFlag", "", flagDecoder));
            db.AddField(new DBFlagField("SCPI.STB.ErrorQueue", "", flagDecoder));
            db.AddField(new DBFlagField("SCPI.STB.ESB", "", flagDecoder));

            //PCR - Protection Condition Register
            //PER - Protection Event Register
            for (int i = 0; i < 2; i++)
            {
                string s = (i == 0) ? "SCPI.PCR." : "SCPI.PER.";

                db.AddField(new DBFlagField(s + "OutputOverVoltage", "", flagDecoder));
                db.AddField(new DBFlagField(s + "OutputOverCurrent", "", flagDecoder));
                db.AddField(new DBFlagField(s + "OutputRegulationFailure", "", flagDecoder));
                db.AddField(new DBFlagField(s + "OutputUnderVoltage", "", flagDecoder));
                db.AddField(new DBFlagField(s + "OverTempShutdown", "", flagDecoder));
                db.AddField(new DBFlagField(s + "FanFailure", "", flagDecoder));
                db.AddField(new DBFlagField(s + "OpenInterlock", "", flagDecoder));
                db.AddField(new DBFlagField(s + "ACUnderVoltage", "", flagDecoder));
                db.AddField(new DBFlagField(s + "ACMissingPhase", "", flagDecoder));
                db.AddField(new DBFlagField(s + "CommWatchdog", "", flagDecoder));
                db.AddField(new DBFlagField(s + "InternalFailure", "", flagDecoder));
                db.AddField(new DBFlagField(s + "MarginalProtectionSetting", "", flagDecoder));
            }

            db.AddField(new DBFlagField("Control.Out.InputRelayEN", "", flagDecoder));
            db.AddField(new DBFlagField("Control.Out.Interlock_OUT", "", flagDecoder));
            db.AddField(new DBFlagField("Control.Out.FAN_24V_EN", "", flagDecoder));
            db.AddField(new DBFlagField("Control.Out.HPF_PWM_EN", "", flagDecoder));
            db.AddField(new DBFlagField("Control.In.Interlock_In", "", flagDecoder));
            db.AddField(new DBFlagField("Control.In.FP_SW_SNS", "", flagDecoder));
            db.AddField(new DBRawIntField("Control.IDCode", "", new RawIntDecoder(16)));

            db.AddField(new DBAnalogField("Control.FPTemp", "", new AnalogDecoder(16, 0.1, -273.1), "°"));
            db.AddField(new DBAnalogField("Control.Vcc33", "", new AnalogDecoder(16, 0.001, 0), "V"));
            db.AddField(new DBAnalogField("Control.AuxA", "", new AnalogDecoder(16, 0.001, 0), "V"));
            db.AddField(new DBAnalogField("Control.AuxB", "", new AnalogDecoder(16, 0.001, 0), "V"));
            db.AddField(new DBAnalogField("Control.Fan24v", "", new AnalogDecoder(16, 0.001, 0), "V"));

            db.AddField(new DBAnalogField("Control.FanTechometer", "", new AnalogDecoder(16, 60, 0), "rpm"));
            db.AddField(new DBAnalogField("Control.HotSpot", "", new AnalogDecoder(16, 0.1, -273.1), "°"));
            db.AddField(new DBAnalogField("Control.FanSpeed", "", new AnalogDecoder(16, 1, 0), "%"));
            db.AddField(new DBAnalogField("Control.testTemp", "", new AnalogDecoder(16, 0.1, -273.1), "°"));
            db.AddField(new DBAnalogField("Control.VoltageCorrection", "", new AnalogSignedDecoder(8, 1, 0), ""));
            db.AddField(new DBAnalogField("Control.VoltageTrimming", "", new AnalogSignedDecoder(8, 1, 0), ""));

            db.AddField(new DBStringField("SCPI.IDN", "", new StringDecoder(100, false)));
            db.AddField(new DBStringField("SCPI.ERR", "", new StringDecoder(100, false)));
            db.AddField(new DBStringField("SCPI.Vout", "", new StringDecoder(100, false)));
            db.AddField(new DBStringField("SCPI.Iout", "", new StringDecoder(100, false)));
            db.AddField(new DBStringField("SCPI.Reg", "", new StringDecoder(100, false)));
            db.AddField(new DBStringField("SCPI.Mode", "", new StringDecoder(100, false)));


            db.AddField(new DBFlagField("SD_Reason.MCU.Reset", "", flagDecoder));
            db.AddField(new DBFlagField("SD_Reason.MCU.Interlock", "", flagDecoder));
            db.AddField(new DBFlagField("SD_Reason.MCU.PowerSwitch", "", flagDecoder));
            db.AddField(new DBFlagField("SD_Reason.MCU.EnableCommand", "", flagDecoder));

            db.AddField(new DBFlagField("SD_Reason.MCU.OverCurent", "", flagDecoder));
            db.AddField(new DBFlagField("SD_Reason.MCU.OverVoltage", "", flagDecoder));
            db.AddField(new DBFlagField("SD_Reason.MCU.OverTemp", "", flagDecoder));
            db.AddField(new DBFlagField("SD_Reason.MCU.OverLoad", "", flagDecoder));
            db.AddField(new DBFlagField("SD_Reason.MCU.CommWD", "", flagDecoder));
            db.AddField(new DBFlagField("SD_Reason.MCU.VersionsFail", "", flagDecoder));
            db.AddField(new DBFlagField("SD_Reason.MCU.InterlockFail", "", flagDecoder));
            db.AddField(new DBFlagField("SD_Reason.MCU.IntComProblem", "", flagDecoder));

            for (int i = 0; i < 2; i++)
            {
                string s = "SD_Reason.HPF[" + i.ToString() + "].";
                db.AddField(new DBFlagField(s + "Ready", "", flagDecoder));
                db.AddField(new DBFlagField(s + "Sync", "", flagDecoder));
                db.AddField(new DBFlagField(s + "InOk", "", flagDecoder));
                db.AddField(new DBFlagField(s + "Inruch", "", flagDecoder));
                db.AddField(new DBFlagField(s + "OverVoltage", "", flagDecoder));
                db.AddField(new DBFlagField(s + "UvccAux", "", flagDecoder));
                db.AddField(new DBFlagField(s + "BusLow", "", flagDecoder));
                db.AddField(new DBFlagField(s + "BusUV", "", flagDecoder));

                s = "SD_Reason.ISO[" + i.ToString() + "].";
                db.AddField(new DBFlagField(s + "Ready", "", flagDecoder));
                db.AddField(new DBFlagField(s + "HpfNotReady", "", flagDecoder));
                db.AddField(new DBFlagField(s + "OverVoltage", "", flagDecoder));
                db.AddField(new DBFlagField(s + "UvccAux", "", flagDecoder));
                db.AddField(new DBFlagField(s + "WakeUpTimeOut", "", flagDecoder));
                db.AddField(new DBFlagField(s + "TestModeFailure", "", flagDecoder));
                db.AddField(new DBFlagField(s + "CurrentFault", "", flagDecoder));

                s = "SD_Reason.Buck[" + i.ToString() + "].";
                db.AddField(new DBFlagField(s + "Ready", "", flagDecoder));
                db.AddField(new DBFlagField(s + "OverVoltage", "", flagDecoder));
                db.AddField(new DBFlagField(s + "OverCurrent", "", flagDecoder));
                db.AddField(new DBFlagField(s + "UvccAux", "", flagDecoder));
                db.AddField(new DBFlagField(s + "IsoNotReady", "", flagDecoder));
                db.AddField(new DBFlagField(s + "ADCFail", "", flagDecoder));
                db.AddField(new DBFlagField(s + "CurrentFault", "", flagDecoder));
                db.AddField(new DBFlagField(s + "CommWatchdog", "", flagDecoder));
            }
        }

    }


    public class AnalogSignedDecoder : AnalogDecoder
    {
        public AnalogSignedDecoder() : base() { }
        public AnalogSignedDecoder(int _fieldSize, double _aFactor, double _bFactor)
            : base(_fieldSize, _aFactor, _bFactor) { }
        public AnalogSignedDecoder(string _context, int _fieldSize, double _aFactor, double _bFactor)
            : base(_context, _fieldSize, _aFactor, _bFactor) { }

        protected override void SimpleDecode(DataBuffer buffer, DBField field)
        {
            if (!(field is DBAnalogField))
            {
                throw new Exception("the field is not a DBAnalogField");
            }
            //base.SimpleDecode(buffer, field);
            //int data = (int)getDataFromBuffer(buffer, fieldSize);
            //(field as DBAnalogField).RowData = data;
            //field.RowDataString = data.ToString();

            int data = (int)buffer.getBits(fieldSize);
            int maxSize = (1 << (fieldSize-1))-1;

            if (data > maxSize)
            {
                data = -((~data + 1) & maxSize);
            }

            field.RowData = data;
            field.RowDataString = data.ToString();

            (field as DBAnalogField).Value = field.RowData * aFactor + bFactor;
        }
    }

    public class TempDecoder : AnalogDecoder
    {
        public TempDecoder() : base() { }
        public TempDecoder(int _fieldSize, double _aFactor, double _bFactor, double _puRes)
            : base(_fieldSize, _aFactor, _bFactor) { puRes = _puRes; }
        public TempDecoder(string _context, int _fieldSize, double _aFactor, double _bFactor, double _puRes)
            : base(_context, _fieldSize, _aFactor, _bFactor) { puRes = _puRes;}

        protected double puRes = 10000;
        public double PURes
        {
            get { return puRes; }
            set { puRes = value; }
        }

        protected override void SimpleDecode(DataBuffer buffer, DBField field)
        {
            if (!(field is DBAnalogField))
            {
                throw new Exception("the field is not a DBAnalogField");
            }
            base.SimpleDecode(buffer, field);
            double voltage = field.RowData * aFactor + bFactor; //normalized voltage

            double res = voltage / (1 - voltage) * puRes;
            (field as DBAnalogField).Value = ResToTemp(res);
        }

        private double ResToTemp(double R)
        {
            const double Rnom = 10000;
            const double A = 0.003354016;
            const double B = 0.000256985;
            const double C = 0.000002620131;
            const double D = 0.00000006383091;

            double Temp;

            Temp = A +
                    B * Math.Log(R / Rnom) +
                    Math.Pow(C * Math.Log(R / Rnom), 2) +
                    Math.Pow(D * Math.Log(R / Rnom), 3);
            Temp = 1 / Temp - 273.15;

            return Temp;
        }

        private double ResBToTemp(double R)
        {
            double Temp;

            Temp = 0.00001 * R * R + 0.2354 * R - 245.61; //y=1E-05x2 + 0.2354x - 245.61
            return Temp;
        }
    }

}

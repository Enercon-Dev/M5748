using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS_Tools;

namespace M5748_Monitor
{
    class TelDecode
    {
        private CommDB db;
        private Log log;
        private MainForm mainForm;
        private TelDecodeMap[] telDecodeMap;

        public TelDecode(CommDB db, Log log, MainForm mainForm)
        {
            this.db = db;
            this.log = log;
            this.mainForm = mainForm;
            telDecodeMap = new TelDecodeMap[]
            {
                //new TelDecodeMap(0x80, "ACK/NACK", null, 0),
                new TelDecodeMap(0x81, "Charger Status Tel", decodeChargerStatusTel, CHARGER_STATUS_TEL_LENGTH),
                new TelDecodeMap(0x82, "Master Status Tel", decodeMasterTel, MASTER_TEL_LENGTH),
                new TelDecodeMap(0x83, "Batt Status Tel", decodeBattStatusTel, BATT_STATUS_TEL_LENGTH),
                new TelDecodeMap(0x84, "Boost-Buck Status Tel", decodeConvTel, CONV_TEL_LENGTH),
                new TelDecodeMap(0x8F, "Software Version Tel", decodeSoftVersionTel, SOFT_VERSION_TEL_LENGTH),

                //new TelDecodeMap(0x8D01, "Array Tel", decodeArrayTel, 0),
            };
        }


        public void DecodeTelemetry(IFrame frame)
        {
            DataBuffer buffer = new DataBuffer(frame.GetDataBuffer());
            int opCode = buffer.getByte();

            foreach (TelDecodeMap decode in telDecodeMap)
            {
                if (decode.opCode != opCode)
                    continue;
                
                frame.Description = decode.description;
                if (mainForm.EnableLogCheckBox.Checked)
                    log.Add(frame);
                if (buffer.RemainingBytes < decode.minLength)
                {
                    log.Add("", "Wrong Tel Length", LogedStatus.LsError);
                    return;
                }
                if (decode.decodeFunction != null)
                {
                    decode.decodeFunction(buffer, (CommAddress)((frame as M5748Frame).Sourse));
                    db.UpdateClients();
                }
                return;
            }

            frame.Description = "unknown tel";
            log.Add(frame, LogedStatus.LsError);
        }



        private string[] ChargerStatusTelFields = {
            "Charger.In.EPO",
            "Charger.In.EN380",
            "Charger.In.BattleMode",
            "Charger.In.InSpare",
            "_SpareBit_","_SpareBit_","_SpareBit_","_SpareBit_", //4 Spare bits

            "Charger.Out.Fault",
            "Charger.Out.FaultLed",
            "Charger.Out.ISO_FLR",
            "Charger.Out.350V_OK",
            "Charger.Out.OutSpare",
            "_SpareBit_","_SpareBit_", "_SpareBit_",//3 Spare bits

            "Charger.AnIn.PCBTemp",
            "Charger.AnIn.VCC_IO",
            "Charger.AnIn.IN28V",
            "Charger.AnIn.TestRef",
            };


        private const int CHARGER_STATUS_TEL_LENGTH = 10;
        private void decodeChargerStatusTel(DataBuffer buffer, CommAddress source)
        {
            //buffer.BigEndian = false;
            decodeTelList(buffer, ChargerStatusTelFields);
            mainForm.NextTelRequests(1);
        }



        private string[] BattStatusTelFields = {
            "Out.dch_EN2",
            "Out.dch_EN1",
            "Out.Heater_En",
            "Out.Charge_Sw_En",
            "Out.Mcu_En",
            "Out.Ov_Test",
            "Out.Bp_Rst",
            "Out.Dsbl_test",

            "In.ID1",
            "In.ID0",
            "In.HeaterDisable",
            "In.dO_Dscharge",
            "In.dO_Charge",
            "In.dEPO",
            "In.dBattleMode",
            "In.DSBL_FB",

            "BattNumber",

            "AnIn.Temp1",
            "AnIn.Temp2",
            "AnIn.Temp3",
            "AnIn.Temp4",
            "AnIn.vBatt",
            "AnIn.Charger",
            "AnIn.Heater_N",
            "AnIn.Ich",
            "AnIn.TestRef",
            "BattSOC",
            "State_FullBatt"


            };

        private const int BATT_STATUS_TEL_LENGTH = 24;
        private void decodeBattStatusTel(DataBuffer buffer, CommAddress source)
        {
            //buffer.BigEndian = false;
            decodeTelList(buffer, BattStatusTelFields);
        }


        private string[] MasterTelFields = {
            "Master.In.EPO",
            "Master.In.OutEnable",
            "Master.In.BattleMode",
            "Master.In.InterlockIn",
            "_SpareBit_","_SpareBit_","_SpareBit_","_SpareBit_", //4 Spare bits

            "Master.Out.InterlockOut",
            "Master.Out.DischargeDisable",
            "Master.Out.Sync",
            "Master.Out.IMD_DRVL",
            "Master.Out.IMD_DRVH",
            "_SpareBit_","_SpareBit_","_SpareBit_", //3 Spare bits

            "Master.AnIn.VBATT",
            "Master.AnIn.VBUS_A",
            "Master.AnIn.VBUS_B",
            "Master.AnIn.MBD",
            "Master.AnIn.AUX",
            "Master.AnIn.AUX_BU",
            "Master.AnIn.ImdBalance",
            "Master.AnIn.Temp",
            "Master.AnIn.BUSR",
            "Master.AnIn.TestRef",

            "Master.OutputOK",
            "Master.ISOFault",
            "Master.Fault",
            "Master.FaultLED",
            "_SpareBit_","_SpareBit_","_SpareBit_","_SpareBit_", //4 Spare bits

            "Master.IMD_IsoRes",
            "Master.IMD_VLow",
            "Master.IMD_VHigh"
        };

        private const int MASTER_TEL_LENGTH = 29;
        private void decodeMasterTel(DataBuffer buffer, CommAddress source)
        {
            //buffer.BigEndian = false;
            decodeTelList(buffer, MasterTelFields);
        }

        private string[] BuckTelFields = {
            "Conv.Version.Major", "Conv.Version.Minor", //byte 1
            "_SpareBit_","_SpareBit_", //2 Spare bits - byte 2
            "Conv.InrushReady",
            "Conv.OverVoltage",
            "Conv.DCM",
            "Conv.Sync",
            "Conv.UVccAUX",
            "Conv.Spare",

            "_SpareBit_","_SpareBit_", //2 Spare bits - byte 3
            "Conv.BuckCL_A",
            "Conv.BuckCL_B",
            "Conv.NegCL_A",
            "Conv.NegCL_B",
            "Conv.CompA",
            "Conv.CompB",

            "_SpareBit_", //1 Spare bit - byte 4
            "Conv.BattleMode",
            "Conv.OutEnable",
            "Conv.EPO",
            "Conv.OV_Test",
            "Conv.BuckSS",
            "Conv.BoostSS",
            "Conv.InrushEN",

            "_SpareBit_","_SpareBit_","_SpareBit_","_SpareBit_","_SpareBit_","_SpareBit_", //6 Spare bits - byte 5
            "Conv.InrushEnCMD",
            "Conv.OutEnCMD",

            "Conv.AnIn.I_A", //byte 6
            "Conv.AnIn.I_B",
            "Conv.AnIn.Temp",
            "Conv.AnIn.Vout",
            "Conv.AnIn.Vrtn",
            "Conv.AnIn.vcc3_3", //byte 16
        };

        private const int CONV_TEL_LENGTH = 16;
        private void decodeConvTel(DataBuffer buffer, CommAddress source)
        {
            decodeTelList(buffer, BuckTelFields);
        }


        public const int DEBUG_TEL_LENGTH = CHARGER_STATUS_TEL_LENGTH + MASTER_TEL_LENGTH + CONV_TEL_LENGTH + 3;
        public void decodeDebugTel(byte[] data)
        {
            //decode debug tel including Opcodes
            DataBuffer buffer = new DataBuffer(data);

            int OpCode = buffer.getByte();
            if (OpCode == 0x81)
            {
                //decodeHPFStatusTel(buffer);
            }
            else
            {
                log.Add(data, "Debug Tel Error", LogedStatus.LsError);
                return;
            }

            OpCode = buffer.getByte();
            if (OpCode == 0x82)
            { 
                //decodeIsoTel(buffer, CommAddress.Non);
            }
            else
            {
                log.Add(data, "Debug Tel Error", LogedStatus.LsError);
                return;
            }

            OpCode = buffer.getByte();
            if (OpCode == 0x83)
            {
                decodeConvTel(buffer, CommAddress.Non);
            }
            else
            {
                log.Add(data, "Debug Tel Error", LogedStatus.LsError);
                return;
            }

            log.Add(data, "Debug Tel", LogedStatus.LsTelemetry);
            db.UpdateClients();
        }


        private string[] ControlTelFields = {
            "Control.Out.InputRelayEN",
            "Control.Out.Interlock_OUT",
            "Control.Out.FAN_24V_EN",
            "Control.Out.HPF_PWM_EN",
            "Control.In.Interlock_In",
            "Control.In.FP_SW_SNS",
            "_SpareBit_","_SpareBit_",
            "Control.IDCode",
            "Control.FPTemp",
            "Control.Vcc33",
            "Control.AuxA",
            "Control.AuxB",
            "Control.Fan24v",
            "Control.FanTechometer",
            "Control.HotSpot",
            "Control.FanSpeed",
            "Control.testTemp",
            "Control.VoltageCorrection",
            "Control.VoltageTrimming"};
        
        private const int CONTROL_TEL_LENGTH = 21;
        public void decodeControlDebugTel(byte[] data)
        {
            if (data.Length < CONTROL_TEL_LENGTH)
            {
                log.Add(data, "wrong length", LogedStatus.LsError);
                return;
            }
            DataBuffer buffer = new DataBuffer(data);
            decodeTelList(buffer, ControlTelFields);
            log.Add(data, "Debug Tel", LogedStatus.LsTelemetry);
            db.UpdateClients();
        }

        public string mainUserTelDecodeDisplay()
        {
            return telDecodeDisplay(BuckTelFields, true, false, 1);
        }

        private const int SOFT_VERSION_TEL_LENGTH = 10;
        private void decodeSoftVersionTel(DataBuffer buffer, CommAddress source)
        {
            switch (source)
            {
                case CommAddress.Charger:
                    db["Charger.Version.Major"].Decode(buffer);
                    db["Charger.Version.Minor"].Decode(buffer);
                    buffer.getShort();
                    db["Charger.Version.String"].Decode(buffer);
                    break;
                case CommAddress.Master:
                    db["Master.Version.Major"].Decode(buffer);
                    db["Master.Version.Minor"].Decode(buffer);
                    buffer.getShort();
                    db["Master.Version.String"].Decode(buffer);
                    break;

            }
            db.UpdateClients();
        }


        private string telDecodeDisplay(string[] telList, bool withValues, bool descOnly, int byteOffest)
        {
            string decodeDisplay = "";
            int byteCounter = byteOffest, bitCounter = 0;

            
            for (int i = 0; i < telList.Length; i++)
            {
                if (telList[i] == "_SpareByte_")
                {
                    if (bitCounter != 0)
                        throw new Exception("not alined spare byte");
                    decodeDisplay += fieldIndex(byteCounter, bitCounter, 8) + "\tSpare Byte" + Environment.NewLine;
                    byteCounter++;
                }
                else if (telList[i] == "_SpareBit_")
                {
                    decodeDisplay += fieldIndex(byteCounter, bitCounter, 1) + "\tSpare Bit" + Environment.NewLine;
                    bitCounter++;
                }
                else
                {
                    IDecoder decoder = db[telList[i]].GetDecoder("");

                    if (decoder.IsFixedSize())
                    {
                        string str = fieldIndex(byteCounter, bitCounter, decoder.FieldSize) + "\t";

                        if (descOnly)
                            str += db[telList[i]].Description;
                        else 
                            str += telList[i];

                        if (withValues)
                            str += "\t" + db[telList[i]].RowDataString + "\t" + db[telList[i]].ValueToString();
                        str += Environment.NewLine;
                        bitCounter += decoder.FieldSize;

                        decodeDisplay += str;
                    }
                    else
                        throw new Exception("telemetry contains not a fixed size field");

                }

                if (bitCounter >= 8)
                {
                    byteCounter += bitCounter / 8;
                    bitCounter = bitCounter%8;
                }
            }

            return decodeDisplay;

        }

        private string fieldIndex(int byteCounter, int bitCounter, int fieldSize)
        {
            string index;

            if (fieldSize < 1)
                throw new Exception("Field Size must be a positive number");

            int endByte = byteCounter + (bitCounter + fieldSize - 1) / 8;
            int endBit = (bitCounter + fieldSize -1) % 8;

            if (bitCounter == 0 && endBit==7)
            {
                //byte alined field

                if (fieldSize == 8) //single byte
                    index = byteCounter.ToString();
                else
                    index = byteCounter.ToString() + "-" + endByte.ToString();
            }
            else
            {
                if (fieldSize == 1) //single bit
                    index = byteCounter.ToString() + "(" + bitCounter.ToString() + ")";
                else if (bitCounter + fieldSize <= 8) //number of bits in single byte
                    index = byteCounter.ToString() + "(" + bitCounter.ToString() + "-" + endBit.ToString() + ")";
                else
                    index = byteCounter.ToString() + "(" + bitCounter.ToString() + ")-" +
                            endByte.ToString() + "(" + endBit.ToString();
            }
            return index;
        }

        private void decodeTelList(DataBuffer buffer, string[] telList)
        {
            for (int i = 0; i < telList.Length; i++)
            {
                if (telList[i] == "_SpareByte_")
                    buffer.getByte();
                else if(telList[i] == "_SpareBit_")
                    buffer.getBits(1);
                else
                    db[telList[i]].Decode(buffer);
            }
        }


        private void decodeArrayTel(DataBuffer buffer)
        {
            mainForm.decodeArrayTel(buffer);
        }


        private string[] ESRFields = {
            "SCPI.ESR.PowerOn",
            "_SpareBit_",
            "SCPI.ESR.CommandError",
            "SCPI.ESR.ExecutionError",
            "_SpareBit_",
            "SCPI.ESR.QueryError",
            "_SpareBit_","_SpareBit_"
             };


        private string[] STBFields = {
            "_SpareBit_",
            "_SpareBit_",
            "SCPI.STB.ESB",
            "_SpareBit_",
            "_SpareBit_",
            "SCPI.STB.ErrorQueue",
            "SCPI.STB.ProtectionEventFlag",
            "_SpareBit_"};

        private string[] PCRFields, PERFields;

        private void initPCRFields()
        {
            if (PCRFields != null) return;

            PCRFields = new string[16];
            PERFields = new string[16];
            for (int i = 0; i < 2; i++)
            {
                string s = (i == 0) ? "SCPI.PCR." : "SCPI.PER.";
                string[] fields = (i == 0) ? PCRFields : PERFields;
                int j = 0;
                fields[j++] = "_SpareBit_";
                fields[j++] = "_SpareBit_";
                fields[j++] = "_SpareBit_";
                fields[j++] = "_SpareBit_";
                fields[j++] = s + "MarginalProtectionSetting";
                fields[j++] = s + "InternalFailure";
                fields[j++] = s + "CommWatchdog";
                fields[j++] = s + "ACMissingPhase";
                fields[j++] = s + "ACUnderVoltage";
                fields[j++] = s + "OpenInterlock";
                fields[j++] = s + "FanFailure";
                fields[j++] = s + "OverTempShutdown";
                fields[j++] = s + "OutputUnderVoltage";
                fields[j++] = s + "OutputRegulationFailure";
                fields[j++] = s + "OutputOverCurrent";
                fields[j++] = s + "OutputOverVoltage";
            }
        }


        private string[] SDReasonFields;

        private void initSDReasonFields()
        {
            if (SDReasonFields != null) return;
            List<string> fields = new List<string>();

            fields.Add("_SpareBit_");
            fields.Add("_SpareBit_");
            fields.Add("_SpareBit_");
            fields.Add("_SpareBit_");
            fields.Add("SD_Reason.MCU.EnableCommand");
            fields.Add("SD_Reason.MCU.PowerSwitch");
            fields.Add("SD_Reason.MCU.Interlock");
            fields.Add("SD_Reason.MCU.Reset");

            fields.Add("_SpareBit_");
            fields.Add("_SpareBit_");
            fields.Add("_SpareBit_");
            fields.Add("SD_Reason.MCU.OverCurent");
            fields.Add("SD_Reason.MCU.OverVoltage");
            fields.Add("SD_Reason.MCU.OverTemp");
            fields.Add("SD_Reason.MCU.OverLoad");
            fields.Add("SD_Reason.MCU.CommWD");

            fields.Add("_SpareBit_");
            fields.Add("_SpareBit_");
            fields.Add("_SpareBit_");
            fields.Add("_SpareBit_");
            fields.Add("_SpareBit_");
            fields.Add("SD_Reason.MCU.VersionsFail");
            fields.Add("SD_Reason.MCU.InterlockFail");
            fields.Add("SD_Reason.MCU.IntComProblem");

            for (int i = 0; i < 2; i++)
            {
                string s = "SD_Reason.HPF[" + i.ToString() + "].";
                fields.Add(s + "Ready");
                fields.Add("_SpareBit_"); fields.Add("_SpareBit_"); fields.Add("_SpareBit_"); fields.Add("_SpareBit_");
                fields.Add("_SpareBit_"); fields.Add("_SpareBit_"); fields.Add("_SpareBit_");

                fields.Add(s + "Sync");
                fields.Add(s + "InOk");
                fields.Add("_SpareBit_");
                fields.Add(s + "Inruch");
                fields.Add(s + "OverVoltage");
                fields.Add(s + "UvccAux");
                fields.Add(s + "BusLow");
                fields.Add(s + "BusUV");

                s = "SD_Reason.ISO[" + i.ToString() + "].";
                fields.Add(s + "Ready");
                fields.Add("_SpareBit_"); fields.Add("_SpareBit_"); fields.Add("_SpareBit_"); fields.Add("_SpareBit_");
                fields.Add("_SpareBit_"); fields.Add("_SpareBit_"); fields.Add("_SpareBit_");

                fields.Add("_SpareBit_");
                fields.Add("_SpareBit_");
                fields.Add(s + "HpfNotReady");
                fields.Add(s + "OverVoltage");
                fields.Add(s + "UvccAux");
                fields.Add(s + "WakeUpTimeOut");
                fields.Add(s + "TestModeFailure");
                fields.Add(s + "CurrentFault");

                s = "SD_Reason.Buck[" + i.ToString() + "].";
                fields.Add(s + "Ready");
                fields.Add("_SpareBit_"); fields.Add("_SpareBit_"); fields.Add("_SpareBit_"); fields.Add("_SpareBit_");
                fields.Add("_SpareBit_"); fields.Add("_SpareBit_"); fields.Add("_SpareBit_");

                fields.Add("_SpareBit_");
                fields.Add(s + "OverVoltage");
                fields.Add(s + "OverCurrent");
                fields.Add(s + "UvccAux");
                fields.Add(s + "IsoNotReady");
                fields.Add(s + "ADCFail");
                fields.Add(s + "CurrentFault");
                fields.Add(s + "CommWatchdog");
            }

            SDReasonFields = fields.ToArray();
        }

        public void decodeScpiResponce(string cmd, string Responce)
        {
            initPCRFields();
            initSDReasonFields();
            DataBuffer buffer = new DataBuffer();
            int val;
            byte[] data;

            //remove new line from Responce string
            if (Responce.Length > 2)
                if (Responce[Responce.Length - 2] == '\r' && Responce[Responce.Length - 1] == '\n')
                    Responce = Responce.Substring(0, Responce.Length - 2);

            try
            {
                switch (cmd.ToUpper())
                {
                    case "*ESR?":
                        val = int.Parse(Responce);
                        buffer.fillByte(val);
                        buffer.Offset = 0;
                        decodeTelList(buffer, ESRFields);
                        break;

                    case "*STB?":
                        val = int.Parse(Responce);
                        buffer.fillByte(val);
                        buffer.Offset = 0;
                        decodeTelList(buffer, STBFields);
                        break;

                    case "STAT:PROT:COND?":
                        val = int.Parse(Responce);
                        buffer.fillShort((uint)val);
                        buffer.Offset = 0;
                        decodeTelList(buffer, PCRFields);
                        break;

                    case "STAT:PROT:EVEN?":
                        val = int.Parse(Responce);
                        buffer.fillShort((uint)val);
                        buffer.Offset = 0;
                        decodeTelList(buffer, PERFields);
                        break;

                    case "SYST:ERR?":
                        buffer.fillString(Responce, 100);
                        buffer.Offset = 0;
                        db["SCPI.ERR"].Decode(buffer);
                        break;

                    case "*IDN?":
                        buffer.fillString(Responce, 100);
                        buffer.Offset = 0;
                        db["SCPI.IDN"].Decode(buffer);
                        break;

                    case "MEAS:VOLT?":
                        buffer.fillString(Responce, 100);
                        buffer.Offset = 0;
                        db["SCPI.Vout"].Decode(buffer);
                        break;

                    case "MEAS:CURR?":
                        buffer.fillString(Responce, 100);
                        buffer.Offset = 0;
                        db["SCPI.Iout"].Decode(buffer);
                        break;

                    case "SOUR:CCORCVMODE?":
                        buffer.fillString(Responce, 100);
                        buffer.Offset = 0;
                        db["SCPI.Reg"].Decode(buffer);
                        break;

                    case "SOUR:OPMODE?":
                        buffer.fillString(Responce, 100);
                        buffer.Offset = 0;
                        db["SCPI.Mode"].Decode(buffer);
                        break;

                    case "OUTP:SDRES?":
                        buffer.fillHexStr(Responce.Replace(" ",""), 0);
                        buffer.Offset = 0;
                        decodeTelList(buffer, SDReasonFields);
                        break;


                    case "DBG1?":
                    case "DBG2?":
                        if (Responce.StartsWith("DBG1"))
                            mainForm.moduleLabel.Text = "Module A";
                        else if (Responce.StartsWith("DBG2"))
                            mainForm.moduleLabel.Text = "Module B";
                        else
                        {
                            log.Add(Responce, "Wrong DBG1/2 TCP Responce", LogedStatus.LsTelemetryError);
                            return;
                        }

                        data = System.Convert.FromBase64String(Responce.Substring(4));
                        log.Add(Responce, "TCP Responce", LogedStatus.LsTelemetry);
                        decodeDebugTel(data);
                        return;

                    case "DBG3?":
                        if (!Responce.StartsWith("DBG3"))
                        {
                            log.Add(Responce, "Wrong DBG3 TCP Responce", LogedStatus.LsTelemetryError);
                            return;
                        }

                        data = System.Convert.FromBase64String(Responce.Substring(4));
                        log.Add(Responce, "TCP Responce", LogedStatus.LsTelemetry);
                        decodeControlDebugTel(data);
                        return;

                    default:
                        log.Add(Responce, "Unknown TCP Responce", LogedStatus.LsTelemetryError);
                        return;
                }
                db.UpdateClients();
                log.Add(Responce, "TCP Responce", LogedStatus.LsTelemetry);
            }
            catch (Exception ex)
            {
                log.Add(Responce, "TCP Responce - Decode Error", LogedStatus.LsTelemetryError);
            }

        }
    }


    public delegate void TelDecodeFunction(DataBuffer buffer, CommAddress source = CommAddress.Non);
    struct TelDecodeMap
    {
        public uint opCode;
        public string description;
        public TelDecodeFunction decodeFunction;
        public int minLength;

        public TelDecodeMap(uint opCode, string description, TelDecodeFunction decodeFunction, int minLength)
        {
            this.opCode = opCode;
            this.description = description;
            this.decodeFunction=decodeFunction;
            this.minLength = minLength;
        }

    }
}

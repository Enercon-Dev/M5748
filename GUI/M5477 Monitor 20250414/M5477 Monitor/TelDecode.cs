using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS_Tools;

namespace M1787_Monitor
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
               // new TelDecodeMap(0x81, "HPF Status Tel", decodeHPFStatusTel, HPF_STATUS_TEL_LENGTH),
               new TelDecodeMap(0x81, "HPF Status Tel", decodeHPFStatusTel, 10),

                new TelDecodeMap(0x82, "ISO Status Tel", decodeIsoTel, ISO_TEL_LENGTH),
                new TelDecodeMap(0x83, "Buck Status Tel", decodeBuckTel, BUCK_TEL_LENGTH),

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
                    decode.decodeFunction(buffer);
                    db.UpdateClients();
                }
                return;
            }

            frame.Description = "unknown tel";
            log.Add(frame, LogedStatus.LsError);
        }


        private string[] HPFStatusTelFields = {
            "Version.Major",
            "Version.Minor",
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


        private const int HPF_STATUS_TEL_LENGTH = 26;
        private void decodeHPFStatusTel(DataBuffer buffer)
        {
            //buffer.BigEndian = false;
            decodeTelList(buffer, HPFStatusTelFields);
            mainForm.NextTelRequests(1);
        }

        private string[] IsoTelFields = {
            "ISO.Version.Major",
            "ISO.Version.Minor",
            "_SpareBit_","_SpareBit_","_SpareBit_","_SpareBit_", //4 Spare bits
            "ISO.CurrentFault",
            "ISO.UVCC_High",
            "ISO.HPF_Ready",
            "ISO.OV",
            "ISO.AnIn.Viso",
            "ISO.AnIn.Iprimary",
            "ISO.AnIn.Temp1",
            "ISO.AnIn.Temp2",
            "ISO.AnIn.Iprimary2",
            "_SpareBit_","_SpareBit_","_SpareBit_", //3 Spare bits
            "ISO.ISO_Ready",
            "ISO.ISO_Enable",
            "ISO.CurrentFaultLock",
            "ISO.OVLock",
            "ISO.WakeUpTimeOut",
            "ISO.SHDNReason"};


        private const int ISO_TEL_LENGTH = 14;
        private void decodeIsoTel(DataBuffer buffer)
        {
            //buffer.BigEndian = false;
            decodeTelList(buffer, IsoTelFields);
            mainForm.NextTelRequests(2);
        }

        private string[] BuckTelFields = {
            "BUCK.Version.Major", "BUCK.Version.Minor", //1
            "_SpareBit_","_SpareBit_","_SpareBit_", //3 Spare bits //2
            "BUCK.CurrentFault",
            "BUCK.UVCC_AUX",
            "BUCK.rowISO_Ready",
            "BUCK.Comp2",
            "BUCK.Comp1",
            "_SpareBit_","_SpareBit_","_SpareBit_","_SpareBit_","_SpareBit_","_SpareBit_", //6 Spare bits //3
            "BUCK.ISO_Ready",
            "BUCK.EnableCMD",
            "BUCK.VoutCMD", //4
            "BUCK.IoutCMD", //6
            "BUCK.OverVoltageCMD", //8
            "BUCK.OverCurrentCMD", //10
            "BUCK.AnIn.Vin", //12
            "BUCK.AnIn.Iout", //14
            "BUCK.AnIn.Temp", //16
            "BUCK.AnIn.PTCTemp", //18
            "BUCK.AnIn.vcc3_3", //20
            "BUCK.AnIn.Vout", //22
            "BUCK.IoutDac", //24
            "BUCK.CommWD", //26
            "BUCK.CurrentFaultSD",
            "BUCK.CCMode",
            "BUCK.BuckOCLock",
            "BUCK.BuckOVLock",
            "BUCK.BuckReady",
            "BUCK.ADCFail",
            "BUCK.BuckEnable",
            "BUCK.SHDNReason"}; //27


        private const int BUCK_TEL_LENGTH = 27;
        private void decodeBuckTel(DataBuffer buffer)
        {
            //buffer.BigEndian = false;
            decodeTelList(buffer, BuckTelFields);
        }


        public const int DEBUG_TEL_LENGTH = HPF_STATUS_TEL_LENGTH + ISO_TEL_LENGTH + BUCK_TEL_LENGTH + 3;
        public void decodeDebugTel(byte[] data)
        {
            //decode debug tel including Opcodes
            DataBuffer buffer = new DataBuffer(data);

            int OpCode = buffer.getByte();
            if (OpCode == 0x81)
            {
                decodeHPFStatusTel(buffer);
            }
            else
            {
                log.Add(data, "Debug Tel Error", LogedStatus.LsError);
                return;
            }

            OpCode = buffer.getByte();
            if (OpCode == 0x82)
            { 
                decodeIsoTel(buffer);
            }
            else
            {
                log.Add(data, "Debug Tel Error", LogedStatus.LsError);
                return;
            }

            OpCode = buffer.getByte();
            if (OpCode == 0x83)
            {
                decodeBuckTel(buffer);
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


    public delegate void TelDecodeFunction(DataBuffer buffer);
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

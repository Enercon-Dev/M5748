using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS_Tools;

namespace M1787_Monitor
{
    public class Commands
    {
        private Log log;
        private MainForm mainForm;
        private const UInt32 CalibPassword = 0x0D4BBCC9;

        public Commands(Log log, MainForm mainForm)
        {
            this.log = log;
            this.mainForm = mainForm;
        }

        private void RequestTelCommand(int telOpCode, byte[] parameters, string description)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(telOpCode);
            buffer.fillArray(parameters);
            mainForm.Send(buffer, description);
        }

        private void RequestTelCommand(int telOpCode, string description)
        {
            RequestTelCommand(telOpCode, new byte[] { }, description);
        }


        public void geHpfStatusTel()
        {
            RequestTelCommand(0x81, "Get Status Tel");
        }

        public void SetOutputControl(bool[] outputs)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0x84);
            for (int i = 0; i < outputs.Length; i++)
                buffer.fillBit(outputs[i]);
            mainForm.Send(buffer, "Output Control Command");
        }

        public void getConfigurationTel()
        {
            RequestTelCommand(0x42, "get Configuration Tel");
        }


        public void getArrayTel()
        {
            RequestTelCommand(0x8D01, "get Array Tel"); //request to master
        }


        public void getIsoStatusTel()
        {
            RequestTelCommand(0x02, "get ISO Status Tel");
        }


        public void setSettingsCommand(SettingsCmdParam param)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0x07);
            for (int i = 0; i < 7; i++) buffer.fillBit(false);
            buffer.fillBit(param.OutputEnable);
                        
            double AFactor = (mainForm.db["BUCK.VoutCMD"].GetDecoder("") as AnalogDecoder).AFactor;
            double BFactor = (mainForm.db["BUCK.VoutCMD"].GetDecoder("") as AnalogDecoder).BFactor;
            buffer.fillShort((uint)((param.Vout - BFactor) / AFactor)); //VoutCMD - module A
            buffer.fillShort((uint)((param.Vout - BFactor) / AFactor)); //VoutCMD - module B

            AFactor = (mainForm.db["BUCK.IoutCMD"].GetDecoder("") as AnalogDecoder).AFactor;
            BFactor = (mainForm.db["BUCK.IoutCMD"].GetDecoder("") as AnalogDecoder).BFactor;
            buffer.fillShort((uint)((param.Iout- BFactor) / AFactor)); //IoutCMD

            AFactor = (mainForm.db["BUCK.OverCurrentCMD"].GetDecoder("") as AnalogDecoder).AFactor;
            BFactor = (mainForm.db["BUCK.OverCurrentCMD"].GetDecoder("") as AnalogDecoder).BFactor;
            buffer.fillShort((uint)((param.Iout - BFactor) / AFactor)); //Iout_meas

            AFactor = (mainForm.db["BUCK.OverVoltageCMD"].GetDecoder("") as AnalogDecoder).AFactor;
            BFactor = (mainForm.db["BUCK.OverVoltageCMD"].GetDecoder("") as AnalogDecoder).BFactor;
            buffer.fillShort((uint)((param.OverVoltage - BFactor) / AFactor)); //Over Voltage Command

            AFactor = (mainForm.db["BUCK.OverCurrentCMD"].GetDecoder("") as AnalogDecoder).AFactor;
            BFactor = (mainForm.db["BUCK.OverCurrentCMD"].GetDecoder("") as AnalogDecoder).BFactor;
            buffer.fillShort((uint)((param.OverCurrent - BFactor) / AFactor)); //Over Current Command

            mainForm.Send(buffer, "BUCK Settings Command");
        }

        public void SetSerialNumberCommand(uint sn)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0x11);
            buffer.fillLong(0x3F235b53);
            buffer.fillShort(sn);

            mainForm.Send(buffer, "Set Serial Numabe Command");
        }

        public void outputEnCommand(bool enable)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0x01);
            buffer.fillByte(enable ? 0x01 : 0x00);


            mainForm.Send(buffer, "Output On/Off Command");
        }

        public void FaultSignalCommand(bool set, bool unset)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0x03);

            if (set)
                buffer.fillByte(0x01);
            else if (unset)
                buffer.fillByte(0x02);
            else
                buffer.fillByte(0x00);

            mainForm.Send(buffer, "Fault Signal Command");
        }




        public void getStatTel()
        {
            RequestTelCommand(0x55, "get Statistics Tel");
        }



    }

    public struct SettingsCmdParam
    {
        public bool OPMode;
        public bool OutputEnable;
        public double Vout;
        public double Iout;
        public double OverVoltage;
        public double OverCurrent;

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS_Tools;

namespace M5748_Monitor
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

        private void RequestTelCommand(int telOpCode, byte[] parameters, string description, CommAddress destination)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0xF0);
            buffer.fillByte(telOpCode);
            buffer.fillArray(parameters);
            mainForm.Send(buffer, description, (int)destination);
        }

        private void RequestTelCommand(int telOpCode, string description, CommAddress destination)
        {
            RequestTelCommand(telOpCode, new byte[] { }, description, destination);
        }


        public void getHpfStatusTel()
        {
            RequestTelCommand(0x83, "Get Status Tel",CommAddress.Batt2);
        }

        public void getChargerStatusTel()
        {
            RequestTelCommand(0x81, "Get Charger Status Tel", CommAddress.Charger);
        }

        public void convCommand(bool inrushEnable, bool enA, bool enB)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0x04);
            for (int i = 0; i < 5; i++)
                buffer.fillBit(false); //bits 3-7
            buffer.fillBit(inrushEnable); //bit 2
            buffer.fillBit(enB); //bit 1 
            buffer.fillBit(enA); //bit 0

            mainForm.Send(buffer, "Boost-Buck Command", (int)CommAddress.ConvBroadcast);
        }

        public void getVersionTel(CommAddress address)
        {
            RequestTelCommand(0x8F, "Get Version Tel", address);
        }

        public void setChargerOutputs(bool outOk, bool isoFault, bool fault, bool spare)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0x82);
            for (int i = 0; i < 22; i++)
                buffer.fillByte(0);
            buffer.fillBit(outOk);
            buffer.fillBit(isoFault);
            buffer.fillBit(fault);
            buffer.fillBit(fault);
            buffer.fillBit(false);
            buffer.fillBit(spare);
            buffer.fillBit(false);
            buffer.fillBit(false);
            for (int i = 0; i < 22; i++)
                buffer.fillByte(0);

            mainForm.Send(buffer, "HPF Status Tel", (int)CommAddress.Charger);
        }


        public void SetOutputControl(bool[] outputs)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0x84);
            for (int i = 0; i < outputs.Length; i++)
                buffer.fillBit(outputs[i]);
            mainForm.Send(buffer, "Output Control Command", (int)CommAddress.Batt3);
        }

        public void getConfigurationTel()
        {
            RequestTelCommand(0x42, "get Configuration Tel", CommAddress.Non);
        }


        public void getArrayTel()
        {
            RequestTelCommand(0x8D01, "get Array Tel", CommAddress.Non); //request to master
        }


        public void getMasterStatusTel()
        {
            RequestTelCommand(0x82, "get Master Status Tel", CommAddress.Master);
        }


        public void SetSerialNumberCommand(uint sn)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0x11);
            buffer.fillLong(0x3F235b53);
            buffer.fillShort(sn);

            mainForm.Send(buffer, "Set Serial Numabe Command", (int)CommAddress.Non);
        }

        public void outputEnCommand(bool enable)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0x01);
            buffer.fillByte(enable ? 0x01 : 0x00);


            mainForm.Send(buffer, "Output On/Off Command", (int)CommAddress.Non);
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

            mainForm.Send(buffer, "Fault Signal Command", (int)CommAddress.Non);
        }




        public void getStatTel()
        {
            RequestTelCommand(0x55, "get Statistics Tel", (int)CommAddress.Non);
        }



    }

    public enum CommAddress { Host = 0x0F, Master = 0x0A, Charger = 0x0B, ConvR = 0x0C, ConvL = 0x0D, ConvBroadcast = 0x0E, Batt1 = 1, Batt2 = 2, Batt3 = 3, Batt4 = 4, Batt5 = 5, Batt6 = 6, Non = 0}

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

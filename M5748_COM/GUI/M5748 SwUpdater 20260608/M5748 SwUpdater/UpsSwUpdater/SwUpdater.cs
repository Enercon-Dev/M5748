using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS_Tools;
using M1787_Monitor; //for M1787Frame

namespace M5748SwUpdater
{
    class SwUpdater
    {
       
        SerialComm comm;
        private const string BattFirmewareVersion = "1.00";
        private const string MasterFirmewareVersion = "1.00";
        private const string ChargerFirmewareVersion = "1.00";
        private byte[] BattProgFile = M5748SwUpdater.Properties.Resources.M5748_Batt_v1_00;
        private byte[] MasterProgFile = M5748SwUpdater.Properties.Resources.M5748_Master_v1_00;
        private byte[] ChargerProgFile = M5748SwUpdater.Properties.Resources.M5748_Com_v1_00;
#warning Do not forget to change the "FirmewareVersion" string !!!

        SerialComm.CommAddress[] moduleAddr = new SerialComm.CommAddress[] { SerialComm.CommAddress.Master, SerialComm.CommAddress.Charger, SerialComm.CommAddress.Batt1, SerialComm.CommAddress.Batt2, SerialComm.CommAddress.Batt3, SerialComm.CommAddress.Batt4, SerialComm.CommAddress.Batt5, SerialComm.CommAddress.Batt6 };
        string[] moduleName = new string[] { "Master", "Charger", "Batt1", "Batt2", "Batt3", "Batt4", "Batt5", "Batt6" };
        int selectedModule = -1;
        private byte[] ProgFile = null;
        private string FirmewareVersion;

        public void Run()
        {
            comm = new SerialComm();
            try
            {
                //Console.WriteLine("-------Device Address 0xE6 -------------"); //defined in comm.OpenCan() for BAM update switch back to J1939_old.cs
                Console.WriteLine("This program is about to update the firmware of M5748");
                Console.WriteLine("    New Master Version " + MasterFirmewareVersion);
                Console.WriteLine("    New Charger Version " + ChargerFirmewareVersion);
                Console.WriteLine("    New Battery Version " + BattFirmewareVersion);
                OpenCommunicationDevice();
              //  ReadAllVersions();
                SelectModule();
                comm.destinationAddress = moduleAddr[selectedModule];
                comm.sourceAddress = SerialComm.CommAddress.Host;

                ReadCurrentVersion();
                Console.WriteLine(moduleName[selectedModule] + " firmware is about to update to version " + FirmewareVersion);
                Console.WriteLine("Are you sure you want to continue? [Y/N] ");
                
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.KeyChar != 'y' && key.KeyChar != 'Y')
                    return;
                Console.WriteLine("");

                UploadFile(ProgFile, ImageStartAddress, ImageLastAddress - ImageStartAddress);

                Console.WriteLine("Rebooting");
                cmd_ChangeFirmeware();
                System.Threading.Thread.Sleep(1000);

                string[] version = cmd_GetVersion();
                Console.WriteLine("New "+ moduleName[selectedModule] + " version is: " + version[0]);
                if (version[0] == FirmewareVersion)
                    Console.WriteLine("Firmware Updated Successfully");
                else
                    Console.WriteLine("Firmware Update Failed");

            }
            catch (CommFailExeption ex)
            {
                Console.WriteLine(""); Console.WriteLine("");
                Console.WriteLine("Command failed: " + ex.FailedCommandDescription);
                Console.WriteLine("   reason: " + ex.Message);
                Console.WriteLine("");
            }
            catch (VerifyFailExaption ex)
            {
                Console.WriteLine(""); Console.WriteLine("");
                Console.WriteLine(ex.Message);
                Console.WriteLine("");
            }
            finally
            {
                Console.CursorVisible = true;
                comm.Close();
            }
            
        }

        public void OpenCommunicationDevice()
        {
            while (true)
            {
                Console.Write("Enter a Com Port number or \"0\" for CAN bus: ");
                string comName = Console.ReadLine().Trim();

                if (comName == "0")
                {
                    try { comm.OpenCan(); }
                    catch
                    {
                        Console.WriteLine("Can not open CAN bus");
                        continue;
                    }
                    break;
                }


                if (comName.Length <= 3 || (comName.Substring(0, 3).ToUpper() != "COM"))
                    comName = "COM" + comName;

                try { comm.OpenPort(comName, 115200); }
                catch
                {
                    Console.WriteLine("Can not open the " + comName);
                    continue;
                }
                break;
            }
        }

        public void ReadAllVersions()
        {
            //read all versions but only if the comunication devise is set to CAN bus
            if (!comm.IsCanOpened())
                return;


            string[] version;
            try
            {
                //comm.TimeOut = 1000;
                comm.sourceAddress = SerialComm.CommAddress.Host;

                for (int i = 0; i < moduleAddr.Length; i++)
                {
                    try
                    {
                        comm.destinationAddress = moduleAddr[i];
                        version = cmd_GetVersion();
                        Console.WriteLine("Current " + moduleName[i] + " version is: " + version[0]);
                    }
                    catch
                    {
                        Console.WriteLine("Failed to read " + moduleName[i] + " version");
                    }
                }
                Console.WriteLine("");
                //comm.TimeOut = SerialComm.DefaultTimeOut;
            }
            catch
            {
                Console.WriteLine("Fail to communicate with the device");
                throw;
            }


        }

        public void SelectModule()
        {
            while (true)
            {
                Console.WriteLine("Select module to be updated.");
                Console.WriteLine("\"M\"-Master; \"C\"-Charger; \"1-6\"-Battery1-6");
                char key = char.ToUpper(Console.ReadKey().KeyChar);
                Console.WriteLine("");
                ProgFile = null;
                if (key == 'M')
                {
                    selectedModule = 0;
                    ProgFile = MasterProgFile;
                    FirmewareVersion = MasterFirmewareVersion;
                }
                else if (key == 'C')
                {
                    selectedModule = 1;
                    ProgFile = ChargerProgFile;
                    FirmewareVersion = ChargerFirmewareVersion;
                }
                else
                {
                    int index = -1;
                    int.TryParse(key.ToString(), out index);
                    if (index >= 1 && index <= 6)
                    {
                        selectedModule = 1 + index;
                        ProgFile = BattProgFile;
                        FirmewareVersion = BattFirmewareVersion;
                    }
                }

                if (ProgFile != null)
                    break;
            }
        }

        public void ReadCurrentVersion()
        {

            string[] version = cmd_GetVersion();
            Console.WriteLine("Current " + moduleName[selectedModule] + " version is: " + version[0]);
        }

        const uint FlashFirstAddress      = 0x08000000;
        //const uint BootVectorCopyAddress  = 0x08000400;
        //const uint BootVectorCopySize     = 1024;
        //const uint BootLoaderStartAddress = 0x08000800;
        //const uint BootloaderLastAddress  = 0x08004000 - 1;
        const uint ProgramStartAddress    = 0x08002000;
        const uint ProgramLastAddress     = 0x08010000 - 1;
        const uint ImageStartAddress = 0x08010000;
        const uint ImageLastAddress  = 0x0801E000 - 1;

        private bool UploadFile(byte[] file, uint startAddress, uint MaxSize)
        {
            Console.CursorVisible = false;
            //erase boot vector
            Console.Write("Erasing");
            cmd_EraseFlash();
            Console.Write("...");
            System.Threading.Thread.Sleep(2000);
            //for (uint i = BootVectorCopyAddress; i < BootVectorCopyAddress + BootVectorCopySize; i += EraseBlockSize)
            //{
            //    cmd_EraseBlock(i);
            //    Console.Write(".");
            //}

            //erase program
            /*
            for (uint i = startAddress; i < startAddress+MaxSize; i += EraseBlockSize)
            {
                cmd_EraseBlock(i);
                Console.Write(".");
            }*/
            Console.WriteLine("");
 
           
            Console.Write("Writing ");
            int cursor = Console.CursorLeft;
            if (startAddress - FlashFirstAddress + MaxSize < file.Length)
            {
                throw new ApplicationException("file too large");
            }

            //write the program
            for (uint addr = startAddress; addr < startAddress + file.Length; addr += WriteBlockSize)
            {
                //break;
                int writeSize = WriteBlockSize;
                int fileOffset = (int)(addr - startAddress);
                if (fileOffset + WriteBlockSize > file.Length)
                {
                    writeSize = file.Length - fileOffset;
                }

                cmd_WriteFirmwareDataBlock((uint)(addr), file, fileOffset, writeSize);
                //System.Threading.Thread.Sleep(100);
                Console.CursorLeft = cursor;
                int progress = (fileOffset + writeSize)*100 / file.Length;
                Console.Write(progress.ToString("D2") + "%  ");
            }
            Console.WriteLine("");


            Console.Write("Verifying ");
            cursor = Console.CursorLeft;

            //Verifying the program
            int[] received;
            for (uint addr = startAddress; addr < startAddress + file.Length; addr += WriteBlockSize)
            {

                int writeSize = WriteBlockSize;
                int fileOffset = (int)(addr - startAddress);
                if (fileOffset + WriteBlockSize > file.Length)
                {
                    writeSize = file.Length - fileOffset;
                }

                received = cmd_getFirmwareDataBlock(addr, (uint)writeSize);
                if (!ArrayContain(received, file,fileOffset))
                {
                    throw new VerifyFailExaption("0x" + addr.ToString("X8"));
                }
                
                Console.CursorLeft = cursor;
                int progress = (fileOffset + writeSize) * 100 / file.Length;
                Console.Write(progress.ToString("D2") + "%  ");
            }
            Console.WriteLine("");

            Console.Write("Finalizing");
            // write file lenght
            byte[] lengthData = new byte[4];
            uint lengthAddress = startAddress + MaxSize +1 - 4;
            //System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(lengthdata, file.Length);
            lengthData[0] = (byte)(file.Length >> 24);
            lengthData[1] = (byte)(file.Length >> 16);
            lengthData[2] = (byte)(file.Length >> 8);
            lengthData[3] = (byte)file.Length;
            cmd_WriteFirmwareDataBlock(lengthAddress, lengthData);
            Console.Write(".");

            //Verify length
            received = cmd_getFirmwareDataBlock(lengthAddress, 4);
            if (!ArrayContain(received, lengthData, 0))
            {
                throw new VerifyFailExaption("0x" + lengthAddress.ToString("X8"));
            }
            Console.Write(".");
            Console.WriteLine("");
            Console.CursorVisible = true;
            return true;
        }

        private bool ArrayContain(int[] a1, byte[] a2, int offset)
        {
            if (a1.Length + offset > a2.Length)
                return false;

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[offset + i])
                    return false;
            }

            return true;
        }


        private const int WriteBlockSize = 64;
        private const int EraseBlockSize = 1024;
        
        private void cmd_EraseFlash()
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0x13); //OpCode
            buffer.fillLong(0xBCE31F90);
            //buffer.fillLong(address);
            comm.Send(buffer, "Erase Firmware Flash");
        }


        private void cmd_WriteFirmwareDataBlock(uint address, byte[] data)
        {
            cmd_WriteFirmwareDataBlock(address, data, 0, data.Length);
        }

        private void cmd_WriteFirmwareDataBlock(uint address, byte[] data, int dataOffset, int length)
        {
            if (length > WriteBlockSize)
            {
                throw new ApplicationException("Max Block length is " + WriteBlockSize.ToString() + " bytes");
            }

            /*
            if (length % 4 != 0)
            {
                int new_length = ((int)((length + 3) / 4)) * 4;
                byte[] new_data = new byte[new_length];
                Array.Copy(data, new_data, length);
                for (int i = length; i < new_length; i++)
                {
                    new_data[i] = 0xFF;
                }
                length = new_length;
                data = new_data;
            }
            */
            int roundLength = ((int)((length + 3) / 4)) * 4;

            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0x12); //OpCode
            buffer.fillLong(0xBCE31F90); //Magic Number
            buffer.fillLong(address);
            buffer.fillShort((uint)roundLength);
            for (uint i = 0; i < length; i++)
            {
                buffer.fillByte(data[dataOffset + i]);
            }
            
            for (; length < roundLength; length++)
            {
                buffer.fillByte(0xFF);
            }

            for (int i = 0; i<2; i++)
            {
                DataBuffer received = comm.SendAndReceive(buffer, "Write Firmware block " + address.ToString("X8"));
                try
                {
                    VerifyWriteAck(received, address, length);
                    break;
                }
                catch { }
                System.Threading.Thread.Sleep(1000);
                comm.ClearRecived();
            }
            
        }


        private void VerifyWriteAck(DataBuffer buffer, uint address, int length)
        {
            if (buffer.RemainingBytes < 8)
                throw new WrongResponceExeption();

            if (buffer.getByte() != 0x92) //Write ACK OpCode
                throw new WrongResponceExeption();

            int errorCode = buffer.getByte();
            uint ackAddress = buffer.getLong();
            uint ackLength = buffer.getShort();

            if (errorCode != 1 || address != ackAddress || length != ackLength)
            {
                //throw new NackResponceExeption(errorOpCode, errorCode, errorDescription);
                throw new WrongResponceExeption();
            }

            return;
        }

        private int[] cmd_getFirmwareDataBlock(uint address, uint length)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0xF0); //OpCode
            buffer.fillByte(0x91);
            buffer.fillLong(0xBCE31F90); //Magic Number
            buffer.fillLong(address);

            int roundLength = ((int)((length + 3) / 4)) * 4;
            buffer.fillShort((uint)roundLength);

            DataBuffer received = comm.SendAndReceive(buffer, "Read Firmware block " + address.ToString("X8"));

            int receiveOpcode = received.getByte();
            uint receivedAddress = received.getLong();
            uint receivedLength = received.getShort();

            if ((receivedAddress != address) ||
               (receivedLength != roundLength))
                throw new WrongResponceExeption();

            if (received.RemainingBytes < receivedLength)
                throw new WrongResponceExeption();

            return received.getArray((int)length);
        }

        private void cmd_ChangeFirmeware()
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0x14); //OpCode
            buffer.fillLong(0xBCE31F90);

            int retries = comm.MaxRetries;
            comm.MaxRetries = 1;
            try{comm.Send(buffer, "Change Firmware");}
            catch (NoResponceExeption)
            {}
            finally
            {
                comm.MaxRetries = retries;
            }
            System.Threading.Thread.Sleep(1000);
        }

        private string[] cmd_GetVersion()
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0xF0);
            buffer.fillByte(0x8F);

            DataBuffer received = comm.SendAndReceive(buffer, "Get Version", false);

            int receiveOpcode = received.getByte();
            string[] version = new string[3];
            //received.getShort(); //serial number
            //for (int i=0; i<16; i++)
            //    received.getByte(); //fixed user str
            version[0] = received.getByte().ToString();
            version[0] += "." + received.getByte().ToString("D2");

            return version;
        }


    }

    class VerifyFailExaption : ApplicationException
    {
        public VerifyFailExaption() { }
        public VerifyFailExaption(string message) : base(message) { }
        public VerifyFailExaption(string message, Exception inner) : base(message, inner) { }

        public override string Message
        {
            get { return "Verify Fail " + base.Message; }
        }
    }
}

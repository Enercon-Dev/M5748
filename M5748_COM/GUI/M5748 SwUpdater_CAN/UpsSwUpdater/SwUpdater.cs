using M1787_Monitor; //for M1787Frame
using M5748SwUpdater;
using MPS_Tools;
using Peak.Can.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;

namespace M5748SWUpdater
{
    class SwUpdater
    {

        bool m_CAN_connected = false;
        J1939 j1939 = new J1939();

        SerialComm comm;
      

        private const string FirmewareVersion = "v0.03";
        private const string FirmewareVersionString = "M5748_COM " + FirmewareVersion;
        //private const string LoaderVersionStringStart = "M1787-1 BL "; 
        //private const string LoaderVersionString = LoaderVersionStringStart + "v0.01";
        //private byte[] BootLoaderFile = M1787SwUpdater.Properties.Resources.M1787_BL;
        private byte[] MainProgFile = M5748SwUpdater.Properties.Resources.M5748_COM_crc32;
#warning Do not forget to change the "FirmewareVersion" and "LoaderVersionString" strings !!!

        public void Run()
        {
            comm = new SerialComm();
            j1939.InitCanBus();

           j1939.CanReceive.OnMessage += (id, data) =>
            {
                j1939.ProcessFrame(id, data);
            };

            j1939.CanReceive.StartCanReceive();
            try
            {
                Console.WriteLine("This program is about to update the firmware of M5748_COM to " + FirmewareVersion);



                string[] version;
                try
                {
                    comm.TimeOut = 1000;
                    version = cmd_GetVersion();
                    comm.TimeOut = SerialComm.DefaultTimeOut;
                    Console.WriteLine("Current version is: " + version[0] + " " + version[1]);
                }
                catch
                {
                    Console.WriteLine("Fail to communicate with the device");
                    throw;
                    //comm.ClosePort();
                    //return;
                }

                Console.Write("Are you sure you want to continue? [Y/N] ");

                ConsoleKeyInfo key = Console.ReadKey();
                if (key.KeyChar != 'y' && key.KeyChar != 'Y')
                    return;
                Console.WriteLine("");

                //Console.Write("Enter the password: ");
                //comm.Password = Console.ReadLine(); //"weBPass";
                //Console.WriteLine("");


                Console.WriteLine("Writing the Program");

                UploadFile(MainProgFile, ImageStartAddress, ImageLastAddress - ImageStartAddress);
                Console.WriteLine("Rebooting");
                cmd_ChangeFirmeware();
                version = cmd_GetVersion();
                Console.WriteLine("New version is: " + version[0] + " " + version[1]);
                if (version[0] == FirmewareVersionString)
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
            comm.ClosePort();
        }


        const uint FlashFirstAddress = 0x08000000;
        //const uint BootVectorCopyAddress  = 0x08000400;
        //const uint BootVectorCopySize     = 1024;
        //const uint BootLoaderStartAddress = 0x08000800;
        //const uint BootloaderLastAddress  = 0x08004000 - 1;
        const uint ProgramStartAddress = 0x08002000;
        const uint ProgramLastAddress = 0x08010000 - 1;
        const uint ImageStartAddress = 0x08010000;
        const uint ImageLastAddress = 0x0801E000 - 1;

        private bool UploadFile(byte[] file, uint startAddress, uint MaxSize)
        {

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


            Console.Write("Writing");
            if (startAddress - FlashFirstAddress + MaxSize < file.Length)
            {
                throw new ApplicationException("file too large");
            }

            //write the program
            for (uint addr = startAddress; addr < startAddress + file.Length; addr += WriteBlockSize)
            {
              //  break; // skip writing for debug
                int writeSize = WriteBlockSize;
                int fileOffset = (int)(addr - startAddress);
                if (fileOffset + WriteBlockSize > file.Length)
                {
                    writeSize = file.Length - fileOffset;
                }

                cmd_WriteFirmwareDataBlock((uint)(addr), file, fileOffset, writeSize);
                //System.Threading.Thread.Sleep(100);
#warning "remove the delay after adding ACK"
                Console.Write(".");
            }
            Console.WriteLine("");


            Console.Write("Verifying");
            //Verifying boot vector copy with crc block
            //for (int i = 0; i < 1024; i += WriteBlockSize)
            //{
            //    int[] received = cmd_getFirmwareDataBlock((uint)(BootVectorCopyAddress + i), WriteBlockSize);
            //
            //    if (!ArrayContain(received, bootAndCrc, i))
            //    {
            //        throw new VerifyFailExaption("0x" + (BootVectorCopyAddress + i).ToString("X8"));
            //    }
            //    Console.Write(".");
            //}

            //Verifying the program
            int[] received;
            for (uint addr = startAddress; addr < startAddress + file.Length; addr += WriteBlockSize)
            {
               // break; // SKIP VERIFY FOR DEBUG
                int writeSize = WriteBlockSize;
                int fileOffset = (int)(addr - startAddress);
                if (fileOffset + WriteBlockSize > file.Length)
                {
                    writeSize = file.Length - fileOffset;
                }

                received = cmd_getFirmwareDataBlock(addr, (uint)writeSize);
                if (!ArrayContain(received, file, fileOffset))
                {
                    throw new VerifyFailExaption("0x" + addr.ToString("X8"));
                }
                Console.Write(".");
            }
            Console.WriteLine("");
            uint imageCrc = Crc32(file, file.Length);
            Console.WriteLine($"Image CRC = 0x{imageCrc:X8}");
            Console.Write("Finalizing");
            uint crcAddress = startAddress + (uint)file.Length;
            byte[] crcData = new byte[4];
            crcData[0] = (byte)(imageCrc >> 24);
            crcData[1] = (byte)(imageCrc >> 16);
            crcData[2] = (byte)(imageCrc >> 8);
            crcData[3] = (byte)(imageCrc);
            cmd_WriteFirmwareDataBlock(crcAddress, crcData);
            Console.Write(".");

            received = cmd_getFirmwareDataBlock(crcAddress, 4);
            if (!ArrayContain(received, crcData, 0))
            {
                throw new VerifyFailExaption("CRC write failed at 0x" + crcAddress.ToString("X8"));
            }
            Console.Write(".");
            // write file linght
            byte[] lengthData = new byte[4];
            uint totalLength = (uint)(file.Length + 4); // image + CRC
            uint lengthAddress = startAddress + MaxSize + 1 - 4;
            //System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(lengthdata, file.Length);
            lengthData[0] = (byte)(totalLength >> 24);
            lengthData[1] = (byte)(totalLength >> 16);
            lengthData[2] = (byte)(totalLength >> 8);
            lengthData[3] = (byte)totalLength;


            cmd_WriteFirmwareDataBlock(lengthAddress, lengthData);
            Console.Write(".");

            //Verify length
            received = cmd_getFirmwareDataBlock(lengthAddress, 4);
            if (!ArrayContain(received, lengthData, 0))
            {
                throw new VerifyFailExaption("0x" + lengthAddress.ToString("X8"));
            }
            Console.Write(".");

            return true;
        }
        private static uint Crc32(byte[] data, int length)
        {
            int i, j;
            uint crc, mask;

            crc = 0xFFFFFFFF;
            for (i = 0; i < length; i++)
            {
                crc = crc ^ data[i];
                for (j = 7; j >= 0; j--)
                {
                    mask = (uint)-(crc & 1);
                    crc = (crc >> 1) ^ (0xEDB88320 & mask);
                }
            }
            return ~crc;
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
        /*
        private byte[] buildBootAndCrc(byte[] file, uint startAddress, uint MaxSize)
        {
            //note: file start from the flash start - address 0x08000000
            uint fileMaxSize = startAddress - FlashFirstAddress + MaxSize;
            
            if (file.Length > fileMaxSize)
                throw new Exception("file size to large");
            
            
            const int CrcBlockSize = 1024/2;
         
            //calc CRC
            List<int> crc = new List<int>();
            byte[] CrcBuffer = new byte[CrcBlockSize];

            //first crc block is IntVector and the second not used mem block
            Array.Copy(file, 0, CrcBuffer, 0, CrcBlockSize);
            crc.Add(M1787Frame.CalcCRC(CrcBuffer));
            crc.Add(0);

            for (int i = (int)(BootVectorCopyAddress - FlashFirstAddress + BootVectorCopySize);
                 i < fileMaxSize; i += CrcBlockSize)
            {
                if (i + CrcBlockSize < file.Length)
                {
                    Array.Copy(file, i, CrcBuffer, 0, CrcBlockSize);
                }
                else
                {
                    for (int j = 0; j < CrcBlockSize; j++)
                    {
                        if (i + j < file.Length)
                            CrcBuffer[j] = file[i + j];
                        else
                            CrcBuffer[j] = 0xFF;
                    }
                    
                }

                crc.Add(M1787Frame.CalcCRC(CrcBuffer));
            }


            byte[] bootAndCrc = new byte[BootVectorCopySize];
            Array.Copy(file, bootAndCrc, 1024/2); //copy interrupts vector

            for (int i = 0; i < crc.Count; i++)
            {
                //note: the memory is Littel Endian
                bootAndCrc[1024 / 2 + i * 2] = (byte)(crc[i] & 0xFF);
                bootAndCrc[1024/2 + i * 2 + 1] = (byte)((crc[i] >> 8) & 0xFF);
            }

            return bootAndCrc;
        }
        */

        private const int WriteBlockSize = 64;
        private const int EraseBlockSize = 1024;

        private void cmd_EraseFlash()
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillByte(0xFB); //source and dest address
            buffer.fillByte(0x13); //OpCode
            buffer.fillLong(0xBCE31F90);
            //buffer.fillLong(address);
            //comm.Send(buffer, "Erase Firmware Flash");
            byte[] payload = buffer.GetDataBuffer();
            uint pgn = 0xEF00; // 
                               //j1939.SendAndWaitJ1939(
                               //    pgn,
                               //    payload,
                               //    expectedOpcode: 0x13,
                               //    timeoutMs: 1000,
                               //    retries: 3);
            uint canId = j1939.BuildCanId(pgn);
            j1939.SendLongMessage(pgn, payload);
            // ----------------------------
            // Parse
            // ----------------------------

          
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
            buffer.fillByte(0xFB);
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
            byte[] payload = buffer.GetDataBuffer();
            uint pgn = 0xEF00; // 
          byte[] response =  j1939.SendAndWaitJ1939(
                pgn,
                payload,
                expectedOpcode: 0x92,
                timeoutMs: 100,
                retries: 10);
            DataBuffer received = new DataBuffer(response);// comm.SendAndReceive(buffer, "Write Firmware block " + address.ToString("X8"));
            received.Offset = 6;
            VerifyWriteAck(received, address, length);
        }


        private void VerifyWriteAck(DataBuffer buffer, uint address, int length)
        {
            if (buffer.Length < 8)
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

            // DataBuffer received = comm.SendAndReceive(buffer, "Read Firmware block " + address.ToString("X8"));
            byte[] payload = buffer.GetDataBuffer();
            uint pgn = 0xEF23; // 
            byte[] response = j1939.SendAndWaitJ1939(
                pgn,
                payload,
                expectedOpcode: 0x91,
                timeoutMs: 1000,
                retries: 10);
            // ----------------------------
            // Parse
            // ----------------------------

            DataBuffer received = new DataBuffer(response);
            received.Offset = 6;
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
            //try { comm.Send(buffer, "Change Firmware"); }
            //catch (NoResponceExeption)
            //{ }
            //finally
            //{
            //    comm.MaxRetries = retries;
            //}
            //System.Threading.Thread.Sleep(1000);

            byte[] payload = buffer.GetDataBuffer();
            uint pgn = 0xEF23; // 
            uint canId = j1939.BuildCanId(pgn);
            j1939.SendLongMessage(canId, payload);
            // ----------------------------
            // Parse
            // ----------------------------

            //DataBuffer received = new DataBuffer(response);
            //received.Offset = 6;
            //int opcode = received.getByte();
        }
        private string[] cmd_GetVersion()
        {
            DataBuffer buffer = new DataBuffer();

            buffer.fillByte(0xFB);
            buffer.fillByte(0xF0);
            buffer.fillByte(0x8F);
            buffer.fillByte(0x8F);
            buffer.fillByte(0x8F);
            buffer.fillByte(0x8F);
            buffer.fillByte(0x8F);
            buffer.fillByte(0x8F);

            byte[] payload = buffer.GetDataBuffer();
            uint pgn = 0xEF23; //   id1 == 0xEF && DA == 0x23)
            byte[] response = j1939.SendAndWaitJ1939(
                pgn,
                payload,
                expectedOpcode: 0x8F,
                timeoutMs: 1000,
                retries: 10);
            // ----------------------------
            // Parse
            // ----------------------------
            
            DataBuffer received = new DataBuffer(response);
            received.Offset = 1;
            int opcode = received.getByte();

           

            string[] version = new string[3];

            version[0] = received.getByte().ToString();
            version[0] += "." + received.getByte().ToString();

            return version;
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
}

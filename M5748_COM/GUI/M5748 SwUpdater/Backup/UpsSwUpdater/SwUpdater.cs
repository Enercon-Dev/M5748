using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS_Tools;
using M339_Monitor; //for M339IntFrame

namespace M339SWUpdater
{
    class SwUpdater
    {
       
        SerialComm comm;
        //define DEV_MASTER/DEV_SLAVE1/DEV_SLAVE2 in ProjectPropertire->Build->Cond. Comp. sumbols
#if DEV_MASTER
        private const string FirmewareVersion = "v2.03";
        private const string FirmewareVersionString = "M339-1 Master " + FirmewareVersion;
        private const string LoaderVersionStringStart = "M339-1 Master BootLoader"; 
        private const string LoaderVersionString = LoaderVersionStringStart + " v1.01";
        private byte[] BootLoaderFile = M339SwUpdater.Properties.Resources.M339_Master_BL;
        private byte[] MainProgFile = M339SwUpdater.Properties.Resources.M339_Master;
        private string DevName = "Master";
#elif DEV_SLAVE1
        private const string FirmewareVersion = "v2.03";
        private const string FirmewareVersionString = "M339-1 Slave1 " + FirmewareVersion;
        private const string LoaderVersionStringStart = "M339-1 Slave1 BootLoader"; 
        private const string LoaderVersionString = LoaderVersionStringStart + " v1.01";
        private byte[] BootLoaderFile = M339SwUpdater.Properties.Resources.M339_Slave1_BL;
        private byte[] MainProgFile = M339SwUpdater.Properties.Resources.M339_Slave1;
        private string DevName = "Slave1";
#elif DEV_SLAVE2
        private const string FirmewareVersion = "v2.03";
        private const string FirmewareVersionString = "M339-1 Slave2 " + FirmewareVersion;
        private const string LoaderVersionStringStart = "M339-1 Slave2 BootLoader"; 
        private const string LoaderVersionString = LoaderVersionStringStart + " v1.01";
        private byte[] BootLoaderFile = M339SwUpdater.Properties.Resources.M339_Slave2_BL;
        private byte[] MainProgFile = M339SwUpdater.Properties.Resources.M339_Slave2;
        private string DevName = "Slave2";
#else
#error "wrong device definition"
#endif
#warning Do not forget to change the "FirmewareVersion" and "LoaderVersionString" strings !!!

        public void Run()
        {
            comm = new SerialComm();
            try
            {
                Console.WriteLine("This program is about to update the firmware of M339-1 " + DevName + " to " + FirmewareVersion);

                while (true)
                {
                    //open the com
                    Console.Write("Enter a Com Port number: ");
                    string comName = Console.ReadLine();
                    comName.Trim();
                    if (comName.Length <= 3 || (comName.Substring(0, 3).ToUpper() != "COM"))
                        comName = "COM" + comName;

                    try { comm.OpenPort(comName, 19200); }
                    catch 
                    {
                        Console.WriteLine("Can not open the " + comName);
                        continue;
                    }
                    break;
                }

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

                bool recovering = (version[0] == LoaderVersionString);
                if (version[0].StartsWith(LoaderVersionStringStart) && !recovering)
                {
                    Console.WriteLine("Unknown Loader Detected - aborting.");
                    System.Threading.Thread.Sleep(3000);
                    return;
                }
                
                if (recovering)
                    Console.WriteLine("Recovering from aborted Upgrade");

                Console.Write("Are you sure you want to continue? [Y/N] ");
                
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.KeyChar != 'y' && key.KeyChar != 'Y')
                    return;
                Console.WriteLine("");

                //Console.Write("Enter the password: ");
                //comm.Password = Console.ReadLine(); //"weBPass";
                //Console.WriteLine("");


                if (!recovering)
                {
                    
                    Console.WriteLine("Writing the Loader");
                    UploadFile(BootLoaderFile, BootLoaderStartAddress, BootloaderLastAddress - BootLoaderStartAddress);
                    Console.WriteLine("Rebooting");
                    cmd_ChangeFirmeware();
                    
                    version = cmd_GetVersion();
                    if (version[0] != LoaderVersionString)
                    {
                        Console.WriteLine("Loader Upload Failed");
                        return;
                    }
                }

                Console.WriteLine("Writing the Program");
                UploadFile(MainProgFile, ProgramStartAddress, ProgramLastAddress - ProgramStartAddress);
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


        const uint FlashFirstAddress      = 0x08000000;
        const uint BootVectorCopyAddress  = 0x08000400;
        const uint BootVectorCopySize     = 1024;
        const uint BootLoaderStartAddress = 0x08000800;
        const uint BootloaderLastAddress  = 0x08004000 - 1;
        const uint ProgramStartAddress    = 0x08004000;
        const uint ProgramLastAddress     = 0x08020000 - 1;

        private bool UploadFile(byte[] file, uint startAddress, uint MaxSize)
        {
            //erase boot vector
            Console.Write("Erasing");
            for (uint i = BootVectorCopyAddress; i < BootVectorCopyAddress + BootVectorCopySize; i += EraseBlockSize)
            {
                cmd_EraseBlock(i);
                Console.Write(".");
            }

            //erase program
            for (uint i = startAddress; i < startAddress+MaxSize; i += EraseBlockSize)
            {
                cmd_EraseBlock(i);
                Console.Write(".");
            }
            Console.WriteLine("");
 
           
            Console.Write("Writing");
            if (startAddress - FlashFirstAddress + MaxSize < file.Length)
            {
                throw new ApplicationException("file too large");
            }

            //write boot vector copy with crc block
            byte[] bootAndCrc = buildBootAndCrc(file, startAddress, MaxSize);
            byte[] data = new byte[WriteBlockSize];

            for (int i = 0; i < 1024; i += WriteBlockSize)
            {
                cmd_WriteFirmwareDataBlock((uint)(BootVectorCopyAddress + i), bootAndCrc, i, WriteBlockSize);
                Console.Write(".");
            }

            //write the program
            for (uint addr = startAddress; addr < FlashFirstAddress + file.Length; addr += WriteBlockSize)
            {
                int writeSize = WriteBlockSize;
                int fileOffset = (int)(addr - FlashFirstAddress);
                if (fileOffset + WriteBlockSize > file.Length)
                {
                    writeSize = file.Length - fileOffset;
                }

                cmd_WriteFirmwareDataBlock((uint)(addr), file, fileOffset, writeSize);
                Console.Write(".");
            }
            Console.WriteLine("");

            
            
            Console.Write("Verifying");
            //Verifying boot vector copy with crc block
            for (int i = 0; i < 1024; i += WriteBlockSize)
            {
                int[] received = cmd_getFirmwareDataBlock((uint)(BootVectorCopyAddress + i), WriteBlockSize);

                if (!ArrayContain(received, bootAndCrc, i))
                {
                    throw new VerifyFailExaption("0x" + (BootVectorCopyAddress + i).ToString("X8"));
                }
                Console.Write(".");
            }

            //Verifying the program
            for (uint addr = startAddress; addr < FlashFirstAddress + file.Length; addr += WriteBlockSize)
            {
                int writeSize = WriteBlockSize;
                int fileOffset = (int)(addr - FlashFirstAddress);
                if (fileOffset + WriteBlockSize > file.Length)
                {
                    writeSize = file.Length - fileOffset;
                }

                int[] received = cmd_getFirmwareDataBlock(addr, (uint)writeSize);
                if (!ArrayContain(received,file,fileOffset))
                {
                    throw new VerifyFailExaption("0x" + addr.ToString("X8"));
                }
                Console.Write(".");
            }
            Console.WriteLine("");

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
            crc.Add(M339IntFrame.CalcCRC(CrcBuffer));
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

                crc.Add(M339IntFrame.CalcCRC(CrcBuffer));
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


        private const int WriteBlockSize = 256;
        private const int EraseBlockSize = 1024;
        
        private void cmd_EraseBlock(uint address)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillShort(0x0503); //OpCode
            buffer.fillLong(0xBCE31F90);
            buffer.fillLong(address);
            comm.Send(buffer, "Erase Firmware block " + address.ToString("X8"));
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
            buffer.fillShort(0x0502); //OpCode
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

            comm.Send(buffer, "Write Firmware block " + address.ToString("X8"));
        }

        private int[] cmd_getFirmwareDataBlock(uint address, uint length)
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillShort(0x0F00);
            buffer.fillShort(0x8501); //OpCode
            buffer.fillLong(0xBCE31F90); //Magic Number
            buffer.fillLong(address);
            buffer.fillShort(length);

            DataBuffer received = comm.SendAndReceive(buffer, "Read Firmware block " + address.ToString("X8"));

            uint receiveOpcode = received.getShort();
            uint receivedAddress = received.getLong();
            uint receivedLength = received.getShort();

            if ((receivedAddress != address) ||
               (receivedLength != length))
                throw new WrongResponceExeption();

            if (received.RemainingBytes < receivedLength)
                throw new WrongResponceExeption();

            return received.getArray((int)receivedLength);
        }

        private void cmd_ChangeFirmeware()
        {
            DataBuffer buffer = new DataBuffer();
            buffer.fillShort(0x0504); //OpCode
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
            buffer.fillShort(0x0F00);
            buffer.fillShort(0x80FF); //OpCode
            
            DataBuffer received = comm.SendAndReceive(buffer, "Get Version", false);

            uint receiveOpcode = received.getShort();
            string[] version = new string[3];
            received.getByte();
            received.getShort();
            received.getShort();
            version[0] = received.getString(32);  //version
            //version[1] = received.getString(32);  //date
            //version[2] = received.getString(64);  //name 

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

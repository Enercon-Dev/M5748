..\..\..\Scripts\srec_cat.exe %1.bin -binary -CRC32_Little_Endian -maximum-addr %1.bin -binary -o %1_crc32.bin -binary
..\..\..\Scripts\srec_cat.exe %1.hex -intel -offset -0x08000000 ..\..\BootLoader\M5748-BL\CubeIDE\Debug\M5748-BL.hex -intel -offset -0x08000000 -o %1_boot.bin -binary
